﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

internal sealed class QuickInfoClassifiedTextElement(params ImmutableArray<QuickInfoClassifiedTextRun> runs) : QuickInfoElement
{
    public ImmutableArray<QuickInfoClassifiedTextRun> Runs { get; } = runs;
}
