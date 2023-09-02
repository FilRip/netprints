using System.Linq;

using Microsoft.Toolkit.Mvvm.ComponentModel;

using NetPrints.Core;

namespace NetPrintsEditor.ViewModels
{
    public class MainEditorVM : ObservableObjectSerializable
    {
        private Project m_project;

        public bool IsProjectOpen => Project != null;

        public bool CanCompile => Project?.CanCompile ?? false;

        public bool CanCompileAndRun => Project?.CanCompileAndRun ?? false;

        public Project Project
        {
            get { return m_project; }
            set
            {
                if (m_project != value)
                {
                    m_project = value;
                    OnProjectChanged();
                }
            }
        }

        public MainEditorVM(Project project)
        {
            Project = project;
        }

        public void OnProjectChanged()
        {
            if (Project != null)
            {
                Project.References.CollectionChanged += (sender, e) => ReloadReflectionProvider();

                // Reload reflection provider when IsCompiling changed to false
                Project.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Project.IsCompiling) && !Project.IsCompiling)
                    {
                        ReloadReflectionProvider();
                    }
                };
            }

            ReloadReflectionProvider();
        }

        private void ReloadReflectionProvider()
        {
            if (Project != null)
            {
                ObservableRangeCollection<CompilationReference> references = Project.References;

                // Add referenced assemblies
                System.Collections.Generic.List<string> assemblyPaths = references.OfType<AssemblyReference>().Select(assemblyRef => assemblyRef.AssemblyPath).ToList();

                // Add source files
                System.Collections.Generic.List<string> sourcePaths = references.OfType<SourceDirectoryReference>().SelectMany(directoryRef => directoryRef.SourceFilePaths).ToList();

                // Add our own sources
                System.Collections.Generic.List<string> sources = Project.GenerateClassSources().ToList();

                App.ReloadReflectionProvider(assemblyPaths, sourcePaths, sources);
            }
        }
    }
}
