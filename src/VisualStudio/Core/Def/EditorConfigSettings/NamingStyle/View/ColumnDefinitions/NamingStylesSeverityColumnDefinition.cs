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

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.View.ColumnDefinitions
{
    using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.NamingStyle;

    [Export(typeof(ITableColumnDefinition))]
    [Name(Severity)]
    internal class NamingStylesSeverityColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NamingStylesSeverityColumnDefinition()
        {
        }

        public override string Name => Severity;
        public override string DisplayName => ServicesVSResources.Severity;
        public override bool IsFilterable => false;
        public override bool IsSortable => false;
        public override double MinWidth => 120;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (!entry.TryGetValue(Severity, out NamingStyleSetting setting))
            {
                content = null;
                return false;
            }

            var viewModel = new NamingStylesSeverityViewModel(setting);
            var control = new NamingStylesSeverityControl(viewModel);
            content = control;
            return true;
        }
    }
}
