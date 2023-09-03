using Microsoft.Toolkit.Mvvm.ComponentModel;

using NetPrints.Core;

namespace NetPrintsEditor.ViewModels
{
    public class ReferenceListViewModel(Project project) : ObservableObjectSerializable
    {
        public Project Project
        {
            get; set;
        } = project;
    }
}
