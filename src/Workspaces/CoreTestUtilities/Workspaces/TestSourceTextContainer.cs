// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal sealed class TestSourceTextContainer : SourceTextContainer
{
    public required SourceText Text { get; init; }

    public override SourceText CurrentText => Text;

#pragma warning disable CS0067 // The event 'TestSourceTextContainer.TextChanged' is never used
    public override event EventHandler<TextChangeEventArgs>? TextChanged;
#pragma warning restore CS0067
}
