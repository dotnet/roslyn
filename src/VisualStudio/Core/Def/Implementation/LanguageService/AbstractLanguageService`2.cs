// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : AbstractLanguageService
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        internal TPackage Package { get; }

        // DevDiv 753309:
        // We've redefined some VS interfaces that had incorrect PIAs. When 
        // we interop with native parts of VS, they always QI, so everything
        // works. However, Razor is now managed, but assumes that the C#
        // language service is native. When setting breakpoints, they
        // get the language service from its GUID and cast it to IVsLanguageDebugInfo.
        // This would QI the native lang service. Since we're managed and
        // we've redefined IVsLanguageDebugInfo, the cast
        // fails. To work around this, we put the LS inside a ComAggregate object,
        // which always force a QueryInterface and allow their cast to succeed.
        // 
        // This also fixes 752331, which is a similar problem with the 
        // exception assistant.
        internal object ComAggregate { get; private set; }

        // Note: The lifetime for state in this class is carefully managed.  For every bit of state
        // we set up, there is a corresponding tear down phase which deconstructs the state in the
        // reverse order it was created in.
        internal VisualStudioWorkspaceImpl Workspace { get; private set; }
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; private set; }
        internal HostDiagnosticUpdateSource HostDiagnosticUpdateSource { get; private set; }
        internal AnalyzerFileWatcherService AnalyzerFileWatcherService { get; private set; }

        /// <summary>
        /// Whether or not we have been torn down.  This is currently only used to make sure we are
        /// not torn down twice.
        /// </summary>
        private bool _isTornDown;

        protected AbstractLanguageService(
            TPackage package)
        {
            this.Package = package;
        }

        public override IServiceProvider SystemServiceProvider
        {
            get
            {
                return this.Package;
            }
        }

        /// <summary>
        /// Setup and TearDown go in reverse order.
        /// </summary>
        internal void Setup()
        {
            this.ComAggregate = CreateComAggregate();

            // First, acquire any services we need throughout our lifetime.
            this.GetServices();

            var componentModel = this.Package.ComponentModel;

            // Start off a background task to prime some components we'll need for editing
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.BackgroundThread,
                () => PrimeLanguageServiceComponentsOnBackground(componentModel));

            // Next, make any connections to these services.
            this.ConnectToServices();

            // Finally, once our connections are established, set up any initial state that we need.
            // Note: we may be instantiated at any time (including when the IDE is already
            // debugging).  We must not assume anything about our initial state and must instead
            // query for all the information we need at this point.
            this.Initialize();
        }

        private object CreateComAggregate()
        {
            return Interop.ComAggregate.CreateAggregatedObject(this);
        }

        internal void TearDown()
        {
            if (_isTornDown)
            {
                throw new InvalidOperationException();
            }

            _isTornDown = true;

            GC.SuppressFinalize(this);
            this.Uninitialize();
            this.DisconnectFromServices();
            this.RemoveServices();
        }

        ~AbstractLanguageService()
        {
            if (!Environment.HasShutdownStarted)
            {
                throw new InvalidOperationException("TearDown not called!");
            }
        }

        protected virtual void GetServices()
        {
            // This method should only contain calls to acquire services off of the component model
            // or service providers.  Anything else which is more complicated should go in Initialize
            // instead.
            this.Workspace = this.Package.Workspace;
            this.EditorAdaptersFactoryService = this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
            this.HostDiagnosticUpdateSource = this.Package.ComponentModel.GetService<HostDiagnosticUpdateSource>();
            this.AnalyzerFileWatcherService = this.Package.ComponentModel.GetService<AnalyzerFileWatcherService>();
        }

        protected virtual void RemoveServices()
        {
            this.EditorAdaptersFactoryService = null;
            this.Workspace = null;
        }

        /// <summary>
        /// Keep ConnectToServices and DisconnectFromServices in 1:1 correspondence.
        /// DisconnectFromServices should clean up resources in the reverse direction that they are
        /// initialized in.
        /// </summary>
        protected virtual void ConnectToServices()
        {
        }

        protected virtual void DisconnectFromServices()
        {
        }

        /// <summary>
        /// Called right after we instantiate the language service.  Used to set up any internal
        /// state we need.
        /// 
        /// Try to keep this method fairly clean.  Any complicated logic should go in methods called
        /// from this one.  Initialize and Uninitialize go in reverse order 
        /// </summary>
        protected virtual void Initialize()
        {
        }

        protected virtual void Uninitialize()
        {
        }

        private void PrimeLanguageServiceComponentsOnBackground(IComponentModel componentModel)
        {
            var commandHandlerServiceFactory = componentModel.GetService<ICommandHandlerServiceFactory>();
            commandHandlerServiceFactory.Initialize(this.ContentTypeName);

            var formatter = this.Workspace.Services.GetLanguageServices(RoslynLanguageName).GetService<ISyntaxFormattingService>();
            if (formatter != null)
            {
                formatter.GetDefaultFormattingRules();
            }
        }

        protected abstract void SetupNewTextView(IVsTextView textView);

        protected abstract string ContentTypeName { get; }
        protected abstract string LanguageName { get; }
        protected abstract string RoslynLanguageName { get; }
    }
}
