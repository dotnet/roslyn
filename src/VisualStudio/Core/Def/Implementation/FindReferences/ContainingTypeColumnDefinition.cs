// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.FindUsages;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ContainingType
{
    /// <summary>   
    /// Custom column to display the containing type for the All References window.
    /// </summary>
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    class ContainingTypeColumnDefinition : AbstractCustomColumnDefinition
    {
        public const string ColumnName = nameof(ContainingTypeInfo);

        [ImportingConstructor]
        public ContainingTypeColumnDefinition()
        {
        }

        public override bool IsFilterable => true;
        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Containing_type;

        public override string GetDisplayStringForColumnValues(ImmutableArray<string> values)
        {
            return values[0];
        }
    }
}
