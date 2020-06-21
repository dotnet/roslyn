// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class OrderableLanguageAndRoleMetadata : OrderableLanguageMetadata
    {
        public IEnumerable<string> Roles { get; }

        public OrderableLanguageAndRoleMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.Roles = (IEnumerable<string>)data.GetValueOrDefault("TextViewRoles");
        }
    }
}
