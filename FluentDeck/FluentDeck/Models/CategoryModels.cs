using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace FluentDeck.Models
{
    public class CategoryMetadataItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class CategoryMetadataRoot
    {
        public System.Collections.Generic.Dictionary<string, CategoryMetadataItem> Categories { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
    }

    public class CategoryNode
    {
        public string Name { get; set; } = "";
        public string RawName { get; set; } = "";
        public string Tag { get; set; } = "";
        public string IconPath { get; set; } = "";
        public System.Uri? IconUri => System.Uri.TryCreate(IconPath, System.UriKind.Absolute, out var uri) ? uri : null;
        public Visibility IconVisibility => !string.IsNullOrEmpty(IconPath) ? Visibility.Visible : Visibility.Collapsed;
        public bool IsExpanded { get; set; } = true;
        public ObservableCollection<CategoryNode> Children { get; } = [];
    }

    public class GridAppGroup : System.Collections.Generic.List<CatalogAppItem>
    {
        public CatalogHeaderItem Header { get; set; }
        public GridAppGroup(CatalogHeaderItem header, System.Collections.Generic.IEnumerable<CatalogAppItem> items) : base(items)
        {
            Header = header;
        }
    }
}
