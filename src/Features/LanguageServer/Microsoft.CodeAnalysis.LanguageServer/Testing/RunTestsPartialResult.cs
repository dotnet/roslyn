// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[DataContract]
internal class RunTestsPartialResult
{
    /// <summary>
    /// The name of the stage that is running, e.g. Build, Discovery, etc.
    /// </summary>
    [DataMember(Name = "stage")]
    public string Stage { get; set; }

    /// <summary>
    /// A message that is output to the .NET test log.
    /// </summary>
    [DataMember(Name = "message")]
    public string Message { get; set; }

    /// <summary>
    /// Data on how many tests have passed,failed,skipped out of the total.
    /// </summary>
    [DataMember(Name = "progress")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public TestProgress? Progress { get; set; }
}