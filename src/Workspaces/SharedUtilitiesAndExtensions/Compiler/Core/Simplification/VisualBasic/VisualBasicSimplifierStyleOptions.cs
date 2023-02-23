// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.VisualBasic.Simplification;

#if CODE_STYLE
internal
#else
public
#endif
interface IVisualBasicSimplifierOptions : ISimplifierOptions
{
}

#if CODE_STYLE
internal
#else
public
#endif
record class VisualBasicSimplifierOptions : SimplifierOptions, IVisualBasicSimplifierOptions
{
    public static readonly VisualBasicSimplifierOptions Default = new();

    public VisualBasicSimplifierOptions()
    {
    }
}

[DataContract]
internal sealed record class VisualBasicSimplifierStyleOptions : SimplifierStyleOptions, IVisualBasicSimplifierOptions
{
    public static readonly VisualBasicSimplifierStyleOptions Default = new();

    public VisualBasicSimplifierStyleOptions()
    {
    }

    public VisualBasicSimplifierStyleOptions(IOptionsReader options, VisualBasicSimplifierStyleOptions? fallbackOptions)
        : base(options, fallbackOptions ?? Default, LanguageNames.VisualBasic)
    {
    }
}
