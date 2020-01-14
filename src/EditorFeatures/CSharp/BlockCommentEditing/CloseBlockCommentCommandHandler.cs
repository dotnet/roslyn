// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(CloseBlockCommentCommandHandler))]
    [Order(After = nameof(BlockCommentEditingCommandHandler))]
    internal sealed class CloseBlockCommentCommandHandler : ICommandHandler<TypeCharCommandArgs>
    {
        [ImportingConstructor]
        public CloseBlockCommentCommandHandler()
        {
        }

        public string DisplayName => EditorFeaturesResources.Block_Comment_Editing;

        public bool ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            if (args.TypedChar == '/')
            {
                var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                if (caret != null)
                {
                    var (snapshot, position) = caret.Value;

                    // Check that the line is all whitespace ending with an asterisk and a single space (| marks caret position):
                    // * |
                    if (position >= 2 &&
                        snapshot[position - 1] == ' ' &&
                        snapshot[position - 2] == '*')
                    {
                        var line = snapshot.GetLineFromPosition(position);
                        if (line.End == position &&
                            line.IsEmptyOrWhitespace(0, line.Length - 2))
                        {
                            if (args.SubjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString) &&
                                BlockCommentEditingCommandHandler.IsCaretInsideBlockCommentSyntax(caret.Value))
                            {
                                args.SubjectBuffer.Replace(new VisualStudio.Text.Span(position - 1, 1), "/");
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public CommandState GetCommandState(TypeCharCommandArgs args)
            => CommandState.Unspecified;
    }
}
