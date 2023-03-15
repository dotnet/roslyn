// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal interface INavigableLocation
    {
        /// <summary>
        /// Navigates to a location opening or presenting it in a UI if necessary.  This work must happen quickly. Any
        /// expensive async work must be done by whatever component creates this value. This method is async only to
        /// allow final clients to call this from a non-UI thread while allowing the navigation to jump to the UI
        /// thread.
        /// </summary>
        Task<bool> NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken);
    }

    internal class NavigableLocation : INavigableLocation
    {
        private readonly Func<NavigationOptions, CancellationToken, Task<bool>> _callback;

        public NavigableLocation(Func<NavigationOptions, CancellationToken, Task<bool>> callback)
            => _callback = callback;

        public Task<bool> NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _callback(options, cancellationToken);
        }

        public static class TestAccessor
        {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
            public static Task<INavigableLocation?> Create(bool value)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
            {
                return Task.FromResult<INavigableLocation?>(
                    new NavigableLocation((_, _) => value ? SpecializedTasks.True : SpecializedTasks.False));
            }
        }
    }
}
