using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    interface IRequestHandlerMetadata
    {
        /// <summary>
        /// Name of the LSP method to handle.
        /// </summary>
        string MethodName { get; }
    }
}
