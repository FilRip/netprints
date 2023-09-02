using Microsoft.Toolkit.Mvvm.ComponentModel;

using NetPrints.Core;

namespace NetPrintsEditor.ViewModels
{
    public class ReferenceListViewModel : ObservableObject
    {
        public Project Project
        {
            get; set;
        }

        public ReferenceListViewModel(Project project)
        {
            Project = project;
        }
    }
}
