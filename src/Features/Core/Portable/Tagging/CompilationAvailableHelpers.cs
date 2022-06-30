﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Tagging
{
    internal static class CompilationAvailableHelpers
    {
        // this method is super basic.  but it ensures that the remote impl and the local impl always agree.
        public static Task ComputeCompilationInCurrentProcessAsync(Project project, CancellationToken cancellationToken)
            => project.GetCompilationAsync(cancellationToken);
    }
}
