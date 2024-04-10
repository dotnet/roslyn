// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract class NestedSuppressionCodeAction : CodeAction
{
    protected NestedSuppressionCodeAction(string title)
        => Title = title;

    public sealed override string Title { get; }

    protected abstract string DiagnosticIdForEquivalenceKey { get; }

    public override string EquivalenceKey => Title + DiagnosticIdForEquivalenceKey;

    // Put suppressions at the end of everything.
    protected sealed override CodeActionPriority ComputePriority()
        => CodeActionPriority.Lowest;

    public static bool IsEquivalenceKeyForGlobalSuppression(string equivalenceKey)
        => equivalenceKey.StartsWith(FeaturesResources.in_Suppression_File);
    public static bool IsEquivalenceKeyForPragmaWarning(string equivalenceKey)
        => equivalenceKey.StartsWith(FeaturesResources.in_Source);
    public static bool IsEquivalenceKeyForRemoveSuppression(string equivalenceKey)
        => equivalenceKey.StartsWith(FeaturesResources.Remove_Suppression);
    public static bool IsEquivalenceKeyForLocalSuppression(string equivalenceKey)
        => equivalenceKey.StartsWith(FeaturesResources.in_Source_attribute);
}
