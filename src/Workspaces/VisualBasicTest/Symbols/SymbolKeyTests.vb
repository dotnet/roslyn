Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.UnitTests.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
    Public Class SymbolKeyTests
        Inherits SymbolKeyTestBase

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestNamespace() As Task
            Const code = "
Namespace N1
    Namespace $$N2
    End Namespace
End Namespace
"
            Const expected = "(N ""N2"" 0 (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of NamespaceStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestModule() As Task
            Const code = "
Namespace N1
    Module $$M1
    End Module
End Namespace
"
            Const expected = "(D ""M1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 8 0 ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of ModuleStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestClass() As Task
            Const code = "
Namespace N1
    Class $$C1
    End Class
End Namespace
"
            Const expected = "(D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 2 0 ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of ClassStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestClassTypeParameter() As Task
            Const code = "
Namespace N1
    Class C1(Of $$T)
    End Class
End Namespace
"
            Const expected = "(Y ""T"" (D ""C1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 2 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of TypeParameterSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestStructure() As Task
            Const code = "
Namespace N1
    Structure $$S1
    End Structure
End Namespace
"
            Const expected = "(D ""S1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 10 0 ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of StructureStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestStructureTypeParameter() As Task
            Const code = "
Namespace N1
    Structure S1(Of $$T)
    End Structure
End Namespace
"
            Const expected = "(Y ""T"" (D ""S1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 10 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of TypeParameterSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestInterface() As Task
            Const code = "
Namespace N1
    Interface $$I1
    End Interface
End Namespace
"
            Const expected = "(D ""I1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 7 0 ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of InterfaceStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestInterfaceTypeParameter() As Task
            Const code = "
Namespace N1
    Interface I1(Of $$T)
    End Interface
End Namespace
"
            Const expected = "(Y ""T"" (D ""I1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 7 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of TypeParameterSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestDelegate() As Task
            Const code = "
Namespace N1
    Delegate Sub $$D1()
End Namespace
"
            Const expected = "(D ""D1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 3 0 ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of DelegateStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestDelegateTypeParameter() As Task
            Const code = "
Namespace N1
    Delegate Sub D1(Of $$T)()
End Namespace
"
            Const expected = "(Y ""T"" (D ""D1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 3 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of TypeParameterSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestEnum() As Task
            Const code = "
Namespace N1
    Enum $$E1
    End Enum
End Namespace
"
            Const expected = "(D ""E1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) 0 5 0 ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of EnumStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestEnumMember() As Task
            Const code = "
Namespace N1
    Enum E1
        One
        $$Two
        Three
    End Enum
End Namespace
"
            Const expected = "(F ""Two"" (D ""E1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 5 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of EnumMemberDeclarationSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestNamespaceAlias() As Task
            Const code = "
Imports $$N2 = N1
Namespace N1
End Namespace
"
            Const expected = "(A ""N2"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 4) 3) 2) 1) ""TestFile"" 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of SimpleImportsClauseSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestTypeAlias() As Task
            Const code = "
Imports $$C2 = N1.C1
Namespace N1
    Class C1
    End Class
End Namespace
"
            Const expected = "(A ""C2"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) ""TestFile"" 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of SimpleImportsClauseSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestGenericTypeAlias() As Task
            Const code = "
Imports $$C2 = N1.C1(Of String)
Namespace N1
    Class C1(Of T)
    End Class
End Namespace
"
            Const expected = "(A ""C2"" (D ""C1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 2 0 (% 1 (D ""String"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 2 0 ! 6)) 1) ""TestFile"" 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of SimpleImportsClauseSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestAliasWithErrorTarget() As Task
            Const code = "
Imports $$N2 = X.Y.Z
"

            ' Note that C# and VB differ here.
            Const expected = "(A ""N2"" (E ""X.Y.Z"" ! 0 ! 1) ""TestFile"" 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of SimpleImportsClauseSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestField() As Task
            Const code = "
Namespace N1
    Class C1
        Dim $$_f1 As Integer
    End Class
End Namespace
"

            Const expected = "(F ""_f1"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of ModifiedIdentifierSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestProperty() As Task
            Const code = "
Namespace N1
    Class C1
        ReadOnly Property $$P As Integer
            Get
                Return 42
            End Get
        End Property
    End Class
End Namespace
"

            Const expected = "(Q ""P"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 (% 0) (% 0) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of PropertyStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestPropertyGetter() As Task
            Const code = "
Namespace N1
    Class C1
        ReadOnly Property P As Integer
            $$Get
                Return 42
            End Get
        End Property
    End Class
End Namespace
"

            Const expected = "(M ""get_P"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 0) (% 0) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AccessorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestPropertySetter() As Task
            Const code = "
Namespace N1
    Class C1
        WriteOnly Property P As Integer
            $$Set
                Return 42
            End Set
        End Property
    End Class
End Namespace
"

            Const expected = "(M ""set_P"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AccessorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestIndexer() As Task
            Const code = "
Namespace N1
    Class C1
        Default ReadOnly Property $$Item(index As Integer) As Integer
            Get
                Return 42
            End Get
        End Property
    End Class
End Namespace
"

            Const expected = "(Q ""Item"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 1 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of PropertyStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestIndexerGetter() As Task
            Const code = "
Namespace N1
    Class C1
        Default ReadOnly Property Item(index As Integer) As Integer
            $$Get
                Return 42
            End Get
        End Property
    End Class
End Namespace
"

            Const expected = "(M ""get_Item"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AccessorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestIndexerSetter() As Task
            Const code = "
Namespace N1
    Class C1
        Default WriteOnly Property Item(index As Integer) As Integer
            $$Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
"

            Const expected = "(M ""set_Item"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 2 0 0) (% 2 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6) (# 6)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AccessorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestIndexerWithGenericParameter() As Task
            Const code = "
Namespace N1
    Class C1(Of T)
        Default ReadOnly Property $$Item(index As T) As Integer
            Get
                Return 42
            End Get
        End Property
    End Class
End Namespace
"

            Const expected = "(Q ""Item"" (D ""C1`1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 1 2 0 ! 1) 1 (% 1 0) (% 1 (Y ""T"" (# 1) 6)) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of PropertyStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestSimpleEventWithNoParameters() As Task
            Const code = "
Namespace N1
    Class C1
        Event $$E()
    End Class
End Namespace
"

            Const expected = "(V ""E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of EventStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestSimpleEventWithParameters() As Task
            Const code = "
Namespace N1
    Class C1
        Event $$E(i As Integer, j As Integer)
    End Class
End Namespace
"

            Const expected = "(V ""E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of EventStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestEventField() As Task
            Const code = "
Imports System
Namespace N1
    Class C1
        Event $$E As EventHandler
    End Class
End Namespace
"

            Const expected = "(V ""E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of EventStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestCustomEvent() As Task
            Const code = "
Imports System
Namespace N1
    Class C1
        Custom Event $$E As EventHandler
            AddHandler(value As EventHandler)
            End AddHandler
            RemoveHandler(value As EventHandler)
            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)
            End RaiseEvent
        End Event
    End Class
End Namespace
"

            Const expected = "(V ""E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of EventStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestCustomEventAdder() As Task
            Const code = "
Imports System
Namespace N1
    Class C1
        Custom Event E As EventHandler
            $$AddHandler(value As EventHandler)
            End AddHandler
            RemoveHandler(value As EventHandler)
            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)
            End RaiseEvent
        End Event
    End Class
End Namespace
"

            Const expected = "(M ""add_E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""EventHandler"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 3 0 ! 6)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AccessorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestCustomEventRemover() As Task
            Const code = "
Imports System
Namespace N1
    Class C1
        Custom Event E As EventHandler
            AddHandler(value As EventHandler)
            End AddHandler
            $$RemoveHandler(value As EventHandler)
            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)
            End RaiseEvent
        End Event
    End Class
End Namespace
"

            Const expected = "(M ""remove_E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""EventHandler"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 3 0 ! 6)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AccessorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestCustomEventRaiser() As Task
            Const code = "
Namespace N1
    Class C1
        Custom Event E As EventHandler
            AddHandler(value As EventHandler)
            End AddHandler
            RemoveHandler(value As EventHandler)
            End RemoveHandler
            $$RaiseEvent(sender As Object, e As EventArgs)
            End RaiseEvent
        End Event
    End Class
End Namespace
"

            Const expected = "(M ""raise_E"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 2 0 0) (% 2 (D ""Object"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 2 0 ! 6) (E ""EventArgs"" ! 0 ! 11)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AccessorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestOperator() As Task
            Const code = "
Namespace N1
    Class C1
        Public Shared Operator $$=(c As C1, i As Integer) As Boolean
        End Operator
    End Class
End Namespace
"

            Const expected = "(M ""op_Equality"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 2 0 0) (% 2 (# 1) (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of OperatorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestConversionOperator() As Task
            Const code = "
Namespace N1
    Class C1
        Public Shared Widening Operator $$CType(i As Integer) As C1
        End Operator
    End Class
End Namespace
"

            Const expected = "(M ""op_Implicit"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) (# 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of OperatorStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestMethod() As Task
            Const code = "
Namespace N1
    Class C1
        Sub $$M()
        End Sub
    End Class
End Namespace
"

            Const expected = "(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 0) (% 0) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of MethodStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestMethodWithParameters() As Task
            Const code = "
Namespace N1
    Class C1
        Sub $$M(i As Integer, s As String, ByRef b As Boolean, ParamArray args As Object())
        End Sub
    End Class
End Namespace
"

            Const expected = "(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 0 0 (% 4 0 0 1 0) (% 4 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6) (D ""String"" (# 7) 0 2 0 ! 11) (D ""Boolean"" (# 7) 0 10 0 ! 12) (R (D ""Object"" (# 7) 0 2 0 ! 14) 1 13)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of MethodStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestGenericMethodWithParameters() As Task
            Const code = "
Namespace N1
    Class C1
        Function $$M(Of T1, TResult)(i As T1, s As String, ByRef b As T1, ParamArray args As Object()) As TResult
        End Function
    End Class
End Namespace
"

            Const expected = "(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 2 0 (% 4 0 0 1 0) (% 4 (@ (# 0) 0 6) (D ""String"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 11) 10) 9) 8) 0 2 0 ! 7) (# 6) (R (D ""Object"" (# 8) 0 2 0 ! 13) 1 12)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of MethodStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestGenericMethodWithNestedGenericParameter() As Task
            Const code = "
Imports System.Collections.Generic

Namespace N1
    Class C1
        Sub $$M(Of T)(d As Dictionary(Of Integer, List(Of T)))
        End Sub
    End Class
End Namespace
"

            Const expected = "(M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 5) 4) 3) 2) 0 2 0 ! 1) 1 0 (% 1 0) (% 1 (D ""Dictionary`2"" (N ""Generic"" 0 (N ""Collections"" 0 (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 12) 11) 10) 9) 8) 7) 2 2 0 (% 2 (D ""Int32"" (# 9) 0 10 0 ! 13) (D ""List`1"" (# 7) 1 2 0 (% 1 (@ (# 0) 0 15)) 14)) 6)) ! 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of MethodStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestReducedExtensionMethod() As Task
            Const code = "
Imports System.Runtime.CompilerServices

Module Extensions
    <Extension>
    Function Square(i As Integer) As Integer
        Return i * i
    End Function
End Module

Class C1
    Sub M()
        Dim i As Integer = 42
        Dim squared As Integer = i.$$Square()
    End Sub
End Class
"

            Const expected = "(X (M ""Square"" (D ""Extensions"" (N """" 0 (U (S ""TestProject"" 5) 4) 3) 0 8 0 ! 2) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 1) (# 6) 0)"

            Await AssertSymbolKeyCreatedFromSymbolInfo(Of InvocationExpressionSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestReducedByRefExtensionMethod() As Task
            Const code = "
Imports System.Runtime.CompilerServices

Module Extensions
    <Extension>
    Function Square(ByRef i As Integer) As Integer
        Return i * i
    End Function
End Module

Class C1
    Sub M()
        Dim i As Integer = 42
        i.$$Square()
    End Sub
End Class
"

            Const expected = "(X (M ""Square"" (D ""Extensions"" (N """" 0 (U (S ""TestProject"" 5) 4) 3) 0 8 0 ! 2) 0 0 (% 1 1) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) ! 1) (# 6) 0)"

            Await AssertSymbolKeyCreatedFromSymbolInfo(Of InvocationExpressionSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestConstructedMethod() As Task
            Const code = "
Class C1
    Sub M(Of T)()
        $$M(Of Integer)()
    End Sub
End Class
"

            Const expected = "(C (M ""M"" (D ""C1"" (N """" 0 (U (S ""TestProject"" 5) 4) 3) 0 2 0 ! 2) 1 0 (% 0) (% 0) ! 1) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 10) 9) 8) 7) 0 10 0 ! 6)) 0)"

            Await AssertSymbolKeyCreatedFromSymbolInfo(Of InvocationExpressionSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestParameter() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M($$i As Integer)
        End Sub
    End Class
End Namespace
"

            Const expected = "(P ""i"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 1 0) (% 1 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 11) 10) 9) 8) 0 10 0 ! 7)) ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of ParameterSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestParameterWithGenericType() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M(Of T)($$i As T)
        End Sub
    End Class
End Namespace
"

            Const expected = "(P ""i"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 1 0 (% 1 0) (% 1 (@ (# 1) 0 7)) ! 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of ParameterSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestLocalVariable() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M()
            Dim $$i As Integer
        End Sub
    End Class
End Namespace
"

            Const expected = "(B ""i"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 0) (% 0) ! 1) 0 8 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of ModifiedIdentifierSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestLabel() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M()
$$label:
            Dim i As Integer
        End Sub
    End Class
End Namespace
"

            Const expected = "(B ""label"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 0) (% 0) ! 1) 0 7 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of LabelStatementSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestRangeVariable() As Task
            Const code = "
Imports System.Linq
Namespace N1
    Class C1
        Sub M()
            Dim q = From $$x in Enumerable.Range(1, 10)
                    Select x
        End Sub
    End Class
End Namespace
"

            Const expected = "(B ""x"" (M ""M"" (D ""C1"" (N ""N1"" 0 (N """" 0 (U (S ""TestProject"" 6) 5) 4) 3) 0 2 0 ! 2) 0 0 (% 0) (% 0) ! 1) 0 16 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of ModifiedIdentifierSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestTupleType() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M()
            Dim t = $$(1, 2)
        End Sub
    End Class
End Namespace
"

            Const expected = "(T 0 (D ""ValueTuple`2"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 5) 4) 3) 2) 2 10 0 (% 2 (D ""Int32"" (# 2) 0 10 0 ! 6) (# 6)) 1) (% 2 ! !) (% 2  1 ""TestFile"" 68 1  1 ""TestFile"" 71 1) 0)"

            Await AssertSymbolKeyCreatedFromTypeInfo(Of TupleExpressionSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestAnonymousType() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M()
            Dim t = $$New With { .X = 19, .Y = 42 }
        End Sub
    End Class
End Namespace
"

            Const expected = "(W (% 2 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 5) 4) 3) 2) 0 10 0 ! 1) (# 1)) (% 2 ""X"" ""Y"") (% 2 0 0) (% 2  1 ""TestFile"" 79 1  1 ""TestFile"" 88 1) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of AnonymousObjectCreationExpressionSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestAnonymousTypeProperty() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M()
            Dim t = New With { .$$X = 19, .Y = 42 }
        End Sub
    End Class
End Namespace
"

            Const expected = "(Q ""X"" (W (% 2 (D ""Int32"" (N ""System"" 0 (N """" 0 (U (S ""mscorlib"" 6) 5) 4) 3) 0 10 0 ! 2) (# 2)) (% 2 ""X"" ""Y"") (% 2 0 0) (% 2  1 ""TestFile"" 79 1  1 ""TestFile"" 88 1) 1) 0 (% 0) (% 0) 0)"

            Await AssertSymbolKeyWithDeclaredSymbol(Of FieldInitializerSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestLambdaExpression() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M()
            Dim a As Action(Of String) = $$Sub(s) Exit Sub
        End Sub
    End Class
End Namespace
"

            Const expected = "(Z 0  1 ""TestFile"" 88 15 0)"

            Await AssertSymbolKeyCreatedFromSymbolInfo(Of LambdaExpressionSyntax)(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SymbolKeys)>
        Public Async Function TestAnonymousDelegate() As Task
            Const code = "
Namespace N1
    Class C1
        Sub M()
            Dim a = $$Sub(s) Exit Sub
        End Sub
    End Class
End Namespace
"

            Const expected = "(Z 1  1 ""TestFile"" 67 15 0)"

            Await AssertSymbolKeyCreatedFromSymbolInfo(Of LambdaExpressionSyntax)(code, expected, symbolFinder:=Function(s) TryCast(s, IMethodSymbol)?.AssociatedAnonymousDelegate)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Function CreateParseOptions() As ParseOptions
            Return VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)
        End Function
    End Class
End Namespace
