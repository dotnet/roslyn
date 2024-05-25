// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

public abstract class AbstractCSharpSnippetCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    protected abstract string ItemToCommit { get; }

    protected AbstractCSharpSnippetCompletionProviderTests()
    {
        ShowNewSnippetExperience = true;
    }

    internal override Type GetCompletionProviderType()
        => typeof(CSharpSnippetCompletionProvider);
}
