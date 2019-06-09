// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
