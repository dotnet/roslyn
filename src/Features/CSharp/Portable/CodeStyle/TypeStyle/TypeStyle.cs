// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle
{
    [Flags]
    internal enum TypeStyle
    {
        None = 0,
        ImplicitTypeForIntrinsicTypes = 1 << 0,
        ImplicitTypeWhereApparent = 1 << 1,
        ImplicitTypeWherePossible = 1 << 2,
    }
}