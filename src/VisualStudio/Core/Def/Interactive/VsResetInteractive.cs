// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    internal sealed class VsResetInteractive : ResetInteractive
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly EnvDTE.DTE _dte;
        private readonly IComponentModel _componentModel;
        private readonly IVsMonitorSelection _monitorSelection;
        private readonly IVsSolutionBuildManager _buildManager;

        internal VsResetInteractive(
            VisualStudioWorkspace workspace,
            EnvDTE.DTE dte,
            IComponentModel componentModel,
            IVsMonitorSelection monitorSelection,
            IVsSolutionBuildManager buildManager,
            Func<string, string> createReference,
            Func<string, string> createImport)
            : base(componentModel.GetService<IEditorOptionsFactoryService>(), createReference, createImport)
        {
            _workspace = workspace;
            _dte = dte;
            _componentModel = componentModel;
            _monitorSelection = monitorSelection;
            _buildManager = buildManager;
        }

        /// <summary>
        /// Gets the properties of the currently selected projects necessary for reset.
        /// </summary>
        protected override bool GetProjectProperties(
            out ImmutableArray<string> references,
            out ImmutableArray<string> referenceSearchPaths,
            out ImmutableArray<string> sourceSearchPaths,
            out ImmutableArray<string> projectNamespaces,
            out string projectDirectory,
            out bool? is64bit)
        {
            var hierarchyPointer = default(IntPtr);
            var selectionContainerPointer = default(IntPtr);
            references = ImmutableArray<string>.Empty;
            referenceSearchPaths = ImmutableArray<string>.Empty;
            sourceSearchPaths = ImmutableArray<string>.Empty;
            projectNamespaces = ImmutableArray<string>.Empty;
            projectDirectory = null;
            is64bit = null;

            try
            {
                Marshal.ThrowExceptionForHR(_monitorSelection.GetCurrentSelection(
                    out hierarchyPointer, out var itemid, out var multiItemSelectPointer, out selectionContainerPointer));

                if (hierarchyPointer != IntPtr.Zero)
                {
                    GetProjectProperties(hierarchyPointer, out references, out referenceSearchPaths, out sourceSearchPaths, out projectNamespaces, out projectDirectory, out is64bit);
                    return true;
                }
            }
            finally
            {
                SafeRelease(hierarchyPointer);
                SafeRelease(selectionContainerPointer);
            }

            return false;
        }

        private void GetProjectProperties(
            IntPtr hierarchyPointer,
            out ImmutableArray<string> references,
            out ImmutableArray<string> referenceSearchPaths,
            out ImmutableArray<string> sourceSearchPaths,
            out ImmutableArray<string> projectNamespaces,
            out string projectDirectory,
            out bool? is64bit)
        {
            var hierarchy = (IVsHierarchy)Marshal.GetObjectForIUnknown(hierarchyPointer);
            Marshal.ThrowExceptionForHR(
                hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out var extensibilityObject));

            var dteProject = (EnvDTE.Project)extensibilityObject;
            var vsProject = (VSLangProj.VSProject)dteProject.Object;
            var projectOpt = GetProjectFromHierarchy(hierarchy);

            var referencesBuilder = ImmutableArray.CreateBuilder<string>();
            var referenceSearchPathsBuilder = ImmutableArray.CreateBuilder<string>();
            var sourceSearchPathsBuilder = ImmutableArray.CreateBuilder<string>();
            var namespacesToImportBuilder = ImmutableArray.CreateBuilder<string>();

            var projectDir = (string)dteProject.Properties.Item("FullPath").Value;
            var outputFileName = (string)dteProject.Properties.Item("OutputFileName").Value;
            var defaultNamespace = (string)dteProject.Properties.Item("DefaultNamespace").Value;
            var relativeOutputPath = (string)dteProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value;

            Debug.Assert(!string.IsNullOrEmpty(projectDir));
            Debug.Assert(!string.IsNullOrEmpty(outputFileName));
            Debug.Assert(!string.IsNullOrEmpty(relativeOutputPath));

            var scriptsDir = Path.Combine(projectDir, "Scripts");
            var outputDir = Path.Combine(projectDir, relativeOutputPath);

            projectDirectory = projectDir;

            referenceSearchPathsBuilder.Add(outputDir);
            referenceSearchPathsBuilder.Add(RuntimeEnvironment.GetRuntimeDirectory());

            foreach (VSLangProj.Reference reference in vsProject.References)
            {
                var str = GetReferenceString(reference);
                if (str != null)
                {
                    referencesBuilder.Add(str);
                }
            }

            referencesBuilder.Add(outputFileName);

            // TODO (tomat): project Scripts dir
            sourceSearchPathsBuilder.Add(Directory.Exists(scriptsDir) ? scriptsDir : projectDir);

            if (!string.IsNullOrEmpty(defaultNamespace))
            {
                namespacesToImportBuilder.Add(defaultNamespace);
            }

            references = referencesBuilder.ToImmutableArray();
            referenceSearchPaths = referenceSearchPathsBuilder.ToImmutableArray();
            sourceSearchPaths = sourceSearchPathsBuilder.ToImmutableArray();
            projectNamespaces = namespacesToImportBuilder.ToImmutableArray();

            is64bit = (projectOpt != null) ? Is64Bit(projectOpt.CompilationOptions.Platform) : null;
        }

        internal Project GetProjectFromHierarchy(IVsHierarchy hierarchy)
            => _workspace.CurrentSolution.Projects.FirstOrDefault(proj => ProjectIdMatchesHierarchy(_workspace, proj.Id, hierarchy));

        private static bool ProjectIdMatchesHierarchy(VisualStudioWorkspace workspace, ProjectId projectId, IVsHierarchy hierarchy)
        {
            var hierarchyForProject = workspace.GetHierarchy(projectId);

            if (hierarchyForProject == null)
            {
                return false;
            }

            if (hierarchyForProject == hierarchy)
            {
                return true;
            }

            // For CPS, the hierarchy for the Roslyn project isn't the same as the one
            // we get from Solution Explorer (it's a wrapper implementation), so we'll
            // have to compare properties.

            hierarchyForProject.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out var rawValue);

            var projectName = rawValue as string;
            if (projectName != null)
            {
                hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out rawValue);
                return projectName == (rawValue as string);
            }

            return false;
        }

        private static bool? Is64Bit(Platform platform)
        {
            switch (platform)
            {
                case Platform.Arm:
                case Platform.AnyCpu32BitPreferred:
                case Platform.X86:
                    return false;

                case Platform.Itanium:
                case Platform.X64:
                case Platform.Arm64:
                    return true;

                default:
                    return null;
            }
        }

        private static string GetReferenceString(VSLangProj.Reference reference)
        {
            if (!reference.StrongName)
            {
                return reference.Path;
            }

            var name = reference.Name;
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

        protected override Task<bool> BuildProject()
        {
            var taskSource = new TaskCompletionSource<bool>();

            var updateSolutionEvents = new VsUpdateSolutionEvents(_buildManager, taskSource);

            // Build the project.  When project build is done, set the task source as being done.
            // (Either succeeded, cancelled, or failed).
            _dte.ExecuteCommand("Build.BuildSelection");

            return taskSource.Task;
        }

        protected override void CancelBuildProject()
        {
            _dte.ExecuteCommand("Build.Cancel");
        }

        protected override IWaitIndicator GetWaitIndicator()
        {
            return _componentModel.GetService<IWaitIndicator>();
        }

        /// <summary>
        /// Return namespaces that can be resolved in the latest interactive compilation.
        /// </summary>
        protected override async Task<IEnumerable<string>> GetNamespacesToImportAsync(IEnumerable<string> namespacesToImport, IInteractiveWindow interactiveWindow)
        {
            var document = interactiveWindow.CurrentLanguageBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(true);
            return namespacesToImport.Where(ns => compilation.GlobalNamespace.GetQualifiedNamespace(ns) != null);
        }
    }
}
