// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal static class FixAllContextExtensions
    {
        public static IProgressTracker GetProgressTracker(this FixAllContext context)
        {
#if CODE_STYLE
            return NoOpProgressTracker.Instance;
#else
            return context.ProgressTracker;
#endif
        }
    }
}
