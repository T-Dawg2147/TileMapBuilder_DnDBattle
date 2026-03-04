using CommunityToolkit.Mvvm.ComponentModel;
using DnDBattle.Data.Enums;
using System.Windows.Media;

namespace DnDBattle.Data.Models
{
    public partial class Token : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Desriptive fields
        [ObservableProperty] private string _name = "Token";

        [ObservableProperty] private string _size = string.Empty;

        #region Monement Tracking

        [ObservableProperty] private int _gridX;
        [ObservableProperty] private int _gridY;

        #endregion
    }
}
