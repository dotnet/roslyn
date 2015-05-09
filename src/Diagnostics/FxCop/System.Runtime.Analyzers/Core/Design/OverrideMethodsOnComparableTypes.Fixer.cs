// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace System.Runtime.Analyzers
{
    public abstract class OverrideMethodsOnComparableTypesFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(OverrideMethodsOnComparableTypesAnalyzer.RuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var generator = SyntaxGenerator.GetGenerator(context.Document);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var declaration = root.FindNode(context.Span);
            declaration = generator.GetDeclaration(declaration);
            if (declaration == null)
            {
                return;
            }

            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var typeSymbol = model.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
            if (typeSymbol?.TypeKind != TypeKind.Class &&
                typeSymbol?.TypeKind != TypeKind.Struct)
            {
                return;
            }

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();

            context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.ImplementComparable,
                                                     async ct => await ImplementComparable(context.Document, declaration, typeSymbol, ct).ConfigureAwait(false)),
                                    diagnostic);
        }

        protected abstract SyntaxNode GenerateOperatorDeclaration(SyntaxNode returnType, string operatorName, IEnumerable<SyntaxNode> parameters, SyntaxNode notImplementedStatement);

        private async Task<Document> ImplementComparable(Document document, SyntaxNode declaration, INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            if (!typeSymbol.DoesOverrideEquals())
            {
                var equalsMethod = generator.MethodDeclaration(WellKnownMemberNames.ObjectEquals,
                                        new[] { generator.ParameterDeclaration("obj", generator.TypeExpression(SpecialType.System_Object)) },
                                        returnType: generator.TypeExpression(SpecialType.System_Boolean),
                                        accessibility: Accessibility.Public,
                                        modifiers: DeclarationModifiers.Override,
                                        statements: new[] { generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException"))) });
                editor.AddMember(declaration, equalsMethod);
            }

            if (!typeSymbol.DoesOverrideGetHashCode())
            {
                var getHashCodeMethod = generator.MethodDeclaration(WellKnownMemberNames.ObjectGetHashCode,
                                            returnType: generator.TypeExpression(SpecialType.System_Int32),
                                            accessibility: Accessibility.Public,
                                            modifiers: DeclarationModifiers.Override,
                                            statements: new[] { generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException"))) });
                editor.AddMember(declaration, getHashCodeMethod);
            }

            if (!typeSymbol.IsOperatorImplemented(WellKnownMemberNames.EqualityOperatorName))
            {
                var equalityOperator = GenerateOperatorDeclaration(generator.TypeExpression(SpecialType.System_Boolean),
                                                                   WellKnownMemberNames.EqualityOperatorName,
                                                                   new[]
                                                                   {
                                                                       generator.ParameterDeclaration("left", generator.TypeExpression(typeSymbol)),
                                                                       generator.ParameterDeclaration("right", generator.TypeExpression(typeSymbol)),
                                                                   },
                                                                   generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException"))));
                editor.AddMember(declaration, equalityOperator);
            }

            if (!typeSymbol.IsOperatorImplemented(WellKnownMemberNames.InequalityOperatorName))
            {
                var inequalityOperator = GenerateOperatorDeclaration(generator.TypeExpression(SpecialType.System_Boolean),
                                                                   WellKnownMemberNames.InequalityOperatorName,
                                                                   new[]
                                                                   {
                                                                       generator.ParameterDeclaration("left", generator.TypeExpression(typeSymbol)),
                                                                       generator.ParameterDeclaration("right", generator.TypeExpression(typeSymbol)),
                                                                   },
                                                                   generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException"))));
                editor.AddMember(declaration, inequalityOperator);
            }

            if (!typeSymbol.IsOperatorImplemented(WellKnownMemberNames.LessThanOperatorName))
            {
                var lessThanOperator = GenerateOperatorDeclaration(generator.TypeExpression(SpecialType.System_Boolean),
                                                                   WellKnownMemberNames.LessThanOperatorName,
                                                                   new[]
                                                                   {
                                                                       generator.ParameterDeclaration("left", generator.TypeExpression(typeSymbol)),
                                                                       generator.ParameterDeclaration("right", generator.TypeExpression(typeSymbol)),
                                                                   },
                                                                   generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException"))));
                editor.AddMember(declaration, lessThanOperator);
            }

            if (!typeSymbol.IsOperatorImplemented(WellKnownMemberNames.GreaterThanOperatorName))
            {
                var greaterThanOperator = GenerateOperatorDeclaration(generator.TypeExpression(SpecialType.System_Boolean),
                                                                   WellKnownMemberNames.GreaterThanOperatorName,
                                                                   new[]
                                                                   {
                                                                       generator.ParameterDeclaration("left", generator.TypeExpression(typeSymbol)),
                                                                       generator.ParameterDeclaration("right", generator.TypeExpression(typeSymbol)),
                                                                   },
                                                                   generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException"))));
                editor.AddMember(declaration, greaterThanOperator);
            }

            return editor.GetChangedDocument();
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
