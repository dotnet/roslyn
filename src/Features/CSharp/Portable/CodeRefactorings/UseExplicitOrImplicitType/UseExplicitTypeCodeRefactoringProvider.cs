// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseType;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseExplicitType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseExplicitType), Shared]
    internal class UseExplicitTypeCodeRefactoringProvider : AbstractUseTypeCodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExplicitTypeCodeRefactoringProvider()
        {
        }

        protected override string Title
            => CSharpAnalyzersResources.Use_explicit_type;

        protected override TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            => CSharpUseExplicitTypeHelper.Instance.FindAnalyzableType(node, semanticModel, cancellationToken);

        protected override TypeStyleResult AnalyzeTypeName(TypeSyntax typeName, SemanticModel semanticModel, CSharpSimplifierOptions options, CancellationToken cancellationToken)
            => CSharpUseExplicitTypeHelper.Instance.AnalyzeTypeName(typeName, semanticModel, options, cancellationToken);

        protected override Task HandleDeclarationAsync(Document document, SyntaxEditor editor, TypeSyntax node, CancellationToken cancellationToken)
            => UseExplicitTypeCodeFixProvider.HandleDeclarationAsync(document, editor, node, cancellationToken);
    }
}
