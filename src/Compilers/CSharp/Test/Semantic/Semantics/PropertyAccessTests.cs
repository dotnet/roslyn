// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class PropertyAccessTests : CompilingTestBase
    {
        [Fact]
        [WorkItem(1140706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1140706")]
        public void SetArrayLength()
        {
            var text =
@"class Test
{
    void M()
    {
        int[] arr = { 1 };
        arr.Length = 0;
    }
}";
            var comp = CreateEmptyCompilation(
                source: new[] { Parse(text) },
                references: new[] { AacorlibRef });

            comp.VerifyEmitDiagnostics();
        }
    }
}
