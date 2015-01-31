// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal sealed class SuppressionCodeAction : CodeAction
    {
        private readonly string _title;
        public override string Title
        {
            get
            {
                return _title;
            }
        }

        public readonly IEnumerable<CodeAction> NestedActions;

        public SuppressionCodeAction(Diagnostic diagnostic, IEnumerable<CodeAction> nestedActions)
        {
            _title = string.Format(FeaturesResources.SuppressionCodeActionTitle, diagnostic.Id);
            this.NestedActions = nestedActions;
        }
    }
}
