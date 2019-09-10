// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    #region FindReferences

    internal class SerializableFindReferencesSearchOptions
    {
        public bool AssociatePropertyReferencesWithSpecificAccessor;

        public static SerializableFindReferencesSearchOptions Dehydrate(FindReferencesSearchOptions options)
        {
            return new SerializableFindReferencesSearchOptions
            {
                AssociatePropertyReferencesWithSpecificAccessor = options.AssociatePropertyReferencesWithSpecificAccessor
            };
        }

        public FindReferencesSearchOptions Rehydrate()
        {
            return new FindReferencesSearchOptions(AssociatePropertyReferencesWithSpecificAccessor);
        }
    }

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
            var symbol = SymbolKey.ResolveString(
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

    internal class SerializableSymbolUsageInfo : IEquatable<SerializableSymbolUsageInfo>
    {
        public bool IsValueUsageInfo;
        public int UsageInfoUnderlyingValue;

        public static SerializableSymbolUsageInfo Dehydrate(SymbolUsageInfo symbolUsageInfo)
        {
            bool isValueUsageInfo;
            int usageInfoUnderlyingValue;
            if (symbolUsageInfo.ValueUsageInfoOpt.HasValue)
            {
                isValueUsageInfo = true;
                usageInfoUnderlyingValue = (int)symbolUsageInfo.ValueUsageInfoOpt.Value;
            }
            else
            {
                isValueUsageInfo = false;
                usageInfoUnderlyingValue = (int)symbolUsageInfo.TypeOrNamespaceUsageInfoOpt.Value;
            }

            return new SerializableSymbolUsageInfo
            {
                IsValueUsageInfo = isValueUsageInfo,
                UsageInfoUnderlyingValue = usageInfoUnderlyingValue
            };
        }

        public SymbolUsageInfo Rehydrate()
        {
            return IsValueUsageInfo
                ? SymbolUsageInfo.Create((ValueUsageInfo)UsageInfoUnderlyingValue)
                : SymbolUsageInfo.Create((TypeOrNamespaceUsageInfo)UsageInfoUnderlyingValue);
        }

        public bool Equals(SerializableSymbolUsageInfo other)
        {
            return other != null &&
                IsValueUsageInfo == other.IsValueUsageInfo &&
                UsageInfoUnderlyingValue == other.UsageInfoUnderlyingValue;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SerializableSymbolUsageInfo);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(IsValueUsageInfo.GetHashCode(), UsageInfoUnderlyingValue.GetHashCode());
        }
    }

    internal class SerializableReferenceLocation
    {
        public DocumentId Document { get; set; }

        public SerializableSymbolAndProjectId Alias { get; set; }

        public TextSpan Location { get; set; }

        public bool IsImplicit { get; set; }

        public SerializableSymbolUsageInfo SymbolUsageInfo { get; set; }

        public ImmutableArray<FindUsageProperty> AdditionalProperties { get; set; }

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
                SymbolUsageInfo = SerializableSymbolUsageInfo.Dehydrate(referenceLocation.SymbolUsageInfo),
                AdditionalProperties = referenceLocation.AdditionalProperties,
                CandidateReason = referenceLocation.CandidateReason
            };
        }

        public async Task<ReferenceLocation> RehydrateAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(this.Document);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var aliasSymbol = await RehydrateAliasAsync(solution, cancellationToken).ConfigureAwait(false);
            var additionalProperties = this.AdditionalProperties;
            return new ReferenceLocation(
                document,
                aliasSymbol,
                CodeAnalysis.Location.Create(syntaxTree, Location),
                isImplicit: IsImplicit,
                symbolUsageInfo: SymbolUsageInfo.Rehydrate(),
                additionalProperties: additionalProperties.NullToEmpty(),
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
}
