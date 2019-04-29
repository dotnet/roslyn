// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Test.Utilities;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// This attribute supports tests that need to use a MEF container (<see cref="ExportProvider"/>) directly or
    /// indirectly during the test sequence. It ensures production code uniformly handles the export provider created
    /// during a test, and cleans up the state before the test completes.
    /// </summary>
    /// <remarks>
    /// <para>This attribute serves several important functions for tests that use state variables which are otherwise
    /// shared at runtime:</para>
    /// <list type="bullet">
    /// <item>Ensures <see cref="HostServices"/> implementations all use the same <see cref="ExportProvider"/>, which is
    /// the one created by the test.</item>
    /// <item>Clears static cached values in production code holding instances of <see cref="HostServices"/>, or any
    /// object obtained from it or one of its related interfaces such as <see cref="HostLanguageServices"/>.</item>
    /// <item>Isolates tests by waiting for asynchronous operations to complete before a test is considered
    /// complete.</item>
    /// <item>When required, provides a separate <see cref="ExportProvider"/> for the <see cref="RemoteWorkspace"/>
    /// executing in the test process. If this provider is created during testing, it is cleaned up with the primary
    /// export provider during test teardown.</item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UseExportProviderAttribute : BeforeAfterTestAttribute
    {
        /// <summary>
        /// Asynchronous operations are expected to be cancelled at the end of the test that started them. Operations
        /// cancelled by the test are cleaned up immediately. The remaining operations are given an opportunity to run
        /// to completion. If this timeout is exceeded by the asynchronous operations running after a test completes,
        /// the test is failed.
        /// </summary>
        private static readonly TimeSpan CleanupTimeout = TimeSpan.FromMinutes(1);

        // Cache the export provider factory for RoslynServices.RemoteHostAssemblies
        private static readonly Lazy<IExportProviderFactory> s_remoteHostExportProviderFactory = new Lazy<IExportProviderFactory>(
            CreateRemoteHostExportProviderFactory,
            LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly Lazy<MefHostServices> _remoteHostServices = new Lazy<MefHostServices>(
            CreateRemoteHostServices,
            LazyThreadSafetyMode.ExecutionAndPublication);

        private MefHostServices _hostServices;

        public override void Before(MethodInfo methodUnderTest)
        {
            MefHostServices.TestAccessor.HookServiceCreation(CreateMefHostServices);
            RoslynServices.TestAccessor.HookHostServices(() => _remoteHostServices.Value);

            // make sure we enable this for all unit tests
            AsynchronousOperationListenerProvider.Enable(enable: true, diagnostics: true);
            ExportProviderCache.SetEnabled_OnlyUseExportProviderAttributeCanCall(true);
        }

        /// <summary>
        /// To the extent reasonably possible, this method resets the state of the test environment to the same state as
        /// it started, ensuring that tests running in sequence cannot influence the outcome of later tests.
        /// </summary>
        /// <remarks>
        /// <para>The test cleanup runs in two primary steps:</para>
        /// <list type="number">
        /// <item>Waiting for asynchronous operations started by the test to complete.</item>
        /// <item>Disposing of mutable resources created by the test.</item>
        /// <item>Clearing static state variables related to the use of MEF during a test.</item>
        /// </list>
        /// </remarks>
        public override void After(MethodInfo methodUnderTest)
        {
            var exportProvider = ExportProviderCache.ExportProviderForCleanup;
            try
            {
                var listenerProvider = exportProvider?.GetExportedValues<IAsynchronousOperationListenerProvider>().SingleOrDefault();
                if (listenerProvider != null)
                {
                    if (exportProvider.GetExportedValues<IThreadingContext>().SingleOrDefault()?.HasMainThread ?? false)
                    {
                        // Immediately clear items from the foreground notification service for which cancellation is
                        // requested. This service maintains a queue separately from Tasks, and work items scheduled for
                        // execution after a delay are not immediately purged when cancellation is requested. This code
                        // instructs the service to walk the list of queued work items and immediately cancel and purge any
                        // which are already cancelled.
                        var foregroundNotificationService = exportProvider?.GetExportedValues<IForegroundNotificationService>().SingleOrDefault() as ForegroundNotificationService;
                        foregroundNotificationService?.ReleaseCancelledItems();
                    }

                    // Join remaining operations with a timeout
                    using (var timeoutTokenSource = new CancellationTokenSource(CleanupTimeout))
                    {
                        try
                        {
                            var waiter = ((AsynchronousOperationListenerProvider)listenerProvider).WaitAllDispatcherOperationAndTasksAsync();
                            waiter.JoinUsingDispatcher(timeoutTokenSource.Token);
                        }
                        catch (OperationCanceledException ex) when (timeoutTokenSource.IsCancellationRequested)
                        {
                            var messageBuilder = new StringBuilder("Failed to clean up listeners in a timely manner.");
                            foreach (var token in ((AsynchronousOperationListenerProvider)listenerProvider).GetTokens())
                            {
                                messageBuilder.AppendLine().Append($"  {token}");
                            }

                            throw new TimeoutException(messageBuilder.ToString(), ex);
                        }
                    }

                    // Verify the synchronization context was not used incorrectly
                    var testExportJoinableTaskContext = exportProvider.GetExportedValues<TestExportJoinableTaskContext>().SingleOrDefault();
                    if (testExportJoinableTaskContext?.SynchronizationContext is TestExportJoinableTaskContext.DenyExecutionSynchronizationContext synchronizationContext)
                    {
                        synchronizationContext.ThrowIfSwitchOccurred();
                    }
                }
            }
            finally
            {
                // Dispose of the export provider, including calling Dispose for any IDisposable services created during
                // the test.
                exportProvider?.Dispose();

                // Replace hooks with ones that always throw exceptions. These hooks detect cases where code executing
                // after the end of a test attempts to create an ExportProvider.
                MefHostServices.TestAccessor.HookServiceCreation(DenyMefHostServicesCreationBetweenTests);
                RoslynServices.TestAccessor.HookHostServices(() => throw new InvalidOperationException("Cannot create host services after test tear down."));

                // Reset static state variables.
                _hostServices = null;
                ExportProviderCache.SetEnabled_OnlyUseExportProviderAttributeCanCall(false);
            }
        }

        private MefHostServices CreateMefHostServices(IEnumerable<Assembly> assemblies, bool requestingDefaultAssemblies)
        {
            if (requestingDefaultAssemblies && ExportProviderCache.ExportProviderForCleanup != null)
            {
                if (_hostServices == null)
                {
                    var hostServices = new ExportProviderMefHostServices(ExportProviderCache.ExportProviderForCleanup);
                    Interlocked.CompareExchange(ref _hostServices, hostServices, null);
                }

                return _hostServices;
            }

            var catalog = ExportProviderCache.GetOrCreateAssemblyCatalog(assemblies);
            Interlocked.CompareExchange(
                ref _hostServices,
                new ExportProviderMefHostServices(ExportProviderCache.GetOrCreateExportProviderFactory(catalog).CreateExportProvider()),
                null);

            return _hostServices;
        }

        private static MefHostServices DenyMefHostServicesCreationBetweenTests(IEnumerable<Assembly> assemblies, bool requestingDefaultAssemblies)
        {
            // If you hit this, one of three situations occurred:
            //
            // 1. A test method that uses ExportProvider is not marked with UseExportProviderAttribute (can also be
            //    applied to the containing type or a base type.
            // 2. A test attempted to create an ExportProvider during the test cleanup operations after the
            //    ExportProvider was already disposed.
            // 3. A test attempted to use an ExportProvider in the constructor of the test, or during the initialization
            //    of a field in the test class.
            throw new InvalidOperationException("Cannot create host services after test tear down.");
        }

        private static IExportProviderFactory CreateRemoteHostExportProviderFactory()
        {
            var configuration = CompositionConfiguration.Create(ExportProviderCache.GetOrCreateAssemblyCatalog(RoslynServices.RemoteHostAssemblies).WithCompositionService());
            var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
            return runtimeComposition.CreateExportProviderFactory();
        }

        private static MefHostServices CreateRemoteHostServices()
        {
            return new ExportProviderMefHostServices(s_remoteHostExportProviderFactory.Value.CreateExportProvider());
        }

        private class ExportProviderMefHostServices : MefHostServices, IMefHostExportProvider
        {
            private readonly VisualStudioMefHostServices _vsHostServices;

            public ExportProviderMefHostServices(ExportProvider exportProvider)
                : base(new ContainerConfiguration().CreateContainer())
            {
                _vsHostServices = VisualStudioMefHostServices.Create(exportProvider);
            }

            protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
            {
                return _vsHostServices.CreateWorkspaceServices(workspace);
            }

            IEnumerable<Lazy<TExtension, TMetadata>> IMefHostExportProvider.GetExports<TExtension, TMetadata>()
            {
                return _vsHostServices.GetExports<TExtension, TMetadata>();
            }

            IEnumerable<Lazy<TExtension>> IMefHostExportProvider.GetExports<TExtension>()
            {
                return _vsHostServices.GetExports<TExtension>();
            }
        }
    }
}
