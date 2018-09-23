// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
class E3
{
    public E GetAsyncEnumerator() => throw null;
    public ValueTask<bool> WaitForNextAsync() => throw null;
    public object TryGetNext(out bool success) => throw null;
    public ValueTask DisposeAsync() => throw null;
}
class E4
{
    public E GetAsyncEnumerator() => throw null;
    public ValueTask<bool> WaitForNextAsync() => throw null;
    public object TryGetNext(out bool success) => throw null;
    public ValueTask DisposeAsync() => throw null;
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

            var e3 = (TypeSymbol)c.GlobalNamespace.GetMembers("E3").Single();
            var gae3 = (MethodSymbol)e3.GetMembers("GetAsyncEnumerator").Single();
            var wfna3 = (MethodSymbol)e3.GetMembers("WaitForNextAsync").Single();
            var tgn3 = (MethodSymbol)e3.GetMembers("TryGetNext").Single();
            var disp3 = (MethodSymbol)e3.GetMembers("DisposeAsync").Single();
            var conv3 = Conversion.NoConversion;

            var e4 = (TypeSymbol)c.GlobalNamespace.GetMembers("E4").Single();
            var gae4 = (MethodSymbol)e4.GetMembers("GetAsyncEnumerator").Single();
            var wfna4 = (MethodSymbol)e4.GetMembers("WaitForNextAsync").Single();
            var tgn4 = (MethodSymbol)e4.GetMembers("TryGetNext").Single();
            var disp4 = (MethodSymbol)e4.GetMembers("DisposeAsync").Single();
            var conv4 = Conversion.NoConversion;

            EqualityTesting.AssertEqual(
                new ForEachStatementInfo(gae3, moveNextMethod: null, currentProperty: null, disp3, e3, conv3, conv3, wfna3, tgn3),
                new ForEachStatementInfo(gae3, moveNextMethod: null, currentProperty: null, disp3, e3, conv3, conv3, wfna3, tgn3));
            EqualityTesting.AssertNotEqual(
                new ForEachStatementInfo(gae3, moveNextMethod: null, currentProperty: null, disp3, e3, conv3, conv3, wfna3, tgn3),
                new ForEachStatementInfo(gae3, moveNextMethod: null, currentProperty: null, disp3, e3, conv3, conv3, wfna4, tgn3));
            EqualityTesting.AssertNotEqual(
                new ForEachStatementInfo(gae3, moveNextMethod: null, currentProperty: null, disp3, e3, conv3, conv3, wfna3, tgn3),
                new ForEachStatementInfo(gae3, moveNextMethod: null, currentProperty: null, disp3, e3, conv3, conv3, wfna3, tgn4));
        }
    }
}
