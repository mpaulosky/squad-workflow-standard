---
name: "blazor-tailwind-theme-persistence"
description: "Persistent dark/light + color theme across Blazor enhanced navigation using Tailwind CSS classes, localStorage, MutationObserver, and a themeManager JS object."
domain: "frontend, blazor, tailwind, theming"
confidence: "high"
source: "earned — feature/tailwind-migration branch, confirmed working by user"
tools: []
---

## ⚠️ DEPRECATION NOTICE

**This skill documents the OLD 8-theme system which has been replaced.**

**Current Architecture (2025-01-29):**

- **4 color swap classes**: `:root.color-blue`, `:root.color-red`, `:root.color-green`, `:root.color-yellow`
- **Native dark mode**: Uses `.dark` class + Tailwind's `dark:` variant
- **Split storage keys**: `theme-color` + `theme-mode` (not unified `tailwind-color-theme`)
- **Standard Tailwind palettes**: Uses Tailwind's default hex colors (not custom OKLCH)
- **CSS custom properties**: Swap `--primary-50` through `--primary-950` based on color class
- **Common elements**: Standardized `body`, `a`, `h1-h3`, `.nav-link`, `.btn-primary`, `.card` with light/dark variants

**See:** `.squad/decisions/inbox/legolas-simplified-theme-architecture.md` for full details.

**Old patterns documented below (for reference only):**

---

## Context

Applies to Blazor Server apps using Tailwind CSS for multi-color theme support (light/dark + color palette). Blazor's enhanced navigation (`enhancedload` / `blazor:navigated` events) and DOM reconciliation can strip `<html>` class attributes mid-session, causing theme resets and FOUC. This skill covers the full solution.

## Patterns (DEPRECATED)

### 1. Unified Storage Key (DEPRECATED — use split keys)

Use a single `localStorage` key that encodes both color and brightness:

```
key:   "tailwind-color-theme"
value: "theme-{color}-{brightness}"  // e.g. "theme-blue-dark", "theme-red-light"
```

Valid themes are enumerated in the IIFE so validation is fast with no network calls.

**Migration shim** — when upgrading from split keys (`colorTheme` + `darkMode`):

```js
if (!localStorage.getItem(STORAGE_KEY)) {
  var oldColor = (localStorage.getItem("colorTheme") || "theme-blue").replace("theme-", "");
  if (["red", "blue", "green", "yellow"].indexOf(oldColor) === -1) oldColor = "blue";
  var brightness = localStorage.getItem("darkMode") === "true" ? "dark" : "light";
  localStorage.setItem(STORAGE_KEY, "theme-" + oldColor + "-" + brightness);
}
```

### 2. Anti-FOUC IIFE in `<head>` Before Stylesheets

Place the entire initialization script as the **first child of `<head>`**, before any `<link>` elements. This guarantees the theme class is on `<html>` before the browser parses CSS selectors.

```razor
<!-- App.razor -->
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <base href="/" />
  <script>
    (function () {
      var STORAGE_KEY = "tailwind-color-theme";
      var VALID_THEMES = [
        "theme-blue-light", "theme-blue-dark",
        "theme-red-light",  "theme-red-dark",
        "theme-green-light","theme-green-dark",
        "theme-yellow-light","theme-yellow-dark"
      ];

      function applyThemeFromStorage() {
        var theme = localStorage.getItem(STORAGE_KEY) || "theme-blue-light";
        if (VALID_THEMES.indexOf(theme) === -1) theme = "theme-blue-light";
        var html = document.documentElement;
        VALID_THEMES.forEach(function (t) { html.classList.remove(t); });
        html.classList.add(theme);
        if (theme.indexOf("-dark") !== -1) {
          html.classList.add("dark");
          html.classList.remove("light");
        } else {
          html.classList.remove("dark");
          html.classList.add("light");
        }
      }

      // ... migration shim, navigation hooks, MutationObserver (see below)
    })();
  </script>
  <link rel="stylesheet" href="css/tailwind.css" />
  <!-- other links -->
</head>
```

### 3. Navigation Re-application (Blazor Events)

Listen to both Blazor navigation events and apply with an immediate + delayed retry to handle post-navigation DOM reconciliation:

```js
function onNavigation() {
  if (window.themeManager) {
    window.themeManager.applyTheme();
  } else {
    applyThemeFromStorage();
  }
  setTimeout(function () {
    if (window.themeManager) {
      window.themeManager.applyTheme();
    } else {
      applyThemeFromStorage();
    }
  }, 100);
}

document.addEventListener("enhancedload", onNavigation);
document.addEventListener("blazor:navigated", onNavigation);
```

### 4. MutationObserver Guard

Blazor's DOM diffing can strip classes from `<html>` outside of navigation events. Guard against this with a `MutationObserver`:

```js
if (window._themeObserver) window._themeObserver.disconnect();

var _reapplying = false;
window._themeObserver = new MutationObserver(function () {
  if (_reapplying) return;
  var html = document.documentElement;
  var hasTheme = VALID_THEMES.some(function (t) { return html.classList.contains(t); });
  if (!hasTheme) {
    _reapplying = true;
    applyThemeFromStorage();
    _reapplying = false;
  }
});
window._themeObserver.observe(document.documentElement, {
  attributes: true,
  attributeFilter: ["class"]
});
```

The `_reapplying` flag prevents the observer from firing on its own writes.

### 5. `themeManager` JS Object (body script)

The `window.themeManager` object (loaded from `wwwroot/js/theme.js` at end of `<body>`) exposes `applyTheme()` and `setTheme(theme)` for Blazor components to call via `IJSRuntime`. The IIFE in `<head>` defers to it when present, keeping the two in sync.

### 6. Footer / Layout Color Consistency

Footer background should match the primary navigation color. Use the same Tailwind color class as the nav:

```razor
<!-- MainLayout.razor -->
<footer class="bg-primary-400 ...">
```

This keeps the footer visually aligned with the nav across all color themes.

### 7. Tailwind CSS Class Architecture

Theme classes are applied to `<html>`. Tailwind selectors use the `.dark` variant and custom theme variants:

```
<html class="theme-blue-dark dark">
```

CSS (tailwind.css / input.css):

```css
.theme-blue-light  { --color-primary: ...; }
.theme-blue-dark   { --color-primary: ...; }
/* etc. */
.dark .some-element { ... }
```

## Examples

**Full `App.razor` head script** — see `src/Web/Components/App.razor` on `feature/tailwind-migration` branch (commits 8105239, 4c41df0, c56ac47, ed53a8d).

**Key commits on `feature/tailwind-migration`:**

| SHA     | What                                           |
| --------- | ------------------------------------------------ |
| 8105239 | Move IIFE to `<head>` — FOUC prevention        |
| 4c41df0 | Footer color consistency                       |
| c56ac47 | MutationObserver + delayed retry               |
| ed53a8d | Docs / orchestration log                       |

## Anti-Patterns

- ❌ **Script at end of `<body>`** — causes FOUC; theme applies after stylesheets paint
- ❌ **Split storage keys** (`colorTheme` + `darkMode`) — hard to keep in sync; use unified key
- ❌ **Setting theme only on `DOMContentLoaded`** — fires too late for Blazor enhanced nav
- ❌ **Not disconnecting the old MutationObserver before creating a new one** — causes observer leak across Blazor hot-reload cycles
- ❌ **Calling `IJSRuntime` from `OnInitializedAsync` to set theme** — races with pre-render; always use the IIFE for initial application
- ❌ **Hard-coding `dark`/`light` class without removing the opposite** — leaves stale classes causing Tailwind selector conflicts
