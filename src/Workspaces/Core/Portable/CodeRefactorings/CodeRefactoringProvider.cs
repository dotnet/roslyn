// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Inherit this type to provide source code refactorings.
/// Remember to use <see cref="ExportCodeRefactoringProviderAttribute"/> so the host environment can offer your refactorings in a UI.
/// </summary>
public abstract class CodeRefactoringProvider
{
    private protected ImmutableArray<string> CustomTags = [];

    /// <summary>
    /// Computes one or more refactorings for the specified <see cref="CodeRefactoringContext"/>.
    /// </summary>
    public abstract Task ComputeRefactoringsAsync(CodeRefactoringContext context);

    /// <summary>
    /// Gets an optional <see cref="FixAllProvider"/> that can apply multiple occurrences of code refactoring(s)
    /// registered by this code refactoring provider across the supported <see cref="CodeFixes.FixAllScope"/>s.
    /// Return null if the provider doesn't support fix all operation.
    /// </summary>
    /// <remarks>
    /// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
    /// </remarks>
    internal virtual FixAllProvider? GetFixAllProvider()
        => null;

    /// <summary>
    /// Computes the <see cref="CodeActionRequestPriority"/> group this provider should be considered to run at. Legal values
    /// this can be must be between <see cref="CodeActionRequestPriority.Low"/> and <see cref="CodeActionPriority.High"/>.
    /// </summary>
    /// <remarks>
    /// Values outside of this range will be clamped to be within that range.  Requests for <see
    /// cref="CodeActionRequestPriority.High"/> may be downgraded to <see cref="CodeActionRequestPriority.Default"/> as they
    /// poorly behaving high-priority providers can cause a negative user experience.
    /// </remarks>
    protected virtual CodeActionRequestPriority ComputeRequestPriority()
        => CodeActionRequestPriority.Default;

    /// <summary>
    /// Priority class this refactoring provider should run at. Returns <see
    /// cref="CodeActionRequestPriority.Default"/> if not overridden.  Slower, or less relevant, providers should
    /// override this and return a lower value to not interfere with computation of normal priority providers.
    /// </summary>
    public CodeActionRequestPriority RequestPriority
    {
        get
        {
            var priority = ComputeRequestPriority();
            Debug.Assert(priority is CodeActionRequestPriority.Low or CodeActionRequestPriority.Default or CodeActionRequestPriority.High, "Provider returned invalid priority");
            return priority.Clamp(this.CustomTags);
        }
    }
}
