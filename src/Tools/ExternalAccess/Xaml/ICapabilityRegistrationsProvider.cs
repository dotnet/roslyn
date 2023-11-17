// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal interface ICapabilityRegistrationsProvider
{
    ImmutableArray<Registration> GetRegistrations(ClientCapabilities clientCapabilities);
}
