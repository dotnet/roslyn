// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.ConvertCast;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    internal partial class StringCopyPasteCommandHandler :
        IChainedCommandHandler<CutCommandArgs>,
        IChainedCommandHandler<CopyCommandArgs>
    {
        private const string KeyAndVersion = nameof(StringCopyPasteCommandHandler) + "V1";

        public CommandState GetCommandState(CutCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public CommandState GetCommandState(CopyCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(CutCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
            => ExecuteCutOrCopyCommand(args.TextView, args.SubjectBuffer, nextCommandHandler, executionContext);

        public void ExecuteCommand(CopyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
            => ExecuteCutOrCopyCommand(args.TextView, args.SubjectBuffer, nextCommandHandler, executionContext);

        private void ExecuteCutOrCopyCommand(ITextView textView, ITextBuffer subjectBuffer, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);
            var (dataToStore, copyPasteService) = CaptureCutCopyInformation(textView, subjectBuffer, executionContext.OperationContext.UserCancellationToken);

            // Ensure that the copy always goes through all other handlers.
            nextCommandHandler();

            if (dataToStore != null)
                copyPasteService.TrySetClipboardData(KeyAndVersion, dataToStore);
        }

        private (string dataToStore, IStringCopyPasteService service) CaptureCutCopyInformation(
            ITextView textView, ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return default;

            var copyPasteService = document.Project.Solution.Workspace.Services.GetService<IStringCopyPasteService>();
            if (copyPasteService == null)
                return default;

            //var editorOptions = textView.Options;
            //var  editorOptions.GetOptionValue<bool>(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
            if (spans.Count != 1)
                return default;

            var span = spans[0];
            var snapshot = span.Snapshot;
            if (span.IsEmpty)
            {
                // cut/copy on an empty span means "cut/copy the entire line".
                var line = snapshot.GetLineFromPosition(span.Start);
                span = line.ExtentIncludingLineBreak;
            }

            var stringExpression = TryGetCompatibleContainingStringExpression(
                document, new NormalizedSnapshotSpanCollection(span), cancellationToken);
            if (stringExpression is null)
                return default;

            var virtualCharService = document.GetRequiredLanguageService<IVirtualCharLanguageService>();
            var stringData = TryGetStringCopyPasteData(virtualCharService, snapshot.AsText(), stringExpression, span.Span.ToTextSpan());
            if (stringData is null)
                return default;

            using var stream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(
                typeof(StringCopyPasteData), new[] { typeof(StringCopyPasteContent) });
            serializer.WriteObject(stream, stringData);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            copyPasteService.TrySetClipboardData(KeyAndVersion, json);
        }

        private StringCopyPasteData? TryGetStringCopyPasteData(IVirtualCharLanguageService virtualCharService, SourceText text, ExpressionSyntax stringExpression, TextSpan span)
            => stringExpression switch
            {
                LiteralExpressionSyntax literal => TryGetStringCopyPasteDataForLiteral(virtualCharService, text, literal, span),
                InterpolatedStringExpressionSyntax interpolatedString => TryGetStringCopyPasteDataForInterpolatedString(text, interpolatedString, span),
                _ => throw ExceptionUtilities.UnexpectedValue(stringExpression.Kind()),
            };

        private StringCopyPasteData? TryGetStringCopyPasteDataForLiteral(
            IVirtualCharLanguageService virtualCharService, SourceText text, LiteralExpressionSyntax literal, TextSpan span)
        {
            var virtualChars = virtualCharService.TryConvertToVirtualChars(literal.Token);
            if (virtualChars.IsDefaultOrEmpty)
                return null;

            var firstChar = virtualChars.FirstOrNull(vc => vc.Span.Start == span.Start);
            var lastChar = virtualChars.LastOrNull(vc => vc.Span.End == span.End);

            if (firstChar is null || lastChar is null)
                return null;

            var firstCharIndex = virtualChars.IndexOf(firstChar.Value);
            var lastCharIndex = virtualChars.IndexOf(lastChar.Value);

        }

        private StringCopyPasteData? TryGetStringCopyPasteDataForInterpolatedString(SourceText text, InterpolatedStringExpressionSyntax interpolatedString, TextSpan span)
        {
            return null;
        }
    }

    [DataContract]
    internal class StringCopyPasteData
    {
        [DataMember(Order = 0)]
        public readonly ImmutableArray<StringCopyPasteContent> Contents;

        public StringCopyPasteData(ImmutableArray<StringCopyPasteContent> contents)
        {
            Contents = contents;
        }
    }

    internal enum StringCopyPasteContentKind
    {
        Text,
        InterpolationCode, // When an interpolation is copied.
    }

    [DataContract]
    internal readonly struct StringCopyPasteContent
    {
        [DataMember(Order = 0)]
        public readonly StringCopyPasteContentKind Kind;

        [DataMember(Order = 1)]
        public readonly string Data;

        public StringCopyPasteContent(StringCopyPasteContentKind kind, string data)
        {
            Kind = kind;
            Data = data;
        }
    }
}
