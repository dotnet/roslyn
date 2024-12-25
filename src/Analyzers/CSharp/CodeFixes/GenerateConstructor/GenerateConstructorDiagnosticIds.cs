// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructor;

internal static class GenerateConstructorDiagnosticIds
{
    public const string CS0122 = nameof(CS0122); // CS0122: 'C' is inaccessible due to its protection level
    public const string CS1729 = nameof(CS1729); // CS1729: 'C' does not contain a constructor that takes n arguments
    public const string CS1739 = nameof(CS1739); // CS1739: The best overload for 'Program' does not have a parameter named 'v'
    public const string CS1503 = nameof(CS1503); // CS1503: Argument 1: cannot convert from 'T1' to 'T2'
    public const string CS1660 = nameof(CS1660); // CS1660: Cannot convert lambda expression to type 'string[]' because it is not a delegate type
    public const string CS7036 = nameof(CS7036); // CS7036: There is no argument given that corresponds to the required parameter 'v' of 'C.C(int)'

    public static readonly ImmutableArray<string> AllDiagnosticIds =
        [CS0122, CS1729, CS1739, CS1503, CS1660, CS7036];

    public static readonly ImmutableArray<string> TooManyArgumentsDiagnosticIds =
        [CS1729];

    public static readonly ImmutableArray<string> CannotConvertDiagnosticIds =
        [CS1503, CS1660];
}
