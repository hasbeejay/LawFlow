/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,cs,html}",
    "./Pages/**/*.{razor,cs,html}",
    "./Services/**/*.cs",
    "./wwwroot/**/*.html",
  ],
  // Classes that are only referenced dynamically (e.g. via C# string concat
  // for chart slice tones) must be safelisted so Tailwind's purge keeps them.
  safelist: [
    'lf-stroke-primary', 'lf-stroke-info', 'lf-stroke-success', 'lf-stroke-warning', 'lf-stroke-danger',
    'lf-area-primary', 'lf-area-info', 'lf-area-success', 'lf-area-warning',
    'lf-grid-line', 'lf-stroke-track',
    'bg-primary', 'bg-info', 'bg-success', 'bg-warning', 'bg-danger', 'bg-muted',
    'text-primary', 'text-info', 'text-success', 'text-warning', 'text-danger',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Midnight Gavel — driven by CSS variables so a single `.dark` class swap works.
        ink:        'rgb(var(--lf-ink) / <alpha-value>)',
        muted:      'rgb(var(--lf-muted) / <alpha-value>)',
        canvas:     'rgb(var(--lf-canvas) / <alpha-value>)',
        surface:    'rgb(var(--lf-surface) / <alpha-value>)',
        elevated:   'rgb(var(--lf-elevated) / <alpha-value>)',
        hairline:   'rgb(var(--lf-hairline) / <alpha-value>)',
        primary:    'rgb(var(--lf-primary) / <alpha-value>)',
        'primary-fg':'rgb(var(--lf-primary-fg) / <alpha-value>)',
        amber:      'rgb(var(--lf-amber) / <alpha-value>)',
        success:    'rgb(var(--lf-success) / <alpha-value>)',
        warning:    'rgb(var(--lf-warning) / <alpha-value>)',
        danger:     'rgb(var(--lf-danger) / <alpha-value>)',
        info:       'rgb(var(--lf-info) / <alpha-value>)',
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'monospace'],
      },
      fontSize: {
        '2xs': ['10px', { lineHeight: '14px', letterSpacing: '0.04em' }],
      },
      borderRadius: {
        'xs': '4px',
        'card': '10px',
      },
      boxShadow: {
        'hairline': '0 0 0 1px rgb(var(--lf-hairline) / 1)',
        'glow-primary': '0 0 0 4px rgb(var(--lf-primary) / 0.18)',
        'pop': '0 10px 30px -12px rgb(0 0 0 / 0.18), 0 4px 10px -4px rgb(0 0 0 / 0.08)',
      },
      keyframes: {
        'fade-in-up': {
          '0%': { opacity: '0', transform: 'translateY(6px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        'pulse-dot': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.45' },
        },
      },
      animation: {
        'fade-in-up': 'fade-in-up 240ms cubic-bezier(0.16, 1, 0.3, 1) both',
        'pulse-dot': 'pulse-dot 1.8s ease-in-out infinite',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms')({ strategy: 'class' }),
  ],
};
