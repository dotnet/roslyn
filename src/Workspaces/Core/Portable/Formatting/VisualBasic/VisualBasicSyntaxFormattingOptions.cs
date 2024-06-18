// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.VisualBasic.Formatting;

[DataContract]
internal sealed record class VisualBasicSyntaxFormattingOptions : SyntaxFormattingOptions, IEquatable<VisualBasicSyntaxFormattingOptions>
{
    public static readonly VisualBasicSyntaxFormattingOptions Default = new();

    internal VisualBasicSyntaxFormattingOptions()
        : base()
    {
    }

    internal VisualBasicSyntaxFormattingOptions(IOptionsReader options)
        : base(options, LanguageNames.VisualBasic)
    {
    }
}
