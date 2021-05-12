// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.Analyzer;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.View.ColumnDefinitions
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(Severity)]
    internal class AnalyzerSeverityColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerSeverityColumnDefinition()
        {
        }

        public override string Name => Severity;
        public override string DisplayName => ServicesVSResources.Severity;
        public override bool IsFilterable => false;
        public override bool IsSortable => false;
        public override double MinWidth => 120;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
        {
            if (!entry.TryGetValue(Severity, out AnalyzerSetting setting))
            {
                content = null;
                return false;
            }

            content = setting.Severity switch
            {
                CodeAnalysis.DiagnosticSeverity.Hidden => ServicesVSResources.Disabled,
                CodeAnalysis.DiagnosticSeverity.Info => ServicesVSResources.Suggestion,
                CodeAnalysis.DiagnosticSeverity.Warning => ServicesVSResources.Warning,
                CodeAnalysis.DiagnosticSeverity.Error => ServicesVSResources.Error,
                _ => string.Empty,
            };
            return true;
        }

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (!entry.TryGetValue(Severity, out AnalyzerSetting severity))
            {
                content = null;
                return false;
            }

            var control = new SeverityControl(severity);
            content = control;
            return true;
        }
    }
}
