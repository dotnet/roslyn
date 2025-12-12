// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

[DataContract]
internal readonly struct ProjectInstanceId(string projectFilePath, string targetFramework)
{
    [DataMember(Name = "projectFilePath")]
    public string ProjectFilePath { get; } = projectFilePath;

    [DataMember(Name = "targetFramework")]
    public string TargetFramework { get; } = targetFramework;
}
