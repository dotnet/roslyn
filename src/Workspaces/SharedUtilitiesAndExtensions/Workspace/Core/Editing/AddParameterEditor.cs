// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editing;

internal static class AddParameterEditor
{
    public static void AddParameter(
        ISyntaxFacts syntaxFacts,
        SyntaxEditor editor,
        SyntaxNode declaration,
        int insertionIndex,
        SyntaxNode parameterDeclaration,
        CancellationToken cancellationToken)
    {
        var sourceText = declaration.SyntaxTree.GetText(cancellationToken);
        var generator = editor.Generator;

        var existingParameters = generator.GetParameters(declaration);
        var placeOnNewLine = ShouldPlaceParametersOnNewLine(existingParameters, cancellationToken);

        if (!placeOnNewLine)
        {
            // Trivial case.  Just let the stock editor impl handle this for us.
            editor.InsertParameter(declaration, insertionIndex, parameterDeclaration);
            return;
        }

        if (insertionIndex >= existingParameters.Count)
        {
            // The parameter is being added after the last parameter and needs to be placed on a new line.
            // Get the indentation of the original last parameter and give the new parameter the same indentation.
            // Even if we're adding multiple parameters past the original last parameter, we can give them all the identation of the original 'last' parameter.
            var leadingIndentation = GetDesiredLeadingIndentation(
                syntaxFacts, existingParameters[existingParameters.Count - 1], includeLeadingNewLine: true);
            parameterDeclaration = parameterDeclaration.WithPrependedLeadingTrivia(leadingIndentation)
                                                       .WithAdditionalAnnotations(Formatter.Annotation);

            editor.AddParameter(declaration, parameterDeclaration);
        }
        else if (insertionIndex == 0)
        {
            // Inserting into the start of the list.  The existing first parameter might
            // be on the same line as the parameter list, or it might be on the next line.
            var firstParameter = existingParameters[0];
            var previousToken = firstParameter.GetFirstToken().GetPreviousToken();

            if (sourceText.AreOnSameLine(previousToken, firstParameter.GetFirstToken()))
            {
                // First parameter is on hte same line as the method.  

                // We want to insert the parameter at the front of the existing parameter
                // list.  That means we need to move the current first parameter to a new
                // line.  Give the current first parameter the indentation of the second
                // parameter in the list.
                editor.InsertParameter(declaration, insertionIndex, parameterDeclaration);
                var nextParameter = existingParameters[insertionIndex];

                var nextLeadingIndentation = GetDesiredLeadingIndentation(
                    syntaxFacts, existingParameters[insertionIndex + 1], includeLeadingNewLine: true);
                editor.ReplaceNode(
                    nextParameter,
                    nextParameter.WithPrependedLeadingTrivia(nextLeadingIndentation)
                                 .WithAdditionalAnnotations(Formatter.Annotation));
            }
            else
            {
                // First parameter is on its own line.  No need to adjust its indentation.
                // Just copy its indentation over to the parameter we're inserting, and
                // make sure the current first parameter gets a newline so it stays on 
                // its own line.

                // We want to insert the parameter at the front of the existing parameter
                // list.  That means we need to move the current first parameter to a new
                // line.  Give the current first parameter the indentation of the second
                // parameter in the list.
                var firstLeadingIndentation = GetDesiredLeadingIndentation(
                    syntaxFacts, existingParameters[0], includeLeadingNewLine: false);

                editor.InsertParameter(declaration, insertionIndex,
                    parameterDeclaration.WithLeadingTrivia(firstLeadingIndentation));
                var nextParameter = existingParameters[insertionIndex];

                editor.ReplaceNode(
                    nextParameter,
                    nextParameter.WithPrependedLeadingTrivia(syntaxFacts.ElasticCarriageReturnLineFeed)
                                 .WithAdditionalAnnotations(Formatter.Annotation));
            }
        }
        else
        {
            // We're inserting somewhere after the start (but not at the end). Because 
            // we've set placeOnNewLine, we know that the current comma we'll be placed
            // after already have a newline following it.  So all we need for this new 
            // parameter is to get the indentation of the following parameter.
            // Because we're going to 'steal' the existing comma from that parameter,
            // ensure that the next parameter has a new-line added to it so that it will
            // still stay on a new line.
            var nextParameter = existingParameters[insertionIndex];
            var leadingIndentation = GetDesiredLeadingIndentation(
                syntaxFacts, existingParameters[insertionIndex], includeLeadingNewLine: false);
            parameterDeclaration = parameterDeclaration.WithPrependedLeadingTrivia(leadingIndentation);

            editor.InsertParameter(declaration, insertionIndex, parameterDeclaration);
            editor.ReplaceNode(
                nextParameter,
                nextParameter.WithPrependedLeadingTrivia(syntaxFacts.ElasticCarriageReturnLineFeed)
                             .WithAdditionalAnnotations(Formatter.Annotation));
        }
    }

    private static ImmutableArray<SyntaxTrivia> GetDesiredLeadingIndentation(
        ISyntaxFacts syntaxFacts, SyntaxNode node, bool includeLeadingNewLine)
    {
        using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var triviaList);
        if (includeLeadingNewLine)
        {
            triviaList.Add(syntaxFacts.ElasticCarriageReturnLineFeed);
        }

        var lastWhitespace = default(SyntaxTrivia);
        foreach (var trivia in node.GetLeadingTrivia().Reverse())
        {
            if (syntaxFacts.IsWhitespaceTrivia(trivia))
            {
                lastWhitespace = trivia;
            }
            else if (syntaxFacts.IsEndOfLineTrivia(trivia))
            {
                break;
            }
        }

        if (lastWhitespace.RawKind != 0)
        {
            triviaList.Add(lastWhitespace);
        }

        return triviaList.ToImmutable();
    }

    private static bool ShouldPlaceParametersOnNewLine(
        IReadOnlyList<SyntaxNode> parameters, CancellationToken cancellationToken)
    {
        if (parameters.Count <= 1)
        {
            return false;
        }

        var text = parameters[0].SyntaxTree.GetText(cancellationToken);
        for (int i = 1, n = parameters.Count; i < n; i++)
        {
            var lastParameter = parameters[i - 1];
            var thisParameter = parameters[i];

            if (text.AreOnSameLine(lastParameter.GetLastToken(), thisParameter.GetFirstToken()))
            {
                return false;
            }
        }

        // All parameters are on different lines.  Place the new parameter on a new line as well.
        return true;
    }
}
