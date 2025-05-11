// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

[Flags]
internal enum ConvertToRawKind
{
    SingleLine = 1 << 0,
    MultiLineIndented = 1 << 1,
    MultiLineWithoutLeadingWhitespace = 1 << 2,

    ContainsEscapedEndOfLineCharacter = 1 << 3,
}
