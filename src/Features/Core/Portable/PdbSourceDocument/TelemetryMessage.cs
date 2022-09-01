// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    internal class TelemetryMessage : IDisposable
    {
        private string? _pdbSource;
        private string? _sourceFileSource;

        private readonly IDisposable _logBlock;

        public TelemetryMessage(CancellationToken cancellationToken)
        {
            var logMessage = KeyValueLogMessage.Create(LogType.UserAction, SetLogProperties);
            _logBlock = Logger.LogBlock(FunctionId.NavigateToExternalSources, logMessage, cancellationToken);
        }

        public void SetPdbSource(string source)
        {
            _pdbSource = source;
        }

        public void SetSourceFileSource(string source)
        {
            _sourceFileSource = source;
        }

        private void SetLogProperties(Dictionary<string, object?> properties)
        {
            properties["pdb"] = _pdbSource ?? "none";
            properties["source"] = _sourceFileSource ?? "none";
        }

        public void Dispose()
        {
            _logBlock.Dispose();
        }
    }
}
