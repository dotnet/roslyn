// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertAnonymousType;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAnonymousType;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToTuple), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpConvertAnonymousTypeToTupleCodeRefactoringProvider()
    : AbstractConvertAnonymousTypeToTupleCodeRefactoringProvider<
        ExpressionSyntax,
        TupleExpressionSyntax,
        AnonymousObjectCreationExpressionSyntax>
{
    protected override int GetInitializerCount(AnonymousObjectCreationExpressionSyntax anonymousType)
        => anonymousType.Initializers.Count;

    protected override TupleExpressionSyntax ConvertToTuple(AnonymousObjectCreationExpressionSyntax anonCreation)
        => TupleExpression(
            OpenParenToken.WithLeadingTrivia(anonCreation.NewKeyword.LeadingTrivia).WithTrailingTrivia(anonCreation.OpenBraceToken.TrailingTrivia),
            ConvertInitializers(anonCreation.Initializers),
            CloseParenToken.WithTriviaFrom(anonCreation.CloseBraceToken));

    private static SeparatedSyntaxList<ArgumentSyntax> ConvertInitializers(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> declarators)
    {
        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var result);

        var originalSeparators = declarators.GetSeparators().ToImmutableArray();
        for (int i = 0, n = declarators.Count; i < n; i++)
        {
            var declarator = declarators[i];
            var argument = ConvertDeclarator(declarator);

            // Keep the trailing newline trivia on the last trailing comma if it exists.
            if (i == n - 1 && i < originalSeparators.Length && originalSeparators[i].TrailingTrivia is [.., (kind: SyntaxKind.EndOfLineTrivia) newLine])
                argument = argument.WithAppendedTrailingTrivia(newLine);

            result.Add(argument);

            // Only keep the commas between elements.  Tuples don't allow trailing commas like anonymous types do.
            if (i < n - 1)
                result.Add(originalSeparators[i]);
        }

        return SeparatedList<ArgumentSyntax>(result);
    }

    private static ArgumentSyntax ConvertDeclarator(AnonymousObjectMemberDeclaratorSyntax declarator)
        => Argument(ConvertName(declarator.NameEquals), refKindKeyword: default, declarator.Expression).WithTriviaFrom(declarator);

    private static NameColonSyntax? ConvertName(NameEqualsSyntax? nameEquals)
        => nameEquals == null
            ? null
            : NameColon(
                // If it's just `Name = ...` then we want to convert that to `Name: ...`.  Otherwise, keep around the
                // existing trivia as it may be relevant to the user.
                nameEquals.Name.GetTrailingTrivia() is [(kind: SyntaxKind.WhitespaceTrivia)]
                    ? nameEquals.Name.WithoutTrailingTrivia()
                    : nameEquals.Name,
                ColonToken.WithTriviaFrom(nameEquals.EqualsToken));
}
