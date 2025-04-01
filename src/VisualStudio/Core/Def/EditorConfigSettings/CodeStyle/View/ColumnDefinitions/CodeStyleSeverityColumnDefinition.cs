// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.ViewModel;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.CodeStyle;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View.ColumnDefinitions;

[Export(typeof(ITableColumnDefinition))]
[Name(Severity)]
internal sealed class CodeStyleSeverityColumnDefinition : TableColumnDefinitionBase
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
    public override bool IsFilterable => false;
    public override bool IsSortable => false;

    public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
    {
        if (!entry.TryGetValue(Severity, out CodeStyleSetting setting))
        {
            content = null;
            return false;
        }

        var viewModel = new CodeStyleSeverityViewModel(setting);
        var control = new CodeStyleSeverityControl(viewModel);
        content = control;
        return true;
    }
}
