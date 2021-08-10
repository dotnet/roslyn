// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MoveStaticMembers;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers
{
    internal class TestMoveStaticMembersService : IMoveStaticMembersOptionsService
    {
        private readonly string _destinationType;

        private readonly ImmutableArray<string> _selectedMembers;

        private readonly string _filename;

        public TestMoveStaticMembersService(string destinationType, string fileName, ImmutableArray<string> members)
        {
            _destinationType = destinationType;
            _filename = fileName;
            _selectedMembers = members;
        }

        public MoveStaticMembersOptions GetMoveMembersToTypeOptions(Document document, INamedTypeSymbol selectedType, ISymbol? selectedNodeSymbol)
        {
            var selectedMembers = selectedType.GetMembers().WhereAsArray(symbol => _selectedMembers.Contains(symbol.Name));
            var namespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : selectedType.ContainingNamespace.ToDisplayString();
            // just return all the selected members
            return new MoveStaticMembersOptions(
                _filename,
                string.Join(".", namespaceDisplay, _destinationType),
                selectedMembers);
        }
    }
}
