// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract partial class VisualStudioBaseDiagnosticListTable : AbstractTable
    {
        protected VisualStudioBaseDiagnosticListTable(Workspace workspace, ITableManagerProvider provider)
            : base(workspace, provider, StandardTables.ErrorsTable)
        {
        }

        internal override ImmutableArray<string> Columns { get; } = ImmutableArray.Create(
            StandardTableColumnDefinitions.ErrorSeverity,
            StandardTableColumnDefinitions.ErrorCode,
            StandardTableColumnDefinitions.Text,
            StandardTableColumnDefinitions.ErrorCategory,
            StandardTableColumnDefinitions.ProjectName,
            StandardTableColumnDefinitions.DocumentName,
            StandardTableColumnDefinitions.Line,
            StandardTableColumnDefinitions.Column,
            StandardTableColumnDefinitions.BuildTool,
            StandardTableColumnDefinitions.ErrorSource,
            StandardTableColumnDefinitions.DetailsExpander,
            StandardTableColumnDefinitions.SuppressionState);

        protected static __VSERRORCATEGORY GetErrorCategory(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => __VSERRORCATEGORY.EC_ERROR,
                DiagnosticSeverity.Warning => __VSERRORCATEGORY.EC_WARNING,
                DiagnosticSeverity.Info => __VSERRORCATEGORY.EC_MESSAGE,
                _ => throw ExceptionUtilities.UnexpectedValue(severity)
            };
        }

        protected abstract class DiagnosticTableEntriesSource : AbstractTableEntriesSource<DiagnosticTableItem>
        {
            public abstract string BuildTool { get; }
            [MemberNotNullWhen(true, nameof(TrackingDocumentId))]
            public abstract bool SupportSpanTracking { get; }
            public abstract DocumentId? TrackingDocumentId { get; }
        }

        protected class AggregatedKey
        {
            public readonly ImmutableArray<DocumentId> DocumentIds;
            public readonly DiagnosticAnalyzer Analyzer;
            public readonly AnalysisKind Kind;

            public AggregatedKey(ImmutableArray<DocumentId> documentIds, DiagnosticAnalyzer analyzer, AnalysisKind kind)
            {
                DocumentIds = documentIds;
                Analyzer = analyzer;
                Kind = kind;
            }

            public override bool Equals(object? obj)
            {
                if (obj is not AggregatedKey other)
                {
                    return false;
                }

                return this.DocumentIds == other.DocumentIds && this.Analyzer == other.Analyzer && this.Kind == other.Kind;
            }

            public override int GetHashCode()
                => Hash.Combine(Analyzer.GetHashCode(), Hash.Combine(DocumentIds.GetHashCode(), (int)Kind));
        }
    }
}
