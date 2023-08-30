// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
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

        public static string GetDefaultFixAllTitle(this FixAllContext context)
            => FixAllHelper.GetDefaultFixAllTitle(context.Scope, title: context.DiagnosticIds.First(), context.Document!, context.Project);
    }
}
