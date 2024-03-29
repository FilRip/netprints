﻿using System;
using System.Globalization;
using System.Windows.Data;

using NetPrints.Core;
using NetPrints.Graph;

using NetPrintsEditor.ViewModels;

namespace NetPrintsEditor.Converters
{
    public class ModelToViewModelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeGraph graph)
            {
                return new NodeGraphVM(graph);
            }
            else if (value is Node node)
            {
                return new NodeVM(node);
            }
            else if (value is ClassGraph cls)
            {
                return new ClassEditorVM(cls);
            }
            else if (value is NodePin pin)
            {
                return new NodePinVM(pin);
            }
            else if (value is CompilationReference reference)
            {
                return new CompilationReferenceVM(reference);
            }

            throw new ArgumentException("Error when trying to convert Model to ViewModel");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
