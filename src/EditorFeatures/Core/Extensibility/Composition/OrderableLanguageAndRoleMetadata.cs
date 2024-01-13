// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class OrderableLanguageAndRoleMetadata(IDictionary<string, object> data) : OrderableLanguageMetadata(data)
    {
        public IEnumerable<string> Roles { get; } = (IEnumerable<string>)data.GetValueOrDefault("TextViewRoles");
    }
}
