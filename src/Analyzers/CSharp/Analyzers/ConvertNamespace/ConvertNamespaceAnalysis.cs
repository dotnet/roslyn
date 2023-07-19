// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    internal static class ConvertNamespaceAnalysis
    {
        public static (string title, string equivalenceKey) GetInfo(NamespaceDeclarationPreference preference)
            => preference switch
            {
                NamespaceDeclarationPreference.BlockScoped => (CSharpAnalyzersResources.Convert_to_block_scoped_namespace, nameof(CSharpAnalyzersResources.Convert_to_block_scoped_namespace)),
                NamespaceDeclarationPreference.FileScoped => (CSharpAnalyzersResources.Convert_to_file_scoped_namespace, nameof(CSharpAnalyzersResources.Convert_to_file_scoped_namespace)),
                _ => throw ExceptionUtilities.UnexpectedValue(preference),
            };

        public static bool CanOfferUseBlockScoped(CodeStyleOption2<NamespaceDeclarationPreference> option, [NotNullWhen(true)] BaseNamespaceDeclarationSyntax? declaration, bool forAnalyzer)
        {
            if (declaration is not FileScopedNamespaceDeclarationSyntax)
                return false;

            var userPrefersRegularNamespaces = option.Value == NamespaceDeclarationPreference.BlockScoped;
            var analyzerDisabled = option.Notification.Severity == ReportDiagnostic.Suppress;
            var forRefactoring = !forAnalyzer;

            // If the user likes regular namespaces, then we offer regular namespaces from the diagnostic analyzer.
            // If the user does not like regular namespaces then we offer regular namespaces bodies from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            var canOffer = userPrefersRegularNamespaces == forAnalyzer || (forRefactoring && analyzerDisabled);
            return canOffer;
        }

        internal static bool CanOfferUseFileScoped(CodeStyleOption2<NamespaceDeclarationPreference> option, CompilationUnitSyntax root, [NotNullWhen(true)] BaseNamespaceDeclarationSyntax? declaration, bool forAnalyzer)
            => CanOfferUseFileScoped(option, root, declaration, forAnalyzer, root.SyntaxTree.Options.LanguageVersion());

        internal static bool CanOfferUseFileScoped(
            CodeStyleOption2<NamespaceDeclarationPreference> option,
            CompilationUnitSyntax root,
            BaseNamespaceDeclarationSyntax? declaration,
            bool forAnalyzer,
            LanguageVersion version)
        {
            if (declaration is not NamespaceDeclarationSyntax namespaceDeclaration)
                return false;

            if (namespaceDeclaration.OpenBraceToken.IsMissing)
                return false;

            if (version < LanguageVersion.CSharp10)
                return false;

            var userPrefersFileScopedNamespaces = option.Value == NamespaceDeclarationPreference.FileScoped;
            var analyzerDisabled = option.Notification.Severity == ReportDiagnostic.Suppress;
            var forRefactoring = !forAnalyzer;

            // If the user likes file scoped namespaces, then we offer file scoped namespaces from the diagnostic analyzer.
            // If the user does not like file scoped namespaces then we offer file scoped namespaces from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            var canOffer = userPrefersFileScopedNamespaces == forAnalyzer || (forRefactoring && analyzerDisabled);
            if (!canOffer)
                return false;

            // even if we could offer this here, we have to make sure it would be legal.  A file scoped namespace is
            // only legal if it's the only namespace in the file and there are no top level statements.
            var tooManyNamespaces = root.DescendantNodesAndSelf(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                                        .OfType<BaseNamespaceDeclarationSyntax>()
                                        .Take(2)
                                        .Count() != 1;
            if (tooManyNamespaces)
                return false;

            if (root.Members.Any(m => m is GlobalStatementSyntax))
                return false;

            return true;
        }
    }
}
