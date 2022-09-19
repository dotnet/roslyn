// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal interface IUnitTestingWorkCoordinatorPriorityService : ILanguageService
    {
        /// <summary>
        /// True if this document is less important than other documents in the project it is 
        /// contained in, and should have work scheduled for it happen after all other documents
        /// in the project.
        /// </summary>
        Task<bool> IsLowPriorityAsync(Document document, CancellationToken cancellationToken);
    }
}
