// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias core;


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using core::Roslyn.Utilities;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using VSLangProj;
using Project = EnvDTE.Project;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    internal sealed class ResetInteractive
    {
        private readonly DTE _dte;
        private readonly IComponentModel _componentModel;
        private readonly IVsMonitorSelection _monitorSelection;
        private readonly IVsSolutionBuildManager _buildManager;
        private readonly Func<string, string> _createReference;
        private readonly Func<string, string> _createImport;

        internal ResetInteractive(DTE dte, IComponentModel componentModel, IVsMonitorSelection monitorSelection, IVsSolutionBuildManager buildManager, Func<string, string> createReference, Func<string, string> createImport)
        {
            _dte = dte;
            _componentModel = componentModel;
            _monitorSelection = monitorSelection;
            _buildManager = buildManager;
            _createReference = createReference;
            _createImport = createImport;
        }

        internal void Execute(IVsInteractiveWindow vsInteractiveWindow, string title)
        {
            var hierarchyPointer = default(IntPtr);
            var selectionContainerPointer = default(IntPtr);

            try
            {
                uint itemid;
                IVsMultiItemSelect multiItemSelectPointer;
                Marshal.ThrowExceptionForHR(_monitorSelection.GetCurrentSelection(
                    out hierarchyPointer, out itemid, out multiItemSelectPointer, out selectionContainerPointer));

                if (hierarchyPointer != IntPtr.Zero)
                {
                    List<string> references, referenceSearchPaths, sourceSearchPaths, namespacesToImport;
                    string projectDirectory;
                    GetProjectProperties(hierarchyPointer, out references, out referenceSearchPaths, out sourceSearchPaths, out namespacesToImport, out projectDirectory);

                    // Now, we're going to do a bunch of async operations.  So create a wait
                    // indicator so the user knows something is happening, and also so they cancel.
                    var waitIndicator = _componentModel.GetService<IWaitIndicator>();
                    var waitContext = waitIndicator.StartWait(title, ServicesVSResources.BuildingProject, allowCancel: true);

                    var resetInteractiveTask = ResetInteractiveAsync(
                        vsInteractiveWindow, 
                        references.ToImmutableArray(),
                        referenceSearchPaths.ToImmutableArray(), 
                        sourceSearchPaths.ToImmutableArray(), 
                        namespacesToImport.ToImmutableArray(), 
                        projectDirectory, 
                        waitContext);

                    // Once we're done resetting, dismiss the wait indicator and focus the REPL window.
                    resetInteractiveTask.SafeContinueWith(
                        _ =>
                        {
                            waitContext.Dispose();

                            // We have to set focus to the Interactive Window *after* the wait indicator is dismissed.
                            vsInteractiveWindow.Show(focus: true);
                        },
                        TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
            finally
            {
                SafeRelease(hierarchyPointer);
                SafeRelease(selectionContainerPointer);
            }
        }

        private async Task ResetInteractiveAsync(
            IVsInteractiveWindow vsInteractiveWindow,
            ImmutableArray<string> referencePaths,
            ImmutableArray<string> referenceSearchPaths,
            ImmutableArray<string> sourceSearchPaths,
            ImmutableArray<string> namespacesToImport,
            string projectDirectory,
            IWaitContext waitContext)
        {
            // First, open the repl window.
            var engine = (InteractiveEvaluator)vsInteractiveWindow.InteractiveWindow.Evaluator;

            // If the user hits the cancel button on the wait indicator, then we want to stop the
            // build.
            waitContext.CancellationToken.Register(() =>
                _dte.ExecuteCommand("Build.Cancel"), useSynchronizationContext: true);

            // First, start a build
            await BuildProject().ConfigureAwait(true);

            // Then reset the REPL
            waitContext.Message = ServicesVSResources.ResettingInteractive;
            await vsInteractiveWindow.InteractiveWindow.Operations.ResetAsync(initialize: false).ConfigureAwait(true);

            // Now send the reference paths we've collected to the repl.
            await engine.SetPathsAsync(referenceSearchPaths, sourceSearchPaths, projectDirectory).ConfigureAwait(true);

            await vsInteractiveWindow.InteractiveWindow.SubmitAsync(new[]
            {
                referencePaths.Select(_createReference).Join("\r\n"),
                namespacesToImport.Select(_createImport).Join("\r\n")
            }).ConfigureAwait(true);
        }

        private static void GetProjectProperties(
            IntPtr hierarchyPointer,
            out List<string> references,
            out List<string> referenceSearchPaths,
            out List<string> sourceSearchPaths,
            out List<string> namespacesToImport,
            out string projectDirectory)
        {
            var hierarchy = (IVsHierarchy)Marshal.GetObjectForIUnknown(hierarchyPointer);
            object extensibilityObject;
            Marshal.ThrowExceptionForHR(
                hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out extensibilityObject));

            // TODO: Revert this back to using dynamic for web projects, since they have copies of these interfaces.
            var project = (Project)extensibilityObject;
            var vsProject = (VSProject)project.Object;

            references = new List<string>();
            referenceSearchPaths = new List<string>();
            sourceSearchPaths = new List<string>();
            namespacesToImport = new List<string>();

            var projectDir = (string)project.Properties.Item("FullPath").Value;
            var outputFileName = (string)project.Properties.Item("OutputFileName").Value;
            var defaultNamespace = (string)project.Properties.Item("DefaultNamespace").Value;
            var relativeOutputPath = (string)project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value;

            Debug.Assert(!string.IsNullOrEmpty(projectDir));
            Debug.Assert(!string.IsNullOrEmpty(outputFileName));
            Debug.Assert(!string.IsNullOrEmpty(relativeOutputPath));

            var scriptsDir = Path.Combine(projectDir, "Scripts");
            var outputDir = Path.Combine(projectDir, relativeOutputPath);

            projectDirectory = projectDir;

            referenceSearchPaths.Add(outputDir);
            referenceSearchPaths.Add(RuntimeEnvironment.GetRuntimeDirectory());

            foreach (Reference reference in vsProject.References)
            {
                var str = GetReferenceString(reference);
                if (str != null)
                {
                    references.Add(str);
                }
            }

            references.Add(outputFileName);

            // TODO (tomat): project Scripts dir
            sourceSearchPaths.Add(Directory.Exists(scriptsDir) ? scriptsDir : projectDir);

            if (!string.IsNullOrEmpty(defaultNamespace))
            {
                namespacesToImport.Add(defaultNamespace);
            }
        }

        private static string GetReferenceString(Reference reference)
        {
            if (!reference.StrongName)
            {
                return reference.Path;
            }

            string name = reference.Name;
            if (name == "mscorlib")
            {
                // mscorlib is always loaded
                return null;
            }

            return reference.Path;

#if TODO // TODO: This shouldn't directly depend on GAC, rather we should have some kind of "reference simplifier".
            var possibleGacNames = GlobalAssemblyCache.GetAssemblyIdentities(name).ToArray();
            if (possibleGacNames.Length == 0)
            {
                // no assembly with simple "name" found in GAC, use path to identify the reference:
                return reference.Path;
            }

            string version = reference.Version;
            string culture = reference.Culture;
            string publicKeyToken = reference.PublicKeyToken;

            var fullName = string.Concat(
                name,
                ", Version=",
                version,
                ", Culture=",
                (culture == "") ? "neutral" : culture,
                ", PublicKeyToken=",
                publicKeyToken.ToLowerInvariant());

            AssemblyIdentity identity;
            if (!AssemblyIdentity.TryParseDisplayName(fullName, out identity))
            {
                // ignore invalid names:
                return null;
            }

            var foundEquivalent = false;
            var foundNonEquivalent = false;
            foreach (var possibleGacName in possibleGacNames)
            {
                if (DesktopAssemblyIdentityComparer.Default.ReferenceMatchesDefinition(identity, possibleGacName))
                {
                    foundEquivalent = true;
                }
                else
                {
                    foundNonEquivalent = true;
                }

                if (foundEquivalent && foundNonEquivalent)
                {
                    break;
                }
            }

            if (!foundEquivalent)
            {
                // The reference name isn't equivalent to any GAC name.
                // The assembly is strong named but not GAC'd, so we need to load it from path:
                return reference.Path;
            }

            if (foundNonEquivalent)
            {
                // We found some equivalent assemblies but also some non-equivalent. 
                // So simple name doesn't identify the reference uniquely.
                return fullName;
            }

            // We found a single simple name match that is equivalent to the given reference.
            // We can use the simple name to load the GAC'd assembly.
            return name;
#endif
        }

        private static void SafeRelease(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero)
            {
                Marshal.Release(pointer);
            }
        }

        private Task<bool> BuildProject()
        {
            var taskSource = new TaskCompletionSource<bool>();

            var updateSolutionEvents = new VsUpdateSolutionEvents(_buildManager, taskSource);

            // Build the project.  When project build is done, set the task source as being done.
            // (Either succeeded, cancelled, or failed).
            _dte.ExecuteCommand("Build.BuildSelection");

            return taskSource.Task;
        }
    }
}
