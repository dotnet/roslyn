// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
