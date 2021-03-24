// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditorConfigSettings.DataProvider
{
    public partial class DataProviderTests
    {
        private class TestViewModel : ISettingsEditorViewModel
        {
            private readonly int _expectedNumberOfTimesCalled;
            private int _releaseCount;
            private readonly SemaphoreSlim _sync;

            public TestViewModel(int expectedNumberOfTimesCalled)
            {
                _sync = new SemaphoreSlim(expectedNumberOfTimesCalled);
                _expectedNumberOfTimesCalled = expectedNumberOfTimesCalled;
                _releaseCount = 0;
            }

            public async Task WaitForComplete()
            {
                do
                {
                    await _sync.WaitAsync().ConfigureAwait(false);
                } while (_releaseCount != _expectedNumberOfTimesCalled);
            }

            public Task<IReadOnlyList<TextChange>?> GetChangesAsync() => throw new NotImplementedException();

            public Task NotifyOfUpdateAsync()
            {
                Interlocked.Increment(ref _releaseCount);
                _sync.Release();
                return Task.CompletedTask;
            }
        }
    }
}
