// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveStaticMembers;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers
{
    [ExportWorkspaceService(typeof(IMoveStaticMembersOptionsService))]
    [Shared]
    [PartNotDiscoverable]
    internal class TestMoveStaticMembersService : IMoveStaticMembersOptionsService
    {
        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestMoveStaticMembersService()
        {
        }

        public string? DestinationType { get; set; }

        public ImmutableArray<string> SelectedMembers { get; set; }

        public string? Filename { get; set; }

        public MoveStaticMembersOptions GetMoveMembersToTypeOptions(Document document, INamedTypeSymbol selectedType, ISymbol? selectedNodeSymbol)
        {
            var selectedMembers = selectedType.GetMembers().WhereAsArray(symbol => SelectedMembers.Contains(symbol.Name));
            var namespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : selectedType.ContainingNamespace.ToDisplayString();
            // just return all the selected members
            return new MoveStaticMembersOptions(
                Filename!,
                string.Join(".", namespaceDisplay, DestinationType!),
                selectedMembers);
        }
    }
}
