// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;

internal partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
{
    private partial class GenerateEqualsAndGetHashCodeAction(
        Document document,
        SyntaxNode typeDeclaration,
        INamedTypeSymbol containingType,
        ImmutableArray<ISymbol> selectedMembers,
        CleanCodeGenerationOptionsProvider fallbackOptions,
        bool generateEquals,
        bool generateGetHashCode,
        bool implementIEquatable,
        bool generateOperators) : CodeAction
    {
        // https://docs.microsoft.com/dotnet/standard/design-guidelines/naming-parameters#naming-operator-overload-parameters
        //  DO use left and right for binary operator overload parameter names if there is no meaning to the parameters.
        private const string LeftName = "left";
        private const string RightName = "right";

        private readonly bool _generateEquals = generateEquals;
        private readonly bool _generateGetHashCode = generateGetHashCode;
        private readonly bool _implementIEquatable = implementIEquatable;
        private readonly bool _generateOperators = generateOperators;
        private readonly Document _document = document;
        private readonly SyntaxNode _typeDeclaration = typeDeclaration;
        private readonly INamedTypeSymbol _containingType = containingType;
        private readonly ImmutableArray<ISymbol> _selectedMembers = selectedMembers;
        private readonly CleanCodeGenerationOptionsProvider _fallbackOptions = fallbackOptions;

        public override string EquivalenceKey => Title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var methods);

            if (_generateEquals)
            {
                methods.Add(await CreateEqualsMethodAsync(cancellationToken).ConfigureAwait(false));
            }

            var constructedTypeToImplement = await GetConstructedTypeToImplementAsync(cancellationToken).ConfigureAwait(false);

            if (constructedTypeToImplement is object)
            {
                methods.Add(await CreateIEquatableEqualsMethodAsync(constructedTypeToImplement, cancellationToken).ConfigureAwait(false));
            }

            if (_generateGetHashCode)
            {
                methods.Add(await CreateGetHashCodeMethodAsync(cancellationToken).ConfigureAwait(false));
            }

            if (_generateOperators)
            {
                await AddOperatorsAsync(methods, cancellationToken).ConfigureAwait(false);
            }

            var info = await _document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, _fallbackOptions, cancellationToken).ConfigureAwait(false);
            var formattingOptions = await _document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

            var newTypeDeclaration = info.Service.AddMembers(_typeDeclaration, methods, info, cancellationToken);

            if (constructedTypeToImplement is object)
            {
                var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();

                newTypeDeclaration = generator.AddInterfaceType(newTypeDeclaration,
                    generator.TypeExpression(constructedTypeToImplement));
            }

            var newDocument = await UpdateDocumentAndAddImportsAsync(
                _typeDeclaration, newTypeDeclaration, cancellationToken).ConfigureAwait(false);

            var service = _document.GetRequiredLanguageService<IGenerateEqualsAndGetHashCodeService>();
            var formattedDocument = await service.FormatDocumentAsync(
                newDocument, formattingOptions, cancellationToken).ConfigureAwait(false);

            return formattedDocument;
        }

        private async Task<INamedTypeSymbol?> GetConstructedTypeToImplementAsync(CancellationToken cancellationToken)
        {
            if (!_implementIEquatable)
                return null;

            var semanticModel = await _document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var equatableType = semanticModel.Compilation.GetTypeByMetadataName(typeof(IEquatable<>).FullName!);
            if (equatableType == null)
                return null;

            var useNullableTypeArgument =
                !_containingType.IsValueType
                && semanticModel.GetNullableContext(_typeDeclaration.SpanStart).AnnotationsEnabled();

            return useNullableTypeArgument
                ? equatableType.Construct(_containingType.WithNullableAnnotation(NullableAnnotation.Annotated))
                : equatableType.Construct(_containingType);
        }

        private async Task<Document> UpdateDocumentAndAddImportsAsync(SyntaxNode oldType, SyntaxNode newType, CancellationToken cancellationToken)
        {
            var oldRoot = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newDocument = _document.WithSyntaxRoot(oldRoot.ReplaceNode(oldType, newType));
            var addImportOptions = await _document.GetAddImportPlacementOptionsAsync(_fallbackOptions, cancellationToken).ConfigureAwait(false);

            newDocument = await ImportAdder.AddImportsFromSymbolAnnotationAsync(newDocument, addImportOptions, cancellationToken).ConfigureAwait(false);
            return newDocument;
        }

        private async Task AddOperatorsAsync(ArrayBuilder<IMethodSymbol> members, CancellationToken cancellationToken)
        {
            var compilation = await _document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
            var generatorInternal = _document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

            // add nullable annotation to the parameter reference type, so that (in)equality operator implementations allow comparison against null
            var parameters = ImmutableArray.Create(
                CodeGenerationSymbolFactory.CreateParameterSymbol(_containingType.IsValueType ? _containingType : _containingType.WithNullableAnnotation(NullableAnnotation.Annotated), LeftName),
                CodeGenerationSymbolFactory.CreateParameterSymbol(_containingType.IsValueType ? _containingType : _containingType.WithNullableAnnotation(NullableAnnotation.Annotated), RightName));

            members.Add(CreateEqualityOperator(compilation, generator, generatorInternal, parameters));
            members.Add(CreateInequalityOperator(compilation, generator, parameters));
        }

        private IMethodSymbol CreateEqualityOperator(
            Compilation compilation,
            SyntaxGenerator generator,
            SyntaxGeneratorInternal generatorInternal,
            ImmutableArray<IParameterSymbol> parameters)
        {
            var expression = _containingType.IsValueType
                ? generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.IdentifierName(LeftName),
                        generator.IdentifierName(EqualsName)),
                    generator.IdentifierName(RightName))
                : generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.GetDefaultEqualityComparer(generatorInternal, compilation, _containingType),
                        generator.IdentifierName(EqualsName)),
                    generator.IdentifierName(LeftName),
                    generator.IdentifierName(RightName));

            return CodeGenerationSymbolFactory.CreateOperatorSymbol(
                default,
                Accessibility.Public,
                new DeclarationModifiers(isStatic: true),
                compilation.GetSpecialType(SpecialType.System_Boolean),
                CodeGenerationOperatorKind.Equality,
                parameters,
                [generator.ReturnStatement(expression)]);
        }

        private static IMethodSymbol CreateInequalityOperator(Compilation compilation, SyntaxGenerator generator, ImmutableArray<IParameterSymbol> parameters)
        {
            var expression = generator.LogicalNotExpression(
                generator.ValueEqualsExpression(
                    generator.IdentifierName(LeftName),
                    generator.IdentifierName(RightName)));

            return CodeGenerationSymbolFactory.CreateOperatorSymbol(
                default,
                Accessibility.Public,
                new DeclarationModifiers(isStatic: true),
                compilation.GetSpecialType(SpecialType.System_Boolean),
                CodeGenerationOperatorKind.Inequality,
                parameters,
                [generator.ReturnStatement(expression)]);
        }

        private Task<IMethodSymbol> CreateGetHashCodeMethodAsync(CancellationToken cancellationToken)
        {
            var service = _document.GetRequiredLanguageService<IGenerateEqualsAndGetHashCodeService>();
            return service.GenerateGetHashCodeMethodAsync(_document, _containingType, _selectedMembers, cancellationToken);
        }

        private Task<IMethodSymbol> CreateEqualsMethodAsync(CancellationToken cancellationToken)
        {
            var service = _document.GetRequiredLanguageService<IGenerateEqualsAndGetHashCodeService>();
            return _implementIEquatable
                ? service.GenerateEqualsMethodThroughIEquatableEqualsAsync(_document, _containingType, cancellationToken)
                : service.GenerateEqualsMethodAsync(_document, _containingType, _selectedMembers, cancellationToken);
        }

        private async Task<IMethodSymbol> CreateIEquatableEqualsMethodAsync(INamedTypeSymbol constructedEquatableType, CancellationToken cancellationToken)
        {
            var service = _document.GetRequiredLanguageService<IGenerateEqualsAndGetHashCodeService>();
            return await service.GenerateIEquatableEqualsMethodAsync(
                _document, _containingType, _selectedMembers, constructedEquatableType, cancellationToken).ConfigureAwait(false);
        }

        public override string Title
            => GetTitle(_generateEquals, _generateGetHashCode);

        internal static string GetTitle(bool generateEquals, bool generateGetHashCode)
            => generateEquals
                ? generateGetHashCode
                    ? FeaturesResources.Generate_Equals_and_GetHashCode
                    : FeaturesResources.Generate_Equals_object
                : FeaturesResources.Generate_GetHashCode;
    }
}
