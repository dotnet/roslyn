// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace System.Runtime.Analyzers
{
    public abstract class UseOrdinalStringComparisonFixerBase : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseOrdinalStringComparisonAnalyzer.RuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxFactoryService = SyntaxGenerator.GetGenerator(context.Document);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();

            if (IsInArgumentContext(node))
            {
                // StringComparison.CurrentCulture => StringComparison.Ordinal
                // StringComparison.CurrentCultureIgnoreCase => StringComparison.OrdinalIgnoreCase
                context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.StringComparisonShouldBeOrdinalOrOrdinalIgnoreCase,
                                                         async ct => await FixArgument(context.Document, syntaxFactoryService, root, node).ConfigureAwait(false)),
                                                    diagnostic);
            }
            else if (IsInIdentifierNameContext(node))
            {
                // string.Equals(a, b) => string.Equals(a, b, StringComparison.Ordinal)
                // string.Compare(a, b) => string.Compare(a, b, StringComparison.Ordinal)
                context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.StringComparisonShouldBeOrdinalOrOrdinalIgnoreCase,
                                                         async ct => await FixIdentifierName(context.Document, syntaxFactoryService, root, node, context.CancellationToken).ConfigureAwait(false)),
                                                    diagnostic);
            }
            else if (IsInEqualsContext(node))
            {
                // "a == b" => "string.Equals(a, b, StringComparison.Ordinal)"
                // "a != b" => "!string.Equals(a, b, StringComparison.Ordinal)"
                context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.StringComparisonShouldBeOrdinalOrOrdinalIgnoreCase,
                                async ct => await FixEquals(context.Document, syntaxFactoryService, root, node, context.CancellationToken).ConfigureAwait(false)),
                        diagnostic);
            }
        }

        protected abstract bool IsInArgumentContext(SyntaxNode node);
        protected abstract Task<Document> FixArgument(Document document, SyntaxGenerator syntaxFactoryService, SyntaxNode root, SyntaxNode argument);

        protected abstract bool IsInIdentifierNameContext(SyntaxNode node);
        protected abstract Task<Document> FixIdentifierName(Document document, SyntaxGenerator syntaxFactoryService, SyntaxNode root, SyntaxNode identifier, CancellationToken cancellationToken);

        protected abstract bool IsInEqualsContext(SyntaxNode node);
        protected abstract Task<Document> FixEquals(Document document, SyntaxGenerator syntaxFactoryService, SyntaxNode root, SyntaxNode node, CancellationToken cancellationToken);

        internal SyntaxNode CreateEqualsExpression(SyntaxGenerator syntaxFactoryService, SemanticModel model, SyntaxNode operand1, SyntaxNode operand2, bool isEquals)
        {
            var stringType = model.Compilation.GetSpecialType(SpecialType.System_String);
            var memberAccess = syntaxFactoryService.MemberAccessExpression(
                        syntaxFactoryService.TypeExpression(stringType),
                        syntaxFactoryService.IdentifierName(UseOrdinalStringComparisonAnalyzer.EqualsMethodName));
            var ordinal = CreateOrdinalMemberAccess(syntaxFactoryService, model);
            var invocation = syntaxFactoryService.InvocationExpression(
                memberAccess,
                operand1,
                operand2.WithoutTrailingTrivia(),
                ordinal)
                .WithAdditionalAnnotations(Formatter.Annotation);
            if (!isEquals)
            {
                invocation = syntaxFactoryService.LogicalNotExpression(invocation);
            }

            invocation = invocation.WithTrailingTrivia(operand2.GetTrailingTrivia());

            return invocation;
        }

        internal SyntaxNode CreateOrdinalMemberAccess(SyntaxGenerator syntaxFactoryService, SemanticModel model)
        {
            var stringComparisonType = WellKnownTypes.StringComparison(model.Compilation);
            return syntaxFactoryService.MemberAccessExpression(
                syntaxFactoryService.TypeExpression(stringComparisonType),
                syntaxFactoryService.IdentifierName(UseOrdinalStringComparisonAnalyzer.OrdinalText));
        }

        protected bool CanAddStringComparison(IMethodSymbol methodSymbol, SemanticModel model)
        {
            if (WellKnownTypes.StringComparison(model.Compilation) == null)
            {
                return false;
            }

            var parameters = methodSymbol.Parameters;
            switch (methodSymbol.Name)
            {
                case UseOrdinalStringComparisonAnalyzer.EqualsMethodName:
                    // can fix .Equals() with (string), (string, string)
                    switch (parameters.Length)
                    {
                        case 1:
                            return parameters[0].Type.SpecialType == SpecialType.System_String;
                        case 2:
                            return parameters[0].Type.SpecialType == SpecialType.System_String &&
                                parameters[1].Type.SpecialType == SpecialType.System_String;
                    }

                    break;
                case UseOrdinalStringComparisonAnalyzer.CompareMethodName:
                    // can fix .Compare() with (string, string), (string, int, string, int, int)
                    switch (parameters.Length)
                    {
                        case 2:
                            return parameters[0].Type.SpecialType == SpecialType.System_String &&
                                parameters[1].Type.SpecialType == SpecialType.System_String;
                        case 5:
                            return parameters[0].Type.SpecialType == SpecialType.System_String &&
                                parameters[1].Type.SpecialType == SpecialType.System_Int32 &&
                                parameters[2].Type.SpecialType == SpecialType.System_String &&
                                parameters[3].Type.SpecialType == SpecialType.System_Int32 &&
                                parameters[4].Type.SpecialType == SpecialType.System_Int32;
                    }

                    break;
            }

            return false;
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
