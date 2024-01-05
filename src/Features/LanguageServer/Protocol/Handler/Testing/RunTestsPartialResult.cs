// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

[DataContract]
internal record RunTestsPartialResult(
    [property: DataMember(Name = "stage")] string Stage,
    [property: DataMember(Name = "message")] string Message,
    [property: DataMember(Name = "progress"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)] TestProgress? Progress
);
