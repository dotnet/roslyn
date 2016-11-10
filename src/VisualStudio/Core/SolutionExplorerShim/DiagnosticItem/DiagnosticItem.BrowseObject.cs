// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class DiagnosticItem : BaseItem
    {
        internal class BrowseObject : LocalizableProperties
        {
            private DiagnosticItem _diagnosticItem;

            public BrowseObject(DiagnosticItem diagnosticItem)
            {
                _diagnosticItem = diagnosticItem;
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.ID))]
            public string Id
            {
                get
                {
                    return _diagnosticItem.Descriptor.Id;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Title))]
            public string Title
            {
                get
                {
                    return _diagnosticItem.Descriptor.Title.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Description))]
            public string Description
            {
                get
                {
                    return _diagnosticItem.Descriptor.Description.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Help_link))]
            public string HelpLink
            {
                get
                {
                    return _diagnosticItem.GetHelpLink()?.ToString();
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Category))]
            public string Category
            {
                get
                {
                    return _diagnosticItem.Descriptor.Category;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Default_severity))]
            public string DefaultSeverity
            {
                get
                {
                    return MapDiagnosticSeverityToText(_diagnosticItem.Descriptor.DefaultSeverity);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Enabled_by_default))]
            public bool EnabledByDefault
            {
                get
                {
                    return _diagnosticItem.Descriptor.IsEnabledByDefault;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Message))]
            public string Message
            {
                get
                {
                    return _diagnosticItem.Descriptor.MessageFormat.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Tags))]
            public string Tags
            {
                get
                {
                    return string.Join(" ", _diagnosticItem.Descriptor.CustomTags);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Effective_severity))]
            public string EffectiveSeverity
            {
                get
                {
                    return MapReportDiagnosticToText(_diagnosticItem.EffectiveSeverity);
                }
            }

            public override string GetClassName()
            {
                return SolutionExplorerShim.Diagnostic_Properties;
            }

            public override string GetComponentName()
            {
                return _diagnosticItem.Descriptor.Id;
            }

            [Browsable(false)]
            public DiagnosticItem DiagnosticItem
            {
                get { return _diagnosticItem; }
            }

            private string MapDiagnosticSeverityToText(DiagnosticSeverity severity)
            {
                switch (severity)
                {
                    case DiagnosticSeverity.Hidden:
                        return SolutionExplorerShim.Hidden;
                    case DiagnosticSeverity.Info:
                        return SolutionExplorerShim.Info;
                    case DiagnosticSeverity.Warning:
                        return SolutionExplorerShim.Warning;
                    case DiagnosticSeverity.Error:
                        return SolutionExplorerShim.Error_;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(severity);
                }
            }

            private string MapReportDiagnosticToText(ReportDiagnostic report)
            {
                switch (report)
                {
                    case ReportDiagnostic.Default:
                        return SolutionExplorerShim.Default_;
                    case ReportDiagnostic.Error:
                        return SolutionExplorerShim.Error_;
                    case ReportDiagnostic.Warn:
                        return SolutionExplorerShim.Warning;
                    case ReportDiagnostic.Info:
                        return SolutionExplorerShim.Info;
                    case ReportDiagnostic.Hidden:
                        return SolutionExplorerShim.Hidden;
                    case ReportDiagnostic.Suppress:
                        return SolutionExplorerShim.Suppressed;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(report);
                }
            }
        }
    }
}
