// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;

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
            _commands = FilterCommands(commands, contentType: PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);
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

            // the tool window now owns the engine:
            vsWindow.InteractiveWindow.TextView.Closed += new EventHandler((_, __) => 
            {
                LogSession(LogMessage.Window, LogMessage.Close);
                evaluator.Dispose();
            });
            // vsWindow.AutoSaveOptions = true;

            var window = vsWindow.InteractiveWindow;

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

        private static ImmutableArray<IInteractiveWindowCommand> FilterCommands(IInteractiveWindowCommand[] commands, string contentType)
        {
            return commands.Where(
                c => c.GetType().GetCustomAttributes(typeof(ContentTypeAttribute), inherit: true).Any(
                    a => ((ContentTypeAttribute)a).ContentTypes == contentType)).ToImmutableArray();
        }
    }
}
