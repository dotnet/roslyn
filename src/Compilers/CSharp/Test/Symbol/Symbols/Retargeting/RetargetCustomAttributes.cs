// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Retargeting
{
    public class RetargetCustomAttributes : CSharpTestBase
    {
        internal class Test01
        {
            public CSharpCompilationReference c1, c2;
            public AssemblySymbol c1MscorLibAssemblyRef, c2MscorlibAssemblyRef;
            public NamedTypeSymbol oldMsCorLib_debuggerTypeProxyAttributeType, newMsCorLib_debuggerTypeProxyAttributeType;
            public MethodSymbol oldMsCorLib_debuggerTypeProxyAttributeCtor, newMsCorLib_debuggerTypeProxyAttributeCtor;
            public NamedTypeSymbol oldMsCorLib_systemType, newMsCorLib_systemType;

            private static readonly AttributeDescription s_attribute = new AttributeDescription(
                "System.Diagnostics",
                "DebuggerTypeProxyAttribute",
                new byte[][] { new byte[] { (byte)SignatureAttributes.Instance, 1, (byte)SignatureTypeCode.Void, (byte)SignatureTypeCode.TypeHandle, (byte)AttributeDescription.TypeHandleTarget.SystemType } });

            public Test01()
            {
                string source = @"
using System.Diagnostics;

[assembly: DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]
[module: DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]


[DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]
class TestClass
{
    [DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]
    public int testField;


    [DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]
    public int TestProperty
    {
        [return: DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]
        get
        {
            return testField;
        }
    }

    [return: DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]
    [DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )]
    public T TestMethod<[DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )] T>
        ([DebuggerTypeProxyAttribute(typeof(System.Type), Target = typeof(int[]), TargetTypeName = ""IntArrayType"" )] T testParameter)
    {
        return testParameter;
    }
}";
                var compilation1 = CSharpCompilation.Create("C1", new[] { Parse(source) }, new[] { OldMsCorLib }, TestOptions.ReleaseDll);
                c1 = new CSharpCompilationReference(compilation1);

                var c1Assembly = compilation1.Assembly;

                var compilation2 = CSharpCompilation.Create("C2", references: new MetadataReference[] { NewMsCorLib, c1 });
                c2 = new CSharpCompilationReference(compilation2);

                var c1AsmRef = compilation2.GetReferencedAssemblySymbol(c1);
                Assert.NotSame(c1Assembly, c1AsmRef);

                c1MscorLibAssemblyRef = compilation1.GetReferencedAssemblySymbol(OldMsCorLib);
                c2MscorlibAssemblyRef = compilation2.GetReferencedAssemblySymbol(NewMsCorLib);
                Assert.NotSame(c1MscorLibAssemblyRef, c2MscorlibAssemblyRef);

                oldMsCorLib_systemType = c1MscorLibAssemblyRef.GetTypeByMetadataName("System.Type");
                newMsCorLib_systemType = c2MscorlibAssemblyRef.GetTypeByMetadataName("System.Type");
                Assert.NotSame(oldMsCorLib_systemType, newMsCorLib_systemType);

                oldMsCorLib_debuggerTypeProxyAttributeType = c1MscorLibAssemblyRef.GetTypeByMetadataName("System.Diagnostics.DebuggerTypeProxyAttribute");
                newMsCorLib_debuggerTypeProxyAttributeType = c2MscorlibAssemblyRef.GetTypeByMetadataName("System.Diagnostics.DebuggerTypeProxyAttribute");
                Assert.NotSame(oldMsCorLib_debuggerTypeProxyAttributeType, newMsCorLib_debuggerTypeProxyAttributeType);

                oldMsCorLib_debuggerTypeProxyAttributeCtor = (MethodSymbol)oldMsCorLib_debuggerTypeProxyAttributeType.GetMembers(".ctor").Single(
                    m => ((MethodSymbol)m).ParameterCount == 1 && TypeSymbol.Equals(((MethodSymbol)m).GetParameterType(0), oldMsCorLib_systemType, TypeCompareKind.ConsiderEverything2));

                newMsCorLib_debuggerTypeProxyAttributeCtor = (MethodSymbol)newMsCorLib_debuggerTypeProxyAttributeType.GetMembers(".ctor").Single(
                    m => ((MethodSymbol)m).ParameterCount == 1 && TypeSymbol.Equals(((MethodSymbol)m).GetParameterType(0), newMsCorLib_systemType, TypeCompareKind.ConsiderEverything2));

                Assert.NotSame(oldMsCorLib_debuggerTypeProxyAttributeCtor, newMsCorLib_debuggerTypeProxyAttributeCtor);
            }

            public void TestAttributeRetargeting(Symbol symbol)
            {
                // Verify GetAttributes()
                TestAttributeRetargeting(symbol.GetAttributes());

                // Verify GetAttributes(AttributeType from Retargeted assembly)
                TestAttributeRetargeting(symbol.GetAttributes(newMsCorLib_debuggerTypeProxyAttributeType));

                // Verify GetAttributes(AttributeType from Underlying assembly)
                Assert.Empty(symbol.GetAttributes(oldMsCorLib_debuggerTypeProxyAttributeType));

                // Verify GetAttributes(AttributeCtor from Retargeted assembly)
                TestAttributeRetargeting(symbol.GetAttributes(newMsCorLib_debuggerTypeProxyAttributeType));

                // Verify GetAttributes(AttributeCtor from Underlying assembly)
                Assert.Empty(symbol.GetAttributes(oldMsCorLib_debuggerTypeProxyAttributeType));

                // Verify GetAttributes(namespaceName, typeName, ctorSignature)
                TestAttributeRetargeting(symbol.GetAttributes(s_attribute));
            }

            public void TestAttributeRetargeting_ReturnTypeAttributes(MethodSymbol symbol)
            {
                // Verify GetReturnTypeAttributes()
                TestAttributeRetargeting(symbol.GetReturnTypeAttributes());

                // Verify GetReturnTypeAttributes(AttributeType from Retargeted assembly)
                TestAttributeRetargeting(symbol.GetReturnTypeAttributes().Where(a => TypeSymbol.Equals(a.AttributeClass, newMsCorLib_debuggerTypeProxyAttributeType, TypeCompareKind.ConsiderEverything2)));

                // Verify GetReturnTypeAttributes(AttributeType from Underlying assembly) returns nothing. Shouldn't match to old attr type
                Assert.Empty(symbol.GetReturnTypeAttributes().Where(a => TypeSymbol.Equals(a.AttributeClass, oldMsCorLib_debuggerTypeProxyAttributeType, TypeCompareKind.ConsiderEverything2)));

                // Verify GetReturnTypeAttributes(AttributeCtor from Retargeted assembly)
                TestAttributeRetargeting(symbol.GetReturnTypeAttributes().Where(a => a.AttributeConstructor == newMsCorLib_debuggerTypeProxyAttributeCtor));

                // Verify GetReturnTypeAttributes(AttributeCtor from Underlying assembly) returns nothing. Shouldn't match to old attr type.
                Assert.Empty(symbol.GetReturnTypeAttributes().Where(a => a.AttributeConstructor == oldMsCorLib_debuggerTypeProxyAttributeCtor));
            }

            private void TestAttributeRetargeting(IEnumerable<CSharpAttributeData> attributes)
            {
                Assert.Equal(1, attributes.Count());

                var attribute = attributes.First();
                Assert.IsType<RetargetingAttributeData>(attribute);

                Assert.Same(newMsCorLib_debuggerTypeProxyAttributeType, attribute.AttributeClass);
                Assert.Same(newMsCorLib_debuggerTypeProxyAttributeCtor, attribute.AttributeConstructor);
                Assert.Same(newMsCorLib_systemType, attribute.AttributeConstructor.GetParameterType(0));

                Assert.Equal(1, attribute.CommonConstructorArguments.Length);
                attribute.VerifyValue(0, TypedConstantKind.Type, newMsCorLib_systemType);

                Assert.Equal(2, attribute.CommonNamedArguments.Length);
                attribute.VerifyNamedArgumentValue<object>(0, "Target", TypedConstantKind.Type, typeof(int[]));
                attribute.VerifyNamedArgumentValue(1, "TargetTypeName", TypedConstantKind.Primitive, "IntArrayType");
            }
        }

        private static MetadataReference OldMsCorLib
        {
            get
            {
                return TestReferences.NetFx.v4_0_21006.mscorlib;
            }
        }

        private static MetadataReference NewMsCorLib
        {
            get
            {
                return TestReferences.NetFx.v4_0_30319.mscorlib;
            }
        }

        [Fact]
        public void Test01_AssemblyAttribute()
        {
            Test01 test = new Test01();
            var c1AsmRef = test.c2.Compilation.GetReferencedAssemblySymbol(test.c1);
            Assert.IsType<RetargetingAssemblySymbol>(c1AsmRef);
            test.TestAttributeRetargeting(c1AsmRef);
        }

        [Fact]
        public void Test01_ModuleAttribute()
        {
            Test01 test = new Test01();
            var c1AsmRef = test.c2.Compilation.GetReferencedAssemblySymbol(test.c1);
            var c1ModuleSym = c1AsmRef.Modules[0];
            Assert.IsType<RetargetingModuleSymbol>(c1ModuleSym);
            test.TestAttributeRetargeting(c1ModuleSym);
        }

        [Fact]
        public void Test01_NamedTypeAttribute()
        {
            Test01 test = new Test01();
            var testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").Single();
            Assert.IsType<RetargetingNamedTypeSymbol>(testClass);
            test.TestAttributeRetargeting(testClass);
        }

        [Fact]
        public void Test01_FieldAttribute()
        {
            Test01 test = new Test01();
            var testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").Single();
            FieldSymbol testField = testClass.GetMembers("testField").OfType<FieldSymbol>().Single();
            Assert.IsType<RetargetingFieldSymbol>(testField);
            test.TestAttributeRetargeting(testField);
        }

        [Fact]
        public void Test01_PropertyAttribute()
        {
            Test01 test = new Test01();
            var testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").Single();
            PropertySymbol testProperty = testClass.GetMembers("TestProperty").OfType<PropertySymbol>().Single();
            Assert.IsType<RetargetingPropertySymbol>(testProperty);
            test.TestAttributeRetargeting(testProperty);

            MethodSymbol testMethod = testProperty.GetMethod;
            Assert.IsType<RetargetingMethodSymbol>(testMethod);
            test.TestAttributeRetargeting_ReturnTypeAttributes(testMethod);
        }

        [Fact]
        public void Test01_MethodAttribute()
        {
            Test01 test = new Test01();
            var testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").Single();
            MethodSymbol testMethod = testClass.GetMembers("TestMethod").OfType<MethodSymbol>().Single();
            Assert.IsType<RetargetingMethodSymbol>(testMethod);
            test.TestAttributeRetargeting(testMethod);
        }

        [Fact]
        public void Test01_TypeParameterAttribute()
        {
            Test01 test = new Test01();
            var testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").Single();
            MethodSymbol testMethod = testClass.GetMembers("TestMethod").OfType<MethodSymbol>().Single();
            Assert.IsType<RetargetingMethodSymbol>(testMethod);
            TypeParameterSymbol testTypeParameter = testMethod.TypeParameters[0];
            Assert.IsType<RetargetingTypeParameterSymbol>(testTypeParameter);
            test.TestAttributeRetargeting(testTypeParameter);
        }

        [Fact]
        public void Test01_ParameterAttribute()
        {
            Test01 test = new Test01();
            var testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").Single();
            MethodSymbol testMethod = testClass.GetMembers("TestMethod").OfType<MethodSymbol>().Single();
            Assert.IsType<RetargetingMethodSymbol>(testMethod);
            ParameterSymbol testParameter = testMethod.Parameters[0];
            Assert.IsType<RetargetingMethodParameterSymbol>(testParameter);
            test.TestAttributeRetargeting(testParameter);
        }

        [Fact]
        public void Test01_ReturnTypeAttribute()
        {
            Test01 test = new Test01();
            var testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").Single();
            MethodSymbol testMethod = testClass.GetMembers("TestMethod").OfType<MethodSymbol>().Single();
            Assert.IsType<RetargetingMethodSymbol>(testMethod);
            test.TestAttributeRetargeting_ReturnTypeAttributes(testMethod);
        }

        [Fact]
        [WorkItem(569089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569089"), WorkItem(575948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/575948")]
        public void NullArrays()
        {
            var source1 = @"
using System;

public class A : Attribute
{
    public A(object[] a, int[] b)
    {
    }

    public object[] P { get; set; }
    public int[] F;
}

[A(null, null, P = null, F = null)]
public class C
{
}
";

            var source2 = @"
";

            var c1 = CreateEmptyCompilation(source1, new[] { OldMsCorLib });
            var c2 = CreateEmptyCompilation(source2, new MetadataReference[] { NewMsCorLib, new CSharpCompilationReference(c1) });

            var c = c2.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.IsType<RetargetingNamedTypeSymbol>(c);

            var attr = c.GetAttributes().Single();
            var args = attr.ConstructorArguments.ToArray();

            Assert.True(args[0].IsNull);
            Assert.Equal("object[]", args[0].Type.ToDisplayString());
            Assert.Throws<InvalidOperationException>(() => args[0].Value);

            Assert.True(args[1].IsNull);
            Assert.Equal("int[]", args[1].Type.ToDisplayString());
            Assert.Throws<InvalidOperationException>(() => args[1].Value);

            var named = attr.NamedArguments.ToDictionary(e => e.Key, e => e.Value);

            Assert.True(named["P"].IsNull);
            Assert.Equal("object[]", named["P"].Type.ToDisplayString());
            Assert.Throws<InvalidOperationException>(() => named["P"].Value);

            Assert.True(named["F"].IsNull);
            Assert.Equal("int[]", named["F"].Type.ToDisplayString());
            Assert.Throws<InvalidOperationException>(() => named["F"].Value);
        }
    }
}
