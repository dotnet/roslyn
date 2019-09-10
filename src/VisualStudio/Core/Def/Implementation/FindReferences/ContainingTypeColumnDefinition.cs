// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>   
    /// Custom column to display the containing type for the All References window.
    /// </summary>
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal class ContainingTypeColumnDefinition : AbstractCustomColumnDefinition
    {
        public const string ColumnName = AbstractReferenceFinder.ContainingTypeInfoPropertyName;

        [ImportingConstructor]
        public ContainingTypeColumnDefinition()
        {
        }

        public override bool IsFilterable => true;
        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Containing_type;
    }
}
