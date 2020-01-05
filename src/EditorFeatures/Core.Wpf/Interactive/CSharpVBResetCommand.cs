// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.VisualStudio.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Interactive
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
        private static readonly int s_noConfigParameterNameLength = NoConfigParameterName.Length;
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        public ResetCommand(IStandardClassificationService registry)
        {
            _registry = registry;
        }

        public string Description
        {
            get { return InteractiveEditorFeaturesResources.Reset_the_execution_environment_to_the_initial_state_keep_history; }
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
            get { return "[" + NoConfigParameterName + "] [32|64]"; }
        }

        public IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get
            {
                yield return new KeyValuePair<string, string>(NoConfigParameterName, InteractiveEditorFeaturesResources.Reset_to_a_clean_environment_only_mscorlib_referenced_do_not_run_initialization_script);
                yield return new KeyValuePair<string, string>("32|64", $"Interactive host process bitness.");
            }
        }

        public Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            if (!TryParseArguments(arguments, out var initialize, out var is64bit))
            {
                ReportInvalidArguments(window);
                return ExecutionResult.Failed;
            }

            var evaluator = (InteractiveEvaluator)window.Evaluator;
            evaluator.ResetOptions = new InteractiveEvaluatorResetOptions(is64bit);
            return window.Operations.ResetAsync(initialize);
        }

        public IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            var arguments = snapshot.GetText(argumentsSpan);
            var argumentsStart = argumentsSpan.Start;
            foreach (var pos in GetNoConfigPositions(arguments))
            {
                var snapshotSpan = new SnapshotSpan(snapshot, new Span(argumentsStart + pos, s_noConfigParameterNameLength));
                yield return new ClassificationSpan(snapshotSpan, _registry.Keyword);
            }
        }
        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal static IEnumerable<int> GetNoConfigPositions(string arguments)
        {
            var startIndex = 0;
            while (true)
            {
                var index = arguments.IndexOf(NoConfigParameterName, startIndex, StringComparison.Ordinal);
                if (index < 0) yield break;

                if ((index == 0 || char.IsWhiteSpace(arguments[index - 1])) &&
                    (index + s_noConfigParameterNameLength == arguments.Length || char.IsWhiteSpace(arguments[index + s_noConfigParameterNameLength])))
                {
                    yield return index;
                }

                startIndex = index + s_noConfigParameterNameLength;
            }
        }

        /// <remarks>
        /// Accessibility is internal for testing.
        /// </remarks>
        internal static bool TryParseArguments(string arguments, out bool initialize, out bool? is64bit)
        {
            is64bit = null;
            initialize = true;

            var noConfigSpecified = false;

            foreach (var argument in arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                switch (argument)
                {
                    case "32":
                        if (is64bit != null)
                        {
                            return false;
                        }

                        is64bit = false;
                        break;

                    case "64":
                        if (is64bit != null)
                        {
                            return false;
                        }

                        is64bit = true;
                        break;

                    case NoConfigParameterName:
                        if (noConfigSpecified)
                        {
                            return false;
                        }

                        noConfigSpecified = true;
                        initialize = false;
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }

        internal static string GetCommandLine(bool initialize, bool? is64bit)
            => CommandName + (initialize ? "" : " " + NoConfigParameterName) + (is64bit == null ? "" : is64bit.Value ? " 64" : " 32");

        private void ReportInvalidArguments(IInteractiveWindow window)
        {
            var commands = (IInteractiveWindowCommands)window.Properties[typeof(IInteractiveWindowCommands)];
            commands.DisplayCommandUsage(this, window.ErrorOutputWriter, displayDetails: false);
        }
    }
}

