// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static class FixAllContextExtensions
    {
        public static IProgress<CodeAnalysisProgress> GetProgress(this FixAllContext context)
        {
#if CODE_STYLE
            return CodeAnalysisProgress.Null;
#else
            return context.Progress;
#endif
        }

        public static string GetDefaultFixAllTitle(this FixAllContext context)
            => FixAllHelper.GetDefaultFixAllTitle(context.Scope, title: context.DiagnosticIds.First(), context.Document!, context.Project);
    }
}
