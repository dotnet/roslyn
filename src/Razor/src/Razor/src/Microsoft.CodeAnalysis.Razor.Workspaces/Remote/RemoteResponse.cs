// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Remote;

[DataContract]
internal record struct RemoteResponse<T>(
    [property: DataMember(Order = 0)] bool StopHandling,
    [property: DataMember(Order = 1)] T Result)
{
    public static RemoteResponse<T> CallHtml => new(StopHandling: false, Result: default!);
    public static RemoteResponse<T> NoFurtherHandling => new(StopHandling: true, Result: default!);
    public static RemoteResponse<T> Results(T result) => new(StopHandling: false, Result: result);
}
