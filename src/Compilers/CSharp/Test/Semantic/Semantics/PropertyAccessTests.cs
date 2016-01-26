// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var comp = CreateCompilation(
                trees: new[] { Parse(text) },
                references: new[] { AacorlibRef });

            comp.VerifyEmitDiagnostics();
        }
    }
}
