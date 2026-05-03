// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty;

internal abstract class AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider<TPropertyDeclarationNode, TTypeDeclarationNode, TCodeGenerationContextInfo> : CodeRefactoringProvider
    where TPropertyDeclarationNode : SyntaxNode
    where TTypeDeclarationNode : SyntaxNode
    where TCodeGenerationContextInfo : CodeGenerationContextInfo
{
    protected abstract Task<string> GetFieldNameAsync(Document document, IPropertySymbol propertySymbol, CancellationToken cancellationToken);
    protected abstract (SyntaxNode newGetAccessor, SyntaxNode? newSetAccessor) GetNewAccessors(
        TCodeGenerationContextInfo info, TPropertyDeclarationNode property, string fieldName, CancellationToken cancellationToken);
    protected abstract TPropertyDeclarationNode GetPropertyWithoutInitializer(TPropertyDeclarationNode property);
    protected abstract SyntaxNode GetInitializerValue(TPropertyDeclarationNode property);
    protected abstract SyntaxNode ConvertPropertyToExpressionBodyIfDesired(TCodeGenerationContextInfo info, SyntaxNode fullProperty);
    protected abstract SyntaxNode GetTypeBlock(SyntaxNode syntaxNode);
    protected abstract Task<Document> ExpandToFieldPropertyAsync(Document document, TPropertyDeclarationNode property, CancellationToken cancellationToken);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var property = await GetPropertyAsync(context).ConfigureAwait(false);
        if (property == null)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetDeclaredSymbol(property) is not IPropertySymbol propertySymbol)
            return;

        if (!IsValidAutoProperty(propertySymbol))
            return;

        context.RegisterRefactoring(CodeAction.Create(
                FeaturesResources.Convert_to_full_property,
                cancellationToken => ExpandToFullPropertyAsync(document, property, propertySymbol, cancellationToken),
                nameof(FeaturesResources.Convert_to_full_property)),
            property.Span);

        // If supported, offer to convert auto-prop to use 'field' instead.
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        if (syntaxFacts.SupportsFieldExpression(semanticModel.SyntaxTree.Options) &&
            !property.DescendantNodes().Any(syntaxFacts.IsFieldExpression))
        {
            context.RegisterRefactoring(CodeAction.Create(
                    FeaturesResources.Convert_to_field_property,
                    cancellationToken => ExpandToFieldPropertyAsync(document, property, cancellationToken),
                    nameof(FeaturesResources.Convert_to_field_property)),
                property.Span);
        }
    }

    internal static bool IsValidAutoProperty(IPropertySymbol propertySymbol)
    {
        var fields = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>();
        var field = fields.FirstOrDefault(f => propertySymbol.Equals(f.AssociatedSymbol));
        return field != null;
    }

    private static async Task<TPropertyDeclarationNode?> GetPropertyAsync(CodeRefactoringContext context)
    {
        var containingProperty = await context.TryGetRelevantNodeAsync<TPropertyDeclarationNode>().ConfigureAwait(false);
        if (containingProperty?.Parent is not TTypeDeclarationNode)
            return null;

        return containingProperty;
    }

    private async Task<Document> ExpandToFullPropertyAsync(
        Document document,
        TPropertyDeclarationNode property,
        IPropertySymbol propertySymbol,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(document.DocumentState.ParseOptions);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var editor = new SyntaxEditor(root, document.Project.Solution.Services);
        var info = (TCodeGenerationContextInfo)await document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, cancellationToken).ConfigureAwait(false);

        // Create full property. If the auto property had an initial value
        // we need to remove it and later add it to the backing field
        var fieldName = await GetFieldNameAsync(document, propertySymbol, cancellationToken).ConfigureAwait(false);
        var (newGetAccessor, newSetAccessor) = GetNewAccessors(info, property, fieldName, cancellationToken);

        var finalProperty = CreateFinalProperty(
            document, GetPropertyWithoutInitializer(property), info, newGetAccessor, newSetAccessor);
        editor.ReplaceNode(property, finalProperty);

        // add backing field, plus initializer if it exists 
        var newField = CodeGenerationSymbolFactory.CreateFieldSymbol(
            default, Accessibility.Private,
            DeclarationModifiers.From(propertySymbol),
            propertySymbol.Type, fieldName,
            initializer: GetInitializerValue(property));

        var typeDeclaration = propertySymbol.ContainingType.DeclaringSyntaxReferences;
        foreach (var td in typeDeclaration)
        {
            var typeBlock = GetTypeBlock(await td.GetSyntaxAsync(cancellationToken).ConfigureAwait(false));
            if (property.Ancestors().Contains(typeBlock))
            {
                editor.ReplaceNode(
                    typeBlock,
                    (currentTypeDeclaration, _) => info.Service.AddField(currentTypeDeclaration, newField, info, cancellationToken));
            }
        }

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }

    protected SyntaxNode CreateFinalProperty(
        Document document,
        TPropertyDeclarationNode property,
        TCodeGenerationContextInfo info,
        SyntaxNode newGetAccessor,
        SyntaxNode? newSetAccessor)
    {
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

        var fullProperty = generator
            .WithAccessorDeclarations(
                property,
                newSetAccessor == null
                    ? [newGetAccessor]
                    : [newGetAccessor, newSetAccessor])
            .WithLeadingTrivia(property.GetLeadingTrivia());
        fullProperty = ConvertPropertyToExpressionBodyIfDesired(info, fullProperty);
        return fullProperty.WithAdditionalAnnotations(Formatter.Annotation);
    }
}
