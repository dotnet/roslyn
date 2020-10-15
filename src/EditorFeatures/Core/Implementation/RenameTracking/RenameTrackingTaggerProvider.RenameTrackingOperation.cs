// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        /// <summary>
        /// This class exists for test usage without fully exposing the private classes of RenameTrackingTaggerProvider
        /// </summary>
        internal abstract class RenameTrackingOperation : CodeActionOperation
        {
            public abstract Task<(Solution originalSolution, Solution newSolution)> GetChangedSolutionAsync(CancellationToken cancellationToken);
        }
    }
}
