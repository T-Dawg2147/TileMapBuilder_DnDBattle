using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileMapBuilder.Core.Services
{
    public interface IViewModelFactory
    {
        /// <summary>
        /// Responsible for creating any ViewModel with all its dependencies
        /// automatically resolved. ViewModels call this when they need to
        /// create other ViewModels (e.g., during navigation).
        /// </summary>
        T Create<T>() where T : class;
    }
}
