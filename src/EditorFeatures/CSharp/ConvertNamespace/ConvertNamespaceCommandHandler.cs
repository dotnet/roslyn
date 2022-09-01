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
using Microsoft.CodeAnalysis.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
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
using Roslyn.Utilities;

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
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertNamespaceCommandHandler(
            ITextUndoHistoryRegistry textUndoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService, IGlobalOptionService globalOptions)
        {
            _textUndoHistoryRegistry = textUndoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _globalOptions = globalOptions;
        }

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public string DisplayName => CSharpAnalyzersResources.Convert_to_file_scoped_namespace;

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Attempt to convert the block-namespace to a file-scoped namespace if we're at the right location.
            var (convertedText, semicolonSpan) = ConvertNamespace(args, executionContext);

            // No matter if we succeeded or not, insert the semicolon.  This way, when we convert, the user can still
            // hit ctrl-z to get back to the code with just the semicolon inserted.
            nextCommandHandler();

            // If we weren't on a block namespace (or couldn't convert it for some reason), then bail out after
            // inserting the semicolon.
            if (convertedText == null)
                return;

            // Otherwise, make a transaction for the edit and replace the buffer with the final text.
            using var transaction = CaretPreservingEditTransaction.TryCreate(
                this.DisplayName, args.TextView, _textUndoHistoryRegistry, _editorOperationsFactoryService);

            var edit = args.SubjectBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null);
            edit.Replace(new Span(0, args.SubjectBuffer.CurrentSnapshot.Length), convertedText.ToString());

            edit.Apply();

            // Place the caret right after the semicolon of the file-scoped namespace.
            args.TextView.Caret.MoveTo(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, semicolonSpan.End));

            transaction?.Complete();
        }

        /// <summary>
        /// Returns the updated file contents if semicolon is typed after a block-scoped namespace name that can be
        /// converted.
        /// </summary>
        private (SourceText? convertedText, TextSpan semicolonSpan) ConvertNamespace(
            TypeCharCommandArgs args,
            CommandExecutionContext executionContext)
        {
            if (args.TypedChar != ';' || !args.TextView.Selection.IsEmpty)
                return default;

            if (!_globalOptions.GetOption(FeatureOnOffOptions.AutomaticallyCompleteStatementOnSemicolon))
                return default;

            var subjectBuffer = args.SubjectBuffer;
            var caretOpt = args.TextView.GetCaretPoint(subjectBuffer);
            if (!caretOpt.HasValue)
                return default;

            var caret = caretOpt.Value.Position;
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return default;

            var cancellationToken = executionContext.OperationContext.UserCancellationToken;
            var root = (CompilationUnitSyntax)document.GetRequiredSyntaxRootSynchronously(cancellationToken);

            // User has to be *after* an identifier token.
            var token = root.FindToken(caret);
            if (token.Kind() != SyntaxKind.IdentifierToken)
                return default;

            if (caret < token.Span.End ||
                caret >= token.FullSpan.End)
            {
                return default;
            }

            var namespaceDecl = token.GetRequiredParent().GetAncestor<NamespaceDeclarationSyntax>();
            if (namespaceDecl == null)
                return default;

            // That identifier token has to be the last part of a namespace name.
            if (namespaceDecl.Name.GetLastToken() != token)
                return default;

            // Pass in our special options, and C#10 so that if we can convert this to file-scoped, we will.
            if (!ConvertNamespaceAnalysis.CanOfferUseFileScoped(s_optionSet, root, namespaceDecl, forAnalyzer: true, LanguageVersion.CSharp10))
                return default;

            var (converted, semicolonSpan) = ConvertNamespaceTransform.ConvertNamespaceDeclarationAsync(document, namespaceDecl, cancellationToken).WaitAndGetResult(cancellationToken);
            var text = converted.GetTextSynchronously(cancellationToken);
            return (text, semicolonSpan);
        }
    }
}
