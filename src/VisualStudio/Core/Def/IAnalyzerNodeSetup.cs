// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// An interface to be implemented in the SolutionExplorerShim project to register the stuff needed there.
    /// </summary>
    internal interface IAnalyzerNodeSetup
    {
        Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken);
        void Unregister();
    }
}
