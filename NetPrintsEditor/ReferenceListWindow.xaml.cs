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
            OpenFileDialog openFolderDialog = new();
            openFolderDialog.CheckFileExists = false;
            openFolderDialog.FileName = "Select a directory...";

            if (openFolderDialog.ShowDialog() == true)
            {
                try
                {
                    SourceDirectoryReference sourceDirectoryReference = new(Path.GetDirectoryName(openFolderDialog.FileName));

                    if (!ViewModel.Project.References.OfType<SourceDirectoryReference>().Any(r =>
                        string.Equals(Path.GetFullPath(r.SourceDirectory), Path.GetFullPath(sourceDirectoryReference.SourceDirectory), StringComparison.OrdinalIgnoreCase)))
                    {
                        ViewModel.Project.References.Add(sourceDirectoryReference);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add sources at {Path.GetDirectoryName(openFolderDialog.FileName)}:\n\n{ex}");
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
