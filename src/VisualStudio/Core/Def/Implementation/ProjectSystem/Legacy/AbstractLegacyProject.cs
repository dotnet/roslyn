﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    /// <summary>
    /// Base type for legacy C# and VB project system shim implementations.
    /// These legacy shims are based on legacy project system interfaces defined in csproj/msvbprj.
    /// </summary>
    internal abstract partial class AbstractLegacyProject : ForegroundThreadAffinitizedObject
    {
        public IVsHierarchy Hierarchy { get; }
        protected VisualStudioProject VisualStudioProject { get; }
        internal VisualStudioProjectOptionsProcessor VisualStudioProjectOptionsProcessor { get; set; }
        protected IProjectCodeModel ProjectCodeModel { get; set; }
        protected VisualStudioWorkspace Workspace { get; }

        #region Mutable fields that should only be used from the UI thread

        private readonly VsENCRebuildableProjectImpl _editAndContinueProject;

        private readonly SolutionEventsBatchScopeCreator _batchScopeCreator;

        #endregion

        public AbstractLegacyProject(
            string projectSystemName,
            IVsHierarchy hierarchy,
            string language,
            IServiceProvider serviceProvider,
            IThreadingContext threadingContext,
            string externalErrorReportingPrefix,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt)
            : base(threadingContext)
        {
            Contract.ThrowIfNull(hierarchy);

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            Workspace = componentModel.GetService<VisualStudioWorkspace>();

            var projectFilePath = hierarchy.GetProjectFilePath();

            if (projectFilePath != null && !File.Exists(projectFilePath))
            {
                projectFilePath = null;
            }
     
            var projectFactory = componentModel.GetService<VisualStudioProjectFactory>();
            VisualStudioProject = projectFactory.CreateAndAddToWorkspace(
                projectSystemName,
                language,
                new VisualStudioProjectCreationInfo
                {
                    // The workspace requires an assembly name so we can make compilations. We'll use
                    // projectSystemName because they'll have a better one eventually.
                    AssemblyName = projectSystemName,
                    FilePath = projectFilePath,
                    Hierarchy = hierarchy,
                    ProjectGuid = GetProjectIDGuid(hierarchy),
                    DefaultNamespace = GetDefaultNamespace(hierarchy, language)
                });

            Hierarchy = hierarchy;
            ConnectHierarchyEvents();
            RefreshBinOutputPath();

            // TODO: remove this terrible hack, which is working around shims throwing in not-good ways
            try
            {
                _externalErrorReporter = new ProjectExternalErrorReporter(VisualStudioProject.Id, externalErrorReportingPrefix, serviceProvider);
                _editAndContinueProject = new VsENCRebuildableProjectImpl(Workspace, VisualStudioProject, serviceProvider);
            }
            catch (Exception)
            {
            }

            _batchScopeCreator = componentModel.GetService<SolutionEventsBatchScopeCreator>();
            _batchScopeCreator.StartTrackingProject(VisualStudioProject, Hierarchy);
        }

        public string AssemblyName => VisualStudioProject.AssemblyName;

        public virtual void Disconnect()
        {
            _batchScopeCreator.StopTrackingProject(VisualStudioProject);

            VisualStudioProjectOptionsProcessor?.Dispose();
            ProjectCodeModel.OnProjectClosed();
            VisualStudioProject.RemoveFromWorkspace();

            // Unsubscribe IVsHierarchyEvents
            DisconnectHierarchyEvents();
        }

        protected void AddFile(
            string filename,
            SourceCodeKind sourceCodeKind)
        {
            AssertIsForeground();

            // We have tests that assert that XOML files should not get added; this was similar
            // behavior to how ASP.NET projects would add .aspx files even though we ultimately ignored
            // them. XOML support is planned to go away for Dev16, but for now leave the logic there.
            if (filename.EndsWith(".xoml"))
            {
                return;
            }

            ImmutableArray<string> folders = default;

            var itemid = Hierarchy.TryGetItemId(filename);
            if (itemid != VSConstants.VSITEMID_NIL)
            {
                folders = GetFolderNamesForDocument(itemid);
            }

            VisualStudioProject.AddSourceFile(filename, sourceCodeKind, folders);
        }

        protected void RemoveFile(string filename)
        {
            AssertIsForeground();

            // We have tests that assert that XOML files should not get added; this was similar
            // behavior to how ASP.NET projects would add .aspx files even though we ultimately ignored
            // them. XOML support is planned to go away for Dev16, but for now leave the logic there.
            if (filename.EndsWith(".xoml"))
            {
                return;
            }

            VisualStudioProject.RemoveSourceFile(filename);
        }

        private void RefreshBinOutputPath()
        {
            var storage = Hierarchy as IVsBuildPropertyStorage;
            if (storage == null)
            {
                return;
            }

            if (ErrorHandler.Failed(storage.GetPropertyValue("OutDir", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out var outputDirectory)) ||
                ErrorHandler.Failed(storage.GetPropertyValue("TargetFileName", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out var targetFileName)))
            {
                return;
            }

            if (targetFileName == null)
            {
                return;
            }

            // web app case
            if (!PathUtilities.IsAbsolute(outputDirectory))
            {
                if (VisualStudioProject.FilePath == null)
                {
                    return;
                }

                outputDirectory = FileUtilities.ResolveRelativePath(outputDirectory, Path.GetDirectoryName(VisualStudioProject.FilePath));
            }

            if (outputDirectory == null)
            {
                return;
            }

            VisualStudioProject.OutputFilePath = FileUtilities.NormalizeAbsolutePath(Path.Combine(outputDirectory, targetFileName));
        }

        private static Guid GetProjectIDGuid(IVsHierarchy hierarchy)
        {
            if (hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid, out var guid))
            {
                return guid;
            }

            return Guid.Empty;
        }

        private static bool GetIsWebsiteProject(IVsHierarchy hierarchy)
        {
            try
            {
                if (hierarchy.TryGetProject(out var project))
                {
                    return project.Kind == VsWebSite.PrjKind.prjKindVenusProject;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// Map of folder item IDs in the workspace to the string version of their path.
        /// </summary>
        /// <remarks>Using item IDs as a key like this in a long-lived way is considered unsupported by CPS and other
        /// IVsHierarchy providers, but this code (which is fairly old) still makes the assumptions anyways.</remarks>
        private readonly Dictionary<uint, ImmutableArray<string>> _folderNameMap = new Dictionary<uint, ImmutableArray<string>>();

        private ImmutableArray<string> GetFolderNamesForDocument(uint documentItemID)
        {
            AssertIsForeground();

            if (documentItemID != (uint)VSConstants.VSITEMID.Nil && Hierarchy.GetProperty(documentItemID, (int)VsHierarchyPropID.Parent, out var parentObj) == VSConstants.S_OK)
            {
                var parentID = UnboxVSItemId(parentObj);
                if (parentID != (uint)VSConstants.VSITEMID.Nil && parentID != (uint)VSConstants.VSITEMID.Root)
                {
                    return GetFolderNamesForFolder(parentID);
                }
            }

            return ImmutableArray<string>.Empty;
        }

        private ImmutableArray<string> GetFolderNamesForFolder(uint folderItemID)
        {
            AssertIsForeground();

            using (var pooledObject = SharedPools.Default<List<string>>().GetPooledObject())
            {
                var newFolderNames = pooledObject.Object;
                ImmutableArray<string> folderNames;

                if (!_folderNameMap.TryGetValue(folderItemID, out folderNames))
                {
                    ComputeFolderNames(folderItemID, newFolderNames, Hierarchy);
                    folderNames = newFolderNames.ToImmutableArray();
                    _folderNameMap.Add(folderItemID, folderNames);
                }
                else
                {
                    // verify names, and change map if we get a different set.
                    // this is necessary because we only get document adds/removes from the project system
                    // when a document name or folder name changes.
                    ComputeFolderNames(folderItemID, newFolderNames, Hierarchy);
                    if (!Enumerable.SequenceEqual(folderNames, newFolderNames))
                    {
                        folderNames = newFolderNames.ToImmutableArray();
                        _folderNameMap[folderItemID] = folderNames;
                    }
                }

                return folderNames;
            }
        }

        // Different hierarchies are inconsistent on whether they return ints or uints for VSItemIds.
        // Technically it should be a uint.  However, there's no enforcement of this, and marshalling
        // from native to managed can end up resulting in boxed ints instead.  Handle both here so 
        // we're resilient to however the IVsHierarchy was actually implemented.
        private static uint UnboxVSItemId(object id)
        {
            return id is uint ? (uint)id : unchecked((uint)(int)id);
        }

        private static void ComputeFolderNames(uint folderItemID, List<string> names, IVsHierarchy hierarchy)
        {
            if (hierarchy.GetProperty((uint)folderItemID, (int)VsHierarchyPropID.Name, out var nameObj) == VSConstants.S_OK)
            {
                // For 'Shared' projects, IVSHierarchy returns a hierarchy item with < character in its name (i.e. <SharedProjectName>)
                // as a child of the root item. There is no such item in the 'visual' hierarchy in solution explorer and no such folder
                // is present on disk either. Since this is not a real 'folder', we exclude it from the contents of Document.Folders.
                // Note: The parent of the hierarchy item that contains < character in its name is VSITEMID.Root. So we don't need to
                // worry about accidental propagation out of the Shared project to any containing 'Solution' folders - the check for
                // VSITEMID.Root below already takes care of that.
                var name = (string)nameObj;
                if (!name.StartsWith("<", StringComparison.OrdinalIgnoreCase))
                {
                    names.Insert(0, name);
                }
            }

            if (hierarchy.GetProperty((uint)folderItemID, (int)VsHierarchyPropID.Parent, out var parentObj) == VSConstants.S_OK)
            {
                var parentID = UnboxVSItemId(parentObj);
                if (parentID != (uint)VSConstants.VSITEMID.Nil && parentID != (uint)VSConstants.VSITEMID.Root)
                {
                    ComputeFolderNames(parentID, names, hierarchy);
                }
            }
        }

        /// <summary>
        /// Get the default namespace of the project ("" if not defined, which means global namespace),
        /// or null if it is unknown or not applicable. 
        /// </summary>
        /// <remarks>
        /// This only has meaning in C# and is explicitly set to null in VB.
        /// </remarks>>
        private static string GetDefaultNamespace(IVsHierarchy hierarchy, string language)
        {
            // While both csproj and vbproj might define <rootnamespace> property in the project file, 
            // they are very different things.
            // 
            // In C#, it's called default namespace (even though we got the value from rootnamespace property),
            // and it doesn't affect the semantic of the code in anyway, just something used by VS.
            // For example, when you create a new class, the namespace for the new class is based on it. 
            // Therefore, we can't get this info from compiler.
            // 
            // However, in VB, it's actually called root namespace, and that info is part of the VB compilation 
            // (parsed from arguments), because VB compiler needs it to determine the root of all the namespace 
            // declared in the compilation.
            // 
            // Unfortunately, although being different concepts, default namespace and root namespace are almost
            // used interchangebly in VS. For example, (1) the value is define in "rootnamespace" property in project 
            // files and, (2) the property name we use to call into DTE project below to retrieve the value is 
            // called "DefaultNamespace".

            if (hierarchy != null && language == LanguageNames.CSharp)
            {
                if (hierarchy.TryGetProject(out var dteProject))
                {
                    try
                    {
                        return (string)dteProject.Properties.Item("DefaultNamespace").Value;
                    }
                    catch (ArgumentException)
                    {
                        // DefaultNamespace does not exist for this project.
                        return string.Empty;
                    }
                }
            }

            return null;
        }
    }
}
