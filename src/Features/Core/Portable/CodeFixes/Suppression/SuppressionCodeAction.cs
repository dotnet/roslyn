// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal sealed class SuppressionCodeAction : CodeAction
    {
        private readonly string _title;
        private readonly string _equivalenceKey;
        public readonly IEnumerable<CodeAction> NestedActions;

        public SuppressionCodeAction(Diagnostic diagnostic, IEnumerable<CodeAction> nestedActions)
        {
            _title = string.Format(FeaturesResources.SuppressionCodeActionTitle, diagnostic.Id);
            _equivalenceKey = ComputeEquivalenceKey(nestedActions);
            this.NestedActions = nestedActions;
        }

        public override string Title => _title;
        public override string EquivalenceKey => _equivalenceKey;

        private static string ComputeEquivalenceKey(IEnumerable<CodeAction> nestedActions)
        {
            if (nestedActions == null)
            {
                return null;
            }

            var equivalenceKey = string.Empty;
            foreach (var action in nestedActions)
            {
                equivalenceKey += (action.EquivalenceKey ?? action.GetHashCode().ToString() + ";");
            }

            return equivalenceKey;
        }
    }
}
