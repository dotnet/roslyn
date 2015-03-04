// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA2231: Overload operator equals on overriding ValueType.Equals
    /// </summary>
    public abstract class OverloadOperatorEqualsOnOverridingValueTypeEqualsFixer : CodeFixProvider
    {
        protected const string LeftName = "left";
        protected const string RightName = "right";
        protected const string NotImplementedExceptionName = "System.NotImplementedException";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(OverloadOperatorEqualsOnOverridingValueTypeEqualsAnalyzer.RuleId);

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
            if (typeSymbol == null)
            {
                return;
            }

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();

            context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.OverloadOperatorEqualsOnOverridingValueTypeEquals,
                                                     async ct => await ImplementOperatorEquals(context.Document, declaration, typeSymbol, ct).ConfigureAwait(false)),
                                    diagnostic);
        }

        protected abstract SyntaxNode GenerateOperatorDeclaration(SyntaxNode returnType, string operatorName, IEnumerable<SyntaxNode> parameters, SyntaxNode notImplementedStatement);

        private async Task<Document> ImplementOperatorEquals(Document document, SyntaxNode declaration, INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

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
