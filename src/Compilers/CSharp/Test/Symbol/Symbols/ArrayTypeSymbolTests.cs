﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class ArrayTypeSymbolTests : CSharpTestBase
    {
        [Fact(), WorkItem(546670, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546670")]
        public void MissingIList()
        {
            var c = CreateEmptyCompilation(@"
public class X 
{
    public static int[] A;
}
", new[] { MinCorlibRef });

            var field = c.GlobalNamespace.GetMember<NamedTypeSymbol>("X").GetMember<FieldSymbol>("A");
            Assert.Equal(0, field.Type.Interfaces().Length);
            c.VerifyDiagnostics();
        }
    }
}
