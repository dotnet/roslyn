// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
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
                bool generateOperators)
            {
                _service = service;
                _document = document;
                _containingType = containingType;
                _selectedMembers = selectedMembers;
                _textSpan = textSpan;
                _generateEquals = generateEquals;
                _generateGetHashCode = generateGetHashCode;
                _generateOperators = generateOperators;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var members = new List<ISymbol>();

                if (_generateEquals)
                {
                    var member = await CreateEqualsMethodAsync(cancellationToken).ConfigureAwait(false);
                    members.Add(member);
                }

                if (_generateGetHashCode)
                {
                    var member = await CreateGetHashCodeMethodAsync(cancellationToken).ConfigureAwait(false);
                    members.Add(member);
                }

                if (_generateOperators)
                {
                    await AddOperatorsAsync(members, cancellationToken).ConfigureAwait(false);
                }

                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                var newDocument = await CodeGenerator.AddMemberDeclarationsAsync(
                    _document.Project.Solution,
                    _containingType,
                    members,
                    new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(_textSpan)),
                    cancellationToken).ConfigureAwait(false);

                var rules = new List<IFormattingRule> { new FormatLargeBinaryExpressionRule(_document.GetLanguageService<ISyntaxFactsService>()) };
                rules.AddRange(Formatter.GetDefaultFormattingRules(_document));

                var formattedDocument = await Formatter.FormatAsync(
                    newDocument, s_specializedFormattingAnnotation,
                    options: null, rules: rules, cancellationToken: cancellationToken).ConfigureAwait(false);

                return formattedDocument;
            }

            private async Task AddOperatorsAsync(List<ISymbol> members, CancellationToken cancellationToken)
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
                    default(ImmutableArray<AttributeData>),
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
                    default(ImmutableArray<AttributeData>),
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
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return _document.GetLanguageService<SyntaxGenerator>().CreateEqualsMethod(
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