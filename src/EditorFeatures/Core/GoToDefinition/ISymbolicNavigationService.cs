using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ISymbolicNavigationService
    {
        /// <summary>
        /// Is this a symbol of your language? If it is, tell us and you can take over the navigation however you would like.
        /// </summary>
        Task<bool> TryNavigateToSymbol(ISymbol symbol, CancellationToken cancellationToken);

        /// <summary>
        /// Is this a metadata name of your language? If it is, tell us and you can take over the navigation however you would like.
        /// </summary>
        Task<bool> TryNavigateToSymbol(string metaDataName, CancellationToken cancellationToken);
    }
}
