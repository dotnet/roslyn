using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    internal sealed class CommandClassifier : IClassifier
    {
        private readonly IStandardClassificationService registry;
        private readonly IInteractiveWindowCommands commands;

        public CommandClassifier(IStandardClassificationService registry, IInteractiveWindowCommands commands)
        {
            this.registry = registry;
            this.commands = commands;
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return commands.Classify(span).ToArray();
        }

#pragma warning disable 67 // unused event
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67
    }
}