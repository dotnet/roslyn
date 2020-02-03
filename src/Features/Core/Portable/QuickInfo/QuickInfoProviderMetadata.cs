// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal class QuickInfoProviderMetadata : OrderableLanguageMetadata
    {
        public QuickInfoProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
        }
    }
}
