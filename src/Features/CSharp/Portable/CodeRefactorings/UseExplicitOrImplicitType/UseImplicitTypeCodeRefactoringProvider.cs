// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseType;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseImplicitType), Shared]
    internal partial class UseImplicitTypeCodeRefactoringProvider : AbstractUseTypeCodeRefactoringProvider
    {
        [ImportingConstructor]
        public UseImplicitTypeCodeRefactoringProvider()
        {
        }

        protected override string Title
            => CSharpFeaturesResources.Use_implicit_type;

        protected override TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            => CSharpUseImplicitTypeHelper.Instance.FindAnalyzableType(node, semanticModel, cancellationToken);

        protected override TypeStyleResult AnalyzeTypeName(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            => CSharpUseImplicitTypeHelper.Instance.AnalyzeTypeName(typeName, semanticModel, optionSet, cancellationToken);

        protected override Task HandleDeclarationAsync(Document document, SyntaxEditor editor, SyntaxNode node, CancellationToken cancellationToken)
        {
            UseImplicitTypeCodeFixProvider.ReplaceTypeWithVar(editor, node);
            return Task.CompletedTask;
        }
    }
}
