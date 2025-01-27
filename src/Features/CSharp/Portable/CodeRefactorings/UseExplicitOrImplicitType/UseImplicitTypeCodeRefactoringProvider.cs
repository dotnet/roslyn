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

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseImplicitType), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class UseImplicitTypeCodeRefactoringProvider() : AbstractUseTypeCodeRefactoringProvider
{
    protected override string Title
        => CSharpAnalyzersResources.Use_implicit_type;

    protected override TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        => CSharpUseImplicitTypeHelper.Instance.FindAnalyzableType(node, semanticModel, cancellationToken);

    protected override TypeStyleResult AnalyzeTypeName(TypeSyntax typeName, SemanticModel semanticModel, CSharpSimplifierOptions options, CancellationToken cancellationToken)
        => CSharpUseImplicitTypeHelper.Instance.AnalyzeTypeName(typeName, semanticModel, options, cancellationToken);

    protected override Task HandleDeclarationAsync(Document document, SyntaxEditor editor, TypeSyntax type, CancellationToken cancellationToken)
    {
        UseImplicitTypeCodeFixProvider.ReplaceTypeWithVar(editor, type);
        return Task.CompletedTask;
    }
}
