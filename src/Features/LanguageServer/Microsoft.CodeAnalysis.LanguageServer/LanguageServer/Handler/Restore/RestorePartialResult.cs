// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[DataContract]
internal sealed record RestorePartialResult(
    [property: DataMember(Name = "stage")] string Stage,
    [property: DataMember(Name = "message")] string Message
);
