' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class UnmanagedTypeConstraintTests
        Inherits BasicTestBase

        <Fact>
        Public Sub LoadingADifferentModifierTypeForUnmanagedConstraint()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.InAttribute)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        dim obj = new TestRef()

        obj.M1(Of Integer)()      ' valid
        obj.M2(Of Integer)()      ' invalid
    End Sub
End Class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: '' is an unsupported type.
        obj.M2(Of Integer)()      ' invalid
        ~~~~~~~~~~~~~~~~~~~~
BC30649: 'T' is an unsupported type.
        obj.M2(Of Integer)()      ' invalid
        ~~~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub LoadingUnmanagedTypeModifier_OptionalIsError()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modopt([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M1(Of Integer)() ' valid
        obj.M2(Of Integer)() ' invalid
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: '' is an unsupported type.
        obj.M2(Of Integer)() ' invalid
        ~~~~~~~~~~~~~~~~~~~~
BC30649: 'T' is an unsupported type.
        obj.M2(Of Integer)() ' invalid
        ~~~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub LoadingUnmanagedTypeModifier_MoreThanOneModifier()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modopt([mscorlib]System.DateTime) modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M1(Of Integer)() ' valid
        obj.M2(Of Integer)() ' invalid
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: '' is an unsupported type.
        obj.M2(Of Integer)() ' invalid
        ~~~~~~~~~~~~~~~~~~~~
BC30649: 'T' is an unsupported type.
        obj.M2(Of Integer)() ' invalid
        ~~~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub LoadingUnmanagedTypeModifier_ModreqWithoutAttribute()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M1(Of Integer)() ' valid
        obj.M2(Of Integer)() ' invalid
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: 'T' is an unsupported type.
        obj.M2(Of Integer)() ' invalid
        ~~~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub LoadingUnmanagedTypeModifier_ModreqGeneric()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType`1)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor
}

.class public auto ansi beforefieldinit System.Runtime.InteropServices.UnmanagedType`1<T>
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M1(Of Integer)()
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: '' is an unsupported type.
        obj.M1(Of Integer)()
        ~~~~~~~~~~~~~~~~~~~~
BC30649: 'T' is an unsupported type.
        obj.M1(Of Integer)()
        ~~~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub LoadingUnmanagedTypeModifier_AttributeWithoutModreq()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M1(Of Integer)() ' valid
        obj.M2(Of Integer)() ' invalid
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: 'T' is an unsupported type.
        obj.M2(Of Integer)() ' invalid
        ~~~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedTypeModreqOnOverriddenMethod()
            Dim reference = CreateCSharpCompilation("
public class Parent
{
    public virtual string M<T>() where T : unmanaged => ""Parent"";
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Child 
    Inherits Parent

    public overrides Function M(Of T as Structure)() As string
        Return ""Child""
    End Function
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})

            Dim typeParameter = compilation.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)

            typeParameter = compilation.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.False(typeParameter.HasUnmanagedTypeConstraint)

            AssertTheseDiagnostics(compilation, <expected>
BC32077: 'Public Overrides Function M(Of T As Structure)() As String' cannot override 'Public Overridable Overloads Function M(Of T As Structure)() As String' because they differ by type parameter constraints.
    public overrides Function M(Of T as Structure)() As string
                              ~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedTypeModreqOnImplementedMethod()
            Dim reference = CreateCSharpCompilation("
public interface Parent
{
    string M<T>() where T : unmanaged;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Child 
    Implements Parent

    Function M(Of T as Structure)() As string Implements Parent.M
        Return ""Child""
    End Function
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})

            Dim typeParameter = compilation.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)

            typeParameter = compilation.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.False(typeParameter.HasUnmanagedTypeConstraint)

            AssertTheseDiagnostics(compilation, <expected>
BC32078: 'Public Function M(Of T As Structure)() As String' cannot implement 'Parent.Function M(Of T As Structure)() As String' because they differ by type parameter constraints.
    Function M(Of T as Structure)() As string Implements Parent.M
                                                         ~~~~~~~~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedConstraintWithClassConstraint_IL()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<class (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M(Of integer)()
        obj.M(Of string)()
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: 'T' is an unsupported type.
        obj.M(Of integer)()
        ~~~~~~~~~~~~~~~~~~~
BC30649: 'T' is an unsupported type.
        obj.M(Of string)()
        ~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedConstraintWithConstructorConstraint_IL()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<.ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M(Of integer)()
        obj.M(Of string)()
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: 'T' is an unsupported type.
        obj.M(Of integer)()
        ~~~~~~~~~~~~~~~~~~~
BC30649: 'T' is an unsupported type.
        obj.M(Of string)()
        ~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedConstraintWithoutValueTypeConstraint_IL()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<(class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M(Of integer)()
        obj.M(Of string)()
    end sub
end class
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC30649: 'T' is an unsupported type.
        obj.M(Of integer)()
        ~~~~~~~~~~~~~~~~~~~
BC30649: 'T' is an unsupported type.
        obj.M(Of string)()
        ~~~~~~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedConstraintWithTypeConstraint_IL()

            Dim ilSource = IsUnmanagedAttributeIL + "
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<valuetype .ctor (class [mscorlib]System.IComparable, class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

}"

            Dim reference = CompileIL(ilSource, prependDefaultHeader:=False)

            Dim code = "
public class Test
    public shared sub Main()
        Dim obj = new TestRef()

        obj.M(Of integer)()
        obj.M(Of string)()
        obj.M(Of S1)()
    end sub
end class

Structure S1
End Structure
"

            CreateCompilation(code, references:={reference}).
                AssertTheseDiagnostics(<expected>
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
        obj.M(Of string)()
            ~~~~~~~~~~~~
BC37332: Type argument 'String' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        obj.M(Of string)()
            ~~~~~~~~~~~~
BC32044: Type argument 'S1' does not inherit from or implement the constraint type 'IComparable'.
        obj.M(Of S1)()
            ~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedTypeModreqNotSet()
            Dim reference = CreateCSharpCompilation("
public interface Parent
{
    string M<T>() where T : struct;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Child 
    Implements Parent

    Function M(Of T as Structure)() As string Implements Parent.M
        Return ""Child""
    End Function
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})

            Dim typeParameter = compilation.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.False(typeParameter.HasUnmanagedTypeConstraint)

            typeParameter = compilation.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.False(typeParameter.HasUnmanagedTypeConstraint)

            AssertNoDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_Array()
            Dim reference = CreateCSharpCompilation("
public class Test<T> where T : unmanaged
{
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As Object
        o = GetType(Test(Of Integer()))
        o = GetType(Test(Of Integer()()))
        o = GetType(Test(Of Integer(,)))
        o = GetType(Test(Of Integer))
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected>
BC32105: Type argument 'Integer()' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer()))
                            ~~~~~~~~~
BC37332: Type argument 'Integer()' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer()))
                            ~~~~~~~~~
BC32105: Type argument 'Integer()()' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer()()))
                            ~~~~~~~~~~~
BC37332: Type argument 'Integer()()' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer()()))
                            ~~~~~~~~~~~
BC32105: Type argument 'Integer(*,*)' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer(,)))
                            ~~~~~~~~~~
BC37332: Type argument 'Integer(*,*)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer(,)))
                            ~~~~~~~~~~
                                                </expected>)

            compilation = CreateCompilation(source, references:={reference}, parseOptions:=TestOptions.Regular17_13)
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC32105: Type argument 'Integer()' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer()))
                            ~~~~~~~~~
BC37332: Type argument 'Integer()' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer()))
                            ~~~~~~~~~
BC32105: Type argument 'Integer()()' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer()()))
                            ~~~~~~~~~~~
BC37332: Type argument 'Integer()()' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer()()))
                            ~~~~~~~~~~~
BC32105: Type argument 'Integer(*,*)' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer(,)))
                            ~~~~~~~~~~
BC37332: Type argument 'Integer(*,*)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer(,)))
                            ~~~~~~~~~~
                                                ]]></expected>)

            compilation = CreateCompilation(source, references:={reference}, parseOptions:=TestOptions.Regular16_9)
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC32105: Type argument 'Integer()' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer()))
                            ~~~~~~~~~
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        o = GetType(Test(Of Integer()))
                            ~~~~~~~~~
BC37332: Type argument 'Integer()' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer()))
                            ~~~~~~~~~
BC32105: Type argument 'Integer()()' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer()()))
                            ~~~~~~~~~~~
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        o = GetType(Test(Of Integer()()))
                            ~~~~~~~~~~~
BC37332: Type argument 'Integer()()' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer()()))
                            ~~~~~~~~~~~
BC32105: Type argument 'Integer(*,*)' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = GetType(Test(Of Integer(,)))
                            ~~~~~~~~~~
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        o = GetType(Test(Of Integer(,)))
                            ~~~~~~~~~~
BC37332: Type argument 'Integer(*,*)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o = GetType(Test(Of Integer(,)))
                            ~~~~~~~~~~
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        o = GetType(Test(Of Integer))
                            ~~~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_AnonymousType()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As New Test
        o.M(new with {.A = 1})
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC32105: Type argument '<anonymous type: A As Integer>' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o.M(new with {.A = 1})
          ~
BC37332: Type argument '<anonymous type: A As Integer>' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(new with {.A = 1})
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_TypedReference()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As New Test
        o.M(CType(Nothing, System.TypedReference))
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        o.M(CType(Nothing, System.TypedReference))
          ~
BC37332: Type argument 'TypedReference' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(CType(Nothing, System.TypedReference))
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_RefStruct()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged, allows ref struct {}

    void Tst()
    {
        this.M((RefS)default);
    }
}

public ref struct RefS { }
public ref struct RefG<T> { public T field; }
public ref struct Ref { ref int field; }
public ref struct StructWithIndirectRefField
{
    public Ref Field;
}
public ref struct StructWithIndirectRefField2
{
    public StructWithRefField<int> Field;
}
public ref struct StructWithRefField<T>
{
    public ref T RefField;
}

", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest), referencedAssemblies:=Basic.Reference.Assemblies.Net90.References.All).EmitToImageReference()

            Dim source = "
public class Program 
    <System.Obsolete>
    Shared Sub Main
        Dim o As New Test
        o.M(CType(Nothing, RefS))
        o.M(CType(Nothing, RefG(Of String)))
        o.M(CType(Nothing, Ref))
        o.M(CType(Nothing, StructWithIndirectRefField))
        o.M(CType(Nothing, StructWithIndirectRefField2))
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, targetFramework:=TargetFramework.Net90)
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC37332: Type argument 'RefG(Of String)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(CType(Nothing, RefG(Of String)))
          ~
BC30656: Field 'field' is of an unsupported type.
        o.M(CType(Nothing, Ref))
        ~~~~~~~~~~~~~~~~~~~~~~~~
BC30656: Field 'field' is of an unsupported type.
        o.M(CType(Nothing, StructWithIndirectRefField))
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30656: Field 'RefField' is of an unsupported type.
        o.M(CType(Nothing, StructWithIndirectRefField2))
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_Class()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As New Test
        o.M(New Program())
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC32105: Type argument 'Program' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o.M(New Program())
          ~
BC37332: Type argument 'Program' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(New Program())
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_StructWithManagedField()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As New Test
        o.M(New S1())
        o.M(New S2())
        o.M(New S3())
        o.M(New S4(Of Integer)())
        o.M(New S4(Of Program)())
        o.M(New S5(Of Program)())
    End Sub
End Class

Structure S1
    Dim F as Program
End Structure

Structure S2
    Dim F as Integer
End Structure

Structure S3
    Shared F as Program
End Structure

Structure S4(Of T)
    Dim F as T
End Structure

Structure S5(Of T)
    Dim F as Integer
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC37332: Type argument 'S1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(New S1())
          ~
BC37332: Type argument 'S4(Of Program)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(New S4(Of Program)())
          ~
                                                ]]></expected>)

            Dim s1 = compilation.GetTypeByMetadataName("S1")
            Dim s2 = compilation.GetTypeByMetadataName("S2")
            Dim s3 = compilation.GetTypeByMetadataName("S3")
            Dim s4 = compilation.GetTypeByMetadataName("S4`1")
            Dim s5 = compilation.GetTypeByMetadataName("S5`1")
            Assert.Equal(ManagedKind.Managed, s1.GetManagedKind(Nothing))
            Assert.Equal(ManagedKind.Unmanaged, s2.GetManagedKind(Nothing))
            Assert.Equal(ManagedKind.Unmanaged, s3.GetManagedKind(Nothing))
            Assert.Equal(ManagedKind.Managed, s4.GetManagedKind(Nothing))
            Assert.Equal(ManagedKind.UnmanagedWithGenerics, s5.GetManagedKind(Nothing))

            Assert.False(DirectCast(s1, INamedTypeSymbol).IsUnmanagedType)
            Assert.True(DirectCast(s2, INamedTypeSymbol).IsUnmanagedType)
            Assert.True(DirectCast(s3, INamedTypeSymbol).IsUnmanagedType)
            Assert.False(DirectCast(s4, INamedTypeSymbol).IsUnmanagedType)
            Assert.True(DirectCast(s5, INamedTypeSymbol).IsUnmanagedType)

            Dim s5T = s5.TypeParameters(0)
            Assert.Equal(ManagedKind.Managed, s5T.GetManagedKind(Nothing))
            Assert.False(DirectCast(s5T, ITypeSymbol).IsUnmanagedType)
            Assert.False(s5T.HasUnmanagedTypeConstraint)
            Assert.False(DirectCast(s5T, ITypeParameterSymbol).HasUnmanagedTypeConstraint)

            Dim mT = compilation.GetTypeByMetadataName("Test").GetMember(Of MethodSymbol)("M").TypeParameters(0)
            Assert.Equal(ManagedKind.Unmanaged, mT.GetManagedKind(Nothing))
            Assert.True(DirectCast(mT, ITypeSymbol).IsUnmanagedType)
            Assert.True(mT.HasUnmanagedTypeConstraint)
            Assert.True(DirectCast(mT, ITypeParameterSymbol).HasUnmanagedTypeConstraint)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_StructWithManagedProperty()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As New Test
        o.M(New S1())
        o.M(New S2())
    End Sub
End Class

Structure S1
    Property F as Program
End Structure

Structure S2
    Property F as Integer
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC37332: Type argument 'S1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(New S1())
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_StructWithEvent()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As New Test
        o.M(New S1())
    End Sub
End Class

Structure S1
    Event E as System.Action
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC37332: Type argument 'S1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(New S1())
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_StructWithCycle()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim o As New Test
        o.M(New S1())
        o.M(New S2())
    End Sub
End Class

Structure S1
    Dim F as S2
End Structure

Structure S2
    Dim F as S1
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S2' (variable 'F').
    'S2' contains 'S1' (variable 'F').
    Dim F as S2
        ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_Tuple()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main(Of T)(x As T)
        Dim o As New Test
        o.M((1, 1))
        o.M((1, x))
        o.M((1, ""s""))
        o.M((1, 2, 3, 4, 5, 6, 7, 8, ""s""))
        o.M(new S1())
        o.M(new S2())
        o.M(new S3())
        o.M(new S4(Of T)())
    End Sub
End Class

Structure S1
    Dim F as (Integer, Integer)
End Structure

Structure S2
    Dim F as (String, Integer)
End Structure

Structure S3
    Dim F as (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, String)
End Structure

Structure S4(Of T)
    Dim F as (T, Integer)
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC37332: Type argument '(Integer, x As T)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M((1, x))
          ~
BC37332: Type argument '(Integer, String)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M((1, "s"))
          ~
BC37332: Type argument '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, String)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M((1, 2, 3, 4, 5, 6, 7, 8, "s"))
          ~
BC37332: Type argument 'S2' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(new S2())
          ~
BC37332: Type argument 'S3' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(new S3())
          ~
BC37332: Type argument 'S4(Of T)' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(new S4(Of T)())
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_Enum()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main()
        Dim o As New Test
        o.M(E1.Val)
        o.M(new S1())
    End Sub
End Class

Enum E1
    Val
End Enum

Structure S1
    Dim F as E1
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            compilation.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_Interface()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main(x As I1)
        Dim o As New Test
        o.M(x)
        o.M(New S2())
    End Sub
End Class

Public Interface I1
End Interface

Structure S2
    Dim F as I1
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC32105: Type argument 'I1' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o.M(x)
          ~
BC37332: Type argument 'I1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(x)
          ~
BC37332: Type argument 'S2' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(New S2())
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_Pointer()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}

    public static S1 GetS1() => default;
    public static S2 GetS2() => default;
}

unsafe public struct S1
{
    int* P1; 
}

unsafe public struct S2
{
    delegate*<void> P1; 
}
",
                parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest),
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(True)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main()
        Dim o As New Test
        o.M(New S1())
        o.M(New S2())
        o.M(Test.GetS1())
        o.M(Test.GetS2())
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30656: Field 'P1' is of an unsupported type.
        o.M(New S1())
        ~~~~~~~~~~~~~
BC30656: Field 'P1' is of an unsupported type.
        o.M(New S2())
        ~~~~~~~~~~~~~
BC30656: Field 'P1' is of an unsupported type.
        o.M(Test.GetS1())
        ~~~~~~~~~~~~~~~~~
BC30656: Field 'P1' is of an unsupported type.
        o.M(Test.GetS2())
        ~~~~~~~~~~~~~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_01()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
    Dim s as string
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_02()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Dim s as string
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_03()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
    Dim s as string
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable '_field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_04()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Dim s as string
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable '_field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_05()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
    Dim s as string
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'fieldEvent').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC31044: Events declared with an 'As' clause must have a delegate type.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_06()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Dim s as string
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'fieldEvent').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC31044: Events declared with an 'As' clause must have a delegate type.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_07()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
    string s;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_08()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    string s;
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Dim field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
        ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_09()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
    string s;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable '_field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_10()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    string s;
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable '_field').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Property field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
             ~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_11()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    public T field;
    string s;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'fieldEvent').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC31044: Events declared with an 'As' clause must have a delegate type.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_ExpandingTypeArgument_12()
            Dim reference = CreateCSharpCompilation("
public struct YourStruct<T> where T : unmanaged
{
    string s;
    public T field;
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
Structure MyStruct(Of T)
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
End Structure
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC30294: Structure 'MyStruct' cannot contain an instance of itself: 
    'MyStruct(Of T)' contains 'YourStruct(Of MyStruct(Of MyStruct(Of T)))' (variable 'fieldEvent').
    'YourStruct(Of MyStruct(Of MyStruct(Of T)))' contains 'MyStruct(Of MyStruct(Of T))' (variable 'field').
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC37332: Type argument 'MyStruct(Of MyStruct(Of T))' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
          ~~~~~
BC31044: Events declared with an 'As' clause must have a delegate type.
    Event field as YourStruct(Of MyStruct(Of MyStruct(Of T)))
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub UnmanagedCheck_TypeParameter()
            Dim reference = CreateCSharpCompilation("
public class Test
{
    public void M<T>(T x) where T : unmanaged {}
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program
    Shared Sub Main(Of T1, T2 As Class, T3 As Structure)(x1 as T1, x2 As T2, x3 As T3)
        Dim o As New Test
        o.M(x1)
        o.M(x2)
        o.M(x3)
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference})
            AssertTheseDiagnostics(compilation, <expected><![CDATA[
BC32105: Type argument 'T1' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o.M(x1)
          ~
BC37332: Type argument 'T1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(x1)
          ~
BC32105: Type argument 'T2' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o.M(x2)
          ~
BC37332: Type argument 'T2' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(x2)
          ~
BC37332: Type argument 'T3' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        o.M(x3)
          ~
                                                ]]></expected>)
        End Sub

        <Fact>
        Public Sub ConsumeAssertEqual()
            Dim reference = CreateCSharpCompilation("
using System;

public class Assert
{
	public static void Equal<T>(
		T[] expected,
		T[] actual)
			where T : unmanaged, IEquatable<T>
	{
        System.Console.Write(""T[]"");
    }

	public static void Equal<T>(
		T expected,
		T actual)
    {
        System.Console.Write(""T"");
    }
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Assert.Equal(New Integer() {1, 2}, New Integer() {1, 2})
        Assert.Equal(New String() {1, 2}, New String() {1, 2})
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:="T[]T").VerifyDiagnostics()

            compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular17_13)
            CompileAndVerify(compilation, expectedOutput:="T[]T").VerifyDiagnostics()

            compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            compilation.AssertTheseDiagnostics(<expected>
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        Assert.Equal(New Integer() {1, 2}, New Integer() {1, 2})
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        Assert.Equal(New String() {1, 2}, New String() {1, 2})
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact>
        Public Sub ConsumeExtensionMethod()
            Dim reference = CreateCSharpCompilation("
using System;

public static class Assert
{
	public static void Equal<T>(
		this T[] expected,
		T[] actual)
			where T : unmanaged, IEquatable<T>
	{
        System.Console.Write(""T[]"");
    }

	public static void Equal<T>(
		this T expected,
		T actual)
    {
        System.Console.Write(""T"");
    }
}
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Call New Integer() {1, 2}.Equal(New Integer() {1, 2})
        Call New String() {1, 2}.Equal(New String() {1, 2})
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:="T[]T").VerifyDiagnostics()

            compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular17_13)
            CompileAndVerify(compilation, expectedOutput:="T[]T").VerifyDiagnostics()

            compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="TT").VerifyDiagnostics()
        End Sub

        Private Const IsUnmanagedAttributeIL As String = "
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly Test
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78 63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module Test.dll
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsUnmanagedAttribute
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}
"

    End Class
End Namespace
