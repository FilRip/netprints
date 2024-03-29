﻿using System.Collections.Generic;
using System.Windows;

using NetPrints.Core;

using NetPrintsEditor.Reflection;

namespace NetPrintsEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] StartupArguments
        {
            get;
            private set;
        }

        public static IReflectionProvider ReflectionProvider
        {
            get;
            private set;
        }

        public static ObservableRangeCollection<TypeSpecifier> NonStaticTypes
        {
            get;
        } = [];

        public static void ReloadReflectionProvider(IEnumerable<string> assemblyPaths, IEnumerable<string> sourcePaths, IEnumerable<string> sources)
        {
            ReflectionProvider = new MemoizedReflectionProvider(new ReflectionProvider(assemblyPaths, sourcePaths, sources));

            // Cache static types.
            // Needs to be done on UI thread since it is an observable collection to
            // which we bind.
            Current.Dispatcher.Invoke(() => NonStaticTypes.ReplaceRange(ReflectionProvider.GetNonStaticTypes()));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            StartupArguments = e.Args;
            base.OnStartup(e);
        }
    }
}
