// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DecompiledSource;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

[ExportMetadataAsSourceFileProvider(ProviderName), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DecompilationMetadataAsSourceFileProvider(IImplementationAssemblyLookupService implementationAssemblyLookupService) : IMetadataAsSourceFileProvider
{
    internal const string ProviderName = "Decompilation";

    /// <summary>
    /// Accessed only in <see cref="GetGeneratedFileAsync"/> and <see cref="CleanupGeneratedFiles"/>, both of which
    /// are called under a lock in <see cref="MetadataAsSourceFileService"/>.  So this is safe as a plain
    /// dictionary.
    /// </summary>
    private readonly Dictionary<UniqueDocumentKey, MetadataAsSourceGeneratedFileInfo> _keyToInformation = [];

    private readonly IImplementationAssemblyLookupService _implementationAssemblyLookupService = implementationAssemblyLookupService;

    public async Task<(MetadataAsSourceFile, MetadataAsSourceFileMetadata)?> GetGeneratedFileAsync(
        MetadataAsSourceWorkspace metadataWorkspace,
        Workspace sourceWorkspace,
        Project sourceProject,
        ISymbol symbol,
        bool signaturesOnly,
        MetadataAsSourceOptions options,
        TelemetryMessage? telemetryMessage,
        IMetadataDocumentPersister persister,
        CancellationToken cancellationToken)
    {
        // Use the current fallback analyzer config options from the source workspace.
        // Decompilation does not add projects to the MAS workspace, hence the workspace might remain empty and not receive fallback options automatically.
        metadataWorkspace.OnSolutionFallbackAnalyzerOptionsChanged(sourceWorkspace.CurrentSolution.FallbackAnalyzerOptions);

        var topLevelNamedType = MetadataAsSourceHelpers.GetTopLevelContainingNamedType(symbol);
        var symbolId = SymbolKey.Create(symbol, cancellationToken);
        var compilation = await sourceProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

        // If we've been asked for signatures only, then we never want to use the decompiler
        var useDecompiler = !signaturesOnly && options.NavigateToDecompiledSources;

        // If the assembly wants to suppress decompilation we respect that
        if (useDecompiler)
        {
#pragma warning disable SYSLIB0025  // 'SuppressIldasmAttribute' is obsolete: 'SuppressIldasmAttribute has no effect in .NET 6.0+.'
            useDecompiler = !symbol.ContainingAssembly.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name == nameof(SuppressIldasmAttribute)
                && attribute.AttributeClass.ToNameDisplayString() == typeof(SuppressIldasmAttribute).FullName);
#pragma warning restore SYSLIB0025
        }

        var refInfo = GetReferenceInfo(compilation, symbol.ContainingAssembly);

        // If its a reference assembly we won't get real code anyway, so better to
        // not use the decompiler, as the stubs will at least be in the right language
        // (decompiler only produces C#)
        if (useDecompiler)
        {
            useDecompiler = !refInfo.isReferenceAssembly;
        }

        var infoKey = await GetUniqueDocumentKeyAsync(sourceProject, topLevelNamedType, signaturesOnly: !useDecompiler, cancellationToken).ConfigureAwait(false);

        var fileInfo = _keyToInformation.GetOrAdd(infoKey,
            _ => new MetadataAsSourceGeneratedFileInfo(sourceWorkspace, sourceProject, topLevelNamedType, signaturesOnly: !useDecompiler, persister));

        var generatedDocumentId = fileInfo.DocumentId;
        Location navigateLocation;

        var existingDocument = metadataWorkspace.CurrentSolution.GetDocument(fileInfo.DocumentId);
        if (existingDocument is null)
        {
            var sourceText = await persister.TryGetExistingTextAsync(fileInfo.TemporaryFilePath, MetadataAsSourceGeneratedFileInfo.Encoding, MetadataAsSourceGeneratedFileInfo.ChecksumAlgorithm,
                verifyExistingDocument: text => true, cancellationToken).ConfigureAwait(false);

            // We don't have this file in the workspace.  We need to create a project to put it in.
            var temporaryProjectInfo = GenerateProjectAndDocumentInfo(fileInfo, metadataWorkspace.CurrentSolution.Services, sourceProject, topLevelNamedType);
            metadataWorkspace.OnProjectAdded(temporaryProjectInfo);
            var temporaryDocument = metadataWorkspace.CurrentSolution
                .GetRequiredDocument(generatedDocumentId);

            // Generate the file if it doesn't exist (we may still have it if there was a previous request for it that was then closed).
            if (sourceText is null)
            {
                if (useDecompiler)
                {
                    try
                    {
                        // Fetch the IDecompiledSourceService from the temporary document, not the original one -- it
                        // may be a different language because we don't have support for decompiling into VB.NET, so we just
                        // use C#.
                        var decompiledSourceService = temporaryDocument.GetLanguageService<IDecompiledSourceService>();

                        if (decompiledSourceService != null)
                        {
                            var decompilationDocument = await decompiledSourceService.AddSourceToAsync(temporaryDocument, compilation, symbol, refInfo.metadataReference, refInfo.assemblyLocation, formattingOptions: null, cancellationToken).ConfigureAwait(false);
                            telemetryMessage?.SetDecompiled(decompilationDocument is not null);
                            if (decompilationDocument is not null)
                            {
                                temporaryDocument = decompilationDocument;
                            }
                            else
                            {
                                useDecompiler = false;
                            }
                        }
                        else
                        {
                            useDecompiler = false;
                        }
                    }
                    catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken, ErrorSeverity.General))
                    {
                        useDecompiler = false;
                    }
                }

                if (!useDecompiler)
                {
                    var sourceFromMetadataService = temporaryDocument.Project.Services.GetRequiredService<IMetadataAsSourceService>();
                    temporaryDocument = await sourceFromMetadataService.AddSourceToAsync(temporaryDocument, compilation, symbol, formattingOptions: null, cancellationToken).ConfigureAwait(false);
                }

                // We have the content, so write it out to disk
                sourceText = await temporaryDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                await persister.WriteMetadataDocumentAsync(fileInfo.TemporaryFilePath, MetadataAsSourceGeneratedFileInfo.Encoding, sourceText,
                    logFailure: e => { }, cancellationToken).ConfigureAwait(false);
            }

            // Retrieve the navigable location for the symbol using the generated syntax.  
            navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryDocument, cancellationToken).ConfigureAwait(false);

            // Update the workspace to pull the text we just generated.
            var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default, fileInfo.TemporaryFilePath);
            var textLoader = TextLoader.From(textAndVersion);

            metadataWorkspace.OnDocumentTextLoaderChanged(generatedDocumentId, textLoader);
        }
        else
        {
            // The file already exists in the workspace, so we can just use that.
            navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, existingDocument, cancellationToken).ConfigureAwait(false);
        }

        var documentName = string.Format(
            "{0} [{1}]",
            topLevelNamedType.Name,
            useDecompiler ? FeaturesResources.Decompiled : FeaturesResources.from_metadata);

        var documentTooltip = topLevelNamedType.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

        return (new MetadataAsSourceFile(fileInfo.TemporaryFilePath, navigateLocation, documentName, documentTooltip), new MetadataAsSourceFileMetadata(fileInfo.SignaturesOnly, fileInfo.Workspace, fileInfo.SourceProjectId));
    }

    private (MetadataReference? metadataReference, string? assemblyLocation, bool isReferenceAssembly) GetReferenceInfo(Compilation compilation, IAssemblySymbol containingAssembly)
    {
        var metadataReference = compilation.GetMetadataReference(containingAssembly);
        var assemblyLocation = (metadataReference as PortableExecutableReference)?.FilePath;

        var isReferenceAssembly = MetadataAsSourceHelpers.IsReferenceAssembly(containingAssembly);

        if (assemblyLocation is not null &&
            isReferenceAssembly &&
            !_implementationAssemblyLookupService.TryFindImplementationAssemblyPath(assemblyLocation, out assemblyLocation))
        {
            try
            {
                var fullAssemblyName = containingAssembly.Identity.GetDisplayName();
                GlobalAssemblyCache.Instance.ResolvePartialName(fullAssemblyName, out assemblyLocation, preferredCulture: CultureInfo.CurrentCulture);
                isReferenceAssembly = assemblyLocation is null;
            }
            catch (IOException)
            {
                // If we get an IO exception we can safely ignore it, and the system will show the metadata view of the reference assembly.
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Diagnostic))
            {
            }
        }

        return (metadataReference, assemblyLocation, isReferenceAssembly);
    }

    public void CleanupGeneratedFiles(MetadataAsSourceWorkspace workspace)
    {
        foreach (var project in _keyToInformation.Values.Select(d => d.DocumentId.ProjectId).Distinct().ToList())
        {
            workspace.OnProjectRemoved(project);
        }

        _keyToInformation.Clear();
    }

    private static ProjectInfo GenerateProjectAndDocumentInfo(
        MetadataAsSourceGeneratedFileInfo fileInfo,
        SolutionServices services,
        Project sourceProject,
        INamedTypeSymbol topLevelNamedType)
    {
        var generatedDocumentId = fileInfo.DocumentId;
        var projectId = generatedDocumentId.ProjectId;

        var parseOptions = sourceProject.Language == fileInfo.LanguageName
            ? sourceProject.ParseOptions
            : sourceProject.Solution.Services.GetLanguageServices(fileInfo.LanguageName).GetRequiredService<ISyntaxTreeFactoryService>().GetDefaultParseOptionsWithLatestLanguageVersion();

        var assemblyIdentity = topLevelNamedType.ContainingAssembly.Identity;

        // Just say it's always a DLL since we probably won't have a Main method
        var compilationOptions = services.GetRequiredLanguageService<ICompilationFactoryService>(fileInfo.LanguageName).GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

        // We need to include the version information of the assembly so InternalsVisibleTo and stuff works
        var assemblyInfoDocumentId = DocumentId.CreateNewId(projectId);
        var assemblyInfoFileName = "AssemblyInfo" + fileInfo.Extension;
        var assemblyInfoString = fileInfo.LanguageName == LanguageNames.CSharp
            ? string.Format(@"[assembly: System.Reflection.AssemblyVersion(""{0}"")]", assemblyIdentity.Version)
            : string.Format(@"<Assembly: System.Reflection.AssemblyVersion(""{0}"")>", assemblyIdentity.Version);

        var assemblyInfoSourceText = SourceText.From(assemblyInfoString, MetadataAsSourceGeneratedFileInfo.Encoding, MetadataAsSourceGeneratedFileInfo.ChecksumAlgorithm);

        var assemblyInfoDocument = DocumentInfo.Create(
            assemblyInfoDocumentId,
            assemblyInfoFileName,
            loader: TextLoader.From(assemblyInfoSourceText.Container, VersionStamp.Default),
            filePath: null,
            isGenerated: true)
            .WithDesignTimeOnly(true);

        var emptySourceText = SourceText.From(string.Empty, MetadataAsSourceGeneratedFileInfo.Encoding, MetadataAsSourceGeneratedFileInfo.ChecksumAlgorithm);
        var generatedDocument = DocumentInfo.Create(
            generatedDocumentId,
            Path.GetFileName(fileInfo.TemporaryFilePath),
            // We'll update the loader later when we actually write the file to disk.
            loader: TextLoader.From(emptySourceText.Container, VersionStamp.Default),
            filePath: fileInfo.TemporaryFilePath,
            isGenerated: true)
            .WithDesignTimeOnly(true);

        var projectInfo = ProjectInfo.Create(
            new ProjectInfo.ProjectAttributes(
                id: projectId,
                version: VersionStamp.Default,
                name: assemblyIdentity.Name,
                assemblyName: assemblyIdentity.Name,
                language: fileInfo.LanguageName,
                compilationOutputInfo: default,
                checksumAlgorithm: MetadataAsSourceGeneratedFileInfo.ChecksumAlgorithm),
            compilationOptions: compilationOptions,
            parseOptions: parseOptions,
            documents: [assemblyInfoDocument, generatedDocument],
            metadataReferences: [.. sourceProject.MetadataReferences]);

        return projectInfo;

    }

    private static async Task<UniqueDocumentKey> GetUniqueDocumentKeyAsync(Project project, INamedTypeSymbol topLevelNamedType, bool signaturesOnly, CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(compilation, "We are trying to produce a key for a language that doesn't support compilations.");

        var peMetadataReference = compilation.GetMetadataReference(topLevelNamedType.ContainingAssembly) as PortableExecutableReference;

        if (peMetadataReference?.FilePath != null)
        {
            return new UniqueDocumentKey(peMetadataReference.FilePath, peMetadataReference.GetMetadataId(), project.Language, SymbolKey.Create(topLevelNamedType, cancellationToken), signaturesOnly);
        }
        else
        {
            var containingAssembly = topLevelNamedType.ContainingAssembly;
            return new UniqueDocumentKey(containingAssembly.Identity, containingAssembly.GetMetadata()?.Id, project.Language, SymbolKey.Create(topLevelNamedType, cancellationToken), signaturesOnly);
        }
    }

    private sealed class UniqueDocumentKey : IEquatable<UniqueDocumentKey>
    {
        private static readonly IEqualityComparer<SymbolKey> s_symbolIdComparer = SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: true);

        /// <summary>
        /// The path to the assembly. Null in the case of in-memory assemblies, where we then use assembly identity.
        /// </summary>
        private readonly string? _filePath;

        /// <summary>
        /// Assembly identity. Only non-null if <see cref="_filePath"/> is null, where it's an in-memory assembly.
        /// </summary>
        private readonly AssemblyIdentity? _assemblyIdentity;

        private readonly MetadataId? _metadataId;
        private readonly string _language;
        private readonly SymbolKey _symbolId;
        private readonly bool _signaturesOnly;

        public UniqueDocumentKey(string filePath, MetadataId? metadataId, string language, SymbolKey symbolId, bool signaturesOnly)
        {
            Contract.ThrowIfNull(filePath);

            _filePath = filePath;
            _metadataId = metadataId;
            _language = language;
            _symbolId = symbolId;
            _signaturesOnly = signaturesOnly;
        }

        public UniqueDocumentKey(AssemblyIdentity assemblyIdentity, MetadataId? metadataId, string language, SymbolKey symbolId, bool signaturesOnly)
        {
            Contract.ThrowIfNull(assemblyIdentity);

            _assemblyIdentity = assemblyIdentity;
            _metadataId = metadataId;
            _language = language;
            _symbolId = symbolId;
            _signaturesOnly = signaturesOnly;
        }

        public bool Equals(UniqueDocumentKey? other)
        {
            if (other == null)
            {
                return false;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(_filePath, other._filePath) &&
                object.Equals(_assemblyIdentity, other._assemblyIdentity) &&
                object.Equals(_metadataId, other._metadataId) &&
                _language == other._language &&
                s_symbolIdComparer.Equals(_symbolId, other._symbolId) &&
                _signaturesOnly == other._signaturesOnly;
        }

        public override bool Equals(object? obj)
            => Equals(obj as UniqueDocumentKey);

        public override int GetHashCode()
        {
            return
                Hash.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(_filePath ?? string.Empty),
                    Hash.Combine(_assemblyIdentity?.GetHashCode() ?? 0,
                        Hash.Combine(_metadataId?.GetHashCode() ?? 0,
                            Hash.Combine(_language.GetHashCode(),
                                Hash.Combine(s_symbolIdComparer.GetHashCode(_symbolId),
                                    _signaturesOnly.GetHashCode())))));
        }
    }
}
