// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract class NestedSuppressionCodeAction : CodeAction
    {
        private readonly string _title;

        protected NestedSuppressionCodeAction(string title)
        {
            _title = title;
        }

        public sealed override string Title => _title;

        protected abstract string DiagnosticIdForEquivalenceKey { get; }

        public override string EquivalenceKey => Title + DiagnosticIdForEquivalenceKey;

        public static bool IsEquivalenceKeyForGlobalSuppression(string equivalenceKey) =>
            equivalenceKey.StartsWith(FeaturesResources.SuppressWithGlobalSuppressMessage);
        public static bool IsEquivalenceKeyForPragmaWarning(string equivalenceKey) =>
            equivalenceKey.StartsWith(FeaturesResources.SuppressWithPragma);
        public static bool IsEquivalenceKeyForRemoveSuppression(string equivalenceKey) =>
            equivalenceKey.StartsWith(FeaturesResources.RemoveSuppressionEquivalenceKeyPrefix);
    }
}
