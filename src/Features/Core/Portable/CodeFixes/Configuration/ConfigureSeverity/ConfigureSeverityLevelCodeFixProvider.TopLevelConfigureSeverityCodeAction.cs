// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
{
    internal sealed partial class ConfigureSeverityLevelCodeFixProvider : IConfigurationFixProvider
    {
        private sealed class TopLevelConfigureSeverityCodeAction : AbstractConfigurationActionWithNestedActions
        {
            public TopLevelConfigureSeverityCodeAction(Diagnostic diagnostic, ImmutableArray<CodeAction> nestedActions)
                : base(nestedActions, string.Format(FeaturesResources.Configure_0_severity, diagnostic.Id))
            {
            }
        }
    }
}
