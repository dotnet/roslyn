// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1001: Types that own disposable fields should be disposable
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public class TypesThatOwnDisposableFieldsShouldBeDisposableFixer : CodeFixProvider
    {
        protected const string NotImplementedExceptionName = "System.NotImplementedException";
        protected const string IDisposableName = "System.IDisposable";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TypesThatOwnDisposableFieldsShouldBeDisposableAnalyzer.RuleId);

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var generator = SyntaxGenerator.GetGenerator(context.Document);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var declaration = root.FindNode(context.Span);
            declaration = generator.GetDeclaration(declaration);

            if (declaration == null)
            {
                return;
            }

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();

            context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.ImplementIDisposableInterface,
                                                     async ct => await ImplementIDisposable(context.Document, declaration, ct).ConfigureAwait(false)),
                                    diagnostic);
        }

        private async Task<Document> ImplementIDisposable(Document document, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var model = editor.SemanticModel;

            // Add the interface to the baselist.
            var interfaceType = generator.TypeExpression(WellKnownTypes.IDisposable(model.Compilation));
            editor.AddInterfaceType(declaration, interfaceType);

            // Find a Dispose method. If one exists make that implement IDisposable, else generate a new method.
            var typeSymbol = model.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
            var disposeMethod = (typeSymbol?.GetMembers("Dispose"))?.OfType<IMethodSymbol>()?.Where(m => m.Parameters.Length == 0).FirstOrDefault();
            if (disposeMethod != null && disposeMethod.DeclaringSyntaxReferences.Length == 1)
            {
                var memberPartNode = await disposeMethod.DeclaringSyntaxReferences.Single().GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                memberPartNode = generator.GetDeclaration(memberPartNode);
                editor.ReplaceNode(memberPartNode, generator.AsPublicInterfaceImplementation(memberPartNode, interfaceType));
            }
            else
            {
                var throwStatement = generator.ThrowStatement(generator.ObjectCreationExpression(WellKnownTypes.NotImplementedException(model.Compilation)));
                var member = generator.MethodDeclaration(TypesThatOwnDisposableFieldsShouldBeDisposableAnalyzer.Dispose, statements: new[] { throwStatement });
                member = generator.AsPublicInterfaceImplementation(member, interfaceType);
                editor.AddMember(declaration, member);
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
