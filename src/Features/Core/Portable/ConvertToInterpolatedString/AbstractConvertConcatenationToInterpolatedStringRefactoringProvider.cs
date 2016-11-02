// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString
{
    internal abstract class AbstractConvertConcatenationToInterpolatedStringRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            if (context.Span.Length > 0)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            if (stringType == null)
            {
                return;
            }

            var position = context.Span.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            if (!token.Span.Contains(position))
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!syntaxFacts.IsStringLiteral(token))
            {
                return;
            }

            var literalExpression = token.Parent;
            var literalParent = literalExpression.Parent;
            if (!IsStringConcat(syntaxFacts, literalParent, semanticModel, cancellationToken))
            {
                return;
            }

            var top = literalParent;
            while (IsStringConcat(syntaxFacts, top.Parent, semanticModel, cancellationToken))
            {
                top = top.Parent;
            }

            var pieces = new List<SyntaxNode>();
            CollectPiecesDown(syntaxFacts, pieces, top, semanticModel, cancellationToken);

            var firstStringToken = pieces.First(syntaxFacts.IsStringLiteralExpression).GetFirstToken();
            if (!pieces.Where(syntaxFacts.IsStringLiteralExpression).All(
                    lit => SameLiteralKind(lit, firstStringToken)))
            {
                return;
            }

            var interpolatedString = CreateInterpolatedString(firstStringToken, pieces);
            context.RegisterRefactoring(new MyCodeAction(
                c => UpdateDocumentAsync(document, root, top, interpolatedString, c)));
        }

        private Task<Document> UpdateDocumentAsync(Document document, SyntaxNode root, SyntaxNode top, SyntaxNode interpolatedString, CancellationToken c)
        {
            var newRoot = root.ReplaceNode(top, interpolatedString);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        protected abstract SyntaxNode CreateInterpolatedString(
            SyntaxToken firstStringToken, List<SyntaxNode> pieces);

        private bool SameLiteralKind(SyntaxNode literal1, SyntaxToken firstStringToken)
        {
            var token1 = literal1.GetFirstToken();

            var text1 = token1.Text;
            var text2 = firstStringToken.Text;
            return text1.Length > 0 && text2.Length > 0 && text1[0] == text2[0];
        }

        private void CollectPiecesDown(
            ISyntaxFactsService syntaxFacts,
            List<SyntaxNode> pieces, 
            SyntaxNode node, 
            SemanticModel semanticModel, 
            CancellationToken cancellationToken)
        {
            if (!IsStringConcat(syntaxFacts, node, semanticModel, cancellationToken))
            {
                pieces.Add(node);
                return;
            }

            SyntaxNode left, right;
            syntaxFacts.GetPartsOfBinaryExpression(node, out left, out right);

            CollectPiecesDown(syntaxFacts, pieces, left, semanticModel, cancellationToken);
            pieces.Add(right);
        }

        private bool IsStringConcat(
            ISyntaxFactsService syntaxFacts, SyntaxNode expression, 
            SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!syntaxFacts.IsBinaryExpression(expression))
            {
                return false;
            }

            var method = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol as IMethodSymbol;
            return method?.MethodKind == MethodKind.BuiltinOperator &&
                   method.ContainingType.SpecialType == SpecialType.System_String &&
                   (method.MetadataName == WellKnownMemberNames.AdditionOperatorName ||
                    method.MetadataName == WellKnownMemberNames.ConcatenateOperatorName);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Convert_to_interpolated_string, createChangedDocument)
            {
            }
        }
    }
}