// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

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
