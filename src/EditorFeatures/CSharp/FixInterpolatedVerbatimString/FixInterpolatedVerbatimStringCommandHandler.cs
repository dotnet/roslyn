// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.FixInterpolatedVerbatimString
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(FixInterpolatedVerbatimStringCommandHandler))]
    internal sealed class FixInterpolatedVerbatimStringCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        public string DisplayName => CSharpEditorResources.Fix_interpolated_verbatim_string;

        private static int GetCharOffset(char ch)
        {
            switch (ch)
            {
                case '@': return 0;
                case '$': return 1;
                case '"': return 2;
                default: return -1;
            }
        }

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // We need to check for the token *after* the last character (@, $ or ") is typed, so defer to the editor first
            nextCommandHandler();

            var charOffset = GetCharOffset(args.TypedChar);
            if (charOffset != -1)
            {
                var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                if (caret != null)
                {
                    var startPosition = caret.Value.Position - charOffset - 1;
                    var snapshot = caret.Value.Snapshot;

                    if (startPosition >= 0 &&
                        snapshot[startPosition + 0] == '@' &&
                        snapshot[startPosition + 1] == '$' &&
                        snapshot[startPosition + 2] == '"')
                    {
                        var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                        if (document != null)
                        {
                            var root = document.GetSyntaxRootSynchronously(executionContext.OperationContext.UserCancellationToken);
                            var token = root.FindToken(startPosition);
                            if (token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
                            {
                                args.SubjectBuffer.Replace(new Span(startPosition, 2), "$@");
                            }
                        }
                    }
                }
            }
        }

        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler)
            => VSCommanding.CommandState.Unspecified;
    }
}
