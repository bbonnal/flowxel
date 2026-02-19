# Flowxel Architecture

This document describes the architectural decisions currently implemented in the Flowxel workspace, why they were made, and how they should guide future development.

## Goals

- Keep `Flowxel.UI` reusable and UI-framework focused.
- Keep app composition decisions in `Flowxel.UITester` (the host app).
- Scale to more pages/features without turning startup and navigation into manual object wiring.
- Make future testing and refactoring easier by coding against interfaces.

## Project Structure

- `rUI.Avalonia.Desktop` (`Flowxel.UI` assembly/package)
  - Reusable controls (`Navigation`, `Ribbon`, `Docking`, dialogs, overlays, info bar).
  - Service abstractions and implementations (`INavigationService`, `IContentDialogService`, etc.).
- `Flowxel.UI.Controls.Drawing`
  - Shape/domain models, interaction engine, scene serialization/export contracts.
- `Flowxel.UITester`
  - App composition root, DI registrations, page ViewModels/Views.

## Key Architectural Decisions

## 1) Dependency Injection at the App Boundary

Decision:
- Use DI in `Flowxel.UITester` to construct services, main window, and page ViewModels.

Implementation:
- Composition root in `Flowxel.UITester/App.axaml.cs`.
- Registrations in `Flowxel.UITester/ServiceCollectionExtensions.cs`.

Why:
- Centralizes object graph wiring.
- Eliminates scattered `new ...` construction in ViewModels.
- Makes future replacement (mock services, alternate implementations) straightforward.

## 2) Type-Based Navigation (ViewModel-first)

Decision:
- Navigation items store `PageViewModelType` instead of page/control factories.
- `NavigationService` resolves ViewModels by type through `INavigationViewModelResolver`.

Implementation:
- `rUI.Avalonia.Desktop/Controls/Navigation/NavigationItemControl.cs`
- `rUI.Avalonia.Desktop/INavigationService.cs`
- `rUI.Avalonia.Desktop/Services/NavigationService.cs`

Why:
- Navigation is decoupled from concrete view construction.
- `rUI.Avalonia.Desktop` stays DI-framework agnostic.
- Page creation policy is owned by host composition and DI lifetimes, not ad-hoc lambdas.
- Cleaner and more predictable lifecycle handling.

## 3) Explicit View Resolution via ViewLocator

Decision:
- `Navigation.CurrentPage` is a ViewModel object.
- Avalonia `ViewLocator` resolves views through an explicit mapping table (not name-based reflection).

Implementation:
- `Flowxel.UITester/ViewLocator.cs`
- `Flowxel.UITester/ViewMappings.cs`
- `Flowxel.UITester/App.axaml` data templates.

Why:
- Keeps ViewModels view-agnostic.
- Makes missing mappings deterministic and testable.

## 4) Lifetime Policy (Improvement over naive DI)

Decision:
- Singleton: cross-cutting app services and main shell.
- Transient: page ViewModels.

Implementation:
- `Flowxel.UITester/ServiceCollectionExtensions.cs`

Why:
- Avoids stale state and accidental state sharing across navigations.
- Still keeps expensive shared services centralized.

## 5) Host Pattern for UI Services

Decision:
- Dialog/overlay/info bar controls are hosted in `MainWindow` and registered with corresponding services.

Implementation:
- `Flowxel.UITester/Views/MainWindow.axaml`
- `Flowxel.UITester/Views/MainWindow.axaml.cs`

Why:
- Clear boundary between service API and visual host.
- Service usage stays simple in ViewModels.

## 6) Cross-Cutting Infrastructure via Framework Abstractions

Decision:
- Use `Microsoft.Extensions.Logging` directly for logging and keep localization behind library-level interfaces.
- Register providers and runtime policy in the host app composition root.

Implementation:
- Logging strategy:
  - `Microsoft.Extensions.Logging.ILogger<TCategoryName>` (injected directly)
  - `Microsoft.Extensions.Logging.ILoggerFactory` (host registration)
- Translation strategy:
  - `rUI.Avalonia.Desktop/Translation/ITranslationService.cs`
  - `rUI.Avalonia.Desktop/Translation/TranslationService.cs`
  - `rUI.Avalonia.Desktop/Translation/JsonTranslationCatalogLoader.cs`

Why:
- Keeps reusable library code framework-focused and testable.
- Lets final apps choose runtime policy (providers, sinks, locales, translation sources) without changing framework APIs.
- Avoids forcing heavy infrastructure into every app.

## Consequences for Future Development

## Adding a New Page

1. Create `XxxPageViewModel` and `XxxPageView`.
2. Register `XxxPageViewModel` in DI (typically transient).
3. Add a `NavigationItemControl` with `PageViewModelType = typeof(XxxPageViewModel)`.
4. Do not manually instantiate page view/viewmodel in `MainWindowViewModel`.

## Adding a New Service

1. Define interface in `rUI.Avalonia.Desktop/Services`.
2. Implement service in `rUI.Avalonia.Desktop/Services`.
3. Register in DI in `ServiceCollectionExtensions.cs`.
4. Inject interface into consumers.
5. Keep behavior policy at host level (for example log providers, localization catalogs, culture defaults).

## Navigation Rules

- Use `INavigationService.NavigateToAsync<TViewModel>()`.
- Do not navigate by creating views/controls manually.
- Keep navigation lifecycle logic (`OnAppearingAsync` / `OnDisappearingAsync`) inside viewmodels implementing `INavigationViewModel`.

## State Rules

- Prefer transient page VMs unless the page must intentionally preserve state globally.
- If persistent state is needed, move state into a dedicated singleton state service.

## Testing Implications

- ViewModels can be tested by injecting fake/mock interfaces.
- Navigation behavior can be tested at service level by substituting DI registrations.
- Architecture guards can enforce boundary rules and mapping completeness (`rUI.ArchitectureTests`).

## Constraints and Gotchas

- Compiled bindings require valid `x:DataType` for binding-heavy XAML.
- Avoid parameterless-constructor assumptions in XAML design-time if constructors are DI-only.
- Keep DI framework usage in host app; reusable libraries should depend on abstractions, not app-specific composition logic.

## Direction Summary

The app is now directed toward a modular architecture where:
- reusable UI components live in library projects,
- composition lives at the app edge,
- navigation is ViewModel/type-driven,
- and object creation is governed by explicit DI lifetimes.

This is the intended foundation for scaling features without increasing coupling.
