// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using global::System;
using global::System.Collections.Generic;
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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

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
                    new MoveMisplacedUsingsCodeAction(cancellationToken => GetTransformedDocumentAsync(context.Document, compilationUnit, options, cancellationToken)),
                    diagnostic);
            }
        }

        private static async Task<Document> GetTransformedDocumentAsync(Document document, CompilationUnitSyntax compilationUnit, OptionSet options, CancellationToken cancellationToken)
        {
            var usingDirectivesPlacement = DeterminePlacement(compilationUnit, options);

            var newCompilationUnit = await ExpandUsingDirectivesAsync(document, compilationUnit, cancellationToken).ConfigureAwait(false);
            ImmutableArray<SyntaxTrivia> fileHeader;

            (newCompilationUnit, fileHeader) = RemoveFileHeader(newCompilationUnit);

            newCompilationUnit = usingDirectivesPlacement switch
            {
                AddImportPlacement.InsideNamespace => MoveUsingsInsideNamespace(newCompilationUnit),
                AddImportPlacement.OutsideNamespace => MoveUsingsOutsideNamespace(newCompilationUnit),
                _ => throw new NotSupportedException()
            };

            newCompilationUnit = AddFileHeader(newCompilationUnit, fileHeader);

            var newDocument = document.WithSyntaxRoot(newCompilationUnit);

            return await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, options, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<CompilationUnitSyntax> ExpandUsingDirectivesAsync(Document document, CompilationUnitSyntax compilationUnit, CancellationToken cancellationToken)
        {
            // Expand the usings declarations in the compilation unit.
            var expandCompilationUnitUsings = compilationUnit.Usings.Select(
                usingDirective => ExpandUsingDirectiveAsync(document, usingDirective, cancellationToken));
            var newCompilationUnitUsings = await Task.WhenAll(expandCompilationUnitUsings).ConfigureAwait(false);

            var newCompilationUnit = compilationUnit.WithUsings(new SyntaxList<UsingDirectiveSyntax>(newCompilationUnitUsings));

            // To expand the using declarations in the namespace we need to get an updated document, since the 
            // expander requires that the SyntaxTree for the semantic model match that of the using directives.
            var newDocument = document.WithSyntaxRoot(newCompilationUnit);
            newCompilationUnit = (CompilationUnitSyntax)await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Expand the using declarations in the namespace declaration.
            var namespaceDeclaration = newCompilationUnit.Members.OfType<NamespaceDeclarationSyntax>().First();

            var expandNamespaceDeclarationUsings = namespaceDeclaration.Usings.Select(
                usingDirective => ExpandUsingDirectiveAsync(newDocument, usingDirective, cancellationToken));
            var newNamespaceDeclarationUsings = await Task.WhenAll(expandNamespaceDeclarationUsings).ConfigureAwait(false);

            var newNamespaceDeclaration = namespaceDeclaration.WithUsings(new SyntaxList<UsingDirectiveSyntax>(newNamespaceDeclarationUsings));

            return newCompilationUnit.ReplaceNode(namespaceDeclaration, newNamespaceDeclaration);
        }

        private static async Task<UsingDirectiveSyntax> ExpandUsingDirectiveAsync(Document document, UsingDirectiveSyntax usingDirective, CancellationToken cancellationToken)
        {
            var newName = await Simplifier.ExpandAsync(usingDirective.Name, document, cancellationToken: cancellationToken).ConfigureAwait(false);
            return usingDirective.WithName(newName);
        }

        private static CompilationUnitSyntax MoveUsingsInsideNamespace(CompilationUnitSyntax compilationUnit)
        {
            // Get the compilation unit usings and set them up to format when moved.
            var usingsToAdd = FixUpUsingDirectives(compilationUnit.Usings);

            // Remove usings and fix leading trivia for compilation unit.
            var newCompilationUnit = compilationUnit.WithUsings(new SyntaxList<UsingDirectiveSyntax>());
            newCompilationUnit = RemoveLeadingEndOfLinesFromCompilationUnit(newCompilationUnit);

            // Fix the leading trivia for the namespace declaration.
            var namespaceDeclaration = newCompilationUnit.Members.OfType<NamespaceDeclarationSyntax>().First();
            var newNamespaceDeclaration = EnsureLeadingEndOfLineForNamespace(namespaceDeclaration);

            // Update the namespace declaration with the usings from the compilation unit.
            var newUsings = newNamespaceDeclaration.Usings.InsertRange(0, usingsToAdd);
            newNamespaceDeclaration = newNamespaceDeclaration.WithUsings(newUsings);

            // Update the compilation unit with the new namespace declaration 
            return newCompilationUnit.ReplaceNode(namespaceDeclaration, newNamespaceDeclaration);
        }

        private static CompilationUnitSyntax MoveUsingsOutsideNamespace(CompilationUnitSyntax compilationUnit)
        {
            var namespaceDeclaration = compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().First();

            // Get the namespace declaration usings and set them up to format when moved.
            var usingsToAdd = FixUpUsingDirectives(namespaceDeclaration.Usings);

            // Remove usings and fix leading trivia for namespace declaration.
            var newNamespaceDeclaration = namespaceDeclaration.WithUsings(new SyntaxList<UsingDirectiveSyntax>());
            newNamespaceDeclaration = RemoveLeadingEndOfLinesFromNamespaceDeclaration(newNamespaceDeclaration);
            var newCompilationUnit = compilationUnit.ReplaceNode(namespaceDeclaration, newNamespaceDeclaration);

            // Update the compilation unit with the usings from the namespace declaration.
            var newUsings = newCompilationUnit.Usings.AddRange(usingsToAdd);
            newCompilationUnit = newCompilationUnit.WithUsings(newUsings);

            // Fix the leading trivia for the compilation unit. 
            return EnsureLeadingEndOfLineForCompilationUnit(newCompilationUnit);
        }

        private static CompilationUnitSyntax RemoveLeadingEndOfLinesFromCompilationUnit(CompilationUnitSyntax compilationUnit)
        {
            if (compilationUnit.Members.Count == 0)
            {
                return compilationUnit;
            }

            var firstMember = compilationUnit.Members.First();
            var firstMemberTrivia = firstMember.GetLeadingTrivia();

            // If the first member already contains a leading new line then, this will already break up the usings from these members.
            if (firstMemberTrivia.Count == 0)
            {
                return compilationUnit;
            }

            var newTrivia = SplitIntoLines(firstMemberTrivia)
                .SkipWhile(trivia => trivia.All(t => t.IsWhitespaceOrEndOfLine()) && trivia.Last().IsKind(SyntaxKind.EndOfLineTrivia))
                .SelectMany(t => t);

            var newFirstMember = firstMember.WithLeadingTrivia(newTrivia);
            return compilationUnit.ReplaceNode(firstMember, newFirstMember);
        }

        private static CompilationUnitSyntax EnsureLeadingEndOfLineForCompilationUnit(CompilationUnitSyntax compilationUnit)
        {
            // If we already have usings then, assume the code is formatted how the user intended.
            // If the namespace contains no members there is no need to break up the usings from the other members.
            if (compilationUnit.Members.Count == 0)
            {
                return compilationUnit;
            }

            var firstMember = compilationUnit.Members.First();
            var firstMemberTrivia = firstMember.GetLeadingTrivia();

            // If the first member already contains a leading new line then, this will already break up the usings from these members.
            if (firstMemberTrivia.Count > 0 && firstMemberTrivia.First().IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return compilationUnit;
            }

            var newFirstMember = firstMember.WithLeadingTrivia(firstMemberTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed));
            return compilationUnit.ReplaceNode(firstMember, newFirstMember);
        }

        private static NamespaceDeclarationSyntax EnsureLeadingEndOfLineForNamespace(NamespaceDeclarationSyntax namespaceDeclaration)
        {
            // If we already have usings then, assume the code is formatted how the user intended.
            // If the namespace contains no members there is no need to break up the usings from the other members.
            if (namespaceDeclaration.Usings.Count > 0 || namespaceDeclaration.Members.Count == 0)
            {
                return namespaceDeclaration;
            }

            var firstMember = namespaceDeclaration.Members.First();
            var firstMemberTrivia = firstMember.GetLeadingTrivia();

            // If the first member already contains a leading new line then, this will already break up the usings from these members.
            if (firstMemberTrivia.Count > 0 && firstMemberTrivia.First().IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return namespaceDeclaration;
            }

            var newFirstMember = firstMember.WithLeadingTrivia(firstMemberTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed));
            return namespaceDeclaration.ReplaceNode(firstMember, newFirstMember);
        }

        private static NamespaceDeclarationSyntax RemoveLeadingEndOfLinesFromNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
        {
            if (namespaceDeclaration.Members.Count == 0)
            {
                return namespaceDeclaration;
            }

            var firstMember = namespaceDeclaration.Members.First();
            var firstMemberTrivia = firstMember.GetLeadingTrivia();

            // If the first member already contains a leading new line then, this will already break up the usings from these members.
            if (firstMemberTrivia.Count == 0)
            {
                return namespaceDeclaration;
            }

            var newTrivia = SplitIntoLines(firstMemberTrivia)
                .SkipWhile(trivia => trivia.All(t => t.IsWhitespaceOrEndOfLine()) && trivia.Last().IsKind(SyntaxKind.EndOfLineTrivia))
                .SelectMany(t => t);

            var newFirstMember = firstMember.WithLeadingTrivia(newTrivia);
            return namespaceDeclaration.ReplaceNode(firstMember, newFirstMember);
        }

        private static SyntaxList<UsingDirectiveSyntax> FixUpUsingDirectives(IEnumerable<UsingDirectiveSyntax> usingDirectives)
        {
            var newUsingDirectives = usingDirectives.Select(FixUpUsingDirective);
            return new SyntaxList<UsingDirectiveSyntax>(newUsingDirectives);
        }

        private static UsingDirectiveSyntax FixUpUsingDirective(UsingDirectiveSyntax usingDirective)
        {
            var newUsingDirective = usingDirective.WithAdditionalAnnotations(Formatter.Annotation);
            var newName = newUsingDirective.Name.WithAdditionalAnnotations(Simplifier.Annotation);
            return newUsingDirective.WithName(newName);
        }

        private static IEnumerable<IEnumerable<SyntaxTrivia>> SplitIntoLines(SyntaxTriviaList triviaList)
        {
            var index = 0;
            for (var i = 0; i < triviaList.Count; i++)
            {
                if (triviaList[i].IsEndOfLine())
                {
                    yield return triviaList.TakeRange(index, i);
                    index = i + 1;
                }
            }

            if (index < triviaList.Count)
            {
                yield return triviaList.TakeRange(index, triviaList.Count - 1);
            }
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

        private static (CompilationUnitSyntax, ImmutableArray<SyntaxTrivia>) RemoveFileHeader(CompilationUnitSyntax syntaxRoot)
        {
            var fileHeader = GetFileHeader(syntaxRoot);
            var leadingTrivia = syntaxRoot.GetLeadingTrivia();

            for (var i = fileHeader.Length - 1; i >= 0; i--)
            {
                leadingTrivia = leadingTrivia.RemoveAt(i);
            }

            var newCompilationUnit = syntaxRoot.WithLeadingTrivia(leadingTrivia);

            return (newCompilationUnit, fileHeader);
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

        private static CompilationUnitSyntax AddFileHeader(CompilationUnitSyntax compilationUnit, ImmutableArray<SyntaxTrivia> fileHeader)
        {
            if (fileHeader.IsEmpty)
            {
                return compilationUnit;
            }

            // Add leading trivia to the first token.
            var firstToken = compilationUnit.GetFirstToken(includeZeroWidth: true);
            var newLeadingTrivia = firstToken.LeadingTrivia.InsertRange(0, fileHeader);
            var newFirstToken = firstToken.WithLeadingTrivia(newLeadingTrivia);

            return compilationUnit.ReplaceToken(firstToken, newFirstToken);
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
                var newDocument = await GetTransformedDocumentAsync(document, (CompilationUnitSyntax)syntaxRoot, options, fixAllContext.CancellationToken).ConfigureAwait(false);
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
