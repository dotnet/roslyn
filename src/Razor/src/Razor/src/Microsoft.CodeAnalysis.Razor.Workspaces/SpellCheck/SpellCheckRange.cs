// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.SpellCheck;

internal readonly record struct SpellCheckRange(int Kind, int AbsoluteStartIndex, int Length);
