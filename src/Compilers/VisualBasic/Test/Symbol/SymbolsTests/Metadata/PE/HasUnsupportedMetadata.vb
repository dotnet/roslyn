' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class HasUnsupportedMetadata : Inherits BasicTestBase

        <Fact()>
        Public Sub Test1()

            Dim iLSource =
            <![CDATA[
.assembly extern NotReferenced {}

.class public auto ansi sealed D1`1<T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(object TargetObject,
                               native int TargetMethod) runtime managed
  {
  } // end of method D1`1::.ctor

  .method public newslot strict virtual instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T x,
                      class [mscorlib]System.AsyncCallback DelegateCallback,
                      object DelegateAsyncState) runtime managed
  {
  } // end of method D1`1::BeginInvoke

  .method public newslot strict virtual instance void 
          EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
  {
  } // end of method D1`1::EndInvoke

  .method public newslot strict virtual instance void 
          Invoke(!T x) runtime managed
  {
  } // end of method D1`1::Invoke

} // end of class D1`1

.class public auto ansi C2`1<T>
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi C3
       extends class C2`1<class [NotReferenced]C1[]>
{
  .field public class C2`1<class [NotReferenced]C1[]> F1
  .field private class D1`1<class [NotReferenced]C1[]> E1Event
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class C2`1<class [NotReferenced]C1[]>::.ctor()
    IL_0006:  ret
  } // end of method C3::.ctor

  .method public instance void  M1(class C2`1<class [NotReferenced]C1[]> x) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method C3::M1

  .method public specialname instance void 
          add_E1(class D1`1<class [NotReferenced]C1[]> obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class D1`1<class [NotReferenced]C1[]> C3::E1Event
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_000d:  castclass  class D1`1<class [NotReferenced]C1[]>
    IL_0012:  stfld      class D1`1<class [NotReferenced]C1[]> C3::E1Event
    IL_0017:  ret
  } // end of method C3::add_E1

  .method public specialname instance void 
          remove_E1(class D1`1<class [NotReferenced]C1[]> obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class D1`1<class [NotReferenced]C1[]> C3::E1Event
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_000d:  castclass  class D1`1<class [NotReferenced]C1[]>
    IL_0012:  stfld      class D1`1<class [NotReferenced]C1[]> C3::E1Event
    IL_0017:  ret
  } // end of method C3::remove_E1

  .method public specialname instance class C2`1<class [NotReferenced]C1[]> 
          get_P1() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (class C2`1<class [NotReferenced]C1[]> V_0)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method C3::get_P1

  .method public specialname instance void 
          set_P1(class C2`1<class [NotReferenced]C1[]> 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method C3::set_P1

  .event class D1`1<class [NotReferenced]C1[]> E1
  {
    .addon instance void C3::add_E1(class D1`1<class [NotReferenced]C1[]>)
    .removeon instance void C3::remove_E1(class D1`1<class [NotReferenced]C1[]>)
  } // end of event C3::E1
  .property instance class C2`1<class [NotReferenced]C1[]>
          P1()
  {
    .get instance class C2`1<class [NotReferenced]C1[]> C3::get_P1()
    .set instance void C3::set_P1(class C2`1<class [NotReferenced]C1[]>)
  } // end of property C3::P1
} // end of class C3
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="Test">
        <file name="a.vb">
        </file>
    </compilation>, iLSource)

            Dim c3 = compilation1.GetTypeByMetadataName("C3")

            Assert.Null(c3.GetUseSiteErrorInfo())
            Assert.False(c3.HasUnsupportedMetadata)
            Assert.False(c3.ContainingSymbol.HasUnsupportedMetadata)

            Dim f1 = c3.GetMembers("F1").Single()
            Assert.NotNull(f1.GetUseSiteErrorInfo())
            Assert.False(f1.HasUnsupportedMetadata)

            Dim m1 = c3.GetMembers("M1").Single()
            Assert.NotNull(m1.GetUseSiteErrorInfo())
            Assert.False(m1.HasUnsupportedMetadata)

            Dim x = DirectCast(m1, MethodSymbol).Parameters(0)
            Assert.Null(x.GetUseSiteErrorInfo())
            Assert.False(x.HasUnsupportedMetadata)

            Dim e1 = c3.GetMembers("E1").Single()
            Assert.NotNull(e1.GetUseSiteErrorInfo())
            Assert.False(e1.HasUnsupportedMetadata)

            Dim p1 = c3.GetMembers("P1").Single()
            Assert.NotNull(p1.GetUseSiteErrorInfo())
            Assert.False(p1.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub Test2()

            Dim iLSource =
            <![CDATA[
.assembly extern NotReferenced {}

.class public auto ansi sealed D1`1<T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(object TargetObject,
                               native int TargetMethod) runtime managed
  {
  } // end of method D1`1::.ctor

  .method public newslot strict virtual instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T x,
                      class [mscorlib]System.AsyncCallback DelegateCallback,
                      object DelegateAsyncState) runtime managed
  {
  } // end of method D1`1::BeginInvoke

  .method public newslot strict virtual instance void 
          EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
  {
  } // end of method D1`1::EndInvoke

  .method public newslot strict virtual instance void 
          Invoke(!T x) runtime managed
  {
  } // end of method D1`1::Invoke

} // end of class D1`1

.class public auto ansi C2`1<T>
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi C3
       extends class C2`1<class [NotReferenced]C1 modreq(int8)[]>
{
  .field public class C2`1<class [NotReferenced]C1 modreq(int8)[]> F1
  .field private class D1`1<class [NotReferenced]C1 modreq(int8)[]> E1Event
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class C2`1<class [NotReferenced]C1 modreq(int8)[]>::.ctor()
    IL_0006:  ret
  } // end of method C3::.ctor

  .method public instance void  M1(class C2`1<class [NotReferenced]C1 modreq(int8)[]> x) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method C3::M1

  .method public specialname instance void 
          add_E1(class D1`1<class [NotReferenced]C1 modreq(int8)[]> obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class D1`1<class [NotReferenced]C1 modreq(int8)[]> C3::E1Event
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_000d:  castclass  class D1`1<class [NotReferenced]C1 modreq(int8)[]>
    IL_0012:  stfld      class D1`1<class [NotReferenced]C1 modreq(int8)[]> C3::E1Event
    IL_0017:  ret
  } // end of method C3::add_E1

  .method public specialname instance void 
          remove_E1(class D1`1<class [NotReferenced]C1 modreq(int8)[]> obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class D1`1<class [NotReferenced]C1 modreq(int8)[]> C3::E1Event
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_000d:  castclass  class D1`1<class [NotReferenced]C1 modreq(int8)[]>
    IL_0012:  stfld      class D1`1<class [NotReferenced]C1 modreq(int8)[]> C3::E1Event
    IL_0017:  ret
  } // end of method C3::remove_E1

  .method public specialname instance class C2`1<class [NotReferenced]C1 modreq(int8)[]> 
          get_P1() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (class C2`1<class [NotReferenced]C1 modreq(int8)[]> V_0)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method C3::get_P1

  .method public specialname instance void 
          set_P1(class C2`1<class [NotReferenced]C1 modreq(int8)[]> 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method C3::set_P1

  .event class D1`1<class [NotReferenced]C1 modreq(int8)[]> E1
  {
    .addon instance void C3::add_E1(class D1`1<class [NotReferenced]C1 modreq(int8)[]>)
    .removeon instance void C3::remove_E1(class D1`1<class [NotReferenced]C1 modreq(int8)[]>)
  } // end of event C3::E1
  .property instance class C2`1<class [NotReferenced]C1 modreq(int8)[]>
          P1()
  {
    .get instance class C2`1<class [NotReferenced]C1 modreq(int8)[]> C3::get_P1()
    .set instance void C3::set_P1(class C2`1<class [NotReferenced]C1 modreq(int8)[]>)
  } // end of property C3::P1
} // end of class C3

.class public auto ansi beforefieldinit Microsoft.VisualC.StlClr.Generic.ContainerRandomAccessIterator`1<TValue>
       extends [mscorlib]System.Object
{
}

.class interface public abstract auto ansi beforefieldinit Microsoft.VisualC.StlClr.IVector`1<TValue>
{
  .method public hidebysig newslot abstract virtual 
          instance !TValue&  front() cil managed
  {
  } // end of method IVector`1::front

.method public hidebysig newslot abstract virtual 
          instance void modreq([mscorlib]System.Runtime.CompilerServices.IsUdtReturn) 
          begin(class Microsoft.VisualC.StlClr.Generic.ContainerRandomAccessIterator`1<!TValue>& A_1) cil managed
  {
  } // end of method IVector`1::begin
}

///////////////////////////////

.class public abstract auto ansi beforefieldinit X
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32&  get_Token() cil managed
  {
  } // end of method X::get_Token

  .property instance int32& Token()
  {
    .get instance int32& X::get_Token()
  } // end of property X::Token
} // end of class X

]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="Test">
        <file name="a.vb">
        </file>
    </compilation>, iLSource)

            Dim c3 = compilation1.GetTypeByMetadataName("C3")

            Assert.Null(c3.GetUseSiteErrorInfo())
            Assert.False(c3.HasUnsupportedMetadata)
            Assert.False(c3.ContainingSymbol.HasUnsupportedMetadata)

            Dim f1 = c3.GetMembers("F1").Single()
            Assert.NotNull(f1.GetUseSiteErrorInfo())
            Assert.True(f1.HasUnsupportedMetadata)

            Dim m1 = c3.GetMembers("M1").Single()
            Assert.NotNull(m1.GetUseSiteErrorInfo())
            Assert.True(m1.HasUnsupportedMetadata)

            Dim x = DirectCast(m1, MethodSymbol).Parameters(0)
            Assert.Null(x.GetUseSiteErrorInfo())
            Assert.True(x.HasUnsupportedMetadata)

            Dim e1 = c3.GetMembers("E1").Single()
            Assert.NotNull(e1.GetUseSiteErrorInfo())
            Assert.True(e1.HasUnsupportedMetadata)

            Dim p1 = c3.GetMembers("P1").Single()
            Assert.NotNull(p1.GetUseSiteErrorInfo())
            Assert.True(p1.HasUnsupportedMetadata)

            Dim intPtr = compilation1.GetTypeByMetadataName("System.IntPtr")

            Assert.True(intPtr.GetMembers("ToPointer").Single().HasUnsupportedMetadata)

            For Each m In intPtr.GetMembers("op_Explicit")
                Dim method = DirectCast(m, MethodSymbol)
                If method.ReturnType.IsErrorType() OrElse method.Parameters(0).Type.IsErrorType() Then
                    Assert.True(m.HasUnsupportedMetadata)
                Else
                    Assert.False(m.HasUnsupportedMetadata)
                End If
            Next

            ''

            Dim vector = compilation1.GetTypeByMetadataName("Microsoft.VisualC.StlClr.IVector`1")
            'unsupported MD in members doesn't propagate up to the type.
            Assert.False(vector.HasUnsupportedMetadata)

            'unsupported MD in the return type should propagate up to the method.
            Dim begin = vector.GetMember("begin")
            Assert.True(begin.HasUnsupportedMetadata)

            ''

            Dim typeX = compilation1.GetTypeByMetadataName("X")
            'unsupported MD in members doesn't propagate up to the type.
            Assert.False(typeX.HasUnsupportedMetadata)
        End Sub

        ''' <summary>
        ''' Throw a (handled) BadImageFormatException from MetadataDecoder
        ''' for a TypeRef resolution scope with Nil reference.
        ''' </summary>
        <Fact>
        <WorkItem(217689, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=217689")>
        <WorkItem(11795, "https://github.com/dotnet/roslyn/issues/11795")>
        Public Sub ResolutionScopeNilRef()
            Dim options = TestOptions.ReleaseDll.WithDeterministic(True)
            Dim comp1 = CreateCompilationWithMscorlib40(
{"Public Class A
End Class"},
                options:=options,
                assemblyName:="4D94B345-92CE-46BB-891E-048109648A0A")
            comp1.VerifyDiagnostics()
            Dim bytes1 = comp1.EmitToArray()
            Dim ref1 = AssemblyMetadata.CreateFromImage(bytes1).GetReference()

            Dim comp2 = CreateCompilationWithMscorlib40(
{"Public Class B
    Inherits A
End Class"},
                options:=options,
                assemblyName:="D4954046-BDDC-42C4-98F8-7E5573C16C33",
                references:={ref1})
            comp2.VerifyDiagnostics()
            Dim bytes2 = comp2.EmitToArray()

            ' Construct an unexpected assembly resolution scope from the valid compilation.
            ' Metadata contains a single TypeRef with assembly resolution scope where the
            ' scope is AssemblyRef 2: specifically the TypeRef to base class A. Replace that
            ' assembly resolution scope with AssemblyRef 0 (the unexpected value).
            Dim source3 =
"Class C
    Shared Sub Main()
        Dim o As New B()
    End Sub
End Class"
            ' In the TypeRef signature below, 10 is the assembly scope where (AssemblyRef << 2 | 2).
            ' (To verify, break in MetadataWriter.SerializeTypeRefTable when emitting comp2.)
            Dim validAssemblyRefTypeRef As Byte() = {10, 0, 82, 0, 0, 0}

            Dim invalidAssemblyRefTypeRef As Byte() = {2, 0, 82, 0, 0, 0} ' 2 is (AssemblyRef << 2 | 2)
            Dim ref2 = AssemblyMetadata.CreateFromImage(ReplaceBytes(bytes2, validAssemblyRefTypeRef, invalidAssemblyRefTypeRef)).GetReference()
            Dim comp3 = CreateCompilationWithMscorlib40({source3}, options:=options, references:={ref2})
            comp3.VerifyDiagnostics()
            Dim tree = comp3.SyntaxTrees(0)
            Dim model = comp3.GetSemanticModel(comp3.SyntaxTrees(0))
            Dim decl = tree.GetRoot().DescendantNodes.OfType(Of ObjectCreationExpressionSyntax).Single()
            Dim type = DirectCast(model.GetTypeInfo(decl).Type, TypeSymbol)
            Assert.Equal("B", type.ToTestDisplayString())
            Assert.False(type.IsErrorType())
            Assert.True(type.BaseType.IsErrorType()) ' Handled exception decoding base type TypeRef.

            ' As above but with nil ModuleRef.
            Dim invalidModuleRefTypeRef As Byte() = {1, 0, 82, 0, 0, 0} ' 1 is (ModuleRef << 2 | 1)
            ref2 = AssemblyMetadata.CreateFromImage(ReplaceBytes(bytes2, validAssemblyRefTypeRef, invalidModuleRefTypeRef)).GetReference()
            comp3 = CreateCompilationWithMscorlib40({source3}, options:=options, references:={ref2})
            comp3.VerifyDiagnostics()
            tree = comp3.SyntaxTrees(0)
            model = comp3.GetSemanticModel(tree)
            decl = tree.GetRoot().DescendantNodes.OfType(Of ObjectCreationExpressionSyntax).Single()
            type = DirectCast(model.GetTypeInfo(decl).Type, TypeSymbol)
            Assert.Equal("B", type.ToTestDisplayString())
            Assert.False(type.IsErrorType())
            Assert.True(type.BaseType.IsErrorType()) ' Handled exception decoding base type TypeRef.

            ' As above but with nil TypeRef.
            Dim invalidTypeRefTypeRef As Byte() = {3, 0, 82, 0, 0, 0} ' 3 is (TypeRef << 2 | 3)
            ref2 = AssemblyMetadata.CreateFromImage(ReplaceBytes(bytes2, validAssemblyRefTypeRef, invalidTypeRefTypeRef)).GetReference()
            comp3 = CreateCompilationWithMscorlib40({source3}, options:=options, references:={ref2})
            comp3.VerifyDiagnostics()
            tree = comp3.SyntaxTrees(0)
            model = comp3.GetSemanticModel(tree)
            decl = tree.GetRoot().DescendantNodes.OfType(Of ObjectCreationExpressionSyntax).Single()
            type = DirectCast(model.GetTypeInfo(decl).Type, TypeSymbol)
            Assert.Equal("B", type.ToTestDisplayString())
            Assert.False(type.IsErrorType())
            Assert.True(type.BaseType.IsErrorType()) ' Handled exception decoding base type TypeRef.
        End Sub

        <Fact>
        Public Sub TestFunctionPointerInMetadata()
            Dim csharpComp = CreateCSharpCompilation("
unsafe public class C
{
    public delegate*<void> field;
}", parseOptions:=New CSharp.CSharpParseOptions().WithLanguageVersion(CSharp.LanguageVersion.CSharp9),
    compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(True))

            Dim vbComp = CreateVisualBasicCompilation(Nothing, "
Public Module M
    Public Sub S(c As C)
        Dim f = c.field
    End Sub
End Module
",
                referencedCompilations:={csharpComp},
                referencedAssemblies:=LatestVbReferences,
                compilationOptions:=TestOptions.DebugDll)

            vbComp.AssertTheseDiagnostics(
<expected>
BC30656: Field 'field' is of an unsupported type.
        Dim f = c.field
                ~~~~~~~
</expected>
            )

            Dim c = vbComp.GetTypeByMetadataName("C")
            Dim field = c.GetField("field")
            Assert.True(field.HasUnsupportedMetadata)
            Assert.True(field.Type.HasUnsupportedMetadata)
            Assert.True(field.Type.IsErrorType())
            Assert.NotNull(field.GetUseSiteErrorInfo())
        End Sub

        Private Shared Function ReplaceBytes(bytes As ImmutableArray(Of Byte), before As Byte(), after As Byte()) As ImmutableArray(Of Byte)
            Dim index = IndexOfBytes(bytes, before, 0)
            Debug.Assert(index >= 0)
            Debug.Assert(IndexOfBytes(bytes, before, index + 1) < 0)
            Dim builder = ArrayBuilder(Of Byte).GetInstance()
            builder.AddRange(bytes.Take(index))
            builder.AddRange(after)
            builder.AddRange(bytes.Skip(index + before.Length))
            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function IndexOfBytes(bytes As ImmutableArray(Of Byte), pattern As Byte(), startIndex As Integer) As Integer
            Dim n = bytes.Length
            Dim m = pattern.Length
            For i = startIndex To n - m - 1
                For j = 0 To m - 1
                    If bytes(i + j) <> pattern(j) Then
                        GoTo EndOfLoop
                    End If
                Next
                Return i
EndOfLoop:
            Next
            Return -1
        End Function

    End Class

End Namespace

