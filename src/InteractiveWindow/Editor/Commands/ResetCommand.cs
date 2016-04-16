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
        private static readonly int s_noConfigParameterNameLength = NoConfigParameterName.Length;
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        public ResetCommand(IStandardClassificationService registry)
        {
            _registry = registry;
        }

        public override string Description
        {
            get { return InteractiveWindowResources.ResetCommandDescription; }
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
                yield return new KeyValuePair<string, string>(NoConfigParameterName, InteractiveWindowResources.ResetCommandParametersDescription);
            }
        }

        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            bool initialize;
            if (!TryParseArguments(arguments, out initialize))
            {
                ReportInvalidArguments(window);
                return ExecutionResult.Failed;
            }

            return window.Operations.ResetAsync(initialize);
        }

        public override IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            string arguments = snapshot.GetText(argumentsSpan);
            int argumentsStart = argumentsSpan.Start;
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
            int startIndex = 0;
            while (true)
            {
                int index = arguments.IndexOf(NoConfigParameterName, startIndex, StringComparison.Ordinal);
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
        /// Internal for testing.
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
    }
}
