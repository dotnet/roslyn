// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal sealed class SuppressionCodeAction : CodeAction.SimpleCodeAction
    {
        public SuppressionCodeAction(Diagnostic diagnostic, IEnumerable<CodeAction> nestedActions)
            : base(string.Format(FeaturesResources.SuppressionCodeActionTitle, diagnostic.Id), nestedActions.AsImmutableOrEmpty())
        {
        }
    }
}
