// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal abstract record class OmniSharpSyntaxFormattingOptions
    {
        internal abstract SyntaxFormattingOptions ToSyntaxFormattingOptions();
    }
}
