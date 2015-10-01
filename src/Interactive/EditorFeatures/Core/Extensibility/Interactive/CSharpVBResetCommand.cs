// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor.Interactive;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    /// <summary>
    /// Represents a reset command which can be run from a REPL window.
    /// </summary>
    [Export(typeof(IInteractiveWindowCommand))]
    [ContentType(CSharpVBInteractiveCommandsContentTypes.CSharpVBInteractiveCommandContentTypeName)]
    internal sealed class ResetCommand : IInteractiveWindowCommand
    {
        private const string CommandName = "reset";
        private const string NoConfigParameterName = "noconfig";
        private static readonly int NoConfigParameterNameLength = NoConfigParameterName.Length;
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        public ResetCommand(IStandardClassificationService registry)
        {
            _registry = registry;
        }

        public string Description
        {
            get { return InteractiveEditorFeaturesResources.ResetCommandDescription; }
        }

        public IEnumerable<string> DetailedDescription
        {
            get { return null; }
        }

        public IEnumerable<string> Names
        {
            get { yield return CommandName; }
        }

        public string CommandLine
        {
            get { return "[" + NoConfigParameterName + "]"; }
        }

        public IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get
            {
                yield return new KeyValuePair<string, string>(NoConfigParameterName, InteractiveEditorFeaturesResources.ResetCommandParametersDescription);
            }
        }

        public Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            bool initialize;
            if (!TryParseArguments(arguments, out initialize))
            {
                ReportInvalidArguments(window);
                return ExecutionResult.Failed;
            }

            return window.Operations.ResetAsync(initialize);
        }

        internal static string BuildCommandLine(bool initialize)
        {
            string result = CommandName;
            return initialize ? result : result + " " + NoConfigParameterName;
        }

        public IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            string arguments = snapshot.GetText(argumentsSpan);
            int argumentsStart = argumentsSpan.Start;
            foreach (var pos in GetNoConfigPositions(arguments))
            {
                var snapshotSpan = new SnapshotSpan(snapshot, new Span(argumentsStart + pos, NoConfigParameterNameLength));
                yield return new ClassificationSpan(snapshotSpan, _registry.Keyword);
            }
        }
        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal static IEnumerable<int> GetNoConfigPositions(string arguments)
        {
            int startIndex = 0;
            while (true)
            {
                int index = arguments.IndexOf(NoConfigParameterName, startIndex, StringComparison.Ordinal);
                if (index < 0) yield break;

                if ((index == 0 || char.IsWhiteSpace(arguments[index - 1])) &&
                    (index + NoConfigParameterNameLength == arguments.Length || char.IsWhiteSpace(arguments[index + NoConfigParameterNameLength])))
                {
                    yield return index;
                }

                startIndex = index + NoConfigParameterNameLength;
            }
        }

        /// <remarks>
        /// Accessibility is internal for testing.
        /// </remarks>
        internal static bool TryParseArguments(string arguments, out bool initialize)
        {
            var trimmed = arguments.Trim();
            if (trimmed.Length == 0)
            {
                initialize = true;
                return true;
            }
            else if (string.Equals(trimmed, NoConfigParameterName, StringComparison.Ordinal))
            {
                initialize = false;
                return true;
            }

            initialize = false;
            return false;
        }


        private void ReportInvalidArguments(IInteractiveWindow window)
        {
            var commands = (IInteractiveWindowCommands)window.Properties[typeof(IInteractiveWindowCommands)];
            commands.DisplayCommandUsage(this, window.ErrorOutputWriter, displayDetails: false);
        }
    }
}

