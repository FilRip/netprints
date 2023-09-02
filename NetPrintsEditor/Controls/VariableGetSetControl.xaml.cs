﻿using System.Windows;
using System.Windows.Controls;

using NetPrints.Core;

namespace NetPrintsEditor.Controls
{
    public delegate void VariableGetSetDelegate(VariableGetSetControl sender,
        VariableSpecifier variableInfo, bool wasSet);

    /// <summary>
    /// Interaction logic for VariableGetSetControl.xaml
    /// </summary>
    public partial class VariableGetSetControl : UserControl
    {
#pragma warning disable CA2211
        public static DependencyProperty VariableSpecifierProperty = DependencyProperty.Register(
            nameof(VariableSpecifier), typeof(VariableSpecifier), typeof(VariableGetSetControl));

        public static DependencyProperty CanGetProperty = DependencyProperty.Register(
            nameof(CanGet), typeof(bool), typeof(VariableGetSetControl));

        public static DependencyProperty CanSetProperty = DependencyProperty.Register(
            nameof(CanSet), typeof(bool), typeof(VariableGetSetControl));
#pragma warning restore CA2211

        public event VariableGetSetDelegate OnVariableGetSet;

        public VariableSpecifier VariableSpecifier
        {
            get => (VariableSpecifier)GetValue(VariableSpecifierProperty);
            set => SetValue(VariableSpecifierProperty, value);
        }

        public bool CanGet
        {
            get => (bool)GetValue(CanGetProperty);
            set => SetValue(CanGetProperty, value);
        }

        public bool CanSet
        {
            get => (bool)GetValue(CanSetProperty);
            set => SetValue(CanSetProperty, value);
        }

        public VariableGetSetControl()
        {
            InitializeComponent();
        }

        private void OnVariableSetClicked(object sender, RoutedEventArgs e)
        {
            OnVariableGetSet?.Invoke(this, VariableSpecifier, true);
        }

        private void OnVariableGetClicked(object sender, RoutedEventArgs e)
        {
            OnVariableGetSet?.Invoke(this, VariableSpecifier, false);
        }
    }
}
