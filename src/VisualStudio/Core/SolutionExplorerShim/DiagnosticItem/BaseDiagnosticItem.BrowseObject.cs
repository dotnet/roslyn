// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal abstract partial class BaseDiagnosticItem
    {
        internal class BrowseObject : LocalizableProperties
        {
            private readonly BaseDiagnosticItem _diagnosticItem;

            public BrowseObject(BaseDiagnosticItem diagnosticItem)
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
                    return _diagnosticItem.GetHelpLink()?.AbsoluteUri;
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
            public BaseDiagnosticItem DiagnosticItem
            {
                get { return _diagnosticItem; }
            }

            private string MapDiagnosticSeverityToText(DiagnosticSeverity severity)
                => severity switch
                {
                    DiagnosticSeverity.Hidden => SolutionExplorerShim.Hidden,
                    DiagnosticSeverity.Info => SolutionExplorerShim.Info,
                    DiagnosticSeverity.Warning => SolutionExplorerShim.Warning,
                    DiagnosticSeverity.Error => SolutionExplorerShim.Error_,
                    _ => throw ExceptionUtilities.UnexpectedValue(severity),
                };

            private string MapReportDiagnosticToText(ReportDiagnostic report)
                => report switch
                {
                    ReportDiagnostic.Default => SolutionExplorerShim.Default_,
                    ReportDiagnostic.Error => SolutionExplorerShim.Error_,
                    ReportDiagnostic.Warn => SolutionExplorerShim.Warning,
                    ReportDiagnostic.Info => SolutionExplorerShim.Info,
                    ReportDiagnostic.Hidden => SolutionExplorerShim.Hidden,
                    ReportDiagnostic.Suppress => SolutionExplorerShim.Suppressed,
                    _ => throw ExceptionUtilities.UnexpectedValue(report),
                };
        }
    }
}
