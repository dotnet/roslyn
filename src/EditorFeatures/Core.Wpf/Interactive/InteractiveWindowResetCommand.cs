// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias InteractiveHost;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Represents a reset command which can be run from a REPL window.
    /// </summary>
    [Export(typeof(IInteractiveWindowCommand))]
    [ContentType(InteractiveWindowContentTypes.CommandContentTypeName)]
    internal sealed class InteractiveWindowResetCommand : IInteractiveWindowCommand
    {
        private const string CommandName = "reset";
        private const string NoConfigParameterName = "noconfig";
        private const string PlatformCore = "core";
        private const string PlatformDesktop32 = "32";
        private const string PlatformDesktop64 = "64";
        private const string PlatformNames = PlatformCore + "|" + PlatformDesktop32 + "|" + PlatformDesktop64;

        private static readonly int s_noConfigParameterNameLength = NoConfigParameterName.Length;
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveWindowResetCommand(IStandardClassificationService registry)
            => _registry = registry;

        public string Description
            => EditorFeaturesWpfResources.Reset_the_execution_environment_to_the_initial_state_and_keep_history_with_the_option_to_switch_the_runtime_of_the_host_process;

        public IEnumerable<string> DetailedDescription
            => null;

        public IEnumerable<string> Names
            => SpecializedCollections.SingletonEnumerable(CommandName);

        public string CommandLine
            => "[" + NoConfigParameterName + "] [" + PlatformNames + "]";

        public IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get
            {
                yield return new KeyValuePair<string, string>(NoConfigParameterName, EditorFeaturesWpfResources.Reset_to_a_clean_environment_only_mscorlib_referenced_do_not_run_initialization_script);
                yield return new KeyValuePair<string, string>(PlatformNames, EditorFeaturesWpfResources.Interactive_host_process_platform);
            }
        }

        public Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            if (!TryParseArguments(arguments, out var initialize, out var platform))
            {
                ReportInvalidArguments(window);
                return ExecutionResult.Failed;
            }

            var evaluator = (CSharpInteractiveEvaluator)window.Evaluator;
            evaluator.ResetOptions = new InteractiveEvaluatorResetOptions(platform);
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
                if (index < 0)
                    yield break;

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
        internal static bool TryParseArguments(string arguments, out bool initialize, out InteractiveHostPlatform? platform)
        {
            platform = null;
            initialize = true;

            var noConfigSpecified = false;

            foreach (var argument in arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                switch (argument.ToLowerInvariant())
                {
                    case PlatformDesktop32:
                        if (platform != null)
                        {
                            return false;
                        }

                        platform = InteractiveHostPlatform.Desktop32;
                        break;

                    case PlatformDesktop64:
                        if (platform != null)
                        {
                            return false;
                        }

                        platform = InteractiveHostPlatform.Desktop64;
                        break;

                    case PlatformCore:
                        if (platform != null)
                        {
                            return false;
                        }

                        platform = InteractiveHostPlatform.Core;
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

        internal static string GetCommandLine(bool initialize, InteractiveHostPlatform? platform)
            => CommandName + (initialize ? "" : " " + NoConfigParameterName) + platform switch
            {
                null => "",
                InteractiveHostPlatform.Core => " " + PlatformCore,
                InteractiveHostPlatform.Desktop64 => " " + PlatformDesktop64,
                InteractiveHostPlatform.Desktop32 => " " + PlatformDesktop32,
                _ => throw ExceptionUtilities.Unreachable()
            };

        private void ReportInvalidArguments(IInteractiveWindow window)
        {
            var commands = (IInteractiveWindowCommands)window.Properties[typeof(IInteractiveWindowCommands)];
            commands.DisplayCommandUsage(this, window.ErrorOutputWriter, displayDetails: false);
        }
    }
}

