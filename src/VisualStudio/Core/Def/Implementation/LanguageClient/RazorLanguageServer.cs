// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Lsp
{
    class RazorLanguageServer : InProcLanguageServer
    {
        public RazorLanguageServer(Stream inputStream, Stream outputStream, LanguageServerProtocol protocol, Workspace workspace, IDiagnosticService diagnosticService)
            : base(inputStream, outputStream, protocol, workspace, diagnosticService)
        {
        }

        protected override Task PublishDiagnosticsAsync(Document document)
        {
            // TODO - Filter razor diagnostics.
            return base.PublishDiagnosticsAsync(document);
        }
    }
}
