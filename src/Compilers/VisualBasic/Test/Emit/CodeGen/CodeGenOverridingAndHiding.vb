' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenOverridingAndHiding
        Inherits BasicTestBase

        <WorkItem(540852, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540852")>
        <Fact>
        Public Sub TestSimpleMustOverride()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
MustInherit Class A
    Public MustOverride Function F As Integer()
    Protected MustOverride Sub Meth()
    Protected Friend MustOverride Property Prop As Integer()
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedSignatures:=
            {
                Signature("A", "F", ".method public newslot strict abstract virtual instance System.Int32[] F() cil managed"),
                Signature("A", "Meth", ".method family newslot strict abstract virtual instance System.Void Meth() cil managed"),
                Signature("A", "get_Prop", ".method famorassem newslot strict specialname abstract virtual instance System.Int32[] get_Prop() cil managed"),
                Signature("A", "set_Prop", ".method famorassem newslot strict specialname abstract virtual instance System.Void set_Prop(System.Int32[] Value) cil managed"),
                Signature("A", "Prop", ".property readwrite System.Int32[] Prop")
            })
        End Sub

        <WorkItem(528311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528311")>
        <WorkItem(540865, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540865")>
        <Fact>
        Public Sub TestSimpleOverrides()
            Dim source =
<compilation>
    <file name="a.vb">
MustInherit Class A
    Public MustOverride Sub F()
End Class
Class B
    Inherits A
    Public Overrides Sub F()
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedSignatures:=
            {
                Signature("B", "F", ".method public hidebysig strict virtual instance System.Void F() cil managed"),
                Signature("A", "F", ".method public newslot strict abstract virtual instance System.Void F() cil managed")
            })

            verifier.VerifyDiagnostics()
        End Sub

        <WorkItem(540884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540884")>
        <Fact>
        Public Sub TestMustOverrideOverrides()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class A
    Public Overridable Sub G()
        Console.WriteLine("A.G")
    End Sub
End Class
MustInherit Class B
    Inherits A
    Public MustOverride Overrides Sub G()
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerify(source, expectedSignatures:=
            {
                Signature("B", "G", ".method public hidebysig strict abstract virtual instance System.Void G() cil managed"),
                Signature("A", "G", ".method public newslot strict virtual instance System.Void G() cil managed")
            })

            verifier.VerifyDiagnostics()
        End Sub

        <WorkItem(542576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542576")>
        <Fact>
        Public Sub TestDontMergePartials()
            Dim source =
                <compilation>
                    <file name="a.vb">
MustInherit Class A
  MustOverride Function F() As Integer
  Overridable Sub G()
  End Sub
End Class

Partial Class B
  Inherits A

  'This would normally be an error if this partial part for class B
  'had the NotInheritable modifier (i.e. NotOverridable and NotInheritable
  'can't be combined). Strangely Dev10 doesn't report the same error
  'when the NotInheritable modifier appears on a different partial part.
  NotOverridable Overrides Function F() As Integer
    Return 1
  End Function

  'This would normally be an error if this partial part for class B
  'had the NotInheritable modifier (i.e. NotOverridable and NotInheritable
  'can't be combined). Strangely Dev10 doesn't report the same error
  'when the NotInheritable modifier appears on a different partial part.
  NotOverridable Overrides Sub G()
  End Sub
End Class</file>
                    <file name="b.vb">
NotInheritable Class B
  Inherits A
End Class
                    </file>
                </compilation>

            CompileAndVerify(source, expectedSignatures:=
            {
                Signature("B", "F", ".method public hidebysig strict virtual final instance System.Int32 F() cil managed"),
                Signature("A", "F", ".method public newslot strict abstract virtual instance System.Int32 F() cil managed"),
                Signature("B", "G", ".method public hidebysig strict virtual final instance System.Void G() cil managed"),
                Signature("A", "G", ".method public newslot strict virtual instance System.Void G() cil managed")
            }).
            VerifyDiagnostics()
        End Sub

        <WorkItem(543751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543751")>
        <Fact(), WorkItem(543751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543751")>
        Public Sub TestMustOverloadWithOptional()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Module Program
    Const str As String = ""
    Sub Main(args As String())
    End Sub
    Function fun()
        test(temp:=Nothing, x:=1)
        Return Nothing
    End Function
    Function test(ByRef x As Integer, temp As Object, Optional y As String = str, Optional z As Object = Nothing)
        Return Nothing
    End Function
    Function test(ByRef x As Integer, Optional temp As Object = Nothing)
        Return Nothing
    End Function
End Module
    </file>
                </compilation>).
            VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub CrossLanguageCase1()
            'Note: For this case Dev10 produces errors (see below) while Roslyn works fine. We believe this
            'is a bug in Dev10 that we fixed in Roslyn - the change is non-breaking.
            Dim vb1Compilation = CreateVisualBasicCompilation("VB1",
            <![CDATA[Public MustInherit Class C1
    MustOverride Sub foo()
End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim vb1Verifier = CompileAndVerify(vb1Compilation)
            vb1Verifier.VerifyDiagnostics()

            Dim cs1Compilation = CreateCSharpCompilation("CS1",
            <![CDATA[using System;
public abstract class C2 : C1
{
    new internal virtual void foo()
    {
        Console.WriteLine("C2");
    }
}]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation})
            cs1Compilation.VerifyDiagnostics()

            Dim vb2Compilation = CreateVisualBasicCompilation("VB2",
            <![CDATA[Imports System
Public Class C3 : Inherits C2
    Public Overrides Sub foo
        Console.WriteLine("C3")
    End Sub
End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation, cs1Compilation})
            Dim vb2Verifier = CompileAndVerify(vb2Compilation)
            vb2Verifier.VerifyDiagnostics()

            'Dev10 reports an error for the below compilation - Roslyn on the other hand allows this code to compile without errors.
            'VB3.vb(2) : error BC30610: Class 'C4' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
            'C1 : Public MustOverride Sub foo().
            'Public Class C4 : Inherits C3
            '             ~~
            Dim vb3Compilation = CreateVisualBasicCompilation("VB3",
            <![CDATA[
Imports System
Public Class C4 : Inherits C3
    End Class

    Public Class C5 : Inherits C2
        ' Corresponding case in C# results in PEVerify errors - See test 'CrossLanguageCase1' in CodeGenOverridingAndHiding.cs
        Public Overrides Sub foo()
            Console.WriteLine("C5")
        End Sub
    End Class

    Public Module Program
        Sub Main()
            Dim x As C1 = New C4
            x.foo()
            Dim y As C2 = New C5
            y.Foo()
        End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={vb1Compilation, cs1Compilation, vb2Compilation})

            Dim vb3Verifier = CompileAndVerify(vb3Compilation,
                expectedOutput:=<![CDATA[C3
C5]]>)
            vb3Verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub CrossLanguageCase2()
            'Note: For this case Dev10 produces errors (see below) while Roslyn works fine. We believe this
            'is a bug in Dev10 that we fixed in Roslyn - the change is non-breaking.
            Dim vb1Compilation = CreateVisualBasicCompilation("VB1",
            <![CDATA[Public MustInherit Class C1
        MustOverride Sub foo()
    End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim vb1Verifier = CompileAndVerify(vb1Compilation)
            vb1Verifier.VerifyDiagnostics()

            Dim cs1Compilation = CreateCSharpCompilation("CS1",
            <![CDATA[using System;
    [assembly:System.Runtime.CompilerServices.InternalsVisibleTo("VB3")]
    public abstract class C2 : C1
    {
        new internal virtual void foo()
        {
            Console.WriteLine("C2");
        }
    }]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation})
            cs1Compilation.VerifyDiagnostics()

            Dim vb2Compilation = CreateVisualBasicCompilation("VB2",
            <![CDATA[Imports System
    Public Class C3 : Inherits C2
        Public Overrides Sub foo
            Console.WriteLine("C3")
        End Sub
    End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation, cs1Compilation})
            Dim vb2Verifier = CompileAndVerify(vb2Compilation)
            vb2Verifier.VerifyDiagnostics()

            'Dev10 reports an error for the below compilation - Roslyn on the other hand allows this code to compile without errors.
            'VB3.vb(2) : error BC30610: Class 'C4' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
            'C1 : Public MustOverride Sub foo().
            'Public Class C4 : Inherits C3
            '             ~~
            Dim vb3Compilation = CreateVisualBasicCompilation("VB3",
            <![CDATA[Imports System
    Public Class C4 : Inherits C3
        Public Overrides Sub foo
            Console.WriteLine("C4")
        End Sub
    End Class

    Public Module Program
        Sub Main()
            Dim x As C1 = New C4
            x.foo
            Dim y As C2 = New C4
            y.foo
        End Sub
    End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={vb1Compilation, cs1Compilation, vb2Compilation})

            Dim vb3Verifier = CompileAndVerify(vb3Compilation,
                expectedOutput:=<![CDATA[C4
C2]]>)
            vb3Verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub CrossLanguageCase3()
            'Note: Dev10 and Roslyn produce identical errors for this case.
            Dim vb1Compilation = CreateVisualBasicCompilation("VB1",
            <![CDATA[Public MustInherit Class C1
        MustOverride Sub foo()
    End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim vb1Verifier = CompileAndVerify(vb1Compilation)
            vb1Verifier.VerifyDiagnostics()

            Dim cs1Compilation = CreateCSharpCompilation("CS1",
            <![CDATA[[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("VB3")]
    public abstract class C2 : C1
    {
        new internal virtual void foo()
        {
        }
    }]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation})
            cs1Compilation.VerifyDiagnostics()

            Dim vb2Compilation = CreateVisualBasicCompilation("VB2",
            <![CDATA[Public Class C3 : Inherits C2
        Public Overrides Sub foo
        End Sub
    End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation, cs1Compilation})
            Dim vb2Verifier = CompileAndVerify(vb2Compilation)
            vb2Verifier.VerifyDiagnostics()

            Dim vb3Compilation = CreateVisualBasicCompilation("VB3",
            <![CDATA[MustInherit Public Class C4 : Inherits C3
        Public Overrides Sub foo
        End Sub
    End Class

    Public Class C5 : Inherits C2
        Public Overrides Sub foo()
        End Sub
    End Class

    Public Class C6 : Inherits C2
        Friend Overrides Sub foo()
        End Sub
    End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation, cs1Compilation, vb2Compilation})
            vb3Compilation.AssertTheseDiagnostics(<expected>
BC30610: Class 'C5' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    C1: Public MustOverride Sub foo().
    Public Class C5 : Inherits C2
                 ~~
BC30266: 'Public Overrides Sub foo()' cannot override 'Friend Overridable Overloads Sub foo()' because they have different access levels.
        Public Overrides Sub foo()
                             ~~~
BC30610: Class 'C6' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    C1: Public MustOverride Sub foo().
    Public Class C6 : Inherits C2
                 ~~
                                                  </expected>)
        End Sub

        <WorkItem(543794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543794")>
        <Fact()>
        Public Sub CrossLanguageTest4()
            Dim vb1Compilation = CreateVisualBasicCompilation("VB1",
            <![CDATA[Public MustInherit Class C1
        MustOverride Sub foo()
    End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim vb1Verifier = CompileAndVerify(vb1Compilation)
            vb1Verifier.VerifyDiagnostics()

            Dim cs1Compilation = CreateCSharpCompilation("CS1",
            <![CDATA[[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("VB2")]
    public abstract class C2 : C1
    {
        new internal virtual void foo()
        {
        }
    }]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation})
            cs1Compilation.VerifyDiagnostics()

            Dim vb2Compilation = CreateVisualBasicCompilation("VB2",
            <![CDATA[MustInherit Public Class C3 : Inherits C2
        Friend Overrides Sub foo()
        End Sub
    End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={vb1Compilation, cs1Compilation})

            CompileAndVerify(vb2Compilation).VerifyDiagnostics()
        End Sub

        <Fact(), WorkItem(544536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544536")>
        Public Sub VBOverrideCsharpOptional()
            Dim cs1Compilation = CreateCSharpCompilation("CS1",
            <![CDATA[
public abstract class Trivia
{
  public abstract void Format(int i, int j = 2);
}
public class Whitespace : Trivia
{
  public override void Format(int i, int j) {}
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            cs1Compilation.VerifyDiagnostics()

            Dim vb2Compilation = CreateVisualBasicCompilation("VB2",
            <![CDATA[
MustInherit Class AbstractLineBreakTrivia
  Inherits Whitespace
  Public Overrides Sub Format(i As Integer, j As Integer)
  End Sub
End Class
 
Class AfterStatementTerminatorTokenTrivia
  Inherits AbstractLineBreakTrivia
End Class
]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={cs1Compilation})

            CompileAndVerify(vb2Compilation).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub VBOverrideCsharpOptional2()
            Dim cs1Compilation = CreateCSharpCompilation("CS1",
            <![CDATA[
public abstract class Trivia
{
  public abstract void Format(int i, int j = 2);
}
public class Whitespace : Trivia
{
  public override void Format(int i, int j) {}
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            cs1Compilation.VerifyDiagnostics()

            Dim vb2Compilation = CreateVisualBasicCompilation("VB2",
            <![CDATA[
MustInherit Class AbstractLineBreakTrivia
  Inherits Trivia
  Public Overrides Sub Format(i As Integer, j As Integer)
  End Sub
End Class
 
Class AfterStatementTerminatorTokenTrivia
  Inherits AbstractLineBreakTrivia
End Class
]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                referencedCompilations:={cs1Compilation})

            CompilationUtils.AssertTheseDiagnostics(vb2Compilation, <expected>
BC30308: 'Public Overrides Sub Format(i As Integer, j As Integer)' cannot override 'Public MustOverride Overloads Sub Format(i As Integer, [j As Integer = 2])' because they differ by optional parameters.
  Public Overrides Sub Format(i As Integer, j As Integer)
                       ~~~~~~
                                                               </expected>)
        End Sub

        <Fact()>
        Public Sub OverloadingBasedOnOptionalParameters()
            ' NOTE: this matches Dev11 implementation, not Dev10
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Class C ' allowed
    Shared Sub f(ByVal x As Integer)
    End Sub
    Shared Sub f(ByVal x As Integer, Optional ByVal y As Integer = 0)
    End Sub
    Shared Sub f(ByVal x As Integer, Optional ByVal s As String = "")
    End Sub
End Class

Class C2 ' allowed
    Shared Sub f(ByVal x As Integer, Optional ByVal y As Short = 1)
    End Sub
    Shared Sub f(ByVal x As Integer, Optional ByVal y As Integer = 1)
    End Sub
End Class

Class C3 ' allowed
    Shared Sub f()
    End Sub
    Shared Sub f(Optional ByVal x As Integer = 0)
    End Sub
End Class

Class C4 ' allowed`
    Shared Sub f(Optional ByVal x As Integer = 0)
    End Sub
    Shared Sub f(ByVal ParamArray xx As Integer())
    End Sub
End Class

Class C5 ' disallowed
    Shared Sub f(Optional ByVal x As Integer = 0)
    End Sub
    Shared Sub f(ByVal x As Integer)
    End Sub
End Class

Class C6 ' disallowed
    Shared Sub f(Optional ByVal x As Integer() = Nothing)
    End Sub
    Shared Sub f(ByVal ParamArray xx As Integer())
    End Sub
End Class

Class C7 ' disallowed
    Shared Sub f(Optional ByVal x As Integer = 0)
    End Sub
    Shared Sub f(ByRef x As Integer)
    End Sub
End Class
        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30300: 'Public Shared Sub f([x As Integer = 0])' and 'Public Shared Sub f(x As Integer)' cannot overload each other because they differ only by optional parameters.
    Shared Sub f(Optional ByVal x As Integer = 0)
               ~
BC30300: 'Public Shared Sub f([x As Integer() = Nothing])' and 'Public Shared Sub f(ParamArray xx As Integer())' cannot overload each other because they differ only by optional parameters.
    Shared Sub f(Optional ByVal x As Integer() = Nothing)
               ~
BC30368: 'Public Shared Sub f([x As Integer() = Nothing])' and 'Public Shared Sub f(ParamArray xx As Integer())' cannot overload each other because they differ only by parameters declared 'ParamArray'.
    Shared Sub f(Optional ByVal x As Integer() = Nothing)
               ~
BC30300: 'Public Shared Sub f([x As Integer = 0])' and 'Public Shared Sub f(ByRef x As Integer)' cannot overload each other because they differ only by optional parameters.
    Shared Sub f(Optional ByVal x As Integer = 0)
               ~
BC30345: 'Public Shared Sub f([x As Integer = 0])' and 'Public Shared Sub f(ByRef x As Integer)' cannot overload each other because they differ only by parameters declared 'ByRef' or 'ByVal'.
    Shared Sub f(Optional ByVal x As Integer = 0)
               ~
</errors>)
        End Sub

        <Fact()>
        Public Sub HidingBySignatureWithOptionalParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System
Class A
    Public Overridable Sub f(Optional x As String = "")
        Console.WriteLine("A::f(Optional x As String = """")")
    End Sub
End Class

Class B
    Inherits A
    Public Overridable Overloads Sub f()
        Console.WriteLine("B::f()")
    End Sub
End Class

Class C
    Inherits B
    Public Sub f(Optional x As String = "")
        Console.WriteLine("C::f(Optional x As String = """")")
    End Sub
    Public Shared Sub Main()
        Dim c As B = New C
        c.f()
        c.f(1)
    End Sub
End Class
        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC40005: sub 'f' shadows an overridable method in the base class 'B'. To override the base method, this method must be declared 'Overrides'.
    Public Sub f(Optional x As String = "")
               ~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC31404ForOverloadingBasedOnOptionalParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
MustInherit Class A
    Public MustOverride Sub f(Optional x As String = "")
End Class

MustInherit Class B1
    Inherits A
    Public MustOverride Overloads Sub f(Optional x As String = "")
End Class

MustInherit Class B2
    Inherits A
    Public MustOverride Overloads Sub f(x As String)
End Class

MustInherit Class B3
    Inherits A
    Public MustOverride Overloads Sub f(x As Integer, Optional y As String = "")
End Class

MustInherit Class B4
    Inherits A
    Public MustOverride Overloads Sub f()
End Class
        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31404: 'Public MustOverride Overloads Sub f([x As String = ""])' cannot shadow a method declared 'MustOverride'.
    Public MustOverride Overloads Sub f(Optional x As String = "")
                                      ~
BC31404: 'Public MustOverride Overloads Sub f(x As String)' cannot shadow a method declared 'MustOverride'.
    Public MustOverride Overloads Sub f(x As String)
                                      ~
</errors>)
        End Sub

        <Fact()>
        Public Sub OverloadingWithNotAccessibleMethods()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Class A
    Public Overridable Sub f(Optional x As String = "")
    End Sub
End Class

Class B
    Inherits A
    Public Overridable Overloads Sub f()
    End Sub
End Class

Class BB
    Inherits A
    Private Overloads Sub f()
    End Sub
    Private Overloads Sub f(Optional x As String = "")
    End Sub
End Class

Class C
    Inherits BB
    Public Overloads Overrides Sub f(Optional x As String = "")
        Console.Write("f(Optional x As String = "");")
    End Sub
End Class
        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact()>
        Public Sub AddressOfWithFunctionOrSub1()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Class Clazz
    Public Shared Sub S(Optional x As Integer = 0)
        Console.WriteLine("Sub S")
    End Sub
    Public Shared Function S() As Boolean
        Console.WriteLine("Function S")
        Return True
    End Function
    Public Shared Sub Main()
        Dim a As action = AddressOf S
        a()
    End Sub
End Class
        </file>
    </compilation>, expectedOutput:="Function S")
        End Sub

        <Fact, WorkItem(546816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546816")>
        Public Sub OverrideFinalizeWithoutNewslot()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Class SelfDestruct
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class
                    </file>
                </compilation>,
                {MscorlibRef_v20}).VerifyDiagnostics()
        End Sub
    End Class
End Namespace
