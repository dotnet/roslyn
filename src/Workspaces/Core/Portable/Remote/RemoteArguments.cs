﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    #region FindReferences

    internal class SerializableSymbolAndProjectId : IEquatable<SerializableSymbolAndProjectId>
    {
        public string SymbolKeyData;
        public ProjectId ProjectId;

        public override int GetHashCode()
            => Hash.Combine(SymbolKeyData, ProjectId.GetHashCode());

        public override bool Equals(object obj)
            => Equals(obj as SerializableSymbolAndProjectId);

        public bool Equals(SerializableSymbolAndProjectId other)
            => other != null && SymbolKeyData.Equals(other.SymbolKeyData) && ProjectId.Equals(other.ProjectId);

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
                ProjectId = symbolAndProjectId.ProjectId
            };
        }

        public async Task<SymbolAndProjectId?> TryRehydrateAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var projectId = ProjectId;
            var project = solution.GetProject(projectId);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // The server and client should both be talking about the same compilation.  As such
            // locations in symbols are save to resolve as we rehydrate the SymbolKey.
            var symbol = SymbolKey.Resolve(
                SymbolKeyData, compilation, resolveLocations: true, cancellationToken: cancellationToken).GetAnySymbol();

            if (symbol == null)
            {
                try
                {
                    throw new InvalidOperationException(
                        $"We should always be able to resolve a symbol back on the host side:\r\n{SymbolKeyData}");
                }
                catch (Exception ex) when (FatalError.ReportWithoutCrash(ex))
                {
                    return null;
                }
            }

            return new SymbolAndProjectId(symbol, projectId);
        }
    }

    internal class SerializableReferenceLocation
    {
        public DocumentId Document { get; set; }

        public SerializableSymbolAndProjectId Alias { get; set; }

        public TextSpan Location { get; set; }

        public bool IsImplicit { get; set; }

        internal bool IsWrittenTo { get; set; }

        public CandidateReason CandidateReason { get; set; }

        public static SerializableReferenceLocation Dehydrate(
            ReferenceLocation referenceLocation)
        {
            return new SerializableReferenceLocation
            {
                Document = referenceLocation.Document.Id,
                Alias = SerializableSymbolAndProjectId.Dehydrate(referenceLocation.Alias, referenceLocation.Document),
                Location = referenceLocation.Location.SourceSpan,
                IsImplicit = referenceLocation.IsImplicit,
                IsWrittenTo = referenceLocation.IsWrittenTo,
                CandidateReason = referenceLocation.CandidateReason
            };
        }

        public async Task<ReferenceLocation> RehydrateAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(this.Document);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var aliasSymbol = await RehydrateAliasAsync(solution, cancellationToken).ConfigureAwait(false);
            return new ReferenceLocation(
                document,
                aliasSymbol,
                CodeAnalysis.Location.Create(syntaxTree, Location),
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

            var symbolAndProjectId = await Alias.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
            return symbolAndProjectId.GetValueOrDefault().Symbol as IAliasSymbol;
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

    internal class SerializablePackageWithAssemblyResult
    {
        public string PackageName;
        public string Version;
        public int Rank;

        public static SerializablePackageWithAssemblyResult Dehydrate(PackageWithAssemblyResult result)
        {
            return new SerializablePackageWithAssemblyResult
            {
                PackageName = result.PackageName,
                Version = result.Version,
                Rank = result.Rank,
            };
        }

        public PackageWithAssemblyResult Rehydrate()
            => new PackageWithAssemblyResult(PackageName, Version, Rank);
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