// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[DataContract]
internal readonly struct RunningProjectOptions
{
    /// <summary>
    /// Required restart of the project when an edit that has no effect until the app is restarted is made to any dependent project.
    /// </summary>
    [DataMember]
    public required bool RestartWhenChangesHaveNoEffect { get; init; }
}
