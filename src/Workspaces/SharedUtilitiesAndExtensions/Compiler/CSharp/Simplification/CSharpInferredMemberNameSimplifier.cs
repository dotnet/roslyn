// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal static class CSharpInferredMemberNameSimplifier
{
    internal static bool CanSimplifyTupleElementName(ArgumentSyntax node, CSharpParseOptions parseOptions)
    {
        // Tuple elements are arguments in a tuple expression
        if (node.NameColon == null || !node.Parent.IsKind(SyntaxKind.TupleExpression))
        {
            return false;
        }

        if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_1)
        {
            return false;
        }

        if (RemovalCausesAmbiguity(((TupleExpressionSyntax)node.Parent).Arguments, node))
        {
            return false;
        }

        var inferredName = node.Expression.TryGetInferredMemberName();
        if (inferredName == null || inferredName != node.NameColon.Name.Identifier.ValueText)
        {
            return false;
        }

        return true;
    }

    internal static bool CanSimplifyAnonymousTypeMemberName(AnonymousObjectMemberDeclaratorSyntax node)
    {
        if (node.NameEquals == null)
        {
            return false;
        }

        if (RemovalCausesAmbiguity(((AnonymousObjectCreationExpressionSyntax)node.Parent!).Initializers, node))
        {
            return false;
        }

        var inferredName = node.Expression.TryGetInferredMemberName();
        if (inferredName == null || inferredName != node.NameEquals.Name.Identifier.ValueText)
        {
            return false;
        }

        return true;
    }

    // An explicit name cannot be removed if some other position would produce it as inferred name
    private static bool RemovalCausesAmbiguity(SeparatedSyntaxList<ArgumentSyntax> arguments, ArgumentSyntax toRemove)
    {
        Contract.ThrowIfNull(toRemove.NameColon);

        var name = toRemove.NameColon.Name.Identifier.ValueText;
        foreach (var argument in arguments)
        {
            if (argument == toRemove)
            {
                continue;
            }

            if (argument.NameColon is null && argument.Expression.TryGetInferredMemberName()?.Equals(name) == true)
            {
                return true;
            }
        }

        return false;
    }

    // An explicit name cannot be removed if some other position would produce it as inferred name
    private static bool RemovalCausesAmbiguity(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers, AnonymousObjectMemberDeclaratorSyntax toRemove)
    {
        Contract.ThrowIfNull(toRemove.NameEquals);

        var name = toRemove.NameEquals.Name.Identifier.ValueText;
        foreach (var initializer in initializers)
        {
            if (initializer == toRemove)
            {
                continue;
            }

            if (initializer.NameEquals is null && initializer.Expression.TryGetInferredMemberName()?.Equals(name) == true)
            {
                return true;
            }
        }

        return false;
    }
}
