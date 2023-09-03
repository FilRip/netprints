using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using NetPrintsEditor.ViewModels;

namespace NetPrintsEditor.Controls
{
    /// <summary>
    /// Interaction logic for SearchableComboBox.xaml
    /// </summary>
    public partial class SearchableComboBox : UserControl
    {
        private ListCollectionView ListView
        {
            get => searchList.ItemsSource as ListCollectionView;
            set => searchList.ItemsSource = value;
        }

        public SuggestionListVM ViewModel
        {
            get => DataContext as SuggestionListVM;
            set => DataContext = value;
        }

        public SearchableComboBox()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SuggestionListVM oldVM)
            {
                oldVM.ItemsChanged -= OnItemsChanged;
            }

            if (e.NewValue is SuggestionListVM newVM)
            {
                UpdateItems();
                newVM.ItemsChanged += OnItemsChanged;
            }
        }

        private void OnItemsChanged(object sender, EventArgs e)
        {
            UpdateItems();
        }

        private void UpdateItems()
        {
            List<SearchableComboBoxItem> items = ViewModel?.Items?.ToList();

            if (items != null)
            {
                ListView = new ListCollectionView(items.ToList());
                ListView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                if (ListView.CanFilter)
                {
                    ListView.Filter = ViewModel.ItemFilter;
                }

                searchList.ItemsSource = ListView;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchText = searchText.Text;
            ListView?.Refresh();
        }

        private void OnListItemSelected(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SearchableComboBoxItem data)
            {
                ViewModel?.OnItemSelected(data.Value);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Unselect
            searchList.SelectedItem = null;

            // Scroll to top left
            // https://stackoverflow.com/a/7182603/4332314
            if (VisualTreeHelper.GetChild(searchList, 0) is Decorator border)
            {
                ScrollViewer scrollViewer = border.Child as ScrollViewer;
                scrollViewer.ScrollToTop();
                scrollViewer.ScrollToLeftEnd();
            }

            // Clear search box and focus it
            searchText.Clear();
            searchText.Focus();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SearchableCustomAutomation(searchList);
        }
    }

    internal class SearchableCustomAutomation : ItemsControlAutomationPeer
    {
        internal SearchableCustomAutomation(ItemsControl owner) : base(owner)
        {
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new SearchableComboBoxItemAutomationPeer(item, this);
        }

        /*protected override string GetNameCore()
        {
            return "";
        }*/

        protected override List<AutomationPeer> GetChildrenCore()
        {
            return new List<AutomationPeer>();
        }
    }

    internal class SearchableComboBoxItemAutomationPeer : ItemAutomationPeer
    {
        internal SearchableComboBoxItemAutomationPeer(object item, ItemsControlAutomationPeer owner) : base(item, owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        /*protected override string GetNameCore()
        {
            return (Item == null ? Item.ToString() : "");
        }*/

        protected override string GetClassNameCore()
        {
            return "MyItemAutomationPeer";
        }
    }
}
