// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract partial class VisualStudioBaseDiagnosticListTable : AbstractTable
    {
        private static readonly string[] s_columns = new string[]
        {
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
            StandardTableColumnDefinitions.SuppressionState
        };

        protected VisualStudioBaseDiagnosticListTable(Workspace workspace, ITableManagerProvider provider) :
            base(workspace, provider, StandardTables.ErrorsTable)
        {
        }

        internal override IReadOnlyCollection<string> Columns => s_columns;

        public static __VSERRORCATEGORY GetErrorCategory(DiagnosticSeverity severity)
        {
            // REVIEW: why is it using old interface for new API?
            return severity switch
            {
                DiagnosticSeverity.Error => __VSERRORCATEGORY.EC_ERROR,
                DiagnosticSeverity.Warning => __VSERRORCATEGORY.EC_WARNING,
                DiagnosticSeverity.Info => __VSERRORCATEGORY.EC_MESSAGE,
                _ => Contract.FailWithReturn<__VSERRORCATEGORY>(),
            };
        }

        protected abstract class DiagnosticTableEntriesSource : AbstractTableEntriesSource<DiagnosticTableItem>
        {
            public abstract string BuildTool { get; }
            public abstract bool SupportSpanTracking { get; }
            public abstract DocumentId TrackingDocumentId { get; }
        }

        protected class AggregatedKey
        {
            public readonly ImmutableArray<DocumentId> DocumentIds;
            public readonly DiagnosticAnalyzer Analyzer;
            public readonly int Kind;

            public AggregatedKey(ImmutableArray<DocumentId> documentIds, DiagnosticAnalyzer analyzer, int kind)
            {
                DocumentIds = documentIds;
                Analyzer = analyzer;
                Kind = kind;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is AggregatedKey other))
                {
                    return false;
                }

                return this.DocumentIds == other.DocumentIds && this.Analyzer == other.Analyzer && this.Kind == other.Kind;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(Analyzer.GetHashCode(), Hash.Combine(DocumentIds.GetHashCode(), Kind));
            }
        }
    }
}
