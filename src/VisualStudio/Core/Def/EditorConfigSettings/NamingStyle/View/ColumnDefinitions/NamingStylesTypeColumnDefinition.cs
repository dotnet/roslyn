// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.View.ColumnDefinitions;

using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.NamingStyle;

[Export(typeof(ITableColumnDefinition))]
[Name(Type)]
internal sealed class NamingStylesTypeColumnDefinition : TableColumnDefinitionBase
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NamingStylesTypeColumnDefinition()
    {
    }

    public override string Name => Type;
    public override string DisplayName => ServicesVSResources.Type;
    public override bool IsFilterable => true;
    public override bool IsSortable => true;
    public override double MinWidth => 350;

    public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
    {
        if (!entry.TryGetValue(Type, out NamingStyleSetting setting))
        {
            content = null;
            return false;
        }

        content = setting.TypeName;
        return true;
    }

    public override bool TryCreateColumnContent(
        ITableEntryHandle entry,
        bool singleColumnView,
        out FrameworkElement? content)
    {
        if (!entry.TryGetValue(Type, out NamingStyleSetting setting))
        {
            content = null;
            return false;
        }

        var viewModel = new NamingStylesTypeViewModel(setting);
        var control = new NamingStylesTypeControl(viewModel);
        content = control;
        return true;
    }
}
