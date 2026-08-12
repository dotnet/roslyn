// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ImplementNotifyPropertyChangedCS
{
    internal class ExpandablePropertyInfo
    {
        public string BackingFieldName { get; internal set; }
        public bool NeedsBackingField { get; internal set; }
        public PropertyDeclarationSyntax PropertyDeclaration { get; internal set; }
        public ITypeSymbol Type { get; internal set; }
    }
}
