// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.Analyzer;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.View.ColumnDefinitions
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(Enabled)]
    internal class AnalyzerEnabledColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerEnabledColumnDefinition()
        {
        }

        public override string Name => Enabled;
        public override string DisplayName => ServicesVSResources.Enabled;
        public override bool IsFilterable => true;
        public override bool IsSortable => true;
        public override double MinWidth => 50;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement content)
        {
            var checkBox = new CheckBox();
            if (entry.TryGetValue(Name, out bool enabled))
            {
                checkBox.IsChecked = enabled;
            }

            content = checkBox;
            return true;
        }
    }
}
