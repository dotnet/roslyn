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

            [DisplayName("ID")]
            public string Id
            {
                get
                {
                    return _diagnosticItem.Descriptor.Id;
                }
            }

            [DisplayName("Title")]
            public string Title
            {
                get
                {
                    return _diagnosticItem.Descriptor.Title.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [DisplayName("Description")]
            public string Description
            {
                get
                {
                    return _diagnosticItem.Descriptor.Description.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [DisplayName("Help link")]
            public string HelpLink
            {
                get
                {
                    return _diagnosticItem.GetHelpLink()?.ToString();
                }
            }

            [DisplayName("Category")]
            public string Category
            {
                get
                {
                    return _diagnosticItem.Descriptor.Category;
                }
            }

            [DisplayName("Default severity")]
            public string DefaultSeverity
            {
                get
                {
                    return MapDiagnosticSeverityToText(_diagnosticItem.Descriptor.DefaultSeverity);
                }
            }

            [DisplayName("Enabled by default")]
            public bool EnabledByDefault
            {
                get
                {
                    return _diagnosticItem.Descriptor.IsEnabledByDefault;
                }
            }

            [DisplayName("Message")]
            public string Message
            {
                get
                {
                    return _diagnosticItem.Descriptor.MessageFormat.ToString(CultureInfo.CurrentUICulture);
                }
            }

            [DisplayName("Tags")]
            public string Tags
            {
                get
                {
                    return string.Join(" ", _diagnosticItem.Descriptor.CustomTags);
                }
            }

            [DisplayName("Effective severity")]
            public string EffectiveSeverity
            {
                get
                {
                    return MapReportDiagnosticToText(_diagnosticItem.EffectiveSeverity);
                }
            }

            public override string GetClassName()
            {
                return SolutionExplorerShim.DiagnosticItem_PropertyWindowClassName;
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
                        return SolutionExplorerShim.Severity_Hidden;
                    case DiagnosticSeverity.Info:
                        return SolutionExplorerShim.Severity_Info;
                    case DiagnosticSeverity.Warning:
                        return SolutionExplorerShim.Severity_Warning;
                    case DiagnosticSeverity.Error:
                        return SolutionExplorerShim.Severity_Error;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(severity);
                }
            }

            private string MapReportDiagnosticToText(ReportDiagnostic report)
            {
                switch (report)
                {
                    case ReportDiagnostic.Default:
                        return SolutionExplorerShim.Severity_Default;
                    case ReportDiagnostic.Error:
                        return SolutionExplorerShim.Severity_Error;
                    case ReportDiagnostic.Warn:
                        return SolutionExplorerShim.Severity_Warning;
                    case ReportDiagnostic.Info:
                        return SolutionExplorerShim.Severity_Info;
                    case ReportDiagnostic.Hidden:
                        return SolutionExplorerShim.Severity_Hidden;
                    case ReportDiagnostic.Suppress:
                        return SolutionExplorerShim.Severity_Suppressed;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(report);
                }
            }
        }
    }
}
