﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.Formatting;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.View.ColumnDefnitions
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(Value)]
    internal class FormattingValueColumnDefinition : TableColumnDefinitionBase
    {
        private readonly IEnumerable<IEnumSettingViewModelFactory> _factories;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormattingValueColumnDefinition([ImportMany] IEnumerable<IEnumSettingViewModelFactory> factories)
        {
            _factories = factories;
        }

        public override string Name => Value;
        public override string DisplayName => ServicesVSResources.Value;
        public override double MinWidth => 80;
        public override bool DefaultVisible => false;
        public override bool IsFilterable => false;
        public override bool IsSortable => false;
        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (!entry.TryGetValue(Value, out FormattingSetting setting))
            {
                content = null;
                return false;
            }

            if (setting.Type == typeof(bool))
            {
                content = new FormattingBoolSettingView(setting);
                return true;
            }

            foreach (var factory in _factories)
            {
                if (factory.IsSupported(setting.Key))
                {
                    var viewModel = factory.CreateViewModel(setting);
                    content = new EnumSettingView(viewModel);
                    return true;
                }
            }

            content = null;
            return false;
        }
    }
}
