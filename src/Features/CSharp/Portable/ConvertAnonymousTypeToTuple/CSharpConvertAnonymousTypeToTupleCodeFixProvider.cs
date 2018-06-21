// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToTuple
{
    internal abstract class CSharpConvertAnonymousTypeToTupleCodeFixProvider 
        : AbstractConvertAnonymousTypeToTupleCodeFixProvider<
            ExpressionSyntax,
            TupleExpressionSyntax,
            AnonymousObjectCreationExpressionSyntax>
    {
        protected override TupleExpressionSyntax ConvertToTuple(AnonymousObjectCreationExpressionSyntax anonCreation)
            => SyntaxFactory.TupleExpression(
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTriviaFrom(anonCreation.OpenBraceToken),
                    ConvertInitializers(anonCreation.Initializers),
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTriviaFrom(anonCreation.CloseBraceToken))
                            .WithPrependedLeadingTrivia(anonCreation.GetLeadingTrivia());

        private static SeparatedSyntaxList<ArgumentSyntax> ConvertInitializers(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers)
            => SyntaxFactory.SeparatedList(initializers.Select(ConvertInitializer), initializers.GetSeparators());

        private static ArgumentSyntax ConvertInitializer(AnonymousObjectMemberDeclaratorSyntax declarator)
            => SyntaxFactory.Argument(ConvertName(declarator.NameEquals), default, declarator.Expression)
                            .WithTriviaFrom(declarator);

        private static NameColonSyntax ConvertName(NameEqualsSyntax nameEquals)
            => nameEquals == null
                ? null
                : SyntaxFactory.NameColon(
                    nameEquals.Name,
                    SyntaxFactory.Token(SyntaxKind.ColonToken).WithTriviaFrom(nameEquals.EqualsToken));

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Convert_anonymous_type_to_tuple, createChangedDocument, FeaturesResources.Convert_anonymous_type_to_tuple)
            {
            }
        }
    }
}
