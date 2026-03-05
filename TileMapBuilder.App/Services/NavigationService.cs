using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.App.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IViewModelFactory _factory;

        public object? CurrentView { get; private set; }
        public event Action? CurrentViewChanged;

        public NavigationService(IViewModelFactory factory)
        {
            _factory = factory;
        }

        public void NavigateTo<TViewModel>() where TViewModel : class
        {
            CurrentView = _factory.Create<TViewModel>();
            CurrentViewChanged?.Invoke();
        }
    }
}
