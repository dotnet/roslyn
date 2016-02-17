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
    Public Class BindingCollectionInitializerTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub CollectionInitializerList()
            Dim source =
<compilation name="CollectionInitializerList">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New List(Of String) From {"Hello World!"}        
        Console.WriteLine(c(0))
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, "Hello World!")
        End Sub

        <Fact()>
        Public Sub CollectionInitializerListEachElementAsCollectionInitializer()
            Dim source =
<compilation name="CollectionInitializerListEachElementAsCollectionInitializer">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New List(Of String) From {{"Hello"}, {" "}, {"World!"}}

        For each element in c
            Console.Write(element)
        next element
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, "Hello World!")
        End Sub

        <Fact()>
        Public Sub CollectionInitializerDictionary()
            Dim source =
<compilation name="CollectionInitializerDictionary">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New Dictionary(Of String, Integer) From {{"Hello", 23}, {"World", 42}}

        For Each keyValue In c
            Console.WriteLine(keyValue.Key + " " + keyValue.Value.ToString)
        Next

    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello 23
World 42
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerCustomCollection()
            Dim source =
<compilation name="CollectionInitializerCustomCollection">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class Custom
    Private list As New List(Of String)()

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(p As String)
        list.Add(p)
    End Sub

    Public Class CustomEnumerator
        Private list As list(Of String)
        Private index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If Me.index &lt; Me.list.Count - 1 Then
                index = index + 1
                Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Class
End Class

    Class C1
        Public Shared Sub Main()
            Dim c as Custom = New Custom() From {"Hello", " ", "World"} 
            Output(c)
        End Sub

        Public Shared Sub Output(c as custom)
            For Each value In c
                Console.Write(value)
            Next
        End Sub
    End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  3
  IL_0000:  newobj     "Sub Custom..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "Hello"
  IL_000b:  callvirt   "Sub Custom.add(String)"
  IL_0010:  dup
  IL_0011:  ldstr      " "
  IL_0016:  callvirt   "Sub Custom.add(String)"
  IL_001b:  dup
  IL_001c:  ldstr      "World"
  IL_0021:  callvirt   "Sub Custom.add(String)"
  IL_0026:  call       "Sub C1.Output(Custom)"
  IL_002b:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerEmptyInitializers()
            Dim source =
<compilation name="CollectionInitializerEmptyInitializers">
    <file name="a.vb">
Option Strict On

Imports System.Collections.Generic

Class C2
End Class

Class C1
    Public Shared Sub Main()
        ' ok
        Dim a as new List(Of Integer) From {}

        ' not ok
        Dim b as new List(Of Integer) From {{}}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC36721: An aggregate collection initializer entry must contain at least one element.
        Dim b as new List(Of Integer) From {{}}
                                            ~~                                               
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerNotACollection()
            Dim source =
    <compilation name="CollectionInitializerNotACollection">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New C1() From {"Hello World!"}        
    End Sub
End Class        
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            AssertTheseDiagnostics(compilation, <expected>
BC36718: Cannot initialize the type 'C1' with a collection initializer because it is not a collection type.
        Dim c As New C1() From {"Hello World!"}        
                          ~~~~~~~~~~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerCannotCombineBothInitializers()
            Dim source =
    <compilation name="CollectionInitializerCannotCombineBothInitializers">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public a as string

    Public Sub Add(p as string)
    End Sub
End Class

Class C1
    public a as string

    Public Shared Sub Main()
        Dim a As New C2() with {.a = "foo"} From {"Hello World!"}
        Dim b As New C2() From {"Hello World!"} with {.a = "foo"}
        Dim c As C2 = New C2() From {"Hello World!"} with {.a = "foo"}
        Dim d As C2 = New C2() with {.a = "foo"} From {"Hello World!"} 
    End Sub
End Class        
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim a As New C2() with {.a = "foo"} From {"Hello World!"}
                                            ~~~~
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim b As New C2() From {"Hello World!"} with {.a = "foo"}
                                                ~~~~
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim c As C2 = New C2() From {"Hello World!"} with {.a = "foo"}
                               ~~~~~~~~~~~~~~~~~~~~~
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim d As C2 = New C2() with {.a = "foo"} From {"Hello World!"} 
                               ~~~~~~~~~~~~~~~~~                                                   
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerNoAddMethod()
            Dim source =
    <compilation name="CollectionInitializerNoAddMethod">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C3
    inherits C2

    Protected Sub Add()
    End Sub
End Class

Class C4
    inherits C2

    Public Property Add() as string
End Class

Class C5
    inherits C2

    Public Add as String
End Class

Class C1
    public a as string

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!"}
        Dim b As New C3() From {"Hello World!"}
        Dim c As New C4() From {"Hello World!"}
        Dim d As New C5() From {"Hello World!"}
    End Sub
End Class        
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC36719: Cannot initialize the type 'C2' with a collection initializer because it does not have an accessible 'Add' method.
        Dim a As New C2() From {"Hello World!"}
                          ~~~~~~~~~~~~~~~~~~~~~
BC36719: Cannot initialize the type 'C3' with a collection initializer because it does not have an accessible 'Add' method.
        Dim b As New C3() From {"Hello World!"}
                          ~~~~~~~~~~~~~~~~~~~~~
BC36719: Cannot initialize the type 'C4' with a collection initializer because it does not have an accessible 'Add' method.
        Dim c As New C4() From {"Hello World!"}
                          ~~~~~~~~~~~~~~~~~~~~~
BC36719: Cannot initialize the type 'C5' with a collection initializer because it does not have an accessible 'Add' method.
        Dim d As New C5() From {"Hello World!"}
                          ~~~~~~~~~~~~~~~~~~~~~                                       
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerAddMethodIsFunction()
            Dim source =
    <compilation name="CollectionInitializerAddMethodIsFunction">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Public Class C1
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public Function Add(p As Integer) As String
        Console.WriteLine("What's the point of returning something here?")
        return "Boo!"
    End Function
End Class

Class C2
    Public Shared Sub Main()
        Dim x As New C1() From {1}
    End Sub
End Class
    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
What's the point of returning something here?
 ]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerOverloadResolutionErrors()
            Dim source =
    <compilation name="CollectionInitializerOverloadResolutionErrors">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public Sub Add()
    End Sub

    Protected Sub Add(p as string)
    End Sub
End Class

Class C3
    inherits C2
    
    ' first argument matches
    Public overloads Sub Add(p as string, q as integer)
    End Sub
End Class

Class C4
    inherits C2
    
    ' first argument does not match -> multiple candidates
    Public overloads Sub Add(p as integer, q as string)
    End Sub
End Class

Class C5
    inherits C2
    
    ' first argument does not match -> multiple candidates
    Public overloads Sub Add(p as Byte)
    End Sub
End Class

Class C1
    public a as string

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
        Dim b As New C3() From {"Hello World!"}
        Dim c As New C4() From {"Hello World!"}
        Dim d As New C5() From {300%}
    End Sub
End Class        
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30057: Too many arguments to 'Public Sub Add()'.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
                                ~~~~~~~~~~~~~~
BC30057: Too many arguments to 'Public Sub Add()'.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
                                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Add' accepts this number of arguments.
        Dim b As New C3() From {"Hello World!"}
                                ~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Add' accepts this number of arguments.
        Dim c As New C4() From {"Hello World!"}
                                ~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Byte'.
        Dim d As New C5() From {300%}
                                ~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerWarningsWillBeKept()
            Dim source =
    <compilation name="CollectionInitializerWarningsWillBeKept">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public Shared Sub Add(p as string)
    End Sub
End Class

Class C1
    public a as string

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
    End Sub
End Class        
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
                                ~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
                                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerExtensionMethodsAreSupported()
            Dim source =
    <compilation name="CollectionInitializerExtensionMethodsAreSupported">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C1
    public a as string

    Public Shared Sub Main()
        ' extensions for custom type
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}

        ' extensions for predefined type
        Dim x0 As LinkedList(Of Integer) = New LinkedList(Of Integer) From {1, 2, 3}
    End Sub
End Class        

Module C2Extensions
    &lt;Extension()&gt;
    Public Sub Add(this as C2, p as string)
    End Sub

    &lt;Extension()&gt;
    Public Sub ADD(ByRef x As LinkedList(Of Integer), ByVal y As Integer)
        x.AddLast(y)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
                                                </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerExtensionMethodsAreSupportedForValueTypes()
            Dim source =
    <compilation name="CollectionInitializerExtensionMethodsAreSupportedForValueTypes">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Structure C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Structure

Class C1
    public a as string

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
    End Sub
End Class        

Module C2Extensions
    &lt;Extension()&gt;
    Public Sub Add(this as C2, p as string)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
                                                </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerTypeConstraintsAreSupported()
            Dim source =
    <compilation name="CollectionInitializerTypeConstraintsAreSupported">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Interface IAdd(Of T)
    Sub Add(p As T)
End Interface

Public Class C2
    Public Sub Add()
    End Sub
End Class

Class C3
    Implements IAdd(Of String), ICollection

    private mylist as new list(of String)()

    Public Sub New()
    End Sub

    Public Sub Add1(p As String) Implements IAdd(Of String).Add
        mylist.add(p)
    End Sub

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return False
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return mylist.getenumerator
    End Function
End Class

Class C1
    Public Shared Sub DoStuff(Of T As {IAdd(Of String), ICollection, New})()
        Dim a As New T() From {"Hello", " ", "World!"}

        for each str as string in a
            Console.Write(str)
        next str
    End Sub

    Public Shared Sub Main()
        DoStuff(Of C3)()
    End Sub
End Class 
    </file>
    </compilation>

            CompileAndVerify(source, "Hello World!")
        End Sub

        <Fact()>
        Public Sub CollectionInitializerTypeConstraintsAndAmbiguity()
            Dim source =
    <compilation name="CollectionInitializerTypeConstraintsAndAmbiguity">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Interface IAdd(Of T)
    Sub Add(p As String)
End Interface

Class C1
    Public Shared Sub DoStuff(Of T As {IAdd(Of String), IAdd(Of Integer), ICollection, New})()
        Dim a As New T() From {"Hello", " ", "World!"}

        for each str as string in a
            Console.Write(str)
        next str
    End Sub

    Public Shared Sub Main()
    End Sub
End Class 
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30521: Overload resolution failed because no accessible 'Add' is most specific for these arguments:
    'Sub IAdd(Of String).Add(p As String)': Not most specific.
    'Sub IAdd(Of Integer).Add(p As String)': Not most specific.
        Dim a As New T() From {"Hello", " ", "World!"}
                               ~~~~~~~
BC30521: Overload resolution failed because no accessible 'Add' is most specific for these arguments:
    'Sub IAdd(Of String).Add(p As String)': Not most specific.
    'Sub IAdd(Of Integer).Add(p As String)': Not most specific.
        Dim a As New T() From {"Hello", " ", "World!"}
                                        ~~~
BC30521: Overload resolution failed because no accessible 'Add' is most specific for these arguments:
    'Sub IAdd(Of String).Add(p As String)': Not most specific.
    'Sub IAdd(Of Integer).Add(p As String)': Not most specific.
        Dim a As New T() From {"Hello", " ", "World!"}
                                             ~~~~~~~~
                                                </expected>)
        End Sub

        <WorkItem(529265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529265")>
        <Fact()>
        Public Sub CollectionInitializerCollectionInitializerArityCheck()
            Dim source =
    <compilation name="CollectionInitializerCollectionInitializerArityCheck">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim x As New Dictionary(Of String, Integer) from {{1}}
    End Sub
End Class 
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30455: Argument not specified for parameter 'value' of 'Public Overloads Sub Add(key As String, value As Integer)'.
        Dim x As New Dictionary(Of String, Integer) from {{1}}
                                                          ~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim x As New Dictionary(Of String, Integer) from {{1}}
                                                           ~                                                   
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerReferencingItself()
            Dim source =
<compilation name="CollectionInitializerReferencingItselfRefType">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Collections

Interface IMissingStuff
    Sub Add(p As String)
    Function Item() As String
End Interface

Structure Custom
    Implements IMissingStuff, IEnumerable(Of String)

    Public Shared list As New List(Of String)()

    Public Sub Add(p As String) Implements IMissingStuff.Add
        list.Add(p)
    End Sub

    Public Function Item() As String Implements IMissingStuff.Item
        Return Nothing
    End Function

    Public Structure CustomEnumerator
        Implements IEnumerator(Of String)

        Private list As List(Of String)
        Private Shared index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If index &lt; Me.list.Count - 1 Then
                index = index + 1
            Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property

        Public ReadOnly Property Current1 As String Implements IEnumerator(Of String).Current
            Get
                Return Current
            End Get
        End Property

        Public ReadOnly Property Current2 As Object Implements IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Function MoveNext1() As Boolean Implements IEnumerator.MoveNext
            Return MoveNext()
        End Function

        Public Sub Reset() Implements IEnumerator.Reset

        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose

        End Sub
    end structure

    Public Function GetEnumerator1() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New CustomEnumerator(list)
    End Function
End Structure

Structure CustomNonEmpty
    Implements IMissingStuff, IEnumerable(Of String)

    Public MakeItNonEmpty as String

    Public Shared list As New List(Of String)()

    Public Sub Add(p As String) Implements IMissingStuff.Add
        list.Add(p)
    End Sub

    Public Function Item() As String Implements IMissingStuff.Item
        Return Nothing
    End Function

    Public Structure CustomEnumerator
        Implements IEnumerator(Of String)

        Private list As List(Of String)
        Private Shared index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If index &lt; Me.list.Count - 1 Then
                index = index + 1
            Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property

        Public ReadOnly Property Current1 As String Implements IEnumerator(Of String).Current
            Get
                Return Current
            End Get
        End Property

        Public ReadOnly Property Current2 As Object Implements IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Function MoveNext1() As Boolean Implements IEnumerator.MoveNext
            Return MoveNext()
        End Function

        Public Sub Reset() Implements IEnumerator.Reset

        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose

        End Sub
    end structure

    Public Function GetEnumerator1() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New CustomEnumerator(list)
    End Function
End Structure

Class CBase(Of T)
    Public Overridable Sub TypeParameterValueTypeAsClassConstraint(Of U As {T, IEnumerable, IMissingStuff})()
    End Sub
End Class

Class CDerived
    Inherits CBase(Of Custom)

    Public Overrides Sub TypeParameterValueTypeAsClassConstraint(Of U As {Custom, IEnumerable, IMissingStuff})()
        Dim m As New U From {"Hello World!", m.Item(0)}                                             ' temp used, m is uninitialized, show warning
        Dim n As U = New U() From {"Hello World!", n.Item(0)}                                       ' temp used, h is uninitialized, show warning
        Dim o, p As New U() From {o.Item(0), p.Item(0)}                                             ' temps used, show warnings (although o is initialized when initializing p)
    End Sub
End Class

        Class C1
            Public Sub TypeParameterNotDefined(Of T As {IEnumerable, IMissingStuff, New})()
                ' no warnings from type parameters as well
                Dim e As New T From {"Hello World!", e.Item(0)}                                     ' Receiver type unknown, no warning
                Dim f As T = New T() From {"Hello World!", f.Item(0)}                               ' Receiver type unknown, no warning
            End Sub

            Public Sub TypeParameterAsStructure(Of T As {Structure, IEnumerable, IMissingStuff})()
                ' no warnings from type parameters as well
                Dim g As New T From {"Hello World!", g.Item(0)}                                     ' temp used, g is uninitialized, show warning
                Dim h As T = New T() From {"Hello World!", h.Item(0)}                               ' temp used, h is uninitialized, show warning
                Dim i, j As New T() From {i.Item(0), j.Item(0)}                                     ' temps used, show warnings (although i is initialized when initializing j)
            End Sub

            Public Sub TypeParameterAsRefType(Of T As {List(Of String), new})()
                Dim k As New T From {"Hello World!", k.Item(0)}                                     ' temp used, k is uninitialized, show warning
                Dim l As T = New T() From {"Hello World!", l.Item(0)}                               ' temp used, l is uninitialized, show warning
            End Sub

            Public Shared Sub Main()
                Dim a As New Custom From {"Hello World!", a.Item(0)}                                ' empty, non trackable structure, no warning
                Dim b As Custom = New Custom() From {"Hello World!", b.Item(0)}                     ' empty, non trackable structure, no warning

                Dim q As New CustomNonEmpty From {"Hello World!", q.Item(0)}                        ' temp used, q is uninitialized, show warning
                Dim r As CustomNonEmpty = New CustomNonEmpty() From {"Hello World!", r.Item(0)}     ' temp used, r is uninitialized, show warning

                ' reference types are not ok, they are still Nothing
                Dim c As New List(Of String) From {"Hello World!", c.Item(0)}                       ' show warning
                Dim d As List(Of String) = New List(Of String)() From {"Hello World!", d.Item(0)}   ' show warning

                ' was already assigned, no warning again.
                c = New List(Of String)() From {"Hello World!", c.Item(0)}                          ' no warning
            End Sub
        End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42109: Variable 'm' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim m As New U From {"Hello World!", m.Item(0)}                                             ' temp used, m is uninitialized, show warning
                                             ~
BC42109: Variable 'n' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim n As U = New U() From {"Hello World!", n.Item(0)}                                       ' temp used, h is uninitialized, show warning
                                                   ~
BC42109: Variable 'o' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim o, p As New U() From {o.Item(0), p.Item(0)}                                             ' temps used, show warnings (although o is initialized when initializing p)
                                  ~
BC42109: Variable 'p' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim o, p As New U() From {o.Item(0), p.Item(0)}                                             ' temps used, show warnings (although o is initialized when initializing p)
                                             ~
BC42109: Variable 'g' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim g As New T From {"Hello World!", g.Item(0)}                                     ' temp used, g is uninitialized, show warning
                                                     ~
BC42109: Variable 'h' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim h As T = New T() From {"Hello World!", h.Item(0)}                               ' temp used, h is uninitialized, show warning
                                                           ~
BC42109: Variable 'i' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim i, j As New T() From {i.Item(0), j.Item(0)}                                     ' temps used, show warnings (although i is initialized when initializing j)
                                          ~
BC42109: Variable 'j' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim i, j As New T() From {i.Item(0), j.Item(0)}                                     ' temps used, show warnings (although i is initialized when initializing j)
                                                     ~
BC42104: Variable 'k' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim k As New T From {"Hello World!", k.Item(0)}                                     ' temp used, k is uninitialized, show warning
                                                     ~
BC42104: Variable 'l' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim l As T = New T() From {"Hello World!", l.Item(0)}                               ' temp used, l is uninitialized, show warning
                                                           ~
BC42109: Variable 'q' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim q As New CustomNonEmpty From {"Hello World!", q.Item(0)}                        ' temp used, q is uninitialized, show warning
                                                                  ~
BC42109: Variable 'r' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim r As CustomNonEmpty = New CustomNonEmpty() From {"Hello World!", r.Item(0)}     ' temp used, r is uninitialized, show warning
                                                                                     ~
BC42104: Variable 'c' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim c As New List(Of String) From {"Hello World!", c.Item(0)}                       ' show warning
                                                                   ~
BC42104: Variable 'd' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim d As List(Of String) = New List(Of String)() From {"Hello World!", d.Item(0)}   ' show warning
                                                                                       ~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerReferencingItself_2()
            Dim source =
    <compilation name="CollectionInitializerReferencingItself_2">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        Dim x, y As New List(Of String)() From {"1", x.Item(0)}
        Dim z As New List(Of String)() From {"1", z.Item(0)}
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim x, y As New List(Of String)() From {"1", x.Item(0)}
                                                     ~
BC42104: Variable 'z' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim z As New List(Of String)() From {"1", z.Item(0)}
                                                  ~                                                   
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerCustomCollectionOptionalParameter()
            Dim source =
<compilation name="CollectionInitializerCustomCollection">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class Custom
    Private list As New List(Of String)()

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(p As String, optional p2 as String = " ")
        list.Add(p)
        list.Add(p2)
    End Sub

    Public Class CustomEnumerator
        Private list As list(Of String)
        Private index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If Me.index &lt; Me.list.Count - 1 Then
                index = index + 1
                Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Class
End Class

    Class C1
        Public Shared Sub Main()
            Dim c as Custom = New Custom() From {"Hello", {"World", "!"}} 
            Output(c)
        End Sub

        Public Shared Sub Output(c as custom)
            For Each value In c
                Console.Write(value)
            Next
        End Sub
    End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerCustomCollectionParamArray()
            Dim source =
<compilation name="CollectionInitializerCustomCollection">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class Custom
    Private list As New List(Of String)()

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(paramarray p() As String)
        list.AddRange(p)
    End Sub

    Public Class CustomEnumerator
        Private list As list(Of String)
        Private index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If Me.index &lt; Me.list.Count - 1 Then
                index = index + 1
                Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Class
End Class

    Class C1
        Public Shared Sub Main()
            Dim c as Custom = New Custom() From {"Hello", {" ", "World"}, ({"!", "!", "!"})} 
            Output(c)
        End Sub

        Public Shared Sub Output(c as custom)
            For Each value In c
                Console.Write(value)
            Next
        End Sub
    End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!!!
]]>)
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_01()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As Integer)
    End Sub

    Sub Add(x As String)
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { String.Empty, 'BIND1:"String.Empty"
                                12}          'BIND2:"12"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            If True Then
                Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

                Assert.NotNull(symbolInfo.Symbol)
                Assert.Equal("Sub X.Add(x As System.String)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            End If

            If True Then
                Dim node2 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node2)

                Assert.NotNull(symbolInfo.Symbol)
                Assert.Equal("Sub X.Add(x As System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            End If
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_02()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As X)
    End Sub

    Sub Add(x As List(Of Byte))
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { String.Empty } 'BIND1:"String.Empty"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length)
            Assert.Equal({"Sub X.Add(x As System.Collections.Generic.List(Of System.Byte))",
                          "Sub X.Add(x As X)"},
                         symbolInfo.CandidateSymbols.Select(Function(s) s.ToTestDisplayString()).Order().ToArray())
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_03()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
Class Base
    Implements IEnumerable(Of Integer)
End Class

class X 
    Inherits Base

    Protected Sub Add(x As String)
    End Sub
End Class

class Y
    Shared Sub Main()
        Dim z = new X() From { String.Empty } 'BIND1:"String.Empty"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_04()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As String, y As Integer)
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { {String.Empty, 12} } 'BIND1:"{String.Empty, 12}"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal("Sub X.Add(x As System.String, y As System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_05()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As String, y As Integer)
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { {String.Empty, 'BIND1:"String.Empty"
                                12} }         'BIND2:"12"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            For i As Integer = 1 To 2
                Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", i)
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Next
        End Sub

    End Class
End Namespace
