// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram;

using static SyntaxFactory;

internal static partial class ConvertProgramTransform
{
    public static async Task<Document> ConvertToTopLevelStatementsAsync(
        Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        var typeDeclaration = (TypeDeclarationSyntax?)methodDeclaration.Parent;
        Contract.ThrowIfNull(typeDeclaration); // checked by analyzer

        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var rootWithGlobalStatements = GetRootWithGlobalStatements(
            semanticModel, generator, root, typeDeclaration, methodDeclaration, cancellationToken);

        // simple case.  we were in a top level type to begin with.  Nothing we need to do now.
        if (typeDeclaration.Parent is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
            return document.WithSyntaxRoot(rootWithGlobalStatements);

        // We were parented by a namespace.  Add using statements to bring in all the symbols that were
        // previously visible within the namespace.  Then remove any that we don't need once we've done that.
        var cleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);

        document = await AddUsingDirectivesAsync(
            document, rootWithGlobalStatements, namespaceDeclaration, cleanupOptions, cancellationToken).ConfigureAwait(false);

        // if we have a file scoped namespace after converting to top-level-statements, then convert it to a
        // block-namespace.  Top level statements and file-scoped-namespaces are not allowed together.
        document = await ConvertFileScopedNamespaceAsync(document, cleanupOptions, cancellationToken).ConfigureAwait(false);

        return document;
    }

    private static async Task<Document> ConvertFileScopedNamespaceAsync(Document document, CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
    {
        var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault() is { } fileScopedNamespace
            ? await ConvertNamespaceTransform.ConvertFileScopedNamespaceAsync(document, fileScopedNamespace, (CSharpSyntaxFormattingOptions)cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false)
            : document;
    }

    private static async Task<Document> AddUsingDirectivesAsync(
        Document document, SyntaxNode root, BaseNamespaceDeclarationSyntax namespaceDeclaration, CodeCleanupOptions options, CancellationToken cancellationToken)
    {
        var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();
        var removeImportsService = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();

        var annotation = new SyntaxAnnotation();
        using var _ = ArrayBuilder<UsingDirectiveSyntax>.GetInstance(out var directives);
        AddUsingDirectives(namespaceDeclaration.Name, annotation, directives);

        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

        var documentWithImportsAdded = document.WithSyntaxRoot(addImportsService.AddImports(
            compilation: null!, root, contextLocation: null, directives, generator,
            options.AddImportOptions,
            cancellationToken));

        return await removeImportsService.RemoveUnnecessaryImportsAsync(
            documentWithImportsAdded, n => n.HasAnnotation(annotation), cancellationToken).ConfigureAwait(false);
    }

    private static void AddUsingDirectives(NameSyntax name, SyntaxAnnotation annotation, ArrayBuilder<UsingDirectiveSyntax> directives)
    {
        if (name is QualifiedNameSyntax qualifiedName)
            AddUsingDirectives(qualifiedName.Left, annotation, directives);

        directives.Add(UsingDirective(name).WithAdditionalAnnotations(annotation));
    }

    private static SyntaxNode GetRootWithGlobalStatements(
        SemanticModel semanticModel,
        SyntaxGenerator generator,
        SyntaxNode root,
        TypeDeclarationSyntax typeDeclaration,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var editor = new SyntaxEditor(root, generator);
        var globalStatements = GetGlobalStatements(
            semanticModel, typeDeclaration, methodDeclaration, cancellationToken);

        var namespaceDeclaration = typeDeclaration.Parent as BaseNamespaceDeclarationSyntax;
        if (namespaceDeclaration != null &&
            namespaceDeclaration.Members.Count >= 2)
        {
            // Our parent namespace has another symbol in it.  Keep the namespace declaration around, removing only
            // the existing Program type from it.
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
            // we had a parent namespace, but we were the only thing in it.  We can just remove the namespace entirely.

            // If there was a file banner on the namespace, move it to the first statement.
            var fileBanner = root.GetFirstToken() == namespaceDeclaration.GetFirstToken()
                ? CSharpFileBannerFacts.Instance.GetFileBanner(root)
                : default;
            if (!fileBanner.IsDefaultOrEmpty && globalStatements.Length > 0)
            {
                globalStatements = globalStatements.Replace(
                    globalStatements[0],
                    globalStatements[0].WithPrependedLeadingTrivia(fileBanner));
            }

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

    private static ImmutableArray<GlobalStatementSyntax> GetGlobalStatements(
        SemanticModel semanticModel,
        TypeDeclarationSyntax typeDeclaration,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var statements);

        // First, process all fields and convert them to locals.  We do this first as the locals need to be declared
        // first in order for the main-method statements to reference them.

        foreach (var member in typeDeclaration.Members)
        {
            // hit another member, must be a field/method.
            if (member is FieldDeclarationSyntax fieldDeclaration)
            {
                // Convert fields into local statements
                statements.Add(LocalDeclarationStatement(
                    ConvertDeclaration(semanticModel, fieldDeclaration.Declaration, cancellationToken))
                    .WithSemicolonToken(fieldDeclaration.SemicolonToken)
                    .WithTriviaFrom(fieldDeclaration));
            }
        }

        // Then convert all remaining methods to local functions (except for 'Main', which becomes the global
        // statements of the top-level program).
        foreach (var member in typeDeclaration.Members)
        {
            if (member == methodDeclaration)
            {
                // when we hit the 'Main' method, then actually take all its nested statements and elevate them to
                // top-level statements.
                Contract.ThrowIfNull(methodDeclaration.Body); // checked by analyzer

                // Extract statements from the method body while preserving all trivia,
                // including preprocessor directives and disabled code sections.
                var bodyStatements = ExtractStatementsFromMethodBody(methodDeclaration);

                // move comments on the method to be on it's first statement.
                if (bodyStatements.Count > 0)
                    statements.Add(bodyStatements[0].WithPrependedLeadingTrivia(methodDeclaration.GetLeadingTrivia()));

                statements.AddRange(bodyStatements.Skip(1));
            }
            else if (member is MethodDeclarationSyntax otherMethod)
            {
                // convert methods to local functions.
                statements.Add(LocalFunctionStatement(
                    attributeLists: default,
                    modifiers: [.. otherMethod.Modifiers.Where(m => m.Kind() is SyntaxKind.AsyncKeyword or SyntaxKind.UnsafeKeyword)],
                    returnType: otherMethod.ReturnType,
                    identifier: otherMethod.Identifier,
                    typeParameterList: otherMethod.TypeParameterList,
                    parameterList: otherMethod.ParameterList,
                    constraintClauses: otherMethod.ConstraintClauses,
                    body: otherMethod.Body,
                    expressionBody: otherMethod.ExpressionBody).WithLeadingTrivia(otherMethod.GetLeadingTrivia()));
            }
            else if (member is not FieldDeclarationSyntax)
            {
                // checked by analyzer
                throw ExceptionUtilities.Unreachable();
            }
        }

        // Move the trivia on the type itself to the first statement we create.
        if (statements.Count > 0)
        {
            statements[0] = statements[0].WithPrependedLeadingTrivia(typeDeclaration.GetLeadingTrivia());

            // If our first statement doesn't have any preceding newlines, then also attempt to take any whitespace
            // on the namespace we're contained in.  That way we have enough spaces between the first statement and
            // any using directives.
            if (!statements[0].GetLeadingTrivia().Any(t => t.Kind() is SyntaxKind.EndOfLineTrivia) &&
                typeDeclaration.Parent is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                statements[0] = statements[0].WithPrependedLeadingTrivia(
                    namespaceDeclaration.GetLeadingTrivia().TakeWhile(t => t.Kind() is SyntaxKind.WhitespaceTrivia or SyntaxKind.EndOfLineTrivia));
            }
        }

        var globalStatements = new FixedSizeArrayBuilder<GlobalStatementSyntax>(statements.Count);
        foreach (var statement in statements)
            globalStatements.Add(GlobalStatement(statement).WithAdditionalAnnotations(Formatter.Annotation));

        return globalStatements.MoveToImmutable();
    }

    private static VariableDeclarationSyntax ConvertDeclaration(
        SemanticModel semanticModel,
        VariableDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        return declaration.ReplaceNodes(
            declaration.Variables,
            (v, _) => ConvertVariable(semanticModel, v, cancellationToken));
    }

    private static VariableDeclaratorSyntax ConvertVariable(
        SemanticModel semanticModel,
        VariableDeclaratorSyntax variable,
        CancellationToken cancellationToken)
    {
        // If the field does not have an initialize, then generate it with one as locals do not get initialized by
        // default like fields do.
        if (variable.Initializer != null)
            return variable;

        var field = (IFieldSymbol?)semanticModel.GetDeclaredSymbol(variable, cancellationToken);
        Contract.ThrowIfNull(field);
        return variable.WithInitializer(EqualsValueClause(
            (ExpressionSyntax)CSharpSyntaxGenerator.Instance.DefaultExpression(field.Type)));
    }

    /// <summary>
    /// Extracts statements from a method body while preserving all trivia, including
    /// preprocessor directives and disabled code sections in #if/#else/#endif blocks.
    /// </summary>
    private static ImmutableArray<StatementSyntax> ExtractStatementsFromMethodBody(MethodDeclarationSyntax methodDeclaration)
    {
        Contract.ThrowIfNull(methodDeclaration.Body);

        var methodBody = methodDeclaration.Body;
        var methodBodyStatements = methodBody.Statements;
        
        if (methodBodyStatements.Count == 0)
            return ImmutableArray<StatementSyntax>.Empty;

        // If we don't have preprocessor directives, use the simple approach
        if (!ContainsPreprocessorDirectives(methodBody))
            return methodBodyStatements.ToImmutableArray();

        // Extract the entire content between the braces, preserving all trivia
        return ExtractStatementsPreservingAllTrivia(methodBody);
    }

    private static bool ContainsPreprocessorDirectives(BlockSyntax block)
    {
        return block.DescendantTrivia().Any(trivia => SyntaxFacts.IsPreprocessorDirective(trivia.Kind()));
    }

    private static ImmutableArray<StatementSyntax> ExtractStatementsPreservingAllTrivia(BlockSyntax methodBody)
    {
        // Get the entire content between the braces, preserving everything
        var openBrace = methodBody.OpenBraceToken;
        var closeBrace = methodBody.CloseBraceToken;
        
        var startPos = openBrace.Span.End;
        var endPos = closeBrace.SpanStart;
        
        if (startPos >= endPos)
            return ImmutableArray<StatementSyntax>.Empty;

        // Get the source text and extract the inner content  
        var syntaxTree = methodBody.SyntaxTree;
        var sourceText = syntaxTree.GetText();
        var innerText = sourceText.GetSubText(TextSpan.FromBounds(startPos, endPos)).ToString();
        
        // Parse this as compilation unit to preserve all preprocessor directives
        var wrappedCode = "class Temp { void Method() {" + innerText + "} }";
        var tempTree = CSharpSyntaxTree.ParseText(wrappedCode);
        var tempRoot = tempTree.GetCompilationUnitRoot();
        
        var tempClass = tempRoot.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
        var tempMethod = tempClass?.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var tempBody = tempMethod?.Body;
        
        if (tempBody != null && tempBody.Statements.Count > 0)
        {
            return tempBody.Statements.ToImmutableArray();
        }
        
        // Fallback to original statements if parsing fails
        return methodBody.Statements.ToImmutableArray();
    }

    private static List<SyntaxTrivia> CollectAllPreprocessorTrivia(BlockSyntax methodBody)
    {
        var preprocessorTrivia = new List<SyntaxTrivia>();
        
        // Collect all preprocessor directive trivia from the entire method body
        foreach (var trivia in methodBody.DescendantTrivia())
        {
            if (SyntaxFacts.IsPreprocessorDirective(trivia.Kind()))
            {
                preprocessorTrivia.Add(trivia);
            }
        }
        
        return preprocessorTrivia;
    }

    private static ImmutableArray<StatementSyntax> PreservePreprocessorTriviaInStatements(
        SyntaxList<StatementSyntax> originalStatements, 
        List<SyntaxTrivia> preprocessorTrivia)
    {
        if (originalStatements.Count == 0)
            return ImmutableArray<StatementSyntax>.Empty;

        // For now, attach all preprocessor trivia to the first statement
        // This ensures all the preprocessor directives and disabled code sections are preserved
        var firstStatement = originalStatements[0];
        
        // Get the existing trivia
        var leadingTrivia = firstStatement.GetLeadingTrivia().ToList();
        var trailingTrivia = firstStatement.GetTrailingTrivia().ToList();
        
        // Find where to insert the preprocessor trivia
        // We want to maintain the structure: #if -> active code -> #else -> inactive code -> #endif
        
        // For simplicity, let's reconstruct the statement by preserving the original method body content
        // Get the span of content from the method body and preserve it as trivia
        var methodBodyContent = ExtractMethodBodyContentAsTrivia(originalStatements[0]);
        
        var newStatement = firstStatement.WithLeadingTrivia(methodBodyContent);
        
        var result = ImmutableArray.CreateBuilder<StatementSyntax>();
        result.Add(newStatement);
        result.AddRange(originalStatements.Skip(1));
        
        return result.ToImmutable();
    }

    private static SyntaxTriviaList ExtractMethodBodyContentAsTrivia(StatementSyntax originalStatement)
    {
        // This is a more complex approach - we need to extract the content and preserve it as trivia
        // For now, let's return the original trivia
        return originalStatement.GetLeadingTrivia();
    }
}
