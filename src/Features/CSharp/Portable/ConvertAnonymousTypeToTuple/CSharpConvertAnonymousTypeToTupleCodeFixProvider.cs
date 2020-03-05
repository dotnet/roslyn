﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToTuple
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertAnonymousTypeToTupleCodeFixProvider)), Shared]
    internal class CSharpConvertAnonymousTypeToTupleCodeFixProvider
        : AbstractConvertAnonymousTypeToTupleCodeFixProvider<
            ExpressionSyntax,
            TupleExpressionSyntax,
            AnonymousObjectCreationExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertAnonymousTypeToTupleCodeFixProvider()
        {
        }

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
    }
}
