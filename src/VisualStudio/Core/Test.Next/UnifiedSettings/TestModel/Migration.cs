// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal record Migration
{
    public Migration(EnumToInteger enumToInteger)
    {
        EnumToInteger = enumToInteger;
    }

    public Migration(Pass pass)
    {
        Pass = pass;
    }

    [JsonPropertyName("pass")]
    public Pass? Pass { get; }

    [JsonPropertyName("EnumToInteger")]
    public EnumToInteger? EnumToInteger { get; }
}

