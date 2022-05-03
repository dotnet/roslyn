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
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.DecompiledSource;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    [ExportMetadataAsSourceFileProvider(ProviderName), Shared]
    internal class DecompilationMetadataAsSourceFileProvider : IMetadataAsSourceFileProvider
    {
        internal const string ProviderName = "Decompilation";

        private readonly Dictionary<UniqueDocumentKey, MetadataAsSourceGeneratedFileInfo> _keyToInformation = new();

        private readonly Dictionary<string, MetadataAsSourceGeneratedFileInfo> _generatedFilenameToInformation = new(StringComparer.OrdinalIgnoreCase);
        private IBidirectionalMap<MetadataAsSourceGeneratedFileInfo, DocumentId> _openedDocumentIds = BidirectionalMap<MetadataAsSourceGeneratedFileInfo, DocumentId>.Empty;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DecompilationMetadataAsSourceFileProvider()
        {
        }

        public async Task<MetadataAsSourceFile?> GetGeneratedFileAsync(Workspace workspace, Project project, ISymbol symbol, bool signaturesOnly, MetadataAsSourceOptions options, string tempPath, CancellationToken cancellationToken)
        {
            MetadataAsSourceGeneratedFileInfo fileInfo;
            Location? navigateLocation = null;
            var topLevelNamedType = MetadataAsSourceHelpers.GetTopLevelContainingNamedType(symbol);
            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // If we've been asked for signatures only, then we never want to use the decompiler
            var useDecompiler = !signaturesOnly && options.NavigateToDecompiledSources;

            // If the assembly wants to suppress decompilation we respect that
            if (useDecompiler)
            {
                useDecompiler = !symbol.ContainingAssembly.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == nameof(SuppressIldasmAttribute)
                    && attribute.AttributeClass.ToNameDisplayString() == typeof(SuppressIldasmAttribute).FullName);
            }

            var refInfo = GetReferenceInfo(compilation, symbol.ContainingAssembly);

            // If its a reference assembly we won't get real code anyway, so better to
            // not use the decompiler, as the stubs will at least be in the right language
            // (decompiler only produces C#)
            if (useDecompiler)
            {
                useDecompiler = !refInfo.isReferenceAssembly;
            }

            var infoKey = await GetUniqueDocumentKeyAsync(project, topLevelNamedType, signaturesOnly: !useDecompiler, cancellationToken).ConfigureAwait(false);
            fileInfo = _keyToInformation.GetOrAdd(infoKey, _ => new MetadataAsSourceGeneratedFileInfo(tempPath, project, topLevelNamedType, signaturesOnly: !useDecompiler));

            _generatedFilenameToInformation[fileInfo.TemporaryFilePath] = fileInfo;

            if (!File.Exists(fileInfo.TemporaryFilePath))
            {
                // We need to generate this. First, we'll need a temporary project to do the generation into. We
                // avoid loading the actual file from disk since it doesn't exist yet.
                var temporaryProjectInfoAndDocumentId = fileInfo.GetProjectInfoAndDocumentId(workspace, loadFileFromDisk: false);
                var temporaryDocument = workspace.CurrentSolution.AddProject(temporaryProjectInfoAndDocumentId.Item1)
                                                                 .GetRequiredDocument(temporaryProjectInfoAndDocumentId.Item2);

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
                            temporaryDocument = await decompiledSourceService.AddSourceToAsync(temporaryDocument, compilation, symbol, refInfo.metadataReference, refInfo.assemblyLocation, options.GenerationOptions.CleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);
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
                    var sourceFromMetadataService = temporaryDocument.Project.LanguageServices.GetRequiredService<IMetadataAsSourceService>();
                    temporaryDocument = await sourceFromMetadataService.AddSourceToAsync(temporaryDocument, compilation, symbol, options.GenerationOptions, cancellationToken).ConfigureAwait(false);
                }

                // We have the content, so write it out to disk
                var text = await temporaryDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                // Create the directory. It's possible a parallel deletion is happening in another process, so we may have
                // to retry this a few times.
                var directoryToCreate = Path.GetDirectoryName(fileInfo.TemporaryFilePath)!;
                while (!Directory.Exists(directoryToCreate))
                {
                    try
                    {
                        Directory.CreateDirectory(directoryToCreate);
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                using (var textWriter = new StreamWriter(fileInfo.TemporaryFilePath, append: false, encoding: MetadataAsSourceGeneratedFileInfo.Encoding))
                {
                    text.Write(textWriter, cancellationToken);
                }

                // Mark read-only
                new FileInfo(fileInfo.TemporaryFilePath).IsReadOnly = true;

                // Locate the target in the thing we just created
                navigateLocation = await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryDocument, cancellationToken).ConfigureAwait(false);
            }

            // If we don't have a location yet, then that means we're re-using an existing file. In this case, we'll want to relocate the symbol.
            if (navigateLocation == null)
            {
                navigateLocation = await RelocateSymbol_NoLockAsync(workspace, fileInfo, symbolId, cancellationToken).ConfigureAwait(false);
            }

            var documentName = string.Format(
                "{0} [{1}]",
                topLevelNamedType.Name,
                FeaturesResources.from_metadata);

            var documentTooltip = topLevelNamedType.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

            return new MetadataAsSourceFile(fileInfo.TemporaryFilePath, navigateLocation, documentName, documentTooltip);
        }

        private (MetadataReference? metadataReference, string? assemblyLocation, bool isReferenceAssembly) GetReferenceInfo(Compilation compilation, IAssemblySymbol containingAssembly)
        {
            var metadataReference = compilation.GetMetadataReference(containingAssembly);
            var assemblyLocation = (metadataReference as PortableExecutableReference)?.FilePath;

            var isReferenceAssembly = containingAssembly.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == nameof(ReferenceAssemblyAttribute)
                && attribute.AttributeClass.ToNameDisplayString() == typeof(ReferenceAssemblyAttribute).FullName);

            if (assemblyLocation is not null &&
                isReferenceAssembly &&
                !MetadataAsSourceHelpers.TryGetImplementationAssemblyPath(assemblyLocation, out assemblyLocation))
            {
                try
                {
                    var fullAssemblyName = containingAssembly.Identity.GetDisplayName();
                    GlobalAssemblyCache.Instance.ResolvePartialName(fullAssemblyName, out assemblyLocation, preferredCulture: CultureInfo.CurrentCulture);
                    isReferenceAssembly = assemblyLocation is null;
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Diagnostic))
                {
                }
            }

            return (metadataReference, assemblyLocation, isReferenceAssembly);
        }

        private async Task<Location> RelocateSymbol_NoLockAsync(Workspace workspace, MetadataAsSourceGeneratedFileInfo fileInfo, SymbolKey symbolId, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(workspace);

            // We need to relocate the symbol in the already existing file. If the file is open, we can just
            // reuse that workspace. Otherwise, we have to go spin up a temporary project to do the binding.
            if (_openedDocumentIds.TryGetValue(fileInfo, out var openDocumentId))
            {
                // Awesome, it's already open. Let's try to grab a document for it
                var document = workspace.CurrentSolution.GetRequiredDocument(openDocumentId);

                return await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, document, cancellationToken).ConfigureAwait(false);
            }

            // Annoying case: the file is still on disk. Only real option here is to spin up a fake project to go and bind in.
            var temporaryProjectInfoAndDocumentId = fileInfo.GetProjectInfoAndDocumentId(workspace, loadFileFromDisk: true);
            var temporaryDocument = workspace.CurrentSolution.AddProject(temporaryProjectInfoAndDocumentId.Item1)
                                                             .GetRequiredDocument(temporaryProjectInfoAndDocumentId.Item2);

            return await MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolId, temporaryDocument, cancellationToken).ConfigureAwait(false);
        }

        public bool TryAddDocumentToWorkspace(Workspace workspace, string filePath, SourceTextContainer sourceTextContainer)
        {
            if (_generatedFilenameToInformation.TryGetValue(filePath, out var fileInfo))
            {
                Contract.ThrowIfTrue(_openedDocumentIds.ContainsKey(fileInfo));

                // We do own the file, so let's open it up in our workspace
                var newProjectInfoAndDocumentId = fileInfo.GetProjectInfoAndDocumentId(workspace, loadFileFromDisk: true);

                workspace.OnProjectAdded(newProjectInfoAndDocumentId.Item1);
                workspace.OnDocumentOpened(newProjectInfoAndDocumentId.Item2, sourceTextContainer);

                _openedDocumentIds = _openedDocumentIds.Add(fileInfo, newProjectInfoAndDocumentId.Item2);

                return true;
            }

            return false;
        }

        public bool TryRemoveDocumentFromWorkspace(Workspace workspace, string filePath)
        {
            if (_generatedFilenameToInformation.TryGetValue(filePath, out var fileInfo))
            {
                if (_openedDocumentIds.ContainsKey(fileInfo))
                {
                    return RemoveDocumentFromWorkspace(workspace, fileInfo);
                }
            }

            return false;
        }

        private bool RemoveDocumentFromWorkspace(Workspace workspace, MetadataAsSourceGeneratedFileInfo fileInfo)
        {
            var documentId = _openedDocumentIds.GetValueOrDefault(fileInfo);
            Contract.ThrowIfNull(documentId);

            workspace.OnDocumentClosed(documentId, new FileTextLoader(fileInfo.TemporaryFilePath, MetadataAsSourceGeneratedFileInfo.Encoding));
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

        public void CleanupGeneratedFiles(Workspace? workspace)
        {
            // Clone the list so we don't break our own enumeration
            var generatedFileInfoList = _generatedFilenameToInformation.Values.ToList();

            foreach (var generatedFileInfo in generatedFileInfoList)
            {
                if (_openedDocumentIds.ContainsKey(generatedFileInfo))
                {
                    Contract.ThrowIfNull(workspace);

                    RemoveDocumentFromWorkspace(workspace, generatedFileInfo);
                }
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
}
