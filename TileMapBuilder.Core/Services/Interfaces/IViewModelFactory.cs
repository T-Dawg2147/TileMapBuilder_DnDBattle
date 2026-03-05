namespace TileMapBuilder.Core.Services.Interfaces
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
