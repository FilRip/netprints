﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using EnvDTE80;

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using NetPrints.Core;
using NetPrints.Translator;

using VSLangProj;

using Task = System.Threading.Tasks.Task;

namespace NetPrints.VSIX
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideBindingPath]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideEditorFactory(typeof(NetPrintsEditorFactory), 106)]
    [ProvideEditorLogicalView(typeof(NetPrintsEditorFactory), "{7651a703-06e5-11d1-8ebd-00a0c90f26ea}")]
    [ProvideEditorExtension(typeof(NetPrintsEditorFactory), ".netpc", 32,
              ProjectGuid = "{A2FE74E1-B743-11d0-AE1A-00A0C90FFFC3}",
              NameResourceID = 106)]
    public sealed class NetPrintsVSIXPackage : AsyncPackage, IVsUpdateSolutionEvents
    {
        /// <summary>
        /// NetPrintsVSIXPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b5e5dd1f-f24f-44dd-b0ed-bbcce219af0c";

        private IVsSolutionBuildManager2 solutionBuildManager;
        private uint solutionBuildManagerCookie;

        private DTE2 dte;

        /*private void AddMetroResources()
        {
            void AddResourceDict(string s)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(s, UriKind.Absolute),
                });
            }

            AddResourceDict(@"pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml");
            AddResourceDict(@"pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml");
            AddResourceDict(@"pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Emerald.xaml");
            AddResourceDict(@"pack://application:,,,/MahApps.Metro;component/Styles/Controls.Buttons.xaml");
        }*/

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            solutionBuildManager = (IVsSolutionBuildManager2)ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager));
            Assumes.Present(solutionBuildManager);
            solutionBuildManager.AdviseUpdateSolutionEvents(this, out solutionBuildManagerCookie);

            dte = await ServiceProvider.GetGlobalServiceAsync(typeof(SDTE)) as DTE2;

            RegisterEditorFactory(new NetPrintsEditorFactory(this));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            ThreadHelper.ThrowIfNotOnUIThread();
            solutionBuildManager?.UnadviseUpdateSolutionEvents(solutionBuildManagerCookie);
        }

        public void ReplaceProjectReferences(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var assemblyReferences = dte.Solution.Projects
                .OfType<EnvDTE.Project>()
                .Select(proj => proj.Object)
                .OfType<VSProject>()
                .SelectMany(proj => proj.References
                    .OfType<Reference>()
                    .Cast<VSLangProj80.Reference3>()
                    .Where(r => r.Path != null && File.Exists(r.Path))
                    .Select(r => new AssemblyReference(r.Path)))
                .ToArray();

            project.References.ReplaceRange(assemblyReferences);
        }

        public IEnumerable<AssemblyReference> GetAssemblyReferences()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return dte.Solution.Projects
                .OfType<EnvDTE.Project>()
                .Select(proj => proj.Object)
                .OfType<VSProject>()
                .SelectMany(proj => proj.References
                    .OfType<Reference>()
                    .Cast<VSLangProj80.Reference3>()
                    .Where(r => r.Path != null && File.Exists(r.Path))
                    .Select(r => new AssemblyReference(r.Path)));
        }

        public IEnumerable<SourceDirectoryReference> GetSourceDirectoryReferences()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return dte.Solution.Projects
                .OfType<EnvDTE.Project>()
                .Where(proj => File.Exists(proj.FileName))
                .Select(proj => new SourceDirectoryReference(Path.GetDirectoryName(proj.FileName)));
        }

        public IEnumerable<string> GetGeneratedCode()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ClassTranslator classTranslator = new();

            foreach (var project in dte.Solution.Projects.OfType<EnvDTE.Project>())
            {
                foreach (var projectItem in project.ProjectItems.OfType<EnvDTE.ProjectItem>())
                {
                    if (projectItem.Properties?.Item("FullPath")?.Value is string fullPath && fullPath.EndsWith(".netpc"))
                    {
                        ClassGraph classGraph = Serialization.SerializationHelper.LoadClass(fullPath);
                        yield return classTranslator.TranslateClass(classGraph);
                    }
                }
            }
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in dte.Solution.Projects.OfType<EnvDTE.Project>())
            {
                foreach (var projectItem in project.ProjectItems.OfType<EnvDTE.ProjectItem>())
                {
                    if (projectItem.Properties.Item("FullPath")?.Value is string fullPath && fullPath.EndsWith(".netpc"))
                    {
                        string outputPath = Path.Combine(Path.ChangeExtension(fullPath, ".cs"));

                        CompilationUtil.CompileNetPrintsClass(fullPath, outputPath);
                    }
                }
            }

            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => VSConstants.S_OK;

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

        public int UpdateSolution_Cancel() => VSConstants.S_OK;

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;
    }
}
