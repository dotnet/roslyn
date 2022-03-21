// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter), Shared]
    [ExtensionOrder(Before = nameof(CSharpAddParameterCheckCodeRefactoringProvider))]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.Wrapping)]
    internal class CSharpInitializeMemberFromParameterCodeRefactoringProvider :
        AbstractInitializeMemberFromParameterCodeRefactoringProvider<
            BaseTypeDeclarationSyntax,
            ParameterSyntax,
            StatementSyntax,
            ExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpInitializeMemberFromParameterCodeRefactoringProvider()
        {
        }

        protected override ISyntaxFacts SyntaxFacts
            => CSharpSyntaxFacts.Instance;

        protected override bool SupportsRecords(ParseOptions options)
            => false;

        protected override bool IsFunctionDeclaration(SyntaxNode node)
            => InitializeParameterHelpers.IsFunctionDeclaration(node);

        protected override SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement)
            => InitializeParameterHelpers.TryGetLastStatement(blockStatement);

        protected override void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, returnsVoid, statementToAddAfter, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);

        // Fields are always private by default in C#.
        protected override Accessibility DetermineDefaultFieldAccessibility(INamedTypeSymbol containingType)
            => Accessibility.Private;

        // Properties are always private by default in C#.
        protected override Accessibility DetermineDefaultPropertyAccessibility()
            => Accessibility.Private;

        protected override SyntaxNode GetBody(SyntaxNode functionDeclaration)
            => InitializeParameterHelpers.GetBody(functionDeclaration);
    }
}
