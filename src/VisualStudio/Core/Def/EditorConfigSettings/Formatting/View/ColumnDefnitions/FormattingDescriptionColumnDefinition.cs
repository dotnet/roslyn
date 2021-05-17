﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.Formatting;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.View.ColumnDefnitions
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(Description)]
    internal class FormattingDescriptionColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormattingDescriptionColumnDefinition()
        {
        }

        public override string Name => Description;
        public override string DisplayName => ServicesVSResources.Description;
        public override bool IsFilterable => false;
        public override bool IsSortable => false;
        public override double DefaultWidth => 350;
    }
}
