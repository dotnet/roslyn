// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    #region Common Arguments

    /// <summary>
    /// Arguments to pass from client to server when performing operations
    /// </summary>
    internal class SerializableProjectId
    {
        public Guid Id;
        public string DebugName;

        public static SerializableProjectId Dehydrate(ProjectId id)
        {
            return new SerializableProjectId { Id = id.Id, DebugName = id.DebugName };
        }

        public ProjectId Rehydrate()
        {
            return ProjectId.CreateFromSerialized(Id, DebugName);
        }
    }

    internal class SerializableDocumentId
    {
        public SerializableProjectId ProjectId;
        public Guid Id;
        public string DebugName;

        public static SerializableDocumentId Dehydrate(Document document)
        {
            return Dehydrate(document.Id);
        }

        public static SerializableDocumentId Dehydrate(DocumentId id)
        {
            return new SerializableDocumentId
            {
                ProjectId = SerializableProjectId.Dehydrate(id.ProjectId),
                Id = id.Id,
                DebugName = id.DebugName
            };
        }

        public DocumentId Rehydrate()
        {
            return DocumentId.CreateFromSerialized(
                ProjectId.Rehydrate(), Id, DebugName);
        }
    }

    internal class SerializableTextSpan
    {
        public int Start;
        public int Length;

        public static SerializableTextSpan Dehydrate(TextSpan textSpan)
        {
            return new SerializableTextSpan { Start = textSpan.Start, Length = textSpan.Length };
        }

        public TextSpan Rehydrate()
        {
            return new TextSpan(Start, Length);
        }
    }

    #endregion

    #region FindReferences

    internal class SerializableSymbolAndProjectId
    {
        public string SymbolKeyData;
        public SerializableProjectId ProjectId;

        public static SerializableSymbolAndProjectId Dehydrate(
            IAliasSymbol alias, Document document)
        {
            return alias == null
                ? null
                : Dehydrate(new SymbolAndProjectId(alias, document.Project.Id));
        }

        public static SerializableSymbolAndProjectId Dehydrate(
            SymbolAndProjectId symbolAndProjectId)
        {
            return new SerializableSymbolAndProjectId
            {
                SymbolKeyData = symbolAndProjectId.Symbol.GetSymbolKey().ToString(),
                ProjectId = SerializableProjectId.Dehydrate(symbolAndProjectId.ProjectId)
            };
        }

        public async Task<SymbolAndProjectId> RehydrateAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var projectId = ProjectId.Rehydrate();
            var project = solution.GetProject(projectId);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbol = SymbolKey.Resolve(SymbolKeyData, compilation, cancellationToken: cancellationToken).GetAnySymbol();
            Debug.Assert(symbol != null, "We should always be able to resolve a symbol back on the host side.");
            return new SymbolAndProjectId(symbol, projectId);
        }
    }

    internal class SerializableReferenceLocation
    {
        public SerializableDocumentId Document { get; set; }

        public SerializableSymbolAndProjectId Alias { get; set; }

        public SerializableTextSpan Location { get; set; }

        public bool IsImplicit { get; set; }

        internal bool IsWrittenTo { get; set; }

        public CandidateReason CandidateReason { get; set; }

        public static SerializableReferenceLocation Dehydrate(
            ReferenceLocation referenceLocation)
        {
            return new SerializableReferenceLocation
            {
                Document = SerializableDocumentId.Dehydrate(referenceLocation.Document),
                Alias = SerializableSymbolAndProjectId.Dehydrate(referenceLocation.Alias, referenceLocation.Document),
                Location = SerializableTextSpan.Dehydrate(referenceLocation.Location.SourceSpan),
                IsImplicit = referenceLocation.IsImplicit,
                IsWrittenTo = referenceLocation.IsWrittenTo,
                CandidateReason = referenceLocation.CandidateReason
            };
        }

        public async Task<ReferenceLocation> RehydrateAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(this.Document.Rehydrate());
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var aliasSymbol = await RehydrateAliasAsync(solution, cancellationToken).ConfigureAwait(false);
            return new ReferenceLocation(
                document,
                aliasSymbol,
                CodeAnalysis.Location.Create(syntaxTree, Location.Rehydrate()),
                isImplicit: IsImplicit,
                isWrittenTo: IsWrittenTo,
                candidateReason: CandidateReason);
        }

        private async Task<IAliasSymbol> RehydrateAliasAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            if (Alias == null)
            {
                return null;
            }

            var symbolAndProjectId = await Alias.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
            return symbolAndProjectId.Symbol as IAliasSymbol;
        }
    }

    #endregion

    #region SymbolSearch

    internal class SerializablePackageWithTypeResult
    {
        public string PackageName;
        public string TypeName;
        public string Version;
        public int Rank;
        public string[] ContainingNamespaceNames;

        public static SerializablePackageWithTypeResult Dehydrate(PackageWithTypeResult result)
        {
            return new SerializablePackageWithTypeResult
            {
                PackageName = result.PackageName,
                TypeName = result.TypeName,
                Version = result.Version,
                Rank = result.Rank,
                ContainingNamespaceNames = result.ContainingNamespaceNames.ToArray(),
            };
        }

        public PackageWithTypeResult Rehydrate()
        {
            return new PackageWithTypeResult(
                PackageName, TypeName, Version, Rank, ContainingNamespaceNames);
        }
    }

    internal class SerializableReferenceAssemblyWithTypeResult
    {
        public string AssemblyName;
        public string TypeName;
        public string[] ContainingNamespaceNames;

        public static SerializableReferenceAssemblyWithTypeResult Dehydrate(
            ReferenceAssemblyWithTypeResult result)
        {
            return new SerializableReferenceAssemblyWithTypeResult
            {
                ContainingNamespaceNames = result.ContainingNamespaceNames.ToArray(),
                AssemblyName = result.AssemblyName,
                TypeName = result.TypeName
            };
        }

        public ReferenceAssemblyWithTypeResult Rehydrate()
        {
            return new ReferenceAssemblyWithTypeResult(AssemblyName, TypeName, ContainingNamespaceNames);
        }
    }

    #endregion
}