using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Editor.Commanding;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.VisualStudio.Editor.Implementation
{
    internal sealed class CommandBindings
    {
        [Export]
        [CommandBinding(Guids.RoslynGroupIdString, ID.RoslynCommands.GoToImplementation, typeof(GoToImplementationCommandArgs))]
        internal CommandBindingDefinition gotoImplementationCommandBinding;

        [Export]
        [CommandBinding(Guids.RoslynGroupIdString, ID.RoslynCommands.GoToBase, typeof(GoToBaseCommandArgs))]
        internal CommandBindingDefinition gotoBaseCommandBinding;

        [Export]
        [CommandBinding(Guids.CSharpGroupIdString, ID.CSharpCommands.OrganizeSortUsings, typeof(SortImportsCommandArgs))]
        internal CommandBindingDefinition organizeSortCommandBinding;

        [Export]
        [CommandBinding(Guids.CSharpGroupIdString, ID.CSharpCommands.OrganizeRemoveAndSort, typeof(SortAndRemoveUnnecessaryImportsCommandArgs))]
        internal CommandBindingDefinition organizeRemoveAndSortCommandBinding;

        [Export]
        [CommandBinding(Guids.CSharpGroupIdString, ID.CSharpCommands.ContextOrganizeRemoveAndSort, typeof(SortAndRemoveUnnecessaryImportsCommandArgs))]
        internal CommandBindingDefinition contextOrganizeRemoveAndSortCommandBinding;
    }
}
