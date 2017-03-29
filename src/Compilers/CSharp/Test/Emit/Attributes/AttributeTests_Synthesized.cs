// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Synthesized : WellKnownAttributesTestBase
    {
        #region CompilerGeneratedAttribute, DebuggerBrowsableAttribute, DebuggerStepThroughAttribute, DebuggerDisplayAttribute

        private static DebuggerBrowsableState GetDebuggerBrowsableState(ImmutableArray<SynthesizedAttributeData> attributes)
        {
            return (DebuggerBrowsableState)attributes.Single(a => a.AttributeClass.Name == "DebuggerBrowsableAttribute").ConstructorArguments.First().Value;
        }

        [Fact, WorkItem(546632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546632")]
        public void PrivateImplementationDetails()
        {
            string source = @"
class C
{
    int[] a = new[] { 1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9, };
}
";
            var reference = CreateCompilationWithMscorlib(source).EmitToImageReference();

            var comp = CreateCompilation("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));

            var pid = (NamedTypeSymbol)comp.GlobalNamespace.GetMembers().Where(s => s.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)).Single();

            var expectedAttrs = new[] { "CompilerGeneratedAttribute" };
            var actualAttrs = GetAttributeNames(pid.GetAttributes());

            AssertEx.SetEqual(expectedAttrs, actualAttrs);
        }

        [Fact, WorkItem(546958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546958")]
        public void FixedSizeBuffers()
        {
            string source = @"
unsafe struct S
{
    public fixed char C[5];
}
";
            var reference = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();
            var comp = CreateCompilation("", new[] { reference }, options: TestOptions.UnsafeReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));

            var s = (NamedTypeSymbol)comp.GlobalNamespace.GetMembers("S").Single();
            var bufferType = (NamedTypeSymbol)s.GetMembers().Where(t => t.Name == "<C>e__FixedBuffer").Single();

            var expectedAttrs = new[] { "CompilerGeneratedAttribute", "UnsafeValueTypeAttribute" };
            var actualAttrs = GetAttributeNames(bufferType.GetAttributes());

            AssertEx.SetEqual(expectedAttrs, actualAttrs);
        }

        [Fact, WorkItem(546927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546927")]
        public void BackingFields()
        {
            string source = @"
using System;

class Test
{
    public string MyProp { get; set; }
    public event Func<int> MyEvent;
}
";
            foreach (var options in new[] { TestOptions.DebugDll, TestOptions.ReleaseDll })
            {
                var comp = CreateCompilationWithMscorlib(source, options: options);

                var c = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var p = c.GetMember<PropertySymbol>("MyProp");
                var e = c.GetMember<EventSymbol>("MyEvent");

                var expectedAttrs =
                    options.OptimizationLevel == OptimizationLevel.Debug
                    ? new[] { "CompilerGeneratedAttribute", "DebuggerBrowsableAttribute" }
                    : new[] { "CompilerGeneratedAttribute" };

                var attrs = ((SourcePropertySymbol)p).BackingField.GetSynthesizedAttributes();
                AssertEx.SetEqual(expectedAttrs, GetAttributeNames(attrs));
                if (options.OptimizationLevel == OptimizationLevel.Debug)
                    Assert.Equal(DebuggerBrowsableState.Never, GetDebuggerBrowsableState(attrs));

                attrs = e.AssociatedField.GetSynthesizedAttributes();
                AssertEx.SetEqual(expectedAttrs, GetAttributeNames(attrs));
                if (options.OptimizationLevel == OptimizationLevel.Debug)
                    Assert.Equal(DebuggerBrowsableState.Never, GetDebuggerBrowsableState(attrs));
            }
        }

        [Fact, WorkItem(546927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546927")]
        public void Accessors()
        {
            string source = @"
using System;

abstract class C
{
    public int P { get; set; }
    public abstract int Q { get; set; }
    public event Func<int> E;
}
";
            foreach (var options in new[] { TestOptions.DebugDll, TestOptions.ReleaseDll })
            {
                var comp = CreateCompilationWithMscorlib(source, options: options);

                var c = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var p = c.GetMember<PropertySymbol>("P");
                // no attributes on abstract property accessors
                var q = c.GetMember<PropertySymbol>("Q");

                var e = c.GetMember<EventSymbol>("E");

                var expected = new[] { "CompilerGeneratedAttribute" };

                var attrs = p.GetMethod.GetSynthesizedAttributes();
                AssertEx.SetEqual(expected, GetAttributeNames(attrs));

                attrs = p.SetMethod.GetSynthesizedAttributes();
                AssertEx.SetEqual(expected, GetAttributeNames(attrs));

                attrs = q.GetMethod.GetSynthesizedAttributes();
                Assert.Equal(0, attrs.Length);

                attrs = q.SetMethod.GetSynthesizedAttributes();
                Assert.Equal(0, attrs.Length);

                attrs = e.AddMethod.GetSynthesizedAttributes();
                AssertEx.SetEqual(expected, GetAttributeNames(attrs));

                attrs = e.RemoveMethod.GetSynthesizedAttributes();
                AssertEx.SetEqual(expected, GetAttributeNames(attrs));
            }
        }

        [Fact]
        public void Lambdas()
        {
            string source = @"
using System;

class C
{
    void Foo()
    {
        int a = 1, b = 2;
        Func<int, int, int> d = (x, y) => a*x+b*y; 
    }
}
";
            foreach (var options in new[] { TestOptions.DebugDll, TestOptions.ReleaseDll })
            {
                var comp = CreateCompilationWithMscorlib(source, options: options);

                CompileAndVerify(comp, symbolValidator: m =>
                {
                    var displayClass = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>c__DisplayClass0_0");
                    AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(displayClass.GetAttributes()));

                    foreach (var member in displayClass.GetMembers())
                    {
                        Assert.Equal(0, member.GetAttributes().Length);
                    }
                });
            }
        }

        [Fact]
        public void AnonymousTypes()
        {
            string source = @"
class C
{
    void Foo()
    {
        var x = new { X = 1, Y = 2 };
    }
}
";
            foreach (var options in new[] { TestOptions.DebugDll, TestOptions.ReleaseDll })
            {
                var comp = CreateCompilationWithMscorlib(source, options: options);

                CompileAndVerify(comp, symbolValidator: m =>
                {
                    var anon = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousType0`2");

                    string[] expected;
                    if (options.OptimizationLevel == OptimizationLevel.Debug)
                    {
                        expected = new[] { "DebuggerDisplayAttribute", "CompilerGeneratedAttribute" };
                    }
                    else
                    {
                        expected = new[] { "CompilerGeneratedAttribute" };
                    }

                    AssertEx.SetEqual(expected, GetAttributeNames(anon.GetAttributes()));

                    foreach (var member in anon.GetMembers())
                    {
                        var actual = GetAttributeNames(member.GetAttributes());

                        switch (member.Name)
                        {
                            case "<X>i__Field":
                            case "<Y>i__Field":
                                expected = new[] { "DebuggerBrowsableAttribute" };
                                break;

                            case ".ctor":
                            case "Equals":
                            case "GetHashCode":
                            case "ToString":
                                expected = new[] { "DebuggerHiddenAttribute" };
                                break;

                            case "X":
                            case "get_X":
                            case "Y":
                            case "get_Y":
                                expected = new string[] { };
                                break;

                            default:
                                throw TestExceptionUtilities.UnexpectedValue(member.Name);
                        }

                        AssertEx.SetEqual(expected, actual);
                    }
                });
            }
        }

        [Fact]
        public void AnonymousTypes_DebuggerDisplay()
        {
            string source = @"
public class C
{
   public void Foo() 
   {
	  var _0 = new { };
	  var _1 = new { X0 = 1 };
	  var _2 = new { X0 = 1, X1 = 1 };
	  var _3 = new { X0 = 1, X1 = 1, X2 = 1 };
	  var _4 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1 };
	  var _5 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1 };
	  var _6 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1 };
	  var _7 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1 };
	  var _8 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1 };
      var _10 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1, X8 = 1 };   
      var _11 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1, X8 = 1, X9 = 1 }; 
      var _12 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1, X8 = 1, X9 = 1, X10 = 1 }; 
      var _13 = new { 
	     X10 = 1, X11 = 1, X12 = 1, X13 = 1, X14 = 1, X15 = 1, X16 = 1, X17 = 1,
	     X20 = 1, X21 = 1, X22 = 1, X23 = 1, X24 = 1, X25 = 1, X26 = 1, X27 = 1,
	     X30 = 1, X31 = 1, X32 = 1, X33 = 1, X34 = 1, X35 = 1, X36 = 1, X37 = 1,
	     X40 = 1, X41 = 1, X42 = 1, X43 = 1, X44 = 1, X45 = 1, X46 = 1, X47 = 1,
	     X50 = 1, X51 = 1, X52 = 1, X53 = 1, X54 = 1, X55 = 1, X56 = 1, X57 = 1,
	     X60 = 1, X61 = 1, X62 = 1, X63 = 1, X64 = 1, X65 = 1, X66 = 1, X67 = 1,
      };  
   }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            CompileAndVerify(comp, symbolValidator: m =>
            {
                var assembly = m.ContainingAssembly;
                Assert.Equal(@"\{ }", GetDebuggerDisplayString(assembly, 0, 0));
                Assert.Equal(@"\{ X0 = {X0} }", GetDebuggerDisplayString(assembly, 1, 1));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1} }", GetDebuggerDisplayString(assembly, 2, 2));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2} }", GetDebuggerDisplayString(assembly, 3, 3));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3} }", GetDebuggerDisplayString(assembly, 4, 4));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4} }", GetDebuggerDisplayString(assembly, 5, 5));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5} }", GetDebuggerDisplayString(assembly, 6, 6));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6} }", GetDebuggerDisplayString(assembly, 7, 7));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7} }", GetDebuggerDisplayString(assembly, 8, 8));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7}, X8 = {X8} }", GetDebuggerDisplayString(assembly, 9, 9));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7}, X8 = {X8}, X9 = {X9} }", GetDebuggerDisplayString(assembly, 10, 10));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7}, X8 = {X8}, X9 = {X9} ... }", GetDebuggerDisplayString(assembly, 11, 11));

                Assert.Equal(@"\{ X10 = {X10}, X11 = {X11}, X12 = {X12}, X13 = {X13}, X14 = {X14}, X15 = {X15}, X16 = {X16}, X17 = {X17}, X20 = {X20}, X21 = {X21} ... }",
                    GetDebuggerDisplayString(assembly, 12, 48));
            });
        }

        private static string GetDebuggerDisplayString(AssemblySymbol assembly, int ordinal, int fieldCount)
        {
            NamedTypeSymbol anon;
            if (fieldCount == 0)
            {
                anon = assembly.GetTypeByMetadataName("<>f__AnonymousType0");
            }
            else
            {
                anon = assembly.GetTypeByMetadataName("<>f__AnonymousType" + ordinal + "`" + fieldCount);
            }

            var dd = anon.GetAttributes().Where(a => a.AttributeClass.Name == "DebuggerDisplayAttribute").Single();
            return (string)dd.ConstructorArguments.Single().Value;
        }

        [Fact]
        public void Iterator()
        {
            string source = @"
using System.Collections.Generic;

public class C
{
    public IEnumerable<int> Iterator()
    {
        yield return 1;
    }
}
";
            foreach (var options in new[] { TestOptions.DebugDll, TestOptions.ReleaseDll })
            {
                var comp = CreateCompilationWithMscorlib(source, options: options);

                CompileAndVerify(comp, symbolValidator: module =>
                {
                    var iter = module.ContainingAssembly.GetTypeByMetadataName("C+<Iterator>d__0");
                    AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(iter.GetAttributes()));

                    foreach (var member in iter.GetMembers().Where(member => member is MethodSymbol))
                    {
                        switch (member.Name)
                        {
                            case ".ctor":
                            case "System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator":
                            case "System.Collections.IEnumerable.GetEnumerator":
                            case "System.Collections.IEnumerator.Reset":
                            case "System.IDisposable.Dispose":
                            case "System.Collections.Generic.IEnumerator<System.Int32>.get_Current":
                            case "System.Collections.IEnumerator.get_Current":
                                AssertEx.SetEqual(new[] { "DebuggerHiddenAttribute" }, GetAttributeNames(member.GetAttributes()));
                                break;

                            case "System.Collections.IEnumerator.Current":
                            case "System.Collections.Generic.IEnumerator<System.Int32>.Current":
                            case "MoveNext":
                                AssertEx.SetEqual(new string[] { }, GetAttributeNames(member.GetAttributes()));
                                break;

                            default:
                                throw TestExceptionUtilities.UnexpectedValue(member.Name);
                        }
                    }
                });
            }
        }

        [Fact]
        public void Async()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    public async Task<int> Foo()
    {
        for (int x = 1; x < 10; x++)
        {
            await Foo();
        }
        
        return 1;
    }
}
";
            foreach (var options in new[] { TestOptions.DebugDll, TestOptions.ReleaseDll })
            {
                var comp = CreateCompilationWithMscorlib45(source, options: options);

                CompileAndVerify(comp, symbolValidator: m =>
                {
                    var foo = m.GlobalNamespace.GetMember<MethodSymbol>("C.Foo");
                    AssertEx.SetEqual(options.OptimizationLevel == OptimizationLevel.Debug ?
                                        new[] { "AsyncStateMachineAttribute", "DebuggerStepThroughAttribute" } :
                                        new[] { "AsyncStateMachineAttribute" }, GetAttributeNames(foo.GetAttributes()));

                    var iter = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<Foo>d__0");
                    AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(iter.GetAttributes()));

                    foreach (var member in iter.GetMembers().Where(s => s.Kind == SymbolKind.Method))
                    {
                        switch (member.Name)
                        {
                            case ".ctor":
                                break;

                            case "SetStateMachine":
                                AssertEx.SetEqual(new[] { "DebuggerHiddenAttribute" }, GetAttributeNames(member.GetAttributes()));
                                break;

                            case "MoveNext":
                                AssertEx.SetEqual(new string[] { }, GetAttributeNames(member.GetAttributes()));
                                break;

                            default:
                                throw TestExceptionUtilities.UnexpectedValue(member.Name);
                        }
                    }
                });
            }
        }

        #endregion

        #region CompilationRelaxationsAttribute, RuntimeCompatibilityAttribute

        private void VerifyCompilationRelaxationsAttribute(CSharpAttributeData attribute, SourceAssemblySymbol sourceAssembly, bool isSynthesized)
        {
            ModuleSymbol module = sourceAssembly.Modules[0];
            NamespaceSymbol compilerServicesNS = Get_System_Runtime_CompilerServices_NamespaceSymbol(module);

            NamedTypeSymbol compilationRelaxationsAttrType = compilerServicesNS.GetTypeMember("CompilationRelaxationsAttribute");
            var compilationRelaxationsCtor = (MethodSymbol)sourceAssembly.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32);

            Assert.Equal(compilationRelaxationsAttrType, attribute.AttributeClass);
            Assert.Equal(compilationRelaxationsCtor, attribute.AttributeConstructor);

            int expectedArgValue = isSynthesized ? (int)CompilationRelaxations.NoStringInterning : 0;
            Assert.Equal(1, attribute.CommonConstructorArguments.Length);
            attribute.VerifyValue<int>(0, TypedConstantKind.Primitive, expectedArgValue);

            Assert.Equal(0, attribute.CommonNamedArguments.Length);
        }

        private void VerifyRuntimeCompatibilityAttribute(CSharpAttributeData attribute, SourceAssemblySymbol sourceAssembly, bool isSynthesized)
        {
            ModuleSymbol module = sourceAssembly.Modules[0];
            NamespaceSymbol compilerServicesNS = Get_System_Runtime_CompilerServices_NamespaceSymbol(module);

            NamedTypeSymbol runtimeCompatibilityAttrType = compilerServicesNS.GetTypeMember("RuntimeCompatibilityAttribute");
            var runtimeCompatibilityCtor = (MethodSymbol)sourceAssembly.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor);

            Assert.Equal(runtimeCompatibilityAttrType, attribute.AttributeClass);
            Assert.Equal(runtimeCompatibilityCtor, attribute.AttributeConstructor);

            Assert.Equal(0, attribute.CommonConstructorArguments.Length);

            if (isSynthesized)
            {
                Assert.Equal(1, attribute.CommonNamedArguments.Length);
                attribute.VerifyNamedArgumentValue<bool>(0, "WrapNonExceptionThrows", TypedConstantKind.Primitive, true);
            }
            else
            {
                Assert.Equal(0, attribute.CommonNamedArguments.Length);
            }
        }

        [Fact]
        public void TestSynthesizedAssemblyAttributes_01()
        {
            // Verify Synthesized CompilationRelaxationsAttribute
            // Verify Synthesized RuntimeCompatibilityAttribute

            var source = @"
public class Test
{
    public static void Main()
    {
    }
}";
            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilationWithMscorlib(source, options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release));

                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();

                if (outputKind != OutputKind.NetModule)
                {
                    // Verify synthesized CompilationRelaxationsAttribute and RuntimeCompatibilityAttribute
                    Assert.Equal(3, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                    VerifyDebuggableAttribute(synthesizedAttributes[2], sourceAssembly, DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            }
        }

        [Fact]
        public void TestSynthesizedAssemblyAttributes_02()
        {
            // Verify Applied CompilationRelaxationsAttribute
            // Verify Synthesized RuntimeCompatibilityAttribute

            var source = @"
using System.Runtime.CompilerServices;

[assembly: CompilationRelaxationsAttribute(0)]

public class Test
{
    public static void Main()
    {
    }
}";
            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilationWithMscorlib(source, options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release));

                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;

                // Verify applied CompilationRelaxationsAttribute
                var appliedAttributes = sourceAssembly.GetAttributes();
                Assert.Equal(1, appliedAttributes.Length);
                VerifyCompilationRelaxationsAttribute(appliedAttributes[0], sourceAssembly, isSynthesized: false);

                // Verify synthesized RuntimeCompatibilityAttribute
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                if (outputKind != OutputKind.NetModule)
                {
                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyDebuggableAttribute(synthesizedAttributes[1], sourceAssembly, DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            }
        }

        [Fact]
        public void TestSynthesizedAssemblyAttributes_03()
        {
            // Verify Synthesized CompilationRelaxationsAttribute
            // Verify Applied RuntimeCompatibilityAttribute

            var source = @"
using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute()]

public class Test
{
    public static void Main()
    {
    }
}";
            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilationWithMscorlib(source, options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release));

                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;

                // Verify applied RuntimeCompatibilityAttribute
                var appliedAttributes = sourceAssembly.GetAttributes();
                Assert.Equal(1, appliedAttributes.Length);
                VerifyRuntimeCompatibilityAttribute(appliedAttributes[0], sourceAssembly, isSynthesized: false);

                // Verify synthesized CompilationRelaxationsAttribute
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                if (outputKind != OutputKind.NetModule)
                {
                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyDebuggableAttribute(synthesizedAttributes[1], sourceAssembly, DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            }
        }

        [Fact]
        public void TestSynthesizedAssemblyAttributes_04()
        {
            // Verify Applied CompilationRelaxationsAttribute
            // Verify Applied RuntimeCompatibilityAttribute

            var source = @"
using System.Runtime.CompilerServices;

[assembly: CompilationRelaxationsAttribute(0)]
[assembly: RuntimeCompatibilityAttribute()]

public class Test
{
    public static void Main()
    {
    }
}";
            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilationWithMscorlib(source, options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release));

                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;

                // Verify applied CompilationRelaxationsAttribute and RuntimeCompatibilityAttribute
                var appliedAttributes = sourceAssembly.GetAttributes();
                Assert.Equal(2, appliedAttributes.Length);
                VerifyCompilationRelaxationsAttribute(appliedAttributes[0], sourceAssembly, isSynthesized: false);
                VerifyRuntimeCompatibilityAttribute(appliedAttributes[1], sourceAssembly, isSynthesized: false);

                // Verify no synthesized attributes
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                if (outputKind != OutputKind.NetModule)
                {
                    Assert.Equal(1, synthesizedAttributes.Length);
                    VerifyDebuggableAttribute(synthesizedAttributes[0], sourceAssembly, DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            }
        }

        [Fact]
        public void TestSynthesizedAssemblyAttributes_05()
        {
            // Verify module attributes don't suppress synthesized assembly attributes:

            // Synthesized CompilationRelaxationsAttribute
            // Synthesized RuntimeCompatibilityAttribute

            var source = @"
using System.Runtime.CompilerServices;

[module: CompilationRelaxationsAttribute(0)]
[module: RuntimeCompatibilityAttribute()]

public class Test
{
    public static void Main()
    {
    }
}";
            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilationWithMscorlib(source, options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release));

                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;

                // Verify no applied assembly attributes
                var appliedAssemblyAttributes = sourceAssembly.GetAttributes();
                Assert.Equal(0, appliedAssemblyAttributes.Length);

                // Verify applied module attributes
                var appliedModuleAttributes = sourceAssembly.Modules[0].GetAttributes();
                Assert.Equal(2, appliedModuleAttributes.Length);
                VerifyCompilationRelaxationsAttribute(appliedModuleAttributes[0], sourceAssembly, isSynthesized: false);
                VerifyRuntimeCompatibilityAttribute(appliedModuleAttributes[1], sourceAssembly, isSynthesized: false);

                // Verify synthesized assembly attributes
                var synthesizedAssemblyAttributes = sourceAssembly.GetSynthesizedAttributes();
                if (!outputKind.IsNetModule())
                {
                    Assert.Equal(3, synthesizedAssemblyAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAssemblyAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAssemblyAttributes[1], sourceAssembly, isSynthesized: true);
                    VerifyDebuggableAttribute(synthesizedAssemblyAttributes[2], sourceAssembly, DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints);
                }
                else
                {
                    Assert.Equal(0, synthesizedAssemblyAttributes.Length);
                }
            }
        }

        [Fact]
        public void TestSynthesizedAssemblyAttributes_06()
        {
            // Verify missing well-known attribute types **DO NOT** generate diagnostics and silently suppress synthesizing CompilationRelaxationsAttribute and RuntimeCompatibilityAttribute.

            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilation("", options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release));

                if (outputKind.IsApplication())
                {
                    compilation.VerifyDiagnostics(
                        // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                        Diagnostic(ErrorCode.ERR_NoEntryPoint));
                }
                else
                {
                    compilation.VerifyDiagnostics();
                }

                // Verify no synthesized assembly attributes
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                Assert.Equal(0, synthesizedAttributes.Length);
            }
        }

        [Fact]
        public void TestSynthesizedAssemblyAttributes_07()
        {
            // Verify missing well-known attribute members **DO** generate diagnostics and suppress synthesizing CompilationRelaxationsAttribute and RuntimeCompatibilityAttribute.

            var source = @"
namespace System.Runtime.CompilerServices
{
    sealed public class CompilationRelaxationsAttribute : System.Attribute
    {
    }

    sealed public class RuntimeCompatibilityAttribute : System.Attribute
    {
        public RuntimeCompatibilityAttribute(int dummy) {}
    }
}

public class Test
{
    public static void Main()
    {
    }
}";
            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilationWithMscorlib(source, options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release));

                if (!outputKind.IsNetModule())
                {
                    compilation.VerifyDiagnostics(
                        // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor'
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.CompilationRelaxationsAttribute", ".ctor"),
                        // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor'
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", ".ctor"),
                        // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.WrapNonExceptionThrows'
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", "WrapNonExceptionThrows"));
                }
                else
                {
                    compilation.VerifyDiagnostics();

                    // Verify no synthesized assembly attributes
                    var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                    var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            }
        }

        // NYI: /addmodule support
        // TODO: Add tests for assembly attributes emitted into netmodules which suppress synthesized CompilationRelaxationsAttribute/RuntimeCompatibilityAttribute

        #endregion

        #region DebuggableAttribute

        private void VerifyDebuggableAttribute(CSharpAttributeData attribute, SourceAssemblySymbol sourceAssembly, DebuggableAttribute.DebuggingModes expectedDebuggingMode)
        {
            ModuleSymbol module = sourceAssembly.Modules[0];
            NamespaceSymbol diagnosticsNS = Get_System_Diagnostics_NamespaceSymbol(module);

            NamedTypeSymbol debuggableAttributeType = diagnosticsNS.GetTypeMember("DebuggableAttribute");
            var debuggableAttributeCtor = (MethodSymbol)sourceAssembly.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute__ctorDebuggingModes);

            Assert.Equal(debuggableAttributeType, attribute.AttributeClass);
            Assert.Equal(debuggableAttributeCtor, attribute.AttributeConstructor);

            Assert.Equal(1, attribute.CommonConstructorArguments.Length);
            attribute.VerifyValue(0, TypedConstantKind.Enum, (int)expectedDebuggingMode);

            Assert.Equal(0, attribute.CommonNamedArguments.Length);
        }

        private void VerifySynthesizedDebuggableAttribute(CSharpAttributeData attribute, SourceAssemblySymbol sourceAssembly, OptimizationLevel optimizations)
        {
            var expectedDebuggingMode = DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints;

            if (optimizations == OptimizationLevel.Debug)
            {
                expectedDebuggingMode |=
                    DebuggableAttribute.DebuggingModes.Default |
                    DebuggableAttribute.DebuggingModes.DisableOptimizations |
                    DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
            }

            VerifyDebuggableAttribute(attribute, sourceAssembly, expectedDebuggingMode);
        }

        private void TestDebuggableAttributeCommon(
            string source,
            Action<CSharpCompilation> validator,
            bool includeMscorlibRef,
            bool compileAndVerify,
            OutputKind outputKind,
            OptimizationLevel optimizations)
        {
            var compilation = CSharpCompilation.Create("comp",
                new[] { Parse(source) },
                includeMscorlibRef ? new[] { MscorlibRef } : null,
                new CSharpCompilationOptions(outputKind, optimizationLevel: optimizations));

            validator(compilation);

            if (compileAndVerify)
            {
                // NYI: /addmodule support
                // TODO: PEVerify currently fails for netmodules with error: "The module X was expected to contain an assembly manifest".
                // TODO: Remove the 'verify' named argument once /addmodule support has been added.
                CompileAndVerify(compilation, verify: !outputKind.IsNetModule());
            }
        }

        private void TestDebuggableAttributeMatrix(string source, Action<CSharpCompilation> validator, bool includeMscorlibRef = true, bool compileAndVerify = true)
        {
            foreach (OutputKind outputKind in Enum.GetValues(typeof(OutputKind)))
            {
                foreach (OptimizationLevel optimizations in Enum.GetValues(typeof(OptimizationLevel)))
                {
                    TestDebuggableAttributeCommon(source, validator, includeMscorlibRef, compileAndVerify, outputKind, optimizations);
                }
            }
        }

        [Fact]
        public void TestDebuggableAttribute_01()
        {
            // Verify Synthesized DebuggableAttribute

            var source = @"
public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (!options.OutputKind.IsNetModule())
                {
                    Assert.Equal(3, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                    VerifySynthesizedDebuggableAttribute(synthesizedAttributes[2], sourceAssembly, options.OptimizationLevel);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        [Fact]
        public void TestDebuggableAttribute_02()
        {
            // Verify applied assembly DebuggableAttribute suppresses synthesized DebuggableAttribute

            var source = @"
using System.Diagnostics;

[assembly: DebuggableAttribute(DebuggableAttribute.DebuggingModes.Default)]

public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (!options.OutputKind.IsNetModule())
                {
                    // Verify no synthesized DebuggableAttribute.

                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }

                // Verify applied Debuggable attribute
                var appliedAttributes = sourceAssembly.GetAttributes();
                Assert.Equal(1, appliedAttributes.Length);
                VerifyDebuggableAttribute(appliedAttributes[0], sourceAssembly, DebuggableAttribute.DebuggingModes.Default);
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        [Fact]
        public void TestDebuggableAttribute_03()
        {
            // Verify applied module DebuggableAttribute does not suppress synthesized assembly DebuggableAttribute.

            var source = @"
using System.Diagnostics;

[module: DebuggableAttribute(DebuggableAttribute.DebuggingModes.Default)]

public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (options.OutputKind != OutputKind.NetModule)
                {
                    Assert.Equal(3, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                    VerifySynthesizedDebuggableAttribute(synthesizedAttributes[2], sourceAssembly, options.OptimizationLevel);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }

                // Verify applied Debuggable attribute
                var appliedAttributes = sourceAssembly.Modules[0].GetAttributes();
                Assert.Equal(1, appliedAttributes.Length);
                VerifyDebuggableAttribute(appliedAttributes[0], sourceAssembly, DebuggableAttribute.DebuggingModes.Default);
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        [Fact]
        public void TestDebuggableAttribute_04()
        {
            // Applied [module: DebuggableAttribute()] and [assembly: DebuggableAttribute()]
            // Verify no synthesized assembly DebuggableAttribute.

            var source = @"
using System.Diagnostics;

[module: DebuggableAttribute(DebuggableAttribute.DebuggingModes.Default)]
[assembly: DebuggableAttribute(DebuggableAttribute.DebuggingModes.None)]

public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (!options.OutputKind.IsNetModule())
                {
                    // Verify no synthesized DebuggableAttribute.

                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }

                // Verify applied module Debuggable attribute
                var appliedAttributes = sourceAssembly.Modules[0].GetAttributes();
                Assert.Equal(1, appliedAttributes.Length);
                VerifyDebuggableAttribute(appliedAttributes[0], sourceAssembly, DebuggableAttribute.DebuggingModes.Default);

                // Verify applied assembly Debuggable attribute
                appliedAttributes = sourceAssembly.GetAttributes();
                Assert.Equal(1, appliedAttributes.Length);
                VerifyDebuggableAttribute(appliedAttributes[0], sourceAssembly, DebuggableAttribute.DebuggingModes.None);
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        [Fact]
        public void TestDebuggableAttribute_MissingWellKnownTypeOrMember_01()
        {
            // Missing Well-known type DebuggableAttribute generates no diagnostics and
            // silently suppresses synthesized DebuggableAttribute.

            var source = @"
public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                compilation.VerifyDiagnostics(
                    // (2,14): error CS0518: Predefined type 'System.Object' is not defined or imported
                    // public class Test
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Object"),
                    // (4,19): error CS0518: Predefined type 'System.Void' is not defined or imported
                    //     public static void Main()
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "void").WithArguments("System.Void"),
                    // (2,14): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                    // public class Test
                    Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test").WithArguments("object", "0"));

                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                Assert.Equal(0, synthesizedAttributes.Length);
            };

            TestDebuggableAttributeMatrix(source, validator, includeMscorlibRef: false, compileAndVerify: false);
        }

        [Fact]
        public void TestDebuggableAttribute_MissingWellKnownTypeOrMember_02()
        {
            // Missing Well-known type DebuggableAttribute.DebuggingModes generates no diagnostics and
            // silently suppresses synthesized DebuggableAttribute.

            var source = @"
using System;
using System.Diagnostics;

namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) {}
    }
}

public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (!options.OutputKind.IsNetModule())
                {
                    // Verify no synthesized DebuggableAttribute.

                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        [Fact]
        public void TestDebuggableAttribute_MissingWellKnownTypeOrMember_03()
        {
            // Inaccessible Well-known type DebuggableAttribute.DebuggingModes generates no diagnostics and
            // silently suppresses synthesized DebuggableAttribute.

            var source = @"
using System;
using System.Diagnostics;

namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) {}

        private enum DebuggingModes
        {
            None = 0,
            Default = 1,
            IgnoreSymbolStoreSequencePoints = 2,
            EnableEditAndContinue = 4,
            DisableOptimizations = 256,
        }
    }
}

public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (!options.OutputKind.IsNetModule())
                {
                    // Verify no synthesized DebuggableAttribute.

                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        [Fact]
        public void TestDebuggableAttribute_MissingWellKnownTypeOrMember_04()
        {
            // Struct Well-known type DebuggableAttribute.DebuggingModes (instead of enum) generates no diagnostics and
            // silently suppresses synthesized DebuggableAttribute.

            var source = @"
using System;
using System.Diagnostics;

namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) {}

        public struct DebuggingModes
        {
        }
    }
}

public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (!options.OutputKind.IsNetModule())
                {
                    // Verify no synthesized DebuggableAttribute.

                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        [Fact]
        public void TestDebuggableAttribute_MissingWellKnownTypeOrMember_05()
        {
            // Missing DebuggableAttribute constructor generates no diagnostics and
            // silently suppresses synthesized DebuggableAttribute.

            var source = @"
using System;
using System.Diagnostics;

namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public enum DebuggingModes
        {
            None = 0,
            Default = 1,
            IgnoreSymbolStoreSequencePoints = 2,
            EnableEditAndContinue = 4,
            DisableOptimizations = 256,
        }
    }
}

public class Test
{
    public static void Main()
    {
    }
}";
            Action<CSharpCompilation> validator = (CSharpCompilation compilation) =>
            {
                var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;
                var synthesizedAttributes = sourceAssembly.GetSynthesizedAttributes();
                CSharpCompilationOptions options = compilation.Options;

                if (!options.OutputKind.IsNetModule())
                {
                    // Verify no synthesized DebuggableAttribute.

                    Assert.Equal(2, synthesizedAttributes.Length);
                    VerifyCompilationRelaxationsAttribute(synthesizedAttributes[0], sourceAssembly, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(synthesizedAttributes[1], sourceAssembly, isSynthesized: true);
                }
                else
                {
                    Assert.Equal(0, synthesizedAttributes.Length);
                }
            };

            TestDebuggableAttributeMatrix(source, validator);
        }

        #endregion

        #region UnverifiableCode, SecurityPermission(SkipVerification)

        [Fact]
        public void CheckUnsafeAttributes1()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics();

            var assembly = (SourceAssemblySymbol)compilation.Assembly;
            var assemblyAttribute = assembly.GetSecurityAttributes().Single();
            VerifySkipVerificationSecurityAttribute(assemblyAttribute, compilation);

            var module = (SourceModuleSymbol)assembly.Modules.Single();
            var moduleAttribute = module.GetSynthesizedAttributes().Single();
            VerifyUnverifiableCodeAttribute(moduleAttribute, compilation);
        }

        [Fact]
        public void CheckUnsafeAttributes2()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll.WithOutputKind(OutputKind.NetModule));
            compilation.VerifyDiagnostics();

            var assembly = (SourceAssemblySymbol)compilation.Assembly;
            var assemblyAttribute = assembly.GetSecurityAttributes().Single();
            VerifySkipVerificationSecurityAttribute(assemblyAttribute, compilation);

            var module = (SourceModuleSymbol)assembly.Modules.Single();
            var moduleAttribute = module.GetSynthesizedAttributes().Single();
            VerifyUnverifiableCodeAttribute(moduleAttribute, compilation);
        }

        internal static void VerifySkipVerificationSecurityAttribute(Cci.SecurityAttribute securityAttribute, CSharpCompilation compilation)
        {
            var assemblyAttribute = (CSharpAttributeData)securityAttribute.Attribute;

            Assert.Equal(compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityPermissionAttribute), assemblyAttribute.AttributeClass);
            Assert.Equal(compilation.GetWellKnownTypeMember(WellKnownMember.System_Security_Permissions_SecurityPermissionAttribute__ctor), assemblyAttribute.AttributeConstructor);

            var assemblyAttributeArgument = assemblyAttribute.CommonConstructorArguments.Single();
            Assert.Equal(compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityAction), assemblyAttributeArgument.Type);
            Assert.Equal(DeclarativeSecurityAction.RequestMinimum, securityAttribute.Action);
            Assert.Equal(DeclarativeSecurityAction.RequestMinimum, (DeclarativeSecurityAction)(int)assemblyAttributeArgument.Value);

            var assemblyAttributeNamedArgument = assemblyAttribute.CommonNamedArguments.Single();
            Assert.Equal("SkipVerification", assemblyAttributeNamedArgument.Key);
            var assemblyAttributeNamedArgumentValue = assemblyAttributeNamedArgument.Value;
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Boolean), assemblyAttributeNamedArgumentValue.Type);
            Assert.Equal(true, assemblyAttributeNamedArgumentValue.Value);
        }

        internal static void VerifyUnverifiableCodeAttribute(CSharpAttributeData moduleAttribute, CSharpCompilation compilation)
        {
            Assert.Equal(compilation.GetWellKnownType(WellKnownType.System_Security_UnverifiableCodeAttribute), moduleAttribute.AttributeClass);
            Assert.Equal(compilation.GetWellKnownTypeMember(WellKnownMember.System_Security_UnverifiableCodeAttribute__ctor), moduleAttribute.AttributeConstructor);

            Assert.Equal(0, moduleAttribute.CommonConstructorArguments.Length);
            Assert.Equal(0, moduleAttribute.CommonNamedArguments.Length);
        }

        #endregion

        #region AsyncStateMachineAttribute

        [Fact]
        public void AsyncStateMachineAttribute_Method()
        {
            string source = @"
using System.Threading.Tasks;

class Test
{
    public static async void F()
    {
        await Task.Delay(0);
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var stateMachine = comp.GetMember<NamedTypeSymbol>("Test.<F>d__0");
            var asyncMethod = comp.GetMember<MethodSymbol>("Test.F");

            var asyncMethodAttributes = asyncMethod.GetAttributes();
            AssertEx.SetEqual(new[] { "AsyncStateMachineAttribute" }, GetAttributeNames(asyncMethodAttributes));

            var attributeArg = (NamedTypeSymbol)asyncMethodAttributes.Single().ConstructorArguments.Single().Value;
            Assert.Equal(attributeArg, stateMachine);
        }

        [Fact]
        public void AsyncStateMachineAttribute_Method_Debug()
        {
            string source = @"
using System.Threading.Tasks;

class Test
{
    public static async void F()
    {
        await Task.Delay(0);
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var stateMachine = comp.GetMember<NamedTypeSymbol>("Test.<F>d__0");
            var asyncMethod = comp.GetMember<MethodSymbol>("Test.F");

            var asyncMethodAttributes = asyncMethod.GetAttributes();
            AssertEx.SetEqual(new[] { "AsyncStateMachineAttribute", "DebuggerStepThroughAttribute" }, GetAttributeNames(asyncMethodAttributes));

            var attributeArg = (NamedTypeSymbol)asyncMethodAttributes.First().ConstructorArguments.Single().Value;
            Assert.Equal(attributeArg, stateMachine);
        }

        [Fact]
        public void AsyncStateMachineAttribute_Lambda()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static void F()
    {
        Action f = async () => { await Task.Delay(0); };
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var stateMachine = comp.GetMember<NamedTypeSymbol>("Test.<>c.<<F>b__0_0>d");
            var asyncMethod = comp.GetMember<MethodSymbol>("Test.<>c.<F>b__0_0");

            var asyncMethodAttributes = asyncMethod.GetAttributes();
            AssertEx.SetEqual(new[] { "AsyncStateMachineAttribute" }, GetAttributeNames(asyncMethodAttributes));

            var attributeArg = (NamedTypeSymbol)asyncMethodAttributes.Single().ConstructorArguments.Single().Value;
            Assert.Equal(attributeArg, stateMachine);
        }

        [Fact]
        public void AsyncStateMachineAttribute_Lambda_Debug()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static void F()
    {
        Action f = async () => { await Task.Delay(0); };
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var stateMachine = comp.GetMember<NamedTypeSymbol>("Test.<>c.<<F>b__0_0>d");
            var asyncMethod = comp.GetMember<MethodSymbol>("Test.<>c.<F>b__0_0");

            var asyncMethodAttributes = asyncMethod.GetAttributes();
            AssertEx.SetEqual(new[] { "AsyncStateMachineAttribute", "DebuggerStepThroughAttribute" }, GetAttributeNames(asyncMethodAttributes));

            var attributeArg = (NamedTypeSymbol)asyncMethodAttributes.First().ConstructorArguments.Single().Value;
            Assert.Equal(attributeArg, stateMachine);
        }

        [Fact]
        public void AsyncStateMachineAttribute_GenericStateMachineClass()
        {
            string source = @"
using System.Threading.Tasks;

public class Test<T>
{
    public async void F<U>(U u) where U : Test<int>, new()
    {
        await Task.Delay(0);
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var stateMachine = comp.GetMember<NamedTypeSymbol>("Test.<F>d__0");
            var asyncMethod = comp.GetMember<MethodSymbol>("Test.F");

            var asyncMethodAttributes = asyncMethod.GetAttributes();
            AssertEx.SetEqual(new[] { "AsyncStateMachineAttribute" }, GetAttributeNames(asyncMethodAttributes));

            var attributeStateMachineClass = (NamedTypeSymbol)asyncMethodAttributes.Single().ConstructorArguments.Single().Value;
            Assert.Equal(attributeStateMachineClass, stateMachine.ConstructUnboundGenericType());
        }

        [Fact]
        public void AsyncStateMachineAttribute_MetadataOnly()
        {
            string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test
{
    public static async void F()
    {
        await Task.Delay(0);
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source).
                EmitToImageReference(new EmitOptions(metadataOnly: true));

            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            Assert.Empty(comp.GetMember<NamedTypeSymbol>("Test").GetMembers("<F>d__1"));

            var asyncMethod = comp.GetMember<MethodSymbol>("Test.F");

            var asyncMethodAttributes = asyncMethod.GetAttributes();
            Assert.Empty(GetAttributeNames(asyncMethodAttributes));
        }

        [Fact]
        public void AsyncStateMachineAttribute_MetadataOnly_Debug()
        {
            string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test
{
    public static async void F()
    {
        await Task.Delay(0);
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll).
                EmitToImageReference(new EmitOptions(metadataOnly: true));

            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            Assert.Empty(comp.GetMember<NamedTypeSymbol>("Test").GetMembers("<F>d__1"));

            var asyncMethod = comp.GetMember<MethodSymbol>("Test.F");

            var asyncMethodAttributes = asyncMethod.GetAttributes();
            AssertEx.SetEqual(new[] { "DebuggerStepThroughAttribute" }, GetAttributeNames(asyncMethodAttributes));
        }

        #endregion

        #region IteratorStateMachineAttribute

        [Fact]
        public void IteratorStateMachineAttribute_Method()
        {
            string source = @"
using System.Collections.Generic;

class Test
{
    public static IEnumerable<int> F()
    {
        yield return 1;
    }
}
";
            foreach (var options in new[] { TestOptions.ReleaseDll, TestOptions.DebugDll })
            {
                var reference = CreateCompilationWithMscorlib45(source, options: options).EmitToImageReference();
                var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

                var stateMachine = comp.GetMember<NamedTypeSymbol>("Test.<F>d__0");
                var asyncMethod = comp.GetMember<MethodSymbol>("Test.F");

                var asyncMethodAttributes = asyncMethod.GetAttributes();
                AssertEx.SetEqual(new[] { "IteratorStateMachineAttribute" }, GetAttributeNames(asyncMethodAttributes));

                var attributeArg = (NamedTypeSymbol)asyncMethodAttributes.Single().ConstructorArguments.Single().Value;
                Assert.Equal(attributeArg, stateMachine);
            }
        }

        [Fact]
        public void IteratorStateMachineAttribute_GenericStateMachineClass()
        {
            string source = @"
using System.Collections.Generic;

public class Test<T>
{
    public IEnumerable<int> F<U>(U u) where U : Test<int>, new()
    {
        yield return 1;
    }
}
";
            var reference = CreateCompilationWithMscorlib45(source).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var stateMachine = comp.GetMember<NamedTypeSymbol>("Test.<F>d__0");
            var iteratorMethod = comp.GetMember<MethodSymbol>("Test.F");

            var iteratorMethodAttributes = iteratorMethod.GetAttributes();
            AssertEx.SetEqual(new[] { "IteratorStateMachineAttribute" }, GetAttributeNames(iteratorMethodAttributes));

            var attributeStateMachineClass = (NamedTypeSymbol)iteratorMethodAttributes.Single().ConstructorArguments.Single().Value;
            Assert.Equal(attributeStateMachineClass, stateMachine.ConstructUnboundGenericType());
        }

        [Fact]
        public void IteratorStateMachineAttribute_MetadataOnly()
        {
            string source = @"
using System.Collections.Generic;

public class Test
{
    public static IEnumerable<int> F()
    {
        yield return 1;
    }
}
";

            foreach (var options in new[] { TestOptions.ReleaseDll, TestOptions.DebugDll })
            {
                var reference = CreateCompilationWithMscorlib45(source, options: options).
                    EmitToImageReference(new EmitOptions(metadataOnly: true));

                var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
                Assert.Empty(comp.GetMember<NamedTypeSymbol>("Test").GetMembers("<F>d__0"));

                var iteratorMethod = comp.GetMember<MethodSymbol>("Test.F");

                var iteratorMethodAttributes = iteratorMethod.GetAttributes();
                Assert.Empty(GetAttributeNames(iteratorMethodAttributes)); // We haven't bound the body, so we don't know that this is an iterator method.
            }
        }

        #endregion

        [Fact, WorkItem(431, "https://github.com/dotnet/roslyn/issues/431")]
        public void BaseMethodWrapper()
        {
            string source = @"
using System.Threading.Tasks;

class A
{
    public virtual async Task<int> GetIntAsync()
    {
        return 42;
    }
}
class B : A
{
    public override async Task<int> GetIntAsync()
    {
        return await base.GetIntAsync();
    }
}
";
            foreach (var options in new[] { TestOptions.ReleaseDll, TestOptions.DebugDll })
            {
                var reference = CreateCompilationWithMscorlib45(source, options: options).EmitToImageReference();
                var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

                var baseMethodWrapper = comp.GetMember<MethodSymbol>("B.<>n__0");

                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute", "DebuggerHiddenAttribute" }, GetAttributeNames(baseMethodWrapper.GetAttributes()));
            }
        }

        [Fact, WorkItem(7809, "https://github.com/dotnet/roslyn/issues/7809")]
        public void SynthesizeAttributeWithUseSiteErrorFails()
        {
            #region "mslib"
            var mslibNoString = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public class ValueType { }
    public class Attribute { }
    public struct Void { }
}";
            var mslib = mslibNoString + @"
namespace System
{
    public class String { }
}";
            #endregion

            // Build an mscorlib including String
            var mslibComp = CreateCompilation(new string[] { mslib }).VerifyDiagnostics();
            var mslibRef = mslibComp.EmitToImageReference();

            // Build an mscorlib without String
            var mslibNoStringComp = CreateCompilation(new string[] { mslibNoString }).VerifyDiagnostics();
            var mslibNoStringRef = mslibNoStringComp.EmitToImageReference();

            var diagLibSource = @"
namespace System.Diagnostics
{
    public class DebuggerDisplayAttribute : System.Attribute
    {
        public DebuggerDisplayAttribute(System.String s) { }
        public System.String Type { get { return null; } set { } }
    }
}
namespace System.Runtime.CompilerServices
{
    public class CompilerGeneratedAttribute { } 
}";
            // Build Diagnostics referencing mscorlib with String
            var diagLibComp = CreateCompilation(new string[] { diagLibSource }, references: new[] { mslibRef }).VerifyDiagnostics();
            var diagLibRef = diagLibComp.EmitToImageReference();

            // Create compilation using Diagnostics but referencing mscorlib without String
            var comp = CreateCompilation(new SyntaxTree[] { Parse("") }, references: new[] { diagLibRef, mslibNoStringRef });

            // Attribute cannot be synthesized because ctor has a use-site error (String type missing)
            var attribute = comp.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor);
            Assert.Equal(null, attribute);

            // Attribute cannot be synthesized because type in named argument has use-site error (String type missing)
            var attribute2 = comp.TrySynthesizeAttribute(
                                WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor,
                                namedArguments: ImmutableArray.Create(new KeyValuePair<WellKnownMember, TypedConstant>(
                                                    WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__Type,
                                                    new TypedConstant(comp.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, "unused"))));
            Assert.Equal(null, attribute2);
        }
    }
}
