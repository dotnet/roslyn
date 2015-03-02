﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    /// <summary>
    /// Represents a command which can be run from a REPL window.
    /// 
    /// This interface is a MEF contract and can be implemented and exported to add commands to the REPL window.
    /// </summary>
    public abstract class InteractiveWindowCommand : IInteractiveWindowCommand
    {
        public virtual IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            return SpecializedCollections.EmptyEnumerable<ClassificationSpan>();
        }

        public abstract Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments);
        public abstract string Description { get; }

        public virtual IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get { return null; }
        }

        public virtual IEnumerable<string> DetailedDescription
        {
            get { return null; }
        }

        public virtual string CommandLine
        {
            get { return null; }
        }

        public virtual string Name
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
