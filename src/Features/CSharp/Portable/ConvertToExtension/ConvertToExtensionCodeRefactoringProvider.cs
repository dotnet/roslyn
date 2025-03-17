// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToExtension;

using FixAllScope = CodeAnalysis.CodeFixes.FixAllScope;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToExtension), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ConvertToExtensionCodeRefactoringProvider() : CodeRefactoringProvider
{
    private readonly record struct ExtensionMethodInfo(
        MethodDeclarationSyntax ExtensionMethod,
        IParameterSymbol FirstParameter,
        ImmutableArray<ITypeParameterSymbol> MethodTypeParameters);

    internal override FixAllProvider? GetFixAllProvider()
        => new ConvertToExtensionFixAllProvider();

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        // If the user is on an extension method, offer to convert it to a modern extension.
        var methodDeclaration = await context.TryGetRelevantNodeAsync<MethodDeclarationSyntax>().ConfigureAwait(false);
        if (methodDeclaration != null)
        {
            if (IsExtensionMethod(methodDeclaration, out var classDeclaration))
            {
                await ComputeRefactoringsAsync(
                    context, classDeclaration, [methodDeclaration],
                    CSharpFeaturesResources.Convert_extension_method_to_extension,
                    nameof(CSharpFeaturesResources.Convert_extension_method_to_extension)).ConfigureAwait(false);
            }

            return;
        }
        else
        {
            // Otherwise, if they're on a static class, which contains extension methods, offer to convert all of them.
            var classDeclaration = await context.TryGetRelevantNodeAsync<ClassDeclarationSyntax>().ConfigureAwait(false);
            if (classDeclaration != null)
            {
                await ComputeRefactoringsAsync(
                    context, classDeclaration, GetExtensionMethods(classDeclaration),
                    CSharpFeaturesResources.Convert_all_extension_methods_to_extension,
                    nameof(CSharpFeaturesResources.Convert_all_extension_methods_to_extension)).ConfigureAwait(false);
            }
        }
    }

    private static bool IsExtensionMethod(
        MethodDeclarationSyntax methodDeclaration,
        [NotNullWhen(true)] out ClassDeclarationSyntax? classDeclaration)
    {
        classDeclaration = null;
        if (methodDeclaration.ParameterList.Parameters is not [var firstParameter, ..])
            return false;

        if (!firstParameter.Modifiers.Any(SyntaxKind.ThisKeyword))
            return false;

        classDeclaration = methodDeclaration.Parent as ClassDeclarationSyntax;
        return classDeclaration != null;
    }

    private static ImmutableArray<MethodDeclarationSyntax> GetExtensionMethods(ClassDeclarationSyntax classDeclaration)
        => classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) && classDeclaration.Parent is BaseNamespaceDeclarationSyntax
            ? [.. classDeclaration.Members.OfType<MethodDeclarationSyntax>().Where(m => IsExtensionMethod(m, out _))]
            : [];

    private async Task ComputeRefactoringsAsync(
        CodeRefactoringContext context,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<MethodDeclarationSyntax> extensionMethods,
        string title,
        string equivalenceKey)
    {
        if (extensionMethods.IsEmpty)
            return;

        context.RegisterRefactoring(CodeAction.Create(
            title,
            cancellationToken => ConvertToExtensionAsync(context.Document, classDeclaration, extensionMethods, cancellationToken),
            equivalenceKey));
    }

    private async Task<Document> ConvertToExtensionAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<MethodDeclarationSyntax> extensionMethods,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(extensionMethods.IsEmpty);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var newDeclaration = await ConvertToExtensionAsync(
            semanticModel, classDeclaration, extensionMethods, cancellationToken).ConfigureAwait(false);

        var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<ClassDeclarationSyntax> ConvertToExtensionAsync(
        SemanticModel semanticModel,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<MethodDeclarationSyntax> extensionMethods,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(extensionMethods.IsEmpty);
        var extensionMethodInfos = extensionMethods
            .Select(extensionMethod =>
            {
                var firstParameter = semanticModel.GetRequiredDeclaredSymbol(extensionMethod.ParameterList.Parameters[0], cancellationToken);
                using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var methodTypeParameters);
                return new ExtensionMethodInfo(
                    extensionMethod, firstParameter, [.. methodTypeParameters.OrderBy(t => t.Name)]);
            });
        var groups = extensionMethodInfos.GroupBy(x => x, ExtensionMethodEqualityComparer.Instance);

        foreach (var group in groups.OrderBy(g => g.Min(info => info.ExtensionMethod.SpanStart)))
        {
            var extensionParameter = group.Key.FirstParameter;

        }
    }

    private sealed class ExtensionMethodEqualityComparer :
        IEqualityComparer<AttributeData>,
        IEqualityComparer<ITypeParameterSymbol>,
        IEqualityComparer<ExtensionMethodInfo>
    {
        public static readonly ExtensionMethodEqualityComparer Instance = new();

        private static readonly SymbolEquivalenceComparer s_equivalenceComparer = new(
            assemblyComparer: null,
            distinguishRefFromOut: true,
            // `void Goo(this (int x, int y) tuple)` doesn't match `void Goo(this (int a, int b) tuple)
            tupleNamesMustMatch: true,
            // `void Goo(this string? x)` doesn't matches `void Goo(this string x)`
            ignoreNullableAnnotations: false,
            // `void Goo(this object x)` doesn't matches `void Goo(this dynamic x)`
            objectAndDynamicCompareEqually: false,
            // `void Goo(this string[] x)` doesn't matches `void Goo(this Span<string> x)`
            arrayAndReadOnlySpanCompareEqually: false);

        #region IEqualityComparer<AttributeData>

        private bool AttributesMatch(ImmutableArray<AttributeData> attributes1, ImmutableArray<AttributeData> attributes2)
            => attributes1.SequenceEqual(attributes2, this);

        public bool Equals(AttributeData? x, AttributeData? y)
        {
            if (x == y)
                return true;

            if (x is null || y is null)
                return false;

            if (!Equals(x.AttributeClass, y.AttributeClass))
                return false;

            return x.ConstructorArguments.SequenceEqual(y.ConstructorArguments) &&
                   x.NamedArguments.SequenceEqual(y.NamedArguments);
        }

        // Not needed as we never group by attributes.  We only SequenceEqual compare them.
        public int GetHashCode([DisallowNull] AttributeData obj)
            => throw ExceptionUtilities.Unreachable();

        #endregion

        #region IEqualityComparer<ITypeParameterSymbol>

        public bool Equals(ITypeParameterSymbol? x, ITypeParameterSymbol? y)
        {
            if (x == y)
                return true;

            if (x is null || y is null)
                return false;

            // Names must match as the code in the extension methods may reference the type parameters by name and has
            // to continue working.
            if (x.Name != y.Name)
                return false;

            // Attributes have to match as we're moving these type parameters up to the extension itself.
            if (!AttributesMatch(x.GetAttributes(), y.GetAttributes()))
                return false;

            // Constraints have to match as we're moving these type parameters up to the extension itself.
            if (x.HasConstructorConstraint != y.HasConstructorConstraint)
                return false;

            if (x.HasNotNullConstraint != y.HasNotNullConstraint)
                return false;

            if (x.HasReferenceTypeConstraint != y.HasReferenceTypeConstraint)
                return false;

            if (x.HasUnmanagedTypeConstraint != y.HasUnmanagedTypeConstraint)
                return false;

            if (x.HasValueTypeConstraint != y.HasValueTypeConstraint)
                return false;

            // Constraints have to match as we're moving these type parameters up to the extension itself.
            // We again 
            if (!x.ConstraintTypes.SequenceEqual(y.ConstraintTypes, s_equivalenceComparer.SignatureTypeEquivalenceComparer))
                return false;

            return true;
        }

        // Not needed as we never group by type parameters.  We only SequenceEqual compare them.
        public int GetHashCode([DisallowNull] ITypeParameterSymbol obj)
            => throw ExceptionUtilities.Unreachable();

        #endregion

        #region IEqualityComparer<ExtensionMethodInfo>

        public bool Equals(ExtensionMethodInfo x, ExtensionMethodInfo y)
        {
            // For us to consider two extension methods to be equivalent, they must have a first parameter that we
            // consider equal, any method type parameters they use must have the same constraints, and they must have
            // the same attributes on them.  
            //
            // Note: The initial check will ensure that the same method-type-parameters are used in both methods *when
            // compared by type parameter ordinal*.  The MethodTypeParameterMatch will then check that the type
            // parameters that we would lift to the extension method would be considered the same as well.
            return s_equivalenceComparer.ParameterEquivalenceComparer.Equals(x.FirstParameter, y.FirstParameter) &&
                AttributesMatch(x.FirstParameter.GetAttributes(), y.FirstParameter.GetAttributes()) &&
                x.MethodTypeParameters.SequenceEqual(y.MethodTypeParameters, this);
        }

        public int GetHashCode(ExtensionMethodInfo obj)
            // Loosely match any extension methods if they have the same first parameter type (treating method type
            // parameters by ordinal) and same name.  We'll do a more full match in .Equals above.
            => s_equivalenceComparer.ParameterEquivalenceComparer.GetHashCode(obj.FirstParameter) ^ obj.FirstParameter.Name.GetHashCode();

        #endregion
    }

    private sealed class ConvertToExtensionFixAllProvider()
        : DocumentBasedFixAllProvider(
            [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution, FixAllScope.ContainingType])
    {
        protected override async Task<Document?> FixAllAsync(
            FixAllContext fixAllContext,
            Document document,
            Optional<ImmutableArray<TextSpan>> fixAllSpans)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            foreach (var declaration in GetTopLevelClassDeclarations(root, fixAllSpans))
            {
                var extensionMethods = GetExtensionMethods(declaration);
                if (extensionMethods.IsEmpty)
                    continue;

                var newDeclaration = await ConvertToExtensionAsync(
                    semanticModel, declaration, extensionMethods, cancellationToken).ConfigureAwait(false);
                editor.ReplaceNode(declaration, newDeclaration);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private static IEnumerable<ClassDeclarationSyntax> GetTopLevelClassDeclarations(
            SyntaxNode root, Optional<ImmutableArray<TextSpan>> fixAllSpans)
        {
            if (!fixAllSpans.HasValue)
            {
                // Processing the whole file.  Return all top level classes in the file.
                return root
                    .DescendantNodes(descendIntoChildren: n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                    .OfType<ClassDeclarationSyntax>();
            }
            else
            {
                // User selected 'fix all in containing type'.  Core code refactoring engine will return the spans
                // of the containing class
                return fixAllSpans.Value
                    .Select(span => root.FindNode(span) as ClassDeclarationSyntax)
                    .WhereNotNull();
            }
        }
    }
}
