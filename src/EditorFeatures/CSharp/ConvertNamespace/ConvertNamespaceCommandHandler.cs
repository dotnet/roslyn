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
        private static readonly SyntaxAnnotation s_annotation = new();
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
            var convertedRoot = ConvertNamespace(args, executionContext);
            nextCommandHandler();

            if (convertedRoot == null)
                return;

            using var transaction = CaretPreservingEditTransaction.TryCreate(
                this.DisplayName, args.TextView, _textUndoHistoryRegistry, _editorOperationsFactoryService);

            var edit = args.SubjectBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null);
            edit.Replace(new Span(0, args.SubjectBuffer.CurrentSnapshot.Length), convertedRoot.ToFullString());

            edit.Apply();

            var annotatedToken = convertedRoot.GetAnnotatedTokens(s_annotation).FirstOrDefault();
            if (annotatedToken != default)
                args.TextView.Caret.MoveTo(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, annotatedToken.Span.End));

            transaction?.Complete();
        }

        /// <summary>
        /// Returns true if semicolon is typed after a namespace name that should be converted.
        /// </summary>
        private CompilationUnitSyntax? ConvertNamespace(
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

            if (namespaceDecl.Name.GetLastToken() != token)
                return null;

            if (!ConvertNamespaceAnalysis.CanOfferUseFileScoped(s_optionSet, root, namespaceDecl, forAnalyzer: true, LanguageVersion.CSharp10))
                return null;

            var fileScopedNamespace = (FileScopedNamespaceDeclarationSyntax)ConvertNamespaceTransform.Convert(namespaceDecl);
            fileScopedNamespace = fileScopedNamespace.WithSemicolonToken(
                fileScopedNamespace.SemicolonToken.WithAdditionalAnnotations(s_annotation));

            var convertedRoot = root.ReplaceNode(namespaceDecl, fileScopedNamespace);
            var formattedRoot = (CompilationUnitSyntax)Formatter.Format(
                convertedRoot, Formatter.Annotation,
                document.Project.Solution.Workspace,
                options: null, rules: null, cancellationToken);

            return formattedRoot;
            // var finalSemicolonLocation = formattedTextChanges.GetFormattedRoot(cancellationToken).GetAnnotatedTokens(s_annotation).Single();
        }
    }
}
