// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class SimpleNameSyntax
    {
        // This override is only intended to support cases where a caller has a value statically typed as NameSyntax in hand 
        // and neither knows nor cares to determine whether that name is qualified or not.
        // If a value is statically typed as a SimpleNameSyntax calling this method is a waste.
        internal sealed override SimpleNameSyntax GetUnqualifiedName()
        {
            return this;
        }
    }
}
