// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA2213: Disposable fields should be disposed
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public class DisposableFieldsShouldBeDisposedFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DisposableFieldsShouldBeDisposedAnalyzer.RuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var declaration = root.FindNode(context.Span);
            if (declaration == null)
            {
                return;
            }

            // find a Dispose method
            var iDisposableType = model.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            var iDisposable_Dispose = (iDisposableType?.GetMembers(DisposableFieldsShouldBeDisposedAnalyzer.Dispose))?.Single();
            if (iDisposableType == null || iDisposable_Dispose == null)
            {
                return;
            }

            var fieldSymbol = model.GetDeclaredSymbol(declaration, context.CancellationToken) as IFieldSymbol;
            var disposeMethod = fieldSymbol?.ContainingType?.FindImplementationForInterfaceMember(iDisposable_Dispose) as IMethodSymbol;
            if (disposeMethod == null)
            {
                return;
            }

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();

            context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.DisposableFieldsShouldBeDisposed,
                                                     async ct => await AddDisposeCall(context.Document, declaration, fieldSymbol, disposeMethod, iDisposableType, iDisposable_Dispose, ct).ConfigureAwait(false)),
                                    diagnostic);
        }

        private async Task<Document> AddDisposeCall(Document document, SyntaxNode declaration, IFieldSymbol fieldSymbol, IMethodSymbol disposeMethod, ITypeSymbol iDisposableType, ISymbol iDisposable_Dispose, CancellationToken cancellationToken)
        {
            SymbolEditor editor = SymbolEditor.Create(document);
            await editor.EditOneDeclarationAsync(disposeMethod, (docEditor, disposeMethodNode) => 
            {
                var generator = docEditor.Generator;
                // handle the case where a local in the Dispose method exists with the same name by generating this (or ClassName) and simplifying it
                var path = fieldSymbol.IsStatic
                                ? generator.IdentifierName(fieldSymbol.ContainingType.MetadataName)
                                : generator.ThisExpression();

                var disposeMethodOfFieldType = fieldSymbol.Type.FindImplementationForInterfaceMember(iDisposable_Dispose) as IMethodSymbol;

                // If the original interface was implemented implicitly then we can simply invoke through the name of the implementation.
                // Note that in VB, the name of the method implementing the interface method can be different from the interface method name.
                SyntaxNode statement;
                if (disposeMethodOfFieldType.CanBeReferencedByName)
                {
                    statement =
                        generator.ExpressionStatement(
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(
                                    generator.MemberAccessExpression(path, generator.IdentifierName(fieldSymbol.Name)).WithAdditionalAnnotations(Simplifier.Annotation),
                                        generator.IdentifierName(disposeMethodOfFieldType.Name))));
                }
                // Otherwise dispatch through the interface
                else
                {
                    statement =
                        generator.ExpressionStatement(
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(
                                    generator.CastExpression(iDisposableType, 
                                        generator.MemberAccessExpression(path, generator.IdentifierName(fieldSymbol.Name)).WithAdditionalAnnotations(Simplifier.Annotation)),
                                            generator.IdentifierName(DisposableFieldsShouldBeDisposedAnalyzer.Dispose))));
                }

                var body = generator.GetStatements(disposeMethodNode);
                docEditor.SetStatements(disposeMethodNode, body.Concat(ImmutableArray.Create(statement)));
            }).ConfigureAwait(false);

            return editor.GetChangedDocuments().First();
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
