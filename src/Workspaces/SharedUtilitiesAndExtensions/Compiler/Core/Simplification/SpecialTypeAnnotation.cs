// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Simplification;

internal class SpecialTypeAnnotation
{
    public const string Kind = "SpecialType";

    private static readonly ConcurrentDictionary<SpecialType, string> s_fromSpecialTypes = new();
    private static readonly ConcurrentDictionary<string, SpecialType> s_toSpecialTypes = new();

    public static SyntaxAnnotation Create(SpecialType specialType)
        => new(Kind, s_fromSpecialTypes.GetOrAdd(specialType, CreateFromSpecialTypes));

    public static SpecialType GetSpecialType(SyntaxAnnotation annotation)
        => s_toSpecialTypes.GetOrAdd(annotation.Data!, CreateToSpecialTypes);

    private static string CreateFromSpecialTypes(SpecialType arg)
        => arg.ToString();

    private static SpecialType CreateToSpecialTypes(string arg)
        => (SpecialType)Enum.Parse(typeof(SpecialType), arg);
}
