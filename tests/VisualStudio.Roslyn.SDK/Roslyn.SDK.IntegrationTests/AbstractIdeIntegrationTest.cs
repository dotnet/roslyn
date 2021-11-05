// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Testing.InProcess;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Testing
{
    [IdeSettings(MinVersion = VisualStudioVersion.VS2022)]
    public abstract class AbstractIdeIntegrationTest : IAsyncLifetime, IDisposable
    {
        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        public static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(4);

        private JoinableTaskContext? _joinableTaskContext;
        private JoinableTaskCollection? _joinableTaskCollection;
        private JoinableTaskFactory? _joinableTaskFactory;

        private TestServices? _testServices;

        private CancellationTokenSource _hangMitigatingCancellationTokenSource;

        protected AbstractIdeIntegrationTest()
        {
            Assert.True(Application.Current.Dispatcher.CheckAccess());

            JoinableTaskContext = ThreadHelper.JoinableTaskContext;

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(HangMitigatingTimeout);
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
        protected TestServices? TestServices
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

        public virtual async Task InitializeAsync()
        {
            TestServices = await CreateTestServicesAsync();
        }

        public virtual async Task DisposeAsync()
        {
            if (_joinableTaskCollection is object)
            {
                await _joinableTaskCollection.JoinTillEmptyAsync();
            }

            JoinableTaskContext = null;
        }

        public virtual void Dispose()
        {
        }

        protected virtual async Task<TestServices> CreateTestServicesAsync()
            => await TestServices.CreateAsync(JoinableTaskFactory);
    }
}
