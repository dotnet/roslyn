// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.TupleElementNames;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics
{
    public partial class TupleElementNamesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace) =>
            (new CSharpTupleElementNameDiagnosticAnalyzer(), new TupleElementNamesCodeFixProvider());

        [Fact]
        public async Task ImplementTupleInterfaceWithValueTuple1()
        {
            string source = @"
public interface I
{
    (int i1, int i2) M((string, string) a);
}

class C : I
{
    public System.ValueTuple<int, int> [|M|](System.ValueTuple<string, string> a)
    {
        return (1, 2);
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs;
            string fixedSource = @"
public interface I
{
    (int i1, int i2) M((string, string) a);
}

class C : I
{
    public (int i1, int i2) M(System.ValueTuple<string, string> a)
    {
        return (1, 2);
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs;

            await TestInRegularAndScriptAsync(source, fixedSource);
        }

        [Fact]
        public async Task ImplementTupleInterfaceWithValueTuple2()
        {
            string source = @"
public interface I
{
    (int i1, int i2) M((string, string) a);
}

class C : I
{
    public (int, int) [|M|]((string, string) a)
    {
        return (1, 2);
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs;
            string fixedSource = @"
public interface I
{
    (int i1, int i2) M((string, string) a);
}

class C : I
{
    public (int i1, int i2) M((string, string) a)
    {
        return (1, 2);
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs;

            await TestInRegularAndScriptAsync(source, fixedSource);
        }
    }
}
