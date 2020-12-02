// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(PreprocessorCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExternAliasCompletionProvider))]
    [Shared]
    internal class PreprocessorCompletionProvider : AbstractPreprocessorCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreprocessorCompletionProvider()
        {
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

        protected override Task<SyntaxContext> CreateContextAsync(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => Task.FromResult<SyntaxContext>(CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken));
    }
}
