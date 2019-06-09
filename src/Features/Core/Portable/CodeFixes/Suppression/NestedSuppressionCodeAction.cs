// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract class NestedSuppressionCodeAction : CodeAction
    {
        protected NestedSuppressionCodeAction(string title)
        {
            Title = title;
        }

        // Put suppressions at the end of everything.
        internal override CodeActionPriority Priority => CodeActionPriority.None;

        public sealed override string Title { get; }

        protected abstract string DiagnosticIdForEquivalenceKey { get; }

        public override string EquivalenceKey => Title + DiagnosticIdForEquivalenceKey;

        public static bool IsEquivalenceKeyForGlobalSuppression(string equivalenceKey) =>
            equivalenceKey.StartsWith(FeaturesResources.in_Suppression_File);
        public static bool IsEquivalenceKeyForPragmaWarning(string equivalenceKey) =>
            equivalenceKey.StartsWith(FeaturesResources.in_Source);
        public static bool IsEquivalenceKeyForRemoveSuppression(string equivalenceKey) =>
            equivalenceKey.StartsWith(FeaturesResources.Remove_Suppression);
        public static bool IsEquivalenceKeyForLocalSuppression(string equivalenceKey) =>
            equivalenceKey.StartsWith(FeaturesResources.in_Source_attribute);
    }
}
