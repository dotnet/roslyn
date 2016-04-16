' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class InaccessibleOverridingTests
        Inherits BasicTestBase

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub EmitExplicitOverride()
            ' In order for Class3.f to override Class1.f (which it can because Class2.f is not
            ' accessible to Class3, since there is no IVT Proj2->Proj3), a explicit override must
            ' be emitted to metadata. 
            Dim proj1 = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="Proj1">
                    <file name="Class1.vb">
                        <![CDATA[
Imports System

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj2")> 
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj3")> 

Public Class Class1
    Friend Overridable Sub f()
        Console.WriteLine("Class1.f")
    End Sub
End Class
]]>
                    </file>
                </compilation>)
            Dim proj1ref = New VisualBasicCompilationReference(proj1)

            Dim proj2 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
                <compilation name="Proj2">
                    <file name="Class2.vb">
                        <![CDATA[
Imports System

Public Class Class2
    Inherits Class1
    Friend Overridable Shadows Sub f()
        Console.WriteLine("Class2.f")
    End Sub
End Class
]]>
                    </file>
                </compilation>, {proj1ref})
            Dim proj2ref = New VisualBasicCompilationReference(proj2)

            Dim proj3 = CompileAndVerify(
                <compilation name="Proj3">
                    <file name="Class3.vb">
                        <![CDATA[
Imports System

Public Class Class3
    Inherits Class2

    Friend Overrides Sub f()
        Console.WriteLine("Class3.f")
    End Sub
End Class

Public Module Module1
    Public Sub Main()
        Dim x As Class1 = New Class3()
        x.f()
    End Sub
End Module
]]>
                    </file>
                </compilation>, additionalRefs:={proj1ref, proj2ref}, expectedOutput:="Class3.f")

            proj3.VerifyDiagnostics()   ' no errors.
        End Sub

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub EmitExplicitOverrideOnProperty()
            ' In order for Class3.p to override Class1.p (which it can because Class2.p is not
            ' accessible to Class3, since there is no IVT Proj2->Proj3), a explicit override must
            ' be emitted to metadata. 
            Dim proj1 = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="Proj1">
                    <file name="Class1.vb">
                        <![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj2")> 
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj3")> 

Public Class Class1
    Friend Overridable ReadOnly Property P() As String
      Get
        Return "Class1.P"
      End Get
    End Property
End Class
]]>
                    </file>
                </compilation>, OutputKind.DynamicallyLinkedLibrary)
            Dim proj1ref = New VisualBasicCompilationReference(proj1)

            Dim proj2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                <compilation name="Proj2">
                    <file name="Class2.vb">
                        <![CDATA[
Public Class Class2
    Inherits Class1
    Friend Overridable Shadows ReadOnly Property P() As String
      Get
        Return "Class2.P"
      End Get
    End Property
End Class
]]>
                    </file>
                </compilation>, additionalRefs:={proj1ref})
            Dim proj2ref = New VisualBasicCompilationReference(proj2)

            Dim proj3 = CompileAndVerify(
                <compilation name="Proj3">
                    <file name="Class3.vb">
                        <![CDATA[
Imports System

Public Class Class3
    Inherits Class2

    Friend Overrides ReadOnly Property P() As String
      Get
        Return "Class3.P"
      End Get
    End Property
End Class

Public Module Module1
    Public Sub Main()
        Dim x As Class1 = New Class3()
        Console.WriteLine(x.P)
    End Sub
End Module
]]>
                    </file>
                </compilation>, additionalRefs:={proj1ref, proj2ref}, expectedOutput:="Class3.P")

            proj3.VerifyDiagnostics()   ' no errors.
        End Sub

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub OverrideWithInterveningFriendOverride()
            Dim proj1 = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="Proj1">
                    <file name="Class1.vb">
                        <![CDATA[
Imports System

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj2")> 
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj3")> 

Public Class Class1
    Friend Overridable Sub f()
        Console.WriteLine("Class1.f")
    End Sub
End Class
]]>
                    </file>
                </compilation>)
            Dim proj1ref = New VisualBasicCompilationReference(proj1)

            Dim proj2 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
                <compilation name="Proj2">
                    <file name="Class2.vb">
                        <![CDATA[
Imports System

Public Class Class2
    Inherits Class1
    Friend Overrides Sub f()
        Console.WriteLine("Class2.f")
    End Sub
End Class
]]>
                    </file>
                </compilation>, {proj1ref})
            Dim proj2ref = New VisualBasicCompilationReference(proj2)

            Dim proj3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                <compilation name="Proj3">
                    <file name="Class3.vb">
                        <![CDATA[
Imports System

Public Class Class3
    Inherits Class2

    Friend Overrides Sub f()
        Console.WriteLine("Class3.f")
    End Sub
End Class

Public Module Module1
    Public Sub Main()
        Dim x As Class1 = New Class3()
        x.f()
    End Sub
End Module
]]>
                    </file>
                </compilation>, additionalRefs:={proj1ref, proj2ref})

            CompilationUtils.AssertTheseDiagnostics(proj3,
<expected>
BC30981: 'Friend Overrides Sub f()' in class 'Class3' cannot override 'Friend Overridable Sub f()' in class 'Class1' because an intermediate class 'Class2' overrides 'Friend Overridable Sub f()' in class 'Class1' but is not accessible.
    Friend Overrides Sub f()
                         ~
</expected>)
        End Sub

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub OverridePropertyWithInterveningFriendOverride()
            Dim proj1 = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="Proj1">
                    <file name="Class1.vb">
                        <![CDATA[
Imports System

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj2")> 
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Proj3")> 

Public Class Class1
    Friend Overridable ReadOnly Property P() As String
      Get
        Return "Class1.P"
      End Get
    End Property
End Class
]]>
                    </file>
                </compilation>, OutputKind.DynamicallyLinkedLibrary)
            Dim proj1ref = New VisualBasicCompilationReference(proj1)

            Dim proj2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                <compilation name="Proj2">
                    <file name="Class2.vb">
                        <![CDATA[
Imports System

Public Class Class2
    Inherits Class1
    Friend Overrides ReadOnly Property P() As String
      Get
        Return "Class2.P"
      End Get
    End Property
End Class
]]>
                    </file>
                </compilation>, additionalRefs:={proj1ref})
            Dim proj2ref = New VisualBasicCompilationReference(proj2)

            Dim proj3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                <compilation name="Proj3">
                    <file name="Class3.vb">
                        <![CDATA[
Imports System

Public Class Class3
    Inherits Class2

    Friend Overrides ReadOnly Property P() As String
      Get
        Return "Class3.P"
      End Get
    End Property
End Class

Public Module Module1
    Public Sub Main()
        Dim x As Class1 = New Class3()
        Console.WriteLine(x.P)
    End Sub
End Module
]]>
                    </file>
                </compilation>, additionalRefs:={proj1ref, proj2ref})

            CompilationUtils.AssertTheseDiagnostics(proj3,
<expected>
BC30981: 'Friend Overrides ReadOnly Property P As String' in class 'Class3' cannot override 'Friend Overridable ReadOnly Property P As String' in class 'Class1' because an intermediate class 'Class2' overrides 'Friend Overridable ReadOnly Property P As String' in class 'Class1' but is not accessible.
    Friend Overrides ReadOnly Property P() As String
                                       ~
</expected>)
        End Sub

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub EmitExplicitOverrideMetadata()
            Dim p1AssemblyName = "P1" + Guid.NewGuid().ToString().Replace("-", "")
            Dim proj2ILText = <![CDATA[
.assembly extern <<P1Name>>
{
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
  .ver 4:0:0:0
}
.assembly '<<GeneratedFileName>>'
{
}
.module '<<GeneratedFileName>>.dll'

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Class2
       extends [<<P1Name>>]Class1
{
  .method assembly hidebysig newslot strict virtual 
          instance void  f() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Class2.P2"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Class2::f

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [<<P1Name>>]Class1::.ctor()
    IL_0006:  ret
  } // end of method Class2::.ctor

} // end of class Class2

// =============================================================
]]>.Value.Replace("<<P1Name>>", p1AssemblyName)

            Using proj2ILFile = IlasmUtilities.CreateTempAssembly(proj2ILText, appendDefaultHeader:=False)
                Dim proj2AssemblyName = IO.Path.GetFileNameWithoutExtension(proj2ILFile.Path)
                Dim proj2Ref = MetadataReference.CreateFromImage(ReadFromFile(proj2ILFile.Path))
                Dim proj2AssemblyNameBytes As New System.Text.StringBuilder()
                proj2AssemblyNameBytes.Append(proj2AssemblyName.Length.ToString("X") + " ")
                For Each c In proj2AssemblyName
                    proj2AssemblyNameBytes.Append(AscW(c).ToString("X") & " ")
                Next

                Dim proj1ILText = <![CDATA[
    .assembly extern mscorlib
    {
      .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
      .ver 4:0:0:0
    }
    .assembly '<<P1Name>>'
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 ]]>.Value & proj2AssemblyNameBytes.ToString() &
                <![CDATA[ 00 00 )                            
      .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 02 50 33 00 00 )                            // ...P3..
    }
    .module '<<P1Name>>.dll'

    // =============== CLASS MEMBERS DECLARATION ===================

    .class public auto ansi beforefieldinit Class1
           extends [mscorlib]System.Object
    {
      .method assembly hidebysig newslot strict virtual 
              instance void  f() cil managed
      {
        // Code size       11 (0xb)
        .maxstack  8
        IL_0000:  ldstr      "Class1.f"
        IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_000a:  ret
      } // end of method Class1::f

      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       7 (0x7)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  ret
      } // end of method Class1::.ctor

    } // end of class Class1    ]]>.Value
                proj1ILText = proj1ILText.Replace("<<P1Name>>", p1AssemblyName)

                Using proj1ILFile = IlasmUtilities.CreateTempAssembly(proj1ILText, appendDefaultHeader:=False)
                    Dim proj1Ref = MetadataReference.CreateFromImage(ReadFromFile(proj1ILFile.Path))

                    Dim proj3 = CompileAndVerify(
                        <compilation name="P3">
                            <file name="Class3.vb">
                                <![CDATA[
Imports System

Public Class Class3
    Inherits Class2

    Friend Overrides Sub f()
        Console.WriteLine("Class3.f")
    End Sub
End Class

Public Module Module1
    Public Sub Main()
        Dim x As Class1 = New Class3()
        x.f()
    End Sub
End Module
]]>
                            </file>
                        </compilation>, additionalRefs:={proj1Ref, proj2Ref}, expectedOutput:="Class3.f")

                    proj3.VerifyDiagnostics()   ' no errors.

                End Using
            End Using
        End Sub

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub OverrideWithInterveningFriendMetadata()
            Dim proj2ILText = <![CDATA[
.assembly extern P1
{
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
  .ver 4:0:0:0
}
.assembly P2
{
}
.module P2.dll

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Class2
       extends [P1]Class1
{
  .method assembly hidebysig strict virtual 
          instance void  f() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Class2.P2"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Class2::f

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [P1]Class1::.ctor()
    IL_0006:  ret
  } // end of method Class2::.ctor

} // end of class Class2

// =============================================================
]]>.Value

            Using proj2ILFile = IlasmUtilities.CreateTempAssembly(proj2ILText, appendDefaultHeader:=False)
                Dim proj2AssemblyName = IO.Path.GetFileNameWithoutExtension(proj2ILFile.Path)
                Dim proj2Ref = MetadataReference.CreateFromImage(ReadFromFile(proj2ILFile.Path))

                Dim proj1ILText = <![CDATA[
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
  .ver 4:0:0:0
}
.assembly P1
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 02 50 32 00 00 )                            // ...P2..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 02 50 33 00 00 )                            // ...P3..
}
.module P1.dll

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Class1
       extends [mscorlib]System.Object
{
  .method assembly newslot strict virtual 
          instance void  f() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Class1.f"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Class1::f

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Class1::.ctor

} // end of class Class1    ]]>.Value

                Using proj1ILFile = IlasmUtilities.CreateTempAssembly(proj1ILText, appendDefaultHeader:=False)
                    Dim proj1Ref = MetadataReference.CreateFromImage(ReadFromFile(proj1ILFile.Path))

                    Dim proj3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                        <compilation name="P3">
                            <file name="Class3.vb">
                                <![CDATA[
Imports System

Public Class Class3
    Inherits Class2

    Friend Overrides Sub f()
        Console.WriteLine("Class3.f")
    End Sub
End Class

Public Module Module1
    Public Sub Main()
        Dim x As Class1 = New Class3()
        x.f()
    End Sub
End Module
]]>
                            </file>
                        </compilation>, additionalRefs:={proj1Ref, proj2Ref})

                    CompilationUtils.AssertTheseDiagnostics(proj3,
        <expected>
BC30981: 'Friend Overrides Sub f()' in class 'Class3' cannot override 'Friend Overridable Sub f()' in class 'Class1' because an intermediate class 'Class2' overrides 'Friend Overridable Sub f()' in class 'Class1' but is not accessible.
    Friend Overrides Sub f()
                         ~
</expected>)

                End Using
            End Using
        End Sub

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub CannotOverrideInAccessibleMemberInMetadata()
            Dim customIL = <![CDATA[

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
  .ver 4:0:0:0
}
.assembly extern Microsoft.VisualBasic
{
  .ver 10:0:0:0
  .publickeytoken = (b0 3f 5f 7f 11 d5 0a 3a)
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
  .ver 4:0:0:0
}
.assembly '<<GeneratedFileName>>'
{
}
.module '<<GeneratedFileName>>.dll'


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi Cls1
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
  } // end of method Cls1::.ctor

  .method private instance void  foo() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Cls1::foo

} // end of class Cls1
]]>

            ' Because the private is defined in another assembly, we don't import it.
            ' So BC30284 is reasonable, and Dev10 does the same.

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, appendDefaultHeader:=False)
                Dim ilRef = MetadataReference.CreateFromImage(ReadFromFile(reference.Path))
                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
                    <compilation name="CannotOverrideInAccessibleMemberInMetadata">
                        <file name="a.vb">
                        Class Cls2
                            Inherits Cls1
                            Private Overrides Sub foo()
                            End Sub
                        End Class
                    </file>
                    </compilation>, references:={ilRef})

                Dim expectedErrors1 = <errors>
BC30284: sub 'foo' cannot be declared 'Overrides' because it does not override a sub in a base class.
                            Private Overrides Sub foo()
                                                  ~~~
                 </errors>

                CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
            End Using
        End Sub

        <WorkItem(541742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541742")>
        <Fact>
        Public Sub Bug14346()
            Dim customIL = <![CDATA[
// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
  .ver 4:0:0:0
}
.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 08 42 75 67 31 34 33 34 36 00 00 )          // ...Bug14346..
}
.module '<<GeneratedFileName>>.dll'

.class public auto ansi beforefieldinit CaseMembers
       extends [mscorlib]System.Object
{
  .method family hidebysig newslot virtual 
          instance int32  foo() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method CaseMembers::foo

  .method assembly hidebysig instance int32 
          Foo() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method CaseMembers::Foo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CaseMembers::.ctor

} // end of class CaseMembers

]]>

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, appendDefaultHeader:=False)
                Dim ilRef = MetadataReference.CreateFromImage(ReadFromFile(reference.Path))
                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                    <compilation name="Bug14346">
                        <file name="a.vb">
Option Strict On
Imports System
Module m1
    class Car : Inherits CaseMembers
        protected overrides function foo() as integer
            return MyBase.foo()
        end function
        public function bar() as integer
            return foo()
        end function
    end class
    sub Main()
        dim c as new Car
        Console.WriteLine( "running test..." )
        Console.WriteLine( c.bar() )
    end sub
End Module
                    </file>
                    </compilation>, additionalRefs:={ilRef})

                CompilationUtils.AssertNoDiagnostics(compilation1)
            End Using
        End Sub



    End Class
End Namespace
