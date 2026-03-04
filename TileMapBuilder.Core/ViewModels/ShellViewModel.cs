using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileMapBuilder.Core.Services;
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
