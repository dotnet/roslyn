﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
    {
        private partial class GenerateEqualsAndGetHashCodeAction : CodeAction
        {
            private static readonly SyntaxAnnotation s_specializedFormattingAnnotation = new SyntaxAnnotation();

            private readonly bool _generateEquals;
            private readonly bool _generateGetHashCode;
            private readonly bool _implementIEquatable;
            private readonly bool _generateOperators;
            private readonly GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider _service;
            private readonly Document _document;
            private readonly INamedTypeSymbol _containingType;
            private readonly ImmutableArray<ISymbol> _selectedMembers;
            private readonly TextSpan _textSpan;

            public GenerateEqualsAndGetHashCodeAction(
                GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> selectedMembers,
                bool generateEquals,
                bool generateGetHashCode,
                bool implementIEquatable,
                bool generateOperators)
            {
                _service = service;
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
                    methods.Add(await CreateIEquatableEqualsMethodAsync((CancellationToken)cancellationToken).ConfigureAwait((bool)false));
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

                var formattedDocument = await FormatDocumentAsync(
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

            private async Task<Document> FormatDocumentAsync(Document newDocument, CancellationToken cancellationToken)
            {
                var rules = new List<IFormattingRule> { new FormatLargeBinaryExpressionRule(_document.GetLanguageService<ISyntaxFactsService>()) };
                rules.AddRange(Formatter.GetDefaultFormattingRules(_document));

                var formattedDocument = await Formatter.FormatAsync(
                    newDocument, s_specializedFormattingAnnotation,
                    options: null, rules: rules, cancellationToken: cancellationToken).ConfigureAwait(false);
                return formattedDocument;
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

                var localName = _containingType.GetLocalName();
                var left = localName + "1";
                var right = localName + "2";

                var parameters = ImmutableArray.Create(
                    CodeGenerationSymbolFactory.CreateParameterSymbol(_containingType, left),
                    CodeGenerationSymbolFactory.CreateParameterSymbol(_containingType, right));

                members.Add(CreateEqualityOperator(compilation, generator, left, right, parameters));
                members.Add(CreateInequalityOperator(compilation, generator, left, right, parameters));
            }

            private IMethodSymbol CreateEqualityOperator(Compilation compilation, SyntaxGenerator generator, string left, string right, ImmutableArray<IParameterSymbol> parameters)
            {
                var expression = _containingType.IsValueType
                    ? generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.IdentifierName(left),
                            generator.IdentifierName(EqualsName)),
                        generator.IdentifierName(right))
                    : generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.GetDefaultEqualityComparer(compilation, _containingType),
                            generator.IdentifierName(EqualsName)),
                        generator.IdentifierName(left),
                        generator.IdentifierName(right));

                return CodeGenerationSymbolFactory.CreateOperatorSymbol(
                    default,
                    Accessibility.Public,
                    new DeclarationModifiers(isStatic: true),
                    compilation.GetSpecialType(SpecialType.System_Boolean),
                    CodeGenerationOperatorKind.Equality,
                    parameters,
                    ImmutableArray.Create(generator.ReturnStatement(expression)));
            }

            private IMethodSymbol CreateInequalityOperator(Compilation compilation, SyntaxGenerator generator, string left, string right, ImmutableArray<IParameterSymbol> parameters)
            {
                var expression = generator.LogicalNotExpression(
                    generator.ValueEqualsExpression(
                        generator.IdentifierName(left),
                        generator.IdentifierName(right)));

                return CodeGenerationSymbolFactory.CreateOperatorSymbol(
                    default,
                    Accessibility.Public,
                    new DeclarationModifiers(isStatic: true),
                    compilation.GetSpecialType(SpecialType.System_Boolean),
                    CodeGenerationOperatorKind.Inequality,
                    parameters,
                    ImmutableArray.Create(generator.ReturnStatement(expression)));
            }

            private async Task<IMethodSymbol> CreateGetHashCodeMethodAsync(CancellationToken cancellationToken)
            {
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return _document.GetLanguageService<SyntaxGenerator>().CreateGetHashCodeMethod(
                    compilation, _containingType, _selectedMembers, cancellationToken);
            }

            private async Task<IMethodSymbol> CreateEqualsMethodAsync(CancellationToken cancellationToken)
            {
                if (_implementIEquatable)
                {
                    return await ImplementEqualsThroughIEqutableEqualsAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    return _document.GetLanguageService<SyntaxGenerator>().CreateEqualsMethod(
                        compilation, _containingType, _selectedMembers,
                        s_specializedFormattingAnnotation, cancellationToken);
                }
            }

            private async Task<IMethodSymbol> CreateIEquatableEqualsMethodAsync(CancellationToken cancellationToken)
            {
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return _document.GetLanguageService<SyntaxGenerator>().CreateIEqutableEqualsMethod(
                    compilation, _containingType, _selectedMembers,
                    s_specializedFormattingAnnotation, cancellationToken);
            }

            private async Task<IMethodSymbol> ImplementEqualsThroughIEqutableEqualsAsync(CancellationToken cancellationToken)
            {
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var generator = _document.GetLanguageService<SyntaxGenerator>();

                var expressions = ArrayBuilder<SyntaxNode>.GetInstance();
                var objName = generator.IdentifierName("obj");
                if (_containingType.IsValueType)
                {
                    // return obj is T && this.Equals((T)obj);
                    expressions.Add(generator.IsTypeExpression(objName, _containingType));
                    expressions.Add(
                        generator.InvocationExpression(
                            generator.MemberAccessExpression(
                                generator.ThisExpression(),
                                generator.IdentifierName(nameof(Equals))),
                            generator.CastExpression(_containingType, objName)));
                }
                else
                {
                    // return this.Equals(obj as T);
                    expressions.Add(
                        generator.InvocationExpression(
                            generator.MemberAccessExpression(
                                generator.ThisExpression(),
                                generator.IdentifierName(nameof(Equals))),
                            generator.TryCastExpression(objName, _containingType)));
                }

                var statement = generator.ReturnStatement(
                    expressions.Aggregate(generator.LogicalAndExpression));

                expressions.Free();
                return compilation.CreateEqualsMethod(
                    ImmutableArray.Create(statement));
            }

            private async Task<IMethodSymbol> CreateIEqutableEqualsMethodAsync(CancellationToken cancellationToken)
            {
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return _document.GetLanguageService<SyntaxGenerator>().CreateIEqutableEqualsMethod(
                    compilation, _containingType, _selectedMembers,
                    s_specializedFormattingAnnotation, cancellationToken);
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
