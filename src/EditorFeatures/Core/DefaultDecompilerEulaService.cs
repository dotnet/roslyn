// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            => _isAccepted = false;

        public Task<bool> IsAcceptedAsync(CancellationToken cancellationToken)
            => _isAccepted ? SpecializedTasks.True : SpecializedTasks.False;

        public Task MarkAcceptedAsync(CancellationToken cancellationToken)
        {
            _isAccepted = true;
            return Task.CompletedTask;
        }
    }
}
