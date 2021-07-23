// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MoveMembersToType
{
    internal class MoveMembersToTypeWithDialogCodeAction : CodeActionWithOptions
    {
        private readonly Document _document;
        private readonly ISymbol? _selectedMember;
        private readonly INamedTypeSymbol _selectedType;
        private readonly SyntaxNode _selectedTypeDeclarationNode;
        private readonly IMoveMembersToTypeOptionsService _service;

        public TextSpan Span { get; }
        public override string Title => "[WIP] Move Members to Type...";

        public MoveMembersToTypeWithDialogCodeAction(
            Document document,
            TextSpan span,
            IMoveMembersToTypeOptionsService service,
            INamedTypeSymbol selectedType,
            SyntaxNode selectedTypeDeclarationNode,
            ISymbol? selectedMember = null)
        {
            _document = document;
            _service = service;
            _selectedType = selectedType;
            _selectedTypeDeclarationNode = selectedTypeDeclarationNode;
            _selectedMember = selectedMember;
            Span = span;
        }

        public override object? GetOptions(CancellationToken cancellationToken)
        {
            var optionsService = _service ?? _document.Project.Solution.Workspace.Services.GetRequiredService<IMoveMembersToTypeOptionsService>();
            return optionsService.GetMoveMembersToTypeOptions(_document, _selectedType, _selectedMember);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
