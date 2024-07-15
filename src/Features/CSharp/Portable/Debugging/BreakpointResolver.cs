// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Debugging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Debugging;

internal class BreakpointResolver(Solution solution, string text) : AbstractBreakpointResolver(solution, text, LanguageNames.CSharp, EqualityComparer<string>.Default)
{
    protected override IEnumerable<ISymbol> GetMembers(INamedTypeSymbol type, string name)
    {
        var members = type.GetMembers()
                          .Where(m => m.Name == name ||
                                      m.ExplicitInterfaceImplementations()
                                       .Where(i => i.Name == name)
                                       .Any());

        return (type.Name == name) ? members.Concat(type.Constructors) : members;
    }

    protected override bool HasMethodBody(IMethodSymbol method, CancellationToken cancellationToken)
    {
        var location = method.Locations.First(loc => loc.IsInSource);
        var tree = location.SourceTree;
        var token = tree.GetRoot(cancellationToken).FindToken(location.SourceSpan.Start);

        return token.GetAncestor<MemberDeclarationSyntax>().GetBody() != null;
    }

    protected override void ParseText(
        out IList<NameAndArity> nameParts,
        out int? parameterCount)
    {
        var text = Text;

        Debug.Assert(text != null);

        var name = SyntaxFactory.ParseName(text, consumeFullText: false);
        var lengthOfParsedText = name.FullSpan.End;
        var parameterList = SyntaxFactory.ParseParameterList(text, lengthOfParsedText, consumeFullText: false);
        var foundIncompleteParameterList = false;

        parameterCount = null;
        if (!parameterList.IsMissing)
        {
            if (parameterList.OpenParenToken.IsMissing || parameterList.CloseParenToken.IsMissing)
            {
                foundIncompleteParameterList = true;
            }
            else
            {
                lengthOfParsedText += parameterList.FullSpan.End;
                parameterCount = parameterList.Parameters.Count;
            }
        }

        // If there is remaining text to parse, attempt to eat a trailing semicolon.
        if (lengthOfParsedText < text.Length)
        {
            var token = SyntaxFactory.ParseToken(text, lengthOfParsedText);
            if (token.IsKind(SyntaxKind.SemicolonToken))
            {
                lengthOfParsedText += token.FullSpan.End;
            }
        }

        // It's not obvious, but this method can handle the case where name "IsMissing" (no suitable name was be parsed).
        var parts = name.GetNameParts();

        // If we could not parse a valid parameter list or there was additional trailing text that could not be
        // interpreted, don't return any names or parameters.
        // Also, "Break at Function" doesn't seem to support alias qualified names with the old language service,
        // and aliases don't seem meaningful for the purposes of resolving symbols from source.  Since we don't
        // have precedent or a clear user scenario, we won't resolve any alias qualified names (alias qualified
        // parameters are accepted, but we still only validate parameter count, similar to the old implementation).
        if (!foundIncompleteParameterList && (lengthOfParsedText == text.Length) &&
            !parts.Any(p => p.IsKind(SyntaxKind.AliasQualifiedName)))
        {
            nameParts = parts.Cast<SimpleNameSyntax>().Select(p => new NameAndArity(p.Identifier.ValueText, p.Arity)).ToList();
        }
        else
        {
            nameParts = SpecializedCollections.EmptyList<NameAndArity>();
        }
    }
}
