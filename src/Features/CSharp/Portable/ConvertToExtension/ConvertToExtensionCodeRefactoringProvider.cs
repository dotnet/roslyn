// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToExtension;

using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToExtension), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ConvertToExtensionCodeRefactoringProvider() : CodeRefactoringProvider
{
    private readonly record struct ExtensionMethodInfo(
        ClassDeclarationSyntax ClassDeclaration,
        MethodDeclarationSyntax ExtensionMethod,
        IParameterSymbol FirstParameter,
        ImmutableArray<ITypeParameterSymbol> MethodTypeParameters)
    {
        public bool Equals(ExtensionMethodInfo info)
            => ExtensionMethodEqualityComparer.Instance.Equals(this, info);

        public override int GetHashCode()
            => ExtensionMethodEqualityComparer.Instance.GetHashCode(this);
    }

    internal override FixAllProvider? GetFixAllProvider()
        => new ConvertToExtensionFixAllProvider();

    private static ExtensionMethodInfo? TryGetExtensionMethodInfo(
        SemanticModel semanticModel,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        if (methodDeclaration.ParameterList.Parameters is not [var firstParameter, ..])
            return null;

        if (!firstParameter.Modifiers.Any(SyntaxKind.ThisKeyword))
            return null;

        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
            return null;

        if (!classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return null;

        if (classDeclaration.Parent is not BaseNamespaceDeclarationSyntax)
            return null;

        var firstParameterSymbol = semanticModel.GetRequiredDeclaredSymbol(firstParameter, cancellationToken);
        var methodSymbol = semanticModel.GetRequiredDeclaredSymbol(methodDeclaration, cancellationToken);

        // Gather the referenced method type parameters (in their original method order) in the extension method 'this'
        // parameter.  If method type parameters are used in that parameter, they must be the a prefix of the type
        // parameters of the method.
        using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var methodTypeParameters);
        firstParameterSymbol.Type.AddReferencedMethodTypeParameters(methodTypeParameters);

        methodTypeParameters.Sort(static (t1, t2) => t1.Ordinal - t2.Ordinal);
        for (var i = 0; i < methodTypeParameters.Count; i++)
        {
            var typeParameter = methodTypeParameters[i];
            if (typeParameter.Ordinal != i)
                return null;
        }

        return new(classDeclaration, methodDeclaration, firstParameterSymbol, methodTypeParameters.ToImmutableAndClear());
    }

    private static ImmutableDictionary<ExtensionMethodInfo, ImmutableArray<ExtensionMethodInfo>> GetAllExtensionMethods(
        SemanticModel semanticModel, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        var map = PooledDictionary<ExtensionMethodInfo, ArrayBuilder<ExtensionMethodInfo>>.GetInstance();

        foreach (var member in classDeclaration.Members)
        {
            if (member is not MethodDeclarationSyntax methodDeclaration)
                continue;

            var extensionInfo = TryGetExtensionMethodInfo(semanticModel, methodDeclaration, cancellationToken);
            if (extensionInfo == null)
                continue;

            map.MultiAdd(extensionInfo.Value, extensionInfo.Value);
        }

        return map.ToImmutableMultiDictionaryAndFree();
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var cancellationToken = context.CancellationToken;
        var semanticModel = await context.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methodDeclaration = await context.TryGetRelevantNodeAsync<MethodDeclarationSyntax>().ConfigureAwait(false);

        if (methodDeclaration != null)
        {
            var extensionInfo = TryGetExtensionMethodInfo(semanticModel, methodDeclaration, cancellationToken);
            if (extensionInfo == null)
                return;

            var allExtensionMethods = GetAllExtensionMethods(
                semanticModel, extensionInfo.Value.ClassDeclaration, cancellationToken);

            // Offer to change all the extension methods that match this particular parameter
            context.RegisterRefactoring(CodeAction.Create(
                string.Format(CSharpFeaturesResources.Convert_0_extension_methods_to_extension, extensionInfo.Value.FirstParameter.Type.ToDisplayString()),
                cancellationToken => ConvertToExtensionAsync(
                    context.Document, extensionInfo.Value.ClassDeclaration, allExtensionMethods, extensionInfo, cancellationToken),
                CSharpFeaturesResources.Convert_0_extension_methods_to_extension));
        }
        else
        {
            // Otherwise, if they're on a static class, which contains extension methods, offer to convert all of them.
            var classDeclaration = await context.TryGetRelevantNodeAsync<ClassDeclarationSyntax>().ConfigureAwait(false);
            if (classDeclaration != null)
            {
                var allExtensionMethods = GetAllExtensionMethods(
                    semanticModel, classDeclaration, cancellationToken);
                if (allExtensionMethods.IsEmpty)
                    return;

                context.RegisterRefactoring(CodeAction.Create(
                    string.Format(CSharpFeaturesResources.Convert_all_extension_methods_in_0_to_extension, classDeclaration.Identifier.ValueText),
                    cancellationToken => ConvertToExtensionAsync(
                        context.Document, classDeclaration, allExtensionMethods, specificExtension: null, cancellationToken),
                    CSharpFeaturesResources.Convert_all_extension_methods_in_0_to_extension));
            }
        }
    }

    private static async Task<Document> ConvertToExtensionAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        ImmutableDictionary<ExtensionMethodInfo, ImmutableArray<ExtensionMethodInfo>> allExtensionMethods,
        ExtensionMethodInfo? specificExtension,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(allExtensionMethods.IsEmpty);

        var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var newDeclaration = ConvertToExtension(
            codeGenerationService, classDeclaration, allExtensionMethods, specificExtension);

        var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Core function that the normal fix and the fix-all-provider call into to fixup one class declaration and the set
    /// of desired extension methods within that class declaration.  When called on an extension method itself, this
    /// will just be one extension method.  When called on a class declaration, this will be all the extension methods
    /// in that class.
    /// </summary>
    private static ClassDeclarationSyntax ConvertToExtension(
        ICodeGenerationService codeGenerationService,
        ClassDeclarationSyntax classDeclaration,
        ImmutableDictionary<ExtensionMethodInfo, ImmutableArray<ExtensionMethodInfo>> allExtensionMethods,
        ExtensionMethodInfo? specificExtension)
    {
        Contract.ThrowIfTrue(allExtensionMethods.IsEmpty);

        var classDeclarationEditor = new SyntaxEditor(classDeclaration, CSharpSyntaxGenerator.Instance);
        if (specificExtension != null)
        {
            var matchingExtensions = allExtensionMethods[specificExtension.Value];
            ConvertAndReplaceExtensions(matchingExtensions);
        }
        else
        {
            foreach (var (_, matchingExtensions) in allExtensionMethods)
                ConvertAndReplaceExtensions(matchingExtensions);
        }

        return (ClassDeclarationSyntax)classDeclarationEditor.GetChangedRoot();

        void ConvertAndReplaceExtensions(ImmutableArray<ExtensionMethodInfo> extensionMethods)
        {
            var newExtension = CreateExtension(extensionMethods);

            classDeclarationEditor.ReplaceNode(extensionMethods.First().ExtensionMethod, newExtension);

            foreach (var siblingExtension in extensionMethods.Skip(1))
                classDeclarationEditor.RemoveNode(siblingExtension.ExtensionMethod);
        }

        ExtensionDeclarationSyntax CreateExtension(ImmutableArray<ExtensionMethodInfo> group)
        {
            Contract.ThrowIfTrue(group.IsEmpty);

            var codeGenerationInfo = new CSharpCodeGenerationContextInfo(
                CodeGenerationContext.Default,
                CSharpCodeGenerationOptions.Default,
                (CSharpCodeGenerationService)codeGenerationService,
                LanguageVersionExtensions.CSharpNext);

            var firstExtensionInfo = group[0];
            var typeParameters = firstExtensionInfo.MethodTypeParameters.CastArray<ITypeParameterSymbol>();

            // Create a disconnected parameter.  This way when we look at it, we won't think of it as an extension method
            // parameter any more.  This will prevent us from undesirable things (like placing 'this' on it when adding to
            // the extension declaration).
            var firstParameter = CodeGenerationSymbolFactory.CreateParameterSymbol(firstExtensionInfo.FirstParameter);

            var extensionDeclaration = ExtensionDeclaration()
                .WithTypeParameterList(TypeParameterGenerator.GenerateTypeParameterList(typeParameters, codeGenerationInfo))
                .WithConstraintClauses(typeParameters.GenerateConstraintClauses())
                .WithParameterList(ParameterGenerator.GenerateParameterList([firstParameter], isExplicit: false, codeGenerationInfo))
                .WithMembers([.. group.Select(ConvertExtensionMethod)]);

            // Move the blank lines above the first extension method inside the extension to the extension itself.
            firstExtensionInfo.ExtensionMethod.GetNodeWithoutLeadingBlankLines(out var leadingBlankLines);
            return extensionDeclaration.WithLeadingTrivia(leadingBlankLines);
        }

        MethodDeclarationSyntax ConvertExtensionMethod(
            ExtensionMethodInfo extensionMethodInfo, int index)
        {
            using var _ = PooledHashSet<string>.GetInstance(out var typeParametersToRemove);

            var converted = extensionMethodInfo.ExtensionMethod
                .WithParameterList(ConvertParameters(extensionMethodInfo))
                .WithTypeParameterList(ConvertTypeParameters(extensionMethodInfo, typeParametersToRemove))
                .WithConstraintClauses(ConvertConstraintClauses(extensionMethodInfo, typeParametersToRemove));

            if (index == 0)
                converted = converted.GetNodeWithoutLeadingBlankLines();

            // Note: Formatting in this fashion is not desirable.  Ideally we would use
            // https://github.com/dotnet/roslyn/issues/59228 to just attach an indentation annotation to the extension
            // method to indent it instead.
            return converted.WithAdditionalAnnotations(Formatter.Annotation);
        }
    }

    private static ParameterListSyntax ConvertParameters(ExtensionMethodInfo extensionMethodInfo)
    {
        var extensionMethod = extensionMethodInfo.ExtensionMethod;

        // skip the first parameter, which is the 'this' parameter, and the comma that follows it.
        return extensionMethod.ParameterList.WithParameters(
            SeparatedList<ParameterSyntax>(extensionMethodInfo.ExtensionMethod.ParameterList.Parameters.GetWithSeparators().Skip(2)));
    }

    private static TypeParameterListSyntax? ConvertTypeParameters(
        ExtensionMethodInfo extensionMethodInfo,
        HashSet<string> typeParametersToRemove)
    {
        var extensionMethod = extensionMethodInfo.ExtensionMethod;

        // If the extension method wasn't generic, or we're not removing any type parameters, there's nothing to do.
        if (extensionMethod.TypeParameterList is null || typeParametersToRemove.Count == 0)
            return extensionMethod.TypeParameterList;

        // If we're removing all the type parameters, remove the type parameter list entirely.
        if (typeParametersToRemove.Count == extensionMethod.TypeParameterList.Parameters.Count)
            return null;

        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var newTypeParameters);
        var nodesAndTokens = extensionMethod.TypeParameterList.Parameters.GetWithSeparators();

        for (var i = 0; i < nodesAndTokens.Count; i += 2)
        {
            var typeParameter = (TypeParameterSyntax)nodesAndTokens[i]!;
            if (typeParametersToRemove.Contains(typeParameter.Identifier.ValueText))
                continue;

            // Add preceding comma if needed.  Note: this will always succeed as we can only have a prior
            // newTypeParameter if we hit a type parameter before us that we want to keep.
            if (newTypeParameters.Count > 0)
                newTypeParameters.Add(nodesAndTokens[i - 1]);

            newTypeParameters.Add(typeParameter);
        }

        return extensionMethod.TypeParameterList.WithParameters(SeparatedList<TypeParameterSyntax>(newTypeParameters));
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> ConvertConstraintClauses(
        ExtensionMethodInfo extensionMethodInfo,
        HashSet<string> typeParametersToRemove)
    {
        var extensionMethod = extensionMethodInfo.ExtensionMethod;

        // If the extension method had no constraints, or we're not removing any type parameters, there's nothing to do.
        if (extensionMethod.ConstraintClauses.Count == 0 || typeParametersToRemove.Count == 0)
            return extensionMethod.ConstraintClauses;

        return [.. extensionMethod.ConstraintClauses.Where(c => !typeParametersToRemove.Contains(c.Name.Identifier.ValueText))];
    }
}
