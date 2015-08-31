// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    internal sealed class CommandClassifierProvider : IClassifierProvider
    {
        [Import]
        public IStandardClassificationService ClassificationRegistry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            var commands = textBuffer.GetInteractiveWindow().GetInteractiveCommands();
            if (commands != null)
            {
                return new CommandClassifier(ClassificationRegistry, commands);
            }

            return null;
        }
    }
}
