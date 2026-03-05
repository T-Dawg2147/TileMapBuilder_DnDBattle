using CommunityToolkit.Mvvm.ComponentModel;
using TileMapBuilder.Core.Services.Interfaces;
using TileMapBuilder.Core.ViewModels.TileViewModels;

namespace TileMapBuilder.Core.ViewModels
{
    public sealed partial class ShellViewModel : ObservableObject
    {
        private readonly INavigationService _navigation;

        [ObservableProperty] private object? _currentView;

        public ShellViewModel(INavigationService navigation)
        {
            _navigation = navigation;
            _navigation.CurrentViewChanged += () => CurrentView = _navigation.CurrentView;

            _navigation.NavigateTo<TileMapEditorViewModel>();
        }
    }
}
