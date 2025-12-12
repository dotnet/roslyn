// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

[DataContract]
internal readonly struct RunningProjectInfo(ProjectInstanceId projectInstanceId, bool restartAutomatically)
{
    [DataMember(Name = "projectInstanceId")]
    public ProjectInstanceId ProjectInstanceId { get; } = projectInstanceId;

    [DataMember(Name = "restartAutomatically")]
    public bool RestartAutomatically { get; } = restartAutomatically;
}
