// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal static class SignatureHelpParameterExtensions
    {
        internal static SignatureHelpParameter WithSymbol(this SignatureHelpParameter parameter, ISymbol symbol)
        {
            if (symbol != null)
            {
                return parameter.WithProperties(parameter.Properties.SetItem("Symbol", SymbolKey.ToString(symbol)));
            }
            else if (parameter.Properties.ContainsKey("Symbol"))
            {
                return parameter.WithProperties(parameter.Properties.Remove("Symbol"));
            }
            else
            {
                return parameter;
            }
        }

        internal static SymbolKey? GetSymbolKey(this SignatureHelpParameter parameter)
        {
            string symbolKey;
            if (parameter.Properties.TryGetValue("Symbol", out symbolKey))
            {
                return new SymbolKey(symbolKey);
            }
            else
            {
                return default(SymbolKey);
            }
        }

        internal static async Task<ISymbol> GetSymbolAsync(this SignatureHelpParameter parameter, Document document, CancellationToken cancellationToken)
        {
            string symbolKey;
            if (parameter.Properties.TryGetValue("Symbol", out symbolKey) && !string.IsNullOrEmpty(symbolKey))
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

        internal static SignatureHelpParameter WithPosition(this SignatureHelpParameter parameter, int position)
        {
            return parameter.WithProperties(parameter.Properties.SetItem("Position", position.ToString()));
        }

        internal static int GetPosition(this SignatureHelpParameter parameter)
        {
            string positionText;
            int position;
            if (parameter.Properties.TryGetValue("Position", out positionText) && int.TryParse(positionText, out position))
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