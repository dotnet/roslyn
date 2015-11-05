// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// This class handles incoming requests from the client, and invokes the compiler to actually
    /// do the compilation. We also handle the caching of assembly bytes and assembly objects here.
    /// </summary>
    internal class CompilerRequestHandler : IRequestHandler
    {
        private readonly DesktopCompilerServerHost _desktopCompilerServerHost;
        private readonly CompilerRunHandler _compilerRunHandler;

        internal CompilerRequestHandler(string clientDirectory)
        {
            _desktopCompilerServerHost = new DesktopCompilerServerHost();
            _compilerRunHandler = new CompilerRunHandler(_desktopCompilerServerHost, clientDirectory);
        }

        /// <summary>
        /// An incoming request as occurred. This is called on a new thread to handle
        /// the request.
        /// </summary>
        public BuildResponse HandleRequest(BuildRequest req, CancellationToken cancellationToken)
        {
            var request = BuildProtocolUtil.GetRunRequest(req);
            var result = _compilerRunHandler.HandleRequest(request, cancellationToken);
            return BuildProtocolUtil.GetBuildResponse(result);
        }
    }
}
