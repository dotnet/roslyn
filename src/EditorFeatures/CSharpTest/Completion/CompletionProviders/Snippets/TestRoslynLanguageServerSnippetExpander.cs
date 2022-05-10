// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    [Export(typeof(IRoslynLSPSnippetExpander))]
    [Shared]
    internal class TestRoslynLanguageServerSnippetExpander : IRoslynLSPSnippetExpander
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestRoslynLanguageServerSnippetExpander()
        {
        }

        public bool CanExpandSnippet()
        {
            return true;
        }
    }
}
