// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal static class SignatureHelpItemExtensions
    {
        internal static SignatureHelpItem WithSymbol(this SignatureHelpItem item, ISymbol symbol)
        {
            if (symbol != null)
            {
                return item.WithProperties(item.Properties.SetItem("Symbol", SymbolKey.ToString(symbol)));
            }
            else if (item.Properties.ContainsKey("Symbol"))
            {
                return item.WithProperties(item.Properties.Remove("Symbol"));
            }
            else
            {
                return item;
            }
        }

        internal static SymbolKey? GetSymbolKey(this SignatureHelpItem item)
        {
            string symbolKey;
            if (item.Properties.TryGetValue("Symbol", out symbolKey))
            {
                return new SymbolKey(symbolKey);
            }
            else
            {
                return default(SymbolKey);
            }
        }

        internal static async Task<ISymbol> GetSymbolAsync(this SignatureHelpItem item, Document document, CancellationToken cancellationToken)
        {
            string symbolKey;
            if (item.Properties.TryGetValue("Symbol", out symbolKey) && !string.IsNullOrEmpty(symbolKey))
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var resolution = SymbolKey.Resolve(symbolKey, compilation, cancellationToken: cancellationToken);
                return resolution.Symbol;
            }
            else
            {
                return null;
            }
        }

        internal static SignatureHelpItem WithPosition(this SignatureHelpItem item, int position)
        {
            return item.WithProperties(item.Properties.SetItem("Position", position.ToString()));
        }

        internal static int GetPosition(this SignatureHelpItem item)
        {
            string positionText;
            int position;
            if (item.Properties.TryGetValue("Position", out positionText) && int.TryParse(positionText, out position))
            {
                return position;
            }
            else
            {
                return 0;
            }
        }
    }
}