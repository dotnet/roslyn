// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    /// <summary>
    /// Represents a command which can be run from a REPL window.
    /// 
    /// This interface is a MEF contract and can be implemented and exported to add commands to the REPL window.
    /// </summary>
    [ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    internal abstract class InteractiveWindowCommand : IInteractiveWindowCommand
    {
        public abstract Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments);

        public abstract string Description { get; }

        public abstract IEnumerable<string> Names { get; }

        public virtual IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            return Enumerable.Empty<ClassificationSpan>();
        }

        public virtual string CommandLine
        {
            get { return null; }
        }

        public virtual IEnumerable<string> DetailedDescription
        {
            get { return null; }
        }

        public virtual IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get { return null; }
        }

        protected void ReportInvalidArguments(IInteractiveWindow window)
        {
            var commands = (IInteractiveWindowCommands)window.Properties[typeof(IInteractiveWindowCommands)];
            commands.DisplayCommandUsage(this, window.ErrorOutputWriter, displayDetails: false);
        }
    }
}
