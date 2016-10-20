// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    partial class NamingRuleTreeItemViewModel : IInteractionPatternProvider
    {
        TPattern IInteractionPatternProvider.GetPattern<TPattern>()
        {
            return this as TPattern;
        }
    }
}
