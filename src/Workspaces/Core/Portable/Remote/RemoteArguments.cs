﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    #region FindReferences

    [DataContract]
    internal sealed class SerializableSymbolAndProjectId : IEquatable<SerializableSymbolAndProjectId>
    {
        [DataMember(Order = 0)]
        public readonly string SymbolKeyData;

        [DataMember(Order = 1)]
        public readonly ProjectId ProjectId;

        public SerializableSymbolAndProjectId(string symbolKeyData, ProjectId projectId)
        {
            SymbolKeyData = symbolKeyData;
            ProjectId = projectId;
        }

        public override bool Equals(object obj)
            => Equals(obj as SerializableSymbolAndProjectId);

        public bool Equals(SerializableSymbolAndProjectId other)
        {
            if (this == other)
                return true;

            return this.ProjectId == other?.ProjectId &&
                   this.SymbolKeyData == other?.SymbolKeyData;
        }

        public override int GetHashCode()
            => Hash.Combine(this.SymbolKeyData, this.ProjectId.GetHashCode());

        public static SerializableSymbolAndProjectId Dehydrate(
            IAliasSymbol alias, Document document, CancellationToken cancellationToken)
        {
            return alias == null
                ? null
                : Dehydrate(document.Project.Solution, alias, cancellationToken);
        }

        public static SerializableSymbolAndProjectId Dehydrate(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var project = solution.GetOriginatingProject(symbol);
            Contract.ThrowIfNull(project, WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution);

            return Create(symbol, project, cancellationToken);
        }

        public static SerializableSymbolAndProjectId Create(ISymbol symbol, Project project, CancellationToken cancellationToken)
            => new(symbol.GetSymbolKey(cancellationToken).ToString(), project.Id);

        public static bool TryCreate(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken,
            out SerializableSymbolAndProjectId result)
        {
            var project = solution.GetOriginatingProject(symbol);
            if (project == null)
            {
                result = null;
                return false;
            }

            return TryCreate(symbol, project, cancellationToken, out result);
        }

        public static bool TryCreate(
            ISymbol symbol, Project project, CancellationToken cancellationToken,
            out SerializableSymbolAndProjectId result)
        {
            if (!SymbolKey.CanCreate(symbol, cancellationToken))
            {
                result = null;
                return false;
            }

            result = new SerializableSymbolAndProjectId(SymbolKey.CreateString(symbol, cancellationToken), project.Id);
            return true;
        }

        public async Task<ISymbol> TryRehydrateAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var projectId = ProjectId;
            var project = solution.GetProject(projectId);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // The server and client should both be talking about the same compilation.  As such
            // locations in symbols are save to resolve as we rehydrate the SymbolKey.
            var symbol = SymbolKey.ResolveString(
                SymbolKeyData, compilation, out var failureReason, cancellationToken).GetAnySymbol();

            if (symbol == null)
            {
                try
                {
                    throw new InvalidOperationException(
                        $"We should always be able to resolve a symbol back on the host side:\r\n{project.Name}\r\n{SymbolKeyData}\r\n{failureReason}");
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                {
                    return null;
                }
            }

            return symbol;
        }
    }

    [DataContract]
    internal readonly struct SerializableReferenceLocation
    {
        [DataMember(Order = 0)]
        public readonly DocumentId Document;

        [DataMember(Order = 1)]
        public readonly SerializableSymbolAndProjectId Alias;

        [DataMember(Order = 2)]
        public readonly TextSpan Location;

        [DataMember(Order = 3)]
        public readonly bool IsImplicit;

        [DataMember(Order = 4)]
        public readonly SymbolUsageInfo SymbolUsageInfo;

        [DataMember(Order = 5)]
        public readonly ImmutableDictionary<string, string> AdditionalProperties;

        [DataMember(Order = 6)]
        public readonly CandidateReason CandidateReason;

        public SerializableReferenceLocation(
            DocumentId document,
            SerializableSymbolAndProjectId alias,
            TextSpan location,
            bool isImplicit,
            SymbolUsageInfo symbolUsageInfo,
            ImmutableDictionary<string, string> additionalProperties,
            CandidateReason candidateReason)
        {
            Document = document;
            Alias = alias;
            Location = location;
            IsImplicit = isImplicit;
            SymbolUsageInfo = symbolUsageInfo;
            AdditionalProperties = additionalProperties;
            CandidateReason = candidateReason;
        }

        public static SerializableReferenceLocation Dehydrate(
            ReferenceLocation referenceLocation, CancellationToken cancellationToken)
        {
            return new SerializableReferenceLocation(
                referenceLocation.Document.Id,
                SerializableSymbolAndProjectId.Dehydrate(referenceLocation.Alias, referenceLocation.Document, cancellationToken),
                referenceLocation.Location.SourceSpan,
                referenceLocation.IsImplicit,
                referenceLocation.SymbolUsageInfo,
                referenceLocation.AdditionalProperties,
                referenceLocation.CandidateReason);
        }

        public async Task<ReferenceLocation> RehydrateAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var document = await solution.GetDocumentAsync(this.Document, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var aliasSymbol = await RehydrateAliasAsync(solution, cancellationToken).ConfigureAwait(false);
            var additionalProperties = this.AdditionalProperties;
            return new ReferenceLocation(
                document,
                aliasSymbol,
                CodeAnalysis.Location.Create(syntaxTree, Location),
                isImplicit: IsImplicit,
                symbolUsageInfo: SymbolUsageInfo,
                additionalProperties: additionalProperties ?? ImmutableDictionary<string, string>.Empty,
                candidateReason: CandidateReason);
        }

        private async Task<IAliasSymbol> RehydrateAliasAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            if (Alias == null)
                return null;

            var symbol = await Alias.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
            return symbol as IAliasSymbol;
        }
    }

    [DataContract]
    internal class SerializableSymbolGroup : IEquatable<SerializableSymbolGroup>
    {
        [DataMember(Order = 0)]
        public readonly HashSet<SerializableSymbolAndProjectId> Symbols;

        private int _hashCode;

        public SerializableSymbolGroup(HashSet<SerializableSymbolAndProjectId> symbols)
        {
            Symbols = new HashSet<SerializableSymbolAndProjectId>(symbols);
        }

        public override bool Equals(object obj)
            => obj is SerializableSymbolGroup group && Equals(group);

        public bool Equals(SerializableSymbolGroup other)
        {
            if (this == other)
                return true;

            return other != null && this.Symbols.SetEquals(other.Symbols);
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                var hashCode = 0;
                foreach (var symbol in Symbols)
                    hashCode += symbol.SymbolKeyData.GetHashCode();
                _hashCode = hashCode;
            }

            return _hashCode;
        }

        public static SerializableSymbolGroup Dehydrate(Solution solution, SymbolGroup group, CancellationToken cancellationToken)
        {
            return new SerializableSymbolGroup(new HashSet<SerializableSymbolAndProjectId>(
                group.Symbols.Select(s => SerializableSymbolAndProjectId.Dehydrate(solution, s, cancellationToken))));
        }
    }

    #endregion
}
