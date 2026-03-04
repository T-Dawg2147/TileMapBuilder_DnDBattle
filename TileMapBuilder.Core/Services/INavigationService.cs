using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileMapBuilder.Core.Services
{
    public interface INavigationService
    {
        object? CurrentView { get; }

        void NavigateTo<TViewModel>() where TViewModel : class;

        event Action? CurrentViewChanged;
    }
}
