// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportArgumentProvider(nameof(ContextVariableArgumentProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(FirstBuiltInArgumentProvider))]
    [Shared]
    internal sealed class ContextVariableArgumentProvider : ArgumentProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ContextVariableArgumentProvider()
        {
        }

        public override async Task ProvideArgumentAsync(ArgumentContext context)
        {
            if (context.PreviousValue is not null)
            {
                return;
            }

            var semanticModel = await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var symbols = semanticModel.LookupSymbols(context.Position, name: context.Parameter.Name);
            foreach (var symbol in symbols)
            {
                if (SymbolEqualityComparer.Default.Equals(context.Parameter.Type, symbol.GetSymbolType()))
                {
                    context.DefaultValue = context.Parameter.Name;
                    return;
                }
            }
        }
    }
}
