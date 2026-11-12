using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DynamicThemeColor.Core;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK;

namespace DynamicThemeColor.UI.ViewModels {
    public class AddFilterViewModel : ObservableObject {
        public List<FilterCandidateViewModel> Candidates { get; }
        public string DescriptionMessage { get; }

        private FilterCandidateViewModel selectedItem;
        [CanBeNull]
        public FilterCandidateViewModel SelectedItem {
            get => selectedItem;
            set => SetValue(ref selectedItem, value);
        }

        private bool editOnAdd;
        public bool EditOnAdd {
            get => editOnAdd;
            set => SetValue(ref editOnAdd, value);
        }

        public RelayCommand<Window> SelectCommand { get; }

        public AddFilterViewModel(IEnumerable<FilterDescriptorNew> candidates, FilterStage stage, bool editOnAdd) {
            Candidates = candidates
                .Select(c => new FilterCandidateViewModel(c.Attribute))
                .OrderBy(a => a.Name)
                .ThenBy(a => a.Version)
                .ToList();
            SelectedItem = null;
            EditOnAdd = editOnAdd;

            DescriptionMessage = string.Format(
                ResourceProvider.GetString("LOCDTCFilterSelectDescription"), stage);
            SelectCommand = new RelayCommand<Window>(ExecuteSelect, window => SelectedItem != null);
        }

        private static void ExecuteSelect(Window window) {
            if (window == null) return;
            window.DialogResult = true;
        }
    }

    public class FilterCandidateViewModel {
        private readonly SchemeFilterAttribute attribute;

        public Guid FilterId => attribute.FilterId;
        public string Name => attribute.Name;
        public string Author => attribute.Author;
        public int Version => attribute.Version;
        public string Description => attribute.Description;

        public FilterCandidateViewModel(SchemeFilterAttribute attribute) {
            this.attribute = attribute;
        }
    }
}
