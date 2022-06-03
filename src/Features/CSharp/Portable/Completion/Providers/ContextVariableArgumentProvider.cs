// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportArgumentProvider(nameof(ContextVariableArgumentProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(FirstBuiltInArgumentProvider))]
    [Shared]
    internal sealed class ContextVariableArgumentProvider : AbstractContextVariableArgumentProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ContextVariableArgumentProvider()
        {
        }

        protected override string ThisOrMeKeyword => SyntaxFacts.GetText(SyntaxKind.ThisKeyword);

        protected override bool IsInstanceContext(SyntaxTree syntaxTree, SyntaxToken targetToken, SemanticModel semanticModel, CancellationToken cancellationToken)
            => syntaxTree.IsInstanceContext(targetToken, semanticModel, cancellationToken);

        public override async Task ProvideArgumentAsync(ArgumentContext context)
        {
            await base.ProvideArgumentAsync(context).ConfigureAwait(false);
            if (context.DefaultValue is not null)
            {
                switch (context.Parameter.RefKind)
                {
                    case RefKind.Ref:
                        context.DefaultValue = "ref " + context.DefaultValue;
                        break;

                    case RefKind.Out:
                        context.DefaultValue = "out " + context.DefaultValue;
                        break;

                    case RefKind.In:
                    case RefKind.None:
                    default:
                        break;
                }
            }
        }
    }
}
