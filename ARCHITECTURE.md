# Architecture Guide — TileMapBuilder_DnDBattle

## 5a. Solution Structure

```
TileMapBuilder_DnDBattle/
│
├── TileMapBuilder.App.slnx              ← Visual Studio solution file
├── Directory.Build.props                 ← Shared MSBuild properties
├── ARCHITECTURE.md                       ← This document
│
├── DnDBattle.Data/                       ← Leaf project: Models, Enums, Data Services
│   ├── DnDBattle.Data.csproj             ← net8.0, CommunityToolkit.Mvvm, System.Text.Json
│   ├── Enums/
│   │   └── DamageType.cs                 ← DamageType flags enum + extension methods (GetIcon, GetColor, etc.)
│   ├── Models/
│   │   ├── Token.cs                      ← Token model (GridX, GridY, Name, Size)
│   │   └── Tiles/
│   │       ├── MapNote.cs                ← Map note model + NoteCategory enum
│   │       ├── Tile.cs                   ← Placed tile instance (GridX, GridY, Rotation, Metadata, etc.)
│   │       ├── TileDefinition.cs         ← Tile definition (Id, ImagePath, Layer, Category, etc.)
│   │       ├── TileLayer.cs              ← TileLayer enum (Floor..Roof) + extension methods
│   │       ├── TileMap.cs                ← TileMap model (Width, Height, PlacedTiles, etc.) + TileMapDto
│   │       └── Metadata/
│   │           ├── TileMetadata.cs       ← Abstract base + TileMetadataType enum + extension methods
│   │           ├── TrapMetadata.cs       ← Trap-specific metadata (DC, damage, triggers, etc.)
│   │           └── SpawnMetadata.cs      ← Spawn point metadata (creature, count, radius, etc.)
│   └── Services/
│       ├── FogOfWarState.cs              ← Fog of war tracking + FogMode enum
│       ├── NativeFolderBrowser.cs        ← Windows COM folder-picker dialog (interop)
│       ├── ServiceLocator.cs             ← Simple static service locator (legacy)
│       ├── UndoManager.cs                ← Undo/redo stack manager
│       ├── Interfaces/
│       │   ├── IUndoableAction.cs        ← Do/Undo/Description interface
│       │   ├── ITileImageCacheService.cs ← Platform-agnostic image caching interface
│       │   ├── ITileLibraryService.cs    ← Tile library loading/querying interface
│       │   └── ITileMapService.cs        ← Map save/load interface
│       ├── TileService/
│       │   ├── TileLibraryService.cs     ← Scans disk for tile images, builds library
│       │   └── TileMapService.cs         ← JSON serialization/deserialization of tile maps
│       └── UndoRedo/
│           └── TileMapUndoAction.cs      ← Undo actions: TilePlaceAction, TileRemoveAction,
│                                            TileBatchAction, TileMetadataAction, MapPropertyChangeAction
│
├── TileMapBuilder.Core/                  ← Middle layer: ViewModels + UI-facing interfaces
│   ├── TileMapBuilder.Core.csproj        ← net8.0, CommunityToolkit.Mvvm, refs DnDBattle.Data
│   ├── Services/
│   │   └── Interfaces/
│   │       ├── IDialogService.cs         ← Dialog abstraction + DialogIcon enum (no WPF types)
│   │       ├── IImageExportService.cs    ← Image export abstraction (object, not Visual)
│   │       ├── IMapVisualProvider.cs     ← Map visual abstraction (object?, not Visual)
│   │       ├── INavigationService.cs     ← Shell navigation interface
│   │       └── IViewModelFactory.cs      ← ViewModel creation via DI
│   └── ViewModels/
│       ├── ShellViewModel.cs             ← Root ViewModel, hosts CurrentView
│       ├── Controls/
│       │   └── TilePaletteViewModel.cs   ← Tile palette search, grouping, selection
│       ├── Dialogs/
│       │   └── NewTileMapViewModel.cs    ← New map dialog ViewModel (name, dims, file location)
│       └── TileViewModels/
│           ├── TileMapControlViewModel.cs ← Map editing: paint/erase/select, undo/redo, zoom/pan, layers
│           └── TileMapEditorViewModel.cs  ← Map file operations: new, load, save, export
│
└── TileMapBuilder.App/                   ← WPF UI project: Views, WPF services, converters
    ├── TileMapBuilder.App.csproj         ← net8.0-windows, UseWPF, DI, refs Core + Data
    ├── App.xaml                          ← Resource dictionaries, global styles, converter keys
    ├── App.xaml.cs                       ← DI container setup, startup, service registration
    ├── AssemblyInfo.cs
    ├── Converters/
    │   ├── RequiredLabelConverter.cs     ← IValueConverter: bool → "Required"/""
    │   └── TileImageConverter.cs         ← IValueConverter: imagePath → BitmapImage (via cache)
    ├── Controls/
    │   ├── MinimapControl.xaml/.cs       ← Minimap overlay (placeholder)
    │   ├── TileMapToolbarControl.xaml/.cs ← Map canvas + floating toolbar + mouse handling
    │   └── TilePalettePanel.xaml/.cs     ← Tile selection panel with search
    ├── Services/
    │   ├── DialogService.cs              ← WPF IDialogService implementation (MessageBox, file dialogs)
    │   ├── ImageExportService.cs         ← WPF image export (RenderTargetBitmap)
    │   ├── MapVisualProviderHolder.cs    ← Provides Visual reference for export
    │   ├── NavigationService.cs          ← ViewModel-based navigation
    │   ├── TileImageCacheService.cs      ← WPF BitmapImage cache (implements ITileImageCacheService)
    │   └── ViewModelFactory.cs           ← DI-backed ViewModel factory
    └── Views/
        ├── MainWindow.xaml/.cs           ← Shell window with DataTemplate-based content
        ├── TileMapEditorWindow.xaml/.cs  ← Editor UserControl (wires child ViewModels)
        └── Dialogs/
            └── NewTileMapDialog.xaml/.cs ← New map dialog window
```

## 5b. Dependency Chain

```
TileMapBuilder.App  (WPF UI — net8.0-windows)
    ├── references → TileMapBuilder.Core
    └── references → DnDBattle.Data

TileMapBuilder.Core  (ViewModels + Interfaces — net8.0)
    └── references → DnDBattle.Data

DnDBattle.Data  (Models + Data Services — net8.0)
    └── references → nothing (leaf project)
```

**Key rules:**
- `DnDBattle.Data` has **zero** references to Core or App
- `TileMapBuilder.Core` has **zero** references to App
- `TileMapBuilder.Core` has **zero** `System.Windows.*` references
- `DnDBattle.Data` has **zero** `System.Windows.*` references (except a `[SupportedOSPlatform("windows")]` COM interop class)
- Only `TileMapBuilder.App` may use WPF types (`System.Windows.*`)

## 5c. What Must Stay in the UI Project

| Item | What It Is | Why It Can't Move | WPF Dependencies |
|------|-----------|-------------------|------------------|
| `DialogService` | Shows file dialogs, message boxes, and custom dialog windows | Uses `MessageBox`, `OpenFileDialog`, `SaveFileDialog`, `Window.ShowDialog()` | `System.Windows.MessageBox`, `Microsoft.Win32.OpenFileDialog/SaveFileDialog`, `System.Windows.Window` |
| `ImageExportService` | Renders a visual to a bitmap file | Uses `RenderTargetBitmap`, `VisualBrush`, `DrawingVisual` | `System.Windows.Media.Imaging.*`, `System.Windows.Media.Visual` |
| `MapVisualProviderHolder` | Provides the map canvas Visual for export | Wraps a `Func<Visual?>` referencing the actual WPF canvas | `System.Windows.Media.Visual` |
| `NavigationService` | Swaps the `CurrentView` on the shell | Implementation is UI-neutral, but kept in App for DI wiring alongside other App services | (none directly, but tightly coupled to App DI) |
| `ViewModelFactory` | Creates ViewModels from the DI container | Uses `IServiceProvider.GetRequiredService<T>()` from `Microsoft.Extensions.DependencyInjection` | `Microsoft.Extensions.DependencyInjection` |
| `TileImageCacheService` | Loads tile images into `BitmapImage` objects and caches them | The entire purpose is creating WPF `BitmapImage` objects | `System.Windows.Media.Imaging.BitmapImage`, `BitmapCacheOption` |
| `RequiredLabelConverter` | Converts `bool` → `"Required"` string for labels | Implements `System.Windows.Data.IValueConverter` | `System.Windows.Data.IValueConverter` |
| `TileImageConverter` | Converts image path → `BitmapImage` for XAML bindings | Implements `IValueConverter`, uses `TileImageCacheService` | `System.Windows.Data.IValueConverter` |
| All `.xaml` files | XAML views, controls, resource dictionaries | XAML is inherently a WPF technology | Everything in `System.Windows.*` |
| `App.xaml` / `App.xaml.cs` | Application entry point, DI setup, resource dictionaries | Must reference `Application`, set up the DI container, create the main window | `System.Windows.Application` |
| `TileMapToolbarControl.xaml.cs` | Canvas mouse handling, tile rendering, fog of war rendering | Directly manipulates `Canvas.Children`, creates WPF `Image`, `Line`, `Rectangle` elements | `System.Windows.Controls.Canvas`, `System.Windows.Shapes.*`, `System.Windows.Media.*` |

## 5d. How to Connect ViewModels to Views

### DataTemplate Approach (Implicit DataTemplates)

In `MainWindow.xaml`, the `Window.Resources` section contains a `DataTemplate` keyed by ViewModel type:

```xml
<DataTemplate DataType="{x:Type tileVm:TileMapEditorViewModel}">
    <!-- The view content for this ViewModel -->
</DataTemplate>
```

The `MainWindow` binds a `ContentPresenter` to `ShellViewModel.CurrentView`:

```xml
<ContentPresenter Content="{Binding CurrentView}" />
```

When `CurrentView` is set to a `TileMapEditorViewModel`, WPF automatically uses the matching `DataTemplate` to render the view.

### Code-Behind DataContext Assignment

For child controls that need their own ViewModels (e.g., `TilePalettePanel`, `TileMapToolbarControl`), the parent view resolves them from DI and assigns `DataContext` in code-behind:

```csharp
private void OnLoaded(object sender, RoutedEventArgs e)
{
    _mapControlVm = App.Services.GetRequiredService<TileMapControlViewModel>();
    _paletteVm = App.Services.GetRequiredService<TilePaletteViewModel>();

    MapEditorControl.DataContext = _mapControlVm;
    PalettePanel.DataContext = _paletteVm;
}
```

### DI Container Registration

In `App.xaml.cs`, services and ViewModels are registered:

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Singletons for services
    services.AddSingleton<IDialogService, DialogService>();
    services.AddSingleton<ITileLibraryService>(sp =>
        new TileLibraryService(sp.GetRequiredService<ITileImageCacheService>()));

    // Transient for ViewModels (new instance each time)
    services.AddTransient<ShellViewModel>();
    services.AddTransient<TileMapEditorViewModel>();
}
```

### Shell/Navigation Pattern

1. `ShellViewModel` has a `CurrentView` property
2. `INavigationService.NavigateTo<TViewModel>()` uses `IViewModelFactory` to create the ViewModel
3. It sets `CurrentView` on `ShellViewModel`, which fires `PropertyChanged`
4. The `ContentPresenter` in `MainWindow` picks up the change and renders the matching `DataTemplate`

### Dialog Windows (PropertyChanged → DialogResult)

For modal dialogs (e.g., `NewTileMapDialog`):

1. The ViewModel has a `DialogResult` property (`bool?`)
2. The dialog window's code-behind subscribes to `PropertyChanged`
3. When `DialogResult` changes, the code-behind sets `Window.DialogResult`:

```csharp
private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(NewTileMapViewModel.DialogResult))
    {
        var vm = (NewTileMapViewModel)sender!;
        if (vm.DialogResult.HasValue)
            DialogResult = vm.DialogResult.Value;
    }
}
```

## 5e. How to Add New Metadata Classes

### Step 1: Create the Metadata Class

In `DnDBattle.Data/Models/Tiles/Metadata/`, create a new class inheriting from `TileMetadata`:

```csharp
namespace DnDBattle.Data.Models.Tiles.Metadata
{
    public sealed class TeleporterMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Teleporter;

        public string DestinationMapId { get; set; } = string.Empty;
        public int DestinationX { get; set; }
        public int DestinationY { get; set; }
        public bool IsTwoWay { get; set; } = true;
    }
}
```

### Step 2: Add the Enum Value

The `TileMetadataType` enum in `TileMetadata.cs` already uses `[Flags]` with bit-shifted values:

```csharp
Teleporter = 1 << 6, // Already exists
```

If adding a new type, use the next available bit shift value.

### Step 3: Add Extension Methods

In `TileMetadataTypeExtension` (in `TileMetadata.cs`), add entries:

```csharp
public static string GetDisplayName(this TileMetadataType type)
{
    // Add: TileMetadataType.Teleporter => "Teleporter",
}

public static string GetIcon(this TileMetadataType type)
{
    // Add: TileMetadataType.Teleporter => "🌀",
}
```

### Step 4 (Optional): Create a ViewModel

In `TileMapBuilder.Core/ViewModels/`, create a ViewModel for editing the metadata properties if needed.

### Step 5 (Optional): Add UI

In `TileMapBuilder.App`, add XAML for displaying/editing the metadata.

### Metadata Border Colors and Icon Backgrounds

In `TileMapToolbarControl.xaml.cs`, the `GetMetadataBorderBrush()` method maps metadata types to border colors:

- **Trap** → Red (`#F44336`)
- **Hazard** → Orange (`#FF9800`)
- **Secret** → Yellow (`#FFEB3B`)
- **Interactive** → Blue (`#2196F3`)
- **Default** → Gray (`#9E9E9E`)

Traps additionally get a red drop-shadow glow effect. To add a new color, add a case to `GetMetadataBorderBrush()`.

## 5f. How to Consume Core + Data in Another Project

### NuGet Packages Needed

The consuming project needs:
- `CommunityToolkit.Mvvm` (8.4.0+) — used by model classes and ViewModels
- `Microsoft.Extensions.DependencyInjection` — for DI container setup
- `System.Text.Json` (8.0+) — pulled transitively by DnDBattle.Data

### Service Interfaces That Need Implementations

When consuming Core + Data in a new project, you must provide implementations for these **Core** interfaces:

| Interface | Purpose | Example |
|-----------|---------|---------|
| `IDialogService` | File dialogs, message boxes, confirmations | Map to your platform's dialog system |
| `IImageExportService` | Export a visual to an image file | Platform-specific rendering |
| `IMapVisualProvider` | Provide the map visual for export | Return your rendering surface |
| `INavigationService` | Navigate between ViewModels | Set `CurrentView` on your shell |
| `IViewModelFactory` | Create ViewModels from DI | Wrap `IServiceProvider` |

And this **Data** interface:

| Interface | Purpose | Example |
|-----------|---------|---------|
| `ITileImageCacheService` | Load and cache tile images | Platform-specific image loading |

### DI Registration Example

```csharp
var services = new ServiceCollection();

// Data services (these have default implementations)
services.AddSingleton<ITileImageCacheService, YourImageCacheService>();
services.AddSingleton<ITileLibraryService>(sp =>
    new TileLibraryService(sp.GetRequiredService<ITileImageCacheService>()));
services.AddSingleton<ITileMapService, TileMapService>();

// Core UI-facing services (you provide implementations)
services.AddSingleton<IDialogService, YourDialogService>();
services.AddSingleton<INavigationService, YourNavigationService>();
services.AddSingleton<IViewModelFactory, YourViewModelFactory>();
services.AddSingleton<IMapVisualProvider, YourMapVisualProvider>();
services.AddSingleton<IImageExportService, YourImageExportService>();

// ViewModels
services.AddTransient<ShellViewModel>();
services.AddTransient<TileMapEditorViewModel>();
services.AddTransient<TileMapControlViewModel>();

var provider = services.BuildServiceProvider();
```

### Non-WPF Consuming Projects

**Console App:**
- Implement `IDialogService` with console prompts (`Console.ReadLine()`)
- Implement `ITileImageCacheService` to return `null` or use `System.Drawing` / SkiaSharp
- Skip `IMapVisualProvider` and `IImageExportService` (or provide no-op implementations)

**ASP.NET:**
- Implement `IDialogService` to throw or return defaults (no user dialogs in web)
- Implement `ITileImageCacheService` using SkiaSharp or ImageSharp
- Map save/load can work directly via `ITileMapService`

**MAUI:**
- Implement `IDialogService` using MAUI's `DisplayAlert` / `FilePicker`
- Implement `ITileImageCacheService` using MAUI's image system
- ViewModels work as-is since they use CommunityToolkit.Mvvm (also used in MAUI)

### Minimal Setup Code (Console Example)

```csharp
using DnDBattle.Data.Services.TileService;
using DnDBattle.Data.Models.Tiles;

// Direct usage without DI:
var libraryService = new TileLibraryService();
libraryService.LoadTileLibrary();

var mapService = new TileMapService(libraryService);
var map = new TileMap { Name = "Test Map", Width = 20, Height = 20 };
await mapService.SaveMapAsync(map, "test.json");
```
