// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
{
    internal sealed partial class ConfigureSeverityLevelCodeFixProvider : IConfigurationFixProvider
    {
        private sealed class TopLevelBulkConfigureSeverityCodeAction : AbstractConfigurationActionWithNestedActions
        {
            public TopLevelBulkConfigureSeverityCodeAction(ImmutableArray<CodeAction> nestedActions, string? category)
                : base(nestedActions,
                      category != null
                        ? string.Format(FeaturesResources.Configure_severity_for_all_0_analyzers, category)
                        : FeaturesResources.Configure_severity_for_all_analyzers)
            {
                // Ensure that 'Category' based bulk configuration actions are shown above
                // the 'All analyzer diagnostics' bulk configuration actions.
                AdditionalPriority = category != null ? CodeActionPriority.Low : CodeActionPriority.None;
            }

            internal override CodeActionPriority AdditionalPriority { get; }

            internal override bool IsBulkConfigurationAction => true;
        }
    }
}
