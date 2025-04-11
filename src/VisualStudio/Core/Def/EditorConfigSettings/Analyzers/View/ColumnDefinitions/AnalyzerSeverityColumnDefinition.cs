// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.Analyzer;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.View.ColumnDefinitions;

[Export(typeof(ITableColumnDefinition))]
[Name(Severity)]
internal sealed class AnalyzerSeverityColumnDefinition : TableColumnDefinitionBase
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
    public override double MinWidth => 150;

    public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
    {
        if (!entry.TryGetValue(Severity, out AnalyzerSetting setting))
        {
            content = null;
            return false;
        }

        content = setting.Severity switch
        {
            ReportDiagnostic.Suppress => ServicesVSResources.Disabled,
            ReportDiagnostic.Hidden => WorkspacesResources.Refactoring_Only,
            ReportDiagnostic.Info => WorkspacesResources.Suggestion,
            ReportDiagnostic.Warn => WorkspacesResources.Warning,
            ReportDiagnostic.Error => WorkspacesResources.Error,
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

        var viewModel = new SeverityViewModel(severity);
        var control = new SeverityControl(viewModel);
        content = control;
        return true;
    }
}
