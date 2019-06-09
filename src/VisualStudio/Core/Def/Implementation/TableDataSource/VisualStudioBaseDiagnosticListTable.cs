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
            SuppressionStateColumnDefinition.ColumnName
        };

        protected VisualStudioBaseDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            base(workspace, provider, StandardTables.ErrorsTable)
        {
        }

        internal override IReadOnlyCollection<string> Columns => s_columns;

        public static __VSERRORCATEGORY GetErrorCategory(DiagnosticSeverity severity)
        {
            // REVIEW: why is it using old interface for new API?
            switch (severity)
            {
                case DiagnosticSeverity.Error:
                    return __VSERRORCATEGORY.EC_ERROR;
                case DiagnosticSeverity.Warning:
                    return __VSERRORCATEGORY.EC_WARNING;
                case DiagnosticSeverity.Info:
                    return __VSERRORCATEGORY.EC_MESSAGE;
                default:
                    return Contract.FailWithReturn<__VSERRORCATEGORY>();
            }
        }

        public static string GetHelpLink(Workspace workspace, DiagnosticData data)
        {
            if (BrowserHelper.TryGetUri(data.HelpLink, out var link))
            {
                return link.AbsoluteUri;
            }

            if (!string.IsNullOrWhiteSpace(data.Id))
            {
                return BrowserHelper.CreateBingQueryUri(workspace, data).AbsoluteUri;
            }

            return null;
        }

        public static string GetHelpLinkToolTipText(Workspace workspace, DiagnosticData item)
        {
            var isBing = false;
            if (!BrowserHelper.TryGetUri(item.HelpLink, out var helpUri) && !string.IsNullOrWhiteSpace(item.Id))
            {
                helpUri = BrowserHelper.CreateBingQueryUri(workspace, item);
                isBing = true;
            }

            // We make sure not to use Uri.AbsoluteUri for the url displayed in the tooltip so that the url displayed in the tooltip stays human readable.
            if (helpUri != null)
            {
                var prefix = isBing
                    ? string.Format(ServicesVSResources.Get_help_for_0_from_Bing, item.Id)
                    : string.Format(ServicesVSResources.Get_help_for_0, item.Id);

                return $"{prefix}\r\n{helpUri}";
            }

            return null;
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
                var other = obj as AggregatedKey;
                if (other == null)
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
