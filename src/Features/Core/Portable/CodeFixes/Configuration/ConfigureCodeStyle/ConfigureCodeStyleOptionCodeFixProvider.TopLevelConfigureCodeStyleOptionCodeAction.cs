// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
