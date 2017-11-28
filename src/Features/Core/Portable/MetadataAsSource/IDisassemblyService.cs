using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal interface IDisassemblyService
    {
        Task<Document> GetDisassemblyAsync(Document document, ISymbol symbol, CancellationToken cancellationToken);
    }
}