// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName.Nodes
{
    internal class RQNamespace : RQTypeOrNamespace<INamespaceSymbol>
    {
        public RQNamespace(IList<string> namespaceNames) : base(namespaceNames) { }

        protected override string RQKeyword
        {
            get { return RQNameStrings.Namespace; }
        }
    }
}
