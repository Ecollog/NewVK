document.addEventListener("DOMContentLoaded", function () {
    const themeSelect = document.getElementById("theme-select");
    const previewName = document.getElementById("theme-preview-name");

    if (!themeSelect) return;

    themeSelect.addEventListener("change", async function () {
        if (!window.NewVkTheme) return;

        const theme = await window.NewVkTheme.applyTheme(themeSelect.value);
        if (previewName && theme?.name) {
            previewName.textContent = theme.name;
        }
    });
});