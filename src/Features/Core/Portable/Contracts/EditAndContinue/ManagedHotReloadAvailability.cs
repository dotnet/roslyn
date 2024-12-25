// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Managed hot reload availability information.
/// </summary>
[DataContract]
internal readonly struct ManagedHotReloadAvailability(
    ManagedHotReloadAvailabilityStatus status,
    string? localizedMessage = null)
{

    /// <summary>
    /// Status for the managed hot reload session.
    /// </summary>
    [DataMember(Name = "status")]
    public ManagedHotReloadAvailabilityStatus Status { get; } = status;

    /// <summary>
    /// [Optional] Localized message for <see cref="Status"/>.
    /// </summary>
    [DataMember(Name = "localizedMessage")]
    public string? LocalizedMessage { get; } = localizedMessage;
}
