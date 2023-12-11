// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(RegisterExtensionHandler)), Shared]
    [Method("extension/registerExtensionHandler")]
    internal class RegisterExtensionHandler : ILspServiceRequestHandler<string, bool>
    {
        public bool MutatesSolutionState => true;

        public bool RequiresLSPSolution => false;

        public Task<bool> HandleRequestAsync(string request, RequestContext context, CancellationToken cancellationToken)
        {
            // We need to register the DLL into our list 
            // How do this??

            // Successful 
            return Task.FromResult(true);
        }
    }
}
