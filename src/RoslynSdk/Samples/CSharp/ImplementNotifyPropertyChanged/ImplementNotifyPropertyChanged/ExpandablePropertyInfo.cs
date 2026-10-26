// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
