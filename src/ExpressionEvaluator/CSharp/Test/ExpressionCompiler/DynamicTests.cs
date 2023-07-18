// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DynamicTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void Local_Simple()
        {
            var source =
@"class C
{
    static void M()
    {
        dynamic d = 1;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, method.ReturnType.TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x01);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (object V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void Local_Array()
        {
            var source =
@"class C
{
    static void M()
    {
        dynamic[] d = new dynamic[1];
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, ((ArrayTypeSymbol)method.ReturnType).ElementType.TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x02);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (dynamic[] V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void Local_Generic()
        {
            var source =
@"class C
{
    static void M()
    {
        System.Collections.Generic.List<dynamic> d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, ((NamedTypeSymbol)method.ReturnType).TypeArguments().Single().TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x02);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (System.Collections.Generic.List<dynamic> V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void LocalConstant_Simple()
        {
            var source =
@"class C
{   
    static void M()
    {
        const dynamic d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, method.ReturnType.TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x01);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void LocalConstant_Array()
        {
            var source =
@"class C
{
    static void M()
    {
        const dynamic[] d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, ((ArrayTypeSymbol)method.ReturnType).ElementType.TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x02);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void LocalConstant_Generic()
        {
            var source =
@"class C
{
    static void M()
    {
        const Generic<dynamic> d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}

class Generic<T>
{
}
";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, ((NamedTypeSymbol)method.ReturnType).TypeArguments().Single().TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x02);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4106")]
        public void LocalDuplicateConstantAndNonConstantDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
#line 799
            dynamic a = null;
            const dynamic b = null;
        }
        {
            const dynamic[] a = null;
#line 899
            dynamic[] b = null;
        }
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);

                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[0], "a", 0x01);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[0], "a", null); // Dynamic info ignored because ambiguous.
                }

                VerifyCustomTypeInfo(locals[1], "b", 0x01);
                locals.Free();

                context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);

                VerifyCustomTypeInfo(locals[0], "b", 0x02);

                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[1], "a", 0x02);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
                }

                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4106")]
        public void LocalDuplicateConstantAndNonConstantNonDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
#line 799
            object a = null;
            const dynamic b = null;
        }
        {
            const dynamic[] a = null;
#line 899
            object[] b = null;
        }
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);
                VerifyCustomTypeInfo(locals[0], "a", null);
                VerifyCustomTypeInfo(locals[1], "b", 0x01);
                locals.Free();

                context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);

                VerifyCustomTypeInfo(locals[0], "b", null);
                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[1], "a", 0x02);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
                }

                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4106")]
        public void LocalDuplicateConstantAndConstantDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
            const dynamic a = null;
            const dynamic b = null;
#line 799
            object e = null;
        }
        {
            const dynamic[] a = null;
            const dynamic[] c = null;
#line 899
            object[] e = null;
        }
        {
#line 999
            object e = null;
            const dynamic a = null;
            const dynamic c = null;
        }
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);
                VerifyCustomTypeInfo(locals[0], "e", null);

                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[1], "a", 0x01);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
                }

                VerifyCustomTypeInfo(locals[2], "b", 0x01);
                locals.Free();

                context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);
                VerifyCustomTypeInfo(locals[0], "e", null);

                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[1], "a", 0x02);
                    VerifyCustomTypeInfo(locals[2], "c", 0x02);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
                    VerifyCustomTypeInfo(locals[2], "c", null); // Dynamic info ignored because ambiguous.
                }

                locals.Free();

                context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 999);
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);

                VerifyCustomTypeInfo(locals[0], "e", null);

                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[1], "a", 0x01);
                    VerifyCustomTypeInfo(locals[2], "c", 0x01);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
                    VerifyCustomTypeInfo(locals[2], "c", null); // Dynamic info ignored because ambiguous.
                }

                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4106")]
        public void LocalDuplicateConstantAndConstantNonDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
            const dynamic a = null;
            const object c = null;
#line 799
            object e = null;
        }
        {
            const dynamic[] b = null;
#line 899
            object[] e = null;
        }
        {
            const object[] a = null;
#line 999
            object e = null;
            const dynamic[] c = null;
        }
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);
                VerifyCustomTypeInfo(locals[0], "e", null);
                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[1], "a", 0x01);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
                }
                VerifyCustomTypeInfo(locals[2], "c", null);
                locals.Free();

                context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);
                VerifyCustomTypeInfo(locals[0], "e", null);
                VerifyCustomTypeInfo(locals[1], "b", 0x02);
                locals.Free();

                context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 999);
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);
                VerifyCustomTypeInfo(locals[0], "e", null);
                VerifyCustomTypeInfo(locals[1], "a", null);
                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[2], "c", 0x02);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[2], "c", null); // Dynamic info ignored because ambiguous.
                }
                locals.Free();
            });
        }

        [Fact]
        public void LocalsWithLongAndShortNames()
        {
            var source =
@"class C
{
	static void M()
	{
        const dynamic a123456789012345678901234567890123456789012345678901234567890123 = null; // 64 chars
        const dynamic b = null;
        dynamic c123456789012345678901234567890123456789012345678901234567890123 = null; // 64 chars
        dynamic d = null;
	}

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(4, locals.Count);

                if (runtime.DebugFormat == DebugInformationFormat.PortablePdb)
                {
                    VerifyCustomTypeInfo(locals[0], "c123456789012345678901234567890123456789012345678901234567890123", 0x01);
                    VerifyCustomTypeInfo(locals[2], "a123456789012345678901234567890123456789012345678901234567890123", 0x01);
                }
                else
                {
                    VerifyCustomTypeInfo(locals[0], "c123456789012345678901234567890123456789012345678901234567890123", null); // dynamic info dropped
                    VerifyCustomTypeInfo(locals[2], "a123456789012345678901234567890123456789012345678901234567890123", null); // dynamic info dropped
                }

                VerifyCustomTypeInfo(locals[1], "d", 0x01);
                VerifyCustomTypeInfo(locals[3], "b", 0x01);
                locals.Free();
            });
        }

        [Fact]
        public void Parameter_Simple()
        {
            var source =
@"class C
{
    static void M(dynamic d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, method.ReturnType.TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x01);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void Parameter_Array()
        {
            var source =
@"class C
{
    static void M(dynamic[] d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, ((ArrayTypeSymbol)method.ReturnType).ElementType.TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x02);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void Parameter_Generic()
        {
            var source =
@"class C
{
    static void M(System.Collections.Generic.List<dynamic> d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                Assert.Equal(TypeKind.Dynamic, ((NamedTypeSymbol)method.ReturnType).TypeArguments().Single().TypeKind);
                VerifyCustomTypeInfo(locals[0], "d", 0x02);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")]
        public void ComplexDynamicType()
        {
            var source =
@"class C
{
    static void M(Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic> d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}

public class Outer<T, U>
{
    public class Inner<V, W>
    {
    }
}
";
            var comp = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: true);
                VerifyCustomTypeInfo(locals[0], "d", 0x04, 0x03);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                string error;
                var result = context.CompileExpression("d", out error);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, 0x04, 0x03);

                // Note that the method produced by CompileAssignment returns void
                // so there is never custom type info.
                result = context.CompileAssignment("d", "d", out error);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, null);

                ResultProperties resultProperties;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                testData = new CompilationTestData();
                result = context.CompileExpression(
                    "var dd = d;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, null);
                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       60 (0x3c)
  .maxstack  6
  IL_0000:  ldtoken    ""Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""dd""
  IL_000f:  ldstr      ""108766ce-df68-46ee-b761-0dcb7ac805f1""
  IL_0014:  newobj     ""System.Guid..ctor(string)""
  IL_0019:  ldc.i4.3
  IL_001a:  newarr     ""byte""
  IL_001f:  dup
  IL_0020:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.D7FC9689723FC6A41ADF105022720FF986ABA464083E7F71C6B921F8164E8878""
  IL_0025:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_002a:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_002f:  ldstr      ""dd""
  IL_0034:  call       ""Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic> Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic>>(string)""
  IL_0039:  ldarg.0
  IL_003a:  stind.ref
  IL_003b:  ret
}");
                locals.Free();
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void DynamicAliases()
        {
            var source =
@"class C
{
    static void M()
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(
                    runtime,
                    "C.M");
                var aliases = ImmutableArray.Create(
                    Alias(
                        DkmClrAliasKind.Variable,
                        "d1",
                        "d1",
                        typeof(object).AssemblyQualifiedName,
                        MakeCustomTypeInfo(true)),
                    Alias(
                        DkmClrAliasKind.Variable,
                        "d2",
                        "d2",
                        typeof(Dictionary<Dictionary<object, Dictionary<object[], object[]>>, object>).AssemblyQualifiedName,
                        MakeCustomTypeInfo(false, false, true, false, false, false, false, true, false)));
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var diagnostics = DiagnosticBag.GetInstance();

                var testData = new CompilationTestData();
                context.CompileGetLocals(
                    locals,
                    argumentsOnly: false,
                    aliases: aliases,
                    diagnostics: diagnostics,
                    typeName: out typeName,
                    testData: testData);
                diagnostics.Free();
                Assert.Equal(2, locals.Count);

                VerifyCustomTypeInfo(locals[0], "d1", 0x01);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d1", expectedILOpt:
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""d1""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  ret
}");

                VerifyCustomTypeInfo(locals[1], "d2", 0x84, 0x00); // Note: read flags right-to-left in each byte: 0010 0001 0(000 0000)
                VerifyLocal(testData, typeName, locals[1], "<>m1", "d2", expectedILOpt:
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""d2""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""System.Collections.Generic.Dictionary<System.Collections.Generic.Dictionary<dynamic, System.Collections.Generic.Dictionary<object[], dynamic[]>>, object>""
  IL_000f:  ret
}");
                locals.Free();
            });
        }

        private static ReadOnlyCollection<byte> MakeCustomTypeInfo(params bool[] flags)
        {
            Assert.NotNull(flags);
            var builder = ArrayBuilder<bool>.GetInstance();
            builder.AddRange(flags);
            var bytes = DynamicFlagsCustomTypeInfo.ToBytes(builder);
            builder.Free();
            return CustomTypeInfo.Encode(bytes, null);
        }

        [Fact]
        public void DynamicAttribute_NotAvailable()
        {
            var source =
@"class C
{
    static void M()
    {
        dynamic d = 1;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.DynamicAttribute, expected: false);
                VerifyCustomTypeInfo(locals[0], "d", null);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (object V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void DynamicCall()
        {
            var source = @"
class C
{
    void M()
    {
        dynamic d = this;
        d.M();
    }
}
";
            var comp = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                var result = context.CompileExpression("d.M()", out error, testData);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, 0x01);
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(TypeKind.Dynamic, ((MethodSymbol)methodData.Method).ReturnType.TypeKind);
                methodData.VerifyIL(@"
{
  // Code size       77 (0x4d)
  .maxstack  9
  .locals init (object V_0) //d
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0037
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""M""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.1
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0032:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_003c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0041:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0046:  ldloc.0
  IL_0047:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_004c:  ret
}
");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1160855")]
        public void AwaitDynamic()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class C
{
    dynamic d;

    void M(int p)
    {
        d.Test(); // Force reference to runtime binder.
    }

    static void G(Func<Task<object>> f)
    {
    }
}
";
            var comp = CreateCompilationWithCSharp(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                var result = context.CompileExpression("G(async () => await d())", out error, testData);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, null);
                var methodData = testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()");
                methodData.VerifyIL(@"
{
  // Code size      542 (0x21e)
  .maxstack  10
  .locals init (int V_0,
                <>x.<>c__DisplayClass0_0 V_1,
                object V_2,
                object V_3,
                System.Runtime.CompilerServices.ICriticalNotifyCompletion V_4,
                System.Runtime.CompilerServices.INotifyCompletion V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""<>x.<>c__DisplayClass0_0 <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>4__this""
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldloc.0
    IL_000f:  brfalse    IL_018a
    IL_0014:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_0019:  brtrue.s   IL_004b
    IL_001b:  ldc.i4.0
    IL_001c:  ldstr      ""GetAwaiter""
    IL_0021:  ldnull
    IL_0022:  ldtoken    ""C""
    IL_0027:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_002c:  ldc.i4.1
    IL_002d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0032:  dup
    IL_0033:  ldc.i4.0
    IL_0034:  ldc.i4.0
    IL_0035:  ldnull
    IL_0036:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_003b:  stelem.ref
    IL_003c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0041:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0046:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_004b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_0050:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_0055:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_005a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_005f:  brtrue.s   IL_008b
    IL_0061:  ldc.i4.0
    IL_0062:  ldtoken    ""C""
    IL_0067:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_006c:  ldc.i4.1
    IL_006d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0072:  dup
    IL_0073:  ldc.i4.0
    IL_0074:  ldc.i4.0
    IL_0075:  ldnull
    IL_0076:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_007b:  stelem.ref
    IL_007c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0081:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0086:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_008b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_0090:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_0095:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_009a:  ldloc.1
    IL_009b:  ldfld      ""C <>x.<>c__DisplayClass0_0.<>4__this""
    IL_00a0:  ldfld      ""dynamic C.d""
    IL_00a5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_00aa:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_00af:  stloc.3
    IL_00b0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00b5:  brtrue.s   IL_00dc
    IL_00b7:  ldc.i4.s   16
    IL_00b9:  ldtoken    ""bool""
    IL_00be:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00c3:  ldtoken    ""C""
    IL_00c8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00cd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
    IL_00d2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_00d7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00e1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
    IL_00e6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_00f0:  brtrue.s   IL_0121
    IL_00f2:  ldc.i4.0
    IL_00f3:  ldstr      ""IsCompleted""
    IL_00f8:  ldtoken    ""C""
    IL_00fd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0102:  ldc.i4.1
    IL_0103:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0108:  dup
    IL_0109:  ldc.i4.0
    IL_010a:  ldc.i4.0
    IL_010b:  ldnull
    IL_010c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0111:  stelem.ref
    IL_0112:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0117:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_011c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_0121:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_0126:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_012b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_0130:  ldloc.3
    IL_0131:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_0136:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_013b:  brtrue.s   IL_01a1
    IL_013d:  ldarg.0
    IL_013e:  ldc.i4.0
    IL_013f:  dup
    IL_0140:  stloc.0
    IL_0141:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
    IL_0146:  ldarg.0
    IL_0147:  ldloc.3
    IL_0148:  stfld      ""object <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>u__1""
    IL_014d:  ldloc.3
    IL_014e:  isinst     ""System.Runtime.CompilerServices.ICriticalNotifyCompletion""
    IL_0153:  stloc.s    V_4
    IL_0155:  ldloc.s    V_4
    IL_0157:  brtrue.s   IL_0174
    IL_0159:  ldloc.3
    IL_015a:  castclass  ""System.Runtime.CompilerServices.INotifyCompletion""
    IL_015f:  stloc.s    V_5
    IL_0161:  ldarg.0
    IL_0162:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
    IL_0167:  ldloca.s   V_5
    IL_0169:  ldarg.0
    IL_016a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitOnCompleted<System.Runtime.CompilerServices.INotifyCompletion, <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d>(ref System.Runtime.CompilerServices.INotifyCompletion, ref <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d)""
    IL_016f:  ldnull
    IL_0170:  stloc.s    V_5
    IL_0172:  br.s       IL_0182
    IL_0174:  ldarg.0
    IL_0175:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
    IL_017a:  ldloca.s   V_4
    IL_017c:  ldarg.0
    IL_017d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ICriticalNotifyCompletion, <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d>(ref System.Runtime.CompilerServices.ICriticalNotifyCompletion, ref <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d)""
    IL_0182:  ldnull
    IL_0183:  stloc.s    V_4
    IL_0185:  leave      IL_021d
    IL_018a:  ldarg.0
    IL_018b:  ldfld      ""object <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>u__1""
    IL_0190:  stloc.3
    IL_0191:  ldarg.0
    IL_0192:  ldnull
    IL_0193:  stfld      ""object <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>u__1""
    IL_0198:  ldarg.0
    IL_0199:  ldc.i4.m1
    IL_019a:  dup
    IL_019b:  stloc.0
    IL_019c:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
    IL_01a1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01a6:  brtrue.s   IL_01d8
    IL_01a8:  ldc.i4.0
    IL_01a9:  ldstr      ""GetResult""
    IL_01ae:  ldnull
    IL_01af:  ldtoken    ""C""
    IL_01b4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_01b9:  ldc.i4.1
    IL_01ba:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_01bf:  dup
    IL_01c0:  ldc.i4.0
    IL_01c1:  ldc.i4.0
    IL_01c2:  ldnull
    IL_01c3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_01c8:  stelem.ref
    IL_01c9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_01ce:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_01d3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01dd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_01e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01e7:  ldloc.3
    IL_01e8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_01ed:  stloc.2
    IL_01ee:  leave.s    IL_0209
  }
  catch System.Exception
  {
    IL_01f0:  stloc.s    V_6
    IL_01f2:  ldarg.0
    IL_01f3:  ldc.i4.s   -2
    IL_01f5:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
    IL_01fa:  ldarg.0
    IL_01fb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
    IL_0200:  ldloc.s    V_6
    IL_0202:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_0207:  leave.s    IL_021d
  }
  IL_0209:  ldarg.0
  IL_020a:  ldc.i4.s   -2
  IL_020c:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
  IL_0211:  ldarg.0
  IL_0212:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
  IL_0217:  ldloc.2
  IL_0218:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_021d:  ret
}
");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072296")]
        public void InvokeStaticMemberInLambda()
        {
            var source = @"
class C
{
    static dynamic x;

    static void Goo(dynamic y)
    {
        System.Action a = () => Goo(x);
    }
}
";
            var comp = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Goo");
                var testData = new CompilationTestData();
                string error;
                var result = context.CompileAssignment("a", "() => Goo(x)", out error, testData);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, null);
                testData.GetMethodData("<>x.<>c.<<>m0>b__0_0").VerifyIL(@"
{
  // Code size      106 (0x6a)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""Goo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_0055:  ldtoken    ""<>x""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldsfld     ""dynamic C.x""
  IL_0064:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0069:  ret
}");

                context = CreateMethodContext(runtime, "C.<>c.<Goo>b__1_0");
                testData = new CompilationTestData();
                result = context.CompileExpression("Goo(x)", out error, testData);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, 0x01);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(@"
{
  // Code size      102 (0x66)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0042
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""Goo""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.2
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.s   33
  IL_0022:  ldnull
  IL_0023:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0028:  stelem.ref
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldc.i4.0
  IL_002c:  ldnull
  IL_002d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0032:  stelem.ref
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0038:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0047:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_004c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0051:  ldtoken    ""<>x""
  IL_0056:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005b:  ldsfld     ""dynamic C.x""
  IL_0060:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0065:  ret
}");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1095613")]
        public void HoistedLocalsLoseDynamicAttribute()
        {
            var source = @"
class C
{
    static void M(dynamic x)
    {
        dynamic y = 3;
        System.Func<dynamic> a = () => x + y;
    }

    static void Goo(int x)
    {
        M(x);
    }
}
";
            var comp = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                var result = context.CompileExpression("Goo(x)", out error, testData);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, 0x01);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size      103 (0x67)
  .maxstack  9
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<dynamic> V_1) //a
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0042
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""Goo""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.2
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.s   33
  IL_0022:  ldnull
  IL_0023:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0028:  stelem.ref
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldc.i4.0
  IL_002c:  ldnull
  IL_002d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0032:  stelem.ref
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0038:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0047:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_004c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0051:  ldtoken    ""<>x""
  IL_0056:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005b:  ldloc.0
  IL_005c:  ldfld      ""dynamic C.<>c__DisplayClass0_0.x""
  IL_0061:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0066:  ret
}");
                testData = new CompilationTestData();
                result = context.CompileExpression("Goo(y)", out error, testData);
                Assert.Null(error);
                VerifyCustomTypeInfo(result, 0x01);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size      103 (0x67)
  .maxstack  9
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<dynamic> V_1) //a
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0042
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""Goo""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.2
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.s   33
  IL_0022:  ldnull
  IL_0023:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0028:  stelem.ref
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldc.i4.0
  IL_002c:  ldnull
  IL_002d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0032:  stelem.ref
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0038:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0047:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_004c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0051:  ldtoken    ""<>x""
  IL_0056:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005b:  ldloc.0
  IL_005c:  ldfld      ""dynamic C.<>c__DisplayClass0_0.y""
  IL_0061:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0066:  ret
}");
            });
        }

        private static void VerifyCustomTypeInfo(LocalAndMethod localAndMethod, string expectedName, params byte[] expectedBytes)
        {
            Assert.Equal(localAndMethod.LocalName, expectedName);
            ReadOnlyCollection<byte> customTypeInfo;
            Guid customTypeInfoId = localAndMethod.GetCustomTypeInfo(out customTypeInfo);
            VerifyCustomTypeInfo(customTypeInfoId, customTypeInfo, expectedBytes);
        }

        private static void VerifyCustomTypeInfo(CompileResult compileResult, params byte[] expectedBytes)
        {
            ReadOnlyCollection<byte> customTypeInfo;
            Guid customTypeInfoId = compileResult.GetCustomTypeInfo(out customTypeInfo);
            VerifyCustomTypeInfo(customTypeInfoId, customTypeInfo, expectedBytes);
        }

        private static void VerifyCustomTypeInfo(Guid customTypeInfoId, ReadOnlyCollection<byte> customTypeInfo, params byte[] expectedBytes)
        {
            if (expectedBytes == null)
            {
                Assert.Equal(Guid.Empty, customTypeInfoId);
                Assert.Null(customTypeInfo);
            }
            else
            {
                Assert.Equal(CustomTypeInfo.PayloadTypeId, customTypeInfoId);
                // Include leading count byte.
                var builder = ArrayBuilder<byte>.GetInstance();
                builder.Add((byte)expectedBytes.Length);
                builder.AddRange(expectedBytes);
                expectedBytes = builder.ToArrayAndFree();
                Assert.Equal(expectedBytes, customTypeInfo);
            }
        }
    }
}
