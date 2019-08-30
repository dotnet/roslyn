// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
    {
        private partial class GenerateEqualsAndGetHashCodeAction : CodeAction
        {
            // https://docs.microsoft.com/dotnet/standard/design-guidelines/naming-parameters#naming-operator-overload-parameters
            //  DO use left and right for binary operator overload parameter names if there is no meaning to the parameters.
            private const string LeftName = "left";
            private const string RightName = "right";

            private readonly bool _generateEquals;
            private readonly bool _generateGetHashCode;
            private readonly bool _implementIEquatable;
            private readonly bool _generateOperators;
            private readonly Document _document;
            private readonly INamedTypeSymbol _containingType;
            private readonly ImmutableArray<ISymbol> _selectedMembers;
            private readonly TextSpan _textSpan;

            public GenerateEqualsAndGetHashCodeAction(
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> selectedMembers,
                bool generateEquals,
                bool generateGetHashCode,
                bool implementIEquatable,
                bool generateOperators)
            {
                _document = document;
                _containingType = containingType;
                _selectedMembers = selectedMembers;
                _textSpan = textSpan;
                _generateEquals = generateEquals;
                _generateGetHashCode = generateGetHashCode;
                _implementIEquatable = implementIEquatable;
                _generateOperators = generateOperators;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var methods = new List<IMethodSymbol>();

                if (_generateEquals)
                {
                    methods.Add(await CreateEqualsMethodAsync(cancellationToken).ConfigureAwait(false));
                }

                if (_implementIEquatable)
                {
                    methods.Add(await CreateIEquatableEqualsMethodAsync(cancellationToken).ConfigureAwait((bool)false));
                }

                if (_generateGetHashCode)
                {
                    methods.Add(await CreateGetHashCodeMethodAsync(cancellationToken).ConfigureAwait(false));
                }

                if (_generateOperators)
                {
                    await AddOperatorsAsync(methods, cancellationToken).ConfigureAwait(false);
                }

                var (oldType, newType) = await AddMethodsAsync(methods, cancellationToken).ConfigureAwait(false);

                if (_implementIEquatable)
                {
                    var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var equatableType = compilation.GetTypeByMetadataName(typeof(IEquatable<>).FullName);
                    var constructed = equatableType.Construct(_containingType);

                    var generator = _document.GetLanguageService<SyntaxGenerator>();

                    newType = generator.AddInterfaceType(newType,
                        generator.TypeExpression(constructed));
                }

                var newDocument = await UpdateDocumentAndAddImportsAsync(
                    oldType, newType, cancellationToken).ConfigureAwait(false);

                var service = _document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();
                var formattedDocument = await service.FormatDocumentAsync(
                    newDocument, cancellationToken).ConfigureAwait(false);

                return formattedDocument;
            }

            private async Task<Document> UpdateDocumentAndAddImportsAsync(SyntaxNode oldType, SyntaxNode newType, CancellationToken cancellationToken)
            {
                var oldRoot = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newDocument = _document.WithSyntaxRoot(
                    oldRoot.ReplaceNode(oldType, newType));

                var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst);

                var codeGenService = _document.GetLanguageService<ICodeGenerationService>();
                newDocument = await codeGenService.AddImportsAsync(
                    newDocument,
                    new CodeGenerationOptions(placeSystemNamespaceFirst: placeSystemNamespaceFirst),
                    cancellationToken).ConfigureAwait(false);
                return newDocument;
            }

            private async Task<(SyntaxNode oldType, SyntaxNode newType)> AddMethodsAsync(
                IList<IMethodSymbol> methods,
                CancellationToken cancellationToken)
            {
                var workspace = _document.Project.Solution.Workspace;
                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                var declarationService = _document.GetLanguageService<ISymbolDeclarationService>();
                var typeDeclaration = declarationService.GetDeclarations(_containingType)
                                                        .Select(r => r.GetSyntax(cancellationToken))
                                                        .First(s => s.FullSpan.IntersectsWith(_textSpan.Start));

                var newTypeDeclaration = CodeGenerator.AddMemberDeclarations(
                    typeDeclaration, methods, workspace,
                    new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(_textSpan)));

                return (typeDeclaration, newTypeDeclaration);
            }

            private async Task AddOperatorsAsync(List<IMethodSymbol> members, CancellationToken cancellationToken)
            {
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var generator = _document.GetLanguageService<SyntaxGenerator>();

                var parameters = ImmutableArray.Create(
                    CodeGenerationSymbolFactory.CreateParameterSymbol(_containingType, LeftName),
                    CodeGenerationSymbolFactory.CreateParameterSymbol(_containingType, RightName));

                members.Add(CreateEqualityOperator(compilation, generator, parameters));
                members.Add(CreateInequalityOperator(compilation, generator, parameters));
            }

            private IMethodSymbol CreateEqualityOperator(Compilation compilation, SyntaxGenerator generator, ImmutableArray<IParameterSymbol> parameters)
            {
                var expression = _containingType.IsValueType
                    ? generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.IdentifierName(LeftName),
                            generator.IdentifierName(EqualsName)),
                        generator.IdentifierName(RightName))
                    : generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.GetDefaultEqualityComparer(compilation, _containingType),
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
                    ImmutableArray.Create(generator.ReturnStatement(expression)));
            }

            private IMethodSymbol CreateInequalityOperator(Compilation compilation, SyntaxGenerator generator, ImmutableArray<IParameterSymbol> parameters)
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
                    ImmutableArray.Create(generator.ReturnStatement(expression)));
            }

            private Task<IMethodSymbol> CreateGetHashCodeMethodAsync(CancellationToken cancellationToken)
            {
                var service = _document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();
                return service.GenerateGetHashCodeMethodAsync(_document, _containingType, _selectedMembers, cancellationToken);
            }

            private Task<IMethodSymbol> CreateEqualsMethodAsync(CancellationToken cancellationToken)
            {
                var service = _document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();
                return _implementIEquatable
                    ? service.GenerateEqualsMethodThroughIEquatableEqualsAsync(_document, _containingType, cancellationToken)
                    : service.GenerateEqualsMethodAsync(_document, _containingType, _selectedMembers, cancellationToken);
            }

            private async Task<IMethodSymbol> CreateIEquatableEqualsMethodAsync(CancellationToken cancellationToken)
            {
                var service = _document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();
                return await service.GenerateIEquatableEqualsMethodAsync(
                    _document, _containingType, _selectedMembers, cancellationToken).ConfigureAwait(false);
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
}
