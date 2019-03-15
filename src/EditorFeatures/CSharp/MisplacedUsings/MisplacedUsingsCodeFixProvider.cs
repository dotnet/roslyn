// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MoveMisplacedUsings)]
    [Shared]
    internal sealed partial class MisplacedUsingsCodeFixProvider : CodeFixProvider
    {
        private const string SystemUsingDirectiveIdentifier = nameof(System);

        // This format is used when checking whether a using directive is importing from the System namespace. 
        // Omitting the global namespace and not using special types ensures we are able to check whether the
        // display name root is 'System' or not.
        private static readonly SymbolDisplayFormat s_fullNamespaceDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
        private static readonly List<UsingDirectiveSyntax> s_emptyUsingsList = new List<UsingDirectiveSyntax>();
        private static readonly SyntaxAnnotation s_usingPlacementCodeFixAnnotation = new SyntaxAnnotation(nameof(s_usingPlacementCodeFixAnnotation));

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return FixAll.Instance;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var compilationUnit = (CompilationUnitSyntax)syntaxRoot;
            var options = await context.Document.GetOptionsAsync(context.CancellationToken).ConfigureAwait(false);

            // do not offer a code fix for IDE0056 when there are multiple namespaces in the source file
            if (CountNamespaces(compilationUnit.Members) > 1)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    new MoveMisplacedUsingsCodeAction(cancellationToken =>
                        GetTransformedDocumentAsync(context.Document, syntaxRoot, options, cancellationToken)),
                    diagnostic);
            }
        }

        private static async Task<Document> GetTransformedDocumentAsync(Document document, SyntaxNode syntaxRoot, OptionSet options, CancellationToken cancellationToken)
        {
            var placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
            var usingDirectivesPlacement = DeterminePlacement((CompilationUnitSyntax)syntaxRoot, options);
            var addImportService = document.GetLanguageService<IAddImportsService>();
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var (newSyntaxRoot, fileHeader) = RemoveFileHeader(syntaxRoot);
            var compilationUnit = (CompilationUnitSyntax)newSyntaxRoot;

            SyntaxNode contextNode = null;
            IEnumerable<UsingDirectiveSyntax> usingsToAdd = null;

            if (usingDirectivesPlacement == AddImportPlacement.InsideNamespace)
            {
                usingsToAdd = compilationUnit.Usings;
                newSyntaxRoot = compilationUnit.WithUsings(new SyntaxList<UsingDirectiveSyntax>()).WithoutLeadingTrivia();
                newSyntaxRoot = AddLeadingEndOfLineToNamespace(newSyntaxRoot);
                contextNode = ((CompilationUnitSyntax)newSyntaxRoot).Members.OfType<NamespaceDeclarationSyntax>().First();
            }
            else if (usingDirectivesPlacement == AddImportPlacement.OutsideNamespace)
            {
                var rootNamespace = compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().First();
                usingsToAdd = rootNamespace.Usings;

                var newRootNamespace = rootNamespace.WithUsings(new SyntaxList<UsingDirectiveSyntax>());
                newSyntaxRoot = newSyntaxRoot.ReplaceNode(rootNamespace, newRootNamespace);
            }

            usingsToAdd = usingsToAdd.Select(FixUpUsingDirective);

            newSyntaxRoot = addImportService.AddImports(compilation, newSyntaxRoot, contextNode, usingsToAdd, placeSystemNamespaceFirst, usingDirectivesPlacement);
            newSyntaxRoot = ReAddFileHeader(newSyntaxRoot, fileHeader);

            if (usingDirectivesPlacement == AddImportPlacement.OutsideNamespace)
            {
                newSyntaxRoot = AddLeadingEndOfLineToCompilationUnit(newSyntaxRoot);
            }

            return document.WithSyntaxRoot(newSyntaxRoot);
        }

        private static SyntaxNode AddLeadingEndOfLineToCompilationUnit(SyntaxNode syntaxRoot)
        {
            // If we already have usings then, assume the code is formatted how the user intended.
            // If the namespace contains no members there is no need to break up the usings from the other members.
            var compilationUnit = ((CompilationUnitSyntax)syntaxRoot);
            if (compilationUnit.Members.Count == 0)
            {
                return syntaxRoot;
            }

            var firstMember = compilationUnit.Members.First();
            var firstMemberTrivia = firstMember.GetLeadingTrivia();

            // If the first member already contains a leading new line then, this will already break up the usings from these members.
            if (firstMemberTrivia.Count > 0 && firstMemberTrivia.First().IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return syntaxRoot;
            }

            var newFirstMember = firstMember.WithLeadingTrivia(firstMemberTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed));
            return compilationUnit.ReplaceNode(firstMember, newFirstMember);
        }

        private static SyntaxNode AddLeadingEndOfLineToNamespace(SyntaxNode syntaxRoot)
        {
            // If we already have usings then, assume the code is formatted how the user intended.
            // If the namespace contains no members there is no need to break up the usings from the other members.
            var namespaceDeclaration = ((CompilationUnitSyntax)syntaxRoot).Members.OfType<NamespaceDeclarationSyntax>().First();
            if (namespaceDeclaration.Usings.Count > 0 || namespaceDeclaration.Members.Count == 0)
            {
                return syntaxRoot;
            }

            var firstMember = namespaceDeclaration.Members.First();
            var firstMemberTrivia = firstMember.GetLeadingTrivia();

            // If the first member already contains a leading new line then, this will already break up the usings from these members.
            if (firstMemberTrivia.Count > 0 && firstMemberTrivia.First().IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return syntaxRoot;
            }

            var newFirstMember = firstMember.WithLeadingTrivia(firstMemberTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed));
            var newNamespaceDeclaration = namespaceDeclaration.ReplaceNode(firstMember, newFirstMember);
            return syntaxRoot.ReplaceNode(namespaceDeclaration, newNamespaceDeclaration);
        }

        private static UsingDirectiveSyntax FixUpUsingDirective(UsingDirectiveSyntax usingDirective)
        {
            var usingTrivia = usingDirective.GetLeadingTrivia();

            // Only keep leading trivia if it isn't Whitespace.
            var shouldRemoveTrivia = usingTrivia.All(trivia =>
                trivia.IsKind(SyntaxKind.WhitespaceTrivia) ||
                trivia.IsKind(SyntaxKind.EndOfLineTrivia));

            if (shouldRemoveTrivia)
            {
                usingDirective = usingDirective.WithoutLeadingTrivia();
            }

            return usingDirective.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static AddImportPlacement DeterminePlacement(CompilationUnitSyntax compilationUnit, OptionSet options)
        {
            switch (options.GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivesPlacement).Value)
            {
                case AddImportPlacement.InsideNamespace:
                    var namespaceCount = CountNamespaces(compilationUnit.Members);

                    // Only move using declarations inside the namespace when
                    // - There are no global attributes
                    // - There is only a single namespace declared at the top level
                    // - OrderingSettings.usingDirectivesPlacement  is set to InsideNamespace
                    if (compilationUnit.AttributeLists.Any()
                        || compilationUnit.Members.Count > 1
                        || namespaceCount > 1)
                    {
                        // Override the user's setting with a more conservative one
                        return AddImportPlacement.Preserve;
                    }

                    if (namespaceCount == 0)
                    {
                        return AddImportPlacement.OutsideNamespace;
                    }

                    return AddImportPlacement.InsideNamespace;

                case AddImportPlacement.OutsideNamespace:
                    return AddImportPlacement.OutsideNamespace;

                case AddImportPlacement.Preserve:
                default:
                    return AddImportPlacement.Preserve;
            }
        }

        private static int CountNamespaces(SyntaxList<MemberDeclarationSyntax> members)
        {
            var result = 0;

            foreach (var namespaceDeclaration in members.OfType<NamespaceDeclarationSyntax>())
            {
                result += 1 + CountNamespaces(namespaceDeclaration.Members);
            }

            return result;
        }

        private static (SyntaxNode, ImmutableArray<SyntaxTrivia>) RemoveFileHeader(SyntaxNode syntaxRoot)
        {
            var fileHeader = GetFileHeader(syntaxRoot);
            var leadingTrivia = syntaxRoot.GetLeadingTrivia();

            for (var i = fileHeader.Length - 1; i >= 0; i--)
            {
                leadingTrivia = leadingTrivia.RemoveAt(i);
            }

            var newSyntaxRoot = syntaxRoot.WithLeadingTrivia(leadingTrivia);

            return (newSyntaxRoot, fileHeader);
        }

        private static ImmutableArray<SyntaxTrivia> GetFileHeader(SyntaxNode syntaxRoot)
        {
            var onBlankLine = true;
            var hasHeader = false;
            var fileHeaderBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();

            var firstToken = syntaxRoot.GetFirstToken(includeZeroWidth: true);
            var firstTokenLeadingTrivia = firstToken.LeadingTrivia;

            int i;
            for (i = 0; i < firstTokenLeadingTrivia.Count; i++)
            {
                var done = false;
                switch (firstTokenLeadingTrivia[i].Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                        fileHeaderBuilder.Add(firstTokenLeadingTrivia[i]);
                        onBlankLine = false;
                        break;

                    case SyntaxKind.WhitespaceTrivia:
                        fileHeaderBuilder.Add(firstTokenLeadingTrivia[i]);
                        break;

                    case SyntaxKind.EndOfLineTrivia:
                        hasHeader = true;
                        fileHeaderBuilder.Add(firstTokenLeadingTrivia[i]);

                        if (onBlankLine)
                        {
                            done = true;
                        }
                        else
                        {
                            onBlankLine = true;
                        }

                        break;

                    default:
                        done = true;
                        break;
                }

                if (done)
                {
                    break;
                }
            }

            return hasHeader ? fileHeaderBuilder.ToImmutableArray() : ImmutableArray.Create<SyntaxTrivia>();
        }

        private static SyntaxNode ReAddFileHeader(SyntaxNode syntaxRoot, ImmutableArray<SyntaxTrivia> fileHeader)
        {
            if (fileHeader.IsEmpty)
            {
                // Only re-add the file header if it was stripped.
                return syntaxRoot;
            }

            var firstToken = syntaxRoot.GetFirstToken(includeZeroWidth: true);
            var newLeadingTrivia = firstToken.LeadingTrivia.InsertRange(0, fileHeader);
            return syntaxRoot.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(newLeadingTrivia));
        }

        private class FixAll : DocumentBasedFixAllProvider
        {
            public static FixAllProvider Instance { get; } = new FixAll();

            protected override string CodeActionTitle => CSharpEditorResources.Move_misplaced_using_directives;

            /// <inheritdoc/>
            protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                if (diagnostics.IsEmpty)
                {
                    return null;
                }

                var syntaxRoot = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var options = await document.GetOptionsAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var newDocument = await GetTransformedDocumentAsync(document, syntaxRoot, options, fixAllContext.CancellationToken).ConfigureAwait(false);
                return await newDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            }
        }

        private class MoveMisplacedUsingsCodeAction : CodeAction
        {
            private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;

            public override string Title => CSharpEditorResources.Move_misplaced_using_directives;

            public MoveMisplacedUsingsCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
            {
                _createChangedDocument = createChangedDocument;
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return _createChangedDocument(cancellationToken);
            }
        }
    }
}
