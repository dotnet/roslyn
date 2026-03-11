' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities.SolutionExplorer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SolutionExplorer
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.SolutionExplorer)>
    Public NotInheritable Class VisualBasicSolutionExplorerSymbolTreeItemProviderTests
        Inherits AbstractSolutionExplorerSymbolTreeItemProviderTests

        Protected Overrides Function CreateWorkspace(code As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(code)
        End Function

        Private Function TestCompilationUnit(code As String, expected As String) As Task
            Return TestNode(Of CompilationUnitSyntax)(code, expected)
        End Function

        Private Function TestCompilationUnitWithNamespaces(code As String, expected As String) As Task
            Return TestNode(Of CompilationUnitSyntax)(code, expected, returnNamespaces:=True)
        End Function

        Private Function TestNamespaceBlock(code As String, expected As String) As Task
            Return TestNode(Of NamespaceBlockSyntax)(code, expected, returnNamespaces:=True)
        End Function

        <Fact>
        Public Async Function TestEmptyFile() As Task
            Await TestCompilationUnit("", "")
        End Function

        <Fact>
        Public Async Function TestTopLevelClass() As Task
            Await TestCompilationUnit("
            class [|C|]
            end class
            ", "
            Name=""C"" Glyph=ClassInternal HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestTwoTopLevelTypes() As Task
            Await TestCompilationUnit("
            class [|C|]
            end class

            class [|D|]
            end class
            ", "
            Name=""C"" Glyph=ClassInternal HasItems=False
            Name=""D"" Glyph=ClassInternal HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestDelegatesAndEnums() As Task
            Await TestCompilationUnit("
                delegate function [|D|](x as Integer) as string

                enum [|E|]
                end enum
                ", "
                Name=""D(Integer) As string"" Glyph=DelegateInternal HasItems=False
                Name=""E"" Glyph=EnumInternal HasItems=False
                ")
        End Function

        <Fact>
        Public Async Function TestTypesInBlockNamespace() As Task
            Await TestCompilationUnit("
            namespace N
                class [|C|]
                end class

                class [|D|]
                end class
            end namespace
            ", "
            Name=""C"" Glyph=ClassInternal HasItems=False
            Name=""D"" Glyph=ClassInternal HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestTypesAcrossNamespaces() As Task
            Await TestCompilationUnit("
            class [|C|]
            end class

            namespace N
                class [|D|]
                end class
            end namespace
            ", "
            Name=""C"" Glyph=ClassInternal HasItems=False
            Name=""D"" Glyph=ClassInternal HasItems=False
            ")
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTypePermutations(
            <CombinatorialValues("Public", "Private", "Protected", "Friend")>
            accessibility As String,
            <CombinatorialValues("Class", "Interface", "Structure")>
            type As String) As Task
            Await TestCompilationUnit($"
                {accessibility.ToLowerInvariant()} {type.ToLowerInvariant()} [|C|]
                end {type.ToLowerInvariant()}
                ", $"
                Name=""C"" Glyph={type}{If(accessibility = "Friend", "Internal", accessibility)} HasItems=False
                ")
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTypeHasItems(
            <CombinatorialValues("Class", "Interface", "Structure")>
            type As String) As Task
            Await TestCompilationUnit($"
                {type.ToLowerInvariant()} [|C|]
                    readonly property P as string
                end {type.ToLowerInvariant()}
                ", $"
                Name=""C"" Glyph={type}Internal HasItems=True
                ")
        End Function

        <Fact>
        Public Async Function TestEnumHasItems() As Task
            Await TestCompilationUnit("
            enum [|E|]
                A
                B
                C
            end enum
            ", "
            Name=""E"" Glyph=EnumInternal HasItems=True
            ")
        End Function

        <Theory>
        <InlineData("Integer", "Integer")>
        <InlineData("Integer()", "Integer()")>
        <InlineData("Integer()()", "Integer()()")>
        <InlineData("Integer(,)(,,)", "Integer(,)(,,)")>
        <InlineData("Integer?", "Integer?")>
        <InlineData("(Integer, string)", "(Integer, string)")>
        <InlineData("(a As Integer, b as string)", "(a As Integer, b As string)")>
        <InlineData("A.B", "B")>
        <InlineData("A", "A")>
        <InlineData("A.B(Of C.D, global.F.G(Of Integer))", "B(Of D, G(Of Integer))")>
        Public Async Function TestTypes(parameterType As String, resultType As String) As Task
            Await TestCompilationUnit($"
                delegate sub [|D|](x as {parameterType})
                ", $"
                Name=""D({resultType})"" Glyph=DelegateInternal HasItems=False
                ")
        End Function

        <Fact>
        Public Async Function TestGenericClass() As Task
            Await TestCompilationUnit("
            class [|C|](of T)
            end class
            ", "
            Name=""C(Of T)"" Glyph=ClassInternal HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestGenericDelegate() As Task
            Await TestCompilationUnit("
            delegate sub [|D|](Of T)()
            ", "
            Name=""D(Of T)()"" Glyph=DelegateInternal HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestEnumMembers() As Task
            Await TestNode(Of EnumBlockSyntax)("
            enum E
                [|A|]
                [|B|]
                [|C|]
            end enum
            ", "
            Name=""A"" Glyph=EnumMemberPublic HasItems=False
            Name=""B"" Glyph=EnumMemberPublic HasItems=False
            Name=""C"" Glyph=EnumMemberPublic HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestClassMembers() As Task
            Await TestNode(Of ClassBlockSyntax)("
            class C
                private [|a|], [|b|] as Integer
                public readonly property [|Prop|] as P
                sub [|New|]()
                end sub

                protected readonly property [|Item|](s as string) as R
                    get
                    end get
                end property

                private custom event [|A|] as Action
                end event

                sub [|M|](Of T)(a as Integer)
                end sub

                public shared operator [|+|](c1 as C, a as Integer)
                end operator

                friend shared widening operator [|CType|](c1 as C) as Integer
                end operator
            end class
            ", "
               Name=""a As Integer"" Glyph=FieldPrivate HasItems=False
               Name=""b As Integer"" Glyph=FieldPrivate HasItems=False
               Name=""Prop As P"" Glyph=PropertyPublic HasItems=False
               Name=""New()"" Glyph=MethodPublic HasItems=False
               Name=""Item(string) As R"" Glyph=PropertyProtected HasItems=False
               Name=""A As Action"" Glyph=EventPrivate HasItems=False
               Name=""M(Of T)(Integer)"" Glyph=MethodPublic HasItems=False
               Name=""Operator +(C, Integer) As Object"" Glyph=OperatorPublic HasItems=False
               Name=""Operator CType(C) As Integer"" Glyph=OperatorInternal HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestExtension1() As Task
            Await TestNode(Of ModuleBlockSyntax)("
            module C
                <Extension>
                public sub [|M|](i as Integer)
                end sub
            end module
            ", "
            Name=""M(Integer)"" Glyph=ExtensionMethodPublic HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestAsClauses() As Task
            Await TestNode(Of ClassBlockSyntax)("
            class C
                dim [|x|] as new Y()
            end class
            ", "
            Name=""x As Y"" Glyph=FieldPrivate HasItems=False
            ")
        End Function

#Region "Namespace Tests (returnNamespaces: True)"

        <Fact>
        Public Async Function TestBlockNamespace() As Task
            Await TestCompilationUnitWithNamespaces("
            namespace [|N|]
                class C
                end class
            end namespace
            ", "
            Name=""N"" Glyph=Namespace HasItems=True
            ")
        End Function

        <Fact>
        Public Async Function TestBlockNamespaceEmpty() As Task
            Await TestCompilationUnitWithNamespaces("
            namespace [|N|]
            end namespace
            ", "
            Name=""N"" Glyph=Namespace HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestMultipleBlockNamespaces() As Task
            Await TestCompilationUnitWithNamespaces("
            namespace [|N1|]
                class C
                end class
            end namespace

            namespace [|N2|]
                class D
                end class
            end namespace
            ", "
            Name=""N1"" Glyph=Namespace HasItems=True
            Name=""N2"" Glyph=Namespace HasItems=True
            ")
        End Function

        <Fact>
        Public Async Function TestQualifiedNamespace() As Task
            Await TestCompilationUnitWithNamespaces("
            namespace [|A|].B.C
                class D
                end class
            end namespace
            ", "
            Name=""A.B.C"" Glyph=Namespace HasItems=True
            ")
        End Function

        <Fact>
        Public Async Function TestNamespaceNextToTopLevelType() As Task
            Await TestCompilationUnitWithNamespaces("
            class [|C|]
            end class

            namespace [|N|]
                class D
                end class
            end namespace
            ", "
            Name=""C"" Glyph=ClassInternal HasItems=False
            Name=""N"" Glyph=Namespace HasItems=True
            ")
        End Function

        <Fact>
        Public Async Function TestNestedBlockNamespaces() As Task
            Await TestCompilationUnitWithNamespaces("
            namespace [|Outer|]
                namespace Inner
                    class C
                    end class
                end namespace
            end namespace
            ", "
            Name=""Outer"" Glyph=Namespace HasItems=True
            ")
        End Function

        <Fact>
        Public Async Function TestNestedNamespaceMembers() As Task
            Await TestNamespaceBlock("
            namespace Outer
                namespace [|Inner|]
                    class C
                    end class
                end namespace
            end namespace
            ", "
            Name=""Inner"" Glyph=Namespace HasItems=True
            ")
        End Function

        <Fact>
        Public Async Function TestNamespaceMembersWithTypes() As Task
            Await TestNamespaceBlock("
            namespace N
                class [|C|]
                end class

                structure [|S|]
                end structure
            end namespace
            ", "
            Name=""C"" Glyph=ClassInternal HasItems=False
            Name=""S"" Glyph=StructureInternal HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestNamespaceMembersWithNestedNamespaceAndTypes() As Task
            Await TestNamespaceBlock("
            namespace N
                class [|C|]
                end class

                namespace [|Inner|]
                    class D
                    end class
                end namespace
            end namespace
            ", "
            Name=""C"" Glyph=ClassInternal HasItems=False
            Name=""Inner"" Glyph=Namespace HasItems=True
            ")
        End Function

        <Fact>
        Public Async Function TestNamespaceWithDelegateAndEnum() As Task
            Await TestNamespaceBlock("
            namespace N
                delegate sub [|D|]()

                enum [|E|]
                    A
                end enum
            end namespace
            ", "
            Name=""D()"" Glyph=DelegateInternal HasItems=False
            Name=""E"" Glyph=EnumInternal HasItems=True
            ")
        End Function

#End Region
    End Class
End Namespace
