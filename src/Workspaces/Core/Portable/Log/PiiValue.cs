// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Represents telemetry data that's classified as personally identifiable information.
/// </summary>
[DataContract]
internal sealed class PiiValue(object value)
{
    [DataMember(Order = 0)]
    public readonly object Value = value;

    public override string? ToString()
        => Value.ToString();
}
