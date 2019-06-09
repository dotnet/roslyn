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
    /// <summary>
    /// Replaces <c>@$"</c> with <c>$@"</c>, which is the preferred and until C# 8.0 the only supported form
    /// of an interpolated verbatim string start token. In C# 8.0 we still auto-correct to this form for consistency.
    /// </summary>
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(FixInterpolatedVerbatimStringCommandHandler))]
    internal sealed class FixInterpolatedVerbatimStringCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        [ImportingConstructor]
        public FixInterpolatedVerbatimStringCommandHandler()
        {
        }

        public string DisplayName => CSharpEditorResources.Fix_interpolated_verbatim_string;

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // We need to check for the token *after* the opening quote is typed, so defer to the editor first
            nextCommandHandler();

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
                            var root = document.GetSyntaxRootSynchronously(executionContext.OperationContext.UserCancellationToken);
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

        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler)
            => nextCommandHandler();
    }
}
