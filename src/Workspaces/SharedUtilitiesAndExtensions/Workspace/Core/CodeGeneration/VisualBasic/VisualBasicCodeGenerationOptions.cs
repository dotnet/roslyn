// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration;

[DataContract]
internal sealed record class VisualBasicCodeGenerationOptions : CodeGenerationOptions
{
    public static readonly VisualBasicCodeGenerationOptions Default = new();

    public VisualBasicCodeGenerationOptions()
    {
    }

    internal VisualBasicCodeGenerationOptions(IOptionsReader options, VisualBasicCodeGenerationOptions? fallbackOptions)
        : base(options, fallbackOptions ?? Default, LanguageNames.VisualBasic)
    {
    }
}
