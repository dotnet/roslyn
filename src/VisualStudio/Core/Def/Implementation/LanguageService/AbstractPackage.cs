﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract class AbstractPackage : AsyncPackage
    {
        protected async Task LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(CancellationToken cancellationToken)
        {
            // UIContexts can be "zombied" if UIContexts aren't supported because we're in a command line build or in other scenarios.
            // Trying to await them will throw.
            if (!KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsZombie)
            {
                await KnownUIContexts.SolutionExistsAndFullyLoadedContext;
                await LoadComponentsAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task LoadComponentsAsync(CancellationToken cancellationToken);
    }
}
