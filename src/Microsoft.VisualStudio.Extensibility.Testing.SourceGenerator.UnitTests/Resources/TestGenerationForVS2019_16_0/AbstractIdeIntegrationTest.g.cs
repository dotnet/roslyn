// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using global::Xunit;
    using global::Xunit.Sdk;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Provides a base class for Visual Studio integration tests.
    /// </summary>
    /// <remarks>
    /// The following is the xunit execution order:
    ///
    /// <list type="number">
    /// <item><description>Instance constructor</description></item>
    /// <item><description><see cref="IAsyncLifetime.InitializeAsync"/></description></item>
    /// <item><description><see cref="BeforeAfterTestAttribute.Before"/></description></item>
    /// <item><description>Test method</description></item>
    /// <item><description><see cref="BeforeAfterTestAttribute.After"/></description></item>
    /// <item><description><see cref="IAsyncLifetime.DisposeAsync"/></description></item>
    /// <item><description><see cref="IDisposable.Dispose"/></description></item>
    /// </list>
    /// </remarks>
    public abstract class AbstractIdeIntegrationTest : IAsyncLifetime, IDisposable
    {
        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        public static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(4);

        /// <summary>
        /// A timeout used to avoid hangs during test cleanup. This is separate from <see cref="HangMitigatingTimeout"/>
        /// to provide tests an opportunity to clean up state even if failure occurred due to timeout.
        /// </summary>
        private static readonly TimeSpan CleanupHangMitigatingTimeout = TimeSpan.FromMinutes(2);

        private readonly CancellationTokenSource _hangMitigatingCancellationTokenSource;
        private readonly CancellationTokenSource _cleanupCancellationTokenSource;

        private JoinableTaskContext? _joinableTaskContext;
        private JoinableTaskCollection? _joinableTaskCollection;
        private JoinableTaskFactory? _joinableTaskFactory;

        private TestServices? _testServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractIdeIntegrationTest"/> class.
        /// </summary>
        protected AbstractIdeIntegrationTest()
        {
            Assert.True(Application.Current.Dispatcher.CheckAccess());

            JoinableTaskContext = ThreadHelper.JoinableTaskContext;

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(HangMitigatingTimeout);
            _cleanupCancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the <see cref="Threading.JoinableTaskContext"/> context for use in integration tests.
        /// </summary>
        [NotNull]
        protected JoinableTaskContext? JoinableTaskContext
        {
            get
            {
                return _joinableTaskContext ?? throw new InvalidOperationException();
            }

            private set
            {
                if (value == _joinableTaskContext)
                {
                    return;
                }

                if (value is null)
                {
                    _joinableTaskContext = null;
                    _joinableTaskCollection = null;
                    _joinableTaskFactory = null;
                }
                else
                {
                    _joinableTaskContext = value;
                    _joinableTaskCollection = value.CreateCollection();
                    _joinableTaskFactory = value.CreateFactory(_joinableTaskCollection).WithPriority(Application.Current.Dispatcher, DispatcherPriority.Background);
                }
            }
        }

        [NotNull]
        private protected TestServices? TestServices
        {
            get
            {
                return _testServices ?? throw new InvalidOperationException();
            }

            private set
            {
                _testServices = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="Threading.JoinableTaskFactory"/> for use in integration tests.
        /// </summary>
        protected JoinableTaskFactory JoinableTaskFactory
            => _joinableTaskFactory ?? throw new InvalidOperationException();

        /// <summary>
        /// Gets a cancellation token for use in integration tests to avoid CI timeouts.
        /// </summary>
        protected CancellationToken HangMitigatingCancellationToken
            => _hangMitigatingCancellationTokenSource.Token;

        /// <remarks>
        /// ⚠️ Note that this token will not be cancelled prior to the call to <see cref="DisposeAsync"/> (which starts
        /// the cancellation timer). Derived types are not likely to make use of this, so it's marked
        /// <see langword="private"/>.
        /// </remarks>
        private CancellationToken CleanupCancellationToken
            => _cleanupCancellationTokenSource.Token;

        /// <inheritdoc/>
        public virtual async Task InitializeAsync()
        {
            TestServices = await CreateTestServicesAsync();
        }

        /// <summary>
        /// This method implements <see cref="IAsyncLifetime.DisposeAsync"/>, and is used for releasing resources
        /// created by <see cref="IAsyncLifetime.InitializeAsync"/>. This method is only called if
        /// <see cref="InitializeAsync"/> completes successfully.
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            _cleanupCancellationTokenSource.CancelAfter(CleanupHangMitigatingTimeout);

            await TestServices.SolutionExplorer.CloseSolutionAsync(CleanupCancellationToken);

            if (_joinableTaskCollection is object)
            {
                await _joinableTaskCollection.JoinTillEmptyAsync(CleanupCancellationToken);
            }

            JoinableTaskContext = null;
        }

        /// <summary>
        /// This method provides the implementation for <see cref="IDisposable.Dispose"/>.
        /// This method is called via the <see cref="IDisposable"/> interface if the constructor completes successfully.
        /// The <see cref="InitializeAsync"/> may or may not have completed successfully.
        /// </summary>
        public virtual void Dispose()
        {
            _hangMitigatingCancellationTokenSource.Dispose();
            _cleanupCancellationTokenSource.Dispose();
        }

        private protected virtual async Task<TestServices> CreateTestServicesAsync()
            => await TestServices.CreateAsync(JoinableTaskFactory);
    }
}
