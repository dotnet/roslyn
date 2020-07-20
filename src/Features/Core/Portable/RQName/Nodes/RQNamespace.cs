// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQNamespace : RQTypeOrNamespace
    {
        public RQNamespace(IList<string> namespaceNames) : base(namespaceNames) { }

        protected override string RQKeyword
        {
            get { return RQNameStrings.Namespace; }
        }
    }
}
