// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [ExportWorkspaceService(typeof(IDecompilerEulaService), ServiceLayer.Default)]
    [Shared]
    internal sealed class DefaultDecompilerEulaService : IDecompilerEulaService
    {
        private bool _isAccepted;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultDecompilerEulaService()
        {
            _isAccepted = false;
        }

        public Task<bool> IsAcceptedAsync(CancellationToken cancellationToken)
        {
            return _isAccepted ? SpecializedTasks.True : SpecializedTasks.False;
        }

        public Task MarkAcceptedAsync(CancellationToken cancellationToken)
        {
            _isAccepted = true;
            return Task.CompletedTask;
        }
    }
}
