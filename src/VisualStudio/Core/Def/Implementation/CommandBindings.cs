// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Editor.Commanding;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.ValueTracking;

namespace Microsoft.VisualStudio.Editor.Implementation;

internal sealed class CommandBindings
{
    [Export]
    [CommandBinding(Guids.RoslynGroupIdString, ID.RoslynCommands.GoToImplementation, typeof(GoToImplementationCommandArgs))]
    internal CommandBindingDefinition gotoImplementationCommandBinding;

    [Export]
    [CommandBinding(Guids.CSharpGroupIdString, ID.CSharpCommands.OrganizeSortUsings, typeof(SortImportsCommandArgs))]
    internal CommandBindingDefinition organizeSortCommandBinding;

    [Export]
    [CommandBinding(Guids.CSharpGroupIdString, ID.CSharpCommands.OrganizeRemoveAndSort, typeof(SortAndRemoveUnnecessaryImportsCommandArgs))]
    internal CommandBindingDefinition organizeRemoveAndSortCommandBinding;

    [Export]
    [CommandBinding(Guids.CSharpGroupIdString, ID.CSharpCommands.ContextOrganizeRemoveAndSort, typeof(SortAndRemoveUnnecessaryImportsCommandArgs))]
    internal CommandBindingDefinition contextOrganizeRemoveAndSortCommandBinding;

    [Export]
    [CommandBinding(Guids.RoslynGroupIdString, ID.RoslynCommands.GoToValueTrackingWindow, typeof(ValueTrackingEditorCommandArgs))]
    internal CommandBindingDefinition gotoDataFlowToolCommandBinding;
}
