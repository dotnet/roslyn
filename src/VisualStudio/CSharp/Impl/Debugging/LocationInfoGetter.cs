// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Debugging;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    internal static class LocationInfoGetter
    {
        private static readonly SymbolDisplayFormat s_nameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        internal static async Task<DebugLocationInfo> GetInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            string name = null;
            int lineOffset = 0;

            // Note that we get the current partial solution here.  Technically, this means that we may
            // not fully understand the signature of the member.  But that's ok.  We just need this
            // symbol so we can create a display string to put into the debugger.  If we try to
            // find the document in the "CurrentSolution" then when we try to get the semantic 
            // model below then it might take a *long* time as all dependent compilations are built.
            var tree = await document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            SyntaxNode memberDecl = token.GetAncestor<MemberDeclarationSyntax>();

            // field or event field declarations may contain multiple variable declarators. Try finding the correct one.
            // If the position does not point to one, try using the first one.
            if (memberDecl != null &&
                (memberDecl.Kind() == SyntaxKind.FieldDeclaration || memberDecl.Kind() == SyntaxKind.EventFieldDeclaration))
            {
                SeparatedSyntaxList<VariableDeclaratorSyntax> variableDeclarators = ((BaseFieldDeclarationSyntax)memberDecl).Declaration.Variables;

                foreach (var declarator in variableDeclarators)
                {
                    if (declarator.FullSpan.Contains(token.FullSpan))
                    {
                        memberDecl = declarator;
                        break;
                    }
                }

                if (memberDecl == null)
                {
                    memberDecl = variableDeclarators.Count > 0 ? variableDeclarators[0] : null;
                }
            }

            if (memberDecl != null)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var memberSymbol = semanticModel.GetDeclaredSymbol(memberDecl, cancellationToken);

                if (memberSymbol != null)
                {
                    var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var lineNumber = text.Lines.GetLineFromPosition(position).LineNumber;

                    var accessor = token.GetAncestor<AccessorDeclarationSyntax>();
                    var memberLine = accessor == null
                        ? text.Lines.GetLineFromPosition(memberDecl.SpanStart).LineNumber
                        : text.Lines.GetLineFromPosition(accessor.SpanStart).LineNumber;

                    name = memberSymbol.ToDisplayString(s_nameFormat);
                    lineOffset = lineNumber - memberLine;
                    return new DebugLocationInfo(name, lineOffset);
                }
            }

            return default(DebugLocationInfo);
        }
    }
}
