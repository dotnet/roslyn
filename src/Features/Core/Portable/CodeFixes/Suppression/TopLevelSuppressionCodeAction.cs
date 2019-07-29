// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal sealed class TopLevelSuppressionCodeAction : AbstractConfigurationActionWithNestedActions
    {
        public TopLevelSuppressionCodeAction(Diagnostic diagnostic, ImmutableArray<NestedSuppressionCodeAction> nestedActions)
            : base(ImmutableArray<CodeAction>.CastUp(nestedActions), string.Format(FeaturesResources.Suppress_0, diagnostic.Id))
        {
        }
    }
}
