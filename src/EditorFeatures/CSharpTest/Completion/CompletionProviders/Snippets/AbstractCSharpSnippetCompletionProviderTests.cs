// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
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
}
