// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
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
            var tree = await document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();

            if (memberDeclaration == null)
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
                    if (declarator.FullSpan.Contains(token.FullSpan))
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

            var name = GetName(memberDeclaration, fieldDeclarator);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var lineNumber = text.Lines.GetLineFromPosition(position).LineNumber;
            var accessor = token.GetAncestor<AccessorDeclarationSyntax>();
            var memberLine = text.Lines.GetLineFromPosition(accessor?.SpanStart ?? memberDeclaration.SpanStart).LineNumber;
            var lineOffset = lineNumber - memberLine;

            return new DebugLocationInfo(name, lineOffset);
        }

        private static string GetName(MemberDeclarationSyntax memberDeclaration, VariableDeclaratorSyntax fieldDeclaratorOpt)
        {
            const string missingInformationPlaceholder = "?";

            // containing namespace(s) and type(s)
            ArrayBuilder<string> containingDeclarationNames = ArrayBuilder<string>.GetInstance();
            var containingDeclaration = memberDeclaration.Parent;
            while (containingDeclaration != null)
            {
                var @namespace = containingDeclaration as NamespaceDeclarationSyntax;
                if (@namespace != null)
                {
                    var syntax = @namespace.Name;
                    containingDeclarationNames.Add(syntax.IsMissing ? missingInformationPlaceholder : syntax.ToString());
                }
                else
                {
                    var type = containingDeclaration as TypeDeclarationSyntax;
                    if (type != null)
                    {
                        var token = type.GetNameToken();
                        containingDeclarationNames.Add(token.IsMissing ? missingInformationPlaceholder : token.Text);
                    }
                }
                containingDeclaration = containingDeclaration.Parent;
            }
            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            for (var i = containingDeclarationNames.Count - 1; i >= 0; i--)
            {
                builder.Append(containingDeclarationNames[i]);
                builder.Append('.');
            }
            containingDeclarationNames.Free();

            // simple name
            var nameToken = fieldDeclaratorOpt?.Identifier ?? memberDeclaration.GetNameToken();
            if (nameToken.IsMissing)
            {
                builder.Append(missingInformationPlaceholder);
            }
            else if (nameToken == default(SyntaxToken))
            {
                Debug.Assert(memberDeclaration.Kind() == SyntaxKind.ConversionOperatorDeclaration);
                builder.Append((memberDeclaration as ConversionOperatorDeclarationSyntax)?.Type);
            }
            else
            {
                if (memberDeclaration.Kind() == SyntaxKind.DestructorDeclaration)
                {
                    builder.Append('~');
                }
                builder.Append(nameToken.Text);
            }

            // parameter list (if any)
            builder.Append(memberDeclaration.GetParameterList());

            return pooled.ToStringAndFree();
        }
    }
}
