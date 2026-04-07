using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Lost_Item.Data;
using Lost_Item.Filters;
using Lost_Item.Services;

Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Services
builder.Services.AddScoped<IAuthService,AuthService>();
builder.Services.AddScoped<IProductService,ProductService>();
builder.Services.AddScoped<IComplaintService,ComplaintService>();

// JWT Auth
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"AUTH FAILED: {context.Exception.GetType().Name}");
                Console.WriteLine($"AUTH FAILED MSG: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                    Console.WriteLine($"INNER: {context.Exception.InnerException.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"CHALLENGE: {context.Error}");
                Console.WriteLine($"CHALLENGE DESC: {context.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context => Task.CompletedTask
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("isAdmin", "True"));
});
builder.Services.AddControllers(options =>
{
    options.Filters.Add<TrimStringInputFilter>();
});

// CORS for React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dev", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Rate limiting — per client IP, sliding window
builder.Services.AddRateLimiter(options =>
{
    static string GetClientIp(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // POST /api/auth/google — 10 requests / 1 min
    options.AddPolicy("auth-login", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(GetClientIp(ctx), _ =>
            new SlidingWindowRateLimiterOptions
            {
                Window             = TimeSpan.FromMinutes(1),
                SegmentsPerWindow  = 6,
                PermitLimit        = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit         = 0,
            }));

    // GET /api/search — 30 requests / 1 min
    options.AddPolicy("search", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(GetClientIp(ctx), _ =>
            new SlidingWindowRateLimiterOptions
            {
                Window             = TimeSpan.FromMinutes(1),
                SegmentsPerWindow  = 6,
                PermitLimit        = 30,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit         = 0,
            }));

    // POST /api/complaints — 5 requests / 1 min
    options.AddPolicy("complaints-create", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(GetClientIp(ctx), _ =>
            new SlidingWindowRateLimiterOptions
            {
                Window             = TimeSpan.FromMinutes(1),
                SegmentsPerWindow  = 6,
                PermitLimit        = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit         = 0,
            }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please wait before trying again.", ct);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "StolenTracker API", Version = "v1" });

    // Allow JWT input in Swagger UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "StolenTracker API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("Dev");              // ← move to top, before everything

app.UseStaticFiles();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();