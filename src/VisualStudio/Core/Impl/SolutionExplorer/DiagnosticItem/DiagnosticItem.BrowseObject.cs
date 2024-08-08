// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class DiagnosticItem
    {
        internal class BrowseObject : LocalizableProperties
        {
            public BrowseObject(DiagnosticItem diagnosticItem)
            {
                DiagnosticItem = diagnosticItem;
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.ID))]
            public string Id
            {
                get
                {
                    return DiagnosticItem.Descriptor.Id;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Title))]
            public string Title
            {
                get
                {
                    return DiagnosticItem.Descriptor.Title.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Description))]
            public string Description
            {
                get
                {
                    return DiagnosticItem.Descriptor.Description.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Help_link))]
            public string? HelpLink
            {
                get
                {
                    return DiagnosticItem.Descriptor.GetValidHelpLinkUri()?.AbsoluteUri;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Category))]
            public string Category
            {
                get
                {
                    return DiagnosticItem.Descriptor.Category;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Default_severity))]
            public string DefaultSeverity
            {
                get
                {
                    return MapDiagnosticSeverityToText(DiagnosticItem.Descriptor.DefaultSeverity);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Enabled_by_default))]
            public bool EnabledByDefault
            {
                get
                {
                    return DiagnosticItem.Descriptor.IsEnabledByDefault;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Message))]
            public string Message
            {
                get
                {
                    return DiagnosticItem.Descriptor.MessageFormat.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Tags))]
            public string Tags
            {
                get
                {
                    return string.Join(" ", DiagnosticItem.Descriptor.CustomTags);
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Effective_severity))]
            public string EffectiveSeverity
                => MapReportDiagnosticToText(DiagnosticItem._effectiveSeverity);

            public override string GetClassName()
            {
                return SolutionExplorerShim.Diagnostic_Properties;
            }

            public override string GetComponentName()
            {
                return DiagnosticItem.Descriptor.Id;
            }

            [Browsable(false)]
            public DiagnosticItem DiagnosticItem { get; }

            private static string MapDiagnosticSeverityToText(DiagnosticSeverity severity)
                => severity switch
                {
                    DiagnosticSeverity.Hidden => SolutionExplorerShim.Hidden,
                    DiagnosticSeverity.Info => SolutionExplorerShim.Info,
                    DiagnosticSeverity.Warning => SolutionExplorerShim.Warning,
                    DiagnosticSeverity.Error => SolutionExplorerShim.Error_,
                    _ => throw ExceptionUtilities.UnexpectedValue(severity),
                };

            private static string MapReportDiagnosticToText(ReportDiagnostic report)
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
