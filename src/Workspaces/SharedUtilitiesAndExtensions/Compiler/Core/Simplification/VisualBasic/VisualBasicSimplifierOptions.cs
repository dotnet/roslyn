// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.VisualBasic.Simplification;

[DataContract]
internal sealed record class VisualBasicSimplifierOptions : SimplifierOptions, IEquatable<VisualBasicSimplifierOptions>
{
    public static readonly VisualBasicSimplifierOptions Default = new();

    public VisualBasicSimplifierOptions()
    {
    }

    public VisualBasicSimplifierOptions(IOptionsReader options, VisualBasicSimplifierOptions? fallbackOptions)
        : base(options, fallbackOptions ?? Default, LanguageNames.VisualBasic)
    {
    }
}
