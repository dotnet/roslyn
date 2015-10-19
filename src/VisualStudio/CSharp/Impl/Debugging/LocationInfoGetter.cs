// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    internal static class LocationInfoGetter
    {
        internal static async Task<DebugLocationInfo> GetInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // PERF:  This method will be called synchronously on the UI thread for every breakpoint in the solution.
            // Therefore, it is important that we make this call as cheap as possible.  Rather than constructing a
            // containing Symbol and using ToDisplayString (which might be more *correct*), we'll just do the best we
            // can with Syntax.  This approach is capable of providing parity with the pre-Roslyn implementation.
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var memberDeclaration = syntaxFactsService.GetContainingMemberDeclaration(root, position, useFullSpan: true);

            // It might be reasonable to return an empty Name and a LineOffset from the beginning of the
            // file for GlobalStatements.  However, the only known caller (Breakpoints Window) doesn't
            // appear to consume this information, so we'll just return the simplest thing (no location).
            if ((memberDeclaration == null) || (memberDeclaration.Kind() == SyntaxKind.GlobalStatement))
            {
                return default(DebugLocationInfo);
            }

            // field or event field declarations may contain multiple variable declarators. Try finding the correct one.
            // If the position does not point to one, try using the first one.
            VariableDeclaratorSyntax fieldDeclarator = null;
            if (memberDeclaration.Kind() == SyntaxKind.FieldDeclaration || memberDeclaration.Kind() == SyntaxKind.EventFieldDeclaration)
            {
                SeparatedSyntaxList<VariableDeclaratorSyntax> variableDeclarators = ((BaseFieldDeclarationSyntax)memberDeclaration).Declaration.Variables;

                foreach (var declarator in variableDeclarators)
                {
                    if (declarator.FullSpan.Contains(position))
                    {
                        fieldDeclarator = declarator;
                        break;
                    }
                }

                if (fieldDeclarator == null)
                {
                    fieldDeclarator = variableDeclarators.Count > 0 ? variableDeclarators[0] : null;
                }
            }

            var name = syntaxFactsService.GetDisplayName(fieldDeclarator ?? memberDeclaration,
                DisplayNameOptions.IncludeNamespaces |
                DisplayNameOptions.IncludeParameters);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var lineNumber = text.Lines.GetLineFromPosition(position).LineNumber;
            var accessor = memberDeclaration.GetAncestorOrThis<AccessorDeclarationSyntax>();
            var memberLine = text.Lines.GetLineFromPosition(accessor?.SpanStart ?? memberDeclaration.SpanStart).LineNumber;
            var lineOffset = lineNumber - memberLine;

            return new DebugLocationInfo(name, lineOffset);
        }
    }
}
