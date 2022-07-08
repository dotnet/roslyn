// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeActions
{
#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    /// <summary>
    /// Internal priority used to bluntly place items in a light bulb in strict orderings.  Priorities take
    /// the highest precedence when ordering items so that we can ensure very important items get top prominence,
    /// and low priority items do not.
    /// </summary>
    /// <remarks>
    /// If <see cref="CodeActionPriority.High"/> is used, the feature that specifies that value should 
    /// implement and return <see cref="CodeActionRequestPriority.High"/> for <see cref="IBuiltInAnalyzer.RequestPriority"/>,
    /// <see cref="T:Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider.RequestPriority"/> and
    /// <see cref="T:Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringProvider.RequestPriority"/>. This
    /// will ensure that the analysis engine runs the providers that will produce those actions first,
    /// thus allowing those actions to be computed and displayed prior to running all other providers.
    /// </remarks>
    internal enum CodeActionPriority
    {
        Lowest = 0,
        Low = 1,
        Medium = 2,
        High = 3,

        Default = Medium
    }
}
