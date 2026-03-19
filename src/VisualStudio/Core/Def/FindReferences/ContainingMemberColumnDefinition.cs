// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences;

/// <summary>
/// Custom column to display the containing member for the Find All References window.
/// </summary>
[Export(typeof(ITableColumnDefinition))]
[Name(ColumnName)]
internal sealed class ContainingMemberColumnDefinition : TableColumnDefinitionBase
{
    public const string ColumnName = AbstractReferenceFinder.ContainingMemberInfoPropertyName;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ContainingMemberColumnDefinition()
    {
    }

    public override bool IsFilterable => true;
    public override string Name => ColumnName;
    public override string DisplayName => ServicesVSResources.Containing_member;
}
