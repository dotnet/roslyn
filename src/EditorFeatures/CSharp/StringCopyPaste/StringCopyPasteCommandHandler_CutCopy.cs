// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
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

        private static (string? dataToStore, IStringCopyPasteService service) CaptureCutCopyInformation(
            ITextView textView, ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return default;

            var copyPasteService = document.Project.Solution.Workspace.Services.GetService<IStringCopyPasteService>();
            if (copyPasteService == null)
                return default;

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
            var stringData = TryGetStringCopyPasteData(virtualCharService, stringExpression, span.Span.ToTextSpan());

            return (stringData?.ToJson(), copyPasteService);
        }

        private static StringCopyPasteData? TryGetStringCopyPasteData(IVirtualCharLanguageService virtualCharService, ExpressionSyntax stringExpression, TextSpan span)
            => stringExpression switch
            {
                LiteralExpressionSyntax literal => TryGetStringCopyPasteDataForLiteral(virtualCharService, literal, span),
                InterpolatedStringExpressionSyntax interpolatedString => TryGetStringCopyPasteDataForInterpolatedString(virtualCharService, interpolatedString, span),
                _ => throw ExceptionUtilities.UnexpectedValue(stringExpression.Kind()),
            };

        private static StringCopyPasteData? TryGetStringCopyPasteDataForLiteral(
            IVirtualCharLanguageService virtualCharService, LiteralExpressionSyntax literal, TextSpan span)
        {
            if (!TryGetContentForSpan(virtualCharService, literal.Token, span, out var content))
                return null;

            return new StringCopyPasteData(ImmutableArray.Create(content));
        }

        private static bool TryGetNormalizedStringForSpan(
            IVirtualCharLanguageService virtualCharService,
            SyntaxToken token,
            TextSpan span,
            [NotNullWhen(true)] out string? normalizedText)
        {
            normalizedText = null;
            var virtualChars = virtualCharService.TryConvertToVirtualChars(token);
            if (virtualChars.IsDefaultOrEmpty)
                return false;

            var firstOverlappingChar = virtualChars.FirstOrNull(vc => vc.Span.OverlapsWith(span));
            var lastOverlappingChar = virtualChars.LastOrNull(vc => vc.Span.OverlapsWith(span));

            if (firstOverlappingChar is null || lastOverlappingChar is null)
                return false;

            var firstCharIndexInclusive = virtualChars.IndexOf(firstOverlappingChar.Value);
            var lastCharIndexInclusive = virtualChars.IndexOf(lastOverlappingChar.Value);

            var subsequence = virtualChars.GetSubSequence(TextSpan.FromBounds(firstCharIndexInclusive, lastCharIndexInclusive + 1));
            normalizedText = subsequence.CreateString();
            return true;
        }

        private static bool TryGetContentForSpan(
            IVirtualCharLanguageService virtualCharService,
            SyntaxToken token,
            TextSpan span,
            out StringCopyPasteContent content)
        {
            if (!TryGetNormalizedStringForSpan(virtualCharService, token, span, out var text))
            {
                content = default;
                return false;
            }
            else
            {
                content = StringCopyPasteContent.ForText(text);
                return true;
            }
        }

        private static StringCopyPasteData? TryGetStringCopyPasteDataForInterpolatedString(
            IVirtualCharLanguageService virtualCharService,
            InterpolatedStringExpressionSyntax interpolatedString,
            TextSpan span)
        {
            using var _ = ArrayBuilder<StringCopyPasteContent>.GetInstance(out var result);

            foreach (var interpolatedContent in interpolatedString.Contents)
            {
                if (interpolatedContent.Span.OverlapsWith(span))
                {
                    if (interpolatedContent is InterpolationSyntax interpolation)
                    {
                        // If the user copies a portion of an interpolation, just treat this as a non-smart copy paste for simplicity.
                        if (!span.Contains(interpolation.Span))
                            return null;

                        var formatClause = (string?)null;
                        if (interpolation.FormatClause != null &&
                            !TryGetNormalizedStringForSpan(virtualCharService, interpolation.FormatClause.FormatStringToken, span, out formatClause))
                        {
                            return null;
                        }

                        result.Add(StringCopyPasteContent.ForInterpolation(
                            interpolation.Expression.ToFullString(),
                            interpolation.AlignmentClause?.ToFullString(),
                            formatClause));
                    }
                    else if (interpolatedContent is InterpolatedStringTextSyntax stringText)
                    {
                        if (!TryGetContentForSpan(virtualCharService, stringText.TextToken, span, out var content))
                            return null;

                        result.Add(content);
                    }
                }
            }

            return new StringCopyPasteData(result.ToImmutable());
        }
    }
}
