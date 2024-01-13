// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;
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

        private MefHostServices? _hostServices;

        static UseExportProviderAttribute()
        {
            // Make sure we run the module initializer for Roslyn.Test.Utilities. C# projects do this via a
            // build-injected module initializer, but VB projects can ensure initialization occurs by applying the
            // UseExportProviderAttribute to test classes that rely on it.
            RuntimeHelpers.RunModuleConstructor(typeof(TestBase).Module.ModuleHandle);
        }

        public override void Before(MethodInfo? methodUnderTest)
        {
            // Need to clear cached MefHostServices between test runs.
            MefHostServices.TestAccessor.HookServiceCreation(CreateMefHostServices);

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
        public override void After(MethodInfo? methodUnderTest)
        {
            try
            {
                DisposeExportProvider(ExportProviderCache.LocalExportProviderForCleanup);
                DisposeExportProvider(ExportProviderCache.RemoteExportProviderForCleanup);
            }
            finally
            {
                // Replace hooks with ones that always throw exceptions. These hooks detect cases where code executing
                // after the end of a test attempts to create an ExportProvider.
                MefHostServices.TestAccessor.HookServiceCreation(DenyMefHostServicesCreationBetweenTests);

                // Reset static state variables.
                _hostServices = null;
                ExportProviderCache.SetEnabled_OnlyUseExportProviderAttributeCanCall(false);
            }
        }

        private static void DisposeExportProvider(ExportProvider? exportProvider)
        {
            if (exportProvider == null)
            {
                return;
            }

            // Dispose of the export provider, including calling Dispose for any IDisposable services created during the test.
            using var _ = exportProvider;

            if (exportProvider.GetExportedValues<IAsynchronousOperationListenerProvider>().SingleOrDefault() is { } listenerProvider)
            {
                // Verify the synchronization context was not used incorrectly
                var testExportJoinableTaskContext = exportProvider.GetExportedValues<TestExportJoinableTaskContext>().SingleOrDefault();
                var denyExecutionSynchronizationContext = testExportJoinableTaskContext?.SynchronizationContext as TestExportJoinableTaskContext.DenyExecutionSynchronizationContext;

                // Join remaining operations with a timeout
                using (var timeoutTokenSource = new CancellationTokenSource(CleanupTimeout))
                {
                    if (denyExecutionSynchronizationContext is object)
                    {
                        // Immediately cancel the test if the synchronization context is improperly used
                        denyExecutionSynchronizationContext.InvalidSwitch += delegate { timeoutTokenSource.CancelAfter(0); };
                        denyExecutionSynchronizationContext.ThrowIfSwitchOccurred();
                    }

                    try
                    {
                        // This attribute cleans up the in-process and out-of-process export providers separately, so we
                        // don't need to provide a workspace when waiting for operations to complete.
                        var waiter = ((AsynchronousOperationListenerProvider)listenerProvider).WaitAllDispatcherOperationAndTasksAsync(workspace: null);

                        if (testExportJoinableTaskContext?.DispatcherTaskJoiner is { } taskJoiner)
                        {
                            taskJoiner.JoinUsingDispatcher(waiter, timeoutTokenSource.Token);
                        }
                        else
                        {
                            waiter.GetAwaiter().GetResult();
                        }
                    }
                    catch (OperationCanceledException ex) when (timeoutTokenSource.IsCancellationRequested)
                    {
                        // If the failure was caused by an invalid thread change, throw that exception
                        denyExecutionSynchronizationContext?.ThrowIfSwitchOccurred();

                        var messageBuilder = new StringBuilder("Failed to clean up listeners in a timely manner.");
                        foreach (var token in ((AsynchronousOperationListenerProvider)listenerProvider).GetTokens())
                        {
                            messageBuilder.AppendLine().Append($"  {token}");
                        }

                        throw new TimeoutException(messageBuilder.ToString(), ex);
                    }
                }

                denyExecutionSynchronizationContext?.ThrowIfSwitchOccurred();

                foreach (var testErrorHandler in exportProvider.GetExportedValues<ITestErrorHandler>())
                {
                    var exceptions = testErrorHandler.Exceptions;
                    if (exceptions.Count > 0)
                    {
                        throw new AggregateException("Tests threw unexpected exceptions", exceptions);
                    }
                }
            }
        }

        private MefHostServices CreateMefHostServices(IEnumerable<Assembly> assemblies)
        {
            ExportProvider exportProvider;

            if (assemblies is ImmutableArray<Assembly> array &&
                array == MefHostServices.DefaultAssemblies &&
                ExportProviderCache.LocalExportProviderForCleanup != null)
            {
                if (_hostServices != null)
                {
                    return _hostServices;
                }

                exportProvider = ExportProviderCache.LocalExportProviderForCleanup;
            }
            else
            {
                exportProvider = ExportProviderCache.GetOrCreateExportProviderFactory(assemblies).CreateExportProvider();
            }

            Interlocked.CompareExchange(
                    ref _hostServices,
                    new ExportProviderMefHostServices(exportProvider),
                    null);

            return _hostServices;
        }

        private static MefHostServices DenyMefHostServicesCreationBetweenTests(IEnumerable<Assembly> assemblies)
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

        private class ExportProviderMefHostServices : MefHostServices, IMefHostExportProvider
        {
            private readonly VisualStudioMefHostServices _vsHostServices;

            public ExportProviderMefHostServices(ExportProvider exportProvider)
                : base(new ContainerConfiguration().CreateContainer())
            {
                _vsHostServices = VisualStudioMefHostServices.Create(exportProvider);
            }

            protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
                => _vsHostServices.CreateWorkspaceServices(workspace);

            IEnumerable<Lazy<TExtension, TMetadata>> IMefHostExportProvider.GetExports<TExtension, TMetadata>()
                => _vsHostServices.GetExports<TExtension, TMetadata>();

            IEnumerable<Lazy<TExtension>> IMefHostExportProvider.GetExports<TExtension>()
                => _vsHostServices.GetExports<TExtension>();
        }
    }
}
