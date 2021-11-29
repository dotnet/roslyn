// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement
{
    /// <summary>
    /// Converts a block-scoped namespace to a file-scoped one if the user types <c>;</c> after its name.
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [Export]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(ConvertNamespaceCommandHandler))]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal sealed class ConvertNamespaceCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        /// <summary>
        /// Annotation used so we can find the semicolon after formatting so that we can properly place the caret.
        /// </summary>
        private static readonly SyntaxAnnotation s_annotation = new();

        /// <summary>
        /// A fake option set where the 'use file scoped' namespace option is on.  That way we can call into the helpers
        /// and have the results come back positive for converting to file-scoped regardless of the current option
        /// value.
        /// </summary>
        private static readonly OptionSet s_optionSet = new OptionValueSet(
            ImmutableDictionary<OptionKey, object?>.Empty.Add(
                new OptionKey(CSharpCodeStyleOptions.NamespaceDeclarations.ToPublicOption()),
                new CodeStyleOption2<NamespaceDeclarationPreference>(
                    NamespaceDeclarationPreference.FileScoped,
                    NotificationOption2.Suggestion)));

        private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertNamespaceCommandHandler(
            ITextUndoHistoryRegistry textUndoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _textUndoHistoryRegistry = textUndoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public string DisplayName => CSharpAnalyzersResources.Convert_to_file_scoped_namespace;

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Attempt to convert the block-namespace to a file-scoped namespace if we're at the right location.
            var convertedRoot = ConvertNamespaceCommandHandler.ConvertNamespace(args, executionContext);

            // No matter if we succeeded or not, insert the semicolon.  This way, when we convert, the user can still
            // hit ctrl-z to get back to the code with just the semicolon inserted.
            nextCommandHandler();

            // If we weren't on a block namespace (or couldn't convert it for some reason), then bail out after
            // inserting the semicolon.
            if (convertedRoot == null)
                return;

            // Otherwise, make a transaction for the edit and replace the buffer with the final text.
            using var transaction = CaretPreservingEditTransaction.TryCreate(
                this.DisplayName, args.TextView, _textUndoHistoryRegistry, _editorOperationsFactoryService);

            var edit = args.SubjectBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null);
            edit.Replace(new Span(0, args.SubjectBuffer.CurrentSnapshot.Length), convertedRoot.ToFullString());

            edit.Apply();

            // Attempt to place the caret right after the semicolon of the file-scoped namespace.
            var annotatedToken = convertedRoot.GetAnnotatedTokens(s_annotation).FirstOrDefault();
            if (annotatedToken != default)
                args.TextView.Caret.MoveTo(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, annotatedToken.Span.End));

            transaction?.Complete();
        }

        /// <summary>
        /// Returns true if semicolon is typed after a namespace name that should be converted.
        /// </summary>
        private static CompilationUnitSyntax? ConvertNamespace(
            TypeCharCommandArgs args,
            CommandExecutionContext executionContext)
        {
            if (args.TypedChar != ';' || !args.TextView.Selection.IsEmpty)
                return null;

            var subjectBuffer = args.SubjectBuffer;
            var caretOpt = args.TextView.GetCaretPoint(subjectBuffer);
            if (!caretOpt.HasValue)
                return null;

            var caret = caretOpt.Value.Position;
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return null;

            var cancellationToken = executionContext.OperationContext.UserCancellationToken;
            var root = (CompilationUnitSyntax)document.GetRequiredSyntaxRootSynchronously(cancellationToken);

            // User has to be *after* an identifier token.
            var token = root.FindToken(caret);
            if (token.Kind() != SyntaxKind.IdentifierToken)
                return null;

            if (caret < token.Span.End ||
                caret >= token.FullSpan.End)
            {
                return null;
            }

            var namespaceDecl = token.GetRequiredParent().GetAncestor<NamespaceDeclarationSyntax>();
            if (namespaceDecl == null)
                return null;

            // That identifier token has to be the last part of a namespace name.
            if (namespaceDecl.Name.GetLastToken() != token)
                return null;

            // Pass in our special options, and C#10 so that if we can convert this to file-scoped, we will.
            if (!ConvertNamespaceAnalysis.CanOfferUseFileScoped(s_optionSet, root, namespaceDecl, forAnalyzer: true, LanguageVersion.CSharp10))
                return null;

            var fileScopedNamespace = (FileScopedNamespaceDeclarationSyntax)ConvertNamespaceTransform.Convert(namespaceDecl);

            // Place an annotation on the semicolon so that we can find it post-formatting to place the caret.
            fileScopedNamespace = fileScopedNamespace.WithSemicolonToken(
                fileScopedNamespace.SemicolonToken.WithAdditionalAnnotations(s_annotation));

            var convertedRoot = root.ReplaceNode(namespaceDecl, fileScopedNamespace);
            var formattedRoot = (CompilationUnitSyntax)Formatter.Format(
                convertedRoot, Formatter.Annotation,
                document.Project.Solution.Workspace,
                options: null, rules: null, cancellationToken);

            return formattedRoot;
        }
    }
}
