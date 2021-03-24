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
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.CodeStyle;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View.ColumnDefinitions
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(Severity)]
    internal class CodeStyleSeverityColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeStyleSeverityColumnDefinition()
        {
        }

        public override string Name => Severity;
        public override string DisplayName => ServicesVSResources.Severity;
        public override double MinWidth => 120;
        public override bool DefaultVisible => false;
        public override bool IsFilterable => true;
        public override bool IsSortable => true;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
        {
            if (!entry.TryGetValue(Severity, out CodeStyleSetting setting))
            {
                content = null;
                return false;
            }

            content = setting.Severity switch
            {
                CodeAnalysis.ReportDiagnostic.Error => ServicesVSResources.Error,
                CodeAnalysis.ReportDiagnostic.Warn => ServicesVSResources.Warning,
                CodeAnalysis.ReportDiagnostic.Info => ServicesVSResources.Suggestion,
                CodeAnalysis.ReportDiagnostic.Hidden => ServicesVSResources.Refactoring_Only,
                CodeAnalysis.ReportDiagnostic.Default => string.Empty,
                CodeAnalysis.ReportDiagnostic.Suppress => string.Empty,
                _ => string.Empty,
            };
            return true;
        }

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (!entry.TryGetValue(Severity, out CodeStyleSetting setting))
            {
                content = null;
                return false;
            }

            var control = new CodeStyleSeverityControl(setting);
            content = control;
            return true;
        }
    }
}
