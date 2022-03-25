// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertToTopLevelStatements), Shared]
    internal class ConvertToTopLevelStatementsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertToTopLevelStatementsCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseTopLevelStatementsId);

        internal override CodeFixCategory CodeFixCategory
            => CodeFixCategory.CodeStyle;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);
            var priority = option.Notification.Severity == ReportDiagnostic.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c), priority),
                context.Diagnostics);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var methodDeclaration = (MethodDeclarationSyntax)diagnostics[0].AdditionalLocations[0].FindNode(cancellationToken);
            var typeDeclaration = (TypeDeclarationSyntax?)methodDeclaration.Parent;
            Contract.ThrowIfNull(typeDeclaration); // checked by analyzer

            var rootWithGlobalStatements = GetRootWithGlobalStatements(editor.Generator, editor.OriginalRoot, typeDeclaration, methodDeclaration);

            if (typeDeclaration.Parent is not NamespaceDeclarationSyntax namespaceDeclaration)
            {
                // simple case.  we were in a top level type to begin with.  Nothing we need to do now.
                editor.ReplaceNode(editor.OriginalRoot, rootWithGlobalStatements);
                return;
            }

            // We were parented by a namespace.  Add using statements to bring in all the symbols that were
            // previously visible within the namespace.  Then remove any that we don't need once we've done that.
            var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();
            var removeImportsService = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();

            var annotation = new SyntaxAnnotation();
            using var _ = ArrayBuilder<UsingDirectiveSyntax>.GetInstance(out var directives);
            AddUsingDirectives(namespaceDeclaration.Name, annotation, directives);

            var rootWithImportsAdded = addImportsService.AddImports(
                compilation: null!, rootWithGlobalStatements, contextLocation: null, directives, editor.Generator,
                await AddImportPlacementOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false),
                cancellationToken);
            var documentWithImportsAdded = document.WithSyntaxRoot(rootWithImportsAdded);

            var documentWithImportsRemoved = await removeImportsService.RemoveUnnecessaryImportsAsync(
                documentWithImportsAdded, n => n.HasAnnotation(annotation), cancellationToken).ConfigureAwait(false);
            var rootWithImportsRemoved = await documentWithImportsRemoved.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(editor.OriginalRoot, rootWithImportsRemoved);
        }

        private void AddUsingDirectives(NameSyntax name, SyntaxAnnotation annotation, ArrayBuilder<UsingDirectiveSyntax> directives)
        {
            if (name is QualifiedNameSyntax qualifiedName)
                AddUsingDirectives(qualifiedName.Left, annotation, directives);

            directives.Add(UsingDirective(name).WithAdditionalAnnotations(annotation));
        }

        private static SyntaxNode GetRootWithGlobalStatements(
            SyntaxGenerator generator,
            SyntaxNode root,
            TypeDeclarationSyntax typeDeclaration,
            MethodDeclarationSyntax methodDeclaration)
        {
            var editor = new SyntaxEditor(root, generator);
            var globalStatements = GetGlobalStatements(typeDeclaration, methodDeclaration);

            var namespaceDeclaration = typeDeclaration.Parent as NamespaceDeclarationSyntax;
            if (namespaceDeclaration != null &&
                namespaceDeclaration.Members.Count >= 2)
            {
                // Our parent namespace has another symbol in it.  Keep it around, but move us to top-level-statements.
                editor.RemoveNode(typeDeclaration);
                editor.ReplaceNode(
                    root,
                    (current, _) =>
                    {
                        var currentRoot = (CompilationUnitSyntax)current;
                        return currentRoot.WithMembers(currentRoot.Members.InsertRange(0, globalStatements));
                    });
            }
            else if (namespaceDeclaration != null)
            {
                // we had a parent namespace, but we were the only thing in it.  We can just remove that namespace entirely.
                editor.ReplaceNode(
                    root,
                    root.ReplaceNode(namespaceDeclaration, globalStatements));
            }
            else
            {
                // type wasn't in a namespace.  just remove the type and replace it with the new global statements.
                editor.ReplaceNode(
                    root, root.ReplaceNode(typeDeclaration, globalStatements));
            }

            return editor.GetChangedRoot();
        }

        private static ImmutableArray<GlobalStatementSyntax> GetGlobalStatements(TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax methodDeclaration)
        {
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var statements);
            foreach (var member in typeDeclaration.Members)
            {
                if (member == methodDeclaration)
                {
                    // when we hit the 'Main' method, then actually take all its nested statements and elevate them to
                    // top-level statements.
                    Contract.ThrowIfNull(methodDeclaration.Body); // checked by analyzer

                    // move comments on the method to be on it's first statement.
                    if (methodDeclaration.Body.Statements.Count > 0)
                        statements.AddRange(methodDeclaration.Body.Statements[0].WithPrependedLeadingTrivia(methodDeclaration.GetLeadingTrivia()));

                    statements.AddRange(methodDeclaration.Body.Statements.Skip(1));
                    continue;
                }

                // hit another member, must be a field/method.
                if (member is FieldDeclarationSyntax fieldDeclaration)
                {
                    // Convert fields into local statements
                    statements.Add(LocalDeclarationStatement(fieldDeclaration.Declaration)
                        .WithSemicolonToken(fieldDeclaration.SemicolonToken)
                        .WithTriviaFrom(fieldDeclaration));
                }
                else if (member is MethodDeclarationSyntax otherMethod)
                {
                    // convert methods to local functions.
                    statements.Add(LocalFunctionStatement(
                        attributeLists: default,
                        modifiers: default,
                        returnType: otherMethod.ReturnType,
                        identifier: otherMethod.Identifier,
                        typeParameterList: otherMethod.TypeParameterList,
                        parameterList: otherMethod.ParameterList,
                        constraintClauses: otherMethod.ConstraintClauses,
                        body: otherMethod.Body,
                        expressionBody: otherMethod.ExpressionBody).WithLeadingTrivia(otherMethod.GetLeadingTrivia()));
                }
                else
                {
                    // checked by analyzer
                    throw ExceptionUtilities.Unreachable;
                }
            }

            // Move the trivia on the type itself to the first statement we create.
            if (statements.Count > 0)
                statements[0] = statements[0].WithPrependedLeadingTrivia(typeDeclaration.GetLeadingTrivia());

            using var _1 = ArrayBuilder<GlobalStatementSyntax>.GetInstance(out var globalStatements);
            foreach (var statement in statements)
                globalStatements.Add(GlobalStatement(statement).WithAdditionalAnnotations(Formatter.Annotation));

            return globalStatements.ToImmutable();
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, CodeActionPriority priority)
                : base(CSharpAnalyzersResources.Convert_to_top_level_statements, createChangedDocument, nameof(ConvertToTopLevelStatementsCodeFixProvider))
            {
                this.Priority = priority;
            }
        }
    }
}
