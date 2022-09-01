// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(AggregateEmbeddedLanguageCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExtensionMethodImportCompletionProvider))]
    [Shared]
    internal class AggregateEmbeddedLanguageCompletionProvider : AbstractAggregateEmbeddedLanguageCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AggregateEmbeddedLanguageCompletionProvider([ImportMany] IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> languageServices) : base(languageServices, LanguageNames.CSharp)
        {
        }

        internal override string Language => LanguageNames.CSharp;
    }
}
