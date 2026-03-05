using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Data.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.Core.ViewModels.Dialogs
{
    public sealed partial class NewTileMap : ObservableObject
    {
        [ObservableProperty] private string _value = string.Empty;

        public string Label { get; }
        public string Placeholder { get; }
        public bool IsRequired { get; }

        public NewTileMap(string label, string placeholder = "", bool isRequired = false)
        {
            Label = label;
            Placeholder = placeholder;
            IsRequired = isRequired;
        }
    }

    public sealed partial class NewTileMapViewModel : ObservableObject
    {
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _fileLocation = string.Empty;
        [ObservableProperty] private int? _width;
        [ObservableProperty] private int? _height;
        [ObservableProperty] private string _dialogTitle = "Enter Details";
        [ObservableProperty] private bool? _dialogResult;

        public bool ShowDimensions { get; }

        public ObservableCollection<NewTileMap> AdditionalFields { get; }

        public bool HasAdditionalFields => AdditionalFields.Count > 0;

        public NewTileMapViewModel(
            bool showDimensions = false,
            IEnumerable<NewTileMap>? additionalFields = null,
            string dialogTitle = "Enter Details")
        {
            ShowDimensions = showDimensions;
            DialogTitle = dialogTitle;

            AdditionalFields = additionalFields is not null
                ? new ObservableCollection<NewTileMap>(additionalFields)
                : new ObservableCollection<NewTileMap>();
        }

        private bool CanConfirm()
        {
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (string.IsNullOrWhiteSpace(FileLocation)) return false;

            foreach (var field in AdditionalFields)
            {
                if (field.IsRequired && string.IsNullOrWhiteSpace(field.Value))
                    return false;
            }

            return true;
        }

        [RelayCommand(CanExecute = nameof(CanConfirm))] private void Confirm() => DialogResult = true;
        [RelayCommand] private void Cancel() => DialogResult = false;

        [RelayCommand]
        private void BrowseFolder()
        {
            if (_dialogService.ShowFolderDialog(out var folder))
                FileLocation = folder;        
        }
    }
}
