// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundPatternSwitchStatement2
    {
        public HashSet<LabelSymbol> ReachableLabels => this.DecisionDag.ReachableLabels;
    }

}
