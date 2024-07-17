// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

[ExportMetadataAsSourceFileProvider(ProviderName), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DecompilationMetadataAsSourceFileProvider(IImplementationAssemblyLookupService implementationAssemblyLookupService) : IMetadataAsSourceFileProvider
{
    internal const string ProviderName = "Decompilation";

    /// <summary>
    /// Accessed only in <see cref="GetGeneratedFileAsync"/> and <see cref="CleanupGeneratedFiles"/>, both of which
    /// are called under a lock in <see cref="MetadataAsSourceFileService"/>.  So this is safe as a plain
    /// dictionary.
    /// </summary>
    private readonly Dictionary<UniqueDocumentKey, MetadataAsSourceGeneratedFileInfo> _keyToInformation = [];

    /// <summary>
    /// Accessed both in <see cref="GetGeneratedFileAsync"/> and in UI thread operations.  Those should not
    /// generally run concurrently.  However, to be safe, we make this a concurrent dictionary to be safe to that
    /// potentially happening.
    /// </summary>
    private readonly ConcurrentDictionary<string, MetadataAsSourceGeneratedFileInfo> _generatedFilenameToInformation = new(StringComparer.OrdinalIgnoreCase);

    private readonly IImplementationAssemblyLookupService _implementationAssemblyLookupService = implementationAssemblyLookupService;

    /// <summary>
    /// Only accessed and mutated from UI thread.
    /// </summary>
    private IBidirectionalMap<MetadataAsSourceGeneratedFileInfo, DocumentId> _openedDocumentIds = BidirectionalMap<MetadataAsSourceGeneratedFileInfo, DocumentId>.Empty;

    public async Task<MetadataAsSourceFile?> GetGeneratedFileAsync(
        MetadataAsSourceWorkspace metadataWorkspace,
        Workspace sourceWorkspace,
        Project sourceProject,
        ISymbol symbol,
        bool signaturesOnly,
        MetadataAsSourceOptions options,
        string tempPath,
        TelemetryMessage? telemetryMessage,
        CancellationToken cancellationToken)
    {
        // Use the current fallback analyzer config options from the source workspace.
        // Decompilation does not add projects to the MAS workspace, hence the workspace might remain empty and not receive fallback options automatically.
        var metadataSolution = metadataWorkspace.CurrentSolution.WithFallbackAnalyzerOptions(sourceWorkspace.CurrentSolution.FallbackAnalyzerOptions);

        MetadataAsSourceGeneratedFileInfo fileInfo;
        Location? navigateLocation = null;
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
        fileInfo = _keyToInformation.GetOrAdd(infoKey,
            _ => new MetadataAsSourceGeneratedFileInfo(tempPath, sourceWorkspace, sourceProject, topLevelNamedType, signaturesOnly: !useDecompiler));

        _generatedFilenameToInformation[fileInfo.TemporaryFilePath] = fileInfo;

        if (!File.Exists(fileInfo.TemporaryFilePath))
        {
            // We need to generate this. First, we'll need a temporary project to do the generation into. We
            // avoid loading the actual file from disk since it doesn't exist yet.

            var (temporaryProjectInfo, temporaryDocumentId) = fileInfo.GetProjectInfoAndDocumentId(metadataSolution.Services, loadFileFromDisk: false);
            var temporaryDocument = metadataSolution
                .AddProject(temporaryProjectInfo)
                .GetRequiredDocument(temporaryDocumentId);

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
            var text = await temporaryDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // Create the directory. It's possible a parallel deletion is happening in another process, so we may have
            // to retry this a few times.
            //
            // If we still can't create the folder after 5 seconds, assume we will not be able to create it and
            // continue without actually writing the text to disk.
            var directoryToCreate = Path.GetDirectoryName(fileInfo.TemporaryFilePath)!;
            var stopwatch = SharedStopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(5);
            var firstAttempt = true;
            var skipWritingFile = false;
            while (!Directory.Exists(directoryToCreate))
            {
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
                {
                    // If we still can't create the folder after 5 seconds, assume we will not be able to create it.
                    skipWritingFile = true;
                    break;
                }

                try
                {
                    if (firstAttempt)
                    {
                        firstAttempt = false;
                    }
                    else
                    {
                        await Task.Delay(DelayTimeSpan.Short, cancellationToken).ConfigureAwait(false);
                    }

                    Directory.CreateDirectory(directoryToCreate);
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            if (!skipWritingFile)
            {
                using (var textWriter = new StreamWriter(fileInfo.TemporaryFilePath, append: false, encoding: MetadataAsSourceGeneratedFileInfo.Encoding))
                {
                    text.Write(textWriter, cancellationToken);
                }

                // Mark read-only
                new FileInfo(fileInfo.TemporaryFilePath).IsReadOnly = true;
            }

            // Locate the target in the thing we just created
            navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryDocument, cancellationToken).ConfigureAwait(false);
        }

        // If we don't have a location yet, then that means we're re-using an existing file. In this case, we'll want to relocate the symbol.
        navigateLocation ??= await RelocateSymbol_NoLockAsync(metadataSolution, fileInfo, symbolId, cancellationToken).ConfigureAwait(false);

        var documentName = string.Format(
            "{0} [{1}]",
            topLevelNamedType.Name,
            useDecompiler ? FeaturesResources.Decompiled : FeaturesResources.from_metadata);

        var documentTooltip = topLevelNamedType.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

        return new MetadataAsSourceFile(fileInfo.TemporaryFilePath, navigateLocation, documentName, documentTooltip);
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

    private async Task<Location> RelocateSymbol_NoLockAsync(Solution solution, MetadataAsSourceGeneratedFileInfo fileInfo, SymbolKey symbolId, CancellationToken cancellationToken)
    {
        // We need to relocate the symbol in the already existing file. If the file is open, we can just
        // reuse that workspace. Otherwise, we have to go spin up a temporary project to do the binding.
        if (_openedDocumentIds.TryGetValue(fileInfo, out var openDocumentId))
        {
            // Awesome, it's already open. Let's try to grab a document for it
            var document = solution.GetRequiredDocument(openDocumentId);

            return await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, document, cancellationToken).ConfigureAwait(false);
        }

        // Annoying case: the file is still on disk. Only real option here is to spin up a fake project to go and bind in.
        var (temporaryProjectInfo, temporaryDocumentId) = fileInfo.GetProjectInfoAndDocumentId(solution.Services, loadFileFromDisk: true);
        var temporaryDocument = solution.AddProject(temporaryProjectInfo).GetRequiredDocument(temporaryDocumentId);

        return await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryDocument, cancellationToken).ConfigureAwait(false);
    }

    private static void AssertIsMainThread(MetadataAsSourceWorkspace workspace)
    {
        Contract.ThrowIfNull(workspace);
        var threadingService = workspace.Services.GetRequiredService<IWorkspaceThreadingServiceProvider>().Service;
        Contract.ThrowIfFalse(threadingService.IsOnMainThread);
    }

    public bool ShouldCollapseOnOpen(MetadataAsSourceWorkspace workspace, string filePath, BlockStructureOptions blockStructureOptions)
    {
        AssertIsMainThread(workspace);

        if (_generatedFilenameToInformation.TryGetValue(filePath, out var info))
        {
            return info.SignaturesOnly
                ? blockStructureOptions.CollapseEmptyMetadataImplementationsWhenFirstOpened
                : blockStructureOptions.CollapseMetadataImplementationsWhenFirstOpened;
        }

        return false;
    }

    public bool TryAddDocumentToWorkspace(MetadataAsSourceWorkspace workspace, string filePath, SourceTextContainer sourceTextContainer)
    {
        AssertIsMainThread(workspace);

        if (_generatedFilenameToInformation.TryGetValue(filePath, out var fileInfo))
        {
            Contract.ThrowIfTrue(_openedDocumentIds.ContainsKey(fileInfo));

            // We do own the file, so let's open it up in our workspace
            var (projectInfo, documentId) = fileInfo.GetProjectInfoAndDocumentId(workspace.Services.SolutionServices, loadFileFromDisk: true);

            workspace.OnProjectAdded(projectInfo);
            workspace.OnDocumentOpened(documentId, sourceTextContainer);

            _openedDocumentIds = _openedDocumentIds.Add(fileInfo, documentId);

            return true;
        }

        return false;
    }

    public bool TryRemoveDocumentFromWorkspace(MetadataAsSourceWorkspace workspace, string filePath)
    {
        AssertIsMainThread(workspace);

        if (_generatedFilenameToInformation.TryGetValue(filePath, out var fileInfo))
        {
            if (_openedDocumentIds.ContainsKey(fileInfo))
                return RemoveDocumentFromWorkspace(workspace, fileInfo);
        }

        return false;
    }

    private bool RemoveDocumentFromWorkspace(MetadataAsSourceWorkspace workspace, MetadataAsSourceGeneratedFileInfo fileInfo)
    {
        AssertIsMainThread(workspace);

        var documentId = _openedDocumentIds.GetValueOrDefault(fileInfo);
        Contract.ThrowIfNull(documentId);

        workspace.OnDocumentClosed(documentId, new WorkspaceFileTextLoader(workspace.Services.SolutionServices, fileInfo.TemporaryFilePath, MetadataAsSourceGeneratedFileInfo.Encoding));
        workspace.OnProjectRemoved(documentId.ProjectId);

        _openedDocumentIds = _openedDocumentIds.RemoveKey(fileInfo);

        return true;
    }

    public Project? MapDocument(Document document)
    {
        MetadataAsSourceGeneratedFileInfo? fileInfo;

        if (!_openedDocumentIds.TryGetKey(document.Id, out fileInfo))
        {
            return null;
        }

        // WARNING: do not touch any state fields outside the lock.
        var solution = fileInfo.Workspace.CurrentSolution;
        var project = solution.GetProject(fileInfo.SourceProjectId);
        return project;
    }

    public void CleanupGeneratedFiles(MetadataAsSourceWorkspace workspace)
    {
        // Clone the list so we don't break our own enumeration
        foreach (var generatedFileInfo in _generatedFilenameToInformation.Values.ToList())
        {
            if (_openedDocumentIds.ContainsKey(generatedFileInfo))
                RemoveDocumentFromWorkspace(workspace, generatedFileInfo);
        }

        _generatedFilenameToInformation.Clear();
        _keyToInformation.Clear();
        Contract.ThrowIfFalse(_openedDocumentIds.IsEmpty);
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

    private class UniqueDocumentKey : IEquatable<UniqueDocumentKey>
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
