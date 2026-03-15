(function () {
    const themesUrl = "/data/themes.json";
    let themesCache = null;

    async function loadThemes() {
        if (themesCache) {
            return themesCache;
        }

        const response = await fetch(themesUrl, { cache: "no-cache" });
        if (!response.ok) {
            throw new Error("Не удалось загрузить каталог тем.");
        }

        const themes = await response.json();
        const map = new Map(themes.map(x => [x.key, x]));

        themesCache = { list: themes, map };
        return themesCache;
    }

    function setCssVar(name, value) {
        document.documentElement.style.setProperty(name, value);
    }

    function applyThemeObject(theme) {
        if (!theme) return;

        setCssVar("--c-primary", theme.primary);
        setCssVar("--c-sand", theme.sand);
        setCssVar("--c-sage", theme.sage);
        setCssVar("--c-bg", theme.bg);
        setCssVar("--c-text", theme.text);

        if (document.body) {
            document.body.dataset.userTheme = theme.key;
        }
    }

    async function applyTheme(themeKey) {
        const catalog = await loadThemes();
        const theme =
            catalog.map.get(themeKey) ||
            catalog.map.get("earthy") ||
            catalog.list[0];

        applyThemeObject(theme);
        return theme;
    }

    async function init() {
        try {
            const themeKey = document.body?.dataset.userTheme || "earthy";
            await applyTheme(themeKey);
        } catch {
            // если JSON не загрузился — останется серверная тема из layout
        }
    }

    window.NewVkTheme = {
        loadThemes,
        applyTheme
    };

    document.addEventListener("DOMContentLoaded", function () {
        void init();
    });
})();