// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class FullNameTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Null()
        {
            IDkmClrFullNameProvider fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            Assert.Equal("null", fullNameProvider.GetClrExpressionForNull(inspectionContext));
        }

        [Fact]
        public void This()
        {
            IDkmClrFullNameProvider fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            Assert.Equal("this", fullNameProvider.GetClrExpressionForThis(inspectionContext));
        }

        [Fact]
        public void ArrayIndex()
        {
            IDkmClrFullNameProvider fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            Assert.Equal("[]", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, new string[0]));
            Assert.Equal("[]", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, [""]));
            Assert.Equal("[ ]", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, [" "]));
            Assert.Equal("[1]", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, ["1"]));
            Assert.Equal("[[], 2, 3]", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, ["[]", "2", "3"]));
            Assert.Equal("[, , ]", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, ["", "", ""]));
        }

        [Fact]
        public void Cast()
        {
            var source =
@"class C
{
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                IDkmClrFullNameProvider fullNameProvider = new CSharpFormatter();
                var inspectionContext = CreateDkmInspectionContext();
                var type = runtime.GetType("C");

                Assert.Equal("(C)o", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.None));
                Assert.Equal("o as C", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.ConditionalCast));
                Assert.Equal("(C)(o)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.ParenthesizeArgument));
                Assert.Equal("(o) as C", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.ParenthesizeArgument | DkmClrCastExpressionOptions.ConditionalCast));
                Assert.Equal("((C)o)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.ParenthesizeEntireExpression));
                Assert.Equal("(o as C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.ParenthesizeEntireExpression | DkmClrCastExpressionOptions.ConditionalCast));
                Assert.Equal("((C)(o))", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.ParenthesizeEntireExpression | DkmClrCastExpressionOptions.ParenthesizeArgument));
                Assert.Equal("((o) as C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, null, DkmClrCastExpressionOptions.ParenthesizeEntireExpression | DkmClrCastExpressionOptions.ParenthesizeArgument | DkmClrCastExpressionOptions.ConditionalCast));

                // Some of the same tests with "..." as the expression ("..." is used
                // by the debugger when the expression cannot be determined).
                Assert.Equal("(C)...", fullNameProvider.GetClrCastExpression(inspectionContext, "...", type, null, DkmClrCastExpressionOptions.None));
                Assert.Equal("... as C", fullNameProvider.GetClrCastExpression(inspectionContext, "...", type, null, DkmClrCastExpressionOptions.ConditionalCast));
                Assert.Equal("(... as C)", fullNameProvider.GetClrCastExpression(inspectionContext, "...", type, null, DkmClrCastExpressionOptions.ParenthesizeEntireExpression | DkmClrCastExpressionOptions.ConditionalCast));
            }
        }

        [Fact]
        public void RootComment()
        {
            var source = @"
class C
{
    public int F;
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate());

            var root = FormatResult("a // Comment", value);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            root = FormatResult(" a // Comment", value);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            root = FormatResult("a// Comment", value);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            root = FormatResult("a /*b*/ +c /*d*/// Comment", value);
            Assert.Equal("(a  +c).F", GetChildren(root).Single().FullName);

            root = FormatResult("a /*//*/+ c// Comment", value);
            Assert.Equal("(a + c).F", GetChildren(root).Single().FullName);

            root = FormatResult("a /*/**/+ c// Comment", value);
            Assert.Equal("(a + c).F", GetChildren(root).Single().FullName);

            root = FormatResult("/**/a// Comment", value);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            // See https://dev.azure.com/devdiv/DevDiv/_workitems/edit/847849
            root = FormatResult(@"""a//b/*"" // c", value);
            Assert.Equal(@"(""a//b/*"").F", GetChildren(root).Single().FullName);

            // incorrect - see https://github.com/dotnet/roslyn/issues/37536 
            root = FormatResult(@"""a"" //""b", value);
            Assert.Equal(@"(""a"" //""b).F", GetChildren(root).Single().FullName);
        }

        [Fact]
        public void RootFormatSpecifiers()
        {
            var source = @"
class C
{
    public int F;
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate());

            var root = FormatResult("a, raw", value); // simple
            Assert.Equal("a, raw", root.FullName);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            root = FormatResult("a, raw, ac, h", value); // multiple specifiers
            Assert.Equal("a, raw, ac, h", root.FullName);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            root = FormatResult("M(a, b), raw", value); // non-specifier comma
            Assert.Equal("M(a, b), raw", root.FullName);
            Assert.Equal("M(a, b).F", GetChildren(root).Single().FullName);

            root = FormatResult("a, raw1", value); // alpha-numeric
            Assert.Equal("a, raw1", root.FullName);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            root = FormatResult("a, $raw", value); // other punctuation
            Assert.Equal("a, $raw", root.FullName);
            Assert.Equal("(a, $raw).F", GetChildren(root).Single().FullName); // not ideal
        }

        [Fact]
        public void RootParentheses()
        {
            var source = @"
class C
{
    public int F;
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate());

            var root = FormatResult("a + b", value);
            Assert.Equal("(a + b).F", GetChildren(root).Single().FullName); // required

            root = FormatResult("new C()", value);
            Assert.Equal("(new C()).F", GetChildren(root).Single().FullName); // documentation

            root = FormatResult("A.B", value);
            Assert.Equal("A.B.F", GetChildren(root).Single().FullName); // desirable

            root = FormatResult("A::B", value);
            Assert.Equal("(A::B).F", GetChildren(root).Single().FullName); // documentation
        }

        [Fact]
        public void RootTrailingSemicolons()
        {
            var source = @"
class C
{
    public int F;
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate());

            var root = FormatResult("a;", value);
            Assert.Equal("a.F", GetChildren(root).Single().FullName);

            root = FormatResult("a + b;;", value);
            Assert.Equal("(a + b).F", GetChildren(root).Single().FullName);

            root = FormatResult(" M( ) ; ;", value);
            Assert.Equal("M( ).F", GetChildren(root).Single().FullName);
        }

        [Fact]
        public void RootMixedExtras()
        {
            var source = @"
class C
{
    public int F;
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate());

            // Semicolon, then comment.
            var root = FormatResult("a; //", value);
            Assert.Equal("a", root.FullName);

            // Comment, then semicolon.
            root = FormatResult("a // ;", value);
            Assert.Equal("a", root.FullName);

            // Semicolon, then format specifier.
            root = FormatResult("a;, ac", value);
            Assert.Equal("a, ac", root.FullName);

            // Format specifier, then semicolon.
            root = FormatResult("a, ac;", value);
            Assert.Equal("a, ac", root.FullName);

            // Comment, then format specifier.
            root = FormatResult("a//, ac", value);
            Assert.Equal("a", root.FullName);

            // Format specifier, then comment.
            root = FormatResult("a, ac //", value);
            Assert.Equal("a, ac", root.FullName);

            // Everything.
            root = FormatResult("/*A*/ a /*B*/ + /*C*/ b /*D*/ ; ; , ac /*E*/, raw // ;, hidden", value);
            Assert.Equal("a  +  b, ac, raw", root.FullName);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022165")]
        public void Keywords_Root()
        {
            var source = @"
class C
{
    void M()
    {
        int @namespace = 3;
    }
}
";
            var assembly = GetAssembly(source);
            var value = CreateDkmClrValue(3);

            var root = FormatResult("@namespace", value);
            Verify(root,
                EvalResult("@namespace", "3", "int", "@namespace"));

            value = CreateDkmClrValue(assembly.GetType("C").Instantiate());
            root = FormatResult("this", value);
            Verify(root,
                EvalResult("this", "{C}", "C", "this"));

            // Verify that keywords aren't escaped by the ResultProvider at the
            // root level (we would never expect to see "namespace" passed as a
            // resultName, but this check verifies that we leave them "as is").
            root = FormatResult("namespace", CreateDkmClrValue(new object()));
            Verify(root,
                EvalResult("namespace", "{object}", "object", "namespace"));
        }

        [Fact]
        public void Keywords_RuntimeType()
        {
            var source = @"
public class @struct
{
}

public class @namespace : @struct
{
    @struct m = new @if();
}

public class @if : @struct
{
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("namespace");
            var declaredType = assembly.GetType("struct");
            var value = CreateDkmClrValue(type.Instantiate(), type);

            var root = FormatResult("o", value, new DkmClrType((TypeImpl)declaredType));
            Verify(GetChildren(root),
                EvalResult("m", "{if}", "struct {if}", "((@namespace)o).m", DkmEvaluationResultFlags.CanFavorite));
        }

        [Fact]
        public void Keywords_ProxyType()
        {
            var source = @"
using System.Diagnostics;

[DebuggerTypeProxy(typeof(@class))]
public class @struct
{
    public bool @true = false;
}

public class @class
{
    public bool @false = true;

    public @class(@struct s) { }
}
";
            var assembly = GetAssembly(source);
            var value = CreateDkmClrValue(assembly.GetType("struct").Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("@false", "true", "bool", "new @class(o).@false", DkmEvaluationResultFlags.Boolean | DkmEvaluationResultFlags.BooleanTrue),
                EvalResult("Raw View", null, "", "o, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            var grandChildren = GetChildren(children.Last());
            Verify(grandChildren,
                EvalResult("@true", "false", "bool", "o.@true", DkmEvaluationResultFlags.Boolean));
        }

        [Fact]
        public void Keywords_MemberAccess()
        {
            var source = @"
public class @struct
{
    public int @true;
}
";
            var assembly = GetAssembly(source);
            var value = CreateDkmClrValue(assembly.GetType("struct").Instantiate());

            var root = FormatResult("o", value);
            Verify(GetChildren(root),
                EvalResult("@true", "0", "int", "o.@true", DkmEvaluationResultFlags.CanFavorite));
        }

        [Fact]
        public void Keywords_StaticMembers()
        {
            var source = @"
public class @struct
{
    public static int @true;
}
";
            var assembly = GetAssembly(source);
            var value = CreateDkmClrValue(assembly.GetType("struct").Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", "@struct", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("@true", "0", "int", "@struct.@true", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Public));
        }

        [Fact]
        public void Keywords_ExplicitInterfaceImplementation()
        {
            var source = @"
namespace @namespace
{
    public interface @interface<T>
    {
        int @return { get; set; }
    }

    public class @class : @interface<@class>
    {
        int @interface<@class>.@return { get; set; }
    }
}
";
            var assembly = GetAssembly(source);
            var value = CreateDkmClrValue(assembly.GetType("namespace.class").Instantiate());

            var root = FormatResult("instance", value);
            Verify(GetChildren(root),
                EvalResult("@namespace.@interface<@namespace.@class>.@return", "0", "int", "((@namespace.@interface<@namespace.@class>)instance).@return"));
        }

        [Fact]
        public void MangledNames_CastRequired()
        {
            var il = @"
.class public auto ansi beforefieldinit '<>Mangled' extends [mscorlib]System.Object
{
  .field public int32 x

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit 'NotMangled' extends '<>Mangled'
{
  .field public int32 x

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void '<>Mangled'::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("NotMangled").Instantiate());

            var root = FormatResult("o", value);
            Verify(GetChildren(root),
                EvalResult("x (<>Mangled)", "0", "int", null),
                EvalResult("x", "0", "int", "o.x", DkmEvaluationResultFlags.CanFavorite));
        }

        [Fact]
        public void MangledNames_StaticMembers()
        {
            var il = @"
.class public auto ansi beforefieldinit '<>Mangled' extends [mscorlib]System.Object
{
  .field public static int32 x

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit 'NotMangled' extends '<>Mangled'
{
  .field public static int32 y

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void '<>Mangled'::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var baseValue = CreateDkmClrValue(assembly.GetType("<>Mangled").Instantiate());

            var root = FormatResult("o", baseValue);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", null, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "int", null));

            var derivedValue = CreateDkmClrValue(assembly.GetType("NotMangled").Instantiate());

            root = FormatResult("o", derivedValue);
            children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", "NotMangled", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "int", null, DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Public),
                EvalResult("y", "0", "int", "NotMangled.y", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Public));
        }

        [Fact]
        public void MangledNames_ExplicitInterfaceImplementation()
        {
            var il = @"
.class interface public abstract auto ansi 'abstract.I<>Mangled'
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_P() cil managed
  {
  }

  .property instance int32 P()
  {
    .get instance int32 'abstract.I<>Mangled'::get_P()
  }
} // end of class 'abstract.I<>Mangled'

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
       implements 'abstract.I<>Mangled'
{
  .method private hidebysig newslot specialname virtual final 
          instance int32  'abstract.I<>Mangled.get_P'() cil managed
  {
    .override 'abstract.I<>Mangled'::get_P
    ldc.i4.1
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 'abstract.I<>Mangled.P'()
  {
    .get instance int32 C::'abstract.I<>Mangled.get_P'()
  }

  .property instance int32 P()
  {
    .get instance int32 C::'abstract.I<>Mangled.get_P'()
  }
} // end of class C
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("C").Instantiate());

            var root = FormatResult("instance", value);
            Verify(GetChildren(root),
                EvalResult("P", "1", "int", "instance.P", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("abstract.I<>Mangled.P", "1", "int", null, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private));
        }

        [Fact]
        public void MangledNames_ArrayElement()
        {
            var il = @"
.class public auto ansi beforefieldinit '<>Mangled'
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit NotMangled
       extends [mscorlib]System.Object
{
  .field public class [mscorlib]System.Collections.Generic.IEnumerable`1<class '<>Mangled'> 'array'
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    ldc.i4.1
    newarr     '<>Mangled'
    stfld      class [mscorlib]System.Collections.Generic.IEnumerable`1<class '<>Mangled'> NotMangled::'array'
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("NotMangled").Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("array", "{<>Mangled[1]}", "System.Collections.Generic.IEnumerable<<>Mangled> {<>Mangled[]}", "o.array", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite));
            Verify(GetChildren(children.Single()),
                EvalResult("[0]", "null", "<>Mangled", null));
        }

        [Fact]
        public void MangledNames_Namespace()
        {
            var il = @"
.class public auto ansi beforefieldinit '<>Mangled.C' extends [mscorlib]System.Object
{
  .field public static int32 x

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var baseValue = CreateDkmClrValue(assembly.GetType("<>Mangled.C").Instantiate());

            var root = FormatResult("o", baseValue);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", null, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "int", null));
        }

        [Fact]
        public void MangledNames_PointerDereference()
        {
            var il = @"
.class public auto ansi beforefieldinit '<>Mangled'
       extends [mscorlib]System.Object
{
  .field private static int32* p

  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(int64 arg) cil managed
  {
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0008:  ldarg.1
    IL_0009:  conv.u
    IL_000a:  stsfld     int32* '<>Mangled'::p
    IL_0010:  ret
  }
} // end of class '<>Mangled'
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            unsafe
            {
                int i = 4;
                long p = (long)&i;
                var type = assembly.GetType("<>Mangled");
                var rootExpr = "m";
                var value = CreateDkmClrValue(type.Instantiate(p));
                var evalResult = FormatResult(rootExpr, value);
                Verify(evalResult,
                    EvalResult(rootExpr, "{<>Mangled}", "<>Mangled", rootExpr, DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("Static members", null, "", null, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
                children = GetChildren(children.Single());
                Verify(children,
                    EvalResult("p", PointerToString(new IntPtr(p)), "int*", null, DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children.Single());
                Verify(children,
                    EvalResult("*p", "4", "int", null));
            }
        }

        [Fact]
        public void MangledNames_DebuggerTypeProxy()
        {
            var il = @"
.class public auto ansi beforefieldinit Type
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Diagnostics.DebuggerTypeProxyAttribute::.ctor(class [mscorlib]System.Type)
           = {type('<>Mangled')}
  .field public bool x
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    ldc.i4.0
    stfld      bool Type::x
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method Type::.ctor

} // end of class Type

.class public auto ansi beforefieldinit '<>Mangled'
       extends [mscorlib]System.Object
{
  .field public bool y
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class Type s) cil managed
  {
    ldarg.0
    ldc.i4.1
    stfld      bool '<>Mangled'::y
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method '<>Mangled'::.ctor

} // end of class '<>Mangled'
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("Type").Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("y", "true", "bool", null, DkmEvaluationResultFlags.Boolean | DkmEvaluationResultFlags.BooleanTrue),
                EvalResult("Raw View", null, "", "o, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            var grandChildren = GetChildren(children.Last());
            Verify(grandChildren,
                EvalResult("x", "false", "bool", "o.x", DkmEvaluationResultFlags.Boolean));
        }

        [Fact]
        public void GenericTypeWithoutBacktick()
        {
            var il = @"
.class public auto ansi beforefieldinit C<T> extends [mscorlib]System.Object
{
  .field public static int32 'x'

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("C").MakeGenericType(typeof(int)).Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", "C<int>", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "int", "C<int>.x"));
        }

        [Fact]
        public void BackTick_NonGenericType()
        {
            var il = @"
.class public auto ansi beforefieldinit 'C`1' extends [mscorlib]System.Object
{
  .field public static int32 'x'

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("C`1").Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", null, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "int", null));
        }

        [Fact]
        public void BackTick_GenericType()
        {
            var il = @"
.class public auto ansi beforefieldinit 'C`1'<T> extends [mscorlib]System.Object
{
  .field public static int32 'x'

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("C`1").MakeGenericType(typeof(int)).Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", "C<int>", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "int", "C<int>.x"));
        }

        [Fact]
        public void BackTick_Member()
        {
            // IL doesn't support using generic methods as property accessors so
            // there's no way to test a "legitimate" backtick in a member name.
            var il = @"
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
  .field public static int32 'x`1'

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("C").Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", "C", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x`1", "0", "int", fullName: null));
        }

        [Fact]
        public void BackTick_FirstCharacter()
        {
            var il = @"
.class public auto ansi beforefieldinit '`1'<T> extends [mscorlib]System.Object
{
  .field public static int32 'x'

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var value = CreateDkmClrValue(assembly.GetType("`1").MakeGenericType(typeof(int)).Instantiate());

            var root = FormatResult("o", value);
            var children = GetChildren(root);
            Verify(children,
                EvalResult("Static members", null, "", null, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "int", fullName: null));
        }

        [Fact]
        public void MangledName_SimplifySynthesizedLocalName()
        {
            IDkmClrFullNameProvider2 fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            // The synthesized locals name should just become an empty string since it's compiler generated.
            Assert.Equal(string.Empty, fullNameProvider.GetClrNameForLocalVariable(inspectionContext, null, default, default, new DkmClrLocalVariable("CS$<>8__locals0")));
        }

        [Fact]
        public void MangledName_SimplifyThisProxyField()
        {
            var il = @"
.class public C
{
  .field public object '<>4__this'
}
";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var fieldToken = assembly.GetType("C").GetFields().First().MetadataToken;

            IDkmClrFullNameProvider2 fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            // The stashed <>4__this should just become an empty string since it's compiler generated.
            Assert.Equal(string.Empty, fullNameProvider.GetClrNameForField(inspectionContext, new DkmClrRuntimeInstance(assembly).Modules[0], fieldToken));
        }

        [Fact]
        public void MangledName_SimplifyHoistedLocal()
        {
            var il = @"
.class public C
{
  .field public object '<myClass>5__1'
}
";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var fieldToken = assembly.GetType("C").GetFields().First().MetadataToken;

            IDkmClrFullNameProvider2 fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            Assert.Equal("myClass", fullNameProvider.GetClrNameForField(inspectionContext, new DkmClrRuntimeInstance(assembly).Modules[0], fieldToken));
        }

        [Fact]
        public void MangledName_SimplifyBackingField_01()
        {
            var il = @"
.class public C
{
  .field public object '<StringProperty>k__BackingField'
}
";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var fieldToken = assembly.GetType("C").GetFields().First().MetadataToken;

            IDkmClrFullNameProvider2 fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            Assert.Equal("StringProperty", fullNameProvider.GetClrNameForField(inspectionContext, new DkmClrRuntimeInstance(assembly).Modules[0], fieldToken));
        }

        [Fact]
        public void MangledName_SimplifyBackingField_02()
        {
            var il = @"
.class public C
{
  .field public object '<StringParameter>P'
}
";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var fieldToken = assembly.GetType("C").GetFields().First().MetadataToken;

            IDkmClrFullNameProvider2 fullNameProvider = new CSharpFormatter();
            var inspectionContext = CreateDkmInspectionContext();
            Assert.Equal("StringParameter", fullNameProvider.GetClrNameForField(inspectionContext, new DkmClrRuntimeInstance(assembly).Modules[0], fieldToken));
        }
    }
}
