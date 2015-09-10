// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
