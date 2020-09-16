// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    #region FindReferences

    internal class SerializableFindReferencesSearchOptions
    {
        public bool AssociatePropertyReferencesWithSpecificAccessor;
        public bool Cascade;

        public static SerializableFindReferencesSearchOptions Dehydrate(FindReferencesSearchOptions options)
        {
            return new SerializableFindReferencesSearchOptions
            {
                AssociatePropertyReferencesWithSpecificAccessor = options.AssociatePropertyReferencesWithSpecificAccessor,
                Cascade = options.Cascade,
            };
        }

        public FindReferencesSearchOptions Rehydrate()
        {
            return new FindReferencesSearchOptions(
                associatePropertyReferencesWithSpecificAccessor: AssociatePropertyReferencesWithSpecificAccessor,
                cascade: Cascade);
        }
    }

    internal class SerializableSymbolAndProjectId
    {
        public string SymbolKeyData;
        public ProjectId ProjectId;

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
            => new SerializableSymbolAndProjectId
            {
                SymbolKeyData = symbol.GetSymbolKey(cancellationToken).ToString(),
                ProjectId = project.Id,
            };

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

            result = new SerializableSymbolAndProjectId
            {
                SymbolKeyData = SymbolKey.CreateString(symbol, cancellationToken),
                ProjectId = project.Id,
            };
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
                catch (Exception ex) when (FatalError.ReportWithoutCrash(ex))
                {
                    return null;
                }
            }

            return symbol;
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
            => Equals(obj as SerializableSymbolUsageInfo);

        public override int GetHashCode()
            => Hash.Combine(IsValueUsageInfo.GetHashCode(), UsageInfoUnderlyingValue.GetHashCode());
    }

    internal class SerializableReferenceLocation
    {
        public DocumentId Document { get; set; }

        public SerializableSymbolAndProjectId Alias { get; set; }

        public TextSpan Location { get; set; }

        public bool IsImplicit { get; set; }

        public SerializableSymbolUsageInfo SymbolUsageInfo { get; set; }

        public ImmutableDictionary<string, string> AdditionalProperties { get; set; }

        public CandidateReason CandidateReason { get; set; }

        public static SerializableReferenceLocation Dehydrate(
            ReferenceLocation referenceLocation, CancellationToken cancellationToken)
        {
            return new SerializableReferenceLocation
            {
                Document = referenceLocation.Document.Id,
                Alias = SerializableSymbolAndProjectId.Dehydrate(referenceLocation.Alias, referenceLocation.Document, cancellationToken),
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

    #endregion
}
