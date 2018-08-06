﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class ForEachStatementInfoTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var c = CreateCompilation(@"
class E1
{
    public E GetEnumerator() { return null; }
    public bool MoveNext() { return false; }
    public object Current { get; }
    public void Dispose() { } 
}

class E2
{
    public E GetEnumerator() { return null; }
    public bool MoveNext() { return false; }
    public object Current { get; }
    public void Dispose() { } 
}
");
            var e1 = (TypeSymbol)c.GlobalNamespace.GetMembers("E1").Single();
            var ge1 = (MethodSymbol)e1.GetMembers("GetEnumerator").Single();
            var mn1 = (MethodSymbol)e1.GetMembers("MoveNext").Single();
            var cur1 = (PropertySymbol)e1.GetMembers("Current").Single();
            var disp1 = (MethodSymbol)e1.GetMembers("Dispose").Single();
            var conv1 = Conversion.Identity;

            var e2 = (TypeSymbol)c.GlobalNamespace.GetMembers("E2").Single();
            var ge2 = (MethodSymbol)e2.GetMembers("GetEnumerator").Single();
            var mn2 = (MethodSymbol)e2.GetMembers("MoveNext").Single();
            var cur2 = (PropertySymbol)e2.GetMembers("Current").Single();
            var disp2 = (MethodSymbol)e2.GetMembers("Dispose").Single();
            var conv2 = Conversion.NoConversion;

            EqualityTesting.AssertEqual(default(ForEachStatementInfo), default(ForEachStatementInfo));
            EqualityTesting.AssertEqual(new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1), new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(ge2, mn1, cur1, disp1, e1, conv1, conv1), new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(ge1, mn2, cur1, disp1, e1, conv1, conv1), new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(ge1, mn1, cur2, disp1, e1, conv1, conv1), new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(ge1, mn1, cur1, disp2, e1, conv1, conv1), new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv2, conv1), new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv2), new ForEachStatementInfo(ge1, mn1, cur1, disp1, e1, conv1, conv1));
        }
    }
}
