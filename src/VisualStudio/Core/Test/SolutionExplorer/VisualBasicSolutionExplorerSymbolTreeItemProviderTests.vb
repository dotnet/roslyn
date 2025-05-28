' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.UnitTests
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer

Namespace Roslyn.VisualStudio.VisualBasic.UnitTests.SolutionExplorer
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.SolutionExplorer)>
    Public NotInheritable Class VisualBasicSolutionExplorerSymbolTreeItemProviderTests
        Inherits AbstractSolutionExplorerSymbolTreeItemProviderTests

        Protected Overrides Function CreateWorkspace(code As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(code, composition:=VisualStudioTestCompositions.LanguageServices)
        End Function

        Private Function TestCompilationUnit(
        code As String, expected As String) As Task

            Return TestNode(Of CompilationUnitSyntax)(code, expected)
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

        '<Theory, CombinatorialData>
        'public async function TestTypePermutations(
        '    [CombinatorialValues("Public", "Private", "Protected", "Internal")> string accessibility,
        '    [CombinatorialValues("Record", "Class", "Interface", "Struct")> string type)
        '{
        '    await TestCompilationUnit($$"
        '        {{accessibility.ToLowerInvariant()}} {{type.ToLowerInvariant()}} [|C|]
        '        {
        '        }
        '        ", $$"
        '        Name=""C"" Glyph={{type switch { "Record" => "Class", "Struct" => "Structure", _ => type }}}{{accessibility}} HasItems=False
        '        ");
        '}

        '[Theory, CombinatorialData]
        'public async function TestTypeHasItems(
        '    [CombinatorialValues("Record", "Class", "Interface", "Struct")> string type)
        '{
        '    await TestCompilationUnit($$"
        '        {{type.ToLowerInvariant()}} [|C|]
        '        {
        '            Integer i;
        '        }
        '        ", $$"
        '        Name=""C"" Glyph={{type switch { "Record" => "Class", "Struct" => "Structure", _ => type }}}Internal HasItems=True
        '        ");
        '}

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

        '<Theory]
        '<InlineData("Integer", "Integer")>
        '<InlineData("Integer[]", "Integer[]")>
        '<InlineData("Integer[][]", "Integer[][]")>
        '<InlineData("Integer[,][,,]", "Integer[,][,,]")>
        '<InlineData("Integer*", "Integer*")>
        '<InlineData("Integer?", "Integer?")>
        '<InlineData("(Integer, string)", "(Integer, string)")>
        '<InlineData("(Integer a, string b)", "(Integer a, string b)")>
        '<InlineData("A.B", "B")>
        '<InlineData("A::B", "B")>
        '<InlineData("A::B.C", "C")>
        '<InlineData("A", "A")>
        '<InlineData("A.B<C::D, E::F.G<Integer>>", "B<D, G<Integer>>")>
        'public async function TestTypes(
        '    string parameterType, string resultType)
        '{
        '    await TestCompilationUnit($$"
        '        delegate void [|D|]({{parameterType}} x);
        '        ", $$"
        '        Name=""D({{resultType}}) As void"" Glyph=DelegateInternal HasItems=False
        '        ");
        'end function

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
                sub [|New|]() { }

                protected readonly property R [|Item|](s as string)
                    get
                    end get
                end property

                private event [|A|] as Action
                end event

                sub [|M|](Of T)(a as Integer)
                end sub

                public shared operator [|+|](c1 as C, a as Integer)
                end operator

                internal shared widening operator [|CType|](c1 as C) as Integer
                end operator
            end class
            ", "
            Name=""a As Integer"" Glyph=FieldPrivate HasItems=False
            Name=""b As Integer"" Glyph=FieldPrivate HasItems=False
            Name=""Prop As P"" Glyph=PropertyPublic HasItems=False
            Name=""C()"" Glyph=MethodInternal HasItems=False
            Name=""~C()"" Glyph=MethodPrivate HasItems=False
            Name=""this[string] As R"" Glyph=PropertyProtected HasItems=False
            Name=""A As Action"" Glyph=EventPrivate HasItems=False
            Name=""B As Action"" Glyph=EventPublic HasItems=False
            Name=""C As Action"" Glyph=EventPublic HasItems=False
            Name=""M<T>(Integer) As void"" Glyph=MethodPrivate HasItems=False
            Name=""O() As void"" Glyph=MethodPublic HasItems=False
            Name=""operator +(C, Integer) As "" Glyph=OperatorPublic HasItems=False
            Name=""implicit operator Integer(C)"" Glyph=OperatorInternal HasItems=False
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
            Name=""M(Integer) As void"" Glyph=ExtensionMethodPublic HasItems=False
            ")
        End Function

        <Fact>
        Public Async Function TestAsClauses() As Task
            Await TestNode(Of ModuleBlockSyntax)("
            class C
                dim x as new Y()
            end class
            ", "
            Name=""M(Integer) As Y"" Glyph=ExtensionMethodPublic HasItems=False
            ")
        End Function
    End Class
End Namespace
