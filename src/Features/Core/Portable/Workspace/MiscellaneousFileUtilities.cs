// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
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
    internal static ParseOptions WithFileBasedProgramFeatureFlag(ParseOptions options, bool enableFileBasedPrograms)
    {
        if (options.Kind == SourceCodeKind.Script)
        {
            // Scripts are never supposed to be treated as file-based programs
            Contract.ThrowIfTrue(options.Features.ContainsKey("FileBasedProgram"));
            return options;
        }

        return options.WithFeatures(enableFileBasedPrograms
            ? [.. options.Features, new("FileBasedProgram", "true")]
            : [.. options.Features.Where(pair => pair.Key != "FileBasedProgram")]);
    }

    /// <param name="enableFileBasedPrograms">Whether the host has globally enabled the C# file-based programs feature.</param>
    internal static ProjectInfo CreateMiscellaneousProjectInfoForDocument(
        Workspace workspace,
        string filePath,
        TextLoader textLoader,
        LanguageInformation languageInformation,
        SourceHashAlgorithm checksumAlgorithm,
        SolutionServices services,
        ImmutableArray<MetadataReference> metadataReferences,
        bool enableFileBasedPrograms)
    {
        var fileExtension = PathUtilities.GetExtension(filePath);
        var fileName = PathUtilities.GetFileName(filePath);

        var languageName = languageInformation.LanguageName;
        var languageServices = services.GetLanguageServices(languageName);
        var miscellaneousProjectInfoService = languageServices.GetService<IMiscellaneousProjectInfoService>();

        if (miscellaneousProjectInfoService is not null)
        {
            // The MiscellaneousProjectInfoService can override the language name to use for the project, and therefore we have to re-get
            // the right set of language services.
            languageName = miscellaneousProjectInfoService.ProjectLanguageOverride;
            languageServices = services.GetLanguageServices(languageName);
        }

        var compilationOptions = languageServices.GetService<ICompilationFactoryService>()?.GetDefaultCompilationOptions();

        // Use latest language version which is more permissive, as we cannot find out language version of the project which the file belongs to
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/575761
        var parseOptions = languageServices.GetService<ISyntaxTreeFactoryService>()?.GetDefaultParseOptionsWithLatestLanguageVersion();

        if (parseOptions != null)
        {
            if (compilationOptions != null &&
                languageInformation.ScriptExtension is not null &&
                fileExtension == languageInformation.ScriptExtension)
            {
                parseOptions = parseOptions.WithKind(SourceCodeKind.Script);
                compilationOptions = GetCompilationOptionsWithScriptReferenceResolvers(services, compilationOptions, filePath);
            }

            parseOptions = WithFileBasedProgramFeatureFlag(parseOptions, enableFileBasedPrograms);
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

        var addAsAdditionalDocument = miscellaneousProjectInfoService?.AddAsAdditionalDocument ?? false;
        var projectInfo = ProjectInfo.Create(
            new ProjectInfo.ProjectAttributes(
                id: projectId,
                version: VersionStamp.Create(),
                name: FeaturesResources.Miscellaneous_Files,
                assemblyName: assemblyName,
                language: languageName,
                compilationOutputInfo: default,
                checksumAlgorithm: checksumAlgorithm,
                // Miscellaneous files projects are never fully loaded since, by definition, it won't know
                // what the full set of information is except when the file is script code.
                hasAllInformation: sourceCodeKind == SourceCodeKind.Script),
            compilationOptions: compilationOptions,
            parseOptions: parseOptions,
            documents: addAsAdditionalDocument ? null : [documentInfo],
            additionalDocuments: addAsAdditionalDocument ? [documentInfo] : null,
            metadataReferences: metadataReferences,
            analyzerReferences: miscellaneousProjectInfoService?.GetAnalyzerReferences(services));

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
            createFromFileFunc: metadataService.GetReference);

        return compilationOptions
            .WithMetadataReferenceResolver(referenceResolver)
            .WithSourceReferenceResolver(new SourceFileResolver(searchPaths: [], baseDirectory));
    }
}

internal sealed class LanguageInformation(string languageName, string? scriptExtension)
{
    public string LanguageName { get; } = languageName;
    public string? ScriptExtension { get; } = scriptExtension;
}

