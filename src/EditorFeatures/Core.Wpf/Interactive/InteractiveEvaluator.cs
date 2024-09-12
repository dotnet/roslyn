// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
    using Microsoft.VisualStudio.Text.Editor;

    // TODO: Rename to InteractiveEvaluator https://github.com/dotnet/roslyn/issues/6441
    // The code is not specific to C#, but Interactive Window has hardcoded "CSharpInteractiveEvaluator" name.
    internal sealed class CSharpInteractiveEvaluator : IResettableInteractiveEvaluator
    {
        private const string CommandPrefix = "#";

        private readonly InteractiveEvaluatorLanguageInfoProvider _languageInfo;

        private readonly IThreadingContext _threadingContext;
        private readonly IViewClassifierAggregatorService _classifierAggregator;
        private readonly IInteractiveWindowCommandsFactory _commandsFactory;
        private readonly ImmutableArray<IInteractiveWindowCommand> _commands;
        private readonly InteractiveWindowWorkspace _workspace;
        private readonly InteractiveSession _session;

        private IInteractiveWindow? _lazyInteractiveWindow;
        private IInteractiveWindowCommands? _lazyInteractiveCommands;

        #region UI Thread only

        /// <remarks>
        /// Submission buffers in the order they were submitted. 
        /// Includes both command buffers as well as language buffers.
        /// Does not include the current buffer unless it has been submitted.
        /// </remarks>
        private readonly List<ITextBuffer> _submittedBuffers = [];

        #endregion

        public IContentType ContentType { get; }

        public InteractiveEvaluatorResetOptions ResetOptions { get; set; }
            = new InteractiveEvaluatorResetOptions(InteractiveHostPlatform.Core);

        internal CSharpInteractiveEvaluator(
            IThreadingContext threadingContext,
            IAsynchronousOperationListener listener,
            IContentType contentType,
            HostServices hostServices,
            IViewClassifierAggregatorService classifierAggregator,
            IInteractiveWindowCommandsFactory commandsFactory,
            ImmutableArray<IInteractiveWindowCommand> commands,
            ITextDocumentFactoryService textDocumentFactoryService,
            EditorOptionsService editorOptionsService,
            InteractiveEvaluatorLanguageInfoProvider languageInfo,
            string initialWorkingDirectory)
        {
            Debug.Assert(languageInfo.InteractiveResponseFileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) == -1);

            _threadingContext = threadingContext;
            ContentType = contentType;
            _languageInfo = languageInfo;
            _classifierAggregator = classifierAggregator;
            _commandsFactory = commandsFactory;
            _commands = commands;

            _workspace = new InteractiveWindowWorkspace(hostServices);

            _session = new InteractiveSession(
                _workspace,
                threadingContext,
                listener,
                textDocumentFactoryService,
                editorOptionsService,
                languageInfo,
                initialWorkingDirectory);

            _session.Host.ProcessInitialized += ProcessInitialized;
        }

        public void Dispose()
        {
            _session.Host.ProcessInitialized -= ProcessInitialized;

            _session.Dispose();
            _workspace.Dispose();

            if (_lazyInteractiveWindow != null)
            {
                _lazyInteractiveWindow.SubmissionBufferAdded -= SubmissionBufferAdded;
            }
        }

        private void ProcessInitialized(InteractiveHostPlatformInfo platformInfo, InteractiveHostOptions options, RemoteExecutionResult result)
        {
            // Capture and clear exising submission buffers. Independent of other operations that occur on restart.
            _ = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                CaptureClassificationSpans();
            });
        }

        public IInteractiveWindow? CurrentWindow
        {
            get => _lazyInteractiveWindow;

            set
            {
                _threadingContext.ThrowIfNotOnUIThread();

                if (_lazyInteractiveWindow != null)
                {
                    throw new NotSupportedException(EditorFeaturesWpfResources.The_CurrentWindow_property_may_only_be_assigned_once);
                }

                _lazyInteractiveWindow = value ?? throw new ArgumentNullException(nameof(value));
                _workspace.Window = value;

                Task.Run(() => _session.Host.SetOutputs(value.OutputWriter, value.ErrorOutputWriter));

                value.SubmissionBufferAdded += SubmissionBufferAdded;
                _lazyInteractiveCommands = _commandsFactory.CreateInteractiveCommands(value, CommandPrefix, _commands);
            }
        }

        /// <summary>
        /// Invoked before the process is reset. The argument is the value of <see cref="InteractiveHostOptions.Platform"/>.
        /// </summary>
        public event Action<InteractiveHostPlatform>? OnBeforeReset;

        public int SubmissionCount
            => _session.SubmissionCount;

        private IInteractiveWindow GetInteractiveWindow()
            => _lazyInteractiveWindow ?? throw new InvalidOperationException(EditorFeaturesResources.Engine_must_be_attached_to_an_Interactive_Window);

        private IInteractiveWindowCommands GetInteractiveCommands()
            => _lazyInteractiveCommands ?? throw new InvalidOperationException(EditorFeaturesResources.Engine_must_be_attached_to_an_Interactive_Window);

        /// <summary>
        /// Invoked on UI thread when a new language buffer is created and before it is added to the projection.
        /// </summary>
        private void SubmissionBufferAdded(object sender, SubmissionBufferAddedEventArgs args)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            _session.AddSubmissionProject(args.NewBuffer);
        }

        private void CaptureClassificationSpans()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var textView = GetInteractiveWindow().TextView;

            // Freeze all existing classifications and then clear the list of submission buffers we have.
            foreach (var textBuffer in _submittedBuffers)
            {
                InertClassifierProvider.CaptureExistingClassificationSpans(_classifierAggregator, textView, textBuffer);
            }

            _submittedBuffers.Clear();
        }

        public bool CanExecuteCode(string text)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            return _lazyInteractiveCommands?.InCommand == true || _languageInfo.IsCompleteSubmission(text);
        }

        /// <summary>
        /// Invoked when the Interactive Window is created.
        /// </summary>
        async Task<ExecutionResult> IInteractiveEvaluator.InitializeAsync()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var window = GetInteractiveWindow();

            var resetOptions = ResetOptions;
            _session.Host.SetOutputs(window.OutputWriter, window.ErrorOutputWriter);
            var isSuccessful = await _session.ResetAsync(_session.GetHostOptions(initialize: true, resetOptions.Platform)).ConfigureAwait(false);
            return new ExecutionResult(isSuccessful);
        }

        /// <summary>
        /// Invoked by the reset toolbar button.
        /// </summary>
        async Task<ExecutionResult> IInteractiveEvaluator.ResetAsync(bool initialize)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var window = GetInteractiveWindow();

            var resetOptions = ResetOptions;
            Debug.Assert(GetInteractiveCommands().CommandPrefix == CommandPrefix);
            window.AddInput(CommandPrefix + InteractiveWindowResetCommand.GetCommandLine(initialize, resetOptions.Platform));
            window.WriteLine(EditorFeaturesWpfResources.Resetting_execution_engine);
            window.FlushOutput();

            var options = _session.GetHostOptions(initialize, resetOptions.Platform);
            OnBeforeReset?.Invoke(options.Platform);
            var isSuccessful = await _session.ResetAsync(options).ConfigureAwait(false);
            return new ExecutionResult(isSuccessful);
        }

        /// <summary>
        /// Called on UI thread by the Interactive Window once a code snippet is submitted.
        /// Followed on UI thread by creation of a new language buffer and call to <see cref="SubmissionBufferAdded"/>.
        /// </summary>
        public async Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            try
            {
                _threadingContext.ThrowIfNotOnUIThread();

                var window = GetInteractiveWindow();
                var commands = GetInteractiveCommands();

                var currentSubmissionBuffer = window.CurrentLanguageBuffer;
                Contract.ThrowIfNull(currentSubmissionBuffer);
                _submittedBuffers.Add(currentSubmissionBuffer);

                if (commands.InCommand)
                {
                    // Takes the content of the current language buffer, parses it as a command
                    // and returns a task that execute the command, or null if the text doesn't parse.
                    var commandTask = commands.TryExecuteCommand();
                    if (commandTask != null)
                    {
                        return await commandTask.ConfigureAwait(false);
                    }
                }

                // If process initialization is in progress we will wait with code 
                // execution after the initialization is completed.
                var isSuccessful = await _session.ExecuteCodeAsync(text).ConfigureAwait(false);
                return new ExecutionResult(isSuccessful);
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public void AbortExecution()
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4725)
        }

        public string? FormatClipboard()
        {
            // keep the clipboard content as is
            return null;
        }

        public string GetPrompt()
        {
            var buffer = GetInteractiveWindow().CurrentLanguageBuffer;
            return buffer != null && buffer.CurrentSnapshot.LineCount > 1
                ? ". "
                : "> ";
        }

        public Task SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string workingDirectory)
            => _session.SetPathsAsync(referenceSearchPaths, sourceSearchPaths, workingDirectory);
    }
}
