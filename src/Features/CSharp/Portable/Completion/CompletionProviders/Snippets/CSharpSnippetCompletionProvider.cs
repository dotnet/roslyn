// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{
    [ExportCompletionProvider(nameof(CSharpSnippetCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(FirstBuiltInCompletionProvider))]
    [Shared]
    internal class CSharpSnippetCompletionProvider : AbstractSnippetCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSnippetCompletionProvider()
        {

        }

        internal override string Language => LanguageNames.CSharp;
    }
}
