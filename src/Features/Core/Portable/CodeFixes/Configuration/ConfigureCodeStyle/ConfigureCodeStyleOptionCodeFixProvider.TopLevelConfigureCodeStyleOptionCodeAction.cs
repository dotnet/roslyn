// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle
{
    internal sealed partial class ConfigureCodeStyleOptionCodeFixProvider : IConfigurationFixProvider
    {
        private sealed class TopLevelConfigureCodeStyleOptionCodeAction : AbstractConfigurationActionWithNestedActions
        {
            public TopLevelConfigureCodeStyleOptionCodeAction(Diagnostic diagnostic, ImmutableArray<CodeAction> nestedActions)
                : base(nestedActions, string.Format(FeaturesResources.Configure_0_code_style, diagnostic.Id))
            {
            }

            public TopLevelConfigureCodeStyleOptionCodeAction(string optionName, ImmutableArray<CodeAction> nestedActions)
                : base(nestedActions, optionName)
            {
            }
        }
    }
}
