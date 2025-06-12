// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.TestHooks;

/// <summary>
/// Return <see cref="IAsynchronousOperationListener"/> for the given featureName
/// 
/// We have this abstraction so that we can have isolated listener/waiter in unit tests
/// </summary>
internal interface IAsynchronousOperationListenerProvider
{
    /// <summary>
    /// Get <see cref="IAsynchronousOperationListener"/> for given feature.
    /// same provider will return a singleton listener for same feature
    /// </summary>
    IAsynchronousOperationListener GetListener(string featureName);
}
