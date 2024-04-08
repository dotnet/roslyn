// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.Workspaces;

internal static class MiscellaneousFileUtilities
{
    internal static ProjectInfo CreateMiscellaneousProjectInfoForDocument(
        Workspace workspace,
        string filePath,
        TextLoader textLoader,
        LanguageInformation languageInformation,
        SourceHashAlgorithm checksumAlgorithm,
        SolutionServices services,
        ImmutableArray<MetadataReference> metadataReferences)
    {
        var fileExtension = PathUtilities.GetExtension(filePath);
        var fileName = PathUtilities.GetFileName(filePath);

        var languageServices = services.GetLanguageServices(languageInformation.LanguageName);
        var compilationOptions = languageServices.GetService<ICompilationFactoryService>()?.GetDefaultCompilationOptions();

        // Use latest language version which is more permissive, as we cannot find out language version of the project which the file belongs to
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/575761
        var parseOptions = languageServices.GetService<ISyntaxTreeFactoryService>()?.GetDefaultParseOptionsWithLatestLanguageVersion();

        if (parseOptions != null &&
            compilationOptions != null &&
            fileExtension == languageInformation.ScriptExtension)
        {
            parseOptions = parseOptions.WithKind(SourceCodeKind.Script);
            compilationOptions = GetCompilationOptionsWithScriptReferenceResolvers(services, compilationOptions, filePath);
        }

        var projectId = ProjectId.CreateNewId(debugName: $"{workspace.GetType().Name} Files Project for {filePath}");
        var documentId = DocumentId.CreateNewId(projectId, debugName: filePath);

        var sourceCodeKind = parseOptions?.Kind ?? SourceCodeKind.Regular;
        var documentInfo = DocumentInfo.Create(
            id: documentId,
            name: fileName,
            loader: textLoader,
            filePath: filePath,
            sourceCodeKind: sourceCodeKind);

        // The assembly name must be unique for each collection of loose files. Since the name doesn't matter
        // a random GUID can be used.
        var assemblyName = Guid.NewGuid().ToString("N");

        var projectInfo = ProjectInfo.Create(
            new ProjectInfo.ProjectAttributes(
                id: projectId,
                version: VersionStamp.Create(),
                name: FeaturesResources.Miscellaneous_Files,
                assemblyName: assemblyName,
                language: languageInformation.LanguageName,
                compilationOutputFilePaths: default,
                checksumAlgorithm: checksumAlgorithm,
                // Miscellaneous files projects are never fully loaded since, by definition, it won't know
                // what the full set of information is except when the file is script code.
                hasAllInformation: sourceCodeKind == SourceCodeKind.Script),
            compilationOptions: compilationOptions,
            parseOptions: parseOptions,
            documents: [documentInfo],
            metadataReferences: metadataReferences);

        return projectInfo;
    }

    // Do not inline this to avoid loading Microsoft.CodeAnalysis.Scripting unless a script file is opened in the workspace.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CompilationOptions GetCompilationOptionsWithScriptReferenceResolvers(SolutionServices services, CompilationOptions compilationOptions, string filePath)
    {
        var metadataService = services.GetRequiredService<IMetadataService>();

        var baseDirectory = PathUtilities.GetDirectoryName(filePath);

        // TODO (https://github.com/dotnet/roslyn/issues/5325, https://github.com/dotnet/roslyn/issues/13886):
        // - Need to have a way to specify these somewhere in VS options.
        // - Add default namespace imports, default metadata references to match csi.rsp
        // - Add default script globals available in 'csi goo.csx' environment: CommandLineScriptGlobals

        var referenceResolver = RuntimeMetadataReferenceResolver.CreateCurrentPlatformResolver(
            searchPaths: [RuntimeEnvironment.GetRuntimeDirectory()],
            baseDirectory: baseDirectory,
            fileReferenceProvider: metadataService.GetReference);

        return compilationOptions
            .WithMetadataReferenceResolver(referenceResolver)
            .WithSourceReferenceResolver(new SourceFileResolver(searchPaths: [], baseDirectory));
    }
}

internal class LanguageInformation(string languageName, string scriptExtension)
{
    public string LanguageName { get; } = languageName;
    public string ScriptExtension { get; } = scriptExtension;
}

