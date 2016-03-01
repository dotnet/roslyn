// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Implementation.RQName.SimpleTree;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName.Nodes
{
    internal class RQExplicitInterfaceMemberName : RQMethodPropertyOrEventName
    {
        public readonly RQType InterfaceType;
        public readonly RQOrdinaryMethodPropertyOrEventName Name;

        public RQExplicitInterfaceMemberName(RQType interfaceType, RQOrdinaryMethodPropertyOrEventName name)
        {
            this.InterfaceType = interfaceType;
            this.Name = name;
        }

        public override string OrdinaryNameValue => Name.OrdinaryNameValue;

        public override SimpleGroupNode ToSimpleTree()
        {
            return new SimpleGroupNode(RQNameStrings.IntfExplName, InterfaceType.ToSimpleTree(), Name.ToSimpleTree());
        }
    }
}
