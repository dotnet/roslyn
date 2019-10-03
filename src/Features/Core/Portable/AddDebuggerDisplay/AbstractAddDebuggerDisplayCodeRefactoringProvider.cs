// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.AddDebuggerDisplay
{
    internal abstract class AbstractAddDebuggerDisplayCodeRefactoringProvider<TTypeDeclarationSyntax, TMethodDeclarationSyntax> : CodeRefactoringProvider
        where TTypeDeclarationSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
    {
        private const string DebuggerDisplayPrefix = "{";
        private const string DebuggerDisplayMethodName = "GetDebuggerDisplay";
        private const string DebuggerDisplaySuffix = "(),nq}";

        protected abstract bool CanNameofAccessNonPublicMembersFromAttributeArgument { get; }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var typeAndPriority =
                await GetRelevantTypeFromHeaderAsync(context).ConfigureAwait(false)
                ?? await GetRelevantTypeFromMethodAsync(context).ConfigureAwait(false);

            if (!(typeAndPriority is var (type, priority))) return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var debuggerAttributeTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Diagnostics.DebuggerDisplayAttribute");
            if (debuggerAttributeTypeSymbol is null) return;

            var typeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(type, context.CancellationToken);

            if (IsClassOrStruct(typeSymbol) && !HasDebuggerDisplayAttribute(typeSymbol, semanticModel.Compilation))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    priority,
                    FeaturesResources.Add_DebuggerDisplay,
                    cancellationToken => ApplyAsync(context.Document, type, debuggerAttributeTypeSymbol, cancellationToken)));
            }
        }

        private static async Task<(TTypeDeclarationSyntax type, CodeActionPriority priority)?> GetRelevantTypeFromHeaderAsync(CodeRefactoringContext context)
        {
            var type = await context.TryGetRelevantNodeAsync<TTypeDeclarationSyntax>().ConfigureAwait(false);

            if (type is null) return null;
            return (type, CodeActionPriority.Low);
        }

        private static async Task<(TTypeDeclarationSyntax type, CodeActionPriority priority)?> GetRelevantTypeFromMethodAsync(CodeRefactoringContext context)
        {
            var method = await context.TryGetRelevantNodeAsync<TMethodDeclarationSyntax>().ConfigureAwait(false);
            if (method != null)
            {
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                var methodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(method);

                var isDebuggerDisplayMethod = IsDebuggerDisplayMethod(methodSymbol);

                if (isDebuggerDisplayMethod || IsToStringMethod(methodSymbol))
                {
                    return (
                        method.FirstAncestorOrSelf<TTypeDeclarationSyntax>(),
                        isDebuggerDisplayMethod ? CodeActionPriority.Medium : CodeActionPriority.Low);
                }
            }

            return null;
        }

        private static bool IsToStringMethod(IMethodSymbol methodSymbol)
            => methodSymbol is
            {
                Arity: 0,
                Parameters: { IsEmpty: true },
                Name: nameof(ToString)
            };

        private static bool IsDebuggerDisplayMethod(IMethodSymbol methodSymbol)
            => methodSymbol is
            {
                Arity: 0,
                Parameters: { IsEmpty: true },
                Name: DebuggerDisplayMethodName
            };

        private static bool IsClassOrStruct(ITypeSymbol typeSymbol)
            => typeSymbol.TypeKind == TypeKind.Class || typeSymbol.TypeKind == TypeKind.Struct;

        private static bool HasDebuggerDisplayAttribute(ITypeSymbol typeSymbol, Compilation compilation)
        {
            return typeSymbol.GetAttributes()
                .Select(data => data.AttributeClass)
                .Contains(compilation.GetTypeByMetadataName("System.Diagnostics.DebuggerDisplayAttribute"));
        }

        private async Task<Document> ApplyAsync(Document document, TTypeDeclarationSyntax type, INamedTypeSymbol debuggerAttributeTypeSymbol, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var generator = SyntaxGenerator.GetGenerator(document);
            var editor = new SyntaxEditor(syntaxRoot, generator);

            var attributeArgument = CanNameofAccessNonPublicMembersFromAttributeArgument
                ? generator.AddExpression(
                    generator.AddExpression(
                        generator.LiteralExpression(DebuggerDisplayPrefix),
                        generator.NameOfExpression(generator.IdentifierName(DebuggerDisplayMethodName))),
                    generator.LiteralExpression(DebuggerDisplaySuffix))
                : generator.LiteralExpression(
                    DebuggerDisplayPrefix
                    + DebuggerDisplayMethodName
                    + DebuggerDisplaySuffix);

            var newAttribute = generator
                .Attribute(generator.TypeExpression(debuggerAttributeTypeSymbol), new[] { attributeArgument })
                .WithAdditionalAnnotations(
                    Simplifier.Annotation,
                    Simplifier.AddImportsAnnotation);

            editor.AddAttribute(type, newAttribute);

            var typeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(type, cancellationToken);

            if (!typeSymbol.GetMembers().OfType<IMethodSymbol>().Any(IsDebuggerDisplayMethod))
            {
                editor.AddMember(type,
                    generator.MethodDeclaration(
                        DebuggerDisplayMethodName,
                        returnType: generator.TypeExpression(SpecialType.System_String),
                        accessibility: Accessibility.Private,
                        statements: new[]
                        {
                            generator.ReturnStatement(generator.InvocationExpression(
                                generator.MemberAccessExpression(generator.ThisExpression(), generator.IdentifierName("ToString"))))
                        }));
            }

            syntaxRoot = editor.GetChangedRoot();

            return document.WithSyntaxRoot(syntaxRoot);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(CodeActionPriority priority, string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
                Priority = priority;
            }
        }
    }
}
