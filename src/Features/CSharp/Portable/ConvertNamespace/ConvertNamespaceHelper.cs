// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    internal static class ConvertNamespaceHelper
    {
        internal static bool CanOfferUseRegular(OptionSet optionSet, BaseNamespaceDeclarationSyntax declaration, bool forAnalyzer)
        {
            if (declaration is not FileScopedNamespaceDeclarationSyntax)
                return false;

            var currentOptionValue = optionSet.GetOption(CSharpCodeStyleOptions.PreferFileScopedNamespace);
            var preference = currentOptionValue.Value;
            var userPrefersRegularNamespaces = preference == false;
            var analyzerDisabled = currentOptionValue.Notification.Severity == ReportDiagnostic.Suppress;

            // If the user likes regular namespaces, then we offer regular namespaces from the diagnostic analyzer.
            // If the user does not like regular namespaces then we offer regular namespaces bodies from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            var canOffer = userPrefersRegularNamespaces == forAnalyzer || (!forAnalyzer && analyzerDisabled);
            return canOffer;
        }

        internal static bool CanOfferUseFileScoped(OptionSet optionSet, CompilationUnitSyntax root, BaseNamespaceDeclarationSyntax declaration, bool forAnalyzer)
        {
            if (declaration is not NamespaceDeclarationSyntax namespaceDeclaration)
                return false;

            if (namespaceDeclaration.OpenBraceToken.IsMissing)
                return false;

            var currentOptionValue = optionSet.GetOption(CSharpCodeStyleOptions.PreferFileScopedNamespace);
            var preference = currentOptionValue.Value;
            var userPrefersFileScopedNamespaces = preference == true;
            var analyzerDisabled = currentOptionValue.Notification.Severity == ReportDiagnostic.Suppress;

            // If the user likes file scoped namespaces, then we offer file scopedregular namespaces from the diagnostic analyzer.
            // If the user does not like file scoped namespaces then we offer file scoped namespaces from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            var canOffer = userPrefersFileScopedNamespaces == forAnalyzer || (!forAnalyzer && analyzerDisabled);
            if (!canOffer)
                return false;

            // even if we could offer this here, we have to make sure it would be legal.  A file scoped namespace is
            // only legal if it's the only namespace in the file and there are no top level statements.
            var tooManyNamespaces = root.DescendantNodesAndSelf(n => n is CompilationUnitSyntax || n is BaseNamespaceDeclarationSyntax)
                                        .OfType<BaseNamespaceDeclarationSyntax>()
                                        .Take(2)
                                        .Count() != 1;
            if (tooManyNamespaces)
                return false;

            if (root.Members.Any(m => m is GlobalStatementSyntax))
                return false;

            return true;
        }

        public static async Task<Document> ConvertAsync(
            Document document, BaseNamespaceDeclarationSyntax baseNamespace, CancellationToken cancellationToken)
        {
            var converted = Convert(baseNamespace);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(root.ReplaceNode(baseNamespace, converted));
        }

        public static BaseNamespaceDeclarationSyntax Convert(BaseNamespaceDeclarationSyntax baseNamespace)
        {
            return baseNamespace switch
            {
                FileScopedNamespaceDeclarationSyntax fileScopedNamespace => ConvertFileScopedNamespace(fileScopedNamespace),
                NamespaceDeclarationSyntax namespaceDeclaration => ConvertNamespaceDeclaration(namespaceDeclaration),
                _ => throw ExceptionUtilities.UnexpectedValue(baseNamespace.Kind()),
            };
        }

        private static FileScopedNamespaceDeclarationSyntax ConvertNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
        {
            var semicolon = SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(namespaceDeclaration.OpenBraceToken.TrailingTrivia);
            var firstBodyToken = namespaceDeclaration.OpenBraceToken.GetNextToken();
            if (firstBodyToken != namespaceDeclaration.CloseBraceToken)
            {
                if (!firstBodyToken.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia))
                    semicolon = semicolon.WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
            }

            return SyntaxFactory.FileScopedNamespaceDeclaration(
                namespaceDeclaration.AttributeLists,
                namespaceDeclaration.Modifiers,
                namespaceDeclaration.NamespaceKeyword,
                namespaceDeclaration.Name,
                semicolon,
                namespaceDeclaration.Externs,
                namespaceDeclaration.Usings,
                namespaceDeclaration.Members).WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static NamespaceDeclarationSyntax ConvertFileScopedNamespace(FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
        {
            return SyntaxFactory.NamespaceDeclaration(
                fileScopedNamespace.AttributeLists,
                fileScopedNamespace.Modifiers,
                fileScopedNamespace.NamespaceKeyword,
                fileScopedNamespace.Name,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(fileScopedNamespace.SemicolonToken.TrailingTrivia),
                fileScopedNamespace.Externs,
                fileScopedNamespace.Usings,
                fileScopedNamespace.Members,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default).WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
