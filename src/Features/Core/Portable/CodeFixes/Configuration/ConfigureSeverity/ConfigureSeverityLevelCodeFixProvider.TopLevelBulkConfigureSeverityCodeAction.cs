// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;

internal sealed partial class ConfigureSeverityLevelCodeFixProvider : IConfigurationFixProvider
{
    private sealed class TopLevelBulkConfigureSeverityCodeAction(ImmutableArray<CodeAction> nestedActions, string? category) : AbstractConfigurationActionWithNestedActions(nestedActions,
              category != null
                    ? string.Format(FeaturesResources.Configure_severity_for_all_0_analyzers, category)
                    : FeaturesResources.Configure_severity_for_all_analyzers)
    {
        internal override CodeActionPriority AdditionalPriority { get; } = category != null ? CodeActionPriority.Low : CodeActionPriority.Lowest;

        internal override bool IsBulkConfigurationAction => true;
    }
}
