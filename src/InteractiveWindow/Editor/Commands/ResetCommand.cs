// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    [Export(typeof(IInteractiveWindowCommand))]
    internal sealed class ResetCommand : InteractiveWindowCommand
    {
        private const string CommandName = "reset";
        private const string NoConfigParameterName = "noconfig";
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        public ResetCommand(IStandardClassificationService registry)
        {
            _registry = registry;
        }

        public override string Description
        {
            // TODO: Needs localization...
            get { return "Reset the execution environment to the initial state, keep history."; }
        }

        public override IEnumerable<string> Names
        {
            get { yield return CommandName; }
        }

        public override string CommandLine
        {
            get { return "[" + NoConfigParameterName + "]"; }
        }

        public override IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get
            {
                // TODO: Needs localization...
                yield return new KeyValuePair<string, string>(NoConfigParameterName, "Reset to a clean environment (only mscorlib referenced), do not run initialization script.");
            }
        }

        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            int noConfigStart, noConfigEnd;
            if (!TryParseArguments(arguments, out noConfigStart, out noConfigEnd))
            {
                ReportInvalidArguments(window);
                return ExecutionResult.Failed;
            }

            return window.Operations.ResetAsync(initialize: noConfigStart > -1);
        }

        internal static string BuildCommandLine(bool initialize)
        {
            string result = CommandName;
            return initialize ? result : result + " " + NoConfigParameterName;
        }

        public override IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            string arguments = snapshot.GetText(argumentsSpan);

            int noConfigStart, noConfigEnd;
            if (TryParseArguments(arguments, out noConfigStart, out noConfigEnd))
            {
                if (noConfigStart > -1)
                {
                    yield return new ClassificationSpan(new SnapshotSpan(snapshot, Span.FromBounds(argumentsSpan.Start + noConfigStart, argumentsSpan.Start + noConfigEnd)), _registry.Keyword);
                }
            }
        }

        private static bool TryParseArguments(string arguments, out int noConfigStart, out int noConfigEnd)
        {
            noConfigStart = noConfigEnd = -1;

            string noconfig = arguments.Trim();
            if (noconfig.Length == 0)
            {
                return true;
            }

            if (string.Compare(noconfig, NoConfigParameterName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                noConfigStart = arguments.IndexOf(noconfig, StringComparison.OrdinalIgnoreCase);
                noConfigEnd = noConfigStart + noconfig.Length;
                return true;
            }

            return false;
        }
    }
}
