﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.Formatting;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.View.ColumnDefnitions
{
    [Export(typeof(IDefaultColumnGroup))]
    [Name(nameof(FormattingCategoryGroupingSet))]    // Required, name of the default group
    [GroupColumns(Category)] // Required, the names of the columns in the grouping
    internal class FormattingCategoryGroupingSet : IDefaultColumnGroup
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormattingCategoryGroupingSet()
        {
        }
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(Category)]
    internal class FormattingCategoryColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormattingCategoryColumnDefinition()
        {
        }

        public override string Name => Category;
        public override string DisplayName => ServicesVSResources.Category;
        public override double MinWidth => 80;
        public override bool DefaultVisible => false;
        public override bool IsFilterable => true;
        public override bool IsSortable => true;
        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        private static string? GetCategoryName(ITableEntryHandle entry)
            => entry.TryGetValue(Category, out string? categoryName)
                ? categoryName
                : null;

        public override IEntryBucket? CreateBucketForEntry(ITableEntryHandle entry)
        {
            var categoryName = GetCategoryName(entry);
            return categoryName is not null ? new StringEntryBucket(GetCategoryName(entry)) : null;
        }
    }
}
