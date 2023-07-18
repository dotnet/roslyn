// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal class MetadataAsSourceFileProviderMetadata(IDictionary<string, object> data) : OrderableLanguageMetadata(data)
    {
    }
}
