// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.AddDebuggerDisplay
{
    internal abstract class AbstractAddDebuggerDisplayCodeRefactoringProvider<
        TTypeDeclarationSyntax,
        TMethodDeclarationSyntax> : CodeRefactoringProvider
        where TTypeDeclarationSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
    {
        private const string DebuggerDisplayPrefix = "{";
        private const string DebuggerDisplayMethodName = "GetDebuggerDisplay";
        private const string DebuggerDisplaySuffix = "(),nq}";

        protected abstract bool CanNameofAccessNonPublicMembersFromAttributeArgument { get; }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var typeAndPriority =
                await GetRelevantTypeFromHeaderAsync(context).ConfigureAwait(false) ??
                await GetRelevantTypeFromMethodAsync(context).ConfigureAwait(false);

            if (typeAndPriority == null)
                return;

            var (type, priority) = typeAndPriority.Value;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            var debuggerAttributeTypeSymbol = compilation.GetTypeByMetadataName("System.Diagnostics.DebuggerDisplayAttribute");
            if (debuggerAttributeTypeSymbol is null)
                return;

            var typeSymbol = (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(type, context.CancellationToken);

            if (!IsClassOrStruct(typeSymbol))
                return;

            if (HasDebuggerDisplayAttribute(typeSymbol, compilation))
                return;

            context.RegisterRefactoring(new MyCodeAction(
                priority,
                FeaturesResources.Add_DebuggerDisplay_attribute,
                c => ApplyAsync(document, type, debuggerAttributeTypeSymbol, c)));
        }

        private static async Task<(TTypeDeclarationSyntax type, CodeActionPriority priority)?> GetRelevantTypeFromHeaderAsync(CodeRefactoringContext context)
        {
            var type = await context.TryGetRelevantNodeAsync<TTypeDeclarationSyntax>().ConfigureAwait(false);
            if (type is null)
                return null;

            return (type, CodeActionPriority.Low);
        }

        private static async Task<(TTypeDeclarationSyntax type, CodeActionPriority priority)?> GetRelevantTypeFromMethodAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var method = await context.TryGetRelevantNodeAsync<TMethodDeclarationSyntax>().ConfigureAwait(false);
            if (method == null)
                return null;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(method, cancellationToken);

            var isDebuggerDisplayMethod = IsDebuggerDisplayMethod(methodSymbol);
            if (!isDebuggerDisplayMethod && !IsToStringMethod(methodSymbol))
                return null;

            // Show the feature if we're on a ToString or GetDebuggerDisplay method. For the former,
            // have this be low-pri so we don't override more important light-bulb options.
            var typeDecl = method.FirstAncestorOrSelf<TTypeDeclarationSyntax>();
            if (typeDecl == null)
                return null;

            var priority = isDebuggerDisplayMethod ? CodeActionPriority.Medium : CodeActionPriority.Low;
            return (typeDecl, priority);
        }

        private static bool IsToStringMethod(IMethodSymbol methodSymbol)
            => methodSymbol is { Name: nameof(ToString), Arity: 0, Parameters: { IsEmpty: true } };

        private static bool IsDebuggerDisplayMethod(IMethodSymbol methodSymbol)
            => methodSymbol is { Name: DebuggerDisplayMethodName, Arity: 0, Parameters: { IsEmpty: true } };

        private static bool IsClassOrStruct(ITypeSymbol typeSymbol)
            => typeSymbol.TypeKind == TypeKind.Class || typeSymbol.TypeKind == TypeKind.Struct;

        private static bool HasDebuggerDisplayAttribute(ITypeSymbol typeSymbol, Compilation compilation)
            => typeSymbol.GetAttributes()
                .Select(data => data.AttributeClass)
                .Contains(compilation.GetTypeByMetadataName("System.Diagnostics.DebuggerDisplayAttribute"));

        private async Task<Document> ApplyAsync(Document document, TTypeDeclarationSyntax type, INamedTypeSymbol debuggerAttributeTypeSymbol, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(syntaxRoot, document.Project.Solution.Workspace);
            var generator = editor.Generator;

            var attributeArgument = CanNameofAccessNonPublicMembersFromAttributeArgument
                ? generator.AddExpression(
                    generator.AddExpression(
                        generator.LiteralExpression(DebuggerDisplayPrefix),
                        generator.NameOfExpression(generator.IdentifierName(DebuggerDisplayMethodName))),
                    generator.LiteralExpression(DebuggerDisplaySuffix))
                : generator.LiteralExpression(
                    DebuggerDisplayPrefix + DebuggerDisplayMethodName + DebuggerDisplaySuffix);

            var newAttribute = generator
                .Attribute(generator.TypeExpression(debuggerAttributeTypeSymbol), new[] { attributeArgument })
                .WithAdditionalAnnotations(
                    Simplifier.Annotation,
                    Simplifier.AddImportsAnnotation);

            editor.AddAttribute(type, newAttribute);

            var typeSymbol = (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(type, cancellationToken);

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
                                generator.MemberAccessExpression(
                                    generator.ThisExpression(),
                                    generator.IdentifierName("ToString"))))
                        }));
            }

            return document.WithSyntaxRoot(editor.GetChangedRoot());
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
