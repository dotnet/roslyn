// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

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
                specializedContentType: CSharpVBInteractiveCommandsContentTypes.CSharpVBInteractiveCommandContentTypeName);
            _vsInteractiveWindowFactory = interactiveWindowFactory;
            _commandsFactory = commandsFactory;
        }

        protected abstract InteractiveEvaluator CreateInteractiveEvaluator(
            SVsServiceProvider serviceProvider,
            IViewClassifierAggregatorService classifierAggregator,
            IContentTypeRegistryService contentTypeRegistry,
            VisualStudioWorkspace workspace);

        protected abstract Guid LanguageServiceGuid { get; }
        protected abstract Guid Id { get; }
        protected abstract string Title { get; }
        protected abstract void LogSession(string key, string value);

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

        public IVsInteractiveWindow Create(int instanceId)
        {
            var evaluator = CreateInteractiveEvaluator(_vsServiceProvider, _classifierAggregator, _contentTypeRegistry, _vsWorkspace);

            var vsWindow = _vsInteractiveWindowFactory.Create(Id, instanceId, Title, evaluator, 0);
            vsWindow.SetLanguage(LanguageServiceGuid, evaluator.ContentType);

            var window = vsWindow.InteractiveWindow;

            // the tool window now owns the engine:
            window.TextView.Closed += new EventHandler((_, __) => 
            {
                LogSession(LogMessage.Window, LogMessage.Close);
                evaluator.Dispose();
            });
            // vsWindow.AutoSaveOptions = true;

            window.TextView.Options.SetOptionValue(InteractiveWindowOptions.SmartUpDown, true);

            // fire and forget:
            window.InitializeAsync();

            LogSession(LogMessage.Window, LogMessage.Create);

            return vsWindow;
        }

        public IVsInteractiveWindow Open(int instanceId, bool focus)
        {
            // TODO: we don't support multi-instance yet
            Debug.Assert(instanceId == 0);

            if (_vsInteractiveWindow == null)
            {
                _vsInteractiveWindow = Create(instanceId);
            }

            _vsInteractiveWindow.Show(focus);

            LogSession(LogMessage.Window, LogMessage.Open);

            return _vsInteractiveWindow;
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
            Dictionary<string, int> interactiveCommandMap = new Dictionary<string, int>();
            for (int i = 0; i < interactiveCommands.Length; i++)
            {
                foreach (var name in interactiveCommands[i].Names)
                {
                    interactiveCommandMap.Add(name, i);
                }
            }

            // swap core commands with specialized command if both exist
            // Command can have multiple names. We need to compare every name to find match.
            int value;
            foreach (var command in specializedInteractiveCommands)
            {
                foreach (var name in command.Names)
                {
                    if (interactiveCommandMap.TryGetValue(name, out value))
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
