/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          primary: '#1E3A8A',   // Deep Blue  — trust, nav, primary actions
          danger:  '#DC2626',   // Red        — stolen alerts, destructive actions
          accent:  '#F59E0B',   // Amber      — pending, warnings, attention
          bg:      '#F9FAFB',   // Light Gray — page backgrounds
          text:    '#111827',   // Dark Gray  — body text
          success: '#16A34A',   // Green      — resolved, success states
          card:    '#FFFFFF',   // White      — card surfaces
          muted:   '#6B7280',   // Gray-500   — secondary text, placeholders
          border:  '#E5E7EB',   // Gray-200   — input/card borders
          subtle:  '#EFF6FF',   // Blue-50    — light blue tint for highlights
        }
      },
      boxShadow: {
        card: '0 1px 3px 0 rgb(0 0 0 / .08), 0 1px 2px -1px rgb(0 0 0 / .06)',
        'card-md': '0 4px 6px -1px rgb(0 0 0 / .08), 0 2px 4px -2px rgb(0 0 0 / .06)',
      }
    }
  },
  plugins: []
}
