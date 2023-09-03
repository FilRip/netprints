using System.Windows.Controls;

namespace NetPrintsEditor.Controls
{
    public class SuggestionListItemBinding(string text, string iconPath)
    {
        public string Text { get; } = text;
        public string IconPath { get; } = iconPath;
    }

    /// <summary>
    /// Interaction logic for SuggestionListItem.xaml
    /// </summary>
    public partial class SuggestionListItem : UserControl
    {
        public SuggestionListItem()
        {
            InitializeComponent();
        }
    }
}
