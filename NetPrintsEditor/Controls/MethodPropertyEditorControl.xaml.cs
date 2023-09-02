using System.Windows.Controls;

using NetPrintsEditor.ViewModels;

namespace NetPrintsEditor.Controls
{
    /// <summary>
    /// Interaction logic for MethodPropertyEditorControl.xaml
    /// </summary>
    public partial class MethodPropertyEditorControl : UserControl
    {
        public NodeGraphVM Graph
        {
            get => DataContext as NodeGraphVM;
        }

        public MethodPropertyEditorControl()
        {
            InitializeComponent();
        }
    }
}
