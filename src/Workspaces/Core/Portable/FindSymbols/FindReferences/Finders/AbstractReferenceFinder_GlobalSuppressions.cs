// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal abstract partial class AbstractReferenceFinder : IReferenceFinder
{
    private static bool ShouldFindReferencesInGlobalSuppressions(ISymbol symbol, [NotNullWhen(returnValue: true)] out string? documentationCommentId)
    {
        if (!SupportsGlobalSuppression(symbol))
        {
            documentationCommentId = null;
            return false;
        }

        documentationCommentId = DocumentationCommentId.CreateDeclarationId(symbol);
        return documentationCommentId != null;

        // Global suppressions are currently supported for types, members and
        // namespaces, except global namespace.
        static bool SupportsGlobalSuppression(ISymbol symbol)
            => symbol.Kind switch
            {
                SymbolKind.Namespace => !((INamespaceSymbol)symbol).IsGlobalNamespace,
                SymbolKind.NamedType => true,
                SymbolKind.Method => true,
                SymbolKind.Field => true,
                SymbolKind.Property => true,
                SymbolKind.Event => true,
                _ => false,
            };
    }

    /// <summary>
    /// Find references to a symbol inside global suppressions.
    /// For example, consider a field 'Field' defined inside a type 'C'.
    /// This field's documentation comment ID is 'F:C.Field'
    /// A reference to this field inside a global suppression would be as following:
    ///     [assembly: SuppressMessage("RuleCategory", "RuleId', Scope = "member", Target = "~F:C.Field")]
    /// </summary>
    [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
    protected static void FindReferencesInDocumentInsideGlobalSuppressions<TData>(
        ISymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        if (!ShouldFindReferencesInGlobalSuppressions(symbol, out var docCommentId))
            return;

        // Check if we have any relevant global attributes in this document.
        var info = state.Cache.SyntaxTreeIndex;
        if (!info.ContainsGlobalSuppressMessageAttribute)
            return;

        var semanticModel = state.SemanticModel;
        var suppressMessageAttribute = semanticModel.Compilation.SuppressMessageAttributeType();
        if (suppressMessageAttribute == null)
            return;

        // Check if we have any instances of the symbol documentation comment ID string literals within global attributes.
        // These string literals represent references to the symbol.
        if (!TryGetExpectedDocumentationCommentId(docCommentId, out var expectedDocCommentId))
            return;

        var syntaxFacts = state.SyntaxFacts;

        // We map the positions of documentation ID literals in tree to string literal tokens,
        // perform semantic checks to ensure these are valid references to the symbol
        // and if so, add these locations to the computed references.
        var root = state.Root;
        foreach (var token in root.DescendantTokens())
        {
            if (IsCandidate(state, token, expectedDocCommentId.Span, suppressMessageAttribute, cancellationToken, out var offsetOfReferenceInToken))
            {
                var referenceLocation = CreateReferenceLocation(offsetOfReferenceInToken, token, root, state.Document, syntaxFacts);
                processResult(new FinderLocation(token.GetRequiredParent(), referenceLocation), processResultData);
            }
        }

        return;

        // Local functions
        static bool IsCandidate(
            FindReferencesDocumentState state, SyntaxToken token, ReadOnlySpan<char> expectedDocCommentId,
            INamedTypeSymbol suppressMessageAttribute, CancellationToken cancellationToken, out int offsetOfReferenceInToken)
        {
            offsetOfReferenceInToken = -1;

            // Check if this token is a named attribute argument to "Target" property of "SuppressMessageAttribute".
            if (!IsValidTargetOfGlobalSuppressionAttribute(
                    token, suppressMessageAttribute, state.SemanticModel, state.SyntaxFacts, cancellationToken))
            {
                return false;
            }

            // Target string must contain a valid symbol DocumentationCommentId.
            if (!ValidateAndSplitDocumentationCommentId(token.ValueText, out var prefix, out var docCommentId))
            {
                return false;
            }

            // We have couple of success cases:
            // 1. The FAR symbol is the same one as the target of the suppression. In this case,
            //    target string for the suppression exactly matches the expectedDocCommentId.
            // 2. The FAR symbol is one of the containing symbols of the target symbol of suppression.
            //    In this case, the target string for the suppression starts with the expectedDocCommentId. 
            //
            // For example, consider the below suppression applied to field 'Field' of type 'C'
            //      [assembly: SuppressMessage("RuleCategory", "RuleId', Scope = "member", Target = "~F:C.Field")]
            // When doing a FAR query on 'Field', we would return true from case 1.
            // When doing a FAR query on 'C', we would return true from case 2.

            if (!docCommentId.Span.StartsWith(expectedDocCommentId))
            {
                return false;
            }

            // We found a match, now compute the offset of the reference within the string literal token.
            offsetOfReferenceInToken = prefix.Length;

            if (expectedDocCommentId.Length < docCommentId.Length)
            {
                // Expected doc comment ID belongs to a containing symbol of the symbol referenced in suppression's target doc comment ID.
                // Verify the next character in suppression doc comment ID is the '.' separator for its member.
                if (docCommentId.Span[expectedDocCommentId.Length] != '.')
                    return false;

                offsetOfReferenceInToken += expectedDocCommentId.LastIndexOf('.') + 1;
            }
            else
            {
                // Expected doc comment ID matches the suppression's target doc comment ID.
                SplitIdAndArguments(docCommentId, out var idPartBeforeArguments, out var _);
                offsetOfReferenceInToken += idPartBeforeArguments.Span.LastIndexOf('.') + 1;
            }

            return true;
        }

        static bool IsValidTargetOfGlobalSuppressionAttribute(
            SyntaxToken token,
            INamedTypeSymbol suppressMessageAttribute,
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            CancellationToken cancellationToken)
        {
            // We need to check if the given token is a non-null, non-empty string literal token
            // passed as a named argument to 'Target' property of a global SuppressMessageAttribute.
            //
            // For example, consider the below global suppression.
            //      [assembly: SuppressMessage("RuleCategory", "RuleId', Scope = "member", Target = "F:C.Field")]
            //
            // We return true when processing "F:C.Field".

            if (!syntaxFacts.IsStringLiteral(token))
            {
                return false;
            }

            var text = token.ValueText;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // We need to go from string literal token "F:C.Field" to the suppression attribute node.
            //  AttributeSyntax
            //   -> AttributeArgumentList
            //     -> AttributeArgument
            //       -> StringLiteralExpression
            //         -> StringLiteralToken
            var attributeArgument = token.Parent?.Parent;
            if (syntaxFacts.GetNameForAttributeArgument(attributeArgument) != "Target")
            {
                return false;
            }

            var attributeNode = attributeArgument!.Parent?.Parent;
            if (attributeNode == null || !syntaxFacts.IsGlobalAttribute(attributeNode))
            {
                return false;
            }

            // Check the attribute type matches 'SuppressMessageAttribute'.
            var attributeSymbol = semanticModel.GetSymbolInfo(attributeNode, cancellationToken).Symbol?.ContainingType;
            return suppressMessageAttribute.Equals(attributeSymbol);
        }

        static ReferenceLocation CreateReferenceLocation(
            int offsetOfReferenceInToken,
            SyntaxToken token,
            SyntaxNode root,
            Document document,
            ISyntaxFacts syntaxFacts)
        {
            // We found a valid reference to the symbol in documentation comment ID string literal.
            // Compute the reference span within this string literal for the identifier.
            // For example, consider the suppression below for field 'Field' defined in type 'C':
            //      [assembly: SuppressMessage("RuleCategory", "RuleId', Scope = "member", Target = "F:C.Field")]
            // We compute the span for 'Field' within the target string literal.
            // NOTE: '#' is also a valid char in documentation comment ID. For example, '#ctor' and '#cctor'.

            var positionOfReferenceInTree = token.SpanStart + offsetOfReferenceInToken + 1;
            var valueText = token.ValueText;
            var length = 0;
            while (offsetOfReferenceInToken < valueText.Length)
            {
                var ch = valueText[offsetOfReferenceInToken++];
                if (ch == '#' || syntaxFacts.IsIdentifierPartCharacter(ch))
                    length++;
                else
                    break;
            }

            // We create a reference location of the identifier span within this string literal
            // that represents the symbol reference.
            // We also add the location for the containing documentation comment ID string literal.
            // For the suppression example above, location points to the span of 'Field' inside "F:C.Field"
            // and containing string location points to the span of the entire string literal "F:C.Field".
            var location = Location.Create(root.SyntaxTree, new TextSpan(positionOfReferenceInTree, length));
            var containingStringLocation = token.GetLocation();
            return new ReferenceLocation(document, location, containingStringLocation);
        }
    }

    private static bool TryGetExpectedDocumentationCommentId(
        string id,
        out ReadOnlyMemory<char> docCommentId)
    {
        return ValidateAndSplitDocumentationCommentId(id, out _, out docCommentId);
    }

    /// <summary>
    /// Validate and split a documentation comment ID into a prefix and complete symbol ID. For the
    /// <paramref name="docCommentId"/> <c>~M:C.X(System.String)</c>, the <paramref name="prefix"/> would be
    /// <c>~M:</c> and <paramref name="id"/> would be <c>C.X(System.String)</c>.
    /// </summary>
    [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", Constraint = "Avoid Regex splitting due to high allocation costs.")]
    private static bool ValidateAndSplitDocumentationCommentId(
        [NotNullWhen(true)] string? docCommentId,
        out ReadOnlyMemory<char> prefix,
        out ReadOnlyMemory<char> id)
    {
        prefix = ReadOnlyMemory<char>.Empty;
        id = ReadOnlyMemory<char>.Empty;
        if (docCommentId is null)
        {
            return false;
        }

        // Parse the prefix
        if (docCommentId.StartsWith("~"))
        {
            if (docCommentId.Length < 3)
                return false;

            prefix = docCommentId.AsMemory()[0..3];
        }
        else
        {
            if (docCommentId.Length < 2)
                return false;

            prefix = docCommentId.AsMemory()[0..2];
        }

        if (prefix.Span[^2] is < 'A' or > 'Z')
        {
            return false;
        }

        if (prefix.Span[^1] is not ':')
        {
            return false;
        }

        // The rest of the ID is returned without splitting
        id = docCommentId.AsMemory()[prefix.Length..];
        return true;
    }

    /// <summary>
    /// Split a full documentation symbol ID into the core symbol ID and optional parameter list. For the
    /// <paramref name="id"/> <c>C.X(System.String)</c>, the <paramref name="idPartBeforeArguments"/> would be
    /// <c>C.X</c> and <paramref name="arguments"/> would be <c>(System.String)</c>.
    /// </summary>
    private static void SplitIdAndArguments(
        ReadOnlyMemory<char> id,
        out ReadOnlyMemory<char> idPartBeforeArguments,
        out ReadOnlyMemory<char> arguments)
    {
        ReadOnlySpan<char> argumentSeparators = stackalloc[] { '(', '[' };
        var indexOfArguments = id.Span.IndexOfAny(argumentSeparators);
        if (indexOfArguments < 0)
        {
            idPartBeforeArguments = id;
            arguments = ReadOnlyMemory<char>.Empty;
        }
        else
        {
            idPartBeforeArguments = id[0..indexOfArguments];
            arguments = id[indexOfArguments..];
        }
    }

    /// <summary>
    /// Validate and split symbol documentation comment ID.
    /// For example, "~M:C.X(System.String)" represents the documentation comment ID of a method named 'X'
    /// that takes a single string-typed parameter and is contained in a type named 'C'.
    ///
    /// We divide the ID into 3 groups:
    /// 1. Prefix:
    ///     - Starts with an optional '~'
    ///     - Followed by a single capital letter indicating the symbol kind (for example, 'M' indicates method symbol)
    ///     - Followed by ':'
    /// 2. Core symbol ID, which is its fully qualified name before the optional parameter list and return type (i.e. before the '(' or '[' tokens)
    /// 3. Optional parameter list and/or return type that begins with a '(' or '[' tokens.
    ///
    /// For the above example, "~M:" is the prefix, "C.X" is the core symbol ID and "(System.String)" is the parameter list.
    /// </summary>
    private static bool ValidateAndSplitDocumentationCommentId(
        [NotNullWhen(true)] string? docCommentId,
        out ReadOnlyMemory<char> prefix,
        out ReadOnlyMemory<char> idPartBeforeArguments,
        out ReadOnlyMemory<char> arguments)
    {
        idPartBeforeArguments = ReadOnlyMemory<char>.Empty;
        arguments = ReadOnlyMemory<char>.Empty;
        if (!ValidateAndSplitDocumentationCommentId(docCommentId, out prefix, out var id))
        {
            return false;
        }

        // Parse the id part and arguments
        SplitIdAndArguments(id, out idPartBeforeArguments, out arguments);
        return true;
    }
}
