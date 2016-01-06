// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests : CompilingTestBase
    {
        #region Function Tests

        [Fact]
        public void TestAssemblyAttributes()
        {
            var source = CreateCompilationWithMscorlib(@"
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(""Roslyn.Compilers.UnitTests"")]
[assembly: InternalsVisibleTo(""Roslyn.Compilers.CSharp"")]
[assembly: InternalsVisibleTo(""Roslyn.Compilers.CSharp.UnitTests"")]
[assembly: InternalsVisibleTo(""Roslyn.Compilers.CSharp.Test.Utilities"")]

[assembly: InternalsVisibleTo(""Roslyn.Compilers.VisualBasic"")]
class C
{
    public static void Main() {}
}
");

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                Symbol assembly = m.ContainingSymbol;
                var attrs = assembly.GetAttributes();
                Assert.Equal(5, attrs.Length);
                attrs[0].VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.UnitTests");
                Assert.Equal(@"System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Roslyn.Compilers.UnitTests"")", attrs[0].ToString());
                attrs[1].VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.CSharp");
                Assert.Equal(@"System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Roslyn.Compilers.CSharp"")", attrs[1].ToString());
                attrs[2].VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.CSharp.UnitTests");
                Assert.Equal(@"System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Roslyn.Compilers.CSharp.UnitTests"")", attrs[2].ToString());
                attrs[3].VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.CSharp.Test.Utilities");
                Assert.Equal(@"System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Roslyn.Compilers.CSharp.Test.Utilities"")", attrs[3].ToString());
                attrs[4].VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.VisualBasic");
                Assert.Equal(@"System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Roslyn.Compilers.VisualBasic"")", attrs[4].ToString());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(984896)]
        [Fact]
        public void TestAssemblyAttributesErr()
        {
            string code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using M = System.Math;

namespace My
{
    using A.B;

    // TODO: <Insert justification for suppressing TestId>
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(""Test"",""TestId"",Justification=""<Pending>"")]
public unsafe partial class A : C, I
    {

    }
}
";

            var source = CreateCompilationWithMscorlibAndSystemCore(code);

            // the following should not crash
            source.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, source.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true);
        }


        [Fact, WorkItem(545326, "DevDiv")]
        public void TestAssemblyAttributes_Bug13670()
        {
            var source = @"
using System;

[assembly: A(Derived.Str)]

public class A: Attribute
{
  public A(string x){}
  public static void Main() {}
}
public class Derived: Base
{
    internal const string Str = ""temp"";
    public override int Foo { get { return 1; } }
}
public class Base
{
    public virtual int Foo { get { return 0; } }
}
";
            CompileAndVerify(source);
        }

        [Fact]
        public void TestAssemblyAttributesReflection()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// These are not pseudo attributes, but encoded as bits in metadata
[assembly: AssemblyAlgorithmId(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)]
[assembly: AssemblyCultureAttribute("""")]
[assembly: AssemblyDelaySign(true)]
[assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)]
[assembly: AssemblyKeyFile(""MyKey.snk"")]
[assembly: AssemblyKeyName(""Key Name"")]

[assembly: AssemblyVersion(""1.2.*"")]
[assembly: AssemblyFileVersionAttribute(""4.3.2.100"")]
class C
{
    public static void Main() {}
}
");

            var attrs = compilation.Assembly.GetAttributes();
            Assert.Equal(8, attrs.Length);

            foreach (var a in attrs)
            {
                switch (a.AttributeClass.Name)
                {
                    case "AssemblyAlgorithmIdAttribute":
                        a.VerifyValue(0, TypedConstantKind.Enum, (int)System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5);
                        Assert.Equal(@"System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)", a.ToString());
                        break;
                    case "AssemblyCultureAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, "");
                        Assert.Equal(@"System.Reflection.AssemblyCultureAttribute("""")", a.ToString());
                        break;
                    case "AssemblyDelaySignAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, true);
                        Assert.Equal(@"System.Reflection.AssemblyDelaySignAttribute(true)", a.ToString());
                        break;
                    case "AssemblyFlagsAttribute":
                        a.VerifyValue(0, TypedConstantKind.Enum, (int)AssemblyNameFlags.Retargetable);
                        Assert.Equal(@"System.Reflection.AssemblyFlagsAttribute(System.Reflection.AssemblyNameFlags.Retargetable)", a.ToString());
                        break;
                    case "AssemblyKeyFileAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, "MyKey.snk");
                        Assert.Equal(@"System.Reflection.AssemblyKeyFileAttribute(""MyKey.snk"")", a.ToString());
                        break;
                    case "AssemblyKeyNameAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, "Key Name");
                        Assert.Equal(@"System.Reflection.AssemblyKeyNameAttribute(""Key Name"")", a.ToString());
                        break;
                    case "AssemblyVersionAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, "1.2.*");
                        Assert.Equal(@"System.Reflection.AssemblyVersionAttribute(""1.2.*"")", a.ToString());
                        break;
                    case "AssemblyFileVersionAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, "4.3.2.100");
                        Assert.Equal(@"System.Reflection.AssemblyFileVersionAttribute(""4.3.2.100"")", a.ToString());
                        break;
                    default:
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name);
                        break;
                }
            }
        }

        // Verify that resolving an attribute defined within a class on a class does not cause infinite recursion
        [Fact]
        public void TestAttributesOnClassDefinedInClass()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
using System.Runtime.CompilerServices;

[A.X()]
public class A
{
    [AttributeUsage(AttributeTargets.All, allowMultiple = true)]
    public class XAttribute : Attribute
    {
    }
}
class C
{
    public static void Main() {}
}
");
            var attrs = compilation.SourceModule.GlobalNamespace.GetMember("A").GetAttributes();
            Assert.Equal(1, attrs.Length);
            Assert.Equal("A.XAttribute", attrs.First().AttributeClass.ToDisplayString());
        }

        [Fact]
        public void TestAttributesOnClassWithConstantDefinedInClass()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
[Attr(Foo.p)]
class Foo
{
    private const object p = null;
}
internal class AttrAttribute : Attribute
{
    public AttrAttribute(object p) { }
}
class C
{
    public static void Main() { }
}
");
            var attrs = compilation.SourceModule.GlobalNamespace.GetMember("Foo").GetAttributes();
            Assert.Equal(1, attrs.Length);
            attrs.First().VerifyValue<object>(0, TypedConstantKind.Primitive, null);
        }

        [Fact]
        public void TestAttributeEmit()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
public enum e1
{
    a,
    b,
    c
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class XAttribute : Attribute
{
    public XAttribute(int i)
    {
    }
    public XAttribute(int i, string s)
    {
    }
    public XAttribute(int i, string s, e1 e)
    {
    }
    public XAttribute(object[] o)
    {
    }
    public XAttribute(int[] i)
    {
    }
    public XAttribute(int[] i, string[] s)
    {
    }
    public XAttribute(int[] i, string[] s, e1[] e)
    {
    }
    public int pi { get; set; }
    public string ps { get; set; }
    public e1 pe { get; set; }
}

[X(1, ""hello"", e1.a)]
[X(new int[] { 1 }, new string[] { ""hello"" }, new e1[] { e1.a, e1.b, e1.c })]
[X(new object[] { 1, ""hello"", e1.a })]
class C
{
    public static void Main() {}
}
");
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("XAttribute..ctor(int)", @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""System.Attribute..ctor()""
  IL_0006:  ret       
}");
        }

        [Fact]
        public void TestAttributesOnClassProperty()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
public class A
{
    [CLSCompliant(true)]
    public string Prop
    {
        get { return null; }
    }
}
class C
{
    public static void Main() {}
}
");
            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var type = (NamedTypeSymbol)m.GlobalNamespace.GetMember("A");
                var prop = type.GetMember("Prop");
                var attrs = prop.GetAttributes();
                Assert.Equal(1, attrs.Length);
                attrs.First().VerifyValue(0, TypedConstantKind.Primitive, true);
                Assert.Equal("System.CLSCompliantAttribute", attrs.First().AttributeClass.ToDisplayString());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(688268, "DevDiv")]
        [Fact]
        public void Bug688268()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
using System.Runtime.InteropServices;
using System.Security;

public interface I
{
    void _VtblGap1_30();
    void _VtblGaP1_30();
}
");
            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
            {
                var metadata = ((PEModuleSymbol)module).Module;

                var typeI = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("I").Single();

                var methods = metadata.GetMethodsOfTypeOrThrow(typeI.Handle);
                Assert.Equal(2, methods.Count);

                var e = methods.GetEnumerator();
                e.MoveNext();
                var flags = metadata.GetMethodDefFlagsOrThrow(e.Current);
                Assert.Equal(
                    MethodAttributes.PrivateScope |
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.HideBySig |
                    MethodAttributes.VtableLayoutMask |
                    MethodAttributes.Abstract |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName,
                    flags);

                e.MoveNext();
                flags = metadata.GetMethodDefFlagsOrThrow(e.Current);
                Assert.Equal(
                    MethodAttributes.PrivateScope |
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.HideBySig |
                    MethodAttributes.VtableLayoutMask |
                    MethodAttributes.Abstract,
                    flags);
            };

            CompileAndVerify(
                compilation,
                sourceSymbolValidator: null,
                symbolValidator: metadataValidator);
        }

        [Fact]
        public void TestAttributesOnPropertyAndGetSet()
        {
            string source = @"
using System;
[AObject(typeof(object), O = A.obj)]
public class A
{
    internal const object obj = null;
    public string RProp
    {
        [AObject(new object[] { typeof(string) })]
        get { return null; }
    }

    [AObject(new object[] {
        1,
        ""two"",
        typeof(string),
        3.1415926
    })]
    public object WProp
    {
        [AObject(new object[] { new object[] { typeof(string) } })]

        set { }
    }
}
";
            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib.AsImmutableOrNull()) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var type = (NamedTypeSymbol)m.GlobalNamespace.GetMember("A");
                var attrs = type.GetAttributes();
                Assert.Equal("AObjectAttribute(typeof(object), O = null)", attrs.First().ToString());
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Type, typeof(object));
                attrs.First().VerifyNamedArgumentValue<object>(0, "O", TypedConstantKind.Primitive, null);

                var prop = type.GetMember<PropertySymbol>("RProp");
                attrs = prop.GetMethod.GetAttributes();
                Assert.Equal("AObjectAttribute({typeof(string)})", attrs.First().ToString());
                attrs.First().VerifyValue(0, TypedConstantKind.Array, new object[] { typeof(string) });

                prop = type.GetMember<PropertySymbol>("WProp");
                attrs = prop.GetAttributes();
                Assert.Equal(@"AObjectAttribute({1, ""two"", typeof(string), 3.1415926})", attrs.First().ToString());
                attrs.First().VerifyValue(0, TypedConstantKind.Array, new object[] { 1, "two", typeof(string), 3.1415926 });
                attrs = prop.SetMethod.GetAttributes();
                Assert.Equal(@"AObjectAttribute({{typeof(string)}})", attrs.First().ToString());
                attrs.First().VerifyValue(0, TypedConstantKind.Array, new object[] { new object[] { typeof(string) } });
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);
        }

        [Fact]
        public void TestAttributesOnEvents()
        {
            string source = @"
public class AA : System.Attribute { }
public class BB : System.Attribute { }
public class CC : System.Attribute { }
public class DD : System.Attribute { }
public class EE : System.Attribute { }
public class FF : System.Attribute { }
public class GG : System.Attribute { }
public class HH : System.Attribute { }
public class II : System.Attribute { }
public class JJ : System.Attribute { }

public class Test
{
    [AA] //in event decl
    public event System.Action E1;
    [event: BB] //in event decl
    public event System.Action E2;
    [method: CC] //in both accessors
    public event System.Action E3;
    [field: DD] //on field
    public event System.Action E4;

    [EE] //in event decl
    public event System.Action E5 { add { } remove { } }
    [event: FF] //in event decl
    public event System.Action E6 { add { } remove { } }

    public event System.Action E7 { [GG] add { } remove { } } //in accessor
    public event System.Action E8 { [method: HH] add { } remove { } } //in accessor
    public event System.Action E9 { [param: II] add { } remove { } } //on parameter (after .param[1])
    public event System.Action E10 { [return: JJ] add { } remove { } } //on return (after .param[0])
}
";

            Func<bool, Action<ModuleSymbol>> symbolValidator = isFromSource => moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var event1 = @class.GetMember<EventSymbol>("E1");
                var event2 = @class.GetMember<EventSymbol>("E2");
                var event3 = @class.GetMember<EventSymbol>("E3");
                var event4 = @class.GetMember<EventSymbol>("E4");
                var event5 = @class.GetMember<EventSymbol>("E5");
                var event6 = @class.GetMember<EventSymbol>("E6");
                var event7 = @class.GetMember<EventSymbol>("E7");
                var event8 = @class.GetMember<EventSymbol>("E8");
                var event9 = @class.GetMember<EventSymbol>("E9");
                var event10 = @class.GetMember<EventSymbol>("E10");

                var accessorsExpected = isFromSource ? new string[0] : new[] { "CompilerGeneratedAttribute" };

                Assert.Equal("AA", GetSingleAttributeName(event1));
                AssertEx.SetEqual(accessorsExpected, GetAttributeNames(event1.AddMethod.GetAttributes()));
                AssertEx.SetEqual(accessorsExpected, GetAttributeNames(event1.RemoveMethod.GetAttributes()));

                if (isFromSource)
                {
                    AssertNoAttributes(event1.AssociatedField);
                    Assert.Equal(0, event1.GetFieldAttributes().Length);
                }

                Assert.Equal("BB", GetSingleAttributeName(event2));
                AssertEx.SetEqual(accessorsExpected, GetAttributeNames(event2.AddMethod.GetAttributes()));
                AssertEx.SetEqual(accessorsExpected, GetAttributeNames(event2.RemoveMethod.GetAttributes()));
                if (isFromSource)
                {
                    AssertNoAttributes(event2.AssociatedField);
                    Assert.Equal(0, event2.GetFieldAttributes().Length);
                }

                AssertNoAttributes(event3);
                AssertEx.SetEqual(accessorsExpected.Concat(new[] { "CC" }), GetAttributeNames(event3.AddMethod.GetAttributes()));
                AssertEx.SetEqual(accessorsExpected.Concat(new[] { "CC" }), GetAttributeNames(event3.RemoveMethod.GetAttributes()));
                if (isFromSource)
                {
                    AssertNoAttributes(event3.AssociatedField);
                    Assert.Equal(0, event3.GetFieldAttributes().Length);
                }

                AssertNoAttributes(event4);
                AssertEx.SetEqual(accessorsExpected, GetAttributeNames(event4.AddMethod.GetAttributes()));
                AssertEx.SetEqual(accessorsExpected, GetAttributeNames(event4.RemoveMethod.GetAttributes()));
                if (isFromSource)
                {
                    Assert.Equal("DD", GetSingleAttributeName(event4.AssociatedField));
                    Assert.Equal("DD", event4.GetFieldAttributes().Single().AttributeClass.Name);
                }

                Assert.Equal("EE", GetSingleAttributeName(event5));
                AssertNoAttributes(event5.AddMethod);
                AssertNoAttributes(event5.RemoveMethod);

                Assert.Equal("FF", GetSingleAttributeName(event6));
                AssertNoAttributes(event6.AddMethod);
                AssertNoAttributes(event6.RemoveMethod);


                AssertNoAttributes(event7);
                Assert.Equal("GG", GetSingleAttributeName(event7.AddMethod));
                AssertNoAttributes(event7.RemoveMethod);

                AssertNoAttributes(event8);
                Assert.Equal("HH", GetSingleAttributeName(event8.AddMethod));
                AssertNoAttributes(event8.RemoveMethod);

                AssertNoAttributes(event9);
                AssertNoAttributes(event9.AddMethod);
                AssertNoAttributes(event9.RemoveMethod);
                Assert.Equal("II", GetSingleAttributeName(event9.AddMethod.Parameters.Single()));

                AssertNoAttributes(event10);
                AssertNoAttributes(event10.AddMethod);
                AssertNoAttributes(event10.RemoveMethod);
                Assert.Equal("JJ", event10.AddMethod.GetReturnTypeAttributes().Single().AttributeClass.Name);
            };

            CompileAndVerify(source, sourceSymbolValidator: symbolValidator(true), symbolValidator: symbolValidator(false));
        }

        [Fact]
        public void TestAttributesOnEvents_NoDuplicateDiagnostics()
        {
            string source = @"
public class AA : System.Attribute { }
public class BB : System.Attribute { }
public class CC : System.Attribute { }
public class DD : System.Attribute { }
public class EE : System.Attribute { }
public class FF : System.Attribute { }
public class GG : System.Attribute { }
public class HH : System.Attribute { }
public class II : System.Attribute { }
public class JJ : System.Attribute { }

public class Test
{
    [AA(0)] //in event decl
    public event System.Action E1;
    [event: BB(0)] //in event decl
    public event System.Action E2;
    [method: CC(0)] //in both accessors
    public event System.Action E3;
    [field: DD(0)] //on field
    public event System.Action E4;

    [EE(0)] //in event decl
    public event System.Action E5 { add { } remove { } }
    [event: FF(0)] //in event decl
    public event System.Action E6 { add { } remove { } }

    public event System.Action E7 { [GG(0)] add { } remove { } } //in accessor
    public event System.Action E8 { [method: HH(0)] add { } remove { } } //in accessor
    public event System.Action E9 { [param: II(0)] add { } remove { } } //on parameter (after .param[1])
    public event System.Action E10 { [return: JJ(0)] add { } remove { } } //on return (after .param[0])
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (15,6): error CS1729: 'AA' does not contain a constructor that takes 1 arguments
                //     [AA(0)] //in event decl
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "AA(0)").WithArguments("AA", "1"),
                // (17,13): error CS1729: 'BB' does not contain a constructor that takes 1 arguments
                //     [event: BB(0)] //in event decl
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "BB(0)").WithArguments("BB", "1"),
                // (19,14): error CS1729: 'CC' does not contain a constructor that takes 1 arguments
                //     [method: CC(0)] //in both accessors
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "CC(0)").WithArguments("CC", "1"),
                // (21,13): error CS1729: 'DD' does not contain a constructor that takes 1 arguments
                //     [field: DD(0)] //on field
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "DD(0)").WithArguments("DD", "1"),
                // (24,6): error CS1729: 'EE' does not contain a constructor that takes 1 arguments
                //     [EE(0)] //in event decl
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "EE(0)").WithArguments("EE", "1"),
                // (26,13): error CS1729: 'FF' does not contain a constructor that takes 1 arguments
                //     [event: FF(0)] //in event decl
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "FF(0)").WithArguments("FF", "1"),
                // (29,38): error CS1729: 'GG' does not contain a constructor that takes 1 arguments
                //     public event System.Action E7 { [GG(0)] add { } remove { } } //in accessor
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "GG(0)").WithArguments("GG", "1"),
                // (30,46): error CS1729: 'HH' does not contain a constructor that takes 1 arguments
                //     public event System.Action E8 { [method: HH(0)] add { } remove { } } //in accessor
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "HH(0)").WithArguments("HH", "1"),
                // (31,45): error CS1729: 'II' does not contain a constructor that takes 1 arguments
                //     public event System.Action E9 { [param: II(0)] add { } remove { } } //on parameter (after .param[1])
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "II(0)").WithArguments("II", "1"),
                // (32,47): error CS1729: 'JJ' does not contain a constructor that takes 1 arguments
                //     public event System.Action E10 { [return: JJ(0)] add { } remove { } } //on return (after .param[0])
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "JJ(0)").WithArguments("JJ", "1"),
                // (22,32): warning CS0067: The event 'Test.E4' is never used
                //     public event System.Action E4;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E4").WithArguments("Test.E4"),
                // (18,32): warning CS0067: The event 'Test.E2' is never used
                //     public event System.Action E2;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E2").WithArguments("Test.E2"),
                // (20,32): warning CS0067: The event 'Test.E3' is never used
                //     public event System.Action E3;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E3").WithArguments("Test.E3"),
                // (16,32): warning CS0067: The event 'Test.E1' is never used
                //     public event System.Action E1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("Test.E1"));
        }

        [Fact]
        public void TestAttributesOnIndexer_NoDuplicateDiagnostics()
        {
            string source = @"
public class AA : System.Attribute { }
public class BB : System.Attribute { }
public class CC : System.Attribute { }
public class DD : System.Attribute { }
public class EE : System.Attribute { }

public class Test
{
    public int this[[AA(0)]int x]
    {
        [return: BB(0)]
        [CC(0)]
        get { return x; }

        [param: DD(0)]
        [EE(0)]
        set { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,22): error CS1729: 'AA' does not contain a constructor that takes 1 arguments
                //     public int this[[AA(0)]int x]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "AA(0)").WithArguments("AA", "1"),
                // (13,10): error CS1729: 'CC' does not contain a constructor that takes 1 arguments
                //         [CC(0)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "CC(0)").WithArguments("CC", "1"),
                // (12,18): error CS1729: 'BB' does not contain a constructor that takes 1 arguments
                //         [return: BB(0)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "BB(0)").WithArguments("BB", "1"),
                // (16,17): error CS1729: 'DD' does not contain a constructor that takes 1 arguments
                //         [param: DD(0)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "DD(0)").WithArguments("DD", "1"),
                // (17,10): error CS1729: 'EE' does not contain a constructor that takes 1 arguments
                //         [EE(0)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "EE(0)").WithArguments("EE", "1"));
        }

        private static string GetSingleAttributeName(Symbol symbol)
        {
            return symbol.GetAttributes().Single().AttributeClass.Name;
        }

        private static void AssertNoAttributes(Symbol symbol)
        {
            Assert.Equal(0, symbol.GetAttributes().Length);
        }

        [Fact]
        public void TestAttributesOnDelegates()
        {
            string source = @"
using System;

public class TypeAttribute : System.Attribute { }
public class ParamAttribute : System.Attribute { }
public class ReturnTypeAttribute : System.Attribute { }
public class TypeParamAttribute : System.Attribute { }

class C
{
    [TypeAttribute]
    [return: ReturnTypeAttribute]
    public delegate T Delegate<[TypeParamAttribute]T> ([ParamAttribute]T p1, [param: ParamAttribute]ref T p2, [ParamAttribute]out T p3);
    
    public delegate int Delegate2 ([ParamAttribute]int p1 = 0, [param: ParamAttribute]params int[] p2);

    static void Main()
    {
        typeof(Delegate<int>).GetCustomAttributes(false);
    }
}";

            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var type = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var typeAttrType = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("TypeAttribute");
                var paramAttrType = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("ParamAttribute");
                var returnTypeAttrType = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("ReturnTypeAttribute");
                var typeParamAttrType = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("TypeParamAttribute");

                // Verify delegate type attribute
                var delegateType = type.GetTypeMember("Delegate");
                Assert.Equal(1, delegateType.GetAttributes(typeAttrType).Count());

                // Verify type parameter attribute
                var typeParameters = delegateType.TypeParameters;
                Assert.Equal(1, typeParameters.Length);
                Assert.Equal(1, typeParameters[0].GetAttributes(typeParamAttrType).Count());

                // Verify delegate methods (return type/parameters) attributes

                // Invoke method
                // 1) Has return type attributes from delegate declaration syntax
                // 2) Has parameter attributes from delegate declaration syntax
                var invokeMethod = delegateType.GetMethod("Invoke");
                Assert.Equal(1, invokeMethod.GetReturnTypeAttributes().Where(a => a.AttributeClass == returnTypeAttrType).Count());
                Assert.Equal(typeParameters[0], invokeMethod.ReturnType);
                var parameters = invokeMethod.GetParameters();
                Assert.Equal(3, parameters.Length);
                Assert.Equal("p1", parameters[0].Name);
                Assert.Equal(1, parameters[0].GetAttributes(paramAttrType).Count());
                Assert.Equal("p2", parameters[1].Name);
                Assert.Equal(1, parameters[1].GetAttributes(paramAttrType).Count());
                Assert.Equal("p3", parameters[2].Name);
                Assert.Equal(1, parameters[2].GetAttributes(paramAttrType).Count());

                // Delegate Constructor:
                // 1) Doesn't have any return type attributes
                // 2) Doesn't have any parameter attributes
                var ctor = delegateType.GetMethod(".ctor");
                Assert.Equal(0, ctor.GetReturnTypeAttributes().Length);
                parameters = ctor.GetParameters();
                Assert.Equal(2, parameters.Length);
                Assert.Equal(0, parameters[0].GetAttributes().Length);
                Assert.Equal(0, parameters[1].GetAttributes().Length);

                // BeginInvoke method:
                // 1) Doesn't have any return type attributes
                // 2) Has parameter attributes from delegate declaration parameters syntax
                var beginInvokeMethod = (MethodSymbol)delegateType.GetMember("BeginInvoke");
                Assert.Equal(0, beginInvokeMethod.GetReturnTypeAttributes().Length);
                parameters = beginInvokeMethod.GetParameters();
                Assert.Equal(5, parameters.Length);
                Assert.Equal("p1", parameters[0].Name);
                Assert.Equal(1, parameters[0].GetAttributes(paramAttrType).Count());
                Assert.Equal("p2", parameters[1].Name);
                Assert.Equal(1, parameters[1].GetAttributes(paramAttrType).Count());
                Assert.Equal("p3", parameters[2].Name);
                Assert.Equal(1, parameters[2].GetAttributes(paramAttrType).Count());
                Assert.Equal(0, parameters[3].GetAttributes(paramAttrType).Count());
                Assert.Equal(0, parameters[4].GetAttributes(paramAttrType).Count());

                // EndInvoke method:
                // 1) Has return type attributes from delegate declaration syntax
                // 2) Has parameter attributes from delegate declaration syntax
                //    only for ref/out parameters.
                var endInvokeMethod = (MethodSymbol)delegateType.GetMember("EndInvoke");
                Assert.Equal(1, endInvokeMethod.GetReturnTypeAttributes().Where(a => a.AttributeClass == returnTypeAttrType).Count());
                parameters = endInvokeMethod.GetParameters();
                Assert.Equal(3, parameters.Length);
                Assert.Equal("p2", parameters[0].Name);
                Assert.Equal(1, parameters[0].GetAttributes(paramAttrType).Count());
                Assert.Equal("p3", parameters[1].Name);
                Assert.Equal(1, parameters[1].GetAttributes(paramAttrType).Count());
                Assert.Equal(0, parameters[2].GetAttributes(paramAttrType).Count());
            };

            CompileAndVerify(source, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
        }

        [Fact]
        public void TestAttributesOnDelegates_NoDuplicateDiagnostics()
        {
            string source = @"
public class TypeAttribute : System.Attribute { }
public class ParamAttribute1 : System.Attribute { }
public class ParamAttribute2 : System.Attribute { }
public class ParamAttribute3 : System.Attribute { }
public class ParamAttribute4 : System.Attribute { }
public class ParamAttribute5 : System.Attribute { }
public class ReturnTypeAttribute : System.Attribute { }
public class TypeParamAttribute : System.Attribute { }

class C
{
    [TypeAttribute(0)]
    [return: ReturnTypeAttribute(0)]
    public delegate T Delegate<[TypeParamAttribute(0)]T> ([ParamAttribute1(0)]T p1, [param: ParamAttribute2(0)]ref T p2, [ParamAttribute3(0)]out T p3);

    public delegate int Delegate2 ([ParamAttribute4(0)]int p1 = 0, [param: ParamAttribute5(0)]params int[] p2);
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (13,6): error CS1729: 'TypeAttribute' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "TypeAttribute(0)").WithArguments("TypeAttribute", "1"),
                // (15,33): error CS1729: 'TypeParamAttribute' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "TypeParamAttribute(0)").WithArguments("TypeParamAttribute", "1"),
                // (15,60): error CS1729: 'ParamAttribute1' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ParamAttribute1(0)").WithArguments("ParamAttribute1", "1"),
                // (15,93): error CS1729: 'ParamAttribute2' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ParamAttribute2(0)").WithArguments("ParamAttribute2", "1"),
                // (15,123): error CS1729: 'ParamAttribute3' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ParamAttribute3(0)").WithArguments("ParamAttribute3", "1"),
                // (14,14): error CS1729: 'ReturnTypeAttribute' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ReturnTypeAttribute(0)").WithArguments("ReturnTypeAttribute", "1"),
                // (17,37): error CS1729: 'ParamAttribute4' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ParamAttribute4(0)").WithArguments("ParamAttribute4", "1"),
                // (17,76): error CS1729: 'ParamAttribute5' does not contain a constructor that takes 1 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ParamAttribute5(0)").WithArguments("ParamAttribute5", "1"));
        }

        [Fact]
        public void TestAttributesOnDelegateWithOptionalAndParams()
        {
            string source = @"
using System;

public class ParamAttribute : System.Attribute { }

class C
{
    public delegate int Delegate ([ParamAttribute]int p1 = 0, [param: ParamAttribute]params int[] p2);

    static void Main()
    {
        typeof(Delegate).GetCustomAttributes(false);
    }
}";

            Func<bool, Action<ModuleSymbol>> symbolValidator = isFromSource => moduleSymbol =>
            {
                var type = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var paramAttrType = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("ParamAttribute");

                // Verify delegate type attribute
                var delegateType = type.GetTypeMember("Delegate");

                // Verify delegate methods (return type/parameters) attributes

                // Invoke method has parameter attributes from delegate declaration syntax
                var invokeMethod = (MethodSymbol)delegateType.GetMember("Invoke");
                var parameters = invokeMethod.GetParameters();
                Assert.Equal(2, parameters.Length);
                Assert.Equal("p1", parameters[0].Name);
                Assert.Equal(1, parameters[0].GetAttributes(paramAttrType).Count());
                Assert.Equal("p2", parameters[1].Name);
                Assert.Equal(1, parameters[1].GetAttributes(paramAttrType).Count());

                // verify ParamArrayAttribute on p2
                if (isFromSource)
                {
                    WellKnownAttributesTestBase.VerifyParamArrayAttribute(parameters[1], (SourceModuleSymbol)moduleSymbol);
                }

                // Delegate Constructor: Doesn't have any parameter attributes
                var ctor = (MethodSymbol)delegateType.GetMember(".ctor");
                parameters = ctor.GetParameters();
                Assert.Equal(2, parameters.Length);
                Assert.Equal(0, parameters[0].GetAttributes().Length);
                Assert.Equal(0, parameters[1].GetAttributes().Length);
                Assert.Equal(0, parameters[0].GetSynthesizedAttributes().Length);
                Assert.Equal(0, parameters[1].GetSynthesizedAttributes().Length);

                // BeginInvoke method: Has parameter attributes from delegate declaration parameters syntax
                var beginInvokeMethod = (MethodSymbol)delegateType.GetMember("BeginInvoke");
                parameters = beginInvokeMethod.GetParameters();
                Assert.Equal(4, parameters.Length);
                Assert.Equal("p1", parameters[0].Name);
                Assert.Equal(1, parameters[0].GetAttributes(paramAttrType).Count());
                Assert.Equal("p2", parameters[1].Name);
                Assert.Equal(1, parameters[1].GetAttributes(paramAttrType).Count());
                Assert.Equal(0, parameters[2].GetAttributes(paramAttrType).Count());
                Assert.Equal(0, parameters[3].GetAttributes(paramAttrType).Count());

                // verify no ParamArrayAttribute on p2
                if (isFromSource)
                {
                    WellKnownAttributesTestBase.VerifyParamArrayAttribute(parameters[1], (SourceModuleSymbol)moduleSymbol, expected: false);
                }
            };

            CompileAndVerify(source, sourceSymbolValidator: symbolValidator(true), symbolValidator: symbolValidator(false));
        }

        [Fact]
        public void TestAttributesOnEnumField()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using CustomAttribute;
using AN = CustomAttribute.AttrName;

// Use AttrName without Attribute suffix
[assembly: AN(UShortField = 4321)]
[assembly: AN(UShortField = 1234)]

// TODO: below attribute seems to be an ambiguous attribute specification
// TODO: modify the test assembly to remove ambiguity
// [module: AttrName(TypeField = typeof(System.IO.FileStream))]
namespace AttributeTest
{
    class Foo
    {
        public class NestedClass
        {
            // enum as object
            [AllInheritMultiple(System.IO.FileMode.Open, BindingFlags.DeclaredOnly | BindingFlags.Public, UIntField = 123 * Field)]
            internal const uint Field = 10;
        }
        [AllInheritMultiple(new char[] { 'q', 'c' }, """")]
        [AllInheritMultiple()]
        enum NestedEnum
        {
            zero,
            one = 1,
            [AllInheritMultiple(null, 256, 0f, -1, AryField = new ulong[] { 0, 1, 12345657 })]
            [AllInheritMultipleAttribute(typeof(Dictionary<string, int>), 255 + NestedClass.Field, -0.0001f, 3 - (short)NestedEnum.oneagain)]
            three = 3,
            oneagain = one
        }
    }
}
";

            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull()) };

            var compilation = CreateCompilationWithMscorlib(source, references, options: TestOptions.ReleaseDll);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var attrs = m.GetAttributes();
                // Assert.Equal(1, attrs.Count);
                // Assert.Equal("CustomAttribute.AttrName", attrs[0].AttributeClass.ToDisplayString());
                // attrs[0].VerifyValue<Type>(0, "TypeField", TypedConstantKind.Type, typeof(System.IO.FileStream));

                var assembly = m.ContainingSymbol;
                attrs = assembly.GetAttributes();
                Assert.Equal(2, attrs.Length);
                Assert.Equal("CustomAttribute.AttrName", attrs[0].AttributeClass.ToDisplayString());
                attrs[1].VerifyNamedArgumentValue<ushort>(0, "UShortField", TypedConstantKind.Primitive, 1234);

                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var top = (NamedTypeSymbol)ns.GetMember("Foo");
                var type = top.GetMember<NamedTypeSymbol>("NestedClass");

                var field = type.GetMember<FieldSymbol>("Field");
                attrs = field.GetAttributes();
                Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attrs[0].AttributeClass.ToDisplayString());
                attrs[0].VerifyValue(0, TypedConstantKind.Enum, (int)FileMode.Open);
                attrs[0].VerifyValue(1, TypedConstantKind.Enum, (int)(BindingFlags.DeclaredOnly | BindingFlags.Public));
                attrs[0].VerifyNamedArgumentValue<uint>(0, "UIntField", TypedConstantKind.Primitive, 1230);

                var nenum = top.GetMember<TypeSymbol>("NestedEnum");
                attrs = nenum.GetAttributes();
                Assert.Equal(2, attrs.Length);
                attrs[0].VerifyValue(0, TypedConstantKind.Array, new char[] { 'q', 'c' });
                Assert.Equal(SyntaxKind.Attribute, attrs[0].ApplicationSyntaxReference.GetSyntax().Kind());
                var syntax = (AttributeSyntax)attrs[0].ApplicationSyntaxReference.GetSyntax();
                Assert.Equal(2, syntax.ArgumentList.Arguments.Count());
                syntax = (AttributeSyntax)attrs[1].ApplicationSyntaxReference.GetSyntax();
                Assert.Equal(0, syntax.ArgumentList.Arguments.Count());

                attrs = nenum.GetMember("three").GetAttributes();
                Assert.Equal(2, attrs.Length);
                attrs[0].VerifyValue<object>(0, TypedConstantKind.Primitive, null);
                attrs[0].VerifyValue<long>(1, TypedConstantKind.Primitive, 256);
                attrs[0].VerifyValue<float>(2, TypedConstantKind.Primitive, 0);
                attrs[0].VerifyValue<short>(3, TypedConstantKind.Primitive, -1);
                attrs[0].VerifyNamedArgumentValue<ulong[]>(0, "AryField", TypedConstantKind.Array, new ulong[] { 0, 1, 12345657 });

                attrs[1].VerifyValue<object>(0, TypedConstantKind.Type, typeof(Dictionary<string, int>));
                attrs[1].VerifyValue<long>(1, TypedConstantKind.Primitive, 265);
                attrs[1].VerifyValue<float>(2, TypedConstantKind.Primitive, -0.0001f);
                attrs[1].VerifyValue<short>(3, TypedConstantKind.Primitive, 2);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [Fact]
        public void TestAttributesOnDelegate()
        {
            string source = @"
using System;
using System.Collections.Generic;
using CustomAttribute;

namespace AttributeTest
{
    public class Foo
    {
        [AllInheritMultiple(new object[] { 0, """", null }, 255, -127 - 1, AryProp = new object[] { new object[] { """", typeof(IList<string>) } })]
        public delegate void NestedSubDele([AllInheritMultiple()]string p1, [Derived(typeof(string[, ,]))]string p2);
    }
}
";

            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("Foo");

                var dele = (NamedTypeSymbol)type.GetTypeMember("NestedSubDele");
                var attrs = dele.GetAttributes();
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Array, new object[] { 0, "", null });
                attrs.First().VerifyValue<byte>(1, TypedConstantKind.Primitive, 255);
                attrs.First().VerifyValue<sbyte>(2, TypedConstantKind.Primitive, -128);
                attrs.First().VerifyNamedArgumentValue<object[]>(0, "AryProp", TypedConstantKind.Array, new object[] { new object[] { "", typeof(IList<string>) } });
                var mem = dele.GetMember<MethodSymbol>("Invoke");
                attrs = mem.Parameters[0].GetAttributes();
                Assert.Equal(1, attrs.Length);
                attrs = mem.Parameters[1].GetAttributes();
                Assert.Equal(1, attrs.Length);
                attrs[0].VerifyValue<object>(0, TypedConstantKind.Type, typeof(string[,,]));
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);
        }

        [Fact]
        public void TestAttributesUseBaseAttributeField()
        {
            string source = @"
using System;
namespace AttributeTest
{
    public interface IFoo
    {
        [CustomAttribute.Derived(new object[] { 1, null, ""Hi"" }, ObjectField = 2)]
        int F(int p);
    }
}
";
            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull()) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("IFoo");
                var attrs = type.GetMember<MethodSymbol>("F").GetAttributes();

                Assert.Equal(@"CustomAttribute.DerivedAttribute({1, null, ""Hi""}, ObjectField = 2)", attrs.First().ToString());
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Array, new object[] { 1, null, "Hi" });
                attrs.First().VerifyNamedArgumentValue<object>(0, "ObjectField", TypedConstantKind.Primitive, 2);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(688007, "DevDiv")]
        [Fact]
        public void Bug688007a()
        {
            string source = @"
using System;
using X;
using Z;

namespace X
{
    public class AttrAttribute : Attribute
    {
    }
}

namespace Z
{
    public class Attr
    {
    }
}

[Attr()]
partial class CDoc
{
    static void Main(string[] args)
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

            var globalNs = compilation.GlobalNamespace;
            var cDoc = globalNs.GetTypeMember("CDoc");
            Assert.NotNull(cDoc);

            var attrs = cDoc.GetAttributes();
            Assert.Equal(1, attrs.Length);
            Assert.Equal("X.AttrAttribute", attrs[0].AttributeClass.ToDisplayString());

            CompileAndVerify(compilation).VerifyDiagnostics();
        }

        [WorkItem(688007, "DevDiv")]
        [Fact]
        public void Bug688007b()
        {
            string source = @"
using System;
using X;
using Z;

namespace X
{
    public class AttrAttribute : Attribute
    {
    }

    public class Attr : Attribute
    {
    }
}

namespace Z
{
    public class Attr : Attribute
    {
    }
}

[Attr()]
partial class CDoc
{
    static void Main(string[] args)
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

            var globalNs = compilation.GlobalNamespace;
            var cDoc = globalNs.GetTypeMember("CDoc");
            Assert.NotNull(cDoc);

            var attrs = cDoc.GetAttributes();
            Assert.Equal(1, attrs.Length);
            Assert.Equal("X.AttrAttribute", attrs[0].AttributeClass.ToDisplayString());

            CompileAndVerify(compilation).VerifyDiagnostics();
        }

        [WorkItem(688007, "DevDiv")]
        [Fact]
        public void Bug688007c()
        {
            string source = @"
using System;
using X;
using Y;
using Z;

namespace X
{
    public class AttrAttribute /*: Attribute*/
    {
    }
}

namespace Y
{
    public class AttrAttribute /*: Attribute*/
    {
    }
}

namespace Z
{
    public class Attr : Attribute
    {
    }
}

[Attr()]
partial class CDoc
{
    static void Main(string[] args)
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

            var globalNs = compilation.GlobalNamespace;
            var cDoc = globalNs.GetTypeMember("CDoc");
            Assert.NotNull(cDoc);

            var attrs = cDoc.GetAttributes();
            Assert.Equal(1, attrs.Length);
            Assert.Equal("Z.Attr", attrs[0].AttributeClass.ToDisplayString());

            CompileAndVerify(compilation).VerifyDiagnostics();
        }

        [WorkItem(688007, "DevDiv")]
        [Fact]
        public void Bug688007d()
        {
            string source = @"
using System;
using X;
using Y;
using Z;

namespace X
{
    public class AttrAttribute : Attribute
    {
    }
}

namespace Y
{
    public class AttrAttribute : Attribute
    {
    }
}

namespace Z
{
    public class Attr : Attribute
    {
    }
}

[Attr()]
partial class CDoc
{
    static void Main(string[] args)
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

            var globalNs = compilation.GlobalNamespace;
            var cDoc = globalNs.GetTypeMember("CDoc");
            Assert.NotNull(cDoc);

            var attrs = cDoc.GetAttributes();
            Assert.Equal(1, attrs.Length);
            Assert.Equal("Z.Attr", attrs[0].AttributeClass.ToDisplayString());
            var syntax = attrs.Single().ApplicationSyntaxReference.GetSyntax();
            Assert.NotNull(syntax);
            Assert.IsType<AttributeSyntax>(syntax);

            CompileAndVerify(compilation).VerifyDiagnostics();
        }

        [Fact]
        public void TestAttributesWithParamArrayInCtor01()
        {
            string source = @"
using System;
using CustomAttribute;

namespace AttributeTest
{
    [AllInheritMultiple(new char[] { ' '}, """")]
    public interface IFoo
    {
    }
}
";
            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull()) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            Action<ModuleSymbol> sourceAttributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("IFoo");
                var attrs = type.GetAttributes();
                attrs.First().VerifyValue<char[]>(0, TypedConstantKind.Array, new char[] { ' ' });
                attrs.First().VerifyValue<string[]>(1, TypedConstantKind.Array, new string[] { "" });

                Assert.True(attrs.First().AttributeConstructor.Parameters.Last().IsParams);
            };

            Action<ModuleSymbol> mdAttributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("IFoo");
                var attrs = type.GetAttributes();
                attrs.First().VerifyValue<char[]>(0, TypedConstantKind.Array, new char[] { ' ' });
                attrs.First().VerifyValue<string[]>(1, TypedConstantKind.Array, new string[] { "" });
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: sourceAttributeValidator, symbolValidator: mdAttributeValidator);
        }

        [Fact]
        public void TestAttributesWithParamArrayInCtor02()
        {
            string source = @"
using System;
namespace AttributeTest
{
    class ExampleAttribute : Attribute
    {
        public int[] Numbers;
        public ExampleAttribute(string message, params int[] numbers)
        {
            Numbers = numbers;
        }
    }
    class Program
    {
        [Example(""MultipleArgumentsToParamsParameter"", 4, 5, 6)]
        public void MultipleArgumentsToParamsParameter() { }

        [Example(""NoArgumentsToParamsParameter"")]
        public void NoArgumentsToParamsParameter() { }

        [Example(""NullArgumentToParamsParameter"", null)]
        public void NullArgumentToParamsParameter() { }

        static void Main()
        {
            ExampleAttribute att = null;
            try
            {
                var programType = typeof(Program);
                var method = programType.GetMember(""MultipleArgumentsToParamsParameter"")[0];
                att = (ExampleAttribute)method.GetCustomAttributes(typeof(ExampleAttribute), false)[0];

                method = programType.GetMember(""NoArgumentsToParamsParameter"")[0];
                att = (ExampleAttribute)method.GetCustomAttributes(typeof(ExampleAttribute), false)[0];

                method = programType.GetMember(""NullArgumentToParamsParameter"")[0];
                att = (ExampleAttribute)method.GetCustomAttributes(typeof(ExampleAttribute), false)[0];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Console.WriteLine(true);
        }
    }
}
";
            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("Program");
                var attributeClass = (NamedTypeSymbol)ns.GetMember("ExampleAttribute");

                var method = (MethodSymbol)type.GetMember("MultipleArgumentsToParamsParameter");
                var attrs = method.GetAttributes(attributeClass);
                var attr = attrs.Single();
                Assert.Equal(2, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<string>(0, TypedConstantKind.Primitive, "MultipleArgumentsToParamsParameter");
                attr.VerifyValue<int[]>(1, TypedConstantKind.Array, new int[] { 4, 5, 6 });

                method = (MethodSymbol)type.GetMember("NoArgumentsToParamsParameter");
                attrs = method.GetAttributes(attributeClass);
                attr = attrs.Single();
                Assert.Equal(2, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<string>(0, TypedConstantKind.Primitive, "NoArgumentsToParamsParameter");
                attr.VerifyValue<int[]>(1, TypedConstantKind.Array, new int[] { });

                method = (MethodSymbol)type.GetMember("NullArgumentToParamsParameter");
                attrs = method.GetAttributes(attributeClass);
                attr = attrs.Single();
                Assert.Equal(2, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<string>(0, TypedConstantKind.Primitive, "NullArgumentToParamsParameter");
                attr.VerifyValue<int[]>(1, TypedConstantKind.Array, null);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            var compVerifier = CompileAndVerify(
                source,
                sourceSymbolValidator: attributeValidator,
                symbolValidator: attributeValidator,
                expectedOutput: "True\r\n",
                expectedSignatures: new[]
                {
                    Signature("AttributeTest.Program", "MultipleArgumentsToParamsParameter", ".method [AttributeTest.ExampleAttribute(\"MultipleArgumentsToParamsParameter\", System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] public hidebysig instance System.Void MultipleArgumentsToParamsParameter() cil managed"),
                    Signature("AttributeTest.Program", "NoArgumentsToParamsParameter", ".method [AttributeTest.ExampleAttribute(\"NoArgumentsToParamsParameter\", System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] public hidebysig instance System.Void NoArgumentsToParamsParameter() cil managed"),
                    Signature("AttributeTest.Program", "NullArgumentToParamsParameter", ".method [AttributeTest.ExampleAttribute(\"NullArgumentToParamsParameter\", )] public hidebysig instance System.Void NullArgumentToParamsParameter() cil managed"),
                });
        }

        [Fact, WorkItem(531385, "DevDiv")]
        public void TestAttributesWithParamArrayInCtor3()
        {
            string source = @"
using System;
using CustomAttribute;

namespace AttributeTest
{
    [AllInheritMultiple(new char[] { ' ' }, new string[] { ""whatever"" })]
    public interface IFoo
    {
    }
}
";
            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull()) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            Action<ModuleSymbol> sourceAttributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("IFoo");
                var attrs = type.GetAttributes();
                attrs.First().VerifyValue<char[]>(0, TypedConstantKind.Array, new char[] { ' ' });
                attrs.First().VerifyValue<string[]>(1, TypedConstantKind.Array, new string[] { "whatever" });

                Assert.True(attrs.First().AttributeConstructor.Parameters.Last().IsParams);
            };

            Action<ModuleSymbol> mdAttributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("IFoo");
                var attrs = type.GetAttributes();
                attrs.First().VerifyValue<char[]>(0, TypedConstantKind.Array, new char[] { ' ' });
                attrs.First().VerifyValue<string[]>(1, TypedConstantKind.Array, new string[] { "whatever" });
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: sourceAttributeValidator, symbolValidator: mdAttributeValidator);
        }

        [Fact]
        public void TestAttributeSpecifiedOnItself()
        {
            string source = @"
using System;

namespace AttributeTest
{
    [MyAttribute(typeof(object))]
    public class MyAttribute : Attribute
    {
        public MyAttribute(Type t)
        {
        }
        public static void Main()
        {
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("MyAttribute");
                var attrs = type.GetAttributes();
                Assert.Equal(1, attrs.Length);
                attrs.First().VerifyValue(0, TypedConstantKind.Type, typeof(Object));
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);
        }

        [Fact]
        public void TestAttributesWithEnumArrayInCtor()
        {
            string source = @"
using System;

namespace AttributeTest
{
    public enum X
    {
        a,
        b
    };

    public class Y : Attribute
    {
        public int f;
        public Y(X[] x) { }
    }

    [Y(A.x)]
    public class A
    {
        public const X[] x = null;
        public static void Main() { }
    }
}
";
            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull()) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("A");
                var attrs = type.GetAttributes();
                attrs.First().VerifyValue(0, TypedConstantKind.Array, (object[])null);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(541058, "DevDiv")]
        [Fact]
        public void TestAttributesWithTypeof()
        {
            string source = @"
using System;
[MyAttribute(typeof(object))]
public class MyAttribute : Attribute
{
    public MyAttribute(Type t)
    {
    }
    public static void Main()
    {
    }
}
";
            CompileAndVerify(source);
        }

        [WorkItem(541071, "DevDiv")]
        [Fact]
        public void TestAttributesWithParams()
        {
            string source = @"
using System;
class ExampleAttribute : Attribute
{
    public int[] Numbers;
    public ExampleAttribute(string message, params int[] numbers)
    {
        Numbers = numbers;
    }
}
[Example(""wibble"", 4, 5, 6)]
class Program
{
    static void Main()
    {
        ExampleAttribute att = null;
        try
        {
            att = (ExampleAttribute)typeof(Program).GetCustomAttributes(typeof(ExampleAttribute), false)[0];
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return;
        }
            Console.WriteLine(true);
    }
}
";
            var expectedOutput = @"True";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestAttributesOnReturnType()
        {
            string source = @"
using System;
using CustomAttribute;

namespace AttributeTest
{    
    public class Foo
    {
        int p;
        public int Property
        {
            [return: AllInheritMultipleAttribute()]
            [AllInheritMultipleAttribute()]
            get { return p; }
            [return: AllInheritMultipleAttribute()]
            [AllInheritMultipleAttribute()]
            set { p = value; }
        }

        [return: AllInheritMultipleAttribute()]
        [return: AllInheritMultipleAttribute()]
        public int Method() { return p; }

        [return: AllInheritMultipleAttribute()]          
        public delegate void Delegate();

        public static void Main() {}
    }
}
";
            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull()) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("Foo");

                var property = (PropertySymbol)type.GetMember("Property");
                var getter = property.GetMethod;
                var attrs = getter.GetReturnTypeAttributes();
                Assert.Equal(1, attrs.Length);
                var attr = attrs.First();
                Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attr.AttributeClass.ToDisplayString());

                var setter = property.SetMethod;
                attrs = setter.GetReturnTypeAttributes();
                Assert.Equal(1, attrs.Length);
                attr = attrs.First();
                Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attr.AttributeClass.ToDisplayString());

                var method = (MethodSymbol)type.GetMember("Method");
                attrs = method.GetReturnTypeAttributes();
                Assert.Equal(2, attrs.Length);
                attr = attrs.First();
                Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attr.AttributeClass.ToDisplayString());
                attr = attrs.Last();
                Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attr.AttributeClass.ToDisplayString());

                var delegateType = type.GetTypeMember("Delegate");
                var invokeMethod = (MethodSymbol)delegateType.GetMember("Invoke");
                attrs = invokeMethod.GetReturnTypeAttributes();
                Assert.Equal(1, attrs.Length);
                attr = attrs.First();
                Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attr.AttributeClass.ToDisplayString());

                var ctor = (MethodSymbol)delegateType.GetMember(".ctor");
                attrs = ctor.GetReturnTypeAttributes();
                Assert.Equal(0, attrs.Length);

                var beginInvokeMethod = (MethodSymbol)delegateType.GetMember("BeginInvoke");
                attrs = beginInvokeMethod.GetReturnTypeAttributes();
                Assert.Equal(0, attrs.Length);

                var endInvokeMethod = (MethodSymbol)delegateType.GetMember("EndInvoke");
                attrs = endInvokeMethod.GetReturnTypeAttributes();
                Assert.Equal(1, attrs.Length);
                attr = attrs.First();
                Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attr.AttributeClass.ToDisplayString());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(541397, "DevDiv")]
        [Fact]
        public void TestAttributeWithSameNameAsTypeParameter()
        {
            string source = @"
using System;

namespace AttributeTest
{
    public class TAttribute : Attribute { }
    public class RAttribute : TAttribute { }
    public class GClass<T>
    {
        [T]
        public enum E { }
        [R]
        internal R M<R>() { return default(R); }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("GClass");
                var enumType = (NamedTypeSymbol)type.GetTypeMember("E");
                var attributeType = (NamedTypeSymbol)ns.GetMember("TAttribute");
                var attributeType2 = (NamedTypeSymbol)ns.GetMember("RAttribute");
                var genMethod = (MethodSymbol)type.GetMember("M");

                var attrs = enumType.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());

                attrs = genMethod.GetAttributes(attributeType2);
                Assert.Equal(1, attrs.Count());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(541615, "DevDiv")]
        [Fact]
        public void TestAttributeWithVarIdentifierName()
        {
            string source = @"
using System;

namespace AttributeTest
{
    public class var: Attribute { }

    [var]
    class Program
    {
        public static void Main() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("Program");
                var attributeType = (NamedTypeSymbol)ns.GetMember("var");

                var attrs = type.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());

                var attr = attrs.First();
                Assert.Equal("AttributeTest.var", attr.ToString());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(541505, "DevDiv")]
        [Fact]
        public void AttributeArgumentBind_PropertyWithSameName()
        {
            var source =
@"using System;
namespace AttributeTest
{
    class TestAttribute : Attribute
    {
      public TestAttribute(ProtectionLevel p){}
    }

    enum ProtectionLevel
    {
      Privacy = 0
    }

    class TestClass
    {
      ProtectionLevel ProtectionLevel { get { return ProtectionLevel.Privacy; } }

      [TestAttribute(ProtectionLevel.Privacy)]
      public int testField;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("TestClass");

                var attributeType = (NamedTypeSymbol)ns.GetMember("TestAttribute");

                var field = (FieldSymbol)type.GetMember("testField");
                var attrs = field.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(541709, "DevDiv")]
        [Fact]
        public void AttributeOnSynthesizedParameterSymbol()
        {
            var source =
@"using System;
namespace AttributeTest
{
    public class TestAttributeForMethod : System.Attribute { }
    public class TestAttributeForParam : System.Attribute { }
    public class TestAttributeForReturn : System.Attribute { }
    
    class TestClass
    {
        int P1
        {
            [TestAttributeForMethod]
            [param: TestAttributeForParam]
            [return: TestAttributeForReturn]
            set { }
        }

        int P2
        {
            [TestAttributeForMethod]
            [return: TestAttributeForReturn]
            get { return 0; }
        }

        public static void Main() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("TestClass");

                var attributeTypeForMethod = (NamedTypeSymbol)ns.GetMember("TestAttributeForMethod");
                var attributeTypeForParam = (NamedTypeSymbol)ns.GetMember("TestAttributeForParam");
                var attributeTypeForReturn = (NamedTypeSymbol)ns.GetMember("TestAttributeForReturn");

                var property = (PropertySymbol)type.GetMember("P1");
                var setter = property.SetMethod;

                var attrs = setter.GetAttributes(attributeTypeForMethod);
                Assert.Equal(1, attrs.Count());
                var attr = attrs.First();
                Assert.Equal("AttributeTest.TestAttributeForMethod", attr.AttributeClass.ToDisplayString());

                Assert.Equal(1, setter.ParameterCount);
                attrs = setter.Parameters[0].GetAttributes(attributeTypeForParam);
                Assert.Equal(1, attrs.Count());
                attr = attrs.First();
                Assert.Equal("AttributeTest.TestAttributeForParam", attr.AttributeClass.ToDisplayString());

                attrs = setter.GetReturnTypeAttributes().Where(a => a.AttributeClass == attributeTypeForReturn);
                Assert.Equal(1, attrs.Count());
                attr = attrs.First();
                Assert.Equal("AttributeTest.TestAttributeForReturn", attr.AttributeClass.ToDisplayString());


                property = (PropertySymbol)type.GetMember("P2");
                var getter = property.GetMethod;

                attrs = getter.GetAttributes(attributeTypeForMethod);
                Assert.Equal(1, attrs.Count());
                attr = attrs.First();
                Assert.Equal("AttributeTest.TestAttributeForMethod", attr.AttributeClass.ToDisplayString());

                attrs = getter.GetReturnTypeAttributes().Where(a => a.AttributeClass == attributeTypeForReturn);
                Assert.Equal(1, attrs.Count());
                attr = attrs.First();
                Assert.Equal("AttributeTest.TestAttributeForReturn", attr.AttributeClass.ToDisplayString());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [Fact]
        public void TestAttributeStringForEnumTypedConstant()
        {
            var source = CreateCompilationWithMscorlib(@"
using System;
namespace AttributeTest
{
    enum X
    {
        One = 1,
        Two = 2,
        Three = 3
    };

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Event, Inherited = false, AllowMultiple = true)]
    class A : System.Attribute 
    {
        public A(X x) { }

        public static void Main() { }

        // AttributeData.ToString() should display 'X.Three' not 'X.One | X.Two'
        [A(X.Three)]
        int field;

        // AttributeData.ToString() should display '5'
        [A((X)5)]
        int field2;
    }   
}
");

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("A");

                var attrs = type.GetAttributes();
                Assert.Equal(1, attrs.Length);

                var attr = attrs.First();

                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue(0, TypedConstantKind.Enum, (int)(AttributeTargets.Field | AttributeTargets.Event));

                Assert.Equal(2, attr.CommonNamedArguments.Length);
                attr.VerifyNamedArgumentValue(0, "Inherited", TypedConstantKind.Primitive, false);
                attr.VerifyNamedArgumentValue(1, "AllowMultiple", TypedConstantKind.Primitive, true);

                Assert.Equal(@"System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Event, Inherited = false, AllowMultiple = true)", attr.ToString());

                var fieldSymbol = (FieldSymbol)type.GetMember("field");

                attrs = fieldSymbol.GetAttributes();
                Assert.Equal(1, attrs.Length);
                Assert.Equal(@"AttributeTest.A(AttributeTest.X.Three)", attrs.First().ToString());

                fieldSymbol = (FieldSymbol)type.GetMember("field2");

                attrs = fieldSymbol.GetAttributes();
                Assert.Equal(1, attrs.Length);
                Assert.Equal(@"AttributeTest.A(5)", attrs.First().ToString());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [Fact]
        public void TestAttributesWithNamedConstructorArguments_01()
        {
            string source = @"
using System;
 
namespace AttributeTest
{
    [A(y:4, z:5, X = 6)]
    public class A : Attribute
    {
        public int X;
        public A(int y, int z) { Console.WriteLine(y); Console.WriteLine(z); }
    
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("A");
                var attrs = type.GetAttributes();

                attrs.First().VerifyValue(0, TypedConstantKind.Primitive, 4);
                attrs.First().VerifyValue(1, TypedConstantKind.Primitive, 5);

                attrs.First().VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, 6);
            };

            string expectedOutput = @"4
5
";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestAttributesWithNamedConstructorArguments_02()
        {
            string source = @"
using System;
 
namespace AttributeTest
{
    [A(3, z:5, y:4, X = 6)]
    public class A : Attribute
    {
        public int X;
        public A(int x, int y, int z) { Console.WriteLine(x); Console.WriteLine(y); Console.WriteLine(z); }
    
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("A");
                var attrs = type.GetAttributes();

                attrs.First().VerifyValue(0, TypedConstantKind.Primitive, 3);
                attrs.First().VerifyValue(1, TypedConstantKind.Primitive, 4);
                attrs.First().VerifyValue(2, TypedConstantKind.Primitive, 5);

                attrs.First().VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, 6);
            };

            string expectedOutput = @"3
4
5
";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [WorkItem(541864, "DevDiv")]
        [Fact]
        public void Bug_8769_TestAttributesWithNamedConstructorArguments()
        {
            string source = @"
using System;
 
namespace AttributeTest
{
    [A(y: 1, x: 2)]
    public class A : Attribute
    {
        public A(int x, int y) { Console.WriteLine(x); Console.WriteLine(y); }
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("A");
                var attrs = type.GetAttributes();

                Assert.Equal(2, attrs.First().CommonConstructorArguments.Length);

                attrs.First().VerifyValue(0, TypedConstantKind.Primitive, 2);
                attrs.First().VerifyValue(1, TypedConstantKind.Primitive, 1);

                Assert.Equal(0, attrs.First().CommonNamedArguments.Length);
            };

            string expectedOutput = @"2
1
";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestAttributesWithOptionalConstructorArguments_01()
        {
            string source = @"
using System;
 
namespace AttributeTest
{
    [A(3, z:5, X = 6)]
    public class A : Attribute
    {
        public int X;
        public A(int x, int y = 4, int z = 0) { Console.WriteLine(x); Console.WriteLine(y); Console.WriteLine(z); }
    
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("A");
                var attrs = type.GetAttributes();

                attrs.First().VerifyValue(0, TypedConstantKind.Primitive, 3);
                attrs.First().VerifyValue(1, TypedConstantKind.Primitive, 4);
                attrs.First().VerifyValue(2, TypedConstantKind.Primitive, 5);

                attrs.First().VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, 6);
            };

            string expectedOutput = @"3
4
5
";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [WorkItem(541861, "DevDiv")]
        [Fact]
        public void Bug_8768_TestAttributesWithOptionalConstructorArguments()
        {
            string source = @"
using System;
 
namespace AttributeTest
{
    [A]
    public class A : Attribute
    {
        public A(int x = 2) { Console.Write(x); }
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("A");
                var attrs = type.GetAttributes();

                var attr = attrs.First();
                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<int>(0, TypedConstantKind.Primitive, 2);

                Assert.Equal(0, attrs.First().CommonNamedArguments.Length);
            };

            string expectedOutput = @"2";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [WorkItem(541854, "DevDiv")]
        [Fact]
        public void Bug8761_StringArrayArgument()
        {
            var source =
@"using System;
 
[A(X = new string[] { """" })]
public class A : Attribute
{
    public object[] X;
 
    static void Main()
    {
        typeof(A).GetCustomAttributes(false);
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [WorkItem(541856, "DevDiv")]
        [Fact]
        public void Bug8763_NullInArrayInitializer()
        {
            var source =
@"using System;
 
[A(X = new object[] { null })]
public class A : Attribute
{
    public object[] X;
 
    static void Main()
    {
        typeof(A).GetCustomAttributes(false);
        typeof(B).GetCustomAttributes(false);
    }
}

[A(X = new object[] { typeof(int), typeof(System.Type), 1, null, ""hi"" })]
public class B
{
    public object[] X;
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [WorkItem(541856, "DevDiv")]
        [Fact]
        public void AttributeArrayTypeArgument()
        {
            var source =
@"using System;
 
[A(objArray = new string[] { ""a"", null })]
public class A : Attribute
{
    public object[] objArray;
    public object obj;

    static void Main()
    {
        typeof(A).GetCustomAttributes(false);
        typeof(B).GetCustomAttributes(false);
        typeof(C).GetCustomAttributes(false);
        typeof(D).GetCustomAttributes(false);
        typeof(E).GetCustomAttributes(false);
        typeof(F).GetCustomAttributes(false);
        typeof(G).GetCustomAttributes(false);
        typeof(H).GetCustomAttributes(false);
        typeof(I).GetCustomAttributes(false);
    }
}

[A(objArray = new object[] { ""a"", null, 3 })]
public class B
{
}

/*
CS0029: Cannot implicitly convert type 'int[]' to 'object[]'

[A(objArray = new int[] { 3 })]
public class Error
{
}
*/

[A(objArray = null)]
public class C
{
}

[A(obj = new string[] { ""a"" })]
public class D
{
}

[A(obj = new object[] { ""a"", null, 3 })]
public class E
{
}

[A(obj = new int[] { 1 })]
public class F
{
}

[A(obj = 1)]
public class G
{
}

[A(obj = ""a"")]
public class H
{
}

[A(obj = null)]
public class I
{
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        [WorkItem(541859, "DevDiv")]
        [Fact]
        public void Bug8766_AttributeCtorOverloadResolution()
        {
            var source =
@"using System;
 
[A(C)]
public class A : Attribute
{
    const int C = 1;
 
    A(int x) { Console.Write(""int""); }
 
    public A(long x) { Console.Write(""long""); }
 
    static void Main()
    {
        typeof(A).GetCustomAttributes(false);
    }
}
";
            CompileAndVerify(source, expectedOutput: "int");
        }

        [WorkItem(541876, "DevDiv")]
        [Fact]
        public void Bug8771_AttributeArgumentNameBinding()
        {
            var source =
@"using System;
 
public class A : Attribute
{
    public A(int x) { Console.WriteLine(x); }
}
 
class B
{
    const int X = 1;
 
    [A(X)]
    class C<[A(X)] T>
    {
        const int X = 2;
    }

    static void Main()
    {
        typeof(C<>).GetCustomAttributes(false);
        typeof(C<>).GetGenericArguments()[0].GetCustomAttributes(false);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol bClass = m.GlobalNamespace.GetTypeMember("B");
                NamedTypeSymbol cClass = bClass.GetTypeMember("C");

                NamedTypeSymbol attributeType = m.GlobalNamespace.GetTypeMember("A");

                var attrs = cClass.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
                attrs.First().VerifyValue(0, TypedConstantKind.Primitive, 2);

                var typeParameters = cClass.TypeParameters;
                Assert.Equal(1, typeParameters.Length);
                attrs = typeParameters[0].GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
                attrs.First().VerifyValue(0, TypedConstantKind.Primitive, 2);
            };

            string expectedOutput = @"2
2
";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [WorkItem(546380, "DevDiv")]
        [Fact]
        public void AttributeWithNestedUnboundGenericType()
        {
            var source =
@"using System;
using System.Collections.Generic;

public class A : Attribute
{
    public A(object o) { }
}

[A(typeof(B<>.C))] 
public class B<T>
{
    public class C
    {
    }
}

public class Program
{
    static void Main(string[] args)
    {
        
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol bClass = m.GlobalNamespace.GetTypeMember("B");
                NamedTypeSymbol cClass = bClass.GetTypeMember("C");
                NamedTypeSymbol attributeType = m.GlobalNamespace.GetTypeMember("A");

                var attrs = bClass.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
                attrs.First().VerifyValue(0, TypedConstantKind.Type, cClass.AsUnboundGenericType());
            };


            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);
        }

        [WorkItem(546380, "DevDiv")]
        [Fact]
        public void AttributeWithUnboundGenericType()
        {
            var source =
@"using System;
using System.Collections.Generic;

public class A : Attribute
{
    public A(object o) { }
}

[A(typeof(B<>))] 
public class B<T>
{
}

class Program
{
    static void Main(string[] args)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol bClass = m.GlobalNamespace.GetTypeMember("B");
                NamedTypeSymbol attributeType = m.GlobalNamespace.GetTypeMember("A");

                var attrs = bClass.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
                attrs.First().VerifyValue(0, TypedConstantKind.Type, bClass.AsUnboundGenericType());
            };


            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);
        }

        [WorkItem(542223, "DevDiv")]
        [Fact]
        public void AttributeArgumentAsEnumFromMetadata()
        {
            var metadataStream1 = CSharpCompilation.Create("bar.dll",
                references: new[] { MscorlibRef },
                syntaxTrees: new[] { Parse("public enum Bar { Baz }") }).EmitToStream(options: new EmitOptions(metadataOnly: true));

            var ref1 = MetadataReference.CreateFromStream(metadataStream1);

            var metadataStream2 = CSharpCompilation.Create("foo.dll", references: new[] { MscorlibRef, ref1 },
                syntaxTrees: new[] {
                    SyntaxFactory.ParseSyntaxTree(
                        "public class Ca : System.Attribute { public Ca(object o) { } } " +
                        "[Ca(Bar.Baz)]" +
                        "public class Foo { }") }).EmitToStream(options: new EmitOptions(metadataOnly: true));

            var ref2 = MetadataReference.CreateFromStream(metadataStream2);

            var compilation = CSharpCompilation.Create("moo.dll", references: new[] { MscorlibRef, ref1, ref2 });

            var foo = compilation.GetTypeByMetadataName("Foo");
            var ca = foo.GetAttributes().First().CommonConstructorArguments.First();

            Assert.Equal("Bar", ca.Type.Name);
        }

        [WorkItem(542318, "DevDiv")]
        [Fact]
        public void AttributeWithDaysOfWeekArgument()
        {
            // DELIBERATE SPEC VIOLATION:
            //
            // Object creation expressions like "new int()" are not considered constant expressions
            // by the specification but they are by the native compiler; we maintain compatibility
            // with this bug.
            // 
            // Additionally, it also treats "new X()", where X is an enum type, as a
            // constant expression with default value 0, we maintaining compatibility with it.

            var source =
@"using System;
 
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
[A(X = new DayOfWeek())]
[A(X = new bool())]
[A(X = new sbyte())]
[A(X = new byte())]
[A(X = new short())]
[A(X = new ushort())]
[A(X = new int())]
[A(X = new uint())]
[A(X = new char())]
[A(X = new float())]
[A(X = new Single())]
[A(X = new double())]
public class A : Attribute
{
    public object X;

    const DayOfWeek dayofweek = new DayOfWeek();
    const bool b = new bool();
    const sbyte sb = new sbyte();
    const byte by = new byte();
    const short s = new short();
    const ushort us = new ushort();
    const int i = new int();
    const uint ui = new uint();
    const char c = new char();
    const float f = new float();
    const Single si = new Single();
    const double d = new double();
    
    public static void Main()
    {
        typeof(A).GetCustomAttributes(false);
    }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol attributeType = m.GlobalNamespace.GetTypeMember("A");

                var attrs = attributeType.GetAttributes(attributeType);
                Assert.Equal(12, attrs.Count());
                var enumerator = attrs.GetEnumerator();
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Enum, (int)new DayOfWeek());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new bool());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new sbyte());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new byte());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new short());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new ushort());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new int());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new uint());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new char());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new float());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new Single());
                enumerator.MoveNext();
                enumerator.Current.VerifyNamedArgumentValue(0, "X", TypedConstantKind.Primitive, new double());
            };

            string expectedOutput = "";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [WorkItem(542534, "DevDiv")]
        [Fact]
        public void AttributeOnDefiningPartialMethodDeclaration()
        {
            var source =
@"
using System;

class A : Attribute { }
partial class Program
{
    [A] 
    static partial void Foo();
    static partial void Foo() { }

    static void Main()
    {
        Console.WriteLine(((Action) Foo).Method.GetCustomAttributesData().Count);
    }
}
";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(542534, "DevDiv")]
        [Fact]
        public void AttributeOnDefiningPartialMethodDeclaration_02()
        {
            var source1 = @"
using System;
class A1 : Attribute {}
class B1 : Attribute {}
class C1 : Attribute {}
class D1 : Attribute {}
class E1 : Attribute {}

partial class Program
{
    [A1]
    [return: B1]
    static partial void Foo<[C1] T, [D1] U>([E1]int x);
}
";

            var source2 =
@"
using System;

class A2 : Attribute {}
class B2 : Attribute {}
class C2 : Attribute {}
class D2 : Attribute {}
class E2 : Attribute {}

partial class Program
{
    [A2]
    [return: B2]
    static partial void Foo<[C2] U, [D2] T>([E2]int y) { }

    static void Main()
    {}
}
";

            var compilation = CreateCompilationWithMscorlib(new[] { source1, source2 }, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var programClass = m.GlobalNamespace.GetTypeMember("Program");
                var fooMethod = (MethodSymbol)programClass.GetMember("Foo");

                TestAttributeOnPartialMethodHelper(m, fooMethod);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: "");
        }

        private void TestAttributeOnPartialMethodHelper(ModuleSymbol m, MethodSymbol fooMethod)
        {
            var a1Class = m.GlobalNamespace.GetTypeMember("A1");
            var a2Class = m.GlobalNamespace.GetTypeMember("A2");
            var b1Class = m.GlobalNamespace.GetTypeMember("B1");
            var b2Class = m.GlobalNamespace.GetTypeMember("B2");
            var c1Class = m.GlobalNamespace.GetTypeMember("C1");
            var c2Class = m.GlobalNamespace.GetTypeMember("C2");
            var d1Class = m.GlobalNamespace.GetTypeMember("D1");
            var d2Class = m.GlobalNamespace.GetTypeMember("D2");
            var e1Class = m.GlobalNamespace.GetTypeMember("E1");
            var e2Class = m.GlobalNamespace.GetTypeMember("E2");

            Assert.Equal(1, fooMethod.GetAttributes(a1Class).Count());
            Assert.Equal(1, fooMethod.GetAttributes(a2Class).Count());

            Assert.Equal(1, fooMethod.GetReturnTypeAttributes().Where(a => a.AttributeClass == b1Class).Count());
            Assert.Equal(1, fooMethod.GetReturnTypeAttributes().Where(a => a.AttributeClass == b2Class).Count());

            var typeParam1 = fooMethod.TypeParameters[0];
            Assert.Equal(1, typeParam1.GetAttributes(c1Class).Count());
            Assert.Equal(1, typeParam1.GetAttributes(c2Class).Count());

            var typeParam2 = fooMethod.TypeParameters[1];
            Assert.Equal(1, typeParam2.GetAttributes(d1Class).Count());
            Assert.Equal(1, typeParam2.GetAttributes(d2Class).Count());

            var param = fooMethod.Parameters[0];
            Assert.Equal(1, param.GetAttributes(e1Class).Count());
            Assert.Equal(1, param.GetAttributes(e2Class).Count());
        }

        [WorkItem(542533, "DevDiv")]
        [Fact]
        public void AttributesInMultiplePartialDeclarations_Type()
        {
            var source1 = @"
using System;
class A : Attribute {}
[A]
partial class X {}";

            var source2 = @"
using System;
class B : Attribute {}
[B]
partial class X {}
class C
{
    public static void Main()
    {
        typeof(X).GetCustomAttributes(false);
    }
}";

            var compilation = CreateCompilationWithMscorlib(new[] { source1, source2 }, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var aClass = m.GlobalNamespace.GetTypeMember("A");
                var bClass = m.GlobalNamespace.GetTypeMember("B");

                var type = m.GlobalNamespace.GetTypeMember("X");

                Assert.Equal(2, type.GetAttributes().Length);
                Assert.Equal(1, type.GetAttributes(aClass).Count());
                Assert.Equal(1, type.GetAttributes(bClass).Count());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: "");
        }

        [WorkItem(542533, "DevDiv")]
        [Fact]
        public void AttributesInMultiplePartialDeclarations_TypeParam()
        {
            var source1 = @"
using System;
class A : Attribute {}
partial class Gen<[A] T> {}";

            var source2 = @"
using System;
class B : Attribute {}
partial class Gen<[B] T> {}
class C
{
    public static void Main() {}
}";

            var compilation = CreateCompilationWithMscorlib(new[] { source1, source2 }, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var aClass = m.GlobalNamespace.GetTypeMember("A");
                var bClass = m.GlobalNamespace.GetTypeMember("B");

                var type = m.GlobalNamespace.GetTypeMember("Gen");
                var typeParameter = type.TypeParameters.First();

                Assert.Equal(2, typeParameter.GetAttributes().Length);
                Assert.Equal(1, typeParameter.GetAttributes(aClass).Count());
                Assert.Equal(1, typeParameter.GetAttributes(bClass).Count());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: "");
        }

        [WorkItem(542550, "DevDiv")]
        [Fact]
        public void Bug9824()
        {
            var source =
@"
using System;
 
public class TAttribute : Attribute { public static void Main () {} }
 
[T]
public class GClass<T> where T : Attribute
{
    [T]
    public enum E { }
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol attributeType = m.GlobalNamespace.GetTypeMember("TAttribute");

                NamedTypeSymbol GClass = m.GlobalNamespace.GetTypeMember("GClass").AsUnboundGenericType();
                Assert.Equal(1, GClass.GetAttributes(attributeType).Count());

                NamedTypeSymbol enumE = GClass.GetTypeMember("E");
                Assert.Equal(1, enumE.GetAttributes(attributeType).Count());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [WorkItem(543135, "DevDiv")]
        [Fact]
        public void AttributeAndDefaultValueArguments_01()
        {
            var source = @"
using System;
[A]
public class A : Attribute
{
    public A(object a = default(A)) { }
}

[A(1)]
class C
{
    public static void Main()
    {
        typeof(C).GetCustomAttributes(false);
    }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol attributeType = m.GlobalNamespace.GetTypeMember("A");
                NamedTypeSymbol cClass = m.GlobalNamespace.GetTypeMember("C");

                var attrs = attributeType.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
                var attr = attrs.First();
                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<object>(0, TypedConstantKind.Primitive, null);

                attrs = cClass.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
                attr = attrs.First();
                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<int>(0, TypedConstantKind.Primitive, 1);
            };

            string expectedOutput = "";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [WorkItem(543135, "DevDiv")]
        [Fact]
        public void AttributeAndDefaultValueArguments_02()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class A : System.Attribute
{
    public A(object o = null) { }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class B : System.Attribute
{
    public B(object o = default(B)) { }
}

[A]
[A(null)]
[B]
[B(default(B))]
class C
{
    public static void Main()
    {
        typeof(C).GetCustomAttributes(false);
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol attributeTypeA = m.GlobalNamespace.GetTypeMember("A");
                NamedTypeSymbol attributeTypeB = m.GlobalNamespace.GetTypeMember("B");
                NamedTypeSymbol cClass = m.GlobalNamespace.GetTypeMember("C");

                // Verify A attributes
                var attrs = cClass.GetAttributes(attributeTypeA);
                Assert.Equal(2, attrs.Count());

                var attr = attrs.First();
                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<object>(0, TypedConstantKind.Primitive, null);

                attr = attrs.ElementAt(1);
                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<object>(0, TypedConstantKind.Primitive, null);


                // Verify B attributes
                attrs = cClass.GetAttributes(attributeTypeB);
                Assert.Equal(2, attrs.Count());

                attr = attrs.First();
                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<object>(0, TypedConstantKind.Primitive, null);

                attr = attrs.ElementAt(1);
                Assert.Equal(1, attr.CommonConstructorArguments.Length);
                attr.VerifyValue<object>(0, TypedConstantKind.Primitive, null);
            };

            string expectedOutput = "";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: expectedOutput);
        }

        [WorkItem(529044, "DevDiv")]
        [Fact]
        public void AttributeNameLookup()
        {
            var source = @"
using System;
public class MyClass<T>
{
}
public class MyClassAttribute : Attribute
{
}
[MyClass]
public class Test
{
    public static void Main()
    {
        typeof(Test).GetCustomAttributes(false);
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol attributeType = m.GlobalNamespace.GetTypeMember("MyClassAttribute");
                NamedTypeSymbol testClass = m.GlobalNamespace.GetTypeMember("Test");

                // Verify attributes
                var attrs = testClass.GetAttributes(attributeType);
                Assert.Equal(1, attrs.Count());
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: null, expectedOutput: "");
        }

        [WorkItem(542003, "DevDiv")]
        [Fact]
        public void Bug8956_NullArgumentToSystemTypeParam()
        {
            string source = @"
using System;
 
class A : Attribute
{
    public A(System.Type t) {}
}

[A(null)]
class Test
{
    static void Main(string[] args)
    {
        typeof(Test).GetCustomAttributes(false);
    }
}
";
            CompileAndVerify(source);
        }

        [Fact]
        public void SpecialNameAttributeFromSource()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

[SpecialName()]
public struct S
{
    [SpecialName]
    byte this[byte x] { get { return x; } }

    [SpecialName]
    public event Action<string> E;
}
";

            var comp = CreateCompilationWithMscorlib(source);
            var global = comp.SourceModule.GlobalNamespace;
            var typesym = global.GetMember("S") as NamedTypeSymbol;
            Assert.NotNull(typesym);
            Assert.True(typesym.HasSpecialName);

            var idxsym = typesym.GetMember(WellKnownMemberNames.Indexer) as PropertySymbol;
            Assert.NotNull(idxsym);
            Assert.True(idxsym.HasSpecialName);

            var etsym = typesym.GetMember("E") as EventSymbol;
            Assert.NotNull(etsym);
            Assert.True(etsym.HasSpecialName);
        }

        [WorkItem(546277, "DevDiv")]
        [Fact]
        public void TestArrayTypeInAttributeArgument()
        {
            var source =
@"using System;

public class W {}

public class Y<T>
{
  public class F {}
  public class Z<U> {}
}

public class X : Attribute
{
    public X(Type y) { }
}

[X(typeof(W[]))]
public class C1 {}

[X(typeof(W[,]))]
public class C2 {}

[X(typeof(W[,][]))]
public class C3 {}

[X(typeof(Y<W>[][,]))]
public class C4 {}

[X(typeof(Y<int>.F[,][][,,]))]
public class C5 {}

[X(typeof(Y<int>.Z<W>[,][]))]
public class C6 {}
";

            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                NamedTypeSymbol classW = m.GlobalNamespace.GetTypeMember("W");
                NamedTypeSymbol classY = m.GlobalNamespace.GetTypeMember("Y");
                NamedTypeSymbol classF = classY.GetTypeMember("F");
                NamedTypeSymbol classZ = classY.GetTypeMember("Z");
                NamedTypeSymbol classX = m.GlobalNamespace.GetTypeMember("X");

                NamedTypeSymbol classC1 = m.GlobalNamespace.GetTypeMember("C1");
                NamedTypeSymbol classC2 = m.GlobalNamespace.GetTypeMember("C2");
                NamedTypeSymbol classC3 = m.GlobalNamespace.GetTypeMember("C3");
                NamedTypeSymbol classC4 = m.GlobalNamespace.GetTypeMember("C4");
                NamedTypeSymbol classC5 = m.GlobalNamespace.GetTypeMember("C5");
                NamedTypeSymbol classC6 = m.GlobalNamespace.GetTypeMember("C6");

                var attrs = classC1.GetAttributes();
                Assert.Equal(1, attrs.Length);
                var typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, classW, default(ImmutableArray<CustomModifier>));
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Type, typeArg);

                attrs = classC2.GetAttributes();
                Assert.Equal(1, attrs.Length);
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, classW, default(ImmutableArray<CustomModifier>), rank: 2);
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Type, typeArg);

                attrs = classC3.GetAttributes();
                Assert.Equal(1, attrs.Length);
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, classW, default(ImmutableArray<CustomModifier>));
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, typeArg, default(ImmutableArray<CustomModifier>), rank: 2);
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Type, typeArg);

                attrs = classC4.GetAttributes();
                Assert.Equal(1, attrs.Length);
                NamedTypeSymbol classYOfW = classY.ConstructIfGeneric(ImmutableArray.Create(new TypeWithModifiers(classW)));
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, classYOfW, default(ImmutableArray<CustomModifier>), rank: 2);
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, typeArg, default(ImmutableArray<CustomModifier>));
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Type, typeArg);

                attrs = classC5.GetAttributes();
                Assert.Equal(1, attrs.Length);
                NamedTypeSymbol classYOfInt = classY.ConstructIfGeneric(ImmutableArray.Create(new TypeWithModifiers(m.ContainingAssembly.GetSpecialType(SpecialType.System_Int32))));
                NamedTypeSymbol substNestedF = classYOfInt.GetTypeMember("F");
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, substNestedF, default(ImmutableArray<CustomModifier>), rank: 3);
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, typeArg, default(ImmutableArray<CustomModifier>));
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, typeArg, default(ImmutableArray<CustomModifier>), rank: 2);
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Type, typeArg);

                attrs = classC6.GetAttributes();
                Assert.Equal(1, attrs.Length);
                NamedTypeSymbol substNestedZ = classYOfInt.GetTypeMember("Z").ConstructIfGeneric(ImmutableArray.Create(new TypeWithModifiers(classW)));
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, substNestedZ, default(ImmutableArray<CustomModifier>));
                typeArg = ArrayTypeSymbol.CreateCSharpArray(m.ContainingAssembly, typeArg, default(ImmutableArray<CustomModifier>), rank: 2);
                attrs.First().VerifyValue<object>(0, TypedConstantKind.Type, typeArg);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);
        }

        [WorkItem(546621, "DevDiv")]
        [Fact]
        public void TestUnicodeAttributeArgument_Bug16353()
        {
            var source =
@"using System;
 
[Obsolete(UnicodeHighSurrogate)]
class C
{
    public const string UnicodeHighSurrogate = ""\uD800"";
    public const string UnicodeReplacementCharacter = ""\uFFFD"";
 
    static void Main()
    {
        string message = ((ObsoleteAttribute)typeof(C).GetCustomAttributes(false)[0]).Message;
 
        Console.WriteLine(message == UnicodeReplacementCharacter + UnicodeReplacementCharacter);
    }
}";
            CompileAndVerify(source, expectedOutput: "True");
        }

        [WorkItem(546621, "DevDiv")]
        [Fact]
        public void TestUnicodeAttributeArgumentsStrings()
        {
            string HighSurrogateCharacter = "\uD800";
            string LowSurrogateCharacter = "\uDC00";
            string UnicodeReplacementCharacter = "\uFFFD";
            string UnicodeLT0080 = "\u007F";
            string UnicodeLT0800 = "\u07FF";
            string UnicodeLT10000 = "\uFFFF";

            string source = @"
using System;

public class C
{
    public const string UnicodeSurrogate1 = ""\uD800"";
    public const string UnicodeSurrogate2 = ""\uD800\uD800"";
    public const string UnicodeSurrogate3 = ""\uD800\uDC00"";
    public const string UnicodeSurrogate4 = ""\uD800\u07FF\uD800"";
    public const string UnicodeSurrogate5 = ""\uD800\u007F\uDC00"";
    public const string UnicodeSurrogate6 = ""\uD800\u07FF\uDC00"";
    public const string UnicodeSurrogate7 = ""\uD800\uFFFF\uDC00"";
    public const string UnicodeSurrogate8 = ""\uD800\uD800\uDC00"";
    public const string UnicodeSurrogate9 = ""\uDC00\uDC00"";
    
    [Obsolete(UnicodeSurrogate1)]
    public int x1;

    [Obsolete(UnicodeSurrogate2)]
    public int x2;

    [Obsolete(UnicodeSurrogate3)]
    public int x3;

    [Obsolete(UnicodeSurrogate4)]
    public int x4;

    [Obsolete(UnicodeSurrogate5)]
    public int x5;

    [Obsolete(UnicodeSurrogate6)]
    public int x6;

    [Obsolete(UnicodeSurrogate7)]
    public int x7;

    [Obsolete(UnicodeSurrogate8)]
    public int x8;

    [Obsolete(UnicodeSurrogate9)]
    public int x9;
}
";

            Action<FieldSymbol, string> VerifyAttributes = (field, value) =>
            {
                var attributes = field.GetAttributes();
                Assert.Equal(1, attributes.Length);
                attributes[0].VerifyValue(0, TypedConstantKind.Primitive, value);
            };

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => (ModuleSymbol module) =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var x1 = type.GetMember<FieldSymbol>("x1");
                var x2 = type.GetMember<FieldSymbol>("x2");
                var x3 = type.GetMember<FieldSymbol>("x3");
                var x4 = type.GetMember<FieldSymbol>("x4");
                var x5 = type.GetMember<FieldSymbol>("x5");
                var x6 = type.GetMember<FieldSymbol>("x6");
                var x7 = type.GetMember<FieldSymbol>("x7");
                var x8 = type.GetMember<FieldSymbol>("x8");
                var x9 = type.GetMember<FieldSymbol>("x9");

                // public const string UnicodeSurrogate1 = ""\uD800"";
                VerifyAttributes(x1, isFromSource ?
                                        HighSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter);

                // public const string UnicodeSurrogate2 = ""\uD800\uD800"";
                VerifyAttributes(x2, isFromSource ?
                                        HighSurrogateCharacter + HighSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeReplacementCharacter);

                // public const string UnicodeSurrogate3 = ""\uD800\uDC00"";
                VerifyAttributes(x3, HighSurrogateCharacter + LowSurrogateCharacter);

                // public const string UnicodeSurrogate4 = ""\uD800\u07FF\uD800"";
                VerifyAttributes(x4, isFromSource ?
                                        HighSurrogateCharacter + UnicodeLT0800 + HighSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeLT0800 + UnicodeReplacementCharacter + UnicodeReplacementCharacter);

                // public const string UnicodeSurrogate5 = ""\uD800\u007F\uDC00"";
                VerifyAttributes(x5, isFromSource ?
                                        HighSurrogateCharacter + UnicodeLT0080 + LowSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeLT0080 + UnicodeReplacementCharacter + UnicodeReplacementCharacter);

                // public const string UnicodeSurrogate6 = ""\uD800\u07FF\uDC00"";
                VerifyAttributes(x6, isFromSource ?
                                        HighSurrogateCharacter + UnicodeLT0800 + LowSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeLT0800 + UnicodeReplacementCharacter + UnicodeReplacementCharacter);

                // public const string UnicodeSurrogate7 = ""\uD800\uFFFF\uDC00"";
                VerifyAttributes(x7, isFromSource ?
                                        HighSurrogateCharacter + UnicodeLT10000 + LowSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeLT10000 + UnicodeReplacementCharacter + UnicodeReplacementCharacter);

                // public const string UnicodeSurrogate8 = ""\uD800\uD800\uDC00"";
                VerifyAttributes(x8, isFromSource ?
                                        HighSurrogateCharacter + HighSurrogateCharacter + LowSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter + HighSurrogateCharacter + LowSurrogateCharacter);

                // public const string UnicodeSurrogate9 = ""\uDC00\uDC00"";
                VerifyAttributes(x9, isFromSource ?
                                        LowSurrogateCharacter + LowSurrogateCharacter :
                                        UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeReplacementCharacter + UnicodeReplacementCharacter);
            };

            CompileAndVerify(source, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        [WorkItem(546896, "DevDiv")]
        public void MissingTypeInSignature()
        {
            string lib1 = @"
public enum E { A, B, C }
";

            string lib2 = @"
public class A : System.Attribute 
{
    public A(E e) { }
}

public class C 
{ 
    [A(E.A)]
    public void M() { }
}
";
            string main = @"
class D : C 
{ 
    void N() { M(); }
}
";

            var c1 = CreateCompilationWithMscorlib(lib1);
            var r1 = c1.EmitToImageReference();

            var c2 = CreateCompilationWithMscorlib(lib2, references: new[] { r1 });
            var r2 = c2.EmitToImageReference();

            var cm = CreateCompilationWithMscorlib(main, new[] { r2 });
            cm.VerifyDiagnostics();

            var model = cm.GetSemanticModel(cm.SyntaxTrees[0]);

            int index = main.IndexOf("M()", StringComparison.Ordinal);
            var m = (ExpressionSyntax)cm.SyntaxTrees[0].GetCompilationUnitRoot().FindToken(index).Parent.Parent;

            var info = model.GetSymbolInfo(m);
            var args = info.Symbol.GetAttributes()[0].CommonConstructorArguments;

            // unresolved type - parameter ignored
            Assert.Equal(0, args.Length);
        }

        [Fact]
        [WorkItem(569089, "DevDiv")]
        public void NullArrays()
        {
            var source = @"
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
class C
{
}
";
            CompileAndVerify(source, symbolValidator: (m) =>
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
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
            });
        }

        [Fact]
        public void NullTypeAndString()
        {
            var source = @"
using System;

public class A : Attribute
{
    public A(Type t, string s)
    {
    }
}

[A(null, null)]
class C
{
}
";
            CompileAndVerify(source, symbolValidator: (m) =>
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var attr = c.GetAttributes().Single();
                var args = attr.ConstructorArguments.ToArray();

                Assert.Null(args[0].Value);
                Assert.Equal("Type", args[0].Type.Name);
                Assert.Throws<InvalidOperationException>(() => args[0].Values);

                Assert.Null(args[1].Value);
                Assert.Equal("String", args[1].Type.Name);
                Assert.Throws<InvalidOperationException>(() => args[1].Values);
            });
        }

        [WorkItem(121)]
        [Fact(Skip = "121")]
        public void Bug_AttributeOnWrongGenericParameter()
        {
            var source = @"
using System;
class XAttribute : Attribute { }
class C<T>
{
    public void M<[X]U>() { }
}
";
            CompileAndVerify(source, symbolValidator: module =>
            {
                var @class = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var classTypeParameter = @class.TypeParameters.Single();
                var method = @class.GetMember<MethodSymbol>("M");
                var methodTypeParameter = method.TypeParameters.Single();

                Assert.Empty(classTypeParameter.GetAttributes());

                var attribute = methodTypeParameter.GetAttributes().Single();
                Assert.Equal("XAttribute", attribute.AttributeClass.Name);
            });
        }


        #endregion

        #region Error Tests

        [Fact]
        public void AttributeConstructorErrors1()
        {
            var compilation = CreateCompilationWithMscorlibAndSystemCore(@"
using System;
static class m
{
    public static int NotAConstant()
    {
        return 9;
    }
}

public enum e1
{
    a
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class XAttribute : Attribute
{
    public XAttribute()
    {
    }
    public XAttribute(decimal d)
    {
    }
    public XAttribute(ref int i)
    {
    }
    public XAttribute(e1 e)
    {
    }
}

[XDoesNotExist()]
[X(1m)]
[X(1)]
[X(e1.a)]
[X(A.dyn)]
[X(m.NotAConstant() + 2)]
class A
{
  public const dynamic dyn = null;
}
", options: TestOptions.ReleaseDll);

            // Note that the dev11 compiler produces errors that XDoesNotExist *and* XDoesNotExistAttribute could not be found.
            // It does not go on to produce the other errors.

            compilation.VerifyDiagnostics(
                // (33,2): error CS0246: The type or namespace name 'XDoesNotExistAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [XDoesNotExist()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "XDoesNotExist").WithArguments("XDoesNotExistAttribute").WithLocation(33, 2),
                // (33,2): error CS0246: The type or namespace name 'XDoesNotExist' could not be found (are you missing a using directive or an assembly reference?)
                // [XDoesNotExist()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "XDoesNotExist").WithArguments("XDoesNotExist").WithLocation(33, 2),
                // (34,2): error CS0181: Attribute constructor parameter 'd' has type 'decimal', which is not a valid attribute parameter type
                // [X(1m)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "X").WithArguments("d", "decimal").WithLocation(34, 2),
                // (35,2): error CS0181: Attribute constructor parameter 'd' has type 'decimal', which is not a valid attribute parameter type
                // [X(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "X").WithArguments("d", "decimal").WithLocation(35, 2),
                // (37,2): error CS0121: The call is ambiguous between the following methods or properties: 'XAttribute.XAttribute(ref int)' and 'XAttribute.XAttribute(e1)'
                // [X(A.dyn)]
                Diagnostic(ErrorCode.ERR_AmbigCall, "X(A.dyn)").WithArguments("XAttribute.XAttribute(ref int)", "XAttribute.XAttribute(e1)").WithLocation(37, 2),
                // (38,2): error CS0181: Attribute constructor parameter 'd' has type 'decimal', which is not a valid attribute parameter type
                // [X(m.NotAConstant() + 2)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "X").WithArguments("d", "decimal").WithLocation(38, 2));
        }

        [Fact]
        public void AttributeNamedArgumentErrors1()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class XAttribute : Attribute
{
    public void F1(int i)
    {
    }

    private int PrivateField;
    public static int SharedProperty { get; set; }
    public int? ReadOnlyProperty
    {
        get { return null; }
    }
    public decimal BadDecimalType { get; set; }
    public System.DateTime BadDateType { get; set; }
    public Attribute[] BadArrayType { get; set; }
}

[X(NotFound = null)]
[X(F1 = null)]
[X(PrivateField = null)]
[X(SharedProperty = null)]
[X(ReadOnlyProperty = null)]
[X(BadDecimalType = null)]
[X(BadDateType = null)]
[X(BadArrayType = null)]
class A
{
}
", options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(    // (21,4): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                                              // [X(NotFound = null)]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound"),
                // (22,4): error CS0617: 'F1' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                // [X(F1 = null)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "F1").WithArguments("F1"),
                // (23,4): error CS0122: 'XAttribute.PrivateField' is inaccessible due to its protection level
                // [X(PrivateField = null)]
                Diagnostic(ErrorCode.ERR_BadAccess, "PrivateField").WithArguments("XAttribute.PrivateField"),
                // (24,4): error CS0617: 'SharedProperty' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                // [X(SharedProperty = null)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "SharedProperty").WithArguments("SharedProperty"),
                // (25,4): error CS0617: 'ReadOnlyProperty' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                // [X(ReadOnlyProperty = null)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "ReadOnlyProperty").WithArguments("ReadOnlyProperty"),
                // (26,4): error CS0655: 'BadDecimalType' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [X(BadDecimalType = null)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "BadDecimalType").WithArguments("BadDecimalType"),
                // (27,4): error CS0655: 'BadDateType' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [X(BadDateType = null)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "BadDateType").WithArguments("BadDateType"),
                // (28,4): error CS0655: 'BadArrayType' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [X(BadArrayType = null)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "BadArrayType").WithArguments("BadArrayType"));
        }

        [Fact]
        public void AttributeNoMultipleAndInvalidTarget()
        {
            string source = @"
using CustomAttribute;
[Base(1)]
[@BaseAttribute(""SOS"")]
static class AttributeMod
{
    [Derived('Q')]
    [Derived('C')]
    public class Foo
    {
    }
    [BaseAttribute(1)]
    [Base("""")]
    public class Bar
    {
    }
}";
            var references = new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull()) };
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, references, options: opt);

            compilation.VerifyDiagnostics(
                // (4,2): error CS0579: Duplicate 'BaseAttribute' attribute
                // [@BaseAttribute("SOS")]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "@BaseAttribute").WithArguments("BaseAttribute").WithLocation(4, 2),
                // (7,6): error CS0592: Attribute 'Derived' is not valid on this declaration type. It is only valid on 'struct, method, parameter' declarations.
                //     [Derived('Q')]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Derived").WithArguments("Derived", "struct, method, parameter").WithLocation(7, 6),
                // (8,6): error CS0579: Duplicate 'Derived' attribute
                //     [Derived('C')]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "Derived").WithArguments("Derived").WithLocation(8, 6),
                // (13,6): error CS0579: Duplicate 'Base' attribute
                //     [Base("")]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "Base").WithArguments("Base").WithLocation(13, 6));
        }

        [Fact]
        public void AttributeAmbiguousSpecification()
        {
            string source = @"
using System;

[AttributeUsage(AttributeTargets.All)]
public class X : Attribute {}

[AttributeUsage(AttributeTargets.All)]
public class XAttribute : Attribute { }

[X]                 // Error: Ambiguous
class Class1 { }

[XAttribute]        // Refers to XAttribute
class Class2 { }

[@X]                // Refers to X
class Class3 { }

[@XAttribute]       // Refers to XAttribute
class Class4 { }
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (10,2): error CS1614: 'X' is ambiguous between 'X' and 'XAttribute'; use either '@X' or 'XAttribute'
                // [X]                 // Error: Ambiguous
                Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "X").WithArguments("X", "X", "XAttribute").WithLocation(10, 2));
        }

        [Fact]
        public void AttributeErrorVerbatimIdentifierInSpecification()
        {
            string source = @"
using System;

[AttributeUsage(AttributeTargets.All)]
public class XAttribute : Attribute { }

[X]                 // Refers to X
class Class1 { }

[XAttribute]        // Refers to XAttribute
class Class2 { }

[@X]                // Error: No attribute named X
class Class3 { }
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (13,2): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                // [@X]                // Error: No attribute named X
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@X").WithArguments("X").WithLocation(13, 2));
        }

        [Fact]
        public void AttributeOpenTypeInAttribute()
        {
            string source = @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.All)]
public class XAttribute : Attribute 
{
    public XAttribute(Type t) { }
}

class G<T>
{
    [X(typeof(T))] T t1;                 // Error: open type in attribute
    [X(typeof(List<T>))] T t2;           // Error: open type in attribute
}

class X
{
    [X(typeof(List<int>))] int x;       // okay: X refers to XAttribute and List<int> is a closed constructed type
    [X(typeof(List<>))] int y;          // okay: X refers to XAttribute and List<> is an unbound generic type
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (13,8): error CS0416: 'T': an attribute argument cannot use type parameters
                //     [X(typeof(T))] T t1;                 // Error: open type in attribute
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(T)").WithArguments("T"),
                // (14,8): error CS0416: 'System.Collections.Generic.List<T>': an attribute argument cannot use type parameters
                //     [X(typeof(List<T>))] T t2;           // Error: open type in attribute
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(List<T>)").WithArguments("System.Collections.Generic.List<T>"),
                // (13,22): warning CS0169: The field 'G<T>.t1' is never used
                //     [X(typeof(T))] T t1;                 // Error: open type in attribute
                Diagnostic(ErrorCode.WRN_UnreferencedField, "t1").WithArguments("G<T>.t1"),
                // (14,28): warning CS0169: The field 'G<T>.t2' is never used
                //     [X(typeof(List<T>))] T t2;           // Error: open type in attribute
                Diagnostic(ErrorCode.WRN_UnreferencedField, "t2").WithArguments("G<T>.t2"),
                // (19,32): warning CS0169: The field 'X.x' is never used
                //     [X(typeof(List<int>))] int x;       // okay: X refers to XAttribute and List<int> is a closed constructed type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("X.x"),
                // (20,29): warning CS0169: The field 'X.y' is never used
                //     [X(typeof(List<>))] int y;          // okay: X refers to XAttribute and List<> is an unbound generic type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "y").WithArguments("X.y")
                );
        }

        [WorkItem(540924, "DevDiv")]
        [Fact]
        public void AttributeEnumsAsAttributeParameters()
        {
            string source = @"
using System;
class EClass
{
    public enum EEK { a, b, c, d };
}
[AttributeUsage(AttributeTargets.Class)]
internal class HelpAttribute : Attribute
{
    public HelpAttribute(EClass.EEK[] b1)
    {
    }
}
[HelpAttribute(new EClass.EEK[2] { EClass.EEK.b, EClass.EEK.c })]
public class MainClass
{
    public static void Main()
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
        }

        [WorkItem(768798, "DevDiv")]
        [Fact(Skip = "768798")]
        public void AttributeInvalidTargetSpecifier()
        {
            string source = @"
using System;
// Below attribute specification generates a warning regarding invalid target specifier, 
// We skip binding the attribute with invalid target specifier, 
// no error generated for invalid use of AttributeUsage on non attribute class.
[method: AttributeUsage(AttributeTargets.All)]
class X
{
    public static void Main() {}
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (6,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type"));
        }

        [WorkItem(768798, "DevDiv")]
        [Fact(Skip = "768798")]
        public void AttributeInvalidTargetSpecifierOnInvalidAttribute()
        {
            string source = @"
[method: OopsForgotToBindThis(Haha)]
class X
{
    public static void Main() {}
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(/*CS0657, CS0246*/);
        }

        [Fact]
        public void AttributeUsageMultipleErrors()
        {
            string source =
@"using System;
class A
{
    [AttributeUsage(AttributeTargets.Method)]
    void M1() { }
    [AttributeUsage(0)]
    void M2() { }
}
[AttributeUsage(0)]
class B
{
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,6): error CS0592: Attribute 'AttributeUsage' is not valid on this declaration type. It is only valid on 'class' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "AttributeUsage").WithArguments("AttributeUsage", "class").WithLocation(4, 6),
                // (6,6): error CS0592: Attribute 'AttributeUsage' is not valid on this declaration type. It is only valid on 'class' declarations.
                //     [AttributeUsage(0)]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "AttributeUsage").WithArguments("AttributeUsage", "class").WithLocation(6, 6),
                // (9,2): error CS0641: Attribute 'AttributeUsage' is only valid on classes derived from System.Attribute
                Diagnostic(ErrorCode.ERR_AttributeUsageOnNonAttributeClass, "AttributeUsage").WithArguments("AttributeUsage").WithLocation(9, 2));
        }

        [Fact]
        public void CS0643ERR_DuplicateNamedAttributeArgument02()
        {
            string source = @"
using System;
[AttributeUsage(AllowMultiple = true, AllowMultiple = false)]
class MyAtt : Attribute
{ }
 
[MyAtt]
public class Test
{
    public static void Main()
    {
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (3,39): error CS0643: 'AllowMultiple' duplicate named attribute argument
                Diagnostic(ErrorCode.ERR_DuplicateNamedAttributeArgument, "AllowMultiple = false").WithArguments("AllowMultiple").WithLocation(3, 39),
                // (3,2): error CS7036: There is no argument given that corresponds to the required formal parameter 'validOn' of 'System.AttributeUsageAttribute.AttributeUsageAttribute(System.AttributeTargets)'
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AttributeUsage(AllowMultiple = true, AllowMultiple = false)").WithArguments("validOn", "System.AttributeUsageAttribute.AttributeUsageAttribute(System.AttributeTargets)").WithLocation(3, 2));
        }

        [WorkItem(541059, "DevDiv")]
        [Fact]
        public void AttributeUsageIsNull()
        {
            string source = @"
using System;
[AttributeUsage(null)]
public class Att1 : Attribute { }
public class Foo



{
    public static void Main()
    {
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "System.AttributeTargets"));
        }

        [WorkItem(541072, "DevDiv")]
        [Fact]
        public void AttributeContainsGeneric()
        {
            string source = @"
[Foo<int>]
class G
{
}
class Foo<T>
{
}
";

            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (2,2): error CS0404: Cannot apply attribute class 'Foo<T>' because it is generic
                // [Foo<int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Foo<int>").WithArguments("Foo<T>").WithLocation(2, 2));
        }

        /// <summary>
        /// Bug 7620: System.Nullreference Exception throws while the value of  parameter  AttributeUsage Is Null
        /// </summary>
        [Fact]
        public void CS1502ERR_NullAttributeUsageArgument()
        {
            string source = @"
using System;

[AttributeUsage(null)]
public class Attr : Attribute { }

public class Foo
{
    public static void Main()
    {
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (4,17): error CS1503: Argument 1: cannot convert from '<null>' to 'System.AttributeTargets'
                // [AttributeUsage(null)]
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "System.AttributeTargets"));
        }

        /// <summary>
        /// Bug 7632: Debug.Assert() Failure  while Attribute Contains Generic
        /// </summary>
        [Fact]
        public void CS0404ERR_GenericAttributeError()
        {
            string source = @"
[Foo<int>]
class G
{
}
class Foo<T>
{
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (2,2): error CS0404: Cannot apply attribute class 'Foo<T>' because it is generic
                // [Foo<int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Foo<int>").WithArguments("Foo<T>").WithLocation(2, 2));
        }

        [WorkItem(541423, "DevDiv")]
        [Fact]
        public void ErrorsInMultipleSyntaxTrees()
        {
            var source1 =
@"using System;
[module: A]
[AttributeUsage(AttributeTargets.Class)]
class A : Attribute
{
}
[AttributeUsage(AttributeTargets.Method)]
class B : Attribute
{
}";
            var source2 =
@"[module: B]";

            var compilation = CreateCompilationWithMscorlib(new[] { source1, source2 });
            compilation.VerifyDiagnostics(
                // (2,10): error CS0592: Attribute 'A' is not valid on this declaration type. It is only valid on 'class' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "A").WithArguments("A", "class").WithLocation(2, 10),
                // (1,10): error CS0592: Attribute 'B' is not valid on this declaration type. It is only valid on 'method' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "B").WithArguments("B", "method").WithLocation(1, 10));
        }

        [WorkItem(542533, "DevDiv")]
        [Fact]
        public void ErrorsInMultipleSyntaxTrees_TypeParam()
        {
            var source1 =
@"using System;
[AttributeUsage(AttributeTargets.Class)]
class A : Attribute
{
}
[AttributeUsage(AttributeTargets.Method)]
class B : Attribute
{
}

class Gen<[A] T> {}
";
            var source2 =
@"class Gen2<[B] T> {}";

            var compilation = CreateCompilationWithMscorlib(new[] { source1, source2 });
            compilation.VerifyDiagnostics(
                // (11,12): error CS0592: Attribute 'A' is not valid on this declaration type. It is only valid on 'class' declarations.
                // class Gen<[A] T> {}
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "A").WithArguments("A", "class").WithLocation(11, 12),
                // (1,13): error CS0592: Attribute 'B' is not valid on this declaration type. It is only valid on 'method' declarations.
                // class Gen2<[B] T> {}
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "B").WithArguments("B", "method").WithLocation(1, 13));
        }

        [WorkItem(541423, "DevDiv")]
        [Fact]
        public void ErrorsInMultiplePartialDeclarations()
        {
            var source =
@"using System;
[AttributeUsage(AttributeTargets.Struct)]
class A : Attribute
{
}
[AttributeUsage(AttributeTargets.Method)]
class B : Attribute
{
}
[A]
partial class C
{
}
[B]
partial class C
{
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (10,2): error CS0592: Attribute 'A' is not valid on this declaration type. It is only valid on 'struct' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "A").WithArguments("A", "struct").WithLocation(10, 2),
                // (14,2): error CS0592: Attribute 'B' is not valid on this declaration type. It is only valid on 'method' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "B").WithArguments("B", "method").WithLocation(14, 2));
        }

        [WorkItem(542533, "DevDiv")]
        [Fact]
        public void ErrorsInMultiplePartialDeclarations_TypeParam()
        {
            var source =
@"using System;
[AttributeUsage(AttributeTargets.Struct)]
class A : Attribute
{
}
[AttributeUsage(AttributeTargets.Method)]
class B : Attribute
{
}

partial class Gen<[A] T>
{
}
partial class Gen<[B] T>
{
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (11,20): error CS0592: Attribute 'A' is not valid on this declaration type. It is only valid on 'struct' declarations.
                // partial class Gen<[A] T>
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "A").WithArguments("A", "struct").WithLocation(11, 20),
                // (14,20): error CS0592: Attribute 'B' is not valid on this declaration type. It is only valid on 'method' declarations.
                // partial class Gen<[B] T>
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "B").WithArguments("B", "method").WithLocation(14, 20));
        }

        [WorkItem(541505, "DevDiv")]
        [Fact]
        public void AttributeArgumentError_CS0120()
        {
            var source =
@"using System;
class A : Attribute
{
  public A(ProtectionLevel p){}
}

enum ProtectionLevel
{
  Privacy = 0
}

class F
{
  int ProtectionLevel;

  [A(ProtectionLevel.Privacy)]
  public int test;
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (16,6): error CS0120: An object reference is required for the non-static field, method, or property 'F.ProtectionLevel'
                //   [A(ProtectionLevel.Privacy)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "ProtectionLevel").WithArguments("F.ProtectionLevel"),
                // (14,7): warning CS0169: The field 'F.ProtectionLevel' is never used
                //   int ProtectionLevel;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "ProtectionLevel").WithArguments("F.ProtectionLevel"),
                // (17,14): warning CS0649: Field 'F.test' is never assigned to, and will always have its default value 0
                //   public int test;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "test").WithArguments("F.test", "0")
                );
        }

        [Fact, WorkItem(541427, "DevDiv")]
        public void AttributeTargetsString()
        {
            var source = @"
using System;
[AttributeUsage(AttributeTargets.All & ~AttributeTargets.Class)] class A : Attribute { }
[A] class C { }
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (3,2): error CS0592: Attribute 'A' is not valid on this declaration type. It is only valid on 'assembly, module, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
    // [A] class C { }
    Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "A").WithArguments("A", "assembly, module, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter")
            );
        }

        [Fact]
        public void AttributeTargetsAssemblyModule()
        {
            var source = @"
using System;
[module: Attr()]
[AttributeUsage(AttributeTargets.Assembly)]
class Attr: Attribute { public Attr(){} }";

            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (3,10): error CS0592: Attribute 'Attr' is not valid on this declaration type. It is only valid on 'assembly' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Attr").WithArguments("Attr", "assembly"));
        }

        [WorkItem(541259, "DevDiv")]
        [Fact]
        public void CS0182_NonConstantArrayCreationAttributeArgument()
        {
            var source =
@"using System;
 
[A(new int[1] {Program.f})]         // error
[A(new int[1])]                     // error
[A(new int[1,1])]                   // error  
[A(new int[1 - 1])]                 // OK create an empty array
[A(new A[0])]                       // error
class Program
{
    static public int f = 10;
    public static void Main()
    {
        typeof(Program).GetCustomAttributes(false);
    }
}
 
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class A : Attribute
{
    public A(object x) { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (3,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new int[1] {Program.f})]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "Program.f"),
                // (4,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new int[1])]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new int[1]"),
                // (5,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new int[1,1])]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new int[1,1]"),
                // (7,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new A[0])]                       
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new A[0]"));
        }

        [WorkItem(541753, "DevDiv")]
        [Fact]
        public void CS0182_NestedArrays()
        {
            var source = @"
using System;
 
[A(new int[][] { new int[] { 1 } })]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(false);
    }
}
 
class A : Attribute
{
    public A(object x) { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new int[][] { new int[] { 1 } })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new int[][] { new int[] { 1 } }").WithLocation(4, 4));
        }

        [WorkItem(541849, "DevDiv")]
        [Fact]
        public void CS0182_MultidimensionalArrays()
        {
            var source =
@"using System;
 
class MyAttribute : Attribute
{
    public MyAttribute(params int[][,] x) { }
}

[My]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(false);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (8,2): error CS0181: Attribute constructor parameter 'x' has type 'int[][*,*]', which is not a valid attribute parameter type
                // [My]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "int[][*,*]").WithLocation(8, 2));
        }

        [WorkItem(541858, "DevDiv")]
        [Fact]
        public void AttributeDefaultValueArgument()
        {
            var source =
@"using System;
 
namespace AttributeTest
{
    [A(3, X = 6)]
    public class A : Attribute
    {
        public int X;
        public A(int x, int y = 4, object a = default(A)) { }
    
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "");
        }

        [WorkItem(541858, "DevDiv")]
        [Fact]
        public void CS0416_GenericAttributeDefaultValueArgument()
        {
            var source =
@"using System;
 
public class A : Attribute
{
    public object X;
    static void Main()
    {
        typeof(C<int>.E).GetCustomAttributes(false);
    }
}
 
public class C<T>
{
    [A(X = default(E))]
    public enum E { }

    [A(X = typeof(E2))]
    public enum E2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (14,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [A(X = default(E))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "default(E)").WithLocation(14, 12),
                // (17,12): error CS0416: 'C<T>.E2': an attribute argument cannot use type parameters
                //     [A(X = typeof(E2))]
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(E2)").WithArguments("C<T>.E2").WithLocation(17, 12));
        }

        [WorkItem(541615, "DevDiv")]
        [Fact]
        public void CS0246_VarAttributeIdentifier()
        {
            var source = @"
[var()]
class Program
{
    public static void Main() {}
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'varAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [var()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "var").WithArguments("varAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'var' could not be found (are you missing a using directive or an assembly reference?)
                // [var()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "var").WithArguments("var").WithLocation(2, 2));
        }

        [Fact]
        public void TestAttributesWithInvalidArgumentsOrder()
        {
            string source = @"
using System;
 
namespace AttributeTest
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [A(3, z: 5, X = 6, y: 1)]
    [A(3, z: 5, 1)]
    [A(3, 1, X = 6, z: 5)]
    [A(X = 6, 0)]
    [A(X = 6, x: 0)]
    public class A : Attribute
    {
        public int X;
        public A(int x, int y = 4, int z = 0) { Console.WriteLine(x); Console.WriteLine(y); Console.WriteLine(z); }
    
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }

    public class B
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (7,27): error CS1016: Named attribute argument expected
                //     [A(3, z: 5, X = 6, y: 1)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "1").WithLocation(7, 27),
                // (9,24): error CS1016: Named attribute argument expected
                //     [A(3, 1, X = 6, z: 5)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "5").WithLocation(9, 24),
                // (10,15): error CS1016: Named attribute argument expected
                //     [A(X = 6, 0)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "0").WithLocation(10, 15),
                // (11,18): error CS1016: Named attribute argument expected
                //     [A(X = 6, x: 0)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "0").WithLocation(11, 18),
                // (8,17): error CS1738: Named argument specifications must appear after all fixed arguments have been specified
                //     [A(3, z: 5, 1)]
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "1").WithLocation(8, 17));
        }

        [WorkItem(541877, "DevDiv")]
        [Fact]
        public void Bug8772_TestDelegateAttributeNameBinding()
        {
            string source = @"
using System;
 
class A : Attribute
{
    public A(int x) { Console.WriteLine(x); }
}
 
class C
{
    [A(Invoke)]
    delegate void F1();
 
    delegate T F2<[A(Invoke)]T> ();

    const int Invoke = 1;
 
    static void Main()
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (11,8): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
                //     [A(Invoke)]
                Diagnostic(ErrorCode.ERR_BadArgType, "Invoke").WithArguments("1", "method group", "int").WithLocation(11, 8),
                // (14,22): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
                //     delegate T F2<[A(Invoke)]T> ();
                Diagnostic(ErrorCode.ERR_BadArgType, "Invoke").WithArguments("1", "method group", "int").WithLocation(14, 22));
        }

        [Fact]
        public void AmbiguousAttributeErrors_01()
        {
            string source = @"
namespace ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace ValidWithoutSuffix
{
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_01
{
    using ValidWithSuffix;
    using ValidWithoutSuffix;

    [Description(null)]
    public class Test { }

    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (23,6): error CS1614: 'Description' is ambiguous between 'ValidWithoutSuffix.Description' and 'ValidWithSuffix.DescriptionAttribute'; use either '@Description' or 'DescriptionAttribute'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "Description").WithArguments("Description", "ValidWithoutSuffix.Description", "ValidWithSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_02()
        {
            string source = @"
namespace ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace ValidWithSuffix_And_ValidWithoutSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_02
{
    using ValidWithSuffix;
    using ValidWithSuffix_And_ValidWithoutSuffix;

    [Description(null)]
    public class Test { }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (30,6): error CS0104: 'DescriptionAttribute' is an ambiguous reference between 'ValidWithSuffix.DescriptionAttribute' and 'ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute'
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "DescriptionAttribute").WithArguments("DescriptionAttribute", "ValidWithSuffix.DescriptionAttribute", "ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_03()
        {
            string source = @"
namespace ValidWithoutSuffix
{
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace ValidWithSuffix_And_ValidWithoutSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_03
{
    using ValidWithoutSuffix;
    using ValidWithSuffix_And_ValidWithoutSuffix;

    [Description(null)]
    public class Test { }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void AmbiguousAttributeErrors_04()
        {
            string source = @"
namespace ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace ValidWithoutSuffix
{
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace ValidWithSuffix_And_ValidWithoutSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_04
{
    using ValidWithSuffix;
    using ValidWithoutSuffix;
    using ValidWithSuffix_And_ValidWithoutSuffix;

    [Description(null)]
    public class Test { }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (36,6): error CS0104: 'Description' is an ambiguous reference between 'ValidWithSuffix_And_ValidWithoutSuffix.Description' and 'ValidWithoutSuffix.Description'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "ValidWithSuffix_And_ValidWithoutSuffix.Description", "ValidWithoutSuffix.Description"),
                // (39,6): error CS0104: 'DescriptionAttribute' is an ambiguous reference between 'ValidWithSuffix.DescriptionAttribute' and 'ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute'
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "DescriptionAttribute").WithArguments("DescriptionAttribute", "ValidWithSuffix.DescriptionAttribute", "ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_05()
        {
            string source = @"
namespace InvalidWithSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace InvalidWithoutSuffix
{
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_05
{
    using InvalidWithSuffix;
    using InvalidWithoutSuffix;

    [Description(null)]
    public class Test { }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (23,6): error CS0616: 'InvalidWithoutSuffix.Description' is not an attribute class
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "Description").WithArguments("InvalidWithoutSuffix.Description"),
                // (26,6): error CS0616: 'InvalidWithSuffix.DescriptionAttribute' is not an attribute class
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "DescriptionAttribute").WithArguments("InvalidWithSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_06()
        {
            string source = @"
namespace InvalidWithSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace InvalidWithSuffix_And_InvalidWithoutSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_06
{
    using InvalidWithSuffix;
    using InvalidWithSuffix_And_InvalidWithoutSuffix;

    [Description(null)]
    public class Test { }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (27,6): error CS0104: 'Description' is an ambiguous reference between 'InvalidWithSuffix.DescriptionAttribute' and 'InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "InvalidWithSuffix.DescriptionAttribute", "InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute"),
                // (30,6): error CS0104: 'DescriptionAttribute' is an ambiguous reference between 'InvalidWithSuffix.DescriptionAttribute' and 'InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute'
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "DescriptionAttribute").WithArguments("DescriptionAttribute", "InvalidWithSuffix.DescriptionAttribute", "InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_07()
        {
            string source = @"
namespace InvalidWithoutSuffix
{
    public class Description
    {
        public Description(string name) { }
    }
}

namespace InvalidWithSuffix_And_InvalidWithoutSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_07
{
    using InvalidWithoutSuffix;
    using InvalidWithSuffix_And_InvalidWithoutSuffix;

    [Description(null)]
    public class Test { }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (30,6): error CS0616: 'InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute' is not an attribute class
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "DescriptionAttribute").WithArguments("InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute"),
                // (27,6): error CS0104: 'Description' is an ambiguous reference between 'InvalidWithSuffix_And_InvalidWithoutSuffix.Description' and 'InvalidWithoutSuffix.Description'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "InvalidWithSuffix_And_InvalidWithoutSuffix.Description", "InvalidWithoutSuffix.Description"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_08()
        {
            string source = @"
namespace InvalidWithSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace InvalidWithoutSuffix
{
    public class Description
    {
        public Description(string name) { }
    }
}

namespace InvalidWithSuffix_And_InvalidWithoutSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_08
{
    using InvalidWithSuffix;
    using InvalidWithoutSuffix;
    using InvalidWithSuffix_And_InvalidWithoutSuffix;

    [Description(null)]
    public class Test { }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (36,6): error CS0104: 'Description' is an ambiguous reference between 'InvalidWithSuffix_And_InvalidWithoutSuffix.Description' and 'InvalidWithoutSuffix.Description' 
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "InvalidWithSuffix_And_InvalidWithoutSuffix.Description", "InvalidWithoutSuffix.Description"),
                // (39,6): error CS0104: 'DescriptionAttribute' is an ambiguous reference between 'InvalidWithSuffix.DescriptionAttribute' and 'InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute'
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "DescriptionAttribute").WithArguments("DescriptionAttribute", "InvalidWithSuffix.DescriptionAttribute", "InvalidWithSuffix_And_InvalidWithoutSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_09()
        {
            string source = @"
namespace InvalidWithoutSuffix_But_ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description
    {
        public Description(string name) { }
    }
}

namespace InvalidWithSuffix_But_ValidWithoutSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_09
{
    using InvalidWithoutSuffix_But_ValidWithSuffix;
    using InvalidWithSuffix_But_ValidWithoutSuffix;

    [Description(null)]
    public class Test { public static void Main() {} }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (31,6): error CS0104: 'Description' is an ambiguous reference between 'InvalidWithSuffix_But_ValidWithoutSuffix.Description' and 'InvalidWithoutSuffix_But_ValidWithSuffix.Description'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "InvalidWithSuffix_But_ValidWithoutSuffix.Description", "InvalidWithoutSuffix_But_ValidWithSuffix.Description"),
                // (34,6): error CS0104: 'DescriptionAttribute' is an ambiguous reference between 'InvalidWithSuffix_But_ValidWithoutSuffix.DescriptionAttribute' and 'InvalidWithoutSuffix_But_ValidWithSuffix.DescriptionAttribute'
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "DescriptionAttribute").WithArguments("DescriptionAttribute", "InvalidWithSuffix_But_ValidWithoutSuffix.DescriptionAttribute", "InvalidWithoutSuffix_But_ValidWithSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_10()
        {
            string source = @"
namespace ValidWithoutSuffix
{
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace InvalidWithoutSuffix
{
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_10
{
    using ValidWithoutSuffix;
    using InvalidWithoutSuffix;

    [Description(null)]
    public class Test { public static void Main() {} }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (23,6): error CS0104: 'Description' is an ambiguous reference between 'InvalidWithoutSuffix.Description' and 'ValidWithoutSuffix.Description'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "InvalidWithoutSuffix.Description", "ValidWithoutSuffix.Description"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_11()
        {
            string source = @"
namespace ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace InvalidWithSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace TestNamespace_11
{
    using ValidWithSuffix;
    using InvalidWithSuffix;

    [Description(null)]
    public class Test { public static void Main() {} }

    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (23,6): error CS0104: 'Description' is an ambiguous reference between 'InvalidWithSuffix.DescriptionAttribute' and 'ValidWithSuffix.DescriptionAttribute'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "InvalidWithSuffix.DescriptionAttribute", "ValidWithSuffix.DescriptionAttribute"),
                // (26,6): error CS0104: 'DescriptionAttribute' is an ambiguous reference between 'InvalidWithSuffix.DescriptionAttribute' and 'ValidWithSuffix.DescriptionAttribute'
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "DescriptionAttribute").WithArguments("DescriptionAttribute", "InvalidWithSuffix.DescriptionAttribute", "ValidWithSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AmbiguousAttributeErrors_12()
        {
            string source = @"
namespace InvalidWithSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace InvalidWithoutSuffix_But_ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_12
{
    using InvalidWithoutSuffix_But_ValidWithSuffix;
    using InvalidWithSuffix;

    [Description(null)]
    public class Test { public static void Main() {} }
    
    [DescriptionAttribute(null)]
    public class Test2 { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (30,6): error CS0104: 'DescriptionAttribute' is an ambiguous reference between 'InvalidWithSuffix.DescriptionAttribute' and 'InvalidWithoutSuffix_But_ValidWithSuffix.DescriptionAttribute'
                //     [DescriptionAttribute(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "DescriptionAttribute").WithArguments("DescriptionAttribute", "InvalidWithSuffix.DescriptionAttribute", "InvalidWithoutSuffix_But_ValidWithSuffix.DescriptionAttribute"),
                // (27,6): error CS0104: 'Description' is an ambiguous reference between 'InvalidWithSuffix.DescriptionAttribute' and 'InvalidWithoutSuffix_But_ValidWithSuffix.DescriptionAttribute'
                //     [Description(null)]
                Diagnostic(ErrorCode.ERR_AmbigContext, "Description").WithArguments("Description", "InvalidWithSuffix.DescriptionAttribute", "InvalidWithoutSuffix_But_ValidWithSuffix.DescriptionAttribute"));
        }

        [Fact]
        public void AliasAttributeName()
        {
            var source =
@"using A = A1;
using AAttribute = A2;
class A1 : System.Attribute { }
class A2 : System.Attribute { }
[A]class C { }";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,2): error CS1614: 'A' is ambiguous between 'A2' and 'A1'; use either '@A' or 'AAttribute'
                Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "A").WithArguments("A", "A1", "A2").WithLocation(5, 2));
        }

        [WorkItem(542279, "DevDiv")]
        [Fact]
        public void MethodSignatureAttributes()
        {
            var text =
@"class A : System.Attribute
{
    public A(object o) { }
}
class B { }
class C
{
    [return: A(new B())]
    static object F(
        [A(new B())] object x,
        [param: A(new B())] object y)
    {
        return null;
    }
}";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new B()").WithLocation(8, 16),
                // (10,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new B()").WithLocation(10, 12),
                // (11,19): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new B()").WithLocation(11, 19));
        }

        [Fact]
        public void AttributeDiagnosticsForEachArgument()
        {
            var source = @"using System;
public class A : Attribute 
{
  public A(object[] a) {}
}

[A(new object[] { default(E), default(E) })]
class C<T, U> { public enum E {} }";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,31): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new object[] { default(E), default(E) })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "default(E)").WithLocation(7, 31),
                // (7,19): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(new object[] { default(E), default(E) })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "default(E)").WithLocation(7, 19));
        }

        [Fact]
        public void AttributeArgumentDecimalTypeConstant()
        {
            var source = @"using System;
[A(X = new decimal())]
public class A : Attribute
{
  public object X;
  const decimal y = new decimal();
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (2,8): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(X = new decimal())]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new decimal()").WithLocation(2, 8));
        }

        [WorkItem(542533, "DevDiv")]
        [Fact]
        public void DuplicateAttributeOnTypeParameterOfPartialClass()
        {
            string source = @"
class A : System.Attribute { }

partial class C<T>  { }
partial class C<[A][A] T> { }
";
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, null, options: opt);

            compilation.VerifyDiagnostics(
                // (4,2): error CS0579: Duplicate 'A' attribute
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, @"A").WithArguments("A"));
        }

        [WorkItem(542486, "DevDiv")]
        [Fact]
        public void MethodParameterScope()
        {
            string source = @"
using System;

class A : Attribute
{
    public A(int x) { Console.WriteLine(x); }
}

class C
{
    [A(qq)] // CS0103 - no 'qq' in scope
    C(int qq) { }

    [A(rr)] // CS0103 - no 'rr' in scope
    void M(int rr) { }

    int P { [A(value)]set { } } // CS0103 - no 'value' in scope

    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (11,8): error CS0103: The name 'qq' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "qq").WithArguments("qq"),
                // (14,8): error CS0103: The name 'rr' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "rr").WithArguments("rr"),
                // (17,16): error CS0103: The name 'value' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "value").WithArguments("value"));

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var attrArgSyntaxes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AttributeArgumentSyntax>();
            Assert.Equal(3, attrArgSyntaxes.Count());

            foreach (var argSyntax in attrArgSyntaxes)
            {
                var info = semanticModel.GetSymbolInfo(argSyntax.Expression);
                Assert.Null(info.Symbol);
                Assert.Equal(0, info.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, info.CandidateReason);
            }
        }

        [WorkItem(542486, "DevDiv")]
        [Fact]
        public void MethodTypeParameterScope()
        {
            string source = @"
using System;

class A : Attribute
{
    public A(int x) { Console.WriteLine(x); }
}

class C
{
    [A(typeof(T))] // CS0246 - no 'T' in scope
    void M<T>() { }

    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (11,15): error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "T").WithArguments("T"));

            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var attrArgSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Single();
            var typeofSyntax = (TypeOfExpressionSyntax)attrArgSyntax.Expression;
            var typeofArgSyntax = typeofSyntax.Type;
            Assert.Equal("T", typeofArgSyntax.ToString());

            var info = semanticModel.GetSymbolInfo(typeofArgSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(0, info.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
        }

        [WorkItem(542625, "DevDiv")]
        [Fact]
        public void DuplicateAttributeOnPartialMethod()
        {
            string source = @"
class A : System.Attribute { }
class B : System.Attribute { }

partial class C
{
    [return: B]
    [A]
    static partial void Foo();
    
    [return: B]
    [A]
    static partial void Foo() { }
}
";
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, null, options: opt);

            compilation.VerifyDiagnostics(
                // error CS0579: Duplicate 'A' attribute
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, @"A").WithArguments("A"),
                // error CS0579: Duplicate 'B' attribute
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, @"B").WithArguments("B"));
        }

        [WorkItem(542625, "DevDiv")]
        [Fact]
        public void DuplicateAttributeOnTypeParameterOfPartialMethod()
        {
            string source = @"
class A : System.Attribute { }

partial class C
{
    static partial void Foo<[A] T>();
    static partial void Foo<[A] T>() { }

    // partial method without implementation, but another method with same name
    static partial void Foo2<[A] T>();
    static void Foo2<[A] T>() { }

    // partial method without implementation, but another member with same name
    static partial void Foo3<[A] T>();
    private int Foo3;

    // partial method without implementation
    static partial void Foo4<[A][A] T>();

    // partial methods differing by signature
    static partial void Foo5<[A] T>(int x);
    static partial void Foo5<[A] T>();

    // partial method without defining declaration
    static partial void Foo6<[A][A] T>() { }

}
";
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, null, options: opt);
            compilation.VerifyDiagnostics(
                // (25,25): error CS0759: No defining declaration found for implementing declaration of partial method 'C.Foo6<T>()'
                //     static partial void Foo6<[A][A] T>() { }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "Foo6").WithArguments("C.Foo6<T>()"),
                // (11,17): error CS0111: Type 'C' already defines a member called 'Foo2' with the same parameter types
                //     static void Foo2<[A] T>() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Foo2").WithArguments("Foo2", "C"),
                // (15,17): error CS0102: The type 'C' already contains a definition for 'Foo3'
                //     private int Foo3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Foo3").WithArguments("C", "Foo3"),
                // (7,30): error CS0579: Duplicate 'A' attribute
                //     static partial void Foo<[A] T>() { }
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A").WithArguments("A"),
                // (18,34): error CS0579: Duplicate 'A' attribute
                //     static partial void Foo4<[A][A] T>();
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A").WithArguments("A"),
                // (25,34): error CS0579: Duplicate 'A' attribute
                //     static partial void Foo6<[A][A] T>() { }
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A").WithArguments("A"),
                // (15,17): warning CS0169: The field 'C.Foo3' is never used
                //     private int Foo3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Foo3").WithArguments("C.Foo3"));
        }

        [WorkItem(542625, "DevDiv")]
        [Fact]
        public void DuplicateAttributeOnParameterOfPartialMethod()
        {
            string source = @"
class A : System.Attribute { }

partial class C
{
    static partial void Foo([param: A]int y);
    static partial void Foo([A] int y) { }

    // partial method without implementation, but another method with same name
    static partial void Foo2([A] int y);
    static void Foo2([A] int y) { }

    // partial method without implementation, but another member with same name
    static partial void Foo3([A] int y);
    private int Foo3;

    // partial method without implementation
    static partial void Foo4([A][param: A] int y);

    // partial methods differing by signature
    static partial void Foo5([A] int y);
    static partial void Foo5([A] int y, int z);

    // partial method without defining declaration
    static partial void Foo6([A][A] int y) { }
}
";
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, null, options: opt);

            compilation.VerifyDiagnostics(
                // (25,25): error CS0759: No defining declaration found for implementing declaration of partial method 'C.Foo6(int)'
                //     static partial void Foo6([A][A] int y) { }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "Foo6").WithArguments("C.Foo6(int)"),
                // (11,17): error CS0111: Type 'C' already defines a member called 'Foo2' with the same parameter types
                //     static void Foo2([A] int y) { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Foo2").WithArguments("Foo2", "C"),
                // (15,17): error CS0102: The type 'C' already contains a definition for 'Foo3'
                //     private int Foo3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Foo3").WithArguments("C", "Foo3"),
                // (7,30): error CS0579: Duplicate 'A' attribute
                //     static partial void Foo([A] int y) { }
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A").WithArguments("A"),
                // (18,41): error CS0579: Duplicate 'A' attribute
                //     static partial void Foo4([A][param: A] int y);
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A").WithArguments("A"),
                // (25,34): error CS0579: Duplicate 'A' attribute
                //     static partial void Foo6([A][A] int y) { }
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A").WithArguments("A"),
                // (15,17): warning CS0169: The field 'C.Foo3' is never used
                //     private int Foo3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Foo3").WithArguments("C.Foo3"));
        }

        [Fact]
        public void PartialMethodOverloads()
        {
            string source = @"
class A : System.Attribute { }

partial class C
{
    static partial void F([A] int y);
    static partial void F(int y, [A]int z);
}
";
            CompileAndVerify(source);
        }

        [WorkItem(543456, "DevDiv")]
        [Fact]
        public void StructLayoutFieldsAreUsed()
        {
            var source =
@"using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential)]
struct S
{
    int a, b, c;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(542662, "DevDiv")]
        [Fact]
        public void FalseDuplicateOnPartial()
        {
            var source =
@"
using System;
 
class A : Attribute { }
partial class Program
{
    static partial void Foo(int x);
    [A]
    static partial void Foo(int x) { }
 
    static partial void Foo();
    [A]
    static partial void Foo() { }
 
    static void Main()
    {
        Console.WriteLine(((Action) Foo).Method.GetCustomAttributesData().Count);
    }
}
";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(542652, "DevDiv")]
        [Fact]
        public void Bug9958()
        {
            var source =
@"
class A : System.Attribute { }
 
partial class C
{
    static partial void Foo<T,[A] S>();
    static partial void Foo<[A]>() { }
}";
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, null, options: opt);

            compilation.VerifyDiagnostics(
                // (7,32): error CS1001: Identifier expected
                //     static partial void Foo<[A]>() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">"),
                // (7,25): error CS0759: No defining declaration found for implementing declaration of partial method 'C.Foo<>()'
                //     static partial void Foo<[A]>() { }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "Foo").WithArguments("C.Foo<>()"));
        }

        [WorkItem(542909, "DevDiv")]
        [Fact]
        public void OverriddenPropertyMissingAccessor()
        {
            var source =
@"using System;
class A : Attribute
{
    public virtual int P { get; set; }
}
class B1 : A
{
    public override int P { get { return base.P; } }
}
class B2 : A
{
    public override int P { set { } }
}
[A(P=0)]
[B1(P=1)]
[B2(P = 2)]
class C
{
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(542899, "DevDiv")]
        [Fact]
        public void TwoSyntaxTrees()
        {
            var source =
                @"
using System.Reflection;
[assembly: AssemblyTitle(""EnterpriseLibraryExtensions"")]
";

            var source2 =
                        @"
using Microsoft.Practices.EnterpriseLibrary.Configuration.Design;
using EnterpriseLibraryExtensions;

[assembly: ConfigurationDesignManager(typeof(ExtensionDesignManager))]
";
            var compilation = CreateCompilationWithMscorlib(new string[] { source, source2 });
            compilation.GetDiagnostics();
        }

        [WorkItem(543785, "DevDiv")]
        [Fact]
        public void OpenGenericTypesUsedAsAttributeArgs()
        {
            var source =
@"
class Gen<T>
{
    [TypeAttribute(typeof(L1.L2.L3<>.L4<>))] public T Fld6;
}";
            var compilation = CreateCompilationWithMscorlib(source);

            Assert.NotEmpty(compilation.GetDiagnostics());

            compilation.VerifyDiagnostics(
                // (4,6): error CS0246: The type or namespace name 'TypeAttributeAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     [TypeAttribute(typeof(L1.L2.L3<>.L4<>))] public T Fld6;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "TypeAttribute").WithArguments("TypeAttributeAttribute").WithLocation(4, 6),
                // (4,6): error CS0246: The type or namespace name 'TypeAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     [TypeAttribute(typeof(L1.L2.L3<>.L4<>))] public T Fld6;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "TypeAttribute").WithArguments("TypeAttribute").WithLocation(4, 6),
                // (4,27): error CS0246: The type or namespace name 'L1' could not be found (are you missing a using directive or an assembly reference?)
                //     [TypeAttribute(typeof(L1.L2.L3<>.L4<>))] public T Fld6;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "L1").WithArguments("L1").WithLocation(4, 27),
                // (4,55): warning CS0649: Field 'Gen<T>.Fld6' is never assigned to, and will always have its default value 
                //     [TypeAttribute(typeof(L1.L2.L3<>.L4<>))] public T Fld6;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Fld6").WithArguments("Gen<T>.Fld6", "").WithLocation(4, 55));
        }

        [WorkItem(543914, "DevDiv")]
        [Fact]
        public void OpenGenericTypeInAttribute()
        {
            var source =
@"
class Gen<T> {}
class Gen2<T>: System.Attribute {}
	
[Gen]
[Gen2]
public class Test
{
	public static int Main()
	{
		return 1;
	}
}";
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateCompilationWithMscorlib(source, null, options: opt);

            compilation.VerifyDiagnostics(
                // (3,16): error CS0698: A generic type cannot derive from 'System.Attribute' because it is an attribute class
                // class Gen2<T>: System.Attribute {}
                Diagnostic(ErrorCode.ERR_GenericDerivingFromAttribute, "System.Attribute").WithArguments("System.Attribute"),
                // (5,2): error CS0404: Cannot apply attribute class 'Gen<T>' because it is generic
                // [Gen]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Gen").WithArguments("Gen<T>"),
                // (6,2): error CS0404: Cannot apply attribute class 'Gen2<T>' because it is generic
                // [Gen2]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Gen2").WithArguments("Gen2<T>"));
        }

        [Fact]
        public void GenericAttributeTypeFromILSource()
        {
            var ilSource = @"
.class public Gen<T> { }
.class public Gen2<T> extends [mscorlib] System.Attribute { }
";
            var csharpSource = @"
[Gen]
[Gen2]
public class Test
{
	public static int Main()
	{
		return 1;
	}
}";

            var comp = CreateCompilationWithCustomILSource(csharpSource, ilSource);

            comp.VerifyDiagnostics(
                // (2,2): error CS0404: Cannot apply attribute class 'Gen<T>' because it is generic
                // [Gen]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Gen").WithArguments("Gen<T>"),
                // (3,2): error CS0404: Cannot apply attribute class 'Gen2<T>' because it is generic
                // [Gen2]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Gen2").WithArguments("Gen2<T>"));
        }

        [WorkItem(544230, "DevDiv")]
        [Fact]
        public void Warnings_Unassigned_Unreferenced_AttributeTypeFields()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class B : Attribute
{
    public Type PublicField; // CS0649
    private Type PrivateField; // CS0169
    protected Type ProtectedField; // CS0649
    internal Type InternalField; // CS0649
}";

            var comp = CreateCompilationWithMscorlib(source);

            comp.VerifyDiagnostics(
                // (7,17): warning CS0649: Field 'B.PublicField' is never assigned to, and will always have its default value null
                //     public Type PublicField; // CS0649
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "PublicField").WithArguments("B.PublicField", "null"),
                // (8,18): warning CS0169: The field 'B.PrivateField' is never used
                //     private Type PrivateField; // CS0169
                Diagnostic(ErrorCode.WRN_UnreferencedField, "PrivateField").WithArguments("B.PrivateField"),
                // (9,20): warning CS0649: Field 'B.ProtectedField' is never assigned to, and will always have its default value null
                //     protected Type ProtectedField; // CS0649
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "ProtectedField").WithArguments("B.ProtectedField", "null"),
                // (10,19): warning CS0649: Field 'B.InternalField' is never assigned to, and will always have its default value null
                //     internal Type InternalField; // CS0649
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "InternalField").WithArguments("B.InternalField", "null"));
        }

        [WorkItem(544230, "DevDiv")]
        [Fact]
        public void No_Warnings_For_Assigned_AttributeTypeFields()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class A : Attribute
{
    public Type PublicField; // No CS0649
    private Type PrivateField; // No CS0649
    protected Type ProtectedField; // No CS0649
    internal Type InternalField; // No CS0649
    
    [A(PublicField = typeof(int))]
    [A(PrivateField = typeof(int))]
    [A(ProtectedField = typeof(int))]
    [A(InternalField = typeof(int))]
    static void Main()
    {
    }
}";

            var comp = CreateCompilationWithMscorlib(source);

            comp.VerifyDiagnostics(
                // (13,8): error CS0617: 'PrivateField' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                //     [A(PrivateField = typeof(int))]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "PrivateField").WithArguments("PrivateField"),
                // (14,8): error CS0617: 'ProtectedField' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                //     [A(ProtectedField = typeof(int))]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "ProtectedField").WithArguments("ProtectedField"),
                // (15,8): error CS0617: 'InternalField' is not a valid named attribute argument. Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static.
                //     [A(InternalField = typeof(int))]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgument, "InternalField").WithArguments("InternalField"));
        }

        [WorkItem(544351, "DevDiv")]
        [Fact]
        public void CS0182_ERR_BadAttributeArgument_Bug_12638()
        {
            var source = @"
using System;
 
[A(X = new Array[] { new[] { 1 } })]
class A : Attribute
{
    public object X;
}";

            var comp = CreateCompilationWithMscorlib(source);

            comp.VerifyDiagnostics(
                // (4,8): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(X = new Array[] { new[] { 1 } })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new Array[] { new[] { 1 } }").WithLocation(4, 8));
        }

        [WorkItem(544348, "DevDiv")]
        [Fact]
        public void CS0182_ERR_BadAttributeArgument_WithConversions()
        {
            var source =
@"using System;
 
[A((int)(object)""ABC"")]
class A : Attribute
{
    public A(int x) { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A((int)(object)"ABC")]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"(int)(object)""ABC""").WithLocation(3, 4));
        }

        [WorkItem(544348, "DevDiv")]
        [Fact]
        public void CS0182_ERR_BadAttributeArgument_WithConversions_02()
        {
            var source =
@"using System;
 
[A((object[])(object)( new [] { 1 }))]
class A : Attribute
{
    public A(object[] x) { }
}

[B((object[])(object)(new string[] { ""a"", null }))]
class B : Attribute
{
    public B(object[] x) { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A((object[])(object)( new [] { 1 }))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "(object[])(object)( new [] { 1 })").WithLocation(3, 4),
                // (9,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [B((object[])(object)(new string[] { "a", null }))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"(object[])(object)(new string[] { ""a"", null })").WithLocation(9, 4));
        }

        [WorkItem(529392, "DevDiv")]
        [Fact]
        public void CS0182_ERR_BadAttributeArgument_OpenType_ConstantValue()
        {
            // SPEC ERROR:  C# language specification does not explicitly disallow constant values of open types. For e.g.

            //  public class C<T>
            //  {
            //      public enum E { V }
            //  }
            //
            //  [SomeAttr(C<T>.E.V)]        // case (a): Constant value of open type.
            //  [SomeAttr(C<int>.E.V)]      // case (b): Constant value of constructed type.

            // Both expressions 'C<T>.E.V' and 'C<int>.E.V' satisfy the requirements for a valid attribute-argument-expression:
            //  (a) Its type is a valid attribute parameter type as per section 17.1.3 of the specification.
            //  (b) It has a compile time constant value.

            // However, native compiler disallows both the above cases.
            // We disallow case (a) as it cannot be serialized correctly, but allow case (b) to compile.

            var source = @"
using System;

class A : Attribute
{
    public object X;
}
 
class C<T>
{
    [A(X = C<T>.E.V)] 
    public enum E { V }
}";

            var comp = CreateCompilationWithMscorlib(source);

            comp.VerifyDiagnostics(
                // (11,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [A(X = C<T>.E.V)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "C<T>.E.V").WithLocation(11, 12));
        }

        [WorkItem(529392, "DevDiv")]
        [Fact]
        public void AttributeArgument_ConstructedType_ConstantValue()
        {
            // See comments for test CS0182_ERR_BadAttributeArgument_OpenType_ConstantValue

            var source = @"
using System;

class A : Attribute
{
    public object X;
    public static void Main()
    {
        typeof(C<>.E).GetCustomAttributes(false);
    }
}
 
public class C<T>
{
    [A(X = C<int>.E.V)]
    public enum E { V }
}";

            CompileAndVerify(source, expectedOutput: "");
        }

        [WorkItem(544512, "DevDiv")]
        [Fact]
        public void LambdaInAttributeArg()
        {
            string source = @"
public delegate void D();

public class myAttr : System.Attribute
{
    public D d
    {
        get { return () => { }; }
        set { }
    }

}

[myAttr(d = () => { })]
class X
{
    public static int Main()
    {
        return 1;
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,9): error CS0655: 'd' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [myAttr(d = () => { })]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "d").WithArguments("d").WithLocation(14, 9));
        }

        [WorkItem(544590, "DevDiv")]
        [Fact]
        public void LambdaInAttributeArg2()
        {
            string source = @"
using System;

[AttributeUsage(AttributeTargets.All)]
public class Foo : Attribute
{
    public Foo(int sName) { }
}

public class Class1 {
    [field: Foo(((System.Func<int>)(() => 5))())]
    public event EventHandler Click;

    public static void Main()
    {
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,17): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [field: Foo(((System.Func<int>)(() => 5))())]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "((System.Func<int>)(() => 5))()"),
                // (12,31): warning CS0067: The event 'Class1.Click' is never used
                //     public event EventHandler Click;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Click").WithArguments("Class1.Click"));
        }

        [WorkItem(545030, "DevDiv")]
        [Fact]
        public void UserDefinedAttribute_Bug13264()
        {
            string source = @"
namespace System.Runtime.InteropServices
{
    [DllImport] // Error
    class DllImportAttribute {}
}
namespace System
{
    [Object]   // Warning
    public class Object : System.Attribute {}
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,6): error CS0616: 'System.Runtime.InteropServices.DllImportAttribute' is not an attribute class
                //     [DllImport] // Error
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "DllImport").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                // (9,6): warning CS0436: The type 'System.Object' in '' conflicts with the imported type 'object' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     [Object]   // Warning
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Object").WithArguments("", "System.Object", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "object"));
        }

        [WorkItem(545241, "DevDiv")]
        [Fact]
        public void ConditionalAttributeOnAttribute()
        {
            string source = @"
using System;
using System.Diagnostics;

[Conditional(""A"")]
class Attr1 : Attribute
{

}

class Attr2 : Attribute
{

}

class Attr3 : Attr1
{

}

[Attr1, Attr2, Attr3]
class Test
{
}
";

            Action<ModuleSymbol> sourceValidator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var attrs = type.GetAttributes();
                Assert.Equal(3, attrs.Length);
            };

            Action<ModuleSymbol> metadataValidator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var attrs = type.GetAttributes();
                Assert.Equal(1, attrs.Length); // only one is not conditional
                Assert.Equal("Attr2", attrs.Single().AttributeClass.Name);
            };

            CompileAndVerify(source, sourceSymbolValidator: sourceValidator, symbolValidator: metadataValidator);
        }

        [WorkItem(545499, "DevDiv")]
        [Fact]
        public void IncompleteMethodParamAttribute()
        {
            string source = @"
using System;

public class MyAttribute2 : Attribute
{
	public Type[] Types;
}

public class Test
{
	public void foo([MyAttribute2(Types = new Type[
";
            var compilation = CreateCompilationWithMscorlib(source);

            Assert.NotEmpty(compilation.GetDiagnostics());
        }

        [Fact, WorkItem(545556, "DevDiv")]
        public void NameLookupInDelegateParameterAttribute()
        {
            var source = @"
using System;
 
class A : Attribute
{
    new const int Equals = 1;
    delegate void F([A(Equals)] int x);
    public A(int x) { }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,24): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
                //     delegate void F([A(Equals)] int x);
                Diagnostic(ErrorCode.ERR_BadArgType, "Equals").WithArguments("1", "method group", "int"));
        }

        [Fact, WorkItem(546234, "DevDiv")]
        public void AmbiguousClassNamespaceLookup()
        {
            // One from source, one from PE
            var source = @"
using System;
[System]
class System : Attribute
{
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            compilation.VerifyDiagnostics(
                // (2,7): warning CS0437: The type 'System' in '' conflicts with the imported namespace 'System' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                // using System;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggNs, "System").WithArguments("", "System", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System"),
                // (2,7): error CS0138: A 'using namespace' directive can only be applied to namespaces; 'System' is a type not a namespace. Consider a 'using static' directive instead
                // using System;
                Diagnostic(ErrorCode.ERR_BadUsingNamespace, "System").WithArguments("System").WithLocation(2, 7),
                // (4,16): error CS0246: The type or namespace name 'Attribute' could not be found (are you missing a using directive or an assembly reference?)
                // class System : Attribute
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Attribute").WithArguments("Attribute"),
                // (3,2): error CS0616: 'System' is not an attribute class
                // [System]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "System").WithArguments("System"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));

            source = @"
[assembly: X]
namespace X
{
}
";
            var source2 = @"
using System;
public class X: Attribute
{
}
";
            var comp1 = CreateCompilationWithMscorlib(source2, assemblyName: "Temp0").ToMetadataReference();
            CreateCompilationWithMscorlib(source, references: new[] { comp1 }).VerifyDiagnostics(
                // (2,12): error CS0616: 'X' is not an attribute class
                // [assembly: X]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "X").WithArguments("X"));

            // Multiple from PE, none from Source
            source2 = @"
using System;
public class X
{
}
";


            var source3 = @"
namespace X
{
}
";
            var source4 = @"
[X]
class Y
{
}
";
            comp1 = CreateCompilationWithMscorlib(source2, assemblyName: "Temp1").ToMetadataReference();
            var comp2 = CreateCompilation(source3, assemblyName: "Temp2").ToMetadataReference();
            var comp3 = CreateCompilationWithMscorlib(source4, references: new[] { comp1, comp2 });
            comp3.VerifyDiagnostics(
                // (2,2): error CS0434: The namespace 'X' in 'Temp2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with the type 'X' in 'Temp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                // [X]
                Diagnostic(ErrorCode.ERR_SameFullNameNsAgg, "X").WithArguments("Temp2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "X", "Temp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "X"));

            // Multiple from PE, one from Source: Failure
            var source5 = @"
[X]
class X
{
}
";
            comp3 = CreateCompilationWithMscorlib(source5, references: new[] { comp1, comp2 });
            comp3.VerifyDiagnostics(
                // (2,2): error CS0616: 'X' is not an attribute class
                // [X]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "X").WithArguments("X"));

            // Multiple from PE, one from Source: Success
            source5 = @"
using System;
[X]
class X: Attribute
{
}
";
            CompileAndVerify(source5, additionalRefs: new[] { comp1, comp2 });

            // Multiple from PE, multiple from Source
            var source6 = @"
[X]
class X
{
}

namespace X
{
}
";
            comp3 = CreateCompilationWithMscorlib(source6, references: new[] { comp1, comp2 });
            comp3.VerifyDiagnostics(
                // (3,7): error CS0101: The namespace '<global namespace>' already contains a definition for 'X'
                // class X
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "X").WithArguments("X", "<global namespace>"));

            // Multiple from PE, one from Source with alias
            var source7 = @"
using System;
using X = Foo;
[X]
class Foo: Attribute
{
}
";
            comp3 = CreateCompilationWithMscorlib(source7, references: new[] { comp1, comp2 });
            comp3.VerifyDiagnostics(
                // (4,2): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'X'
                // [X]
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "X").WithArguments("X", "<global namespace>"),
                // (4,2): error CS0616: 'X' is not an attribute class
                // [X]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "X").WithArguments("X"));
        }

        [WorkItem(546283, "DevDiv")]
        [Fact]
        public void ApplyIndexerNameAttributeTwice()
        {
            var source =
@"using System.Runtime.CompilerServices;

public class IA
{
	[IndexerName(""ItemX"")]
	[IndexerName(""ItemY"")]
	public virtual int this[int index]
	{
		get	{ return 1;}
		set	{}
	}
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (6,3): error CS0579: Duplicate 'IndexerName' attribute
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "IndexerName").WithArguments("IndexerName"));

            var indexer = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("IA").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            Assert.Equal("ItemX", indexer.MetadataName); //First one wins.
        }

        [WorkItem(530524, "DevDiv")]
        [Fact]
        public void PEMethodSymbolExtensionAttribute1()
        {
            var source1 =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly '<<GeneratedFileName>>'
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
}
.class public E
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public static void M(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}";
            var reference1 = CompileIL(source1, appendDefaultHeader: false);
            var source2 =
@"class C
{
    static void M(object o)
    {
        o.M();
    }
}";
            var compilation = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation.VerifyDiagnostics();
            var assembly = compilation.Assembly;
            Assert.Equal(assembly.GetAttributes().Length, 0);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(type.GetAttributes().Length, 0);
            var method = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetMember<PEMethodSymbol>("M");
            Assert.Equal(method.GetAttributes().Length, 0);
            Assert.True(method.TestIsExtensionBitSet);
            Assert.True(method.TestIsExtensionBitTrue);
            Assert.True(method.IsExtensionMethod);
        }

        [WorkItem(530524, "DevDiv")]
        [Fact]
        public void PEMethodSymbolExtensionAttribute2()
        {
            var source1 =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly '<<GeneratedFileName>>'
{
}
.class public E
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public static void M(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}";
            var reference1 = CompileIL(source1, appendDefaultHeader: false);
            var source2 =
@"class C
{
    static void M(object o)
    {
        o.M();
    }
}";
            var compilation = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation.VerifyDiagnostics(); // we now recognize the extension method even without the assembly-level attribute

            var assembly = compilation.Assembly;
            Assert.Equal(assembly.GetAttributes().Length, 0);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(type.GetAttributes().Length, 0);
            var method = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetMember<PEMethodSymbol>("M");
            Assert.Equal(method.GetAttributes().Length, 0);
            Assert.True(method.TestIsExtensionBitSet);
            Assert.True(method.TestIsExtensionBitTrue);
            Assert.True(method.IsExtensionMethod);
        }

        [WorkItem(530524, "DevDiv")]
        [Fact]
        public void PEMethodSymbolExtensionAttribute3()
        {
            var source1 =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
}
.class public E
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public static void M(object o)
  {
    ret
  }
}";
            var reference1 = CompileIL(source1, appendDefaultHeader: false);
            var source2 =
@"class C
{
    static void M(object o)
    {
        o.M();
    }
}";
            var compilation = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation.VerifyDiagnostics(
                // (5,11): error CS1061: 'object' does not contain a definition for 'M' and no extension method 'M' accepting a 
                // first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         o.M();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("object", "M"));

            var assembly = compilation.Assembly;
            Assert.Equal(0, assembly.GetAttributes().Length);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, type.GetAttributes().Length);
            var method = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetMember<PEMethodSymbol>("M");
            Assert.Equal(0, method.GetAttributes().Length);
            Assert.True(method.TestIsExtensionBitSet);
            Assert.False(method.TestIsExtensionBitTrue);
            Assert.False(method.IsExtensionMethod);
        }

        [WorkItem(530310, "DevDiv")]
        [Fact]
        public void PEParameterSymbolParamArrayAttribute()
        {
            var source1 =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>'
{
}
.class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public static void M(int32 x, int32[] y)
  {
    .param [2]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}";
            var reference1 = CompileIL(source1, appendDefaultHeader: false);
            var source2 =
@"class C
{
    static void Main(string[] args)
    {
        A.M(1, 2, 3);
        A.M(1, 2, 3, 4);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation.VerifyDiagnostics();

            var method = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PEMethodSymbol>("M");
            Assert.Equal(0, method.GetAttributes().Length);
            var yParam = method.Parameters[1];
            Assert.True(yParam.IsParams);
            Assert.Equal(0, yParam.GetAttributes().Length);
        }

        [WorkItem(546490, "DevDiv")]
        [Fact]
        public void Bug15984()
        {
            var source1 =
@"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern FSharp.Core {}
.assembly '<<GeneratedFileName>>'
{
}

.class public abstract auto ansi sealed Library1.Foo
       extends [mscorlib]System.Object
{
  .custom instance void [FSharp.Core]Microsoft.FSharp.Core.CompilationMappingAttribute::.ctor(valuetype [FSharp.Core]Microsoft.FSharp.Core.SourceConstructFlags) = ( 01 00 07 00 00 00 00 00 ) 
  .method public static int32  inc(int32 x) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  add
    IL_0004:  ret
  } // end of method Foo::inc

} // end of class Library1.Foo
";
            var reference1 = CompileIL(source1, appendDefaultHeader: false);

            var compilation = CreateCompilationWithMscorlib("", new[] { reference1 });

            var type = compilation.GetTypeByMetadataName("Library1.Foo");
            Assert.Equal(0, type.GetAttributes()[0].ConstructorArguments.Count());
        }

        [WorkItem(611177, "DevDiv")]
        [Fact]
        public void GenericAttributeType()
        {
            var source = @"
using System;

[A<>]
[A<int>]
[B]
[B<>]
[B<int>]
[C]
[C<>]
[C<int>]
[C<,>]
[C<int, int>]
class Test
{
}

public class A : Attribute
{
}

public class B<T> : Attribute
{
}

public class C<T, U> : Attribute
{
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(

                // NOTE: Dev11 reports ERR_AttributeCantBeGeneric for these, but this makes more sense.

                // (4,2): error CS0308: The non-generic type 'A' cannot be used with type arguments
                // [A<>]
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "A<>").WithArguments("A", "type"),
                // (5,2): error CS0308: The non-generic type 'A' cannot be used with type arguments
                // [A<int>]
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "A<int>").WithArguments("A", "type"),

                // (6,2): error CS0404: Cannot apply attribute class 'B<T>' because it is generic
                // [B]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "B").WithArguments("B<T>"),
                // (7,2): error CS0404: Cannot apply attribute class 'B<T>' because it is generic
                // [B<>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "B<>").WithArguments("B<T>"),
                // (8,2): error CS0404: Cannot apply attribute class 'B<T>' because it is generic
                // [B<int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "B<int>").WithArguments("B<T>"),
                // (9,2): error CS0404: Cannot apply attribute class 'C<T, U>' because it is generic
                // [C]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "C").WithArguments("C<T, U>"),
                // (10,2): error CS0404: Cannot apply attribute class 'C<T, U>' because it is generic
                // [C<>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "C<>").WithArguments("C<T, U>"),
                // (11,2): error CS0404: Cannot apply attribute class 'C<T, U>' because it is generic
                // [C<int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "C<int>").WithArguments("C<T, U>"),
                // (12,2): error CS0404: Cannot apply attribute class 'C<T, U>' because it is generic
                // [C<,>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "C<,>").WithArguments("C<T, U>"),
                // (13,2): error CS0404: Cannot apply attribute class 'C<T, U>' because it is generic
                // [C<int, int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "C<int, int>").WithArguments("C<T, U>"),

                // (22,21): error CS0698: A generic type cannot derive from 'System.Attribute' because it is an attribute class
                // public class B<T> : Attribute
                Diagnostic(ErrorCode.ERR_GenericDerivingFromAttribute, "Attribute").WithArguments("System.Attribute"),
                // (26,24): error CS0698: A generic type cannot derive from 'System.Attribute' because it is an attribute class
                // public class C<T, U> : Attribute
                Diagnostic(ErrorCode.ERR_GenericDerivingFromAttribute, "Attribute").WithArguments("System.Attribute"));
        }

        [WorkItem(611177, "DevDiv")]
        [Fact]
        public void AliasedGenericAttributeType_Source()
        {
            var source = @"
using System;
using Alias = C<int>;

[Alias]
[Alias<>]
[Alias<int>]
class Test
{
}

public class C<T> : Attribute
{
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (5,2): error CS0404: Cannot apply attribute class 'C<int>' because it is generic
                // [Alias]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Alias").WithArguments("C<int>"),
                // (6,2): error CS0404: Cannot apply attribute class 'C<int>' because it is generic
                // [Alias<>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Alias<>").WithArguments("C<int>"),
                // (7,2): error CS0404: Cannot apply attribute class 'C<int>' because it is generic
                // [Alias<int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Alias<int>").WithArguments("C<int>"),

                // (12,21): error CS0698: A generic type cannot derive from 'System.Attribute' because it is an attribute class
                // public class C<T> : Attribute
                Diagnostic(ErrorCode.ERR_GenericDerivingFromAttribute, "Attribute").WithArguments("System.Attribute"));
        }

        [WorkItem(611177, "DevDiv")]
        [Fact]
        public void AliasedGenericAttributeType_Metadata()
        {
            var il = @"
.class public auto ansi beforefieldinit C`1<T>
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Attribute::.ctor()
    ret
  }
}
";

            var source = @"
using Alias = C<int>;

[Alias]
[Alias<>]
[Alias<int>]
class Test
{
}
";

            // NOTE: Dev11 does not give an error for "[Alias]" - it just silently drops the
            // attribute at emit-time.
            var comp = CreateCompilationWithCustomILSource(source, il);
            comp.VerifyDiagnostics(
                // (4,2): error CS0404: Cannot apply attribute class 'C<int>' because it is generic
                // [Alias]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Alias").WithArguments("C<int>"),
                // (5,2): error CS0404: Cannot apply attribute class 'C<int>' because it is generic
                // [Alias<>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Alias<>").WithArguments("C<int>"),
                // (6,2): error CS0404: Cannot apply attribute class 'C<int>' because it is generic
                // [Alias<int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Alias<int>").WithArguments("C<int>"));
        }

        [WorkItem(611177, "DevDiv")]
        [Fact]
        public void AliasedGenericAttributeType_Nested()
        {
            var source = @"
using InnerAlias = Outer<int>.Inner;
using OuterAlias = Outer<int>;

[InnerAlias]
class Test
{
    [OuterAlias.Inner]
    static void Main()
    {
    }
}

public class Outer<T>
{
    // Not a subtype of Attribute, since that wouldn't compile.
    public class Inner 
    {
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
            // (5,2): error CS0404: Cannot apply attribute class 'Outer<int>.Inner' because it is generic
            // [InnerAlias]
            Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "InnerAlias").WithArguments("Outer<int>.Inner"),
            // (8,17): error CS0404: Cannot apply attribute class 'Outer<int>.Inner' because it is generic
            //     [OuterAlias.Inner]
            Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Inner").WithArguments("Outer<int>.Inner"));
        }

        [WorkItem(687816, "DevDiv")]
        [Fact]
        public void VerbatimAliasVersusNonVerbatimAlias()
        {
            var source = @"
using Action = A.ActionAttribute;
using ActionAttribute = A.ActionAttribute;

namespace A
{
    class ActionAttribute : System.Attribute { }
}

class Program
{
    [Action]
    static void Main(string[] args) { }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,6): error CS1614: 'Action' is ambiguous between 'A.ActionAttribute' and 'A.ActionAttribute'; use either '@Action' or 'ActionAttribute'
                //     [Action]
                Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "Action").WithArguments("Action", "A.ActionAttribute", "A.ActionAttribute"));
        }

        [WorkItem(687816, "DevDiv")]
        [Fact]
        public void DeclarationVersusNonVerbatimAlias()
        {
            var source = @"
using Action = A.ActionAttribute;
using A;

namespace A
{
    class ActionAttribute : System.Attribute { }
}

class Program
{
    [Action]
    static void Main(string[] args) { }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,6): error CS1614: 'Action' is ambiguous between 'A.ActionAttribute' and 'A.ActionAttribute'; use either '@Action' or 'ActionAttribute'
                //     [Action]
                Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "Action").WithArguments("Action", "A.ActionAttribute", "A.ActionAttribute"));
        }

        [WorkItem(728865, "DevDiv")]
        [Fact]
        public void Repro728865()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Yeti;

namespace PFxIntegration
{
    
    public class ProducerConsumerScenario
    {

        static void Main(string[] args)
        {
            Type program = typeof(ProducerConsumerScenario);
            MethodInfo methodInfo = program.GetMethod(""ProducerConsumer"");
            Object[] myAttributes = methodInfo.GetCustomAttributes(false); ;
            if (myAttributes.Length > 0)
            {
                Console.WriteLine(""\r\nThe attributes for the method - {0} - are: \r\n"", methodInfo);
                for (int j = 0; j < myAttributes.Length; j++)
                    Console.WriteLine(""The type of the attribute is {0}"", myAttributes[j]);
            }
        }

        public enum CollectionType
        {
            Default,
            Queue,
            Stack,
            Bag
        }

        public ProducerConsumerScenario()
        {

        }

        [CartesianRowData(
            new int[] { 5, 100, 100000 },
            new CollectionType[] { CollectionType.Default, CollectionType.Queue, CollectionType.Stack, CollectionType.Bag })]
        public void ProducerConsumer(int inputSize, CollectionType collectionType)
        {
            Console.WriteLine(""Hello"");

        }

    }
}

namespace Microsoft.Yeti
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CartesianRowDataAttribute : Attribute
    {
        public CartesianRowDataAttribute()
        {
        }

        public CartesianRowDataAttribute(params object[] data)
        {
            IEnumerable<object>[] asEnum = new IEnumerable<object>[data.Length];

            for (int i = 0; i < data.Length; ++i)
            {
                WrapEnum((IEnumerable)data[i]);
            }
        }

        static void WrapEnum(IEnumerable x)
        {
            foreach (object a in x)
            {
                Console.WriteLine("" - "" + a + "" -"");
            }
        }
    } // class
} // namespace
";

            CompileAndVerify(source, expectedOutput: @"
 - 5 -
 - 100 -
 - 100000 -
 - Default -
 - Queue -
 - Stack -
 - Bag -

The attributes for the method - Void ProducerConsumer(Int32, CollectionType) - are: 

The type of the attribute is Microsoft.Yeti.CartesianRowDataAttribute
");
        }

        [WorkItem(728865, "DevDiv")]
        [Fact]
        public void StringArrayArgument1()
        {
            var source = @"
using System;

public class Test
{
    [ArrayOnlyAttribute(new string[] { ""A"" })] //error
    [ObjectOnlyAttribute(new string[] { ""A"" })]
    [ArrayOrObjectAttribute(new string[] { ""A"" })] //error, even though the object ctor would work
    void M1() { }

    [ArrayOnlyAttribute(null)]
    [ObjectOnlyAttribute(null)]
    [ArrayOrObjectAttribute(null)] //array
    void M2() { }

    [ArrayOnlyAttribute(new object[] { ""A"" })]
    [ObjectOnlyAttribute(new object[] { ""A"" })]
    [ArrayOrObjectAttribute(new object[] { ""A"" })] //array
    void M3() { }
}

public class ArrayOnlyAttribute : Attribute
{
    public ArrayOnlyAttribute(object[] array) { }
}

public class ObjectOnlyAttribute : Attribute
{
    public ObjectOnlyAttribute(object o) { }
}

public class ArrayOrObjectAttribute : Attribute
{
    public ArrayOrObjectAttribute(object[] array) { }
    public ArrayOrObjectAttribute(object o) { }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,6): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [ArrayOnlyAttribute(new string[] { "A" })] //error
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"ArrayOnlyAttribute(new string[] { ""A"" })"),
                // (8,6): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [ArrayOrObjectAttribute(new string[] { "A" })] //error, even though the object ctor would work
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"ArrayOrObjectAttribute(new string[] { ""A"" })"));

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var method1 = type.GetMember<MethodSymbol>("M1");
            var method2 = type.GetMember<MethodSymbol>("M2");
            var method3 = type.GetMember<MethodSymbol>("M3");

            var attrs1 = method1.GetAttributes();
            var value1 = new string[] { "A" };
            attrs1[0].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[1].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[2].VerifyValue(0, TypedConstantKind.Array, value1);

            var attrs2 = method2.GetAttributes();
            attrs2[0].VerifyValue(0, TypedConstantKind.Array, (object[])null);
            attrs2[1].VerifyValue(0, TypedConstantKind.Primitive, (object)null);
            attrs2[2].VerifyValue(0, TypedConstantKind.Array, (object[])null);

            var attrs3 = method3.GetAttributes();
            var value3 = new object[] { "A" };
            attrs3[0].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[1].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[2].VerifyValue(0, TypedConstantKind.Array, value3);
        }

        [WorkItem(728865, "DevDiv")]
        [Fact]
        public void StringArrayArgument2()
        {
            var source = @"
using System;

public class Test
{
    [ParamArrayOnlyAttribute(new string[] { ""A"" })] //error
    [ObjectOnlyAttribute(new string[] { ""A"" })]
    [ParamArrayOrObjectAttribute(new string[] { ""A"" })] //error, even though the object ctor would work
    void M1() { }

    [ParamArrayOnlyAttribute(null)]
    [ObjectOnlyAttribute(null)]
    [ParamArrayOrObjectAttribute(null)] //array
    void M2() { }

    [ParamArrayOnlyAttribute(new object[] { ""A"" })]
    [ObjectOnlyAttribute(new object[] { ""A"" })]
    [ParamArrayOrObjectAttribute(new object[] { ""A"" })] //array
    void M3() { }

    [ParamArrayOnlyAttribute(""A"")]
    [ObjectOnlyAttribute(""A"")]
    [ParamArrayOrObjectAttribute(""A"")] //object
    void M4() { }
}

public class ParamArrayOnlyAttribute : Attribute
{
    public ParamArrayOnlyAttribute(params object[] array) { }
}

public class ObjectOnlyAttribute : Attribute
{
    public ObjectOnlyAttribute(object o) { }
}

public class ParamArrayOrObjectAttribute : Attribute
{
    public ParamArrayOrObjectAttribute(params object[] array) { }
    public ParamArrayOrObjectAttribute(object o) { }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,6): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [ParamArrayOnlyAttribute(new string[] { "A" })] //error
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"ParamArrayOnlyAttribute(new string[] { ""A"" })"),
                // (8,6): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [ParamArrayOrObjectAttribute(new string[] { "A" })] //error, even though the object ctor would work
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"ParamArrayOrObjectAttribute(new string[] { ""A"" })"));

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var method1 = type.GetMember<MethodSymbol>("M1");
            var method2 = type.GetMember<MethodSymbol>("M2");
            var method3 = type.GetMember<MethodSymbol>("M3");
            var method4 = type.GetMember<MethodSymbol>("M4");

            // As in the test above (i.e. not affected by params modifier).
            var attrs1 = method1.GetAttributes();
            var value1 = new string[] { "A" };
            attrs1[0].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[1].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[2].VerifyValue(0, TypedConstantKind.Array, value1);

            // As in the test above (i.e. not affected by params modifier).
            var attrs2 = method2.GetAttributes();
            attrs2[0].VerifyValue(0, TypedConstantKind.Array, (object[])null);
            attrs2[1].VerifyValue(0, TypedConstantKind.Primitive, (object)null);
            attrs2[2].VerifyValue(0, TypedConstantKind.Array, (object[])null);

            // As in the test above (i.e. not affected by params modifier).
            var attrs3 = method3.GetAttributes();
            var value3 = new object[] { "A" };
            attrs3[0].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[1].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[2].VerifyValue(0, TypedConstantKind.Array, value3);

            var attrs4 = method4.GetAttributes();
            attrs4[0].VerifyValue(0, TypedConstantKind.Array, new object[] { "A" });
            attrs4[1].VerifyValue(0, TypedConstantKind.Primitive, "A");
            attrs4[2].VerifyValue(0, TypedConstantKind.Primitive, "A");
        }

        [WorkItem(728865, "DevDiv")]
        [Fact]
        public void StringArrayArgument3()
        {
            var source = @"
using System;

public class Test
{
    [StringOnlyAttribute(new string[] { ""A"" })]
    [ObjectOnlyAttribute(new string[] { ""A"" })] //error
    [StringOrObjectAttribute(new string[] { ""A"" })] //string
    void M1() { }

    [StringOnlyAttribute(null)]
    [ObjectOnlyAttribute(null)]
    [StringOrObjectAttribute(null)] //string
    void M2() { }

    //[StringOnlyAttribute(new object[] { ""A"" })] //overload resolution failure
    [ObjectOnlyAttribute(new object[] { ""A"" })]
    [StringOrObjectAttribute(new object[] { ""A"" })] //object
    void M3() { }

    [StringOnlyAttribute(""A"")]
    [ObjectOnlyAttribute(""A"")]
    [StringOrObjectAttribute(""A"")] //string
    void M4() { }
}

public class StringOnlyAttribute : Attribute
{
    public StringOnlyAttribute(params string[] array) { }
}

public class ObjectOnlyAttribute : Attribute
{
    public ObjectOnlyAttribute(params object[] array) { }
}

public class StringOrObjectAttribute : Attribute
{
    public StringOrObjectAttribute(params string[] array) { }
    public StringOrObjectAttribute(params object[] array) { }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (7,6): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [ObjectOnlyAttribute(new string[] { "A" })] //error
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"ObjectOnlyAttribute(new string[] { ""A"" })"));

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var method1 = type.GetMember<MethodSymbol>("M1");
            var method2 = type.GetMember<MethodSymbol>("M2");
            var method3 = type.GetMember<MethodSymbol>("M3");
            var method4 = type.GetMember<MethodSymbol>("M4");

            var attrs1 = method1.GetAttributes();
            var value1 = new string[] { "A" };
            attrs1[0].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[1].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[2].VerifyValue(0, TypedConstantKind.Array, value1);

            var attrs2 = method2.GetAttributes();
            attrs2[0].VerifyValue(0, TypedConstantKind.Array, (object[])null);
            attrs2[1].VerifyValue(0, TypedConstantKind.Array, (object[])null);
            attrs2[2].VerifyValue(0, TypedConstantKind.Array, (string[])null);

            var attrs3 = method3.GetAttributes();
            var value3 = new object[] { "A" };
            attrs3[0].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[1].VerifyValue(0, TypedConstantKind.Array, value3);

            var attrs4 = method4.GetAttributes();
            var value4 = new object[] { "A" };
            attrs4[0].VerifyValue(0, TypedConstantKind.Array, value4);
            attrs4[1].VerifyValue(0, TypedConstantKind.Array, value4);
            attrs4[2].VerifyValue(0, TypedConstantKind.Array, value4);
        }

        [WorkItem(728865, "DevDiv")]
        [Fact]
        public void IntArrayArgument1()
        {
            var source = @"
using System;

public class Test
{
    //[ArrayOnlyAttribute(new int[] { 1 })] //overload resolution failure
    [ObjectOnlyAttribute(new int[] { 1 })]
    [ArrayOrObjectAttribute(new int[] { 1 })] //object
    void M1() { }

    [ArrayOnlyAttribute(null)]
    [ObjectOnlyAttribute(null)]
    [ArrayOrObjectAttribute(null)] //array
    void M2() { }

    [ArrayOnlyAttribute(new object[] { 1 })]
    [ObjectOnlyAttribute(new object[] { 1 })]
    [ArrayOrObjectAttribute(new object[] { 1 })] //array
    void M3() { }
}

public class ArrayOnlyAttribute : Attribute
{
    public ArrayOnlyAttribute(object[] array) { }
}

public class ObjectOnlyAttribute : Attribute
{
    public ObjectOnlyAttribute(object o) { }
}

public class ArrayOrObjectAttribute : Attribute
{
    public ArrayOrObjectAttribute(object[] array) { }
    public ArrayOrObjectAttribute(object o) { }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var method1 = type.GetMember<MethodSymbol>("M1");
            var method2 = type.GetMember<MethodSymbol>("M2");
            var method3 = type.GetMember<MethodSymbol>("M3");

            var attrs1 = method1.GetAttributes();
            var value1 = new int[] { 1 };
            attrs1[0].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[1].VerifyValue(0, TypedConstantKind.Array, value1);

            var attrs2 = method2.GetAttributes();
            attrs2[0].VerifyValue(0, TypedConstantKind.Array, (object[])null);
            attrs2[1].VerifyValue(0, TypedConstantKind.Primitive, (object)null);
            attrs2[2].VerifyValue(0, TypedConstantKind.Array, (object[])null);

            var attrs3 = method3.GetAttributes();
            var value3 = new object[] { 1 };
            attrs3[0].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[1].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[2].VerifyValue(0, TypedConstantKind.Array, value3);
        }

        [WorkItem(728865, "DevDiv")]
        [Fact]
        public void IntArrayArgument2()
        {
            var source = @"
using System;

public class Test
{
    //[ParamArrayOnlyAttribute(new int[] { 1 })] //overload resolution failure
    [ObjectOnlyAttribute(new int[] { 1 })]
    [ParamArrayOrObjectAttribute(new int[] { 1 })] //object
    void M1() { }

    [ParamArrayOnlyAttribute(null)]
    [ObjectOnlyAttribute(null)]
    [ParamArrayOrObjectAttribute(null)] //array
    void M2() { }

    [ParamArrayOnlyAttribute(new object[] { 1 })]
    [ObjectOnlyAttribute(new object[] { 1 })]
    [ParamArrayOrObjectAttribute(new object[] { 1 })] //array
    void M3() { }

    [ParamArrayOnlyAttribute(1)]
    [ObjectOnlyAttribute(1)]
    [ParamArrayOrObjectAttribute(1)] //object
    void M4() { }
}

public class ParamArrayOnlyAttribute : Attribute
{
    public ParamArrayOnlyAttribute(params object[] array) { }
}

public class ObjectOnlyAttribute : Attribute
{
    public ObjectOnlyAttribute(object o) { }
}

public class ParamArrayOrObjectAttribute : Attribute
{
    public ParamArrayOrObjectAttribute(params object[] array) { }
    public ParamArrayOrObjectAttribute(object o) { }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var method1 = type.GetMember<MethodSymbol>("M1");
            var method2 = type.GetMember<MethodSymbol>("M2");
            var method3 = type.GetMember<MethodSymbol>("M3");
            var method4 = type.GetMember<MethodSymbol>("M4");

            // As in the test above (i.e. not affected by params modifier).
            var attrs1 = method1.GetAttributes();
            var value1 = new int[] { 1 };
            attrs1[0].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[1].VerifyValue(0, TypedConstantKind.Array, value1);

            // As in the test above (i.e. not affected by params modifier).
            var attrs2 = method2.GetAttributes();
            attrs2[0].VerifyValue(0, TypedConstantKind.Array, (object[])null);
            attrs2[1].VerifyValue(0, TypedConstantKind.Primitive, (object)null);
            attrs2[2].VerifyValue(0, TypedConstantKind.Array, (object[])null);

            // As in the test above (i.e. not affected by params modifier).
            var attrs3 = method3.GetAttributes();
            var value3 = new object[] { 1 };
            attrs3[0].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[1].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[2].VerifyValue(0, TypedConstantKind.Array, value3);

            var attrs4 = method4.GetAttributes();
            attrs4[0].VerifyValue(0, TypedConstantKind.Array, new object[] { 1 });
            attrs4[1].VerifyValue(0, TypedConstantKind.Primitive, 1);
            attrs4[2].VerifyValue(0, TypedConstantKind.Primitive, 1);
        }

        [WorkItem(728865, "DevDiv")]
        [Fact]
        public void IntArrayArgument3()
        {
            var source = @"
using System;

public class Test
{
    [IntOnlyAttribute(new int[] { 1 })]
    [ObjectOnlyAttribute(new int[] { 1 })]
    [IntOrObjectAttribute(new int[] { 1 })] //int
    void M1() { }

    [IntOnlyAttribute(null)]
    [ObjectOnlyAttribute(null)]
    //[IntOrObjectAttribute(null)] //ambiguous
    void M2() { }

    //[IntOnlyAttribute(new object[] { 1 })] //overload resolution failure
    [ObjectOnlyAttribute(new object[] { 1 })]
    [IntOrObjectAttribute(new object[] { 1 })] //object
    void M3() { }

    [IntOnlyAttribute(1)]
    [ObjectOnlyAttribute(1)]
    [IntOrObjectAttribute(1)] //int
    void M4() { }
}

public class IntOnlyAttribute : Attribute
{
    public IntOnlyAttribute(params int[] array) { }
}

public class ObjectOnlyAttribute : Attribute
{
    public ObjectOnlyAttribute(params object[] array) { }
}

public class IntOrObjectAttribute : Attribute
{
    public IntOrObjectAttribute(params int[] array) { }
    public IntOrObjectAttribute(params object[] array) { }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var method1 = type.GetMember<MethodSymbol>("M1");
            var method2 = type.GetMember<MethodSymbol>("M2");
            var method3 = type.GetMember<MethodSymbol>("M3");
            var method4 = type.GetMember<MethodSymbol>("M4");

            var attrs1 = method1.GetAttributes();
            var value1 = new int[] { 1 };
            attrs1[0].VerifyValue(0, TypedConstantKind.Array, value1);
            attrs1[1].VerifyValue(0, TypedConstantKind.Array, new object[] { value1 });
            attrs1[2].VerifyValue(0, TypedConstantKind.Array, value1);

            var attrs2 = method2.GetAttributes();
            var value2 = (object[])null;
            attrs2[0].VerifyValue(0, TypedConstantKind.Array, value2);
            attrs2[1].VerifyValue(0, TypedConstantKind.Array, value2);

            var attrs3 = method3.GetAttributes();
            var value3 = new object[] { 1 };
            attrs3[0].VerifyValue(0, TypedConstantKind.Array, value3);
            attrs3[1].VerifyValue(0, TypedConstantKind.Array, value3);

            var attrs4 = method4.GetAttributes();
            var value4 = new object[] { 1 };
            attrs4[0].VerifyValue(0, TypedConstantKind.Array, value4);
            attrs4[1].VerifyValue(0, TypedConstantKind.Array, value4);
            attrs4[2].VerifyValue(0, TypedConstantKind.Array, value4);
        }

        [WorkItem(739630, "DevDiv")]
        [Fact]
        public void NullVersusEmptyArray()
        {
            var source = @"
using System;

public class ArrayAttribute : Attribute
{
    public int[] field;

    public ArrayAttribute(int[] param) { }
}

public class Test
{
    [Array(null)]
    void M0() { }

    [Array(new int[] { })]
    void M1() { }

    [Array(null, field=null)]
    void M2() { }

    [Array(new int[] { }, field = null)]
    void M3() { }

    [Array(null, field = new int[] { })]
    void M4() { }

    [Array(new int[] { }, field = new int[] { })]
    void M5() { }

    static void Main() { }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var methods = Enumerable.Range(0, 6).Select(i => type.GetMember<MethodSymbol>("M" + i));
            var attrs = methods.Select(m => m.GetAttributes().Single()).ToArray();

            var nullArray = (int[])null;
            var emptyArray = new int[0];
            const string fieldName = "field";

            attrs[0].VerifyValue(0, TypedConstantKind.Array, nullArray);

            attrs[1].VerifyValue(0, TypedConstantKind.Array, emptyArray);

            attrs[2].VerifyValue(0, TypedConstantKind.Array, nullArray);
            attrs[2].VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, nullArray);

            attrs[3].VerifyValue(0, TypedConstantKind.Array, emptyArray);
            attrs[3].VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, nullArray);

            attrs[4].VerifyValue(0, TypedConstantKind.Array, nullArray);
            attrs[4].VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, emptyArray);

            attrs[5].VerifyValue(0, TypedConstantKind.Array, emptyArray);
            attrs[5].VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, emptyArray);
        }

        [Fact]
        [WorkItem(530266, "DevDiv")]
        public void UnboundGenericTypeInTypedConstant()
        {
            var source = @"
using System;

public class TestAttribute : Attribute
{
    public TestAttribute(Type x){}
}

[TestAttribute(typeof(Target<>))]
class Target<T>
{}";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Target");

            var typeInAttribute = (NamedTypeSymbol)type.GetAttributes()[0].ConstructorArguments.First().Value;
            Assert.True(typeInAttribute.IsUnboundGenericType);
            Assert.True(((INamedTypeSymbol)typeInAttribute).IsUnboundGenericType);
            Assert.Equal("Target<>", typeInAttribute.ToTestDisplayString());

            var comp2 = CreateCompilationWithMscorlib("", new[] { comp.EmitToImageReference() });
            type = comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("Target");

            Assert.IsAssignableFrom<PENamedTypeSymbol>(type);

            typeInAttribute = (NamedTypeSymbol)type.GetAttributes()[0].ConstructorArguments.First().Value;
            Assert.True(typeInAttribute.IsUnboundGenericType);
            Assert.True(((INamedTypeSymbol)typeInAttribute).IsUnboundGenericType);
            Assert.Equal("Target<>", typeInAttribute.ToTestDisplayString());
        }

        [Fact, WorkItem(1020038, "DevDiv")]
        public void Bug1020038()
        {
            var source1 = @"
public class CTest
{}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, assemblyName: "Bug1020038");

            var source2 = @"
class CAttr : System.Attribute
{
    public CAttr(System.Type x){}
}

[CAttr(typeof(CTest))]
class Test
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { new CSharpCompilationReference(compilation1) });

            CompileAndVerify(compilation2, symbolValidator: (m) =>
            {
                Assert.Equal(2, m.ReferencedAssemblies.Length);
                Assert.Equal("Bug1020038", m.ReferencedAssemblies[1].Name);
            });

            var source3 = @"
class CAttr : System.Attribute
{
    public CAttr(System.Type x){}
}

[CAttr(typeof(System.Func<System.Action<CTest>>))]
class Test
{}
";

            var compilation3 = CreateCompilationWithMscorlib(source3, new[] { new CSharpCompilationReference(compilation1) });

            CompileAndVerify(compilation3, symbolValidator: (m) =>
            {
                Assert.Equal(2, m.ReferencedAssemblies.Length);
                Assert.Equal("Bug1020038", m.ReferencedAssemblies[1].Name);
            });
        }

        [Fact, WorkItem(937575, "DevDiv"), WorkItem(121, "CodePlex")]
        public void Bug937575()
        {
            var source = @"
using System;
class XAttribute : Attribute { }
class C<T>
{
    public void M<[X]U>() { }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            CompileAndVerify(compilation, symbolValidator: (m) =>
            {
                var cc = m.GlobalNamespace.GetTypeMember("C");
                var mm = cc.GetMember<MethodSymbol>("M");

                Assert.True(cc.TypeParameters.Single().GetAttributes().IsEmpty);
                Assert.Equal("XAttribute", mm.TypeParameters.Single().GetAttributes().Single().ToString());
            });
        }

        [WorkItem(1144603, "DevDiv")]
        [Fact]
        public void EmitMetadataOnlyInPresenceOfErrors()
        {
            var source1 =
@"
public sealed class DiagnosticAnalyzerAttribute : System.Attribute
{
    public DiagnosticAnalyzerAttribute(string firstLanguage, params string[] additionalLanguages)
    {}
}

public static class LanguageNames
{
    public const xyz CSharp = ""C#"";
}
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);
            compilation1.VerifyDiagnostics(
    // (10,18): error CS0246: The type or namespace name 'xyz' could not be found (are you missing a using directive or an assembly reference?)
    //     public const xyz CSharp = "C#";
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "xyz").WithArguments("xyz").WithLocation(10, 18)
                );

            var source2 =
@"
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpCompilerDiagnosticAnalyzer
{}
";

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { new CSharpCompilationReference(compilation1) }, options: TestOptions.DebugDll, assemblyName: "Test.dll");
            Assert.Same(compilation1.Assembly, compilation2.SourceModule.ReferencedAssemblySymbols[1]);
            compilation2.VerifyDiagnostics();

            var emitResult2 = compilation2.Emit(peStream: new MemoryStream(), options: new EmitOptions(metadataOnly: true));
            Assert.False(emitResult2.Success);
            emitResult2.Diagnostics.Verify(
    // error CS7038: Failed to emit module 'Test.dll'.
    Diagnostic(ErrorCode.ERR_ModuleEmitFailure).WithArguments("Test.dll").WithLocation(1, 1)
                );

            // Use different mscorlib to test retargeting scenario
            var compilation3 = CreateCompilationWithMscorlib45(source2, new[] { new CSharpCompilationReference(compilation1) }, options: TestOptions.DebugDll);
            Assert.NotSame(compilation1.Assembly, compilation3.SourceModule.ReferencedAssemblySymbols[1]);
            compilation3.VerifyDiagnostics(
    // (2,35): error CS0246: The type or namespace name 'xyz' could not be found (are you missing a using directive or an assembly reference?)
    // [DiagnosticAnalyzer(LanguageNames.CSharp)]
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CSharp").WithArguments("xyz").WithLocation(2, 35)
                );

            var emitResult3 = compilation3.Emit(peStream: new MemoryStream(), options: new EmitOptions(metadataOnly: true));
            Assert.False(emitResult3.Success);
            emitResult3.Diagnostics.Verify(
    // (2,35): error CS0246: The type or namespace name 'xyz' could not be found (are you missing a using directive or an assembly reference?)
    // [DiagnosticAnalyzer(LanguageNames.CSharp)]
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CSharp").WithArguments("xyz").WithLocation(2, 35)
                );
        }

        #endregion
    }
}
