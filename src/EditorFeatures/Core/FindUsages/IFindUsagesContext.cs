// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal interface IFindUsagesContext
    {
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Report a message to be displayed to the user.
        /// </summary>
        Task ReportMessageAsync(string message);

        /// <summary>
        /// Set the title of the window that results are displayed in.
        /// </summary>
        Task SetSearchTitleAsync(string title);

        Task OnDefinitionFoundAsync(DefinitionItem definition);
        Task OnReferenceFoundAsync(SourceReferenceItem reference);

        Task ReportProgressAsync(int current, int maximum);
    }
}
