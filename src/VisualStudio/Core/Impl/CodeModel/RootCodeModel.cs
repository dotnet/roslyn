// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

[ComVisible(true)]
[ComDefaultInterface(typeof(EnvDTE.CodeModel))]
public sealed class RootCodeModel : AbstractCodeModelObject, ICodeElementContainer<AbstractExternalCodeElement>, EnvDTE.CodeModel, EnvDTE80.CodeModel2
{
    internal static EnvDTE.CodeModel Create(CodeModelState state, EnvDTE.Project parent, ProjectId projectId)
    {
        var rootCodeModel = new RootCodeModel(state, parent, projectId);
        return (EnvDTE.CodeModel)ComAggregate.CreateAggregatedObject(rootCodeModel);
    }

    private readonly ParentHandle<EnvDTE.Project> _parentHandle;
    private readonly ProjectId _projectId;

    private RootCodeModel(CodeModelState state, EnvDTE.Project parent, ProjectId projectId)
        : base(state)
    {
        _parentHandle = new ParentHandle<EnvDTE.Project>(parent);
        _projectId = projectId;
    }

    private Project GetProject()
        => Workspace.CurrentSolution.GetProject(_projectId);

    private Task<Compilation> GetCompilationAsync()
        => GetProject().GetCompilationAsync();

    private ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetFileCodeModel(object location)
    {
        if (location is string locationString)
        {
            var vsProject = _parentHandle.Value;
            var vsProjectItems = vsProject.ProjectItems;

            if (vsProjectItems == null)
            {
                throw Exceptions.ThrowEFail();
            }

            var project = GetProject();
            var projectDirectory = Path.GetDirectoryName(project.FilePath);
            var absoluteFilePath = Path.GetFullPath(Path.Combine(projectDirectory, locationString));

            var foundFile = false;
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                if (document.FilePath != null && string.Equals(absoluteFilePath, document.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    foundFile = true;
                    break;
                }
            }

            if (!foundFile)
            {
                // File doesn't belong to the project, prepare & add it to project
                using (FileUtilities.CreateFileStreamChecked(File.Create, absoluteFilePath))
                {
                    // Note: We just want to create an empty file here, so we immediately close it.
                }

                vsProjectItems.AddFromFile(absoluteFilePath);
            }

            return this.State.ProjectCodeModelFactory.GetProjectCodeModel(_projectId).GetOrCreateFileCodeModel(absoluteFilePath);
        }

        throw Exceptions.ThrowEInvalidArg();
    }

    public EnvDTE.Project Parent
    {
        get { return _parentHandle.Value; }
    }

    public bool IsCaseSensitive
    {
        get { return SyntaxFactsService.IsCaseSensitive; }
    }

    public EnvDTE.CodeAttribute AddAttribute(string name, object location, string value, object position)
        => GetFileCodeModel(location).Object.AddAttribute(name, value, position);

    public EnvDTE.CodeClass AddClass(string name, object location, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        => GetFileCodeModel(location).Object.AddClass(name, position, bases, implementedInterfaces, access);

    public EnvDTE.CodeDelegate AddDelegate(string name, object location, object type, object position, EnvDTE.vsCMAccess access)
        => GetFileCodeModel(location).Object.AddDelegate(name, type, position, access);

    public EnvDTE.CodeEnum AddEnum(string name, object location, object position, object bases, EnvDTE.vsCMAccess access)
        => GetFileCodeModel(location).Object.AddEnum(name, position, bases, access);

    public EnvDTE.CodeFunction AddFunction(string name, object location, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access)
        => GetFileCodeModel(location).Object.AddFunction(name, kind, type, position, access);

    public EnvDTE.CodeInterface AddInterface(string name, object location, object position, object bases, EnvDTE.vsCMAccess access)
        => GetFileCodeModel(location).Object.AddInterface(name, position, bases, access);

    public EnvDTE.CodeNamespace AddNamespace(string name, object location, object position)
        => GetFileCodeModel(location).Object.AddNamespace(name, position);

    public EnvDTE.CodeStruct AddStruct(string name, object location, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        => GetFileCodeModel(location).Object.AddStruct(name, position, bases, implementedInterfaces, access);

    public EnvDTE.CodeVariable AddVariable(string name, object location, object type, object position, EnvDTE.vsCMAccess access)
        => GetFileCodeModel(location).Object.AddVariable(name, type, position, access);

    EnvDTE.CodeElements ICodeElementContainer<AbstractExternalCodeElement>.GetCollection()
        => CodeElements;

    public EnvDTE.CodeElements CodeElements
        => this.State.ThreadingContext.JoinableTaskFactory.Run(async () =>
        {
            var compilation = await GetCompilationAsync().ConfigureAwait(true);

            // Need to ensure we're on the UI thread to make the ExternalCodeNamespace.  It creates UI thread bound
            // com aggregates.
            await this.State.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            var rootNamespace = ExternalCodeNamespace.Create(this.State, _projectId, compilation.GlobalNamespace);
            return rootNamespace.Members;
        });

    public EnvDTE.CodeType CodeTypeFromFullName(string name)
        => this.State.ThreadingContext.JoinableTaskFactory.Run(async () =>
        {
            var compilation = await GetCompilationAsync().ConfigureAwait(true);
            var typeSymbol = CodeModelService.GetTypeSymbolFromFullName(name, compilation);
            if (typeSymbol == null ||
                typeSymbol.TypeKind is TypeKind.Error or TypeKind.Unknown)
            {
                return null;
            }

            // Need to ensure we're on the UI thread to make the call to CreateCodeType.  It creates  UI thread
            // bound com aggregates.
            await this.State.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            return (EnvDTE.CodeType)CodeModelService.CreateCodeType(this.State, _projectId, typeSymbol);
        });

    public EnvDTE.CodeTypeRef CreateCodeTypeRef(object type)
        => CodeModelService.CreateCodeTypeRef(this.State, _projectId, type);

    public bool IsValidID(string name)
        => SyntaxFactsService.IsValidIdentifier(name);

    public void Remove(object element)
        => throw Exceptions.ThrowENotImpl();

    public string DotNetNameFromLanguageSpecific(string languageName)
        => this.State.ThreadingContext.JoinableTaskFactory.Run(async () =>
        {
            var compilation = await GetCompilationAsync().ConfigureAwait(true);
            var typeSymbol = CodeModelService.GetTypeSymbolFromFullName(languageName, compilation);
            if (typeSymbol == null)
                throw Exceptions.ThrowEInvalidArg();

            return MetadataNameHelpers.GetMetadataName(typeSymbol);
        });

    public EnvDTE.CodeElement ElementFromID(string id)
        => throw Exceptions.ThrowENotImpl();

    public string LanguageSpecificNameFromDotNet(string dotNetName)
    {
        // VB implemented this but C# never did. Does it matter?
        throw Exceptions.ThrowENotImpl();
    }

    public void Synchronize()
        => throw Exceptions.ThrowENotImpl();
}
