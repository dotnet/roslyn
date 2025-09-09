// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;

internal sealed partial class ConfigureSeverityLevelCodeFixProvider : IConfigurationFixProvider
{
    private sealed class TopLevelConfigureSeverityCodeAction(Diagnostic diagnostic, ImmutableArray<CodeAction> nestedActions) : AbstractConfigurationActionWithNestedActions(nestedActions, string.Format(FeaturesResources.Configure_0_severity, diagnostic.Id))
    {
    }
}
