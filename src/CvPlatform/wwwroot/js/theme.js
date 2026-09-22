// CV Management Platform - Theme Management (Light / Dark)
(function () {
    const THEME_KEY = 'cv_platform_theme';

    function getStoredTheme() {
        return localStorage.getItem(THEME_KEY);
    }

    function getPreferredTheme() {
        const storedTheme = getStoredTheme();
        if (storedTheme) {
            return storedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
        window.dispatchEvent(new CustomEvent('themeChanged', { detail: { theme } }));
    }

    // Immediately apply theme to avoid FOUC
    const initialTheme = getPreferredTheme();
    applyTheme(initialTheme);

    // Global theme API for Blazor interoperability
    window.ThemeManager = {
        getTheme: function () {
            return getPreferredTheme();
        },
        setTheme: function (theme) {
            applyTheme(theme);
        },
        toggleTheme: function () {
            const current = getPreferredTheme();
            const next = current === 'dark' ? 'light' : 'dark';
            applyTheme(next);
            return next;
        }
    };
})();
