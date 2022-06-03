// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias InteractiveHost;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;
using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    internal abstract class VsInteractiveWindowProvider
    {
        private readonly IVsInteractiveWindowFactory _vsInteractiveWindowFactory;
        private readonly SVsServiceProvider _vsServiceProvider;
        private readonly VisualStudioWorkspace _vsWorkspace;
        private readonly IViewClassifierAggregatorService _classifierAggregator;
        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly IInteractiveWindowCommandsFactory _commandsFactory;
        private readonly ImmutableArray<IInteractiveWindowCommand> _commands;

        // TODO: support multi-instance windows
        // single instance of the Interactive Window
        private IVsInteractiveWindow _vsInteractiveWindow;

        public VsInteractiveWindowProvider(
           SVsServiceProvider serviceProvider,
           IVsInteractiveWindowFactory interactiveWindowFactory,
           IViewClassifierAggregatorService classifierAggregator,
           IContentTypeRegistryService contentTypeRegistry,
           IInteractiveWindowCommandsFactory commandsFactory,
           IInteractiveWindowCommand[] commands,
           VisualStudioWorkspace workspace)
        {
            _vsServiceProvider = serviceProvider;
            _classifierAggregator = classifierAggregator;
            _contentTypeRegistry = contentTypeRegistry;
            _vsWorkspace = workspace;
            _commands = GetApplicableCommands(commands, coreContentType: PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName,
                specializedContentType: InteractiveWindowContentTypes.CommandContentTypeName);
            _vsInteractiveWindowFactory = interactiveWindowFactory;
            _commandsFactory = commandsFactory;
        }

        protected abstract CSharpInteractiveEvaluator CreateInteractiveEvaluator(
            SVsServiceProvider serviceProvider,
            IViewClassifierAggregatorService classifierAggregator,
            IContentTypeRegistryService contentTypeRegistry,
            VisualStudioWorkspace workspace);

        protected abstract Guid LanguageServiceGuid { get; }
        protected abstract Guid Id { get; }
        protected abstract string Title { get; }
        protected abstract FunctionId InteractiveWindowFunctionId { get; }

        protected IInteractiveWindowCommandsFactory CommandsFactory
        {
            get
            {
                return _commandsFactory;
            }
        }

        protected ImmutableArray<IInteractiveWindowCommand> Commands
        {
            get
            {
                return _commands;
            }
        }

        public void Create(int instanceId)
        {
            var evaluator = CreateInteractiveEvaluator(_vsServiceProvider, _classifierAggregator, _contentTypeRegistry, _vsWorkspace);

            Debug.Assert(_vsInteractiveWindow == null);

            // ForceCreate means that the window should be created if the persisted layout indicates that it is visible.
            _vsInteractiveWindow = _vsInteractiveWindowFactory.Create(Id, instanceId, Title, evaluator, __VSCREATETOOLWIN.CTW_fForceCreate);
            _vsInteractiveWindow.SetLanguage(LanguageServiceGuid, evaluator.ContentType);

            if (_vsInteractiveWindow is ToolWindowPane interactiveWindowPane)
            {
                evaluator.OnBeforeReset += platform => interactiveWindowPane.Caption = Title + platform switch
                {
                    InteractiveHostPlatform.Desktop64 => " (.NET Framework " + ServicesVSResources.Bitness64 + ")",
                    InteractiveHostPlatform.Desktop32 => " (.NET Framework " + ServicesVSResources.Bitness32 + ")",
                    InteractiveHostPlatform.Core => " (.NET Core)",
                    _ => throw ExceptionUtilities.Unreachable
                };
            }

            var window = _vsInteractiveWindow.InteractiveWindow;
            window.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.SuggestionMarginId, true);

            void closeEventDelegate(object sender, EventArgs e)
            {
                window.TextView.Closed -= closeEventDelegate;
                LogCloseSession(evaluator.SubmissionCount);
                evaluator.Dispose();
            }

            // the tool window now owns the engine:
            window.TextView.Closed += closeEventDelegate;
            // vsWindow.AutoSaveOptions = true;

            // fire and forget:
            window.InitializeAsync();

            LogSession(LogMessage.Window, LogMessage.Create);
        }

        public IVsInteractiveWindow Open(int instanceId, bool focus)
        {
            // TODO: we don't support multi-instance yet
            Debug.Assert(instanceId == 0);

            if (_vsInteractiveWindow == null)
            {
                Create(instanceId);
            }

            _vsInteractiveWindow.Show(focus);

            return _vsInteractiveWindow;
        }

        protected void LogSession(string key, string value)
            => Logger.Log(InteractiveWindowFunctionId, KeyValueLogMessage.Create(m => m.Add(key, value)));

        private void LogCloseSession(int languageBufferCount)
        {
            Logger.Log(InteractiveWindowFunctionId,
                KeyValueLogMessage.Create(m =>
                {
                    m.Add(LogMessage.Window, LogMessage.Close);
                    m.Add(LogMessage.LanguageBufferCount, languageBufferCount);
                }));
        }

        private static ImmutableArray<IInteractiveWindowCommand> GetApplicableCommands(IInteractiveWindowCommand[] commands, string coreContentType, string specializedContentType)
        {
            // get all commands of coreContentType - generic interactive window commands
            var interactiveCommands = commands.Where(
                c => c.GetType().GetCustomAttributes(typeof(ContentTypeAttribute), inherit: true).Any(
                    a => ((ContentTypeAttribute)a).ContentTypes == coreContentType)).ToArray();

            // get all commands of specializedContentType - smart C#/VB command implementations
            var specializedInteractiveCommands = commands.Where(
                c => c.GetType().GetCustomAttributes(typeof(ContentTypeAttribute), inherit: true).Any(
                    a => ((ContentTypeAttribute)a).ContentTypes == specializedContentType)).ToArray();

            // We should choose specialized C#/VB commands over generic core interactive window commands
            // Build a map of names and associated core command first
            var interactiveCommandMap = new Dictionary<string, int>();
            for (var i = 0; i < interactiveCommands.Length; i++)
            {
                foreach (var name in interactiveCommands[i].Names)
                {
                    interactiveCommandMap.Add(name, i);
                }
            }

            // swap core commands with specialized command if both exist
            // Command can have multiple names. We need to compare every name to find match.
            foreach (var command in specializedInteractiveCommands)
            {
                foreach (var name in command.Names)
                {
                    if (interactiveCommandMap.TryGetValue(name, out var value))
                    {
                        interactiveCommands[value] = command;
                        break;
                    }
                }
            }

            return interactiveCommands.ToImmutableArray();
        }
    }
}
