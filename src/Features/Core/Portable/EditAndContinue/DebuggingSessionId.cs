// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[DataContract]
public readonly record struct DebuggingSessionId([property: DataMember] int Ordinal)
{
    public override string ToString()
        => Ordinal.ToString();
}

public readonly record struct UpdateId(DebuggingSessionId SessionId, int Ordinal);
