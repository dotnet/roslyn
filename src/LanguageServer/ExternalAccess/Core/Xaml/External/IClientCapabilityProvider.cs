// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// Represents a provider for checking the support for dynamically registering capabilities.
/// </summary>
internal interface IClientCapabilityProvider
{
    /// <summary>
    /// Checks whether the client supports dynamically registering the capability for the given method name.
    /// </summary>
    bool IsDynamicRegistrationSupported(string methodName);

    bool SupportsMarkdownDocumentation { get; }

    bool SupportsCompletionListData { get; }
}
