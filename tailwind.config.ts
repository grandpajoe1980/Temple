import type { Config } from "tailwindcss";

const config: Config = {
  darkMode: ["class"],
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
    "./lib/**/*.{ts,tsx}"
  ],
  theme: {
    extend: {
      colors: {
        sand: {
          50: "#fff9f5",
          100: "#fef1e6",
          200: "#fbd9be",
          300: "#f7c195",
          400: "#f1a267",
          500: "#e4793c",
          600: "#c45b2a",
          700: "#9a4421",
          800: "#6c2f18",
          900: "#3b180c"
        }
      },
      borderRadius: {
        xl: "1.25rem"
      }
    }
  },
  plugins: []
};

export default config;
