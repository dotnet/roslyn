// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;

/// <summary>
/// Service to allow adding or removing bulk suppressions (in source or suppressions file).
/// </summary>
/// <remarks>*** NOTE: These internal APIs are used in the VSO repo in Microsoft.VisualStudio.CodeAnalysis.MenuHandlers ***</remarks>
internal interface IVisualStudioSuppressionFixService
{
    /// <summary>
    /// Adds source suppressions for all the diagnostics in the error list, i.e. baseline all active issues.
    /// </summary>
    /// <param name="projectHierarchy">An optional project hierarchy object in the solution explorer. If non-null, then only the diagnostics from the project will be suppressed.</param>
    bool AddSuppressions(IVsHierarchy? projectHierarchy);

    /// <summary>
    /// Adds source suppressions for diagnostics.
    /// </summary>
    /// <param name="selectedErrorListEntriesOnly">If true, then only the currently selected entries in the error list will be suppressed. Otherwise, all suppressable entries in the error list will be suppressed.</param>
    /// <param name="suppressInSource">If true, then suppressions will be generated inline in the source file. Otherwise, they will be generated in a separate global suppressions file.</param>
    /// <param name="projectHierarchy">An optional project hierarchy object in the solution explorer. If non-null, then only the diagnostics from the project will be suppressed.</param>
    bool AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy? projectHierarchy);

    /// <summary>
    /// Removes source suppressions for suppressed diagnostics.
    /// </summary>
    /// <param name="selectedErrorListEntriesOnly">If true, then only the currently selected entries in the error list will be unsuppressed. Otherwise, all unsuppressable entries in the error list will be unsuppressed.</param>
    /// <param name="projectHierarchy">An optional project hierarchy object in the solution explorer. If non-null, then only the diagnostics from the project will be unsuppressed.</param>
    bool RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy? projectHierarchy);
}
