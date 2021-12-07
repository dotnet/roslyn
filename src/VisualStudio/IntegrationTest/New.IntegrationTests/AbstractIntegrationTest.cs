// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;
using Xunit.Sdk;

namespace Roslyn.VisualStudio.IntegrationTests
{
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
    [IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "RoslynDev")]
    public abstract class AbstractIntegrationTest : IAsyncLifetime, IDisposable
    {
        protected const string ProjectName = "TestProj";
        protected const string SolutionName = "TestSolution";

        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        public static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(4);

        /// <summary>
        /// A timeout used to avoid hangs during test cleanup. This is separate from <see cref="HangMitigatingTimeout"/>
        /// to provide tests an opportunity to clean up state even if failure occurred due to timeout.
        /// </summary>
        private static readonly TimeSpan s_cleanupHangMitigatingTimeout = TimeSpan.FromMinutes(2);

        private JoinableTaskContext? _joinableTaskContext;
        private JoinableTaskCollection? _joinableTaskCollection;
        private JoinableTaskFactory? _joinableTaskFactory;

        private TestServices? _testServices;

        private readonly CancellationTokenSource _hangMitigatingCancellationTokenSource;
        private readonly CancellationTokenSource _cleanupCancellationTokenSource;

        protected AbstractIntegrationTest()
        {
            Assert.True(Application.Current.Dispatcher.CheckAccess());

            JoinableTaskContext = ThreadHelper.JoinableTaskContext;

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(HangMitigatingTimeout);
            _cleanupCancellationTokenSource = new CancellationTokenSource();
        }

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

        protected JoinableTaskFactory JoinableTaskFactory
            => _joinableTaskFactory ?? throw new InvalidOperationException();

        protected CancellationToken HangMitigatingCancellationToken
            => _hangMitigatingCancellationTokenSource.Token;

        /// <summary>
        /// ⚠️ Note that this token will not be cancelled prior to the call to <see cref="DisposeAsync"/> (which starts
        /// the cancellation timer). Derived types are not likely to make use of this, so it's marked
        /// <see langword="private"/>.
        /// </summary>
        private CancellationToken CleanupCancellationToken
            => _cleanupCancellationTokenSource.Token;

        public virtual async Task InitializeAsync()
        {
            TestServices = await CreateTestServicesAsync();

            await TestServices.StateReset.ResetGlobalOptionsAsync(HangMitigatingCancellationToken);
            await TestServices.StateReset.ResetHostSettingsAsync(HangMitigatingCancellationToken);
        }

        /// <summary>
        /// This method implements <see cref="IAsyncLifetime.DisposeAsync"/>, and is used for releasing resources
        /// created by <see cref="IAsyncLifetime.InitializeAsync"/>. This method is only called if
        /// <see cref="InitializeAsync"/> completes successfully.
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            _cleanupCancellationTokenSource.CancelAfter(s_cleanupHangMitigatingTimeout);

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
        }

        private protected virtual async Task<TestServices> CreateTestServicesAsync()
            => await TestServices.CreateAsync(JoinableTaskFactory);
    }
}
