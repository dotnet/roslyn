// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal sealed class CSharpSimplification : AbstractSimplification
{
    public static readonly CSharpSimplification Instance = new();

    public override SimplifierStyleOptions DefaultOptions
        => CSharpSimplifierStyleOptions.Default;

    public override SimplifierStyleOptions GetSimplifierOptions(IOptionsReader options, SimplifierStyleOptions? fallbackOptions)
        => new CSharpSimplifierStyleOptions(options, (CSharpSimplifierStyleOptions?)fallbackOptions);
}
