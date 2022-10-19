// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldTests : CSharpTestBase
    {
        [Fact]
        public void InitializerInStruct()
        {
            var text = @"struct S
{
    public int I = 9;

    public S(int i) {}
}";

            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (3,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int I = 9;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I").WithArguments("struct field initializers", "10.0").WithLocation(3, 16));
        }

        [Fact]
        public void InitializerInStruct2()
        {
            var text = @"struct S
{
    public int I = 9;

    public S(int i) : this() {}
}";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (3,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int I = 9;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I").WithArguments("struct field initializers", "10.0").WithLocation(3, 16));
        }

        [Fact]
        public void Simple1()
        {
            var text =
@"
class A {
    A F;
}
";
            var comp = CreateEmptyCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var sym = a.GetMembers("F").Single() as FieldSymbol;

            Assert.Equal(TypeKind.Class, sym.Type.TypeKind);
            Assert.Equal<TypeSymbol>(a, sym.Type);
            Assert.Equal(Accessibility.Private, sym.DeclaredAccessibility);
            Assert.Equal(SymbolKind.Field, sym.Kind);
            Assert.False(sym.IsStatic);
            Assert.False(sym.IsAbstract);
            Assert.False(sym.IsSealed);
            Assert.False(sym.IsVirtual);
            Assert.False(sym.IsOverride);

            // Assert.Equal(0, sym.GetAttributes().Count());
        }

        [Fact]
        public void Simple2()
        {
            var text =
@"
class A {
    A F, G;
    A G;
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var f = a.GetMembers("F").Single() as FieldSymbol;
            Assert.Equal(TypeKind.Class, f.Type.TypeKind);
            Assert.Equal<TypeSymbol>(a, f.Type);
            Assert.Equal(Accessibility.Private, f.DeclaredAccessibility);
            var gs = a.GetMembers("G");
            Assert.Equal(2, gs.Length);
            foreach (var g in gs)
            {
                Assert.Equal(a, (g as FieldSymbol).Type); // duplicate, but all the same.
            }

            var errors = comp.GetDeclarationDiagnostics();
            var one = errors.Single();
            Assert.Equal(ErrorCode.ERR_DuplicateNameInClass, (ErrorCode)one.Code);
        }

        [Fact]
        public void Ambig1()
        {
            var text =
@"
class A {
    A F;
    A F;
}
";
            var comp = CreateEmptyCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var fs = a.GetMembers("F");
            Assert.Equal(2, fs.Length);
            foreach (var f in fs)
            {
                Assert.Equal(a, (f as FieldSymbol).Type);
            }
        }

        [WorkItem(537237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537237")]
        [Fact]
        public void FieldModifiers()
        {
            var text =
@"
class A
{
    internal protected const long N1 = 0;
    public volatile byte N2 = 0;
    private static char N3 = ' ';
}
";
            var comp = CreateEmptyCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var n1 = a.GetMembers("N1").Single() as FieldSymbol;
            Assert.True(n1.IsConst);
            Assert.False(n1.IsVolatile);
            Assert.True(n1.IsStatic);
            Assert.Equal(0, n1.TypeWithAnnotations.CustomModifiers.Length);

            var n2 = a.GetMembers("N2").Single() as FieldSymbol;
            Assert.False(n2.IsConst);
            Assert.True(n2.IsVolatile);
            Assert.False(n2.IsStatic);
            Assert.Equal(1, n2.TypeWithAnnotations.CustomModifiers.Length);
            CustomModifier mod = n2.TypeWithAnnotations.CustomModifiers[0];
            Assert.False(mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsVolatile[missing]", mod.Modifier.ToTestDisplayString());

            var n3 = a.GetMembers("N3").Single() as FieldSymbol;
            Assert.False(n3.IsConst);
            Assert.False(n3.IsVolatile);
            Assert.True(n3.IsStatic);
            Assert.Equal(0, n3.TypeWithAnnotations.CustomModifiers.Length);
        }

        [Fact]
        public void Nullable()
        {
            var text =
@"
class A {
    int? F = null;
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var sym = a.GetMembers("F").Single() as FieldSymbol;
            Assert.Equal("System.Int32? A.F", sym.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, sym.Type.TypeKind);
            Assert.Equal("System.Int32?", sym.Type.ToTestDisplayString());
        }

        [Fact]
        public void Generic01()
        {
            var text =
@"public class C<T>
{
    internal struct S<V>
    {
        public System.Collections.Generic.List<T> M<V>(V p) { return null; }
        private System.Collections.Generic.List<T> field1;
        internal System.Collections.Generic.IList<V> field2;
        public S<string> field3;
    }
}
";
            var comp = CreateCompilation(text);
            var type1 = comp.GlobalNamespace.GetTypeMembers("C", 1).Single();
            var type2 = type1.GetTypeMembers("S").Single();

            var s = type2.GetMembers("M").Single() as MethodSymbol;
            Assert.Equal("M", s.Name);
            Assert.Equal("System.Collections.Generic.List<T> C<T>.S<V>.M<V>(V p)", s.ToTestDisplayString());

            var sym = type2.GetMembers("field1").Single() as FieldSymbol;
            Assert.Equal("System.Collections.Generic.List<T> C<T>.S<V>.field1", sym.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, sym.Type.TypeKind);
            Assert.Equal("System.Collections.Generic.List<T>", sym.Type.ToTestDisplayString());

            sym = type2.GetMembers("field2").Single() as FieldSymbol;
            Assert.Equal("System.Collections.Generic.IList<V> C<T>.S<V>.field2", sym.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, sym.Type.TypeKind);
            Assert.Equal("System.Collections.Generic.IList<V>", sym.Type.ToTestDisplayString());

            sym = type2.GetMembers("field3").Single() as FieldSymbol;
            Assert.Equal("C<T>.S<System.String> C<T>.S<V>.field3", sym.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, sym.Type.TypeKind);
            Assert.Equal("C<T>.S<System.String>", sym.Type.ToTestDisplayString());
        }

        [WorkItem(537401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537401")]
        [Fact]
        public void EventEscapedIdentifier()
        {
            var text = @"
delegate void @out();
class C1
{
    @out @in;
}
";
            var comp = CreateCompilation(Parse(text));
            NamedTypeSymbol c1 = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("C1").Single();
            FieldSymbol ein = (FieldSymbol)c1.GetMembers("in").Single();
            Assert.Equal("in", ein.Name);
            Assert.Equal("C1.@in", ein.ToString());
            NamedTypeSymbol dout = (NamedTypeSymbol)ein.Type;
            Assert.Equal("out", dout.Name);
            Assert.Equal("@out", dout.ToString());
        }

        [WorkItem(539653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539653")]
        [Fact]
        public void ConstFieldWithoutValueErr()
        {
            var text = @"
class C
{
    const int x;
}
";
            var comp = CreateCompilation(Parse(text));
            NamedTypeSymbol type1 = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("C").Single();
            FieldSymbol mem = (FieldSymbol)type1.GetMembers("x").Single();
            Assert.Equal("x", mem.Name);
            Assert.True(mem.IsConst);
            Assert.False(mem.HasConstantValue);
            Assert.Null(mem.ConstantValue);
        }

        [WorkItem(543538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543538")]
        [Fact]
        public void Error_InvalidConst()
        {
            var source = @"
class A
{
    const delegate void D();
    protected virtual void Finalize const () { }
}
";

            // CONSIDER: Roslyn's cascading errors are much uglier than Dev10's.
            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular).VerifyDiagnostics(
                // (4,11): error CS1031: Type expected
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(4, 11),
                // (4,11): error CS1001: Identifier expected
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "delegate").WithLocation(4, 11),
                // (4,11): error CS0145: A const field requires a value to be provided
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "delegate").WithLocation(4, 11),
                // (4,11): error CS1002: ; expected
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "delegate").WithLocation(4, 11),
                // (5,37): error CS1002: ; expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "const").WithLocation(5, 37),
                // (5,44): error CS8124: Tuple must contain at least two elements.
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 44),
                // (5,46): error CS1001: Identifier expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(5, 46),
                // (5,46): error CS0145: A const field requires a value to be provided
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "{").WithLocation(5, 46),
                // (5,46): error CS1003: Syntax error, ',' expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(5, 46),
                // (5,48): error CS1002: ; expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(5, 48),
                // (6,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 1),
                // (5,28): error CS0106: The modifier 'virtual' is not valid for this item
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Finalize").WithArguments("virtual").WithLocation(5, 28),
                // (5,43): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "()").WithArguments("System.ValueTuple`2").WithLocation(5, 43),
                // (5,23): error CS0670: Field cannot have void type
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void").WithLocation(5, 23),
                // (5,46): error CS0102: The type 'A' already contains a definition for ''
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("A", "").WithLocation(5, 46),
                // (5,28): warning CS0649: Field 'A.Finalize' is never assigned to, and will always have its default value 
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Finalize").WithArguments("A.Finalize", "").WithLocation(5, 28)
                );
        }

        [WorkItem(543538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543538")]
        [Fact]
        public void Error_InvalidConstWithCSharp6()
        {
            var source = @"
class A
{
    const delegate void D();
    protected virtual void Finalize const () { }
}
";

            // CONSIDER: Roslyn's cascading errors are much uglier than Dev10's.
            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                // (4,11): error CS1031: Type expected
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(4, 11),
                // (4,11): error CS1001: Identifier expected
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "delegate").WithLocation(4, 11),
                // (4,11): error CS0145: A const field requires a value to be provided
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "delegate").WithLocation(4, 11),
                // (4,11): error CS1002: ; expected
                //     const delegate void D();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "delegate").WithLocation(4, 11),
                // (5,37): error CS1002: ; expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "const").WithLocation(5, 37),
                // (5,43): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "()").WithArguments("tuples", "7.0").WithLocation(5, 43),
                // (5,44): error CS8124: Tuple must contain at least two elements.
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 44),
                // (5,46): error CS1001: Identifier expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(5, 46),
                // (5,46): error CS0145: A const field requires a value to be provided
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "{").WithLocation(5, 46),
                // (5,46): error CS1003: Syntax error, ',' expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(5, 46),
                // (5,48): error CS1002: ; expected
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(5, 48),
                // (6,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 1),
                // (5,28): error CS0106: The modifier 'virtual' is not valid for this item
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Finalize").WithArguments("virtual").WithLocation(5, 28),
                // (5,43): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "()").WithArguments("System.ValueTuple`2").WithLocation(5, 43),
                // (5,23): error CS0670: Field cannot have void type
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void").WithLocation(5, 23),
                // (5,46): error CS0102: The type 'A' already contains a definition for ''
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("A", "").WithLocation(5, 46),
                // (5,28): warning CS0649: Field 'A.Finalize' is never assigned to, and will always have its default value 
                //     protected virtual void Finalize const () { }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Finalize").WithArguments("A.Finalize", "").WithLocation(5, 28)
                );
        }

        [WorkItem(543791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543791")]
        [Fact]
        public void MultipleDeclaratorsOneError()
        {
            var source = @"
class A
{
    Unknown a, b;
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (4,5): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown"),
                // (4,13): warning CS0169: The field 'A.a' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("A.a"),
                // (4,16): warning CS0169: The field 'A.b' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b").WithArguments("A.b"));
        }

        /// <summary>
        /// Fields named "value__" should be marked rtspecialname.
        /// </summary>
        [WorkItem(546185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546185")]
        [ClrOnlyFact(ClrOnlyReason.Unknown, Skip = "https://github.com/dotnet/roslyn/issues/6190")]
        public void RTSpecialName()
        {
            var source =
@"class A
{
    object value__ = null;
}
class B
{
    object VALUE__ = null;
}
class C
{
    void value__() { }
}
class D
{
    object value__ { get; set; }
}
class E
{
    event System.Action value__;
}
class F
{
    event System.Action value__ { add { } remove { } }
}
class G
{
    interface value__ { }
}
class H
{
    class value__ { }
}
class K
{
    static System.Action<object> F()
    {
        object value__;
        return v => { value__ = v; };
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (19,25): warning CS0067: The event 'E.value__' is never used
                //     event System.Action value__;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "value__").WithArguments("E.value__"),
                // (7,12): warning CS0414: The field 'B.VALUE__' is assigned but its value is never used
                //     object VALUE__ = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "VALUE__").WithArguments("B.VALUE__"),
                // (3,12): warning CS0414: The field 'A.value__' is assigned but its value is never used
                //     object value__ = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "value__").WithArguments("A.value__"));

            // PEVerify should not report "Field value__ ... is not marked RTSpecialName".
            var verifier = new CompilationVerifier(compilation);
            verifier.EmitAndVerify(
                "Error: Field name value__ is reserved for Enums only.",
                "Error: Field name value__ is reserved for Enums only.",
                "Error: Field name value__ is reserved for Enums only.");
        }

        [WorkItem(26364, "https://github.com/dotnet/roslyn/issues/26364"), WorkItem(54799, "https://github.com/dotnet/roslyn/issues/54799")]
        [Fact]
        public void FixedSizeBufferTrue()
        {
            var text =
@"
unsafe struct S
{
    private fixed byte goo[10];
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var s = global.GetTypeMember("S");
            var goo = (IFieldSymbol)s.GetMember("goo").GetPublicSymbol();

            Assert.True(goo.IsFixedSizeBuffer);
            Assert.Equal(10, goo.FixedSize);
        }

        [WorkItem(26364, "https://github.com/dotnet/roslyn/issues/26364"), WorkItem(54799, "https://github.com/dotnet/roslyn/issues/54799")]
        [Fact]
        public void FixedSizeBufferFalse()
        {
            var text =
@"
unsafe struct S
{
    private byte goo;
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var s = global.GetTypeMember("S");
            var goo = (IFieldSymbol)s.GetMember("goo").GetPublicSymbol();

            Assert.False(goo.IsFixedSizeBuffer);
            Assert.Equal(0, goo.FixedSize);
        }

        [Fact]
        public void StaticFieldDoesNotRequireInstanceReceiver()
        {
            var source = @"
class C
{
    public static int F = 42;
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var field = compilation.GetMember<FieldSymbol>("C.F");
            Assert.False(field.RequiresInstanceReceiver);
        }

        [Fact]
        public void InstanceFieldRequiresInstanceReceiver()
        {
            var source = @"
class C
{
    public int F = 42;
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var field = compilation.GetMember<FieldSymbol>("C.F");
            Assert.True(field.RequiresInstanceReceiver);
        }

        [Fact]
        public void UnreferencedInterpolatedStringConstants()
        {
            var comp = CreateCompilation(@"
class C
{
    private static string s1 = $"""";
    private static readonly string s2 = $"""";
    private string s3 = $"""";
    private readonly string s4 = $"""";
}
struct S
{
    private static string s1 = $"""";
    private static readonly string s2 = $"""";
}
");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnreferencedRawInterpolatedStringConstants()
        {
            var comp = CreateCompilation(@"
class C
{
    private static string s1 = $"""""" """""";
    private static readonly string s2 = $"""""" """""";
    private string s3 = $"""""" """""";
    private readonly string s4 = $"""""" """""";
}
struct S
{
    private static string s1 = $"""""" """""";
    private static readonly string s2 = $"""""" """""";
}
");

            comp.VerifyDiagnostics();
        }
    }
}
