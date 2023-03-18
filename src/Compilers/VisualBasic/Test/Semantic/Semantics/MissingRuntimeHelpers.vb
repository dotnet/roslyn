' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class MissingRuntimeHelpers
        Inherits BasicTestBase

        <Fact>
        Public Sub SpecialMembers()

            Dim compilationDef =
<compilation name="SpecialMembers">
    <file name="a.vb">
Class Program
  Sub Main()
    Dim Ob As Object = Nothing
    Dim St As String = Nothing
    Dim [Do] as Double = 0
    Dim Da as Date = Nothing
    Dim De as Decimal = Nothing
    Dim ChArray As Char() = Nothing

    Test([Do] ^ [Do])
    Test(Da > Da)
    Test(De > De)
    Test(De + De)
    Test(St + St)
    Test(-De)
    Test(CType(ChArray, String))
    Test(CDec([Do]))
    Test(CInt(De))
    Test(CInt([Do]))
  End Sub

  Sub Test(x As Object)
  End Sub
End Class

Namespace System
    Public Class [Object]
    End Class

    Public Class [String]
    End Class

    Public Class Array
    End Class

    Public Structure Void
    End Structure

    Public Structure [Double]
    End Structure

    Public Structure Int32
    End Structure

    Public Structure [Boolean]
    End Structure

    Public Structure DateTime
    End Structure

    Public Structure [Decimal]
    End Structure

    Public Structure [Char]
    End Structure

    Public Class ValueType
    End Class
End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, New MetadataReference() {})

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.DateTime.New' is not defined.
    Dim Da as Date = Nothing
                     ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Decimal.New' is not defined.
    Dim De as Decimal = Nothing
                        ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Math.Pow' is not defined.
    Test([Do] ^ [Do])
         ~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.DateTime.Compare' is not defined.
    Test(Da > Da)
         ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Decimal.Compare' is not defined.
    Test(De > De)
         ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Decimal.Add' is not defined.
    Test(De + De)
         ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.String.Concat' is not defined.
    Test(St + St)
         ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Decimal.Negate' is not defined.
    Test(-De)
         ~~~
BC35000: Requested operation is not available because the runtime library function 'System.String.New' is not defined.
    Test(CType(ChArray, String))
         ~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Decimal.New' is not defined.
    Test(CDec([Do]))
         ~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Convert.ToInt32' is not defined.
    Test(CInt(De))
         ~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Math.Round' is not defined.
    Test(CInt([Do]))
         ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub WellKnownMembers()
            Dim compilationDef =
<compilation name="WellKnownMembers">
    <file name="a.vb">
Class Program
  Sub Main()
    Dim Ob As Object = Nothing
    Dim St as String = Nothing
    Dim [Do] as Double = 0
    Dim Bo As Boolean = false

    Test(St > St)
    Test(Ob AndAlso Ob)
    Test(Ob + Ob)
    Test(St Like St)
    Test(Ob > Ob)
    Test(Not Ob)
    Test(CType(Ob, Char()))
    Test(CLng(Ob))
    Test(CStr([Do]))
    Test(CInt(St))
    Test(CDec(Bo))
  End Sub

  Sub Test(x As Object)
  End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Operators.CompareString' is not defined.
    Test(St > St)
         ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean' is not defined.
    Test(Ob AndAlso Ob)
         ~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Operators.AddObject' is not defined.
    Test(Ob + Ob)
         ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString' is not defined.
    Test(St Like St)
         ~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectGreater' is not defined.
    Test(Ob > Ob)
         ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Operators.NotObject' is not defined.
    Test(Not Ob)
         ~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToCharArrayRankOne' is not defined.
    Test(CType(Ob, Char()))
         ~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToLong' is not defined.
    Test(CLng(Ob))
         ~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToString' is not defined.
    Test(CStr([Do]))
         ~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger' is not defined.
    Test(CInt(St))
         ~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToDecimal' is not defined.
    Test(CDec(Bo))
         ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub MissingCompareExchange()

            Dim compilationDef =
<compilation name="SpecialMembers">
    <file name="a.vb">

Delegate Sub E1()
Class Program
  public event e as E1

  public shared Sub Main()
     dim v as new Program
     AddHandler v.e, AddressOf Main
  End Sub  
End Class

Namespace System
    Public Class [Object]
    End Class

    Public Class [String]
    End Class

    Public Class Array
    End Class

    Public Structure Void
    End Structure

    Public Structure [Double]
    End Structure

    Public Structure Int32
    End Structure

    Public Structure [Boolean]
    End Structure

    Public Structure DateTime
    End Structure

    Public Structure [Decimal]
    End Structure

    Public Structure [Char]
    End Structure

    Public Structure IntPtr
    End Structure

    Public Class ValueType
    End Class

    Public Class [Delegate]
    End Class

    Public Class MulticastDelegate
    End Class
End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, New MetadataReference() {})

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<expected>
BC30002: Type 'System.AsyncCallback' is not defined.
Delegate Sub E1()
~~~~~~~~~~~~~~~~~
BC30002: Type 'System.IAsyncResult' is not defined.
Delegate Sub E1()
~~~~~~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseCompileDiagnostics(compilation,
<expected>
BC30002: Type 'System.AsyncCallback' is not defined.
Delegate Sub E1()
~~~~~~~~~~~~~~~~~
BC30002: Type 'System.IAsyncResult' is not defined.
Delegate Sub E1()
~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Delegate.Combine' is not defined.
  public event e as E1
  ~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Delegate.Remove' is not defined.
  public event e as E1
  ~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub MalformedSystemArray()

            Dim compilationDef =
<compilation name="SpecialMembers">
    <file name="a.vb">

Class Program
  public shared Sub Main()
    dim x() as integer = nothing

    for each e in x
    next

  End Sub  
End Class

Namespace System
    Public Class [Object]
    End Class

    Public Class [String]
    End Class

    Public Class Array
        Public Readonly Property Length as integer
            Get
                return 0
            end get
        end property
    End Class

    Public Structure Void
    End Structure

    Public Structure [Double]
    End Structure

    Public Structure Int32
    End Structure

    Public Structure [Boolean]
    End Structure

    Public Structure DateTime
    End Structure

    Public Structure [Decimal]
    End Structure

    Public Structure [Char]
    End Structure

    Public Structure IntPtr
    End Structure

    Public Class ValueType
    End Class
End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, New MetadataReference() {})

            CompilationUtils.AssertTheseCompileDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Integer()', which is not a collection type.
    for each e in x
                  ~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Integer()', which is not a collection type.
    for each e in x
                  ~
</expected>)

        End Sub

    End Class
End Namespace
