// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.MSBuild.Rpc;

internal sealed class Response
{
    public int Id { get; init; }
    public JToken? Value { get; init; }
    public string? Exception { get; init; }
}

