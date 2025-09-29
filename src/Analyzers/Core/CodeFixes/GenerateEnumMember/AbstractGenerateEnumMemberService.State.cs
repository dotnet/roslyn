// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember;

internal abstract partial class AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax>
{
    private sealed partial class State
    {
        // public TypeDeclarationSyntax ContainingTypeDeclaration { get; private set; }
        public INamedTypeSymbol TypeToGenerateIn { get; private set; } = null!;

        // Just the name of the method.  i.e. "Goo" in "Goo" or "X.Goo"
        public SyntaxToken IdentifierToken { get; private set; }
        public TSimpleNameSyntax SimpleName { get; private set; } = null!;
        public TExpressionSyntax SimpleNameOrMemberAccessExpression { get; private set; } = null!;

        public static async Task<State?> GenerateAsync(
            TService service,
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var state = new State();
            return await state.TryInitializeAsync(service, document, node, cancellationToken).ConfigureAwait(false) ? state : null;
        }

        private async ValueTask<bool> TryInitializeAsync(
            TService service,
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            if (service.IsIdentifierNameGeneration(node))
            {
                if (!TryInitializeIdentifierName(service, document, (TSimpleNameSyntax)node, cancellationToken))
                    return false;
            }
            else
            {
                return false;
            }

            // Ok.  It either didn't bind to any symbols, or it bound to a symbol but with
            // errors.  In the former case we definitely want to offer to generate a field.  In
            // the latter case, we want to generate a field *unless* there's an existing member
            // with the same name.  Note: it's ok if there's an existing field with the same
            // name.
            var existingMembers = TypeToGenerateIn.GetMembers(IdentifierToken.ValueText);
            if (existingMembers.Any())
            {
                // TODO: Code coverage There was an existing member that the new member would
                // clash with.  
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var sourceType = (await SymbolFinderInternal.FindSourceDefinitionAsync(TypeToGenerateIn, document.Project.Solution, cancellationToken).ConfigureAwait(false)) as INamedTypeSymbol;
            if (!ValidateTypeToGenerateIn(sourceType, true, EnumType))
                return false;

            TypeToGenerateIn = sourceType;
            return CodeGenerator.CanAdd(document.Project.Solution, TypeToGenerateIn, cancellationToken);
        }

        private bool TryInitializeIdentifierName(
            TService service,
            SemanticDocument semanticDocument,
            TSimpleNameSyntax identifierName,
            CancellationToken cancellationToken)
        {
            SimpleName = identifierName;
            if (!service.TryInitializeIdentifierNameState(semanticDocument, identifierName, cancellationToken,
                    out var identifierToken, out var simpleNameOrMemberAccessExpression))
            {
                return false;
            }

            IdentifierToken = identifierToken;
            SimpleNameOrMemberAccessExpression = simpleNameOrMemberAccessExpression;

            var semanticModel = semanticDocument.SemanticModel;
            var semanticFacts = semanticDocument.Document.GetRequiredLanguageService<ISemanticFactsService>();
            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (semanticFacts.IsWrittenTo(semanticModel, SimpleNameOrMemberAccessExpression, cancellationToken) ||
                syntaxFacts.IsInNamespaceOrTypeContext(SimpleNameOrMemberAccessExpression))
            {
                return false;
            }

            // Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
            // binds uniquely, then we don't need to offer this quick fix.
            cancellationToken.ThrowIfCancellationRequested();
            var containingType = semanticModel.GetEnclosingNamedType(identifierToken.SpanStart, cancellationToken);
            if (containingType == null)
                return false;

            var semanticInfo = semanticModel.GetSymbolInfo(SimpleNameOrMemberAccessExpression, cancellationToken);
            if (semanticInfo.Symbol != null)
                return false;

            // Either we found no matches, or this was ambiguous. Either way, we might be able
            // to generate a method here.  Determine where the user wants to generate the method
            // into, and if it's valid then proceed.
            if (!TryDetermineTypeToGenerateIn(
                    semanticDocument, containingType, simpleNameOrMemberAccessExpression, cancellationToken,
                    out var typeToGenerateIn, out var isStatic, out var isColorColorCase))
            {
                return false;
            }

            if (!isStatic && !isColorColorCase)
                return false;

            TypeToGenerateIn = typeToGenerateIn;
            return true;
        }
    }
}
