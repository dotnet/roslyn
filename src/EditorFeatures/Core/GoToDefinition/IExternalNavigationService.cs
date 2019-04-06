using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IExternalNavigationService
    {
        /// <summary>
        /// Finds the symbol in the host language and navigates to it, or returns false if it doesn't recognize this symbol.
        /// ISymbol is nullable.
        /// </summary>
        Task<bool> TryNavigateToSymbolAsync(string rqName, ISymbol symbol, CancellationToken cancellationToken);
    }
}
