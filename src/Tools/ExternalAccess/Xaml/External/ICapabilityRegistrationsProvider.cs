// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// Represents a provider to be exported via MEF for dynamically registering capabilities.
/// </summary>
internal interface ICapabilityRegistrationsProvider
{
    /// <summary>
    /// Gets the registrations for the given client capabilities.
    /// </summary>
    ImmutableArray<Registration> GetRegistrations(ClientCapabilities clientCapabilities);
}
