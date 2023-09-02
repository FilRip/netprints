using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using MahApps.Metro.Controls.Dialogs;

using Microsoft.Win32;

using NetPrints.Core;

using NetPrintsEditor.ViewModels;

namespace NetPrintsEditor
{
    /// <summary>
    /// Interaction logic for ReferenceListWindow.xaml
    /// </summary>
    public partial class ReferenceListWindow : CustomDialog
    {
        public ReferenceListViewModel ViewModel
        {
            get => DataContext as ReferenceListViewModel;
            set => DataContext = value;
        }

        public ReferenceListWindow()
        {
            InitializeComponent();
        }

        private void OnAddAssemblyReferenceClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    AssemblyReference assemblyReference = new(openFileDialog.FileName);

                    if (!ViewModel.Project.References.OfType<AssemblyReference>().Any(r =>
                        string.Equals(Path.GetFullPath(r.AssemblyPath), Path.GetFullPath(assemblyReference.AssemblyPath), StringComparison.OrdinalIgnoreCase)))
                    {
                        ViewModel.Project.References.Add(assemblyReference);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add assembly at {openFileDialog.FileName}:\n\n{ex}");
                }
            }
        }

        private void OnAddSourceDirectoryReferenceClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFolderDialog = new();
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    SourceDirectoryReference sourceDirectoryReference = new(openFolderDialog.SelectedPath);

                    if (!ViewModel.Project.References.OfType<SourceDirectoryReference>().Any(r =>
                        string.Equals(Path.GetFullPath(r.SourceDirectory), Path.GetFullPath(sourceDirectoryReference.SourceDirectory), StringComparison.OrdinalIgnoreCase)))
                    {
                        ViewModel.Project.References.Add(sourceDirectoryReference);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add sources at {openFolderDialog.SelectedPath}:\n\n{ex}");
                }
            }
        }

        private void OnRemoveReferenceClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CompilationReferenceVM reference)
            {
                ViewModel.Project.References.Remove(reference.Reference);
            }
        }
    }
}
