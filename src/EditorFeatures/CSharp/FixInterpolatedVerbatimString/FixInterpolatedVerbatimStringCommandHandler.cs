// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.FixInterpolatedVerbatimString
{
    /// <summary>
    /// Replaces <c>@$"</c> with <c>$@"</c>, which is the preferred and until C# 8.0 the only supported form
    /// of an interpolated verbatim string start token. In C# 8.0 we still auto-correct to this form for consistency.
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(FixInterpolatedVerbatimStringCommandHandler))]
    internal sealed class FixInterpolatedVerbatimStringCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FixInterpolatedVerbatimStringCommandHandler()
        {
        }

        public string DisplayName => CSharpEditorResources.Fix_interpolated_verbatim_string;

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // We need to check for the token *after* the opening quote is typed, so defer to the editor first
            nextCommandHandler();

            var cancellationToken = executionContext.OperationContext.UserCancellationToken;
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                ExecuteCommandWorker(args, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // According to Editor command handler API guidelines, it's best if we return early if cancellation
                // is requested instead of throwing. Otherwise, we could end up in an invalid state due to already
                // calling nextCommandHandler().
            }
        }

        private static void ExecuteCommandWorker(TypeCharCommandArgs args, CancellationToken cancellationToken)
        {
            if (args.TypedChar == '"')
            {
                var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                if (caret != null)
                {
                    var position = caret.Value.Position;
                    var snapshot = caret.Value.Snapshot;

                    if (position >= 3 &&
                        snapshot[position - 1] == '"' &&
                        snapshot[position - 2] == '$' &&
                        snapshot[position - 3] == '@')
                    {
                        var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                        if (document != null)
                        {
                            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
                            var token = root.FindToken(position - 3);
                            if (token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
                            {
                                args.SubjectBuffer.Replace(new Span(position - 3, 2), "$@");
                            }
                        }
                    }
                }
            }
        }

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();
    }
}
