﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;

internal sealed class SplitStringLiteralOptions
{
    public static Option2<bool> Enabled = new("SplitStringLiteralOptions_Enabled", defaultValue: true);
}
