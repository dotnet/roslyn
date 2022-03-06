// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnDefinitions.Analyzer.Location)]
    internal class AnalyzerLocationColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerLocationColumnDefinition()
        {
        }

        public override string Name => ColumnDefinitions.Analyzer.Location;
        public override string DisplayName => ServicesVSResources.Location;
        public override bool IsFilterable => true;
        public override bool IsSortable => true;
        public override double MinWidth => 350;
    }
}
