// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class ForEachStatementInfoTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var c = (Compilation)CreateCompilation(@"
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
            var e1 = (ITypeSymbol)c.GlobalNamespace.GetMembers("E1").Single();
            var ge1 = (IMethodSymbol)e1.GetMembers("GetEnumerator").Single();
            var mn1 = (IMethodSymbol)e1.GetMembers("MoveNext").Single();
            var cur1 = (IPropertySymbol)e1.GetMembers("Current").Single();
            var disp1 = (IMethodSymbol)e1.GetMembers("Dispose").Single();
            var dispArgs1 = ImmutableArray<IArgumentOperation>.Empty;
            var mnArgs1 = ImmutableArray<IArgumentOperation>.Empty;
            var geArgs1 = ImmutableArray<IArgumentOperation>.Empty;
            var conv1 = Conversion.Identity;

            var e2 = (ITypeSymbol)c.GlobalNamespace.GetMembers("E2").Single();
            var ge2 = (IMethodSymbol)e2.GetMembers("GetEnumerator").Single();
            var mn2 = (IMethodSymbol)e2.GetMembers("MoveNext").Single();
            var cur2 = (IPropertySymbol)e2.GetMembers("Current").Single();
            var disp2 = (IMethodSymbol)e2.GetMembers("Dispose").Single();
            var conv2 = Conversion.NoConversion;

            EqualityTesting.AssertEqual(default(ForEachStatementInfo), default(ForEachStatementInfo));
            EqualityTesting.AssertEqual(new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge2, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn2, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur2, disp1, dispArgs1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp2, dispArgs1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv2, conv1), new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv2), new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: false, ge1, geArgs1, mn1, mnArgs1, cur1, disp1, dispArgs1, e1, conv1, conv1));
        }
    }
}
