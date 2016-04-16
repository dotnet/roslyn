// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractDescriptionBuilder
    {
        [Flags]
        protected enum LinkFlags
        {
            None = 0,
            ExpandPredefinedTypes = 1 << 1,
            SplitNamespaceAndType = 1 << 2
        }
    }
}
