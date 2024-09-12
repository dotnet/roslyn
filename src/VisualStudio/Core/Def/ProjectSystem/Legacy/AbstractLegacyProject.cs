// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

/// <summary>
/// Base type for legacy C# and VB project system shim implementations.
/// These legacy shims are based on legacy project system interfaces defined in csproj/msvbprj.
/// </summary>
internal abstract partial class AbstractLegacyProject : ForegroundThreadAffinitizedObject
{
    public IVsHierarchy Hierarchy { get; }
    protected ProjectSystemProject ProjectSystemProject { get; }
    internal ProjectSystemProjectOptionsProcessor ProjectSystemProjectOptionsProcessor { get; set; }
    protected IProjectCodeModel ProjectCodeModel { get; set; }
    protected VisualStudioWorkspace Workspace { get; }

    internal ProjectSystemProject Test_ProjectSystemProject => ProjectSystemProject;

    /// <summary>
    /// The path to the directory of the project. Read-only, since although you can rename
    /// a project in Visual Studio you can't change the folder of a project without an
    /// unload/reload.
    /// </summary>
    private readonly string _projectDirectory = null;

    /// <summary>
    /// Whether we should ignore the output path for this project because it's a special project.
    /// </summary>
    private readonly bool _ignoreOutputPath;

    private static readonly char[] PathSeparatorCharacters = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    #region Mutable fields that should only be used from the UI thread

    private readonly SolutionEventsBatchScopeCreator _batchScopeCreator;

    #endregion

    public AbstractLegacyProject(
        string projectSystemName,
        IVsHierarchy hierarchy,
        string language,
        bool isVsIntellisenseProject,
        IServiceProvider serviceProvider,
        IThreadingContext threadingContext,
        string externalErrorReportingPrefix)
        : base(threadingContext, assertIsForeground: true)
    {
        Contract.ThrowIfNull(hierarchy);

        var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
        Workspace = componentModel.GetService<VisualStudioWorkspace>();
        var workspaceImpl = (VisualStudioWorkspaceImpl)Workspace;

        var projectFilePath = hierarchy.TryGetProjectFilePath();

        if (projectFilePath != null && !File.Exists(projectFilePath))
        {
            projectFilePath = null;
        }

        if (projectFilePath != null)
        {
            _projectDirectory = Path.GetDirectoryName(projectFilePath);
        }

        if (isVsIntellisenseProject)
        {
            // IVsIntellisenseProjects are usually used for contained language cases, which means these projects don't have any real
            // output path that we should consider. Since those point to the same IVsHierarchy as another project, we end up with two projects
            // with the same output path, which potentially breaks conversion of metadata references to project references. However they're
            // also used for database projects and a few other cases where there there isn't a "primary" IVsHierarchy.
            // As a heuristic here we'll ignore the output path if we already have another project tied to the IVsHierarchy.
            foreach (var projectId in Workspace.CurrentSolution.ProjectIds)
            {
                if (Workspace.GetHierarchy(projectId) == hierarchy)
                {
                    _ignoreOutputPath = true;
                    break;
                }
            }
        }

        var projectFactory = componentModel.GetService<VisualStudioProjectFactory>();
        ProjectSystemProject = threadingContext.JoinableTaskFactory.Run(() => projectFactory.CreateAndAddToWorkspaceAsync(
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
            },
            CancellationToken.None));

        workspaceImpl.AddProjectRuleSetFileToInternalMaps(
            ProjectSystemProject,
            () => ProjectSystemProjectOptionsProcessor.EffectiveRuleSetFilePath);

        // Right now VB doesn't have the concept of "default namespace". But we conjure one in workspace 
        // by assigning the value of the project's root namespace to it. So various feature can choose to 
        // use it for their own purpose.
        // In the future, we might consider officially exposing "default namespace" for VB project 
        // (e.g. through a <defaultnamespace> msbuild property)
        ProjectSystemProject.DefaultNamespace = GetRootNamespacePropertyValue(hierarchy);

        if (TryGetPropertyValue(hierarchy, BuildPropertyNames.MaxSupportedLangVersion, out var maxLangVer))
        {
            ProjectSystemProject.MaxLangVersion = maxLangVer;
        }

        if (TryGetBoolPropertyValue(hierarchy, BuildPropertyNames.RunAnalyzers, out var runAnayzers))
        {
            ProjectSystemProject.RunAnalyzers = runAnayzers;
        }

        if (TryGetBoolPropertyValue(hierarchy, BuildPropertyNames.RunAnalyzersDuringLiveAnalysis, out var runAnayzersDuringLiveAnalysis))
        {
            ProjectSystemProject.RunAnalyzersDuringLiveAnalysis = runAnayzersDuringLiveAnalysis;
        }

        Hierarchy = hierarchy;
        ConnectHierarchyEvents();
        RefreshBinOutputPath();

        var projectHierarchyGuid = GetProjectIDGuid(hierarchy);

        _externalErrorReporter = new ProjectExternalErrorReporter(ProjectSystemProject.Id, projectHierarchyGuid, externalErrorReportingPrefix, language, workspaceImpl);
        _batchScopeCreator = componentModel.GetService<SolutionEventsBatchScopeCreator>();
        _batchScopeCreator.StartTrackingProject(ProjectSystemProject, Hierarchy);
    }

    public string AssemblyName => ProjectSystemProject.AssemblyName;

    public string GetOutputFileName()
        => ProjectSystemProject.CompilationOutputAssemblyFilePath;

    public virtual void Disconnect()
    {
        _batchScopeCreator.StopTrackingProject(ProjectSystemProject);

        ProjectSystemProjectOptionsProcessor?.Dispose();
        ProjectCodeModel.OnProjectClosed();
        ProjectSystemProject.RemoveFromWorkspace();

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

        var folders = GetFolderNamesForDocument(filename);

        ProjectSystemProject.AddSourceFile(filename, sourceCodeKind, folders);
    }

    protected void AddFile(
        string filename,
        string linkMetadata,
        SourceCodeKind sourceCodeKind)
    {
        // We have tests that assert that XOML files should not get added; this was similar
        // behavior to how ASP.NET projects would add .aspx files even though we ultimately ignored
        // them. XOML support is planned to go away for Dev16, but for now leave the logic there.
        if (filename.EndsWith(".xoml"))
        {
            return;
        }

        var folders = ImmutableArray<string>.Empty;
        if (!string.IsNullOrEmpty(linkMetadata))
        {
            var linkFolderPath = Path.GetDirectoryName(linkMetadata);
            folders = linkFolderPath.Split(PathSeparatorCharacters, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
        }
        else if (!string.IsNullOrEmpty(ProjectSystemProject.FilePath))
        {
            var relativePath = PathUtilities.GetRelativePath(_projectDirectory, filename);
            var relativePathParts = relativePath.Split(PathSeparatorCharacters);
            folders = ImmutableArray.Create(relativePathParts, start: 0, length: relativePathParts.Length - 1);
        }

        ProjectSystemProject.AddSourceFile(filename, sourceCodeKind, folders);
    }

    protected void RemoveFile(string filename)
    {
        // We have tests that assert that XOML files should not get added; this was similar
        // behavior to how ASP.NET projects would add .aspx files even though we ultimately ignored
        // them. XOML support is planned to go away for Dev16, but for now leave the logic there.
        if (filename.EndsWith(".xoml"))
        {
            return;
        }

        ProjectSystemProject.RemoveSourceFile(filename);
        ProjectCodeModel.OnSourceFileRemoved(filename);
    }

    protected void RefreshBinOutputPath()
    {
        // These projects are created against the same hierarchy as the "main" project that
        // hosts the rest of the code; if we query the IVsHierarchy for the output path
        // we'll end up with duplicate output paths which can break P2P referencing. Since the output
        // path doesn't make sense for these, we'll ignore them.
        if (_ignoreOutputPath)
        {
            return;
        }

        if (Hierarchy is not IVsBuildPropertyStorage storage)
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
            if (ProjectSystemProject.FilePath == null)
            {
                return;
            }

            outputDirectory = FileUtilities.ResolveRelativePath(outputDirectory, Path.GetDirectoryName(ProjectSystemProject.FilePath));
        }

        if (outputDirectory == null)
        {
            return;
        }

        ProjectSystemProject.OutputFilePath = FileUtilities.NormalizeAbsolutePath(Path.Combine(outputDirectory, targetFileName));

        if (ErrorHandler.Succeeded(storage.GetPropertyValue("TargetRefPath", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out var targetRefPath)) && !string.IsNullOrEmpty(targetRefPath))
        {
            ProjectSystemProject.OutputRefFilePath = targetRefPath;
        }
        else
        {
            ProjectSystemProject.OutputRefFilePath = null;
        }
    }

    private static Guid GetProjectIDGuid(IVsHierarchy hierarchy)
    {
        if (hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid, out var guid))
        {
            return guid;
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Map of folder item IDs in the workspace to the string version of their path.
    /// </summary>
    /// <remarks>Using item IDs as a key like this in a long-lived way is considered unsupported by CPS and other
    /// IVsHierarchy providers, but this code (which is fairly old) still makes the assumptions anyways.</remarks>
    private readonly Dictionary<uint, ImmutableArray<string>> _folderNameMap = [];

    private ImmutableArray<string> GetFolderNamesForDocument(string filename)
    {
        var itemid = Hierarchy.TryGetItemId(filename);
        if (itemid != VSConstants.VSITEMID_NIL)
        {
            return GetFolderNamesForDocument(itemid);
        }

        return default;
    }

    private ImmutableArray<string> GetFolderNamesForDocument(uint documentItemID)
    {
        AssertIsForeground();

        if (documentItemID != (uint)VSConstants.VSITEMID.Nil && Hierarchy.GetProperty(documentItemID, (int)VsHierarchyPropID.Parent, out var parentObj) == VSConstants.S_OK)
        {
            var parentID = UnboxVSItemId(parentObj);
            if (parentID is not ((uint)VSConstants.VSITEMID.Nil) and not ((uint)VSConstants.VSITEMID.Root))
            {
                return GetFolderNamesForFolder(parentID);
            }
        }

        return ImmutableArray<string>.Empty;
    }

    private ImmutableArray<string> GetFolderNamesForFolder(uint folderItemID)
    {
        AssertIsForeground();

        using var pooledObject = SharedPools.Default<List<string>>().GetPooledObject();

        var newFolderNames = pooledObject.Object;

        if (!_folderNameMap.TryGetValue(folderItemID, out var folderNames))
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

    // Different hierarchies are inconsistent on whether they return ints or uints for VSItemIds.
    // Technically it should be a uint.  However, there's no enforcement of this, and marshalling
    // from native to managed can end up resulting in boxed ints instead.  Handle both here so 
    // we're resilient to however the IVsHierarchy was actually implemented.
    private static uint UnboxVSItemId(object id)
        => id is uint ? (uint)id : unchecked((uint)(int)id);

    private static void ComputeFolderNames(uint folderItemID, List<string> names, IVsHierarchy hierarchy)
    {
        if (hierarchy.GetProperty(folderItemID, (int)VsHierarchyPropID.Name, out var nameObj) == VSConstants.S_OK)
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

        if (hierarchy.GetProperty(folderItemID, (int)VsHierarchyPropID.Parent, out var parentObj) == VSConstants.S_OK)
        {
            var parentID = UnboxVSItemId(parentObj);
            if (parentID is not ((uint)VSConstants.VSITEMID.Nil) and not ((uint)VSConstants.VSITEMID.Root))
            {
                ComputeFolderNames(parentID, names, hierarchy);
            }
        }
    }

    /// <summary>
    /// Get the value of "rootnamespace" property of the project ("" if not defined, which means global namespace),
    /// or null if it is unknown or not applicable. 
    /// </summary>
    /// <remarks>
    /// This property has different meaning between C# and VB, each project type can decide how to interpret the value.
    /// </remarks>>
    private static string GetRootNamespacePropertyValue(IVsHierarchy hierarchy)
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
        // used interchangeably in VS. For example, (1) the value is define in "rootnamespace" property in project 
        // files and, (2) the property name we use to call into hierarchy below to retrieve the value is 
        // called "DefaultNamespace".

        if (hierarchy.TryGetProperty(__VSHPROPID.VSHPROPID_DefaultNamespace, out string value))
        {
            return value;
        }

        return null;
    }

    private static bool TryGetPropertyValue(IVsHierarchy hierarchy, string propertyName, out string propertyValue)
    {
        if (hierarchy is not IVsBuildPropertyStorage storage)
        {
            propertyValue = null;
            return false;
        }

        return ErrorHandler.Succeeded(storage.GetPropertyValue(propertyName, null, (uint)_PersistStorageType.PST_PROJECT_FILE, out propertyValue));
    }

    private static bool TryGetBoolPropertyValue(IVsHierarchy hierarchy, string propertyName, out bool? propertyValue)
    {
        if (!TryGetPropertyValue(hierarchy, propertyName, out var stringPropertyValue))
        {
            propertyValue = null;
            return false;
        }

        propertyValue = bool.TryParse(stringPropertyValue, out var parsedBoolValue) ? parsedBoolValue : null;
        return true;
    }
}
