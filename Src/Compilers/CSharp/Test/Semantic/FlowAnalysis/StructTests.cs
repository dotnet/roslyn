// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class StructTests : FlowTestBase
    {
        [Fact]
        public void SelfDefaultConstructor()
        {
            string program = @"
struct S
{
    public int x, y;
    public S(int x, int y) : this() { this.x = x; this.y = y; }
}
";
            var comp = CreateCompilationWithMscorlib(program);
            comp.VerifyDiagnostics();

            var structType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
            var constructors = structType.GetMembers(WellKnownMemberNames.InstanceConstructorName);
            Assert.Equal(2, constructors.Length);

            var sourceConstructor = (MethodSymbol)constructors.First(c => !c.IsImplicitlyDeclared);
            var synthesizedConstructor = (MethodSymbol)constructors.First(c => c.IsImplicitlyDeclared);
            Assert.NotEqual(sourceConstructor, synthesizedConstructor);

            Assert.Equal(2, sourceConstructor.Parameters.Length);
            Assert.Equal(0, synthesizedConstructor.Parameters.Length);
        }

        [Fact, WorkItem(543133, "DevDiv")]
        public void FieldAssignedAndReferenced()
        {
            var text =
@"using System;
 
class Program
{
    static P p;
 
    static void Main(string[] args)
    {
        p.X = 5;
        Console.WriteLine(p.X);
    }
}
 
struct P { public int X; }
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestEmptyStructs()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowStatements(
@"struct S { }

class C
{
    public static void Main(string[] args)
    {
        S s1;
        S s2;
        S s3;
        /*<bind>*/
        S s4 = s2;
        s3 = s4;
        /*</bind>*/
        S s6 = s1;
        S s5 = s4;
    }
}");
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
        }

        [Fact]
        [WorkItem(545509, "DevDiv")]
        public void StructIndexerReceiver()
        {
            string program = @"
struct SafeBitVector
{
    private int _data;
    internal bool this[int bit]
    {
        get
        {
            return (_data & bit) == bit;
        }
        set
        {
            _data = value ? _data | bit : _data & ~bit;
        }
    }
    internal bool Foo(int bit)
    {
        return this[bit];
    }
}

class SectionInformation
{
    SafeBitVector _flags;
    internal bool Foo(int x)
    {
        return _flags[x];
    }
}

class SectionInformation2
{
    SafeBitVector _flags;
    internal bool Foo(int x)
    {
        return _flags.Foo(x);
    }
}";
            var comp = CreateCompilationWithMscorlib(program);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(545710, "DevDiv")]
        public void StructFieldWithAssignedPropertyMembers()
        {
            string program = @"
public struct PointF
{
    private float x; private float y;
    public PointF(float x, float y) { this.x = x; this.y = y; }

    public float X { get { return x; } set { x = value; } }
    public float Y { get { return y; } set { y = value; } }
    public void M() {}
}
class GraphicsContext
{
    private PointF transformOffset;
    public GraphicsContext()
    {
        this.transformOffset.X = 0; this.transformOffset.Y = 0;
    }
    public PointF TransformOffset { get { return this.transformOffset; } }
}";
            var comp = CreateCompilationWithMscorlib(program);
            comp.VerifyDiagnostics();
        }
    }
}
