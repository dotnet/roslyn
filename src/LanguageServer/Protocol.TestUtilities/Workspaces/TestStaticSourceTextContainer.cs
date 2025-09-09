// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities;

/// <summary>
/// Various tests often need a source text container to simulate workspace OnDocumentOpened calls.
/// </summary>
internal sealed class TestStaticSourceTextContainer(SourceText text) : SourceTextContainer
{
    public override SourceText CurrentText => text;

    public override event EventHandler<TextChangeEventArgs> TextChanged
    {
        add { }
        remove { }
    }
}
