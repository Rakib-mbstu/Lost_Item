using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lost_Item.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldLengthLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Trim any oversized test data before narrowing columns ────────────────
            migrationBuilder.Sql("UPDATE [Products]  SET [Brand]         = LEFT([Brand],         100) WHERE LEN([Brand])         > 100");
            migrationBuilder.Sql("UPDATE [Products]  SET [Model]         = LEFT([Model],         100) WHERE LEN([Model])         > 100");
            migrationBuilder.Sql("UPDATE [Products]  SET [TrackingId]    = LEFT([TrackingId],    100) WHERE LEN([TrackingId])    > 100");
            migrationBuilder.Sql("UPDATE [Products]  SET [IMEI]          = LEFT([IMEI],           20) WHERE LEN([IMEI])          > 20");
            migrationBuilder.Sql("UPDATE [Products]  SET [FrameNumber]   = LEFT([FrameNumber],    50) WHERE LEN([FrameNumber])   > 50");
            migrationBuilder.Sql("UPDATE [Products]  SET [EngineNumber]  = LEFT([EngineNumber],   50) WHERE LEN([EngineNumber])  > 50");
            migrationBuilder.Sql("UPDATE [Products]  SET [SerialNumber]  = LEFT([SerialNumber],  100) WHERE LEN([SerialNumber])  > 100");
            migrationBuilder.Sql("UPDATE [Products]  SET [MacAddress]    = LEFT([MacAddress],     17) WHERE LEN([MacAddress])    > 17");
            migrationBuilder.Sql("UPDATE [Complaints] SET [LocationStolen] = LEFT([LocationStolen], 200) WHERE LEN([LocationStolen]) > 200");

            // ── Non-indexed columns — safe to use AlterColumn directly ──────────────
            migrationBuilder.AlterColumn<string>(
                name: "Brand", table: "Products",
                type: "nvarchar(100)", maxLength: 100, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Model", table: "Products",
                type: "nvarchar(100)", maxLength: 100, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LocationStolen", table: "Complaints",
                type: "nvarchar(200)", maxLength: 200, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            // ── TrackingId — regular unique index, always exists ─────────────────────
            migrationBuilder.DropIndex(name: "IX_Products_TrackingId", table: "Products");
            migrationBuilder.AlterColumn<string>(
                name: "TrackingId", table: "Products",
                type: "nvarchar(100)", maxLength: 100, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(450)");
            migrationBuilder.CreateIndex(
                name: "IX_Products_TrackingId", table: "Products",
                column: "TrackingId", unique: true);

            // ── Nullable indexed columns — use raw SQL to avoid EF auto-managing ─────
            // EF's AlterColumn() internally drops and recreates indexes it knows about,
            // causing a failure when the index doesn't exist. Raw SQL lets us be explicit.

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_IMEI' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_IMEI] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [IMEI] nvarchar(20) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_IMEI' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_IMEI]
                        ON [Products] ([IMEI]) WHERE [IMEI] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_FrameNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_FrameNumber] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [FrameNumber] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_FrameNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_FrameNumber]
                        ON [Products] ([FrameNumber]) WHERE [FrameNumber] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_EngineNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_EngineNumber] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [EngineNumber] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_EngineNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_EngineNumber]
                        ON [Products] ([EngineNumber]) WHERE [EngineNumber] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_SerialNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_SerialNumber] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [SerialNumber] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_SerialNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_SerialNumber]
                        ON [Products] ([SerialNumber]) WHERE [SerialNumber] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_MacAddress' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_MacAddress] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [MacAddress] nvarchar(17) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_MacAddress' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_MacAddress]
                        ON [Products] ([MacAddress]) WHERE [MacAddress] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Brand", table: "Products",
                type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Model", table: "Products",
                type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LocationStolen", table: "Complaints",
                type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(200)", oldMaxLength: 200);

            migrationBuilder.DropIndex(name: "IX_Products_TrackingId", table: "Products");
            migrationBuilder.AlterColumn<string>(
                name: "TrackingId", table: "Products",
                type: "nvarchar(450)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
            migrationBuilder.CreateIndex(
                name: "IX_Products_TrackingId", table: "Products",
                column: "TrackingId", unique: true);

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_IMEI' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_IMEI] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [IMEI] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_IMEI' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_IMEI]
                        ON [Products] ([IMEI]) WHERE [IMEI] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_FrameNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_FrameNumber] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [FrameNumber] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_FrameNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_FrameNumber]
                        ON [Products] ([FrameNumber]) WHERE [FrameNumber] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_EngineNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_EngineNumber] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [EngineNumber] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_EngineNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_EngineNumber]
                        ON [Products] ([EngineNumber]) WHERE [EngineNumber] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_SerialNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_SerialNumber] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [SerialNumber] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_SerialNumber' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_SerialNumber]
                        ON [Products] ([SerialNumber]) WHERE [SerialNumber] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Products_MacAddress' AND object_id = OBJECT_ID(N'[Products]'))
                    DROP INDEX [IX_Products_MacAddress] ON [Products];
                ALTER TABLE [Products] ALTER COLUMN [MacAddress] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Products_MacAddress' AND object_id = OBJECT_ID(N'[Products]'))
                    CREATE UNIQUE INDEX [IX_Products_MacAddress]
                        ON [Products] ([MacAddress]) WHERE [MacAddress] IS NOT NULL;");
        }
    }
}
