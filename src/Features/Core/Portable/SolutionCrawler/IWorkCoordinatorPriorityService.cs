// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler;

[Obsolete("Remove your implementation of this interface and let Roslyn know so that this interface can be removed.", error: false)]
internal interface IWorkCoordinatorPriorityService : ILanguageService
{
    /// <summary>
    /// True if this document is less important than other documents in the project it is 
    /// contained in, and should have work scheduled for it happen after all other documents
    /// in the project.
    /// </summary>
    Task<bool> IsLowPriorityAsync(Document document, CancellationToken cancellationToken);
}
