' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.ComponentInterfaces
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class FullNameTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub Null()
            Dim fullNameProvider As IDkmClrFullNameProvider = New VisualBasicFormatter()
            Dim inspectionContext = CreateDkmInspectionContext()
            Assert.Equal("Nothing", fullNameProvider.GetClrExpressionForNull(inspectionContext))
        End Sub

        <Fact>
        Public Sub This()
            Dim fullNameProvider As IDkmClrFullNameProvider = New VisualBasicFormatter()
            Dim inspectionContext = CreateDkmInspectionContext()
            Assert.Equal("Me", fullNameProvider.GetClrExpressionForThis(inspectionContext))
        End Sub

        <Fact>
        Public Sub ArrayIndex()
            Dim fullNameProvider As IDkmClrFullNameProvider = New VisualBasicFormatter()
            Dim inspectionContext = CreateDkmInspectionContext()
            Assert.Equal("()", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, {}))
            Assert.Equal("()", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, {""}))
            Assert.Equal("( )", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, {" "}))
            Assert.Equal("(1)", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, {"1"}))
            Assert.Equal("((), 2, 3)", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, {"()", "2", "3"}))
            Assert.Equal("(, , )", fullNameProvider.GetClrArrayIndexExpression(inspectionContext, {"", "", ""}))
        End Sub

        <Fact>
        Public Sub Cast()
            Const source =
"Class C
End Class"
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)))
            Using runtime.Load()
                Dim fullNameProvider As IDkmClrFullNameProvider = New VisualBasicFormatter()
                Dim inspectionContext = CreateDkmInspectionContext()
                Dim type = runtime.GetType("C")

                Assert.Equal("DirectCast(o, C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.None))
                Assert.Equal("TryCast(o, C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.ConditionalCast))
                Assert.Equal("DirectCast((o), C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.ParenthesizeArgument))
                Assert.Equal("TryCast((o), C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.ParenthesizeArgument Or DkmClrCastExpressionOptions.ConditionalCast))
                Assert.Equal("DirectCast(o, C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.ParenthesizeEntireExpression))
                Assert.Equal("TryCast(o, C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.ParenthesizeEntireExpression Or DkmClrCastExpressionOptions.ConditionalCast))
                Assert.Equal("DirectCast((o), C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.ParenthesizeEntireExpression Or DkmClrCastExpressionOptions.ParenthesizeArgument))
                Assert.Equal("TryCast((o), C)", fullNameProvider.GetClrCastExpression(inspectionContext, "o", type, Nothing, DkmClrCastExpressionOptions.ParenthesizeEntireExpression Or DkmClrCastExpressionOptions.ParenthesizeArgument Or DkmClrCastExpressionOptions.ConditionalCast))

                ' Some of the same tests with "..." as the expression ("..." is used
                ' by the debugger when the expression cannot be determined).
                Assert.Equal("DirectCast(..., C)", fullNameProvider.GetClrCastExpression(inspectionContext, "...", type, Nothing, DkmClrCastExpressionOptions.None))
                Assert.Equal("TryCast(..., C)", fullNameProvider.GetClrCastExpression(inspectionContext, "...", type, Nothing, DkmClrCastExpressionOptions.ConditionalCast))
                Assert.Equal("TryCast(..., C)", fullNameProvider.GetClrCastExpression(inspectionContext, "...", type, Nothing, DkmClrCastExpressionOptions.ParenthesizeEntireExpression Or DkmClrCastExpressionOptions.ConditionalCast))
            End Using
        End Sub

        <Fact>
        Public Sub RootComment()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            Dim root = FormatResult("a ' Comment", value)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult(" a ' Comment", value)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a' Comment", value)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a  +c '' Comment", value)
            Assert.Equal("(a  +c).F", GetChildren(root).Single().FullName)

            root = FormatResult("a + c' Comment", value)
            Assert.Equal("(a + c).F", GetChildren(root).Single().FullName)

            ' The result provider should never see a value like this in the "real-world"
            root = FormatResult("''a' Comment", value)
            Assert.Equal(".F", GetChildren(root).Single().FullName)

            ' See https://dev.azure.com/devdiv/DevDiv/_workitems/edit/847849
            root = FormatResult("""a'b"" ' c", value)
            Assert.Equal("(""a'b"").F", GetChildren(root).Single().FullName)

            ' incorrect - see https://github.com/dotnet/roslyn/issues/37536 
            root = FormatResult("""a"" '""b", value)
            Assert.Equal("(""a"" '""b).F", GetChildren(root).Single().FullName)
        End Sub

        <Fact>
        Public Sub RootFormatSpecifiers()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            Dim root = FormatResult("a, raw", value) ' simple
            Assert.Equal("a, raw", root.FullName)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a, raw, ac, h", value) ' multiple specifiers
            Assert.Equal("a, raw, ac, h", root.FullName)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("M(a, b), raw", value) ' non - specifier comma
            Assert.Equal("M(a, b), raw", root.FullName)
            Assert.Equal("M(a, b).F", GetChildren(root).Single().FullName)

            root = FormatResult("a, raw1", value) ' alpha - numeric
            Assert.Equal("a, raw1", root.FullName)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a, $raw", value) ' other punctuation
            Assert.Equal("a, $raw", root.FullName)
            Assert.Equal("(a, $raw).F", GetChildren(root).Single().FullName) ' Not ideal
        End Sub

        <Fact>
        Public Sub RootParentheses()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            Dim root = FormatResult("a + b", value)
            Assert.Equal("(a + b).F", GetChildren(root).Single().FullName) ' required

            root = FormatResult("new C()", value)
            Assert.Equal("(new C()).F", GetChildren(root).Single().FullName) ' documentation

            root = FormatResult("A.B", value)
            Assert.Equal("A.B.F", GetChildren(root).Single().FullName) ' desirable

            root = FormatResult("Global.A.B", value)
            Assert.Equal("Global.A.B.F", GetChildren(root).Single().FullName) ' desirable
        End Sub

        <Fact>
        Public Sub RootTrailingSemicolons()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            ' The result provider should never see a values like these in the "real-world"

            Dim root = FormatResult("a;", value)
            Assert.Equal("(a;).F", GetChildren(root).Single().FullName)

            root = FormatResult("a + b;", value)
            Assert.Equal("(a + b;).F", GetChildren(root).Single().FullName)

            root = FormatResult(" M( )  ; ", value)
            Assert.Equal("(M( )  ;).F", GetChildren(root).Single().FullName)
        End Sub

        <Fact>
        Public Sub RootMixedExtras()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            ' Comment, then format specifier.
            Dim root = FormatResult("a', ac", value)
            Assert.Equal("a", root.FullName)

            ' Format specifier, then comment.
            root = FormatResult("a, ac , raw ', h", value)
            Assert.Equal("a, ac, raw", root.FullName)
        End Sub

        <Fact, WorkItem(1022165, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022165")>
        Public Sub Keywords_Root()
            Dim source = "
Class C
    Sub M()
        Dim [Namespace] As Integer = 3
    End Sub
End Class
"
            Dim assembly = GetAssembly(source)
            Dim value = CreateDkmClrValue(3)

            Dim root = FormatResult("[Namespace]", value)
            Verify(root,
                EvalResult("[Namespace]", "3", "Integer", "[Namespace]"))

            value = CreateDkmClrValue(assembly.GetType("C").Instantiate())
            root = FormatResult("Me", value)
            Verify(root,
                EvalResult("Me", "{C}", "C", "Me"))

            ' Verify that keywords aren't escaped by the ResultProvider at the
            ' root level (we would never expect to see "Namespace" passed as a
            ' resultName, but this check verifies that we leave them "as is").
            root = FormatResult("Namespace", CreateDkmClrValue(New Object()))
            Verify(root,
                EvalResult("Namespace", "{Object}", "Object", "Namespace"))
        End Sub

        <Fact>
        Public Sub MangledNames_CastRequired()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("NotMangled").Instantiate())

            Dim root = FormatResult("o", value)
            Verify(GetChildren(root),
                EvalResult("x (<>Mangled)", "0", "Integer", Nothing),
                EvalResult("x", "0", "Integer", "o.x"))
        End Sub

        <Fact>
        Public Sub MangledNames_StaticMembers()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim baseValue = CreateDkmClrValue(assembly.GetType("<>Mangled").Instantiate())

            Dim root = FormatResult("o", baseValue)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", Nothing, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "Integer", Nothing))


            Dim derivedValue = CreateDkmClrValue(assembly.GetType("NotMangled").Instantiate())

            root = FormatResult("o", derivedValue)
            children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", "NotMangled", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "Integer", Nothing, DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Public),
                EvalResult("y", "0", "Integer", "NotMangled.y", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Public))
        End Sub

        <Fact>
        Public Sub MangledNames_ExplicitInterfaceImplementation()
            Const il = "
.class interface public abstract auto ansi 'I<>Mangled'
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_P() cil managed
  {
  }

  .property instance int32 P()
  {
    .get instance int32 'I<>Mangled'::get_P()
  }
} // end of class 'I<>Mangled'

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
       implements 'I<>Mangled'
{
  .method private hidebysig newslot specialname virtual final 
          instance int32  'I<>Mangled.get_P'() cil managed
  {
    .override 'I<>Mangled'::get_P
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

  .property instance int32 'I<>Mangled.P'()
  {
    .get instance int32 C::'I<>Mangled.get_P'()
  }

  .property instance int32 P()
  {
    .get instance int32 C::'I<>Mangled.get_P'()
  }
} // end of class C
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("C").Instantiate())

            Dim root = FormatResult("instance", value)
            Verify(GetChildren(root),
                EvalResult("I<>Mangled.P", "1", "Integer", Nothing, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("P", "1", "Integer", "instance.P", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private))
        End Sub

        <Fact>
        Public Sub MangledNames_ArrayElement()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("NotMangled").Instantiate())

            Dim root = FormatResult("o", value)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("array", "{Length=1}", "System.Collections.Generic.IEnumerable(Of <>Mangled) {<>Mangled()}", "o.array", DkmEvaluationResultFlags.Expandable))
            Verify(GetChildren(children.Single()),
                EvalResult("(0)", "Nothing", "<>Mangled", Nothing))
        End Sub

        <Fact>
        Public Sub MangledNames_Namespace()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim baseValue = CreateDkmClrValue(assembly.GetType("<>Mangled.C").Instantiate())

            Dim root = FormatResult("o", baseValue)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", Nothing, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "Integer", Nothing))
        End Sub

        <Fact>
        Public Sub MangledNames_DebuggerTypeProxy()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("Type").Instantiate())

            Dim root = FormatResult("o", value)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("y", "True", "Boolean", Nothing, DkmEvaluationResultFlags.Boolean Or DkmEvaluationResultFlags.BooleanTrue),
                EvalResult("Raw View", Nothing, "", "o, raw", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data))

            Dim grandChildren = GetChildren(children.Last())
            Verify(grandChildren,
                EvalResult("x", "False", "Boolean", "o.x", DkmEvaluationResultFlags.Boolean))
        End Sub

        <Fact>
        Public Sub GenericTypeWithoutBacktick()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("C").MakeGenericType(GetType(Integer)).Instantiate())

            Dim root = FormatResult("o", value)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", "C(Of Integer)", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "Integer", "C(Of Integer).x"))
        End Sub

        <Fact>
        Public Sub BackTick_NonGenericType()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("C`1").Instantiate())

            Dim root = FormatResult("o", value)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", Nothing, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "Integer", Nothing))
        End Sub

        <Fact>
        Public Sub BackTick_GenericType()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("C`1").MakeGenericType(GetType(Integer)).Instantiate())

            Dim root = FormatResult("o", value)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", "C(Of Integer)", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "Integer", "C(Of Integer).x"))
        End Sub

        <Fact>
        Public Sub BackTick_Member()
            ' IL doesn't support using generic methods as property accessors so
            ' there's no way to test a "legitimate" backtick in a member name.
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("C").Instantiate())

            Dim root = FormatResult("o", value)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", "C", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x`1", "0", "Integer", fullName:=Nothing))
        End Sub

        <Fact>
        Public Sub BackTick_FirstCharacter()
            Const il = "
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
"

            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(il, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim value = CreateDkmClrValue(assembly.GetType("`1").MakeGenericType(GetType(Integer)).Instantiate())

            Dim root = FormatResult("o", value)
            Dim children = GetChildren(root)
            Verify(children,
                EvalResult("Shared members", Nothing, "", Nothing, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children.Single()),
                EvalResult("x", "0", "Integer", fullName:=Nothing))
        End Sub

    End Class

End Namespace
