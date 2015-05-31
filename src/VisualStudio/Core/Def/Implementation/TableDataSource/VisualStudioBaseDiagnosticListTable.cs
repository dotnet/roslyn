// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract partial class VisualStudioBaseDiagnosticListTable : AbstractTable<DiagnosticsUpdatedArgs, DiagnosticData>
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
            StandardTableColumnDefinitions.DetailsExpander
        };

        protected VisualStudioBaseDiagnosticListTable(
            SVsServiceProvider serviceProvider, Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
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

        public static string GetHelpLink(DiagnosticData item)
        {
            Uri link;
            if (BrowserHelper.TryGetUri(item.HelpLink, out link))
            {
                return link.AbsoluteUri;
            }

            if (!string.IsNullOrWhiteSpace(item.Id))
            {
                return BrowserHelper.CreateBingQueryUri(item.Id, item.ENUMessageForBingSearch).AbsoluteUri;
            }

            return null;
        }

        public static string GetHelpLinkToolTipText(DiagnosticData item)
        {
            var isBing = false;
            Uri helpUri = null;
            if (!BrowserHelper.TryGetUri(item.HelpLink, out helpUri) && !string.IsNullOrWhiteSpace(item.Id))
            {
                helpUri = BrowserHelper.CreateBingQueryUri(item.Id, item.ENUMessageForBingSearch);
                isBing = true;
            }

            // We make sure not to use Uri.AbsoluteUri for the url displayed in the tooltip so that the url dislayed in the tooltip stays human readable.
            if (helpUri != null)
            {
                return string.Format(ServicesVSResources.DiagnosticIdHyperlinkTooltipText, item.Id,
                    isBing ? ServicesVSResources.FromBing : null, Environment.NewLine, helpUri);
            }

            return null;
        }
    }
}
