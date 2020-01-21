' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    ' this place is dedicated to various parser and/or symbol errors
    Public Class SymbolErrorTests
        Inherits BasicTestBase

#Region "Targeted Error Tests"

        <Fact>
        Public Sub BC30002ERR_UndefinedType1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UndefinedType1">
        <file name="a.vb"><![CDATA[
        Structure myStruct
            Sub Scen1(FixedRankArray_7(,) As unknowntype)
            End Sub
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30002: Type 'unknowntype' is not defined.
            Sub Scen1(FixedRankArray_7(,) As unknowntype)
                                             ~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30002ERR_UndefinedType1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UndefinedType1">
        <file name="a.vb"><![CDATA[
        Namespace NS1
            Structure myStruct
                Sub Scen1()
                End Sub
            End Structure
        End Namespace
        Namespace NS2
            Structure myStruct1
                Sub Scen1(ByVal S As myStruct)
                End Sub
            End Structure
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30002: Type 'myStruct' is not defined.
                Sub Scen1(ByVal S As myStruct)
                                     ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30015ERR_InheritsFromRestrictedType1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InheritsFromRestrictedType1">
        <file name="a.vb"><![CDATA[
        Structure myStruct1
            Class C1
                'COMPILEERROR: BC30015, "System.Enum"
                Inherits System.Enum
            End Class

            Class C2
                'COMPILEERROR: BC30015, "System.MulticastDelegate"
                Inherits System.MulticastDelegate
            End Class
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30015: Inheriting from '[Enum]' is not valid.
                Inherits System.Enum
                         ~~~~~~~~~~~
BC30015: Inheriting from 'MulticastDelegate' is not valid.
                Inherits System.MulticastDelegate
                         ~~~~~~~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30022ERR_ReadOnlyHasSet()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ReadOnlyHasSet">
        <file name="a.vb"><![CDATA[
        Class C1
            Private newPropertyValue As String
            Public ReadOnly Property NewProperty() As String
                Get
                    Return newPropertyValue
                End Get
                Set(ByVal value As String)
                    newPropertyValue = value
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30022: Properties declared 'ReadOnly' cannot have a 'Set'.
                Set(ByVal value As String)
                ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30022ERR_ReadOnlyHasSet2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="ReadOnlyHasSet2">
        <file name="a.vb"><![CDATA[
        Class C1
            Inherits D2
            Public Overrides ReadOnly Property P_rw_rw_r As Integer
                Get
                    Return MyBase.P_rw_rw_r
                End Get
                Set(value As Integer)
                    MyBase.P_rw_rw_r = value
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>, ClassesWithReadWriteProperties)
            Dim expectedErrors1 = <errors><![CDATA[
BC30022: Properties declared 'ReadOnly' cannot have a 'Set'.
                Set(value As Integer)
                ~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30023ERR_WriteOnlyHasGet()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="WriteOnlyHasGet">
        <file name="a.vb"><![CDATA[
        Class C1
            Private newPropertyValue As String
            Public WriteOnly Property NewProperty() As String
                Get
                    Return newPropertyValue
                End Get
                Set(ByVal value As String)
                    newPropertyValue = value
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30023: Properties declared 'WriteOnly' cannot have a 'Get'.
                Get
                ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30023ERR_WriteOnlyHasGet2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="WriteOnlyHasGet2">
        <file name="a.vb"><![CDATA[
        Class C1
            Inherits D2
            Public Overrides WriteOnly Property P_rw_rw_w As Integer
                Get
                    Return MyBase.P_rw_rw_w
                End Get
                Set(value As Integer)
                    MyBase.P_rw_rw_w = value
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>, ClassesWithReadWriteProperties)
            Dim expectedErrors1 = <errors><![CDATA[
BC30023: Properties declared 'WriteOnly' cannot have a 'Get'.
                Get
                ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30031ERR_FullyQualifiedNameTooLong1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB.CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC.DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD
    Namespace EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE.FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF.GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
        Namespace HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH.IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII.JJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJ
            Namespace n
                Delegate Sub s()
            End Namespace

            Namespace KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK.LLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL.MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
                Namespace NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN.OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO.PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP
                    Namespace QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ.RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR.SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS
                        Namespace TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT.UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU.VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVv
                            Class CCCCC(Of T)
                                Sub s1()

                                End Sub

                                Dim x As Integer

                                Class nestedCCCC

                                End Class
                            End Class

                            Structure ssssss

                            End Structure


                            Module mmmmmm

                            End Module

                            Enum eee
                                dummy
                            End Enum

                            Interface iii

                            End Interface

                            Delegate Sub s()

                            Delegate Function f() As Integer

                        End Namespace
                    End Namespace
                End Namespace
            End Namespace
        End Namespace
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            Dim namespaceName As String = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB.CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC.DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD.EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE.FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF.GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG.HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH.IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII.JJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJ.KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK.LLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL.MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM.NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN.OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO.PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP.QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ.RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR.SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS.TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT.UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU.VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVv"
            Dim expectedErrors = <errors>
BC37220: Name '<%= namespaceName %>.CCCCC`1' exceeds the maximum length allowed in metadata.
                            Class CCCCC(Of T)
                                  ~~~~~
BC37220: Name '<%= namespaceName %>.ssssss' exceeds the maximum length allowed in metadata.
                            Structure ssssss
                                      ~~~~~~
BC37220: Name '<%= namespaceName %>.mmmmmm' exceeds the maximum length allowed in metadata.
                            Module mmmmmm
                                   ~~~~~~
BC37220: Name '<%= namespaceName %>.eee' exceeds the maximum length allowed in metadata.
                            Enum eee
                                 ~~~
BC37220: Name '<%= namespaceName %>.iii' exceeds the maximum length allowed in metadata.
                            Interface iii
                                      ~~~
BC37220: Name '<%= namespaceName %>.s' exceeds the maximum length allowed in metadata.
                            Delegate Sub s()
                                         ~
BC37220: Name '<%= namespaceName %>.f' exceeds the maximum length allowed in metadata.
                            Delegate Function f() As Integer
                                              ~
</errors>

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
            CompilationUtils.AssertTheseEmitDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC30050ERR_ParamArrayNotArray()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ERR_ParamArrayNotArray">
        <file name="a.vb"><![CDATA[
        Option Explicit
        Structure myStruct1
            Public sub m1(ParamArray byval q)
            End Sub
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30050: ParamArray parameter must be an array.
            Public sub m1(ParamArray byval q)
                                           ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30051ERR_ParamArrayRank()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ParamArrayRank">
        <file name="a.vb"><![CDATA[
        Class zzz
            Shared Sub Main()
            End Sub
            Sub abc(ByVal ParamArray s(,) As Integer)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30051: ParamArray parameter must be a one-dimensional array.
            Sub abc(ByVal ParamArray s(,) As Integer)
                                     ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30121ERR_MultipleExtends()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MultipleExtends">
        <file name="a.vb"><![CDATA[
        Class C1(of T)
            Inherits Object
            Inherits system.StringComparer
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30121: 'Inherits' can appear only once within a 'Class' statement and can only specify one class.
            Inherits system.StringComparer
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30124ERR_PropMustHaveGetSet_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C1
            'COMPILEERROR: BC30124, "aprop"
            Property aprop() As String
                Get
                    Return "30124"
                End Get
            End Property
        End Class
        Partial Class C1
            'COMPILEERROR: BC30124, "aprop"
            Property aprop() As String
                Set(ByVal Value As String)
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.
            Property aprop() As String
                     ~~~~~
BC30269: 'Public Property aprop As String' has multiple definitions with identical signatures.
            Property aprop() As String
                     ~~~~~
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.
            Property aprop() As String
                     ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30124ERR_PropMustHaveGetSet_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Property P
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.
            Property P
                     ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30125ERR_WriteOnlyHasNoWrite_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C1
            'COMPILEERROR: BC30125, "aprop"
            WriteOnly Property aprop() As String
            End Property
        End Class
        Partial Class C1
            Property aprop() As String
                Set(ByVal Value As String)
                End Set
                Get
                    Return "30125"
                End Get
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30125: 'WriteOnly' property must provide a 'Set'.
            WriteOnly Property aprop() As String
                               ~~~~~
BC30366: 'Public WriteOnly Property aprop As String' and 'Public Property aprop As String' cannot overload each other because they differ only by 'ReadOnly' or 'WriteOnly'.
            WriteOnly Property aprop() As String
                               ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30126ERR_ReadOnlyHasNoGet_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C1
            'COMPILEERROR: BC30126, "aprop"
            ReadOnly Property aprop() As String
            End Property
        End Class
        Partial Class C1
            Property aprop() As String
                Set(ByVal Value As String)
                End Set
                Get
                    Return "30126"
                End Get
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30126: 'ReadOnly' property must provide a 'Get'.
            ReadOnly Property aprop() As String
                              ~~~~~
BC30366: 'Public ReadOnly Property aprop As String' and 'Public Property aprop As String' cannot overload each other because they differ only by 'ReadOnly' or 'WriteOnly'.
            ReadOnly Property aprop() As String
                              ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30149ERR_UnimplementedMember3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnimplementedMember3">
        <file name="a.vb"><![CDATA[
        Interface Ibase
            Function one() As String
        End Interface
        Interface Iderived
            Inherits Ibase
            Function two() As String
        End Interface
        Class C1
            Implements Iderived
        End Class
        Partial Class C1
            Public Function two() As String Implements Iderived.two
                Return "two"
            End Function
        End Class
        Class C2
            Implements Iderived
        End Class
        Partial Class C2
            Public Function one() As String Implements Iderived.one
                Return "one"
            End Function
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30149: Class 'C1' must implement 'Function one() As String' for interface 'Ibase'.
            Implements Iderived
                       ~~~~~~~~
BC30149: Class 'C2' must implement 'Function two() As String' for interface 'Iderived'.
            Implements Iderived
                       ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Spec changed in Roslyn
        <WorkItem(528701, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528701")>
        <Fact>
        Public Sub BC30154ERR_UnimplementedProperty3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnimplementedProperty3">
        <file name="a.vb"><![CDATA[
        Interface PropInterface
            Property Scen1() As System.Collections.Generic.IEnumerable(Of Integer)
        End Interface
        Class HasProps
            'COMPILEERROR:BC30154,"PropInterface"
            Implements PropInterface
        End Class
                ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30149: Class 'HasProps' must implement 'Property Scen1 As IEnumerable(Of Integer)' for interface 'PropInterface'.
            Implements PropInterface
                       ~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30177ERR_DuplicateModifierCategoryUsed()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateModifierCategoryUsed">
        <file name="a.vb"><![CDATA[
        MustInherit Class C1
            MustOverride notoverridable Overridable Function StringFunc() As String
        End Class
                ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30177: Only one of 'NotOverridable', 'MustOverride', or 'Overridable' can be specified.
            MustOverride notoverridable Overridable Function StringFunc() As String
                         ~~~~~~~~~~~~~~
BC30177: Only one of 'NotOverridable', 'MustOverride', or 'Overridable' can be specified.
            MustOverride notoverridable Overridable Function StringFunc() As String
                                        ~~~~~~~~~~~                                      
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30177ERR_DuplicateModifierCategoryUsed_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateModifierCategoryUsed">
        <file name="a.vb"><![CDATA[
        MustInherit Class C1
            MustOverride Overridable Function StringFunc() As String
        End Class
                ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30177: Only one of 'NotOverridable', 'MustOverride', or 'Overridable' can be specified.
            MustOverride Overridable Function StringFunc() As String
                         ~~~~~~~~~~~                                      
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30178ERR_DuplicateSpecifier()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateSpecifier">
        <file name="a.vb"><![CDATA[
        Class test
            Shared Shared Sub Goo()
            End Sub
        End Class
                ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30178: Specifier is duplicated.
            Shared Shared Sub Goo()
                   ~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30178ERR_DuplicateSpecifier_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateSpecifier">
        <file name="a.vb"><![CDATA[
        class test
            friend shared friend function Goo()
            End function
        End class
                ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30178: Specifier is duplicated.
            friend shared friend function Goo()
                          ~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Checks for duplicate type declarations
        <Fact>
        Public Sub BC30179ERR_TypeConflict6()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class cc1
        End Class

        Class cC1
        End Class

        Class Cc1
        End Class

        structure CC1
        end structure
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30179: class 'cc1' and structure 'CC1' conflict in namespace '<Default>'.
Class cc1
      ~~~
BC30179: class 'cC1' and structure 'CC1' conflict in namespace '<Default>'.
        Class cC1
              ~~~
BC30179: class 'Cc1' and structure 'CC1' conflict in namespace '<Default>'.
        Class Cc1
              ~~~
BC30179: structure 'CC1' and class 'cc1' conflict in namespace '<Default>'.
        structure CC1
                  ~~~                                      
                        ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30179ERR_TypeConflict6_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
        Enum Unicode
            one
        End Enum
        Class Unicode
        End Class
        Module Unicode
        End Module
        Interface Unicode
        End Interface
        Delegate Sub Unicode()
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30179: enum 'Unicode' and class 'Unicode' conflict in namespace '<Default>'.
Enum Unicode
     ~~~~~~~
BC30179: class 'Unicode' and enum 'Unicode' conflict in namespace '<Default>'.
        Class Unicode
              ~~~~~~~
BC30179: module 'Unicode' and enum 'Unicode' conflict in namespace '<Default>'.
        Module Unicode
               ~~~~~~~
BC30179: interface 'Unicode' and enum 'Unicode' conflict in namespace '<Default>'.
        Interface Unicode
                  ~~~~~~~
BC30179: delegate Class 'Unicode' and enum 'Unicode' conflict in namespace '<Default>'.
        Delegate Sub Unicode()
                     ~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528149")>
        <Fact>
        Public Sub BC30179ERR_TypeConflict6_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Namespace N
            Interface I
                ReadOnly Property P
                ReadOnly Property Q
            End Interface
            Interface I ' BC30179
                Property Q
            End Interface
            Structure S
                Class T
                End Class
                Interface I
                End Interface
            End Structure
            Structure S ' BC30179
                Structure T
                End Structure
                Dim I
            End Structure
            Enum E
                A
                B
            End Enum
            Enum E ' BC30179
                A
            End Enum
            Class S ' BC30179
                Dim T
            End Class
            Delegate Sub D()
            Delegate Function D(s$) ' BC30179
        End Namespace
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30179: interface 'I' and interface 'I' conflict in namespace 'N'.
            Interface I ' BC30179
                      ~
BC30179: structure 'S' and class 'S' conflict in namespace 'N'.
            Structure S
                      ~
BC30179: structure 'S' and class 'S' conflict in namespace 'N'.
            Structure S ' BC30179
                      ~
BC30179: enum 'E' and enum 'E' conflict in namespace 'N'.
            Enum E
                 ~
BC30179: enum 'E' and enum 'E' conflict in namespace 'N'.
            Enum E ' BC30179
                 ~
BC30179: class 'S' and structure 'S' conflict in namespace 'N'.
            Class S ' BC30179
                  ~
BC30179: delegate Class 'D' and delegate Class 'D' conflict in namespace 'N'.
            Delegate Function D(s$) ' BC30179
                              ~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528149")>
        <Fact>
        Public Sub BC30179ERR_TypeConflict6_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Interface I
                ReadOnly Property P
                ReadOnly Property Q
            End Interface
            Interface I ' BC30179
                Property Q
            End Interface
            Structure S
                Class T
                End Class
                Interface I
                End Interface
            End Structure
            Structure S ' BC30179
                Structure T
                End Structure
                Dim I
            End Structure
            Enum E
                A
                B
            End Enum
            Enum E ' BC30179
                A
            End Enum
            Class S ' BC30179
                Dim T
            End Class
            Delegate Sub D()
            Delegate Function D(s$) ' BC30179
        End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30179: interface 'I' and interface 'I' conflict in class 'C'.
            Interface I ' BC30179
                      ~
BC30179: structure 'S' and class 'S' conflict in class 'C'.
            Structure S
                      ~
BC30179: structure 'S' and class 'S' conflict in class 'C'.
            Structure S ' BC30179
                      ~
BC30179: enum 'E' and enum 'E' conflict in class 'C'.
            Enum E
                 ~
BC30179: enum 'E' and enum 'E' conflict in class 'C'.
            Enum E ' BC30179
                 ~
BC30179: class 'S' and structure 'S' conflict in class 'C'.
            Class S ' BC30179
                  ~
BC30179: delegate Class 'D' and delegate Class 'D' conflict in class 'C'.
            Delegate Function D(s$) ' BC30179
                              ~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30179ERR_TypeConflict6_DupNestedEnumDeclarations()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateEnum1">
        <file name="a.vb"><![CDATA[
            Structure S1
                Enum goo
                    bar
                End Enum
                Enum goo
                    bar
                    another_bar
                End Enum
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30179: enum 'goo' and enum 'goo' conflict in structure 'S1'.
                Enum goo
                     ~~~
BC30179: enum 'goo' and enum 'goo' conflict in structure 'S1'.
                Enum goo
                     ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30179ERR_TypeConflict6_DupEnumDeclarations()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateEnum2">
        <file name="a.vb"><![CDATA[
                Enum goo
                    bar
                End Enum
                Enum goo
                    bar
                    another_bar
                End Enum
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30179: enum 'goo' and enum 'goo' conflict in namespace '<Default>'.
Enum goo
     ~~~
BC30179: enum 'goo' and enum 'goo' conflict in namespace '<Default>'.
                Enum goo
                     ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30182_ERR_UnrecognizedType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnrecognizedType">
        <file name="a.vb"><![CDATA[
        Namespace NS
            Class C1
                Function GOO() As NS
                End Function
            End Class
        End Namespace
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30182: Type expected.
                Function GOO() As NS
                                  ~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30182_ERR_UnrecognizedType_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnrecognizedType">
        <file name="a.vb"><![CDATA[
        Namespace NS
            Class C1
                inherits NS
            End Class
        End Namespace
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30182: Type expected.
                inherits NS
                         ~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30210ERR_StrictDisallowsImplicitProc()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Option Strict On
        Structure S1
            Public Function Goo()
            End Function
        End Structure
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
            Public Function Goo()
                            ~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30210ERR_StrictDisallowsImplicitProc_1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
        option strict on
        Public Class c1
            Shared Operator + (ByVal c As c1, ByVal b As c1)
            End Operator
        End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_StrictDisallowsImplicitProc, "+"),
                                    Diagnostic(ERRID.WRN_DefAsgNoRetValOpRef1, "End Operator").WithArguments("+"))

        End Sub

        <Fact>
        Public Sub BC30210ERR_StrictDisallowsImplicitProc_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Option Strict On
        Interface I
            ReadOnly Property P
        End Interface
        Structure S
            WriteOnly Property P
                Set(value As Object)
                End Set
            End Property
        End Structure
        Class C
            Property P(i As Integer)
                Get
                    Return Nothing
                End Get
                Set(value As Object)
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
            ReadOnly Property P
                              ~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
            WriteOnly Property P
                               ~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
            Property P(i As Integer)
                     ~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30210ERR_StrictDisallowsImplicitProc_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Option Strict On
        Delegate Sub D()
        Delegate Function E(o As Object)
        Delegate Function F() As Object
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
        Delegate Function E(o As Object)
                          ~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30210ERR_StrictDisallowsImplicitProc_Declare()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Module M
    Declare Sub test Lib "???" (a As Integer)
    Declare Sub F Lib "bar" ()
    Declare Function G Lib "bar" ()
End Module
    ]]></file>
</compilation>)

            Dim expectedErrors1 =
<errors><![CDATA[
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
    Declare Function G Lib "bar" ()
                     ~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30211ERR_StrictDisallowsImplicitArgs()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Option Strict On
        Structure S1
            Public Function Goo(byval x) as integer
                return 1
            End Function
        End Structure
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30211: Option Strict On requires that all method parameters have an 'As' clause.
            Public Function Goo(byval x) as integer
                                      ~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30211ERR_StrictDisallowsImplicitArgs_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Option Strict On
        Interface I
            ReadOnly Property P(i) As Object
        End Interface
        Structure S
            WriteOnly Property P(x) As Object
                Set(value)
                End Set
            End Property
        End Structure
        Class C
            Property P(val) As I
                Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30211: Option Strict On requires that all method parameters have an 'As' clause.
            ReadOnly Property P(i) As Object
                                ~
BC30211: Option Strict On requires that all method parameters have an 'As' clause.
            WriteOnly Property P(x) As Object
                                 ~
BC30211: Option Strict On requires that all method parameters have an 'As' clause.
                Set(value)
                    ~~~~~
BC30211: Option Strict On requires that all method parameters have an 'As' clause.
            Property P(val) As I
                       ~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30230ERR_ModuleCantInherit()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ModuleCantInherit">
        <file name="a.vb"><![CDATA[
        interface I1
        End interface 
        module M1 
            Inherits I1
        End module
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30230: 'Inherits' not valid in Modules.
            Inherits I1
            ~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30231ERR_ModuleCantImplement()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ModuleCantImplement">
        <file name="a.vb"><![CDATA[
        Module M1
            Implements Object
        End Module
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30231: 'Implements' not valid in Modules.
            Implements Object
            ~~~~~~~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30232ERR_BadImplementsType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadImplementsType">
        <file name="a.vb"><![CDATA[
        Class cls1
            Implements system.Enum
        End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30232: Implemented type must be an interface.
            Implements system.Enum
                       ~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30233ERR_BadConstFlags1()
            Dim source =
<compilation name="delegates">
    <file name="a.vb"><![CDATA[
Option strict on
imports system

Class C1
    ' BC30233: 'Shared' is not valid on a constant declaration.
    Public Shared Const b As String = "goo"
    Public Const Shared c As String = "goo"

    ' BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public ReadOnly Const d As String = "goo"
    Public Const ReadOnly e As String = "goo"

    ' BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public Shared ReadOnly Const f As String = "goo"
End Class

Module M1
    ' Roslyn: Only BC30593: Variables in Modules cannot be declared 'Shared'.
    ' Dev10: Additional BC30233: 'Shared' is not valid on a constant declaration.
    Public Shared Const b As String = "goo"
    Public Const Shared c As String = "goo"

    ' BC30233: 'ReadOnly' is not valid on a constant declaration.    
    Public ReadOnly Const d As String = "goo"
    Public Const ReadOnly e As String = "goo"
End Module

Structure S1
    ' BC30233: 'Shared' is not valid on a constant declaration.
    Public Shared Const b As String = "goo"
    Public Const Shared c As String = "goo"

    ' BC30233: 'ReadOnly' is not valid on a constant declaration.    
    Public ReadOnly Const d As String = "goo"
    Public Const ReadOnly e As String = "goo"
End Structure
    ]]></file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected><![CDATA[
BC30233: 'Shared' is not valid on a constant declaration.
    Public Shared Const b As String = "goo"
           ~~~~~~
BC30233: 'Shared' is not valid on a constant declaration.
    Public Const Shared c As String = "goo"
                 ~~~~~~
BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public ReadOnly Const d As String = "goo"
           ~~~~~~~~
BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public Const ReadOnly e As String = "goo"
                 ~~~~~~~~
BC30233: 'Shared' is not valid on a constant declaration.
    Public Shared ReadOnly Const f As String = "goo"
           ~~~~~~
BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public Shared ReadOnly Const f As String = "goo"
                  ~~~~~~~~
BC30593: Variables in Modules cannot be declared 'Shared'.
    Public Shared Const b As String = "goo"
           ~~~~~~
BC30593: Variables in Modules cannot be declared 'Shared'.
    Public Const Shared c As String = "goo"
                 ~~~~~~
BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public ReadOnly Const d As String = "goo"
           ~~~~~~~~
BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public Const ReadOnly e As String = "goo"
                 ~~~~~~~~
BC30233: 'Shared' is not valid on a constant declaration.
    Public Shared Const b As String = "goo"
           ~~~~~~
BC30233: 'Shared' is not valid on a constant declaration.
    Public Const Shared c As String = "goo"
                 ~~~~~~
BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public ReadOnly Const d As String = "goo"
           ~~~~~~~~
BC30233: 'ReadOnly' is not valid on a constant declaration.
    Public Const ReadOnly e As String = "goo"
                 ~~~~~~~~
]]></expected>)
        End Sub

        <WorkItem(528365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528365")>
        <Fact>
        Public Sub BC30233ERR_BadConstFlags1_02()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadConstFlags1">
        <file name="a.vb"><![CDATA[
        Imports Microsoft.VisualBasic
        Module M1
            'COMPILEERROR: BC30233, "Static"
            Static Const p As Integer = AscW("10")
        End Module
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30235: 'Static' is not valid on a member variable declaration.
            Static Const p As Integer = AscW("10")
            ~~~~~~
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact()>
        Public Sub BC30234ERR_BadWithEventsFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadWithEventsFlags1">
        <file name="a.vb"><![CDATA[
        Module M1
            'COMPILEERROR: BC30234, "ReadOnly"
            ReadOnly WithEvents var1 As Object
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30234: 'ReadOnly' is not valid on a WithEvents declaration.
            ReadOnly WithEvents var1 As Object
            ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30235ERR_BadDimFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadDimFlags1">
        <file name="a.vb"><![CDATA[
        Class cls1
            Default i As Integer
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30235: 'Default' is not valid on a member variable declaration.
            Default i As Integer
            ~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30237ERR_DuplicateParamName1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
            <compilation>
                <file name="a.vb"><![CDATA[
                Option strict on
                Module m
                    Sub s1(ByVal a As Integer, ByVal a As Integer, a as string)

                    End Sub
                End Module
                ]]></file>
            </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC30237: Parameter already declared with name 'a'.
                    Sub s1(ByVal a As Integer, ByVal a As Integer, a as string)
                                                     ~
BC30237: Parameter already declared with name 'a'.
                    Sub s1(ByVal a As Integer, ByVal a As Integer, a as string)
                                                                   ~                                     
                       ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC30237ERR_DuplicateParamName1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
                Class C
                    WriteOnly Property P(ByVal a, ByVal b)
                        Set(ByVal b)
                        End Set
                    End Property
                    Property Q(x, y, x)
                        Get
                            Return Nothing
                        End Get
                        Set(value)
                        End Set
                    End Property
                End Class
                ]]></file>
            </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC30237: Parameter already declared with name 'b'.
                        Set(ByVal b)
                                  ~
BC30237: Parameter already declared with name 'x'.
                    Property Q(x, y, x)
                                     ~
                       ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub



        <Fact>
        Public Sub BC30237ERR_DuplicateParamName1_ExternalMethods()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    <MethodImplAttribute(MethodCodeType:=MethodCodeType.Runtime)>
    Shared Sub Runtime(a As Integer, a As Integer)
    End Sub

    <MethodImpl(MethodImplOptions.InternalCall)>
    Shared Sub InternalCall(b As Integer, b As Integer)
    End Sub

    <DllImport("goo")>
    Shared Sub DllImp(c As Integer, c As Integer)
    End Sub

    Declare Sub DeclareSub Lib "bar" (d As Integer, d As Integer)
End Class
]]></file>
</compilation>
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_DuplicateParamName1, "a").WithArguments("a"),
                Diagnostic(ERRID.ERR_DuplicateParamName1, "b").WithArguments("b"),
                Diagnostic(ERRID.ERR_DuplicateParamName1, "c").WithArguments("c"))
        End Sub

        <Fact>
        Public Sub BC30242ERR_BadMethodFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadMethodFlags1">
        <file name="a.vb"><![CDATA[
        Structure S1
            Default function  goo()
            End function
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30242: 'Default' is not valid on a method declaration.
            Default function  goo()
            ~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30243ERR_BadEventFlags1()
            CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadEventFlags1">
        <file name="a.vb"><![CDATA[
        Class c1
            Shared Event e1()
            Public Event e2()
            Sub raiser1()
                RaiseEvent e1()
            End Sub
            Sub raiser2()
                RaiseEvent e2()
            End Sub
        End Class
            Class c2
                Inherits c1
                'COMPILEERROR: BC30243, "Overrides"
                Overrides Event e1()
                'COMPILEERROR: BC30243, "Overloads"
                Overloads Event e2(ByVal i As Integer)
                'COMPILEERROR: BC30243, "Overloads", BC30243, "Overrides"
                Overloads Overrides Event e3(ByVal i As Integer)
                'COMPILEERROR: BC30243, "NotOverridable"
                NotOverridable Event e4()
                'COMPILEERROR: BC30243, "Default"
                Default Event e5()
                'COMPILEERROR: BC30243, "Static"
                Static Event e6()
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_BadEventFlags1, "Overrides").WithArguments("Overrides"),
            Diagnostic(ERRID.ERR_BadEventFlags1, "Overloads").WithArguments("Overloads"),
            Diagnostic(ERRID.ERR_BadEventFlags1, "Overloads").WithArguments("Overloads"),
            Diagnostic(ERRID.ERR_BadEventFlags1, "Overrides").WithArguments("Overrides"),
            Diagnostic(ERRID.ERR_BadEventFlags1, "NotOverridable").WithArguments("NotOverridable"),
            Diagnostic(ERRID.ERR_BadEventFlags1, "Default").WithArguments("Default"),
            Diagnostic(ERRID.ERR_BadEventFlags1, "Static").WithArguments("Static"),
            Diagnostic(ERRID.WRN_OverrideType5, "e1").WithArguments("event", "e1", "event", "class", "c1"),
            Diagnostic(ERRID.WRN_OverrideType5, "e2").WithArguments("event", "e2", "event", "class", "c1"))
        End Sub

        ' BC30244ERR_BadDeclareFlags1
        ' see AttributeTests

        <WorkItem(527697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527697")>
        <Fact>
        Public Sub BC30246ERR_BadLocalConstFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadLocalConstFlags1">
        <file name="a.vb"><![CDATA[
        Module M1
            Sub Main()
                'COMPILEERROR: BC30246, "Shared"
                Shared Const x As Integer = 10
            End Sub
        End Module
        ]]></file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Shared").WithArguments("Shared"),
            Diagnostic(ERRID.WRN_UnusedLocalConst, "x").WithArguments("x"))
        End Sub

        <WorkItem(538967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538967")>
        <Fact>
        Public Sub BC30247ERR_BadLocalDimFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadLocalDimFlags1">
        <file name="a.vb"><![CDATA[
        Module M1
            Sub Main()
                'COMPILEERROR: BC30247, "Shared"
                Shared x As Integer = 10
            End Sub
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30247: 'Shared' is not valid on a local variable declaration.
                Shared x As Integer = 10
                ~~~~~~
     ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact>
        Public Sub BC30257ERR_InheritanceCycle1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InheritanceCycle1">
        <file name="a.vb"><![CDATA[
        Public Class C1
            Inherits C2
        End Class
        Public Class C2
            Inherits C1
        End Class
        Module M1
            Sub Main()
            End Sub
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30257: Class 'C1' cannot inherit from itself: 
    'C1' inherits from 'C2'.
    'C2' inherits from 'C1'.
            Inherits C2
                     ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact>
        Public Sub BC30258ERR_InheritsFromNonClass()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InheritsFrom2">
        <file name="a.vb"><![CDATA[
        Structure myStruct1
            Class C1
            Inherits myStruct1
            End Class
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30258: Classes can inherit only from other classes.
            Inherits myStruct1
                     ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Checks for duplicate type declarations
        ' DEV code
        <Fact>
        Public Sub BC30260ERR_MultiplyDefinedType3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            class c
                sub i(ByVal i As Integer) 
                End Sub

                dim i(,,) as integer

                Public i, i As Integer

                Private i As Integer 

                Sub i()
                End Sub

            end class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30260: 'i' is already declared as 'Public Sub i(i As Integer)' in this class.
                dim i(,,) as integer
                    ~
BC30260: 'i' is already declared as 'Public Sub i(i As Integer)' in this class.
                Public i, i As Integer
                       ~
BC30260: 'i' is already declared as 'Public Sub i(i As Integer)' in this class.
                Public i, i As Integer
                          ~
BC30260: 'i' is already declared as 'Public Sub i(i As Integer)' in this class.
                Private i As Integer 
                        ~   
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30260ERR_MultiplyDefinedType3_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
        Class C
            Enum E As Short
                A
            End Enum
            Function E()
            End Function
        End Class
        Interface I
            Structure S
            End Structure
            Sub S()
        End Interface
        Structure S
            Class C
            End Class
            Property C
            Interface I
            End Interface
            Private I
        End Structure
        ]]></file>
</compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30260: 'E' is already declared as 'Enum E' in this class.
            Function E()
                     ~
BC30260: 'S' is already declared as 'Structure S' in this interface.
            Sub S()
                ~
BC30260: 'C' is already declared as 'Class C' in this structure.
            Property C
                     ~
BC30260: 'I' is already declared as 'Interface I' in this structure.
            Private I
                    ~
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30260ERR_MultiplyDefinedType3_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        MustInherit Class C
            Overloads Property P
            Overloads Function P(o)
                Return Nothing
            End Function
            Overloads Shared Property Q(o)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Overloads Sub Q()
            End Sub
            Class R
            End Class
            ReadOnly Property R(x, y)
                Get
                    Return Nothing
                End Get
            End Property
            Private WriteOnly Property S As Integer
                Set(value As Integer)
                End Set
            End Property
            Structure S
            End Structure
            Private Shared ReadOnly Property T As Integer
                Get
                    Return 0
                End Get
            End Property
            Private T
            Friend MustOverride Overloads Function U()
            Protected Property U
            Protected V As Object
            Friend Shared Property V
        End Class
        ]]></file>
    </compilation>)

            ' Note: Unlike Dev10, the error with 'S' is reported on the property
            ' rather than the struct, even though the struct appears first in source.
            ' That is because types are resolved before other members.
            Dim expectedErrors1 = <errors><![CDATA[
BC30260: 'P' is already declared as 'Public Overloads Property P As Object' in this class.
            Overloads Function P(o)
                               ~
BC30260: 'Q' is already declared as 'Public Shared Overloads Property Q(o As Object) As Object' in this class.
            Overloads Sub Q()
                          ~
BC30260: 'R' is already declared as 'Class R' in this class.
            ReadOnly Property R(x, y)
                              ~
BC30260: 'S' is already declared as 'Structure S' in this class.
            Private WriteOnly Property S As Integer
                                       ~
BC30260: 'T' is already declared as 'Private Shared ReadOnly Property T As Integer' in this class.
            Private T
                    ~
BC30260: 'U' is already declared as 'Friend MustOverride Overloads Function U() As Object' in this class.
            Protected Property U
                               ~
BC30260: 'V' is already declared as 'Protected V As Object' in this class.
            Friend Shared Property V
                                   ~
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30266ERR_BadOverrideAccess2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadLocalDimFlags1">
        <file name="a.vb"><![CDATA[
        Namespace NS1
            Class base
                Protected Overridable Function scen2(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                Public Overridable Function scen1(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
            End Class
            Partial Class base
                Friend Overridable Function scen3(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                Protected Friend Overridable Function scen4(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                Private Protected Overridable Function scen5(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                Protected Overridable Function scen6(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
            End Class
            Partial Class derived
                'COMPILEERROR: BC30266, "scen4"
                Protected Overrides Function scen4(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
            End Class
            Class derived
                Inherits base
                'COMPILEERROR: BC30266, "scen2"
                Friend Overrides Function scen2(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                'COMPILEERROR: BC30266, "scen3"
                Public Overrides Function scen3(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                'COMPILEERROR: BC30266, "scen1"
                Protected Overrides Function scen1(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                'COMPILEERROR: BC30266, "scen5"
                Protected Overrides Function scen5(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
                'COMPILEERROR: BC30266, "scen6"
                Private Protected Overrides Function scen6(Of t)(ByVal x As t) As String
                    Return "30266"
                End Function
            End Class
        End Namespace
        ]]></file>
    </compilation>,
        parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            Dim expectedErrors1 = <errors><![CDATA[
BC30266: 'Protected Overrides Function scen4(Of t)(x As t) As String' cannot override 'Protected Friend Overridable Function scen4(Of t)(x As t) As String' because they have different access levels.
                Protected Overrides Function scen4(Of t)(ByVal x As t) As String
                                             ~~~~~
BC30266: 'Friend Overrides Function scen2(Of t)(x As t) As String' cannot override 'Protected Overridable Function scen2(Of t)(x As t) As String' because they have different access levels.
                Friend Overrides Function scen2(Of t)(ByVal x As t) As String
                                          ~~~~~
BC30266: 'Public Overrides Function scen3(Of t)(x As t) As String' cannot override 'Friend Overridable Function scen3(Of t)(x As t) As String' because they have different access levels.
                Public Overrides Function scen3(Of t)(ByVal x As t) As String
                                          ~~~~~
BC30266: 'Protected Overrides Function scen1(Of t)(x As t) As String' cannot override 'Public Overridable Function scen1(Of t)(x As t) As String' because they have different access levels.
                Protected Overrides Function scen1(Of t)(ByVal x As t) As String
                                             ~~~~~
BC30266: 'Protected Overrides Function scen5(Of t)(x As t) As String' cannot override 'Private Protected Overridable Function scen5(Of t)(x As t) As String' because they have different access levels.
                Protected Overrides Function scen5(Of t)(ByVal x As t) As String
                                             ~~~~~
BC30266: 'Private Protected Overrides Function scen6(Of t)(x As t) As String' cannot override 'Protected Overridable Function scen6(Of t)(x As t) As String' because they have different access levels.
                Private Protected Overrides Function scen6(Of t)(ByVal x As t) As String
                                                     ~~~~~
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact>
        Public Sub BC30267ERR_CantOverrideNotOverridable2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantOverrideNotOverridable2">
        <file name="a.vb"><![CDATA[
        Class c1
            Class c1_0
                Overridable Sub goo()
                End Sub
            End Class
            Class c1_1
                Inherits c1_0
                NotOverridable Overrides Sub goo()
                End Sub
            End Class
            Class c1_2
                Inherits c1_1
                'COMPILEERROR: BC30267, "goo"
                Overrides Sub goo()
                End Sub
            End Class
        End Class
        Class c1_2
            Inherits c1.c1_1
            'COMPILEERROR: BC30267, "goo"
            Overrides Sub goo()
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30267: 'Public Overrides Sub goo()' cannot override 'Public NotOverridable Overrides Sub goo()' because it is declared 'NotOverridable'.
                Overrides Sub goo()
                              ~~~
BC30267: 'Public Overrides Sub goo()' cannot override 'Public NotOverridable Overrides Sub goo()' because it is declared 'NotOverridable'.
            Overrides Sub goo()
                          ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30269ERR_DuplicateProcDef1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateProcDef1">
        <file name="a.vb"><![CDATA[
            Class C1
                Public Sub New() ' 1
                End Sub
                Public Sub New() ' 2
                End Sub
                Shared Sub New() ' ok :)
                End Sub

                Public Sub Goo1() ' 1
                End Sub
                Public Sub Goo1() ' 2
                End Sub
                Public Sub Goo1() ' 3
                End Sub

                Public overloads Sub Goo2() ' 1
                End Sub
                Public overloads Sub Goo2() ' 2
                End Sub

                Public Sub Goo3(Of T)(X As T) ' 1
                End Sub
                Public Sub Goo3(Of TT)(X As TT) ' 2
                End Sub

                Public Sub GooOK(x as Integer) ' 1
                End Sub
                Public Sub GooOK(x as Decimal) ' 2
                End Sub

                Public Shared Sub Main()
                End Sub
            End Class

            Class Base
               public overridable sub goo4() ' base
              End sub
            End Class

            Class Derived
                Inherits Base

                public overrides sub goo4() ' derived 1
                End sub
                public overloads sub goo4() ' derived 2
                End sub
            End Class

            Class PartialClass
                Public Sub Goo5() ' 1
                End Sub

                Public Sub Goo6(x as integer)
                End Sub
            End Class
            Partial Class PartialClass
                Public Sub Goo5() ' 2
                End Sub

                Public Sub Goo6(y as integer)
                End Sub
            End Class

        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30269: 'Public Sub New()' has multiple definitions with identical signatures.
                Public Sub New() ' 1
                           ~~~
BC30269: 'Public Sub Goo1()' has multiple definitions with identical signatures.
                Public Sub Goo1() ' 1
                           ~~~~
BC30269: 'Public Sub Goo1()' has multiple definitions with identical signatures.
                Public Sub Goo1() ' 2
                           ~~~~
BC30269: 'Public Overloads Sub Goo2()' has multiple definitions with identical signatures.
                Public overloads Sub Goo2() ' 1
                                     ~~~~
BC30269: 'Public Sub Goo3(Of T)(X As T)' has multiple definitions with identical signatures.
                Public Sub Goo3(Of T)(X As T) ' 1
                           ~~~~
BC30269: 'Public Overrides Sub goo4()' has multiple definitions with identical signatures.
                public overrides sub goo4() ' derived 1
                                     ~~~~
BC30269: 'Public Sub Goo5()' has multiple definitions with identical signatures.
                Public Sub Goo5() ' 1
                           ~~~~
BC30269: 'Public Sub Goo6(x As Integer)' has multiple definitions with identical signatures.
                Public Sub Goo6(x as integer)
                           ~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30269ERR_DuplicateProcDef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateProcDef1">
        <file name="a.vb"><![CDATA[
        Interface ii
            Sub abc()
        End Interface
        Structure teststruct
            Implements ii
            'COMPILEERROR: BC30269, "abc"
            Public Sub abc() Implements ii.abc
            End Sub
            'COMPILEERROR: BC30269, "New"
            Public Sub New(ByVal x As String)
            End Sub
        End Structure
        Partial Structure teststruct
            Implements ii
            Public Sub New(ByVal x As String)
            End Sub
            Public Sub abc() Implements ii.abc
            End Sub
        End Structure

        ]]></file>
    </compilation>)

            ' TODO: The last error is expected to go away once "Implements" is supported.
            Dim expectedErrors1 = <errors><![CDATA[
BC30269: 'Public Sub abc()' has multiple definitions with identical signatures.
            Public Sub abc() Implements ii.abc
                       ~~~
BC30269: 'Public Sub New(x As String)' has multiple definitions with identical signatures.
            Public Sub New(ByVal x As String)
                       ~~~
BC30583: 'ii.abc' cannot be implemented more than once.
            Public Sub abc() Implements ii.abc
                                        ~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(543162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543162")>
        <Fact()>
        Public Sub BC30269ERR_DuplicateProcDef1_Shared()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateProcDef1">
        <file name="a.vb"><![CDATA[
Class C1
    Public Sub New1() ' 1
    End Sub
    Public Shared Sub New1() ' 2
    End Sub
End Class
        ]]></file>
    </compilation>)

            AssertTheseDiagnostics(compilation1, errs:=<expected><![CDATA[
BC30269: 'Public Sub New1()' has multiple definitions with identical signatures.
    Public Sub New1() ' 1
               ~~~~                                                     
                                                  ]]></expected>)
        End Sub

        <Fact>
        Public Sub BC30270ERR_BadInterfaceMethodFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadInterfaceMethodFlags1">
        <file name="a.vb"><![CDATA[
        Imports System.Collections
        Interface I
            Public Function A()
            Private Function B()
            Protected Function C()
            Friend Function D()
            Shared Function E()
            MustInherit Function F()
            NotInheritable Function G()
            Overrides Function H()
            Partial Function J()
            NotOverridable Function K()
            Overridable Function L()
            Iterator Function Goo() as IEnumerator
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30270: 'Public' is not valid on an interface method declaration.
            Public Function A()
            ~~~~~~
BC30270: 'Private' is not valid on an interface method declaration.
            Private Function B()
            ~~~~~~~
BC30270: 'Protected' is not valid on an interface method declaration.
            Protected Function C()
            ~~~~~~~~~
BC30270: 'Friend' is not valid on an interface method declaration.
            Friend Function D()
            ~~~~~~
BC30270: 'Shared' is not valid on an interface method declaration.
            Shared Function E()
            ~~~~~~
BC30242: 'MustInherit' is not valid on a method declaration.
            MustInherit Function F()
            ~~~~~~~~~~~
BC30242: 'NotInheritable' is not valid on a method declaration.
            NotInheritable Function G()
            ~~~~~~~~~~~~~~
BC30270: 'Overrides' is not valid on an interface method declaration.
            Overrides Function H()
            ~~~~~~~~~
BC30270: 'Partial' is not valid on an interface method declaration.
            Partial Function J()
            ~~~~~~~
BC30270: 'NotOverridable' is not valid on an interface method declaration.
            NotOverridable Function K()
            ~~~~~~~~~~~~~~
BC30270: 'Overridable' is not valid on an interface method declaration.
            Overridable Function L()
            ~~~~~~~~~~~
BC30270: 'Iterator' is not valid on an interface method declaration.
            Iterator Function Goo() as IEnumerator
            ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30273ERR_BadInterfacePropertyFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadInterfacePropertyFlags1">
        <file name="a.vb"><![CDATA[
        Imports System.Collections
        Interface I
            Public Property A
            Private Property B
            Protected Property C
            Friend Property D
            Shared Property E
            MustInherit Property F
            NotInheritable Property G
            Overrides Property H
            NotOverridable Property J
            Overridable Property K
            ReadOnly Property L ' No error
            WriteOnly Property M ' No error
            Default Property N(o) ' No error
            Iterator Property Goo() as IEnumerator
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30273: 'Public' is not valid on an interface property declaration.
            Public Property A
            ~~~~~~
BC30273: 'Private' is not valid on an interface property declaration.
            Private Property B
            ~~~~~~~
BC30273: 'Protected' is not valid on an interface property declaration.
            Protected Property C
            ~~~~~~~~~
BC30273: 'Friend' is not valid on an interface property declaration.
            Friend Property D
            ~~~~~~
BC30273: 'Shared' is not valid on an interface property declaration.
            Shared Property E
            ~~~~~~
BC30639: Properties cannot be declared 'MustInherit'.
            MustInherit Property F
            ~~~~~~~~~~~
BC30639: Properties cannot be declared 'NotInheritable'.
            NotInheritable Property G
            ~~~~~~~~~~~~~~
BC30273: 'Overrides' is not valid on an interface property declaration.
            Overrides Property H
            ~~~~~~~~~
BC30273: 'NotOverridable' is not valid on an interface property declaration.
            NotOverridable Property J
            ~~~~~~~~~~~~~~
BC30273: 'Overridable' is not valid on an interface property declaration.
            Overridable Property K
            ~~~~~~~~~~~
BC30273: 'Iterator' is not valid on an interface property declaration.
            Iterator Property Goo() as IEnumerator
            ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30275ERR_InterfaceCantUseEventSpecifier1()
            CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceCantUseEventSpecifier1">
        <file name="a.vb"><![CDATA[
        Interface I1
            'COMPILEERROR: BC30275, "friend"
            Friend Event goo()
        End Interface
        Interface I2
            'COMPILEERROR: BC30275, "protected"
            Protected Event goo()
        End Interface
        Interface I3
            'COMPILEERROR: BC30275, "Private"
            Private Event goo()
        End Interface
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InterfaceCantUseEventSpecifier1, "Friend").WithArguments("Friend"),
    Diagnostic(ERRID.ERR_InterfaceCantUseEventSpecifier1, "Protected").WithArguments("Protected"),
    Diagnostic(ERRID.ERR_InterfaceCantUseEventSpecifier1, "Private").WithArguments("Private"))

        End Sub

        <WorkItem(539943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539943")>
        <Fact>
        Public Sub BC30280ERR_BadEmptyEnum1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadEmptyEnum1">
        <file name="a.vb"><![CDATA[
        Enum SEX
        End Enum
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30280: Enum 'SEX' must contain at least one member.
Enum SEX
     ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30283ERR_CantOverrideConstructor()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantOverrideConstructor">
        <file name="a.vb"><![CDATA[
        Class Class2
        Inherits Class1
            Overrides Sub New()
        End Sub
        End Class
        Class Class1
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30283: 'Sub New' cannot be declared 'Overrides'.
            Overrides Sub New()
            ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30284ERR_OverrideNotNeeded3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverrideNotNeeded3">
        <file name="a.vb"><![CDATA[
        Imports System
        Namespace NS30284
            Public Class Class1
                Dim xprop2, xprop3, xprop5
            End Class
            Public Class Class2
                Inherits Class1
                Interface interface1
                    ReadOnly Property prop1()
                    WriteOnly Property prop2()
                    WriteOnly Property prop3()
                    ReadOnly Property prop5()
                End Interface
                Public Sub New()
                    MyBase.New()
                End Sub
                Public Sub New(ByVal val As Short)
                    MyBase.New()
                End Sub
                'COMPILEERROR: BC30284, "prop1"
                Overrides WriteOnly Property prop1() As String
                    Set(ByVal Value As String)
                    End Set
                End Property
                'COMPILEERROR: BC30284, "prop2"
                Overrides ReadOnly Property prop2() As String
                    Get
                        Return "30284"
                    End Get
                End Property
                'COMPILEERROR: BC30284, "prop3"
                Overrides Property prop3() As String
                    Get
                        Return "30284"
                    End Get
                    Set(ByVal Value As String)
                    End Set
                End Property
                'COMPILEERROR: BC30284, "prop5"
                Overrides ReadOnly Property prop5()
                    Get
                        Return "30284"
                    End Get
                End Property
            End Class
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30284: property 'prop1' cannot be declared 'Overrides' because it does not override a property in a base class.
                Overrides WriteOnly Property prop1() As String
                                             ~~~~~
BC30284: property 'prop2' cannot be declared 'Overrides' because it does not override a property in a base class.
                Overrides ReadOnly Property prop2() As String
                                            ~~~~~
BC30284: property 'prop3' cannot be declared 'Overrides' because it does not override a property in a base class.
                Overrides Property prop3() As String
                                   ~~~~~
BC30284: property 'prop5' cannot be declared 'Overrides' because it does not override a property in a base class.
                Overrides ReadOnly Property prop5()
                                            ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        '        <Fact()>
        '        Public Sub BC30293ERR_RecordEmbeds2()
        '            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
        '    <compilation name="RecordEmbeds2">
        '       <file name="a.vb"><![CDATA[
        '            Class C1
        '            End Class
        '            Class C2
        '                Inherits C1
        '                Overrides WriteOnly Property prop1() As String
        '                    Set(ByVal value As String)
        '                    End Set
        '                End Property
        '            End Class
        '        ]]></file>
        '    </compilation>)
        '            Dim expectedErrors1 = <errors><![CDATA[
        'BC30293: property 'prop1' cannot be declared 'Overrides' because it does not override a property in a base class.	
        '            Overrides WriteOnly Property prop1() As String
        '                ~~~~~~
        '     ]]></errors>
        '            CompilationUtils.AssertTheseDeclarationErrors(compilation1, expectedErrors1)
        '        End Sub

        <Fact>
        Public Sub BC30294ERR_RecordCycle2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RecordCycle2">
        <file name="a.vb"><![CDATA[
        Public Structure yyy
            Dim a As yyy
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30294: Structure 'yyy' cannot contain an instance of itself: 
    'yyy' contains 'yyy' (variable 'a').
            Dim a As yyy
                ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30296ERR_InterfaceCycle1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceCycle1">
        <file name="a.vb"><![CDATA[
        Public Class c0
            Protected Class cls1
                Public Interface I2
                    Interface I2
                        Inherits I2
                    End Interface
                End Interface
            End Class
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30296: Interface 'c0.cls1.I2.I2' cannot inherit from itself: 
    'c0.cls1.I2.I2' inherits from 'c0.cls1.I2.I2'.
                        Inherits I2
                                 ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30299ERR_InheritsFromCantInherit3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InheritsFromCantInherit3">
        <file name="a.vb"><![CDATA[
        Class c1
        NotInheritable Class c1_1
            Class c1_4
                Inherits c1_1
            End Class
        End Class
            Class c1_2
                Inherits c1_1
            End Class
        End Class
        Class c1_3
            Inherits c1.c1_1
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30299: 'c1_4' cannot inherit from class 'c1_1' because 'c1_1' is declared 'NotInheritable'.
                Inherits c1_1
                         ~~~~
BC30299: 'c1_2' cannot inherit from class 'c1_1' because 'c1_1' is declared 'NotInheritable'.
                Inherits c1_1
                         ~~~~
BC30299: 'c1_3' cannot inherit from class 'c1_1' because 'c1_1' is declared 'NotInheritable'.
            Inherits c1.c1_1
                     ~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30300ERR_OverloadWithOptional2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithOptional2">
        <file name="a.vb"><![CDATA[
        Class Cla30300
            'COMPILEERROR: BC30300, "goo"
            Public Function goo(ByVal arg As ULong)
                Return "BC30300"
            End Function
            Public Function goo(Optional ByVal arg As ULong = 1)
                Return "BC30300"
            End Function
            'COMPILEERROR: BC30300, "goo1"
            Public Function goo1()
                Return "BC30300"
            End Function
            Public Function goo1(Optional ByVal arg As ULong = 1)
                Return "BC30300"
            End Function
            'COMPILEERROR: BC30300, "goo2"
            Public Function goo2(ByVal arg As Integer)
                Return "BC30300"
            End Function
            Public Function goo2(ByVal arg As Integer, Optional ByVal arg1 As ULong = 1)
                Return "BC30300"
            End Function
            'COMPILEERROR: BC30300, "goo3"
            Public Function goo3(ByVal arg As Integer, ByVal arg1 As ULong)
                Return "BC30300"
            End Function
            Public Function goo3(ByVal arg As Integer, Optional ByVal arg1 As ULong = 1)
                Return "BC30300"
            End Function
        End Class
        Interface Scen2_1
            'COMPILEERROR: BC30300, "goo"
            Function goo(ByVal arg As ULong)
            Function goo(Optional ByVal arg As ULong = 1)
            'COMPILEERROR: BC30300, "goo1"
            Function goo1()
            Function goo1(Optional ByVal arg As ULong = 1)
            'COMPILEERROR: BC30300, "goo2"
            Function goo2(ByVal arg As Integer)
            Function goo2(ByVal arg As Integer, Optional ByVal arg1 As ULong = 1)
            'COMPILEERROR: BC30300, "goo3"
            Function goo3(ByVal arg As Integer, ByVal arg1 As ULong)
            Function goo3(ByVal arg As Integer, Optional ByVal arg1 As ULong = 1)
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30300: 'Public Function goo(arg As ULong) As Object' and 'Public Function goo([arg As ULong = 1]) As Object' cannot overload each other because they differ only by optional parameters.
            Public Function goo(ByVal arg As ULong)
                            ~~~
BC30300: 'Public Function goo3(arg As Integer, arg1 As ULong) As Object' and 'Public Function goo3(arg As Integer, [arg1 As ULong = 1]) As Object' cannot overload each other because they differ only by optional parameters.
            Public Function goo3(ByVal arg As Integer, ByVal arg1 As ULong)
                            ~~~~
BC30300: 'Function goo(arg As ULong) As Object' and 'Function goo([arg As ULong = 1]) As Object' cannot overload each other because they differ only by optional parameters.
            Function goo(ByVal arg As ULong)
                     ~~~
BC30300: 'Function goo3(arg As Integer, arg1 As ULong) As Object' and 'Function goo3(arg As Integer, [arg1 As ULong = 1]) As Object' cannot overload each other because they differ only by optional parameters.
            Function goo3(ByVal arg As Integer, ByVal arg1 As ULong)
                     ~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30301ERR_OverloadWithReturnType2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateProcDef1">
        <file name="a.vb"><![CDATA[
            Class C1
            End Class

            Class C2
            End Class

            Class C3
                Public Function Goo1(x as Integer) as Boolean ' 1
                  return true
                End Function
                Public Function Goo1(x as Integer) as Boolean ' 2
                  return true
                End Function
                Public Function Goo1(x as Integer) as Boolean ' 3
                  return true
                End Function
                Public Function Goo1(x as Integer) as Decimal ' 4
                  return 2.2
                End Function
                Public Function Goo1(x as Integer) as String  ' 5
                  return "42"
                End Function

                Public Function Goo2(Of T as C1)(x as Integer) as T ' 1
                  return nothing
                End Function
                Public Function Goo2(Of S as C2)(x as Integer) as S  ' 2
                  return nothing
                End Function

                Public Function Goo3(x as Integer) as Boolean ' 1
                  return true
                End Function
                Public Function Goo3(x as Decimal) as Boolean ' 2
                  return true
                End Function
                Public Function Goo3(x as Integer) as Boolean ' 3
                  return true
                End Function
                Public Function Goo3(x as Integer) as Boolean ' 4
                  return true
                End Function
                Public Function Goo3(x as Integer) as String  ' 5
                  return true
                End Function

                Public Shared Sub Main()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30269: 'Public Function Goo1(x As Integer) As Boolean' has multiple definitions with identical signatures.
                Public Function Goo1(x as Integer) as Boolean ' 1
                                ~~~~
BC30269: 'Public Function Goo1(x As Integer) As Boolean' has multiple definitions with identical signatures.
                Public Function Goo1(x as Integer) as Boolean ' 2
                                ~~~~
BC30301: 'Public Function Goo1(x As Integer) As Boolean' and 'Public Function Goo1(x As Integer) As Decimal' cannot overload each other because they differ only by return types.
                Public Function Goo1(x as Integer) as Boolean ' 3
                                ~~~~
BC30301: 'Public Function Goo1(x As Integer) As Decimal' and 'Public Function Goo1(x As Integer) As String' cannot overload each other because they differ only by return types.
                Public Function Goo1(x as Integer) as Decimal ' 4
                                ~~~~
BC30269: 'Public Function Goo2(Of T As C1)(x As Integer) As T' has multiple definitions with identical signatures.
                Public Function Goo2(Of T as C1)(x as Integer) as T ' 1
                                ~~~~
BC30269: 'Public Function Goo3(x As Integer) As Boolean' has multiple definitions with identical signatures.
                Public Function Goo3(x as Integer) as Boolean ' 1
                                ~~~~
BC30269: 'Public Function Goo3(x As Integer) As Boolean' has multiple definitions with identical signatures.
                Public Function Goo3(x as Integer) as Boolean ' 3
                                ~~~~
BC30301: 'Public Function Goo3(x As Integer) As Boolean' and 'Public Function Goo3(x As Integer) As String' cannot overload each other because they differ only by return types.
                Public Function Goo3(x as Integer) as Boolean ' 4
                                ~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30301ERR_OverloadWithReturnType2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithReturnType2">
        <file name="a.vb"><![CDATA[
        Class Cla30301
            'COMPILEERROR: BC30301, "goo"
            Public Function goo(ByVal arg As ULong)
                Return "BC30301"
            End Function
            Public Function goo(ByVal arg As ULong) As String
                Return "BC30301"
            End Function
            'COMPILEERROR: BC30301, "goo1"
            Public Function goo1()
                Return "BC30301"
            End Function
            Public Function goo1() As String
                Return "BC30301"
            End Function
        End Class
        Interface Interface30301
            'COMPILEERROR: BC30301, "goo"
            Function goo(ByVal arg As ULong)
            Function goo(ByVal arg As ULong) As String
            'COMPILEERROR: BC30301, "goo1"
            Function goo1()
            Function goo1() As String
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30301: 'Public Function goo(arg As ULong) As Object' and 'Public Function goo(arg As ULong) As String' cannot overload each other because they differ only by return types.
            Public Function goo(ByVal arg As ULong)
                            ~~~
BC30301: 'Public Function goo1() As Object' and 'Public Function goo1() As String' cannot overload each other because they differ only by return types.
            Public Function goo1()
                            ~~~~
BC30301: 'Function goo(arg As ULong) As Object' and 'Function goo(arg As ULong) As String' cannot overload each other because they differ only by return types.
            Function goo(ByVal arg As ULong)
                     ~~~
BC30301: 'Function goo1() As Object' and 'Function goo1() As String' cannot overload each other because they differ only by return types.
            Function goo1()
                     ~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30302ERR_TypeCharWithType1_01()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="c.vb"><![CDATA[
        Class C
            Dim A% As Integer
            Dim B& As Long
            Dim C@ As Decimal
            Dim D! As Single
            Dim E# As Double
            Dim F$ As String
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
            Dim A% As Integer
                ~~
BC30302: Type character '&' cannot be used in a declaration with an explicit type.
            Dim B& As Long
                ~~
BC30302: Type character '@' cannot be used in a declaration with an explicit type.
            Dim C@ As Decimal
                ~~
BC30302: Type character '!' cannot be used in a declaration with an explicit type.
            Dim D! As Single
                ~~
BC30302: Type character '#' cannot be used in a declaration with an explicit type.
            Dim E# As Double
                ~~
BC30302: Type character '$' cannot be used in a declaration with an explicit type.
            Dim F$ As String
                ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30302ERR_TypeCharWithType1_02()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="c.vb"><![CDATA[
        Class C
            Property A% As Integer
            Property B& As Long
            Property C@ As Decimal
            Property D! As Single
            Property E# As Double
            Property F$ As String
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
            Property A% As Integer
                     ~~
BC30302: Type character '&' cannot be used in a declaration with an explicit type.
            Property B& As Long
                     ~~
BC30302: Type character '@' cannot be used in a declaration with an explicit type.
            Property C@ As Decimal
                     ~~
BC30302: Type character '!' cannot be used in a declaration with an explicit type.
            Property D! As Single
                     ~~
BC30302: Type character '#' cannot be used in a declaration with an explicit type.
            Property E# As Double
                     ~~
BC30302: Type character '$' cannot be used in a declaration with an explicit type.
            Property F$ As String
                     ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30302ERR_TypeCharWithType1_03()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="c.vb"><![CDATA[
Option Infer On            
Class C
    Shared Sub Main()
        For Each x% In New Integer() {1, 1}
        Next
        For Each x& In New Long() {1, 1}
        Next
        For Each x! In New Double() {1, 1}
        Next
        For Each x# In New Double() {1, 1}
        Next
        For Each x@ In New Decimal() {1, 1}
        Next
        'COMPILEERROR: BC30302
        For Each x% As Long In New Long() {1, 1, 1}
        Next
        For Each x# As Single In New Double() {1, 1, 1}
        Next
        For Each x@ As Decimal In New Decimal() {1, 1, 1}
        Next
        For Each x! As Object In New Long() {1, 1, 1}
        Next
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
        For Each x% As Long In New Long() {1, 1, 1}
                 ~~
BC30302: Type character '#' cannot be used in a declaration with an explicit type.
        For Each x# As Single In New Double() {1, 1, 1}
                 ~~
BC30302: Type character '@' cannot be used in a declaration with an explicit type.
        For Each x@ As Decimal In New Decimal() {1, 1, 1}
                 ~~
BC30302: Type character '!' cannot be used in a declaration with an explicit type.
        For Each x! As Object In New Long() {1, 1, 1}
                 ~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30302ERR_TypeCharWithType1_04()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="c.vb"><![CDATA[
Option Infer On
Imports System
Module Module1
    Sub Main()
        Dim arr15#(,) As Double ' Invalid
    End Sub
End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30302: Type character '#' cannot be used in a declaration with an explicit type.
        Dim arr15#(,) As Double ' Invalid
            ~~~~~~
BC42024: Unused local variable: 'arr15'.
        Dim arr15#(,) As Double ' Invalid
            ~~~~~~
]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30303ERR_TypeCharOnSub()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeCharOnSub">
        <file name="a.vb"><![CDATA[
        Interface I1
             Sub x%()
             Sub x#()
             Sub x@()
             Sub x!()
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30269: 'Sub x()' has multiple definitions with identical signatures.
             Sub x%()
                 ~~
BC30303: Type character cannot be used in a 'Sub' declaration because a 'Sub' doesn't return a value.
             Sub x%()
                 ~~
BC30269: 'Sub x()' has multiple definitions with identical signatures.
             Sub x#()
                 ~~
BC30303: Type character cannot be used in a 'Sub' declaration because a 'Sub' doesn't return a value.
             Sub x#()
                 ~~
BC30269: 'Sub x()' has multiple definitions with identical signatures.
             Sub x@()
                 ~~
BC30303: Type character cannot be used in a 'Sub' declaration because a 'Sub' doesn't return a value.
             Sub x@()
                 ~~
BC30303: Type character cannot be used in a 'Sub' declaration because a 'Sub' doesn't return a value.
             Sub x!()
                 ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30305ERR_PartialMethodDefaultParameterValue2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            'COMPILEERROR: BC30305, "Goo6"
            Partial Private Sub Goo6(Optional ByVal x As Integer = 1)
            End Sub
            Private Sub Goo6(Optional ByVal x As Integer = 2)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC37203: Optional parameter of a method 'Private Sub Goo6([x As Integer = 2])' does not have the same default value as the corresponding parameter of the partial method 'Private Sub Goo6([x As Integer = 1])'.
            Private Sub Goo6(Optional ByVal x As Integer = 2)
                        ~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30305ERR_OverloadWithDefault2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            'COMPILEERROR: BC30305, "Goo6"
            Partial Private Sub Goo6(Optional ByVal x As Integer = 1)
            End Sub
            Sub Goo6(Optional ByVal x As Integer = 2)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31441: Method 'Goo6' must be declared 'Private' in order to implement partial method 'Goo6'.
            Sub Goo6(Optional ByVal x As Integer = 2)
                ~~~~
BC37203: Optional parameter of a method 'Public Sub Goo6([x As Integer = 2])' does not have the same default value as the corresponding parameter of the partial method 'Private Sub Goo6([x As Integer = 1])'.
            Sub Goo6(Optional ByVal x As Integer = 2)
                ~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(ParamArray x() As Integer)
            End Sub
            Private Sub Goo6(x() As Integer)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC37204: Parameter of a method 'Private Sub Goo6(x As Integer())' differs by ParamArray modifier from the corresponding parameter of the partial method 'Private Sub Goo6(ParamArray x As Integer())'.
            Private Sub Goo6(x() As Integer)
                        ~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(x() As Integer)
            End Sub
            Private Sub Goo6(ParamArray x() As Integer)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC37204: Parameter of a method 'Private Sub Goo6(ParamArray x As Integer())' differs by ParamArray modifier from the corresponding parameter of the partial method 'Private Sub Goo6(x As Integer())'.
            Private Sub Goo6(ParamArray x() As Integer)
                        ~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ''' <remarks>Only use with PEModuleSymbol and PEParameterSymbol</remarks>
        Private Sub AssertHasExactlyOneParamArrayAttribute(m As ModuleSymbol, paramSymbol As ParameterSymbol)
            Dim peModule = DirectCast(m, PEModuleSymbol).Module
            Dim paramHandle = DirectCast(paramSymbol, PEParameterSymbol).Handle

            Assert.Equal(1, peModule.GetParamArrayCountOrThrow(paramHandle))
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(<System.ParamArray()> x() As Integer)
            End Sub
            Private Sub Goo6(x() As Integer)
            End Sub

            Sub Use()
                Goo6()
            End Sub
        End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation1,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim Cls30305 = m.GlobalNamespace.GetTypeMember("Cls30305")
                                                  Dim Goo6 = Cls30305.GetMember(Of MethodSymbol)("Goo6")
                                                  Dim GooParam = Goo6.Parameters(0)
                                                  Assert.Equal(0, GooParam.GetAttributes().Length)
                                                  Assert.True(GooParam.IsParamArray)
                                                  AssertHasExactlyOneParamArrayAttribute(m, GooParam)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(x() As Integer)
            End Sub
            Private Sub Goo6(<System.ParamArray()> x() As Integer)
            End Sub

            Sub Use()
                Goo6()
            End Sub
        End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation1,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim Cls30305 = m.GlobalNamespace.GetTypeMember("Cls30305")
                                                  Dim Goo6 = Cls30305.GetMember(Of MethodSymbol)("Goo6")
                                                  Dim GooParam = Goo6.Parameters(0)
                                                  Assert.Equal(0, GooParam.GetAttributes().Length)
                                                  Assert.True(GooParam.IsParamArray)
                                                  AssertHasExactlyOneParamArrayAttribute(m, GooParam)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(ParamArray x() As Integer)
            End Sub
            Private Sub Goo6(<System.ParamArray()> x() As Integer)
            End Sub

            Sub Use()
                Goo6()
            End Sub
        End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation1,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim Cls30305 = m.GlobalNamespace.GetTypeMember("Cls30305")
                                                  Dim Goo6 = Cls30305.GetMember(Of MethodSymbol)("Goo6")
                                                  Dim GooParam = Goo6.Parameters(0)
                                                  Assert.Equal(0, GooParam.GetAttributes().Length)
                                                  Assert.True(GooParam.IsParamArray)
                                                  AssertHasExactlyOneParamArrayAttribute(m, GooParam)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_6()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(<System.ParamArray()> x() As Integer)
            End Sub
            Private Sub Goo6(ParamArray x() As Integer)
            End Sub

            Sub Use()
                Goo6()
            End Sub
        End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation1,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim Cls30305 = m.GlobalNamespace.GetTypeMember("Cls30305")
                                                  Dim Goo6 = Cls30305.GetMember(Of MethodSymbol)("Goo6")
                                                  Dim GooParam = Goo6.Parameters(0)
                                                  Assert.Equal(0, GooParam.GetAttributes().Length)
                                                  Assert.True(GooParam.IsParamArray)
                                                  AssertHasExactlyOneParamArrayAttribute(m, GooParam)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_7()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(<System.ParamArray()> x() As Integer)
            End Sub
            Private Sub Goo6(<System.ParamArray()> x() As Integer)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30663: Attribute 'ParamArrayAttribute' cannot be applied multiple times.
            Private Sub Goo6(<System.ParamArray()> x() As Integer)
                              ~~~~~~~~~~~~~~~~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_8()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(ParamArray x() As Integer)
            End Sub
            Private Sub Goo6(ParamArray x() As Integer)
            End Sub

            Sub Use()
                Goo6()
            End Sub
        End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation1,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim Cls30305 = m.GlobalNamespace.GetTypeMember("Cls30305")
                                                  Dim Goo6 = Cls30305.GetMember(Of MethodSymbol)("Goo6")
                                                  Dim GooParam = Goo6.Parameters(0)
                                                  Assert.Equal(0, GooParam.GetAttributes().Length)
                                                  Assert.True(GooParam.IsParamArray)
                                                  AssertHasExactlyOneParamArrayAttribute(m, GooParam)
                                              End Sub)
        End Sub

        <Fact>
        Public Sub ERR_PartialMethodParamArrayMismatch2_9()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30305
            Partial Private Sub Goo6(x() As Integer)
            End Sub
            Private Sub Goo6(x() As Integer)
            End Sub

            Sub Use()
                Goo6(Nothing)
            End Sub
        End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation1,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim Cls30305 = m.GlobalNamespace.GetTypeMember("Cls30305")
                                                  Dim Goo6 = Cls30305.GetMember(Of MethodSymbol)("Goo6")
                                                  Dim GooParam = Goo6.Parameters(0)
                                                  Assert.Equal(0, GooParam.GetAttributes().Length)
                                                  Assert.False(GooParam.IsParamArray)
                                              End Sub)
        End Sub

        <Fact()>
        Public Sub BC30307ERR_OverrideWithDefault2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OverloadWithDefault2">
        <file name="a.vb"><![CDATA[
        Module mod30307
            Class Class1
                Overridable Sub Scen1(Optional ByVal x As String = "bob")
                End Sub
                Overridable Sub Scen2(Optional ByVal x As Object = "bob")
                End Sub
                Overridable Function Scen3(Optional ByVal x As Integer = 3)
                End Function
                Overridable Function Scen4(Optional ByVal x As Integer = 3)
                End Function
            End Class
            Class class2
                Inherits Class1
                'COMPILEERROR:  BC30307, "Scen1"
                Overrides Sub Scen1(Optional ByVal x As String = "BOB")
                End Sub
                'COMPILEERROR:  BC30307, "Scen2"
                Overrides Sub Scen2(Optional ByVal x As Object = "BOB")
                End Sub
                Overrides Function Scen3(Optional ByVal x As Integer = 2 + 1)
                End Function
                'COMPILEERROR:  BC30307, "Scen4"
                Overrides Function Scen4(Optional ByVal x As Integer = 4)
                End Function
            End Class
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30307: 'Public Overrides Sub Scen1([x As String = "BOB"])' cannot override 'Public Overridable Sub Scen1([x As String = "bob"])' because they differ by the default values of optional parameters.
                Overrides Sub Scen1(Optional ByVal x As String = "BOB")
                              ~~~~~
BC30307: 'Public Overrides Sub Scen2([x As Object = "BOB"])' cannot override 'Public Overridable Sub Scen2([x As Object = "bob"])' because they differ by the default values of optional parameters.
                Overrides Sub Scen2(Optional ByVal x As Object = "BOB")
                              ~~~~~
BC30307: 'Public Overrides Function Scen4([x As Integer = 4]) As Object' cannot override 'Public Overridable Function Scen4([x As Integer = 3]) As Object' because they differ by the default values of optional parameters.
                Overrides Function Scen4(Optional ByVal x As Integer = 4)
                                   ~~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30308ERR_OverrideWithOptional2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OverrideWithOptional2">
        <file name="a.vb"><![CDATA[
        Module mod30308
            Class Class1
                Overridable Sub Scen1(Optional ByVal x As String = "bob")
                End Sub
                Overridable Sub Scen2(Optional ByVal x As Object = "bob")
                End Sub
                Overridable Function Scen3(Optional ByVal x As Integer = 3)
                End Function
                Overridable Function Scen4(Optional ByVal x As Integer = 3)
                End Function
            End Class
            Class class2
                Inherits Class1
                'COMPILEERROR:  BC30308, "Scen1"
                Overrides Sub Scen1(ByVal x As String)
                End Sub
                'COMPILEERROR:  BC30308, "Scen2"
                Overrides Sub Scen2(ByVal x As Object)
                End Sub
                Overrides Function Scen3(ByVal x As Integer)
                End Function
                'COMPILEERROR:  BC30308, "Scen4"
                Overrides Function Scen4(ByVal x As Integer)
                End Function
            End Class
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
 BC30308: 'Public Overrides Sub Scen1(x As String)' cannot override 'Public Overridable Sub Scen1([x As String = "bob"])' because they differ by optional parameters.
                Overrides Sub Scen1(ByVal x As String)
                              ~~~~~
BC30308: 'Public Overrides Sub Scen2(x As Object)' cannot override 'Public Overridable Sub Scen2([x As Object = "bob"])' because they differ by optional parameters.
                Overrides Sub Scen2(ByVal x As Object)
                              ~~~~~
BC30308: 'Public Overrides Function Scen3(x As Integer) As Object' cannot override 'Public Overridable Function Scen3([x As Integer = 3]) As Object' because they differ by optional parameters.
                Overrides Function Scen3(ByVal x As Integer)
                                   ~~~~~
BC30308: 'Public Overrides Function Scen4(x As Integer) As Object' cannot override 'Public Overridable Function Scen4([x As Integer = 3]) As Object' because they differ by optional parameters.
                Overrides Function Scen4(ByVal x As Integer)
                                   ~~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30345ERR_OverloadWithByref2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithByref2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30345
            Public overloads Sub Goo1(ByVal x as Integer) ' 1
            End Sub
            Public overloads Sub Goo1(ByRef x as Integer) ' 2
            End Sub

            Public overloads Function Goo2(ByVal x as Integer) as Integer' 1
              return 1
            End Function
            Public overloads Function Goo2(ByRef x as Integer) as Decimal' 2
              return 2.2
            End Function

            Public Shared Sub Main()
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30345: 'Public Overloads Sub Goo1(x As Integer)' and 'Public Overloads Sub Goo1(ByRef x As Integer)' cannot overload each other because they differ only by parameters declared 'ByRef' or 'ByVal'.
            Public overloads Sub Goo1(ByVal x as Integer) ' 1
                                 ~~~~
BC30301: 'Public Overloads Function Goo2(x As Integer) As Integer' and 'Public Overloads Function Goo2(ByRef x As Integer) As Decimal' cannot overload each other because they differ only by return types.
            Public overloads Function Goo2(ByVal x as Integer) as Integer' 1
                                      ~~~~
BC30345: 'Public Overloads Function Goo2(x As Integer) As Integer' and 'Public Overloads Function Goo2(ByRef x As Integer) As Decimal' cannot overload each other because they differ only by parameters declared 'ByRef' or 'ByVal'.
            Public overloads Function Goo2(ByVal x as Integer) as Integer' 1
                                      ~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30345ERR_OverloadWithByref2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithByref2">
        <file name="a.vb"><![CDATA[
        Public Class Cls30345
            'COMPILEERROR: BC30345, "Goo"
            Partial Private Sub Goo(Optional ByVal x As Integer = 2)
            End Sub
            Sub Goo(Optional ByRef x As Integer = 2)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30345: 'Private Sub Goo([x As Integer = 2])' and 'Public Sub Goo([ByRef x As Integer = 2])' cannot overload each other because they differ only by parameters declared 'ByRef' or 'ByVal'.
            Partial Private Sub Goo(Optional ByVal x As Integer = 2)
                                ~~~                                      
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30354ERR_InheritsFromNonInterface()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InheritsFromNonInterface">
        <file name="a.vb"><![CDATA[
        Module M1
            Interface I1
                 Inherits System.Enum
            End Interface
            Interface I2
                Inherits Scen1
            End Interface
            Interface I3
                Inherits System.Exception
            End Interface
            NotInheritable Class Scen1
                End Class
            END Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30354: Interface can inherit only from another interface.
                 Inherits System.Enum
                          ~~~~~~~~~~~
BC30354: Interface can inherit only from another interface.
                Inherits Scen1
                         ~~~~~
BC30354: Interface can inherit only from another interface.
                Inherits System.Exception
                         ~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30359ERR_DuplicateDefaultProps1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Interface I
            Default Property P(o)
            ReadOnly Property Q(o)
            Default WriteOnly Property R(o) ' BC30359
            Default Property P(x, y)
            Default Property Q(x, y) ' BC30359
            Property R(x, y)
        End Interface
        Class C
            Default Property P(o)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            ReadOnly Property Q(o)
                Get
                    Return Nothing
                End Get
            End Property
            Default WriteOnly Property R(o) ' BC30359
                Set(value)
                End Set
            End Property
            Default Property P(x, y)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Default Property Q(x, y) ' BC30359
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Property R(x, y)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
        End Class
        Structure S
            Default Property P(o)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            ReadOnly Property Q(o)
                Get
                    Return Nothing
                End Get
            End Property
            Default WriteOnly Property R(o) ' BC30359
                Set(value)
                End Set
            End Property
            Default Property P(x, y)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Default Property Q(x, y) ' BC30359
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Property R(x, y)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
        End Structure
        ]]></file>
    </compilation>)
            compilation1.AssertTheseDeclarationDiagnostics(<errors><![CDATA[
BC30359: 'Default' can be applied to only one property name in a interface.
            Default WriteOnly Property R(o) ' BC30359
                                       ~
BC30359: 'Default' can be applied to only one property name in a interface.
            Default Property Q(x, y) ' BC30359
                             ~
BC30359: 'Default' can be applied to only one property name in a class.
            Default WriteOnly Property R(o) ' BC30359
                                       ~
BC30359: 'Default' can be applied to only one property name in a class.
            Default Property Q(x, y) ' BC30359
                             ~
BC30359: 'Default' can be applied to only one property name in a structure.
            Default WriteOnly Property R(o) ' BC30359
                                       ~
BC30359: 'Default' can be applied to only one property name in a structure.
            Default Property Q(x, y) ' BC30359
                             ~
     ]]></errors>)
        End Sub

        ' Property names with different case.
        <Fact>
        Public Sub BC30359ERR_DuplicateDefaultProps1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
    Default ReadOnly Property P(o)
    Default WriteOnly Property p(x, y)
End Interface
Class C
    Default ReadOnly Property q(o)
        Get
            Return Nothing
        End Get
    End Property
    Default WriteOnly Property Q(x, y)
        Set(value)
        End Set
    End Property
End Class
Structure S
    Default ReadOnly Property R(o)
        Get
            Return Nothing
        End Get
    End Property
    Default WriteOnly Property r(x, y)
        Set(value)
        End Set
    End Property
End Structure
        ]]></file>
    </compilation>)
            compilation1.AssertNoErrors()
        End Sub

        <Fact>
        Public Sub BC30361ERR_DefaultMissingFromProperty2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DefaultMissingFromProperty2">
        <file name="a.vb"><![CDATA[
        MustInherit Class C
            Default Overridable Overloads ReadOnly Property P(x, y)
                Get
                    Return Nothing
                End Get
            End Property
            MustOverride Overloads Property P
        End Class
        Interface I
            Overloads WriteOnly Property Q(o)
            Overloads ReadOnly Property Q
            Default Overloads Property Q(x, y)
        End Interface
        Structure S
            ReadOnly Property R(x)
                Get
                    Return Nothing
                End Get
            End Property
            Default ReadOnly Property R(x, y)
                Get
                    Return Nothing
                End Get
            End Property
            Default ReadOnly Property R(x, y, z)
                Get
                    Return Nothing
                End Get
            End Property
        End Structure
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30361: 'Public Overridable Overloads ReadOnly Default Property P(x As Object, y As Object) As Object' and 'Public MustOverride Overloads Property P As Object' cannot overload each other because only one is declared 'Default'.
            MustOverride Overloads Property P
                                            ~
BC30361: 'Default Property Q(x As Object, y As Object) As Object' and 'WriteOnly Property Q(o As Object) As Object' cannot overload each other because only one is declared 'Default'.
            Overloads WriteOnly Property Q(o)
                                         ~
BC30361: 'Default Property Q(x As Object, y As Object) As Object' and 'ReadOnly Property Q As Object' cannot overload each other because only one is declared 'Default'.
            Overloads ReadOnly Property Q
                                        ~
BC30361: 'Public ReadOnly Default Property R(x As Object, y As Object) As Object' and 'Public ReadOnly Property R(x As Object) As Object' cannot overload each other because only one is declared 'Default'.
            ReadOnly Property R(x)
                              ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Property names with different case.
        <Fact>
        Public Sub BC30361ERR_DefaultMissingFromProperty2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DefaultMissingFromProperty2">
        <file name="a.vb"><![CDATA[
Interface I
    Default Overloads ReadOnly Property P(o)
    Overloads WriteOnly Property p(x, y)
End Interface
Class C
    Overloads ReadOnly Property q(o)
        Get
            Return Nothing
        End Get
    End Property
    Default Overloads WriteOnly Property Q(x, y)
        Set(value)
        End Set
    End Property
End Class
Structure S
    Overloads ReadOnly Property R(o)
        Get
            Return Nothing
        End Get
    End Property
    Default Overloads WriteOnly Property r(x, y)
        Set(value)
        End Set
    End Property
End Structure
        ]]></file>
    </compilation>)
            compilation1.AssertTheseDeclarationDiagnostics(<errors><![CDATA[
BC30361: 'ReadOnly Default Property P(o As Object) As Object' and 'WriteOnly Property p(x As Object, y As Object) As Object' cannot overload each other because only one is declared 'Default'.
    Overloads WriteOnly Property p(x, y)
                                 ~
BC30361: 'Public Overloads WriteOnly Default Property Q(x As Object, y As Object) As Object' and 'Public Overloads ReadOnly Property q(o As Object) As Object' cannot overload each other because only one is declared 'Default'.
    Overloads ReadOnly Property q(o)
                                ~
BC30361: 'Public Overloads WriteOnly Default Property r(x As Object, y As Object) As Object' and 'Public Overloads ReadOnly Property R(o As Object) As Object' cannot overload each other because only one is declared 'Default'.
    Overloads ReadOnly Property R(o)
                                ~
     ]]></errors>)
        End Sub

        <Fact>
        Public Sub BC30362ERR_OverridingPropertyKind2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverridingPropertyKind2">
        <file name="a.vb"><![CDATA[
        Public Class Class1
            Dim xprop11, xprop12
            Public Overridable ReadOnly Property prop10()
                Get
                    Return "Class1 prop10"
                End Get
            End Property
            Public Overridable WriteOnly Property prop11()
                Set(ByVal Value)
                    xprop11 = Value
                End Set
            End Property
            Public Overridable Property prop12()
                Get
                    prop12 = xprop12
                End Get
                Set(ByVal Value)
                    xprop12 = Value
                End Set
            End Property
        End Class
        Public Class Class2
            Inherits Class1
            'COMPILEERROR: BC30362, "prop10"
            Overrides WriteOnly Property prop10()
                Set(ByVal Value)
                End Set
            End Property
            'COMPILEERROR: BC30362, "prop11"
            Overrides ReadOnly Property prop11()
                Get
                End Get
            End Property
            'COMPILEERROR: BC30362, "prop12"
            Overrides ReadOnly Property prop12()
                Get
                End Get
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30362: 'Public Overrides WriteOnly Property prop10 As Object' cannot override 'Public Overridable ReadOnly Property prop10 As Object' because they differ by 'ReadOnly' or 'WriteOnly'.
            Overrides WriteOnly Property prop10()
                                         ~~~~~~
BC30362: 'Public Overrides ReadOnly Property prop11 As Object' cannot override 'Public Overridable WriteOnly Property prop11 As Object' because they differ by 'ReadOnly' or 'WriteOnly'.
            Overrides ReadOnly Property prop11()
                                        ~~~~~~
BC30362: 'Public Overrides ReadOnly Property prop12 As Object' cannot override 'Public Overridable Property prop12 As Object' because they differ by 'ReadOnly' or 'WriteOnly'.
            Overrides ReadOnly Property prop12()
                                        ~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30362ERR_OverridingPropertyKind3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="OverridingPropertyKind3">
        <file name="a.vb"><![CDATA[
    Class D3
        Inherits D2

        Public Overrides Property P_rw_rw_r As Integer
            Get
                Return MyBase.P_rw_rw_r
            End Get
            Set(value As Integer)
                MyBase.P_rw_r_w = value
            End Set
        End Property

        Public Overrides Property P_rw_rw_w As Integer
            Get
                Return MyBase.P_rw_rw_r
            End Get
            Set(value As Integer)
                MyBase.P_rw_rw_w = value
            End Set
        End Property

    End Class
        ]]></file>
    </compilation>, ClassesWithReadWriteProperties)
            Dim expectedErrors1 = <errors><![CDATA[
BC30362: 'Public Overrides Property P_rw_rw_r As Integer' cannot override 'Public Overrides ReadOnly Property P_rw_rw_r As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
        Public Overrides Property P_rw_rw_r As Integer
                                  ~~~~~~~~~
BC30362: 'Public Overrides Property P_rw_rw_w As Integer' cannot override 'Public Overrides WriteOnly Property P_rw_rw_w As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
        Public Overrides Property P_rw_rw_w As Integer
                                  ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30364ERR_BadFlagsOnNew1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadFlagsOnNew1">
        <file name="a.vb"><![CDATA[
        Class S1
            overridable SUB NEW()
            end sub
        End Class 
        Class S2
            Shadows SUB NEW()
            end sub
        End Class 
        Class S3
            MustOverride SUB NEW()
        End Class 
        Class S4
            notoverridable SUB NEW()
            end sub
        End Class 
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30364: 'Sub New' cannot be declared 'overridable'.
            overridable SUB NEW()
            ~~~~~~~~~~~
BC30364: 'Sub New' cannot be declared 'Shadows'.
            Shadows SUB NEW()
            ~~~~~~~
BC30364: 'Sub New' cannot be declared 'MustOverride'.
            MustOverride SUB NEW()
            ~~~~~~~~~~~~
BC30364: 'Sub New' cannot be declared 'notoverridable'.
            notoverridable SUB NEW()
            ~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30366ERR_OverloadingPropertyKind2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadingPropertyKind2">
        <file name="a.vb"><![CDATA[
        Class Cls30366(Of T)
            ' COMPILEERROR: BC30366, "P1"
            ReadOnly Property P1() As T
                Get
                End Get
            End Property
            ' COMPILEERROR: BC30366, "P2"
            WriteOnly Property P2() As T
                Set(ByVal Value As T)
                End Set
            End Property
            Default Property P3(ByVal i As Integer) As T
                Get
                End Get
                Set(ByVal Value As T)
                End Set
            End Property
        End Class
        Partial Class Cls30366(Of T)
            WriteOnly Property P1() As T
                Set(ByVal Value As T)
                End Set
            End Property
            ReadOnly Property P2() As T
                Get
                End Get
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30366: 'Public ReadOnly Property P1 As T' and 'Public WriteOnly Property P1 As T' cannot overload each other because they differ only by 'ReadOnly' or 'WriteOnly'.
            ReadOnly Property P1() As T
                              ~~
BC30366: 'Public WriteOnly Property P2 As T' and 'Public ReadOnly Property P2 As T' cannot overload each other because they differ only by 'ReadOnly' or 'WriteOnly'.
            WriteOnly Property P2() As T
                               ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30368ERR_OverloadWithArrayVsParamArray2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithArrayVsParamArray2">
        <file name="a.vb"><![CDATA[
        Class Cls30368_1(Of T)
            Sub goo(ByVal p() As String)
            End Sub
            Sub goo(ByVal ParamArray v() As String)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30368: 'Public Sub goo(p As String())' and 'Public Sub goo(ParamArray v As String())' cannot overload each other because they differ only by parameters declared 'ParamArray'.
            Sub goo(ByVal p() As String)
                ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30368ERR_OverloadWithArrayVsParamArray2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithArrayVsParamArray2">
        <file name="a.vb"><![CDATA[
        Class Cls30368_1(Of T)
            Sub goo(ByVal p() As String)
            End Sub
            Sub goo(ByVal ParamArray v() As String)
            End Sub
        End Class
        Class Cls30368(Of T)
            Overloads Property Goo1(ByVal x()) As String
                Get
                    Goo1 = "get: VariantArray"
                End Get
                Set(ByVal Value As String)
                End Set
            End Property
            Overloads Property Goo1(ByVal ParamArray x()) As String
                Get
                    Goo1 = "get: ParamArray"
                End Get
                Set(ByVal Value As String)
                End Set
            End Property
            Overloads Property Goo2(ByVal x()) As String
                Get
                    Goo2 = "get: FixedArray"
                End Get
                Set(ByVal Value As String)
                End Set
            End Property
            Overloads Property Goo2(ByVal ParamArray x()) As String
                Get
                    Goo2 = "get: ParamArray"
                End Get
                Set(ByVal Value As String)
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30368: 'Public Sub goo(p As String())' and 'Public Sub goo(ParamArray v As String())' cannot overload each other because they differ only by parameters declared 'ParamArray'.
            Sub goo(ByVal p() As String)
                ~~~
BC30368: 'Public Overloads Property Goo1(x As Object()) As String' and 'Public Overloads Property Goo1(ParamArray x As Object()) As String' cannot overload each other because they differ only by parameters declared 'ParamArray'.
            Overloads Property Goo1(ByVal x()) As String
                               ~~~~
BC30368: 'Public Overloads Property Goo2(x As Object()) As String' and 'Public Overloads Property Goo2(ParamArray x As Object()) As String' cannot overload each other because they differ only by parameters declared 'ParamArray'.
            Overloads Property Goo2(ByVal x()) As String
                               ~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30371ERR_ModuleAsType1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module M1
End Module
Module M2
End Module
Module M3
End Module
Interface IA(Of T As M1)
End Interface
Interface IB
    Sub M(Of T As M2)()
End Interface
Interface IC(Of T)
End Interface
Class C
    Shared Sub M(o As IC(Of M3))
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30371: Module 'M1' cannot be used as a type.
Interface IA(Of T As M1)
                     ~~
BC30371: Module 'M2' cannot be used as a type.
    Sub M(Of T As M2)()
                  ~~
BC30371: Module 'M3' cannot be used as a type.
    Shared Sub M(o As IC(Of M3))
                            ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30371ERR_ModuleAsType1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
            Module M1
                Class C1
                    Function goo As M1
                        Return Nothing
                    End Function
                End Class
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30371: Module 'M1' cannot be used as a type.
                    Function goo As M1
                                    ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30385ERR_BadDelegateFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadDelegateFlags1">
        <file name="a.vb"><![CDATA[
            Class C1
                MustOverride Delegate Sub Del1()
                NotOverridable Delegate Sub Del2()
                Shared Delegate Sub Del3()
                Overrides Delegate Sub Del4()
                Overloads Delegate Sub Del5(ByRef y As Integer)
                Partial Delegate Sub Del6()
                Default Delegate Sub Del7()
                ReadOnly Delegate Sub Del8()
                WriteOnly Delegate Sub Del9()
                MustInherit Delegate Sub Del10()
                NotInheritable Delegate Sub Del11()
                Widening Delegate Sub Del12()
                Narrowing Delegate Sub Del13()
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30385: 'MustOverride' is not valid on a Delegate declaration.
                MustOverride Delegate Sub Del1()
                ~~~~~~~~~~~~
BC30385: 'NotOverridable' is not valid on a Delegate declaration.
                NotOverridable Delegate Sub Del2()
                ~~~~~~~~~~~~~~
BC30385: 'Shared' is not valid on a Delegate declaration.
                Shared Delegate Sub Del3()
                ~~~~~~
BC30385: 'Overrides' is not valid on a Delegate declaration.
                Overrides Delegate Sub Del4()
                ~~~~~~~~~
BC30385: 'Overloads' is not valid on a Delegate declaration.
                Overloads Delegate Sub Del5(ByRef y As Integer)
                ~~~~~~~~~
BC30385: 'Partial' is not valid on a Delegate declaration.
                Partial Delegate Sub Del6()
                ~~~~~~~
BC30385: 'Default' is not valid on a Delegate declaration.
                Default Delegate Sub Del7()
                ~~~~~~~
BC30385: 'ReadOnly' is not valid on a Delegate declaration.
                ReadOnly Delegate Sub Del8()
                ~~~~~~~~
BC30385: 'WriteOnly' is not valid on a Delegate declaration.
                WriteOnly Delegate Sub Del9()
                ~~~~~~~~~
BC30385: 'MustInherit' is not valid on a Delegate declaration.
                MustInherit Delegate Sub Del10()
                ~~~~~~~~~~~
BC30385: 'NotInheritable' is not valid on a Delegate declaration.
                NotInheritable Delegate Sub Del11()
                ~~~~~~~~~~~~~~
BC30385: 'Widening' is not valid on a Delegate declaration.
                Widening Delegate Sub Del12()
                ~~~~~~~~
BC30385: 'Narrowing' is not valid on a Delegate declaration.
                Narrowing Delegate Sub Del13()
                ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(538884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538884")>
        <Fact>
        Public Sub BC30385ERR_BadDelegateFlags1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadDelegateFlags1">
        <file name="a.vb"><![CDATA[
            Structure S1
                MustOverride Delegate Sub Del1()
                NotOverridable Delegate Sub Del1()
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30385: 'MustOverride' is not valid on a Delegate declaration.
                MustOverride Delegate Sub Del1()
                ~~~~~~~~~~~~
BC30385: 'NotOverridable' is not valid on a Delegate declaration.
                NotOverridable Delegate Sub Del1()
                ~~~~~~~~~~~~~~
BC30179: delegate Class 'Del1' and delegate Class 'Del1' conflict in structure 'S1'.
                NotOverridable Delegate Sub Del1()
                                            ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30389ERR_InaccessibleSymbol2_AccessCheckCrossAssemblyDerived()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="AccessCheckCrossAssemblyDerived1">
    <file name="a.vb"><![CDATA[
Public Class C
    Public Shared c_pub As Integer
    Friend Shared c_int As Integer
    Protected Shared c_pro As Integer
    Protected Friend Shared c_intpro As Integer
    Private Shared c_priv As Integer
End Class

Friend Class D
    Public Shared d_pub As Integer
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertNoErrors(other)

            Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssemblyDerived2">
    <file name="a.vb"><![CDATA[
Public Class A
    Inherits C

    Public Sub m()
        Dim aa As Integer = C.c_pub
        Dim bb As Integer = C.c_int
        Dim cc As Integer = C.c_pro
        Dim dd As Integer = C.c_intpro
        Dim ee As Integer = C.c_priv
        Dim ff As Integer = D.d_pub
    End Sub
End Class
    ]]></file>
</compilation>,
                {New VisualBasicCompilationReference(other)})
            'BC30389:    'C.c_int' is not accessible in this context because it is 'Friend'.
            '        Dim bb As Integer = C.c_int
            '                            ~~~~~~~
            'BC30389:    'C.c_priv' is not accessible in this context because it is 'Private'.
            '       Dim ee As Integer = C.c_priv
            '                           ~~~~~~~~
            'BC30389:    'D' is not accessible in this context because it is 'Friend'.
            '        Dim ff As Integer = D.d_pub
            '                            ~
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_InaccessibleSymbol2, "C.c_int").WithArguments("C.c_int", "Friend"),
                                   Diagnostic(ERRID.ERR_InaccessibleSymbol2, "C.c_priv").WithArguments("C.c_priv", "Private"),
                                   Diagnostic(ERRID.ERR_InaccessibleSymbol2, "D").WithArguments("D", "Friend"))

        End Sub

        <Fact>
        Public Sub BC30395ERR_BadRecordFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ERR_BadRecordFlags1">
        <file name="a.vb"><![CDATA[
            MustOverride Structure Del1
            End Structure
            NotOverridable Structure Del2
            End Structure
            Shared Structure Del3
            End Structure
            Overrides Structure Del4
            End Structure
            Overloads Structure Del5
            End Structure
            Partial Structure Del6
            End Structure
            Default Structure Del7
            End Structure
            ReadOnly Structure Del8
            End Structure
            WriteOnly Structure Del9
            End Structure
            Widening Structure Del12
            End Structure
            Narrowing Structure Del13
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30395: 'MustOverride' is not valid on a Structure declaration.
MustOverride Structure Del1
~~~~~~~~~~~~
BC30395: 'NotOverridable' is not valid on a Structure declaration.
            NotOverridable Structure Del2
            ~~~~~~~~~~~~~~
BC30395: 'Shared' is not valid on a Structure declaration.
            Shared Structure Del3
            ~~~~~~
BC30395: 'Overrides' is not valid on a Structure declaration.
            Overrides Structure Del4
            ~~~~~~~~~
BC30395: 'Overloads' is not valid on a Structure declaration.
            Overloads Structure Del5
            ~~~~~~~~~
BC30395: 'Default' is not valid on a Structure declaration.
            Default Structure Del7
            ~~~~~~~
BC30395: 'ReadOnly' is not valid on a Structure declaration.
            ReadOnly Structure Del8
            ~~~~~~~~
BC30395: 'WriteOnly' is not valid on a Structure declaration.
            WriteOnly Structure Del9
            ~~~~~~~~~
BC30395: 'Widening' is not valid on a Structure declaration.
            Widening Structure Del12
            ~~~~~~~~
BC30395: 'Narrowing' is not valid on a Structure declaration.
            Narrowing Structure Del13
            ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30396ERR_BadEnumFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadEnumFlags1">
        <file name="a.vb"><![CDATA[
            MustOverride enum Del1
                male
            End enum
            NotOverridable enum Del2
                male
            End enum
            Shared enum Del3
                male
            End enum
            Overrides enum Del4
                male
            End enum
            Overloads enum Del5
                male
            End enum
            Partial enum Del6
                male
            End enum
            Default enum Del7
                male
            End enum
            ReadOnly enum Del8
                male
            End enum
            WriteOnly enum Del9
                male
            End enum
            Widening enum Del12
                male
            End enum
            Narrowing enum Del13
                male
            End enum
            MustInherit Enum Del14
                male
            End Enum
            NotInheritable Enum Del15
                male
            End Enum
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30396: 'MustOverride' is not valid on an Enum declaration.
MustOverride enum Del1
~~~~~~~~~~~~
BC30396: 'NotOverridable' is not valid on an Enum declaration.
            NotOverridable enum Del2
            ~~~~~~~~~~~~~~
BC30396: 'Shared' is not valid on an Enum declaration.
            Shared enum Del3
            ~~~~~~
BC30396: 'Overrides' is not valid on an Enum declaration.
            Overrides enum Del4
            ~~~~~~~~~
BC30396: 'Overloads' is not valid on an Enum declaration.
            Overloads enum Del5
            ~~~~~~~~~
BC30396: 'Partial' is not valid on an Enum declaration.
            Partial enum Del6
            ~~~~~~~
BC30396: 'Default' is not valid on an Enum declaration.
            Default enum Del7
            ~~~~~~~
BC30396: 'ReadOnly' is not valid on an Enum declaration.
            ReadOnly enum Del8
            ~~~~~~~~
BC30396: 'WriteOnly' is not valid on an Enum declaration.
            WriteOnly enum Del9
            ~~~~~~~~~
BC30396: 'Widening' is not valid on an Enum declaration.
            Widening enum Del12
            ~~~~~~~~
BC30396: 'Narrowing' is not valid on an Enum declaration.
            Narrowing enum Del13
            ~~~~~~~~~
BC30396: 'MustInherit' is not valid on an Enum declaration.
            MustInherit Enum Del14
            ~~~~~~~~~~~
BC30396: 'NotInheritable' is not valid on an Enum declaration.
            NotInheritable Enum Del15
            ~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30397ERR_BadInterfaceFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadInterfaceFlags1">
        <file name="a.vb"><![CDATA[
            Class C1
                MustOverride Interface Del1
                End Interface
                NotOverridable Interface Del2
                End Interface
                Shared Interface Del3
                End Interface
                Overrides Interface Del4
                End Interface
                Overloads Interface Del5
                End Interface
                Default Interface Del7
                End Interface
                ReadOnly Interface Del8
                End Interface
                WriteOnly Interface Del9
                End Interface
                Widening Interface Del12
                End Interface
                Narrowing Interface Del13
                End Interface
                MustInherit Interface Del14
                End Interface
                NotInheritable Interface Del15
                End Interface
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30397: 'MustOverride' is not valid on an Interface declaration.
                MustOverride Interface Del1
                ~~~~~~~~~~~~
BC30397: 'NotOverridable' is not valid on an Interface declaration.
                NotOverridable Interface Del2
                ~~~~~~~~~~~~~~
BC30397: 'Shared' is not valid on an Interface declaration.
                Shared Interface Del3
                ~~~~~~
BC30397: 'Overrides' is not valid on an Interface declaration.
                Overrides Interface Del4
                ~~~~~~~~~
BC30397: 'Overloads' is not valid on an Interface declaration.
                Overloads Interface Del5
                ~~~~~~~~~
BC30397: 'Default' is not valid on an Interface declaration.
                Default Interface Del7
                ~~~~~~~
BC30397: 'ReadOnly' is not valid on an Interface declaration.
                ReadOnly Interface Del8
                ~~~~~~~~
BC30397: 'WriteOnly' is not valid on an Interface declaration.
                WriteOnly Interface Del9
                ~~~~~~~~~
BC30397: 'Widening' is not valid on an Interface declaration.
                Widening Interface Del12
                ~~~~~~~~
BC30397: 'Narrowing' is not valid on an Interface declaration.
                Narrowing Interface Del13
                ~~~~~~~~~
BC30397: 'MustInherit' is not valid on an Interface declaration.
                MustInherit Interface Del14
                ~~~~~~~~~~~
BC30397: 'NotInheritable' is not valid on an Interface declaration.
                NotInheritable Interface Del15
                ~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30398ERR_OverrideWithByref2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OverrideWithByref2">
        <file name="a.vb"><![CDATA[
            Module mod30398
                Class Class1
                    Overridable Sub Scen1(ByRef x As String)
                    End Sub
                    Overridable Sub Scen2(ByRef x As Object)
                    End Sub
                    Overridable Function Scen3(ByRef x As Integer)
                    End Function
                    Overridable Function Scen4(ByRef x As Integer)
                    End Function
                End Class
                Class class2
                    Inherits Class1
                    'COMPILEERROR:  BC30398, "Scen1"
                    Overrides Sub Scen1(ByVal x As String)
                    End Sub
                    'COMPILEERROR:  BC30398, "Scen2"
                    Overrides Sub Scen2(ByVal x As Object)
                    End Sub
                    Overrides Function Scen3(ByVal x As Integer)
                    End Function
                    'COMPILEERROR:  BC30398, "Scen4"
                    Overrides Function Scen4(ByVal x As Integer)
                    End Function
                End Class
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30398: 'Public Overrides Sub Scen1(x As String)' cannot override 'Public Overridable Sub Scen1(ByRef x As String)' because they differ by a parameter that is marked as 'ByRef' versus 'ByVal'.
                    Overrides Sub Scen1(ByVal x As String)
                                  ~~~~~
BC30398: 'Public Overrides Sub Scen2(x As Object)' cannot override 'Public Overridable Sub Scen2(ByRef x As Object)' because they differ by a parameter that is marked as 'ByRef' versus 'ByVal'.
                    Overrides Sub Scen2(ByVal x As Object)
                                  ~~~~~
BC30398: 'Public Overrides Function Scen3(x As Integer) As Object' cannot override 'Public Overridable Function Scen3(ByRef x As Integer) As Object' because they differ by a parameter that is marked as 'ByRef' versus 'ByVal'.
                    Overrides Function Scen3(ByVal x As Integer)
                                       ~~~~~
BC30398: 'Public Overrides Function Scen4(x As Integer) As Object' cannot override 'Public Overridable Function Scen4(ByRef x As Integer) As Object' because they differ by a parameter that is marked as 'ByRef' versus 'ByVal'.
                    Overrides Function Scen4(ByVal x As Integer)
                                       ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30401ERR_IdentNotMemberOfInterface4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="IdentNotMemberOfInterface4">
        <file name="a.vb"><![CDATA[
Interface IA
    Function F() As Integer
    Property P As Object
End Interface
Interface IB
End Interface
Class A
    Implements IA
    Public Function F(o As Object) As Integer Implements IA.F
        Return Nothing
    End Function
    Public Property P As Boolean Implements IA.P
End Class
Class B
    Implements IB
    Public Function F() As Integer Implements IB.F
        Return Nothing
    End Function
    Public Property P As Boolean Implements IB.P
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30149: Class 'A' must implement 'Function F() As Integer' for interface 'IA'.
    Implements IA
               ~~
BC30149: Class 'A' must implement 'Property P As Object' for interface 'IA'.
    Implements IA
               ~~
BC30401: 'F' cannot implement 'F' because there is no matching function on interface 'IA'.
    Public Function F(o As Object) As Integer Implements IA.F
                                                         ~~~~
BC30401: 'P' cannot implement 'P' because there is no matching property on interface 'IA'.
    Public Property P As Boolean Implements IA.P
                                            ~~~~
BC30401: 'F' cannot implement 'F' because there is no matching function on interface 'IB'.
    Public Function F() As Integer Implements IB.F
                                              ~~~~
BC30401: 'P' cannot implement 'P' because there is no matching property on interface 'IB'.
    Public Property P As Boolean Implements IB.P
                                            ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30412ERR_WithEventsRequiresClass()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="WithEventsRequiresClass">
        <file name="a.vb"><![CDATA[
            Class ClsTest30412
                'COMPILEERROR: BC30412, "Field003WithEvents"
                Public WithEvents Field003WithEvents = {1, 2, 3}
            End Class
            Class ClsTest30412_2
                'COMPILEERROR: BC30412, "Field002WithEvents"
                Public WithEvents Field002WithEvents = New List(Of Integer) From {1, 2, 3}
            End Class
                    ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30412: 'WithEvents' variables must have an 'As' clause.
                Public WithEvents Field003WithEvents = {1, 2, 3}
                                  ~~~~~~~~~~~~~~~~~~
BC30412: 'WithEvents' variables must have an 'As' clause.
                Public WithEvents Field002WithEvents = New List(Of Integer) From {1, 2, 3}
                                  ~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30413ERR_WithEventsAsStruct()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="WithEventsAsStruct">
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Class A
End Class
Structure S
End Structure
Class C(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)
    WithEvents _i As I
    WithEvents _a As A
    WithEvents _s As S
    WithEvents _1 As T1
    WithEvents _2 As T2
    WithEvents _3 As T3
    WithEvents _4 As T4
    WithEvents _5 As T5
    WithEvents _6 As T6
    WithEvents _7 As T7
End Class
                    ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30413: 'WithEvents' variables can only be typed as classes, interfaces or type parameters with class constraints.
    WithEvents _s As S
               ~~
BC30413: 'WithEvents' variables can only be typed as classes, interfaces or type parameters with class constraints.
    WithEvents _1 As T1
               ~~
BC30413: 'WithEvents' variables can only be typed as classes, interfaces or type parameters with class constraints.
    WithEvents _3 As T3
               ~~
BC30413: 'WithEvents' variables can only be typed as classes, interfaces or type parameters with class constraints.
    WithEvents _4 As T4
               ~~
BC30413: 'WithEvents' variables can only be typed as classes, interfaces or type parameters with class constraints.
    WithEvents _5 As T5
               ~~
BC30413: 'WithEvents' variables can only be typed as classes, interfaces or type parameters with class constraints.
    WithEvents _7 As T7
               ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30433ERR_ModuleCantUseMethodSpecifier1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
        Module M
            Protected Sub M()
            End Sub
            Shared Sub N()
            End Sub
            MustOverride Sub O()
            Overridable Sub P()
            End Sub
            Overrides Sub Q()
            End Sub
            Shadows Sub R()
            End Sub
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30433: Methods in a Module cannot be declared 'Protected'.
            Protected Sub M()
            ~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Shared'.
            Shared Sub N()
            ~~~~~~
BC30433: Methods in a Module cannot be declared 'MustOverride'.
            MustOverride Sub O()
            ~~~~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Overridable'.
            Overridable Sub P()
            ~~~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Overrides'.
            Overrides Sub Q()
            ~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Shadows'.
            Shadows Sub R()
            ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30434ERR_ModuleCantUseEventSpecifier1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ModuleCantUseEventSpecifier1">
        <file name="a.vb"><![CDATA[
            Module Shdmod
                'COMPILEERROR: BC30434, "Shadows"
                Shadows Event testx()
            End Module
                    ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
            BC30434: Events in a Module cannot be declared 'Shadows'.
                Shadows Event testx()
                ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30435ERR_StructCantUseVarSpecifier1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Structure S
    Protected F
    Protected Property P
    Property Q
        Protected Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
    Property R
        Get
            Return Nothing
        End Get
        Protected Set(value)
        End Set
    End Property
End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30435: Members in a Structure cannot be declared 'Protected'.
    Protected F
    ~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected'.
    Protected Property P
    ~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected'.
        Protected Get
        ~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected'.
        Protected Set(value)
        ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30435ERR_StructCantUseVarSpecifier1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Structure S
    Protected Friend F
    Protected Friend Property P
    Property Q
        Protected Friend Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
    Property R
        Get
            Return Nothing
        End Get
        Protected Friend Set(value)
        End Set
    End Property
End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30435: Members in a Structure cannot be declared 'Protected Friend'.
    Protected Friend F
    ~~~~~~~~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected Friend'.
    Protected Friend Property P
    ~~~~~~~~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected Friend'.
        Protected Friend Get
        ~~~~~~~~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected Friend'.
        Protected Friend Set(value)
        ~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30435ERR_StructCantUseVarSpecifier1_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Structure S
    Property P
        Protected Get
            Return Nothing
        End Get
        Protected Friend Set(value)
        End Set
    End Property
End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30435: Members in a Structure cannot be declared 'Protected'.
        Protected Get
        ~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected Friend'.
        Protected Friend Set(value)
        ~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(531467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531467")>
        <Fact>
        Public Sub BC30435ERR_StructCantUseVarSpecifier1_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Structure ms
    Dim s As Integer
    Public Overridable Property p1()
        Get
            Return Nothing
        End Get
        Friend Set(ByVal Value)
        End Set
    End Property
    Public NotOverridable Property p2()
        Get
            Return Nothing
        End Get
        Friend Set(ByVal Value)
        End Set
    End Property
    Public MustOverride Property p3()
    Public Overridable Sub T1()
    End Sub
    Public NotOverridable Sub T2()
    End Sub
    Public MustOverride Sub T3()
End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30435: Members in a Structure cannot be declared 'Overridable'.
    Public Overridable Property p1()
           ~~~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'NotOverridable'.
    Public NotOverridable Property p2()
           ~~~~~~~~~~~~~~
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
    Public NotOverridable Property p2()
                                   ~~
BC30435: Members in a Structure cannot be declared 'MustOverride'.
    Public MustOverride Property p3()
           ~~~~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Overridable'.
    Public Overridable Sub T1()
           ~~~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'NotOverridable'.
    Public NotOverridable Sub T2()
           ~~~~~~~~~~~~~~
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
    Public NotOverridable Sub T2()
                              ~~
BC30435: Members in a Structure cannot be declared 'MustOverride'.
    Public MustOverride Sub T3()
           ~~~~~~~~~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30436ERR_ModuleCantUseMemberSpecifier1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ModuleCantUseMemberSpecifier1">
        <file name="a.vb"><![CDATA[
            Module m1
                Protected Enum myenum As Integer
                    one
                End Enum
            End Module
                    ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30735: Type in a Module cannot be declared 'Protected'.
                Protected Enum myenum As Integer
                ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30437ERR_InvalidOverrideDueToReturn2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Class base(Of t)
                'COMPILEERROR: BC30437, "toString"
                Overrides Function tostring() As t
                End Function
            End Class
                    ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30437: 'Public Overrides Function tostring() As t' cannot override 'Public Overridable Overloads Function ToString() As String' because they differ by their return types.
                Overrides Function tostring() As t
                                   ~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30438ERR_ConstantWithNoValue()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
            Imports System
            Namespace NS30438
                Class c1
                    'COMPILEERROR: BC30438,"c1"
                    Const c1 As UInt16
                End Class
                Structure s1
                    'COMPILEERROR: BC30438,"c1"
                    Const c1 As UInt16
                End Structure
                Friend Module USign_001mod
                    'COMPILEERROR: BC30438,"c1"
                    Const c1 As UInt16
                End Module
            End Namespace
                    ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
            BC30438: Constants must have a value.
                    Const c1 As UInt16
                          ~~
BC30438: Constants must have a value.
                    Const c1 As UInt16
                          ~~
BC30438: Constants must have a value.
                    Const c1 As UInt16
                          ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542127, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542127")>
        <Fact>
        Public Sub BC30438ERR_ConstantWithNoValue02()
            Dim source =
<compilation name="delegates">
    <file name="a.vb"><![CDATA[
Option strict on
imports system

Class C1
    Sub GOO()
        'COMPILEERROR: BC30438
        Const l6 As UInt16
        Const l7 as new UInt16
    End Sub
End Class
    ]]></file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected><![CDATA[
BC30438: Constants must have a value.
        Const l6 As UInt16
              ~~
BC30438: Constants must have a value.
        Const l7 as new UInt16
              ~~
BC30246: 'new' is not valid on a local constant declaration.
        Const l7 as new UInt16
                    ~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub BC30443ERR_DuplicatePropertyGet()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicatePropertyGet">
        <file name="a.vb"><![CDATA[
            Class C
                Property P
                    Get
                        Return Nothing
                    End Get
                    Get
                        Return Nothing
                    End Get
                    Set
                    End Set
                    Get
                        Return Nothing
                    End Get
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30443: 'Get' is already declared.
                    Get
                    ~~~
BC30443: 'Get' is already declared.
                    Get
                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30444ERR_DuplicatePropertySet()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicatePropertySet">
        <file name="a.vb"><![CDATA[
            Class C
                WriteOnly Property P
                    Set
                    End Set
                    Set
                    End Set
                    Set
                    End Set
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30444: 'Set' is already declared.
                    Set
                    ~~~
BC30444: 'Set' is already declared.
                    Set
                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        '    <Fact()>
        '    Public Sub BC30445ERR_ConstAggregate()
        '        Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
        '<compilation name="DuplicatePropertySet">
        '   <file name="a.vb"><![CDATA[
        '        Module Constant
        '            Const d() As Long = {1, 2}, e() As Long = {1, 2}
        '            Sub main()
        '            End Sub
        '        End Module
        '    ]]></file>
        '</compilation>)
        '        Dim expectedErrors1 = <errors><![CDATA[
        '        BC30445: 'Set' is already declared.
        '                    Set(ByRef value)
        '                                ~~~~~~
        '             ]]></errors>
        '        CompilationUtils.AssertTheseDeclarationErrors(compilation1, expectedErrors1)
        '    End Sub

        <Fact>
        Public Sub BC30461ERR_BadClassFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadClassFlags1">
        <file name="a.vb"><![CDATA[
            NotOverridable class  C1
            End class 
            Shared class  C2
            End class 
            Overrides class  C3
            End class 
            Overloads class  C4
            End class 
            Partial class  C5
            End class 
            Default class  C6
            End class 
            ReadOnly class  C7
            End class 
            WriteOnly class  C8
            End class 
            Widening class  C9
            End class 
            Narrowing class  C10
            End class 
            MustInherit class  C11
            End class 
            NotInheritable class  C12
            End class 
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30461: Classes cannot be declared 'NotOverridable'.
NotOverridable class  C1
~~~~~~~~~~~~~~
BC30461: Classes cannot be declared 'Shared'.
            Shared class  C2
            ~~~~~~
BC30461: Classes cannot be declared 'Overrides'.
            Overrides class  C3
            ~~~~~~~~~
BC30461: Classes cannot be declared 'Overloads'.
            Overloads class  C4
            ~~~~~~~~~
BC30461: Classes cannot be declared 'Default'.
            Default class  C6
            ~~~~~~~
BC30461: Classes cannot be declared 'ReadOnly'.
            ReadOnly class  C7
            ~~~~~~~~
BC30461: Classes cannot be declared 'WriteOnly'.
            WriteOnly class  C8
            ~~~~~~~~~
BC30461: Classes cannot be declared 'Widening'.
            Widening class  C9
            ~~~~~~~~
BC30461: Classes cannot be declared 'Narrowing'.
            Narrowing class  C10
            ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30467ERR_NonNamespaceOrClassOnImport2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicatePropertySet">
        <file name="a.vb"><![CDATA[
        'COMPILEERROR: BC30467, "ns1.intfc1"
        Imports ns1.Intfc1
        'COMPILEERROR: BC30467, "ns1.intfc2(Of String)"
        Imports ns1.Intfc2(Of String)
        Namespace ns1
            Public Interface Intfc1
                Sub Intfc1goo()
            End Interface
            Public Interface Intfc2(Of t)
                Inherits Intfc1
                Sub intfc2goo()
            End Interface
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30467: 'Intfc1' for the Imports 'Intfc1' does not refer to a Namespace, Class, Structure, Enum or Module.
        Imports ns1.Intfc1
                ~~~~~~~~~~
BC30467: 'Intfc2' for the Imports 'Intfc2(Of String)' does not refer to a Namespace, Class, Structure, Enum or Module.
        Imports ns1.Intfc2(Of String)
                ~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30468ERR_TypecharNotallowed()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypecharNotallowed">
        <file name="a.vb"><![CDATA[
        Module M1
            Function scen1() as System.Datetime@
            End Function
        End Module
        Structure S1
            Function scen1() as System.sTRING#
            End Function
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30468: Type declaration characters are not valid in this context.
            Function scen1() as System.Datetime@
                                       ~~~~~~~~~
BC30468: Type declaration characters are not valid in this context.
            Function scen1() as System.sTRING#
                                       ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30476ERR_EventSourceIsArray()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventSourceIsArray">
        <file name="a.vb"><![CDATA[
        Public Class Class1
            Class Class2
                Event Goo()
            End Class
            'COMPILEERROR: BC30591, "h(3)",BC30476, "Class2"
            Dim WithEvents h(3) As Class2
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30476: 'WithEvents' variables cannot be typed as arrays.
            Dim WithEvents h(3) As Class2
                           ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30479ERR_SharedConstructorWithParams()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SharedConstructorWithParams">
        <file name="a.vb"><![CDATA[
        Structure Struct1
            Shared Sub new(ByVal x As Integer)
            End Sub
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30479: Shared 'Sub New' cannot have any parameters.
            Shared Sub new(ByVal x As Integer)
                          ~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30480ERR_SharedConstructorIllegalSpec1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SharedConstructorIllegalSpec1">
        <file name="a.vb"><![CDATA[
        Structure Struct1
            Shared public Sub new()
            End Sub
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30480: Shared 'Sub New' cannot be declared 'public'.
            Shared public Sub new()
                   ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30490ERR_BadFlagsWithDefault1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadFlagsWithDefault1">
        <file name="a.vb"><![CDATA[
        Class A
            Default Private Property P(o)
                Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
        End Class
        Class B
            Private ReadOnly Default Property Q(x, y)
                Get
                    Return Nothing
                End Get
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30490: 'Default' cannot be combined with 'Private'.
            Default Private Property P(o)
                    ~~~~~~~
BC30490: 'Default' cannot be combined with 'Private'.
            Private ReadOnly Default Property Q(x, y)
            ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30497ERR_NewCannotHandleEvents()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NewCannotHandleEvents">
        <file name="a.vb"><![CDATA[
        Class Cla30497
            'COMPILEERROR: BC30497, "New"
            Sub New() Handles var1.event1
            End Sub
        End Class
        Class Cla30497_1
            'COMPILEERROR: BC30497, "New"
            Shared Sub New() Handles var1.event1
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30497: 'Sub New' cannot handle events.
            Sub New() Handles var1.event1
                ~~~
BC30497: 'Sub New' cannot handle events.
            Shared Sub New() Handles var1.event1
                       ~~~
]]></errors>
            CompilationUtils.AssertTheseParseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30501ERR_BadFlagsOnSharedMeth1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        MustInherit Class C
            Shared NotOverridable Function F()
                Return Nothing
            End Function
            Shared Overrides Function G()
                Return Nothing
            End Function
            Overridable Shared Sub M()
            End Sub
            MustOverride Shared Sub N()
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30501: 'Shared' cannot be combined with 'NotOverridable' on a method declaration.
            Shared NotOverridable Function F()
                   ~~~~~~~~~~~~~~
BC30501: 'Shared' cannot be combined with 'Overrides' on a method declaration.
            Shared Overrides Function G()
                   ~~~~~~~~~
BC30501: 'Shared' cannot be combined with 'Overridable' on a method declaration.
            Overridable Shared Sub M()
            ~~~~~~~~~~~
BC30501: 'Shared' cannot be combined with 'MustOverride' on a method declaration.
            MustOverride Shared Sub N()
            ~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528324")>
        <Fact>
        Public Sub BC30501ERR_BadFlagsOnSharedMeth2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Class A
    Public Overridable Sub G()
        Console.WriteLine("A.G")
    End Sub
End Class
MustInherit Class B
    Inherits A
    Public Overrides Shared Sub G()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30501: 'Shared' cannot be combined with 'Overrides' on a method declaration.
    Public Overrides Shared Sub G()
           ~~~~~~~~~
BC40005: sub 'G' shadows an overridable method in the base class 'A'. To override the base method, this method must be declared 'Overrides'.
    Public Overrides Shared Sub G()
                                ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30502ERR_BadFlagsOnSharedProperty1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        MustInherit Class C
            NotOverridable Shared Property P
            Overrides Shared Property Q
            Overridable Shared Property R
            Shared MustOverride Property S
            Default Shared Property T(ByVal v)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30502: 'Shared' cannot be combined with 'NotOverridable' on a property declaration.
            NotOverridable Shared Property P
            ~~~~~~~~~~~~~~
BC30502: 'Shared' cannot be combined with 'Overrides' on a property declaration.
            Overrides Shared Property Q
            ~~~~~~~~~
BC30502: 'Shared' cannot be combined with 'Overridable' on a property declaration.
            Overridable Shared Property R
            ~~~~~~~~~~~
BC30502: 'Shared' cannot be combined with 'MustOverride' on a property declaration.
            Shared MustOverride Property S
                   ~~~~~~~~~~~~
BC30502: 'Shared' cannot be combined with 'Default' on a property declaration.
            Default Shared Property T(ByVal v)
            ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30503ERR_BadFlagsOnStdModuleProperty1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadFlagsOnStdModuleProperty1">
        <file name="a.vb"><![CDATA[
        Module M
            Protected Property P
            Shared Property Q
            MustOverride Property R
            Overridable Property S
            Overrides Property T
            Shadows Property U
            Default Property V(o)
                Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30503: Properties in a Module cannot be declared 'Protected'.
            Protected Property P
            ~~~~~~~~~
BC30503: Properties in a Module cannot be declared 'Shared'.
            Shared Property Q
            ~~~~~~
BC30503: Properties in a Module cannot be declared 'MustOverride'.
            MustOverride Property R
            ~~~~~~~~~~~~
BC30503: Properties in a Module cannot be declared 'Overridable'.
            Overridable Property S
            ~~~~~~~~~~~
BC30503: Properties in a Module cannot be declared 'Overrides'.
            Overrides Property T
            ~~~~~~~~~
BC30503: Properties in a Module cannot be declared 'Shadows'.
            Shadows Property U
            ~~~~~~~
BC30503: Properties in a Module cannot be declared 'Default'.
            Default Property V(o)
            ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30505ERR_SharedOnProcThatImpl()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SharedOnProcThatImpl">
        <file name="a.vb"><![CDATA[
Delegate Sub D()
Interface I
    Sub M()
    Property P As Object
    Event E As D
End Interface
Class C
    Implements I
    Shared Sub M() Implements I.M
    End Sub
    Shared Property P As Object Implements I.P
    Shared Event E() Implements I.E
End Class
        ]]></file>
    </compilation>)
            compilation1.AssertTheseDiagnostics(<errors><![CDATA[
BC30149: Class 'C' must implement 'Event E As D' for interface 'I'.
    Implements I
               ~
BC30149: Class 'C' must implement 'Property P As Object' for interface 'I'.
    Implements I
               ~
BC30149: Class 'C' must implement 'Sub M()' for interface 'I'.
    Implements I
               ~
BC30505: Methods or events that implement interface members cannot be declared 'Shared'.
    Shared Sub M() Implements I.M
    ~~~~~~
BC30505: Methods or events that implement interface members cannot be declared 'Shared'.
    Shared Property P As Object Implements I.P
    ~~~~~~
BC30505: Methods or events that implement interface members cannot be declared 'Shared'.
    Shared Event E() Implements I.E
    ~~~~~~
                 ]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30506ERR_NoWithEventsVarOnHandlesList()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoWithEventsVarOnHandlesList">
        <file name="a.vb"><![CDATA[
            Module mod30506
                'COMPILEERROR: BC30506, "clsEventError1"
                Sub scenario1() Handles clsEventError1.Event1
                End Sub
                'COMPILEERROR: BC30506, "I1"
                Sub scenario2() Handles I1.goo
                End Sub
                'COMPILEERROR: BC30506, "button1"
                Sub scenario3() Handles button1.click
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
                Sub scenario1() Handles clsEventError1.Event1
                                        ~~~~~~~~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
                Sub scenario2() Handles I1.goo
                                        ~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
                Sub scenario3() Handles button1.click
                                        ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub HandlesHiddenEvent()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoWithEventsVarOnHandlesList">
        <file name="a.vb"><![CDATA[
Imports System

Class HasEvents
    Event E1()
End Class

Class HasWithEvents
    Public WithEvents WE0 As HasEvents
    Public WithEvents WE1 As HasEvents
    Public WithEvents WE2 As HasEvents
    Public WithEvents _WE3 As HasEvents
End Class

Class HasWithEventsDerived
    Inherits HasWithEvents

    Overrides Property WE0() As HasEvents
        Get
            Return Nothing
        End Get
        Set(value As HasEvents)

        End Set
    End Property

    Overloads Property WE1(x As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)

        End Set
    End Property

    Property WE2(x As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)

        End Set
    End Property

    Property WE3 As Integer

    Shared Sub goo0() Handles WE0.E1
        Console.WriteLine("handled2")
    End Sub

    Shared Sub goo1() Handles WE1.E1
        Console.WriteLine("handled2")
    End Sub

    Shared Sub goo2() Handles WE2.E2
        Console.WriteLine("handled2")
    End Sub

    Shared Sub goo3() Handles WE3.E3
        Console.WriteLine("handled2")
    End Sub
End Class


Class Program
    Shared Sub Main(args As String())
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30284: property 'WE0' cannot be declared 'Overrides' because it does not override a property in a base class.
    Overrides Property WE0() As HasEvents
                       ~~~
BC40004: property 'WE0' conflicts with WithEvents variable 'WE0' in the base class 'HasWithEvents' and should be declared 'Shadows'.
    Overrides Property WE0() As HasEvents
                       ~~~
BC40004: property 'WE1' conflicts with WithEvents variable 'WE1' in the base class 'HasWithEvents' and should be declared 'Shadows'.
    Overloads Property WE1(x As Integer) As Integer
                       ~~~
BC40004: property 'WE2' conflicts with WithEvents variable 'WE2' in the base class 'HasWithEvents' and should be declared 'Shadows'.
    Property WE2(x As Integer) As Integer
             ~~~
BC40012: property 'WE3' implicitly declares '_WE3', which conflicts with a member in the base class 'HasWithEvents', and so the property should be declared 'Shadows'.
    Property WE3 As Integer
             ~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Shared Sub goo0() Handles WE0.E1
                              ~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Shared Sub goo1() Handles WE1.E1
                              ~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Shared Sub goo2() Handles WE2.E2
                              ~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Shared Sub goo3() Handles WE3.E3
                              ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub HandlesProperty001()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="HandlesProperty001">
        <file name="a.vb"><![CDATA[
Imports System
Imports System.ComponentModel

Namespace Project1
    Module m1
        Public Sub main()
            Dim c = New Sink
            Dim s = New OuterClass
            c.x = s
            s.Test()
        End Sub
    End Module

    Class EventSource
        Public Event MyEvent()
        Sub test()
            RaiseEvent MyEvent()
        End Sub
    End Class

    Class SomeBase
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property SomePropertyInBase() As EventSource
            Get
                Console.Write("#Get#")
                Return Nothing
            End Get
            Set(value As EventSource)

            End Set
        End Property
    End Class

    Class OuterClass
        Inherits SomeBase

        Private Shared SubObject As New EventSource

        <DesignOnly(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Shared Property SomePropertyWrongValue() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property

        Public Property SomePropertyNoAttribute() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property

        Public Shared Property SomePropertyWriteOnly() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property

        Sub Test()
            SubObject.test()
        End Sub
    End Class

    Class Sink

        Public WithEvents x As OuterClass
        Sub goo1() Handles x.SomePropertyWrongValue.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub goo2() Handles x.SomePropertyNoAttribute.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub goo3() Handles x.SomePropertyWriteonly.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub goo4() Handles x.SomePropertyInBase.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub test()
            x.Test()
        End Sub
        Sub New()
            x = New OuterClass
        End Sub
    End Class
    '.....
End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31412: 'Handles' in classes must specify a 'WithEvents' variable, 'MyBase', 'MyClass' or 'Me' qualified with a single identifier.
        Sub goo1() Handles x.SomePropertyWrongValue.MyEvent
                           ~~~~~~~~~~~~~~~~~~~~~~~~
BC31412: 'Handles' in classes must specify a 'WithEvents' variable, 'MyBase', 'MyClass' or 'Me' qualified with a single identifier.
        Sub goo2() Handles x.SomePropertyNoAttribute.MyEvent
                           ~~~~~~~~~~~~~~~~~~~~~~~~~
BC31412: 'Handles' in classes must specify a 'WithEvents' variable, 'MyBase', 'MyClass' or 'Me' qualified with a single identifier.
        Sub goo3() Handles x.SomePropertyWriteonly.MyEvent
                           ~~~~~~~~~~~~~~~~~~~~~~~
BC31412: 'Handles' in classes must specify a 'WithEvents' variable, 'MyBase', 'MyClass' or 'Me' qualified with a single identifier.
        Sub goo4() Handles x.SomePropertyInBase.MyEvent
                           ~~~~~~~~~~~~~~~~~~~~
]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub WithEventsHides()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="WithEventsHides">
        <file name="a.vb"><![CDATA[
Imports System

Class HasEvents
    Event E1()
End Class

Class HasWithEvents
    Public WithEvents WE0 As HasEvents
    Public WithEvents WE1 As HasEvents
    Public Property WE2 As HasEvents
    Public Property WE3 As HasEvents
End Class

Class HasWithEventsDerived
    Inherits HasWithEvents

    Public WithEvents WE0 As HasEvents

    ' no warnings
    Public Shadows WithEvents WE1 As HasEvents

    Public WithEvents WE2 As HasEvents

    ' no warnings
    Public Shadows WithEvents WE3 As HasEvents

    Shared Sub goo0() Handles WE0.E1
        Console.WriteLine("handled2")
    End Sub

    Shared Sub goo1() Handles WE1.E1
        Console.WriteLine("handled2")
    End Sub

    Shared Sub goo2() Handles WE2.E1
        Console.WriteLine("handled2")
    End Sub

    Shared Sub goo3() Handles WE3.E1
        Console.WriteLine("handled2")
    End Sub
End Class


Class Program
    Shared Sub Main(args As String())
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40004: WithEvents variable 'WE0' conflicts with WithEvents variable 'WE0' in the base class 'HasWithEvents' and should be declared 'Shadows'.
    Public WithEvents WE0 As HasEvents
                      ~~~
BC40004: WithEvents variable 'WE2' conflicts with property 'WE2' in the base class 'HasWithEvents' and should be declared 'Shadows'.
    Public WithEvents WE2 As HasEvents
                      ~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub WithEventsOverloads()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="WithEventsOverloads">
        <file name="a.vb"><![CDATA[
Imports System

Class HasEvents
    Event E1()
End Class

Class HasWithEventsDerived
    Public WithEvents _WE1 As HasEvents

    Public Property WE0(x As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)

        End Set
    End Property

    Public Overloads Property WE2(x As Integer, y As Long) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)

        End Set
    End Property

    Public WithEvents WE0 As HasEvents

    Public Property WE1

    Public WithEvents WE2 As HasEvents

End Class


Class Program
    Shared Sub Main(args As String())
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31061: WithEvents variable '_WE1' conflicts with a member implicitly declared for property 'WE1' in class 'HasWithEventsDerived'.
    Public WithEvents _WE1 As HasEvents
                      ~~~~
BC30260: 'WE0' is already declared as 'Public Property WE0(x As Integer) As Integer' in this class.
    Public WithEvents WE0 As HasEvents
                      ~~~
BC30260: 'WE2' is already declared as 'Public Overloads Property WE2(x As Integer, y As Long) As Integer' in this class.
    Public WithEvents WE2 As HasEvents
                      ~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30508ERR_AccessMismatch6()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Namespace N
            Public Class A
                Protected Class C
                End Class
                Protected Structure S
                End Structure
                Private Interface I
                End Interface
                Public F As I
                Friend Function M() As C
                    Return Nothing
                End Function
            End Class
            Public Class B
                Inherits A
                Public G As C
                Friend ReadOnly H As S
                Public Function N(x As C) As S
                    Return Nothing
                End Function
            End Class
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30508: 'F' cannot expose type 'A.I' in namespace 'N' through class 'A'.
                Public F As I
                            ~
BC30508: 'M' cannot expose type 'A.C' in namespace 'N' through class 'A'.
                Friend Function M() As C
                                       ~
BC30508: 'G' cannot expose type 'A.C' in namespace 'N' through class 'B'.
                Public G As C
                            ~
BC30508: 'H' cannot expose type 'A.S' in namespace 'N' through class 'B'.
                Friend ReadOnly H As S
                                     ~
BC30508: 'x' cannot expose type 'A.C' in namespace 'N' through class 'B'.
                Public Function N(x As C) As S
                                       ~
BC30508: 'N' cannot expose type 'A.S' in namespace 'N' through class 'B'.
                Public Function N(x As C) As S
                                             ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30508ERR_AccessMismatch6_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Namespace NS30508
            Interface i1
            End Interface
            Interface i2
            End Interface
            Class C1(Of v)
                Implements i1
            End Class
            Class c2
                Implements i1
                Implements i2
                'COMPILEERROR: BC30508, "ProtectedClass"
                Function fiveB(Of t As ProtectedClass)() As String
                    Return "In fiveB"
                End Function
                'COMPILEERROR: BC30508, "Privateclass"
                Function fiveC(Of t As Privateclass)() As String
                    Return "In fiveC"
                End Function
                Protected Function fiveD(Of t As ProtectedClass)() As String
                    Return "In fiveB"
                End Function
                Private Function fiveE(Of t As Privateclass)() As String
                    Return "In fiveC"
                End Function
                Protected Class ProtectedClass
                End Class
                Private Class Privateclass
                End Class
            End Class
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30508: 'fiveB' cannot expose type 'c2.ProtectedClass' in namespace 'NS30508' through class 'c2'.
                Function fiveB(Of t As ProtectedClass)() As String
                                       ~~~~~~~~~~~~~~
BC30508: 'fiveC' cannot expose type 'c2.Privateclass' in namespace 'NS30508' through class 'c2'.
                Function fiveC(Of t As Privateclass)() As String
                                       ~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30508ERR_AccessMismatch6_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Namespace N
            Public Class A
                Protected Class C
                End Class
                Protected Structure S
                End Structure
                Private Interface I
                End Interface
                Protected Enum E
                    A
                End Enum
                Property P As I
            End Class
            Public Class B
                Inherits A
                Property Q As C
                Friend ReadOnly Property R(x As E) As S
                    Get
                        Return Nothing
                    End Get
                End Property
            End Class
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30508: 'P' cannot expose type 'A.I' in namespace 'N' through class 'A'.
                Property P As I
                              ~
BC30508: 'Q' cannot expose type 'A.C' in namespace 'N' through class 'B'.
                Property Q As C
                              ~
BC30508: 'x' cannot expose type 'A.E' in namespace 'N' through class 'B'.
                Friend ReadOnly Property R(x As E) As S
                                                ~
BC30508: 'R' cannot expose type 'A.S' in namespace 'N' through class 'B'.
                Friend ReadOnly Property R(x As E) As S
                                                      ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528153")>
        <Fact()>
        Public Sub BC30508ERR_AccessMismatch6_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Namespace N
            Public Class A
                Protected Class B
                End Class
                Public Class C
                    Function F() As B
                        Return Nothing
                    End Function
                    Public Class D
                        Sub M(o As B)
                        End Sub
                    End Class
                End Class
            End Class
            Public Class E
                Inherits A
                Function F() As B
                    Return Nothing
                End Function
            End Class
        End Namespace
        Public Class F
            Inherits N.A
            Sub M(o As B)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30508: 'F' cannot expose type 'A.B' in namespace 'N' through class 'C'.
                    Function F() As B
                                    ~
BC30508: 'o' cannot expose type 'A.B' in namespace 'N' through class 'D'.
                        Sub M(o As B)
                                   ~
BC30508: 'F' cannot expose type 'A.B' in namespace 'N' through class 'E'.
                Function F() As B
                                ~
BC30508: 'o' cannot expose type 'A.B' in namespace '<Default>' through class 'F'.
            Sub M(o As B)
                       ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30509ERR_InheritanceAccessMismatch5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InheritanceAccessMismatch5">
        <file name="a.vb"><![CDATA[
            Module Module1
                Private Class C1
                End Class
                Class C2
                    Inherits C1
                End Class
                Class C3
                    Protected Class C4
                    End Class
                    Class C5
                        Inherits C4
                    End Class
                End Class
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30509: 'C2' cannot inherit from class 'Module1.C1' because it expands the access of the base class to namespace '<Default>'.
                    Inherits C1
                             ~~
BC30509: 'C5' cannot inherit from class 'Module1.C3.C4' because it expands the access of the base class to namespace '<Default>'.
                        Inherits C4
                                 ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30529ERR_ParamTypingInconsistency()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ParamTypingInconsistency">
        <file name="a.vb"><![CDATA[
          class M1
                Sub Goo(ByVal [a], ByRef [continue], ByVal c As Single)
                End Sub
            End class  
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30529: All parameters must be explicitly typed if any of them are explicitly typed.
                Sub Goo(ByVal [a], ByRef [continue], ByVal c As Single)
                              ~~~
BC30529: All parameters must be explicitly typed if any of them are explicitly typed.
                Sub Goo(ByVal [a], ByRef [continue], ByVal c As Single)
                                         ~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30530ERR_ParamNameFunctionNameCollision_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
          class M1
                Function Goo(Of Goo)(ByVal Goo As Goo)
                End Function
          End class  
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32090: Type parameter cannot have the same name as its defining function.
                Function Goo(Of Goo)(ByVal Goo As Goo)
                                ~~~
BC30530: Parameter cannot have the same name as its defining function.
                Function Goo(Of Goo)(ByVal Goo As Goo)
                                           ~~~
BC32089: 'Goo' is already declared as a type parameter of this method.
                Function Goo(Of Goo)(ByVal Goo As Goo)
                                           ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30530ERR_ParamNameFunctionNameCollision_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            WriteOnly Property P(p, q)
                Set(value)
                End Set
            End Property
            Private _q
            WriteOnly Property Q
                Set(Q) ' No error
                    _q = Q
                End Set
            End Property
            Property R
                Get
                    Return Nothing
                End Get
                Set(get_R) ' No error
                End Set
            End Property
            WriteOnly Property S
                Set(set_S) ' No error
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30530: Parameter cannot have the same name as its defining function.
            WriteOnly Property P(p, q)
                                 ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540629")>
        <Fact>
        Public Sub BC30548ERR_InvalidAssemblyAttribute1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InvalidAssemblyAttribute1">
        <file name="a.vb"><![CDATA[
Imports System

'COMPILEERROR: BC30548, "Assembly: c1()"
<Assembly: c1()> 
'COMPILEERROR: BC30549, "Module: c1()"
<Module: c1()> 

<AttributeUsageAttribute(AttributeTargets.Class, Inherited:=False)>
NotInheritable Class c1Attribute
    Inherits Attribute
End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_InvalidAssemblyAttribute1, "c1").WithArguments("c1Attribute"),
    Diagnostic(ERRID.ERR_InvalidModuleAttribute1, "c1").WithArguments("c1Attribute"))

        End Sub

        <WorkItem(540629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540629")>
        <Fact>
        Public Sub BC30549ERR_InvalidModuleAttribute1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InvalidModuleAttribute1">
        <file name="a.vb"><![CDATA[
Imports System
<Module: InternalsVisibleTo()> 
<AttributeUsageAttribute(AttributeTargets.Delegate)>
Class InternalsVisibleTo
    Inherits Attribute
End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidModuleAttribute1, "InternalsVisibleTo").WithArguments("InternalsVisibleTo"))

        End Sub

        <Fact>
        Public Sub BC30561ERR_AmbiguousInImports2()
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"N1", "N2"}))
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AmbiguousInImports2">
        <file name="a.vb"><![CDATA[
           Public Class Cls2
                Implements i1
              End Class 
        ]]></file>
        <file name="b.vb"><![CDATA[
            Namespace N1
                Interface I1
                End Interface
            End Namespace
            Namespace N2
                Interface  I1
                End Interface
            End Namespace
        ]]></file>
    </compilation>, options:=options)

            Dim expectedErrors1 = <errors><![CDATA[
BC30561: 'I1' is ambiguous, imported from the namespaces or types 'N1, N2'.
                Implements i1
                           ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30562ERR_AmbiguousInModules2()
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"N1", "N2"}))
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AmbiguousInModules2">
        <file name="a.vb"><![CDATA[
           Public Class Cls2
                Implements i1
              End Class 
        ]]></file>
        <file name="b.vb"><![CDATA[
            Module N1
                Interface I1
                End Interface
            End Module
            Module N2
                Interface  I1
                End Interface
            End Module
        ]]></file>
    </compilation>, options)

            Dim expectedErrors1 = <errors><![CDATA[
BC30562: 'I1' is ambiguous between declarations in Modules 'N1, N2'.
                Implements i1
                           ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30572ERR_DuplicateNamedImportAlias1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30572ERR_DuplicateNamedImportAlias1">
        <file name="a.vb"><![CDATA[
         Imports Alias1 = N1.C1(of String)
         Imports Alias1 = N1.C1(of String)
        ]]></file>
        <file name="b.vb"><![CDATA[
            Module N1
                Class C1(of T)
                End class
            End Module
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30572: Alias 'Alias1' is already declared.
         Imports Alias1 = N1.C1(of String)
                 ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30573ERR_DuplicatePrefix()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Xml.Linq
Imports <xmlns="default1">
Imports <xmlns="default2">
Imports <xmlns:p="p1">
Imports <xmlns:q="q">
Imports <xmlns:p="p2">
Class C
    Private F As XElement = <p:a xmlns:p="p3" xmlns:q="q"/>
End Class
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30573: XML namespace prefix '' is already declared.
Imports <xmlns="default2">
        ~~~~~~~~~~~~~~~~~~
BC30573: XML namespace prefix 'p' is already declared.
Imports <xmlns:p="p2">
        ~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30573ERR_DuplicatePrefix_1()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns=""default1"">", "<xmlns=""default2"">", "<xmlns:p=""p1"">", "<xmlns:q=""q"">", "<xmlns:p=""p2"">"}))
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns="default">
Imports <xmlns:p="p">
Imports <xmlns:q="q">
Class C
End Class
    ]]></file>
</compilation>, references:=XmlReferences, options:=options)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30573: Error in project-level import '<xmlns:p="p2">' at '<xmlns:p="p2">' : XML namespace prefix 'p' is already declared.
BC30573: Error in project-level import '<xmlns="default2">' at '<xmlns="default2">' : XML namespace prefix '' is already declared.
]]></errors>)
            Dim embedded = compilation.GetTypeByMetadataName("Microsoft.VisualBasic.Embedded")
            Assert.IsType(Of EmbeddedSymbolManager.EmbeddedNamedTypeSymbol)(embedded)
            Assert.False(DirectCast(embedded, INamedTypeSymbol).IsSerializable)
        End Sub

        <Fact>
        Public Sub BC30583ERR_MethodAlreadyImplemented2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MethodAlreadyImplemented2">
        <file name="a.vb"><![CDATA[
            Namespace NS1
                Interface i1
                    Sub goo()
                End Interface
                Interface i2
                    Inherits i1
                End Interface
                Interface i3
                    Inherits i1
                End Interface
                Class cls1
                    Implements i2, i3
                    Sub i2goo() Implements i2.goo
                        Console.WriteLine("in i1, goo")
                    End Sub
                    'COMPILEERROR: BC30583, "i3.goo"
                    Sub i3goo() Implements i3.goo
                        Console.WriteLine("in i3, goo")
                    End Sub
                End Class
            End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30583: 'i1.goo' cannot be implemented more than once.
                    Sub i3goo() Implements i3.goo
                                           ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30584ERR_DuplicateInInherits1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateInInherits1">
        <file name="a.vb"><![CDATA[
            Interface sce(Of T)
            End Interface
            Interface sce1
                Inherits sce(Of Integer)
                        Inherits sce(Of Integer)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30584: 'sce(Of Integer)' cannot be inherited more than once.
                        Inherits sce(Of Integer)
                                 ~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30584ERR_DuplicateInInherits1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateInInherits1">
        <file name="a.vb"><![CDATA[
            interface  I1
            End interface
            Structure s1
                Interface sce1
                    Inherits I1
                    Inherits I1
                End Interface
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30584: 'I1' cannot be inherited more than once.
                    Inherits I1
                             ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30590ERR_EventNotFound1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventNotFound1">
        <file name="a.vb"><![CDATA[
            Friend Module mod30590
                Class cls1
                    Event Ev2()
                End Class
                Class cls2 : Inherits cls1
                    Public WithEvents o As cls2
                    Private Shadows Sub Ev2()
                    End Sub
                    'COMPILEERROR: BC30590, "Ev2"
                    Private Sub o_Ev2() Handles o.Ev2
                    End Sub
                End Class
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30590: Event 'Ev2' cannot be found.
                    Private Sub o_Ev2() Handles o.Ev2
                                                  ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30593ERR_ModuleCantUseVariableSpecifier1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ModuleCantUseVariableSpecifier1">
        <file name="a.vb"><![CDATA[
            Class class1
            End Class
            Structure struct1
            End Structure
            Friend Module ObjInit14mod
                'COMPILEERROR: BC30593, "Shared"
                Shared cls As New class1()
                'COMPILEERROR: BC30593, "Shared"
                Shared xint As New Integer()
                'COMPILEERROR: BC30593, "Shared"
                Shared xdec As New Decimal(40@)
                'COMPILEERROR: BC30593, "Shared"
                Shared xstrct As New struct1()
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30593: Variables in Modules cannot be declared 'Shared'.
                Shared cls As New class1()
                ~~~~~~
BC30593: Variables in Modules cannot be declared 'Shared'.
                Shared xint As New Integer()
                ~~~~~~
BC30593: Variables in Modules cannot be declared 'Shared'.
                Shared xdec As New Decimal(40@)
                ~~~~~~
BC30593: Variables in Modules cannot be declared 'Shared'.
                Shared xstrct As New struct1()
                ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30594ERR_SharedEventNeedsSharedHandler()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SharedEventNeedsSharedHandler">
        <file name="a.vb"><![CDATA[
            Module M1
                Class myCls
                    Event Event1()
                End Class
                Class myClass1
                    Shared WithEvents x As myCls
                    Public Sub EventHandler() Handles x.Event1
                    End Sub
                    Private Sub EventHandler1() Handles x.Event1
                    End Sub
                    Protected Sub EventHandler2() Handles x.Event1
                    End Sub
                    Friend Sub EventHandler3() Handles x.Event1
                    End Sub
                End Class
                Class myClass2
                    Shared WithEvents x As myCls
                    Overridable Sub EventHandler() Handles x.Event1
                    End Sub
                End Class
                MustInherit Class myClass3
                    Shared WithEvents x As myCls
                    MustOverride Sub EventHandler() Handles x.Event1
                End Class
                Class myClass4
                    Overridable Sub EventHandler()
                    End Sub
                End Class
                Class myClass7
                    Inherits myClass4
                    Shared WithEvents x As myCls
                    NotOverridable Overrides Sub EventHandler() Handles x.Event1
                    End Sub
                End Class
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30594: Events of shared WithEvents variables cannot be handled by non-shared methods.
                    Public Sub EventHandler() Handles x.Event1
                                                      ~
BC31029: Method 'EventHandler' cannot handle event 'Event1' because they do not have a compatible signature.
                    Public Sub EventHandler() Handles x.Event1
                                                        ~~~~~~
BC30594: Events of shared WithEvents variables cannot be handled by non-shared methods.
                    Private Sub EventHandler1() Handles x.Event1
                                                        ~
BC31029: Method 'EventHandler1' cannot handle event 'Event1' because they do not have a compatible signature.
                    Private Sub EventHandler1() Handles x.Event1
                                                          ~~~~~~
BC30594: Events of shared WithEvents variables cannot be handled by non-shared methods.
                    Protected Sub EventHandler2() Handles x.Event1
                                                          ~
BC31029: Method 'EventHandler2' cannot handle event 'Event1' because they do not have a compatible signature.
                    Protected Sub EventHandler2() Handles x.Event1
                                                            ~~~~~~
BC30594: Events of shared WithEvents variables cannot be handled by non-shared methods.
                    Friend Sub EventHandler3() Handles x.Event1
                                                       ~
BC31029: Method 'EventHandler3' cannot handle event 'Event1' because they do not have a compatible signature.
                    Friend Sub EventHandler3() Handles x.Event1
                                                         ~~~~~~
BC30594: Events of shared WithEvents variables cannot be handled by non-shared methods.
                    Overridable Sub EventHandler() Handles x.Event1
                                                           ~
BC31029: Method 'EventHandler' cannot handle event 'Event1' because they do not have a compatible signature.
                    Overridable Sub EventHandler() Handles x.Event1
                                                             ~~~~~~
BC30594: Events of shared WithEvents variables cannot be handled by non-shared methods.
                    MustOverride Sub EventHandler() Handles x.Event1
                                                            ~
BC31029: Method 'EventHandler' cannot handle event 'Event1' because they do not have a compatible signature.
                    MustOverride Sub EventHandler() Handles x.Event1
                                                              ~~~~~~
BC30594: Events of shared WithEvents variables cannot be handled by non-shared methods.
                    NotOverridable Overrides Sub EventHandler() Handles x.Event1
                                                                        ~
BC31029: Method 'EventHandler' cannot handle event 'Event1' because they do not have a compatible signature.
                    NotOverridable Overrides Sub EventHandler() Handles x.Event1
                                                                          ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30927ERR_MustOverOnNotInheritPartClsMem1_1()
            CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Test

    NotInheritable Class C

    End Class

    Partial Class C
        'COMPILEERROR: BC30927, "goo"
        MustOverride Function goo() As Integer
    End Class

End Class
    ]]></file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_MustOverOnNotInheritPartClsMem1, "MustOverride").WithArguments("MustOverride"))

        End Sub

        <Fact>
        Public Sub NotInheritableInOtherPartial()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class B
    Overridable Sub f()
    End Sub

    Overridable Property p As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class

NotInheritable Class C
    Inherits B
End Class

Partial Class C
    MustOverride Sub f1()
    MustOverride Property p1 As Integer

    Overridable Sub f2()
    End Sub

    Overridable Property p2 As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    NotOverridable Overrides Sub f()
    End Sub

    NotOverridable Overrides Property p As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class    
]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30927: 'MustOverride' cannot be specified on this member because it is in a partial type that is declared 'NotInheritable' in another partial definition.
    MustOverride Sub f1()
    ~~~~~~~~~~~~
BC30927: 'MustOverride' cannot be specified on this member because it is in a partial type that is declared 'NotInheritable' in another partial definition.
    MustOverride Property p1 As Integer
    ~~~~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC30607ERR_BadFlagsInNotInheritableClass1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            MustInherit Class A
                MustOverride Sub O()
                MustOverride Property R
            End Class
            NotInheritable Class B
                Inherits A
                Overridable Sub M()
                End Sub
                MustOverride Sub N()
                NotOverridable Overrides Sub O()
                End Sub
                Overridable Property P
                MustOverride Property Q
                NotOverridable Overrides Property R
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC30607: 'NotInheritable' classes cannot have members declared 'Overridable'.
                Overridable Sub M()
                ~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
                MustOverride Sub N()
                ~~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'NotOverridable'.
                NotOverridable Overrides Sub O()
                ~~~~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'Overridable'.
                Overridable Property P
                ~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
                MustOverride Property Q
                ~~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'NotOverridable'.
                NotOverridable Overrides Property R
                ~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(540594, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540594")>
        <Fact>
        Public Sub BC30610ERR_BaseOnlyClassesMustBeExplicit2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BaseOnlyClassesMustBeExplicit2">
        <file name="a.vb"><![CDATA[
           MustInherit Class Cls1
                MustOverride Sub Goo(ByVal Arg As Integer)
                MustOverride Sub Goo(ByVal Arg As Double)
            End Class
            'COMPILEERROR: BC30610, "cls2"
            Class cls2
                Inherits Cls1
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30610: Class 'cls2' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    Cls1: Public MustOverride Sub Goo(Arg As Integer)
    Cls1: Public MustOverride Sub Goo(Arg As Double).
            Class cls2
                  ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(541026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541026")>
        <Fact()>
        Public Sub BC30610ERR_BaseOnlyClassesMustBeExplicit2WithBC31411()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BaseOnlyClassesMustBeExplicit2">
        <file name="a.vb"><![CDATA[
           MustInherit Class Cls1
                MustOverride Sub Goo(ByVal Arg As Integer)
                MustOverride Sub Goo(ByVal Arg As Double)
            End Class
            'COMPILEERROR: BC30610, "cls2"
            Class cls2
                Inherits Cls1
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30610: Class 'cls2' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    Cls1: Public MustOverride Sub Goo(Arg As Integer)
    Cls1: Public MustOverride Sub Goo(Arg As Double).
            Class cls2
                  ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30628ERR_StructCantInherit()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StructCantInherit">
        <file name="a.vb"><![CDATA[
            Structure S1
                Structure S2
                    Inherits s1
                End Structure
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30628: Structures cannot have 'Inherits' statements.
                    Inherits s1
                    ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30629ERR_NewInStruct()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NewInStruct">
        <file name="a.vb"><![CDATA[
            Module mod30629
                Structure Struct1
                    'COMPILEERROR:BC30629,"new"
                    Public Sub New()
                    End Sub
                End Structure
            End Module
        ]]></file>
    </compilation>, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic12))
            Dim expectedErrors1 = <errors><![CDATA[
BC30629: Structures cannot declare a non-shared 'Sub New' with no parameters.
                    Public Sub New()
                               ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC37240ERR_StructParameterlessInstanceCtorMustBePublic()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NewInStruct">
        <file name="a.vb"><![CDATA[
            Module mod37240
                Structure Struct1
                    'COMPILEERROR:BC37240,"new"
                    Private Sub New()
                    End Sub
                End Structure

                sub Main
                end sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30629: Structures cannot declare a non-shared 'Sub New' with no parameters.
                    Private Sub New()
                                ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30639ERR_BadPropertyFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadPropertyFlags1">
        <file name="a.vb"><![CDATA[
        Interface I
            NotInheritable Property P
            MustInherit ReadOnly Property Q
            WriteOnly Const Property R(o)
            Partial Property S
        End Interface
        Class C
            NotInheritable Property T
            MustInherit ReadOnly Property U
                Get
                    Return Nothing
                End Get
            End Property
            WriteOnly Const Property V(o)
                Set(value)
                End Set
            End Property
            Partial Property W
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30639: Properties cannot be declared 'NotInheritable'.
            NotInheritable Property P
            ~~~~~~~~~~~~~~
BC30639: Properties cannot be declared 'MustInherit'.
            MustInherit ReadOnly Property Q
            ~~~~~~~~~~~
BC30639: Properties cannot be declared 'Const'.
            WriteOnly Const Property R(o)
                      ~~~~~
BC30639: Properties cannot be declared 'Partial'.
            Partial Property S
            ~~~~~~~
BC30639: Properties cannot be declared 'NotInheritable'.
            NotInheritable Property T
            ~~~~~~~~~~~~~~
BC30639: Properties cannot be declared 'MustInherit'.
            MustInherit ReadOnly Property U
            ~~~~~~~~~~~
BC30639: Properties cannot be declared 'Const'.
            WriteOnly Const Property V(o)
                      ~~~~~
BC30639: Properties cannot be declared 'Partial'.
            Partial Property W
            ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30645ERR_InvalidOptionalParameterUsage1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidOptionalParameterUsage1">
        <file name="a.vb"><![CDATA[
Module M1
    <System.Web.Services.WebMethod(True)>
    Public Function HelloWorld(Optional ByVal n As String = "e") As String
        Return "Hello World"
    End Function
End Module
        ]]></file>
    </compilation>)
            compilation1 = compilation1.AddReferences(TestReferences.NetFx.v4_0_30319.System_Web_Services)

            Dim expectedErrors1 = <errors><![CDATA[
BC30645: Attribute 'WebMethod' cannot be applied to a method with optional parameters.
    Public Function HelloWorld(Optional ByVal n As String = "e") As String
                    ~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30645ERR_InvalidOptionalParameterUsage1a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidOptionalParameterUsage1a">
        <file name="a.vb"><![CDATA[
Module M1
    <System.Web.Services.WebMethod(True)>
    <System.Web.Services.WebMethod(False)>
    Public Function HelloWorld(Optional ByVal n As String = "e") As String
        Return "Hello World"
    End Function
End Module
        ]]></file>
    </compilation>)
            compilation1 = compilation1.AddReferences(TestReferences.NetFx.v4_0_30319.System_Web_Services)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
    BC30663: Attribute 'WebMethodAttribute' cannot be applied multiple times.
    <System.Web.Services.WebMethod(False)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30645: Attribute 'WebMethod' cannot be applied to a method with optional parameters.
    Public Function HelloWorld(Optional ByVal n As String = "e") As String
                    ~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30645ERR_InvalidOptionalParameterUsage1b()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidOptionalParameterUsage1b">
        <file name="a.vb"><![CDATA[
Module M1
    <System.Web.Services.WebMethod()>
    Public Function HelloWorld1(Optional ByVal n As String = "e") As String
        Return "Hello World"
    End Function

    <System.Web.Services.WebMethod(True, System.EnterpriseServices.TransactionOption.Disabled)>
    Public Function HelloWorld2(Optional ByVal n As String = "e") As String
        Return "Hello World"
    End Function

    <System.Web.Services.WebMethod(True, System.EnterpriseServices.TransactionOption.Disabled, 1)>
    Public Function HelloWorld3(Optional ByVal n As String = "e") As String
        Return "Hello World"
    End Function

    <System.Web.Services.WebMethod(True, System.EnterpriseServices.TransactionOption.Disabled, 1, True)>
    Public Function HelloWorld4(Optional ByVal n As String = "e") As String
        Return "Hello World"
    End Function

    <System.Web.Services.WebMethod(True, System.EnterpriseServices.TransactionOption.Disabled, 1, True, 123)>
    Public Function HelloWorld5(Optional ByVal n As String = "e") As String
        Return "Hello World"
    End Function
End Module
        ]]></file>
    </compilation>)
            compilation1 = compilation1.AddReferences(TestReferences.NetFx.v4_0_30319.System_Web_Services,
                                                      TestReferences.NetFx.v4_0_30319.System_EnterpriseServices.dll)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors><![CDATA[
BC30645: Attribute 'WebMethod' cannot be applied to a method with optional parameters.
    Public Function HelloWorld1(Optional ByVal n As String = "e") As String
                    ~~~~~~~~~~~
BC30645: Attribute 'WebMethod' cannot be applied to a method with optional parameters.
    Public Function HelloWorld2(Optional ByVal n As String = "e") As String
                    ~~~~~~~~~~~
BC30645: Attribute 'WebMethod' cannot be applied to a method with optional parameters.
    Public Function HelloWorld3(Optional ByVal n As String = "e") As String
                    ~~~~~~~~~~~
BC30645: Attribute 'WebMethod' cannot be applied to a method with optional parameters.
    Public Function HelloWorld4(Optional ByVal n As String = "e") As String
                    ~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
    <System.Web.Services.WebMethod(True, System.EnterpriseServices.TransactionOption.Disabled, 1, True, 123)>
                         ~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30650ERR_InvalidEnumBase()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidEnumBase">
        <file name="a.vb"><![CDATA[
            Module ICase1mod
                Enum color1 As Double
                    blue
                End Enum
                Enum color2 As String
                    blue
                End Enum
                Enum color3 As Single
                    blue
                End Enum
                Enum color4 As Date
                    blue
                End Enum
                Enum color5 As Object
                    blue
                End Enum
                Enum color6 As Boolean
                    blue
                End Enum
                Enum color7 As Decimal
                    blue
                End Enum
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30650: Enums must be declared as an integral type.
                Enum color1 As Double
                               ~~~~~~
BC30650: Enums must be declared as an integral type.
                Enum color2 As String
                               ~~~~~~
BC30650: Enums must be declared as an integral type.
                Enum color3 As Single
                               ~~~~~~
BC30650: Enums must be declared as an integral type.
                Enum color4 As Date
                               ~~~~
BC30650: Enums must be declared as an integral type.
                Enum color5 As Object
                               ~~~~~~
BC30650: Enums must be declared as an integral type.
                Enum color6 As Boolean
                               ~~~~~~~
BC30650: Enums must be declared as an integral type.
                Enum color7 As Decimal
                               ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30651ERR_ByRefIllegal1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ByRefIllegal1">
        <file name="a.vb"><![CDATA[
            Class C
                Property P(ByVal x, ByRef y)
                    Get
                        Return Nothing
                    End Get
                    Set(value)
                    End Set
                End Property
                ReadOnly Property Q(ParamArray p())
                    Get
                        Return Nothing
                    End Get
                End Property
                WriteOnly Property R(Optional x = Nothing)
                    Set(value)
                    End Set
                End Property
            End Class
            Interface I
                ReadOnly Property P(ByRef x, Optional ByVal y = Nothing)
                WriteOnly Property Q(ParamArray p())
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30651: property parameters cannot be declared 'ByRef'.
                Property P(ByVal x, ByRef y)
                                    ~~~~~
BC30651: property parameters cannot be declared 'ByRef'.
                ReadOnly Property P(ByRef x, Optional ByVal y = Nothing)
                                    ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30652ERR_UnreferencedAssembly3()
            Dim Lib1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Lib1">
        <file name="a.vb"><![CDATA[
            Public Class C1
            End Class
        ]]></file>
    </compilation>)
            Dim Lib2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Lib2">
        <file name="a.vb"><![CDATA[
            Public Class C2
                Dim s as C1
            End Class
        ]]></file>
    </compilation>)
            Dim ref1 = New VisualBasicCompilationReference(Lib1)
            Lib2 = Lib2.AddReferences(ref1)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnreferencedAssembly3">
        <file name="a.vb"><![CDATA[
            Public Class C
                Dim s as C1
            End Class
        ]]></file>
    </compilation>)
            Dim ref2 = New VisualBasicCompilationReference(Lib2)
            compilation1 = compilation1.AddReferences(ref2)
            Dim expectedErrors1 = <errors><![CDATA[
BC30002: Type 'C1' is not defined.
                Dim s as C1
                         ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(538153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538153")>
        <Fact>
        Public Sub BC30656ERR_UnsupportedField1()
            Dim csharpComp = CSharp.CSharpCompilation.Create("Test", options:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim text = "public class A  {      public static volatile int X;  }"
            Dim ref = TestReferences.NetFx.v4_0_21006.mscorlib
            csharpComp = csharpComp.AddSyntaxTrees(CSharp.SyntaxFactory.ParseSyntaxTree(text))
            csharpComp = csharpComp.AddReferences(ref)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UnsupportedField1">
        <file name="a.vb"><![CDATA[
        Module M
            Dim X = A.X 
        End Module
        ]]></file>
    </compilation>)
            compilation1 = compilation1.AddReferences(MetadataReference.CreateFromImage(csharpComp.EmitToArray()))
            Dim expectedErrors1 = <errors><![CDATA[
BC30656: Field 'X' is of an unsupported type.
            Dim X = A.X 
                    ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30660ERR_LocalsCannotHaveAttributes()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="LocalsCannotHaveAttributes">
        <file name="at30660.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    Inherits Attribute
    Public Sub New(p As ULong)

    End Sub
End Class

Public Class Goo
    Public Function SSS() As Byte
        <My(12345)> Dim x As Byte = 1
        Return x
    End Function
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_LocalsCannotHaveAttributes, "<My(12345)>"))

        End Sub

        <Fact>
        Public Sub BC30662ERR_InvalidAttributeUsage2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InvalidAttributeUsage2">
        <file name="a.vb"><![CDATA[
Imports System

Namespace DecAttr
    <AttributeUsageAttribute(AttributeTargets.Property)> Class attr1
        Inherits Attribute
    End Class
    <AttributeUsage(AttributeTargets.Event)> Class attr2
        Inherits Attribute
    End Class
    <AttributeUsageAttribute(AttributeTargets.Delegate)> Class attr3
        Inherits Attribute
    End Class
    <AttributeUsage(AttributeTargets.Constructor)> Class attr4
        Inherits Attribute
    End Class
    <AttributeUsageAttribute(AttributeTargets.Field)> Class attr5
        Inherits Attribute
    End Class
    Class scen1
        <attr1()> Public Declare Function Beep1 Lib "kernel32" Alias "Beep" (ByVal dwFreq As Long, ByVal dwDuration As Long) As Long
        <attr2()> Public Declare Function Beep2 Lib "kernel32" Alias "Beep" (ByVal dwFreq As Long, ByVal dwDuration As Long) As Long
        <attr3()> Public Declare Function Beep3 Lib "kernel32" Alias "Beep" (ByVal dwFreq As Long, ByVal dwDuration As Long) As Long
        <attr4()> Public Declare Function Beep4 Lib "kernel32" Alias "Beep" (ByVal dwFreq As Long, ByVal dwDuration As Long) As Long
        <attr5()> Public Declare Function Beep5 Lib "kernel32" Alias "Beep" (ByVal dwFreq As Long, ByVal dwDuration As Long) As Long
    End Class
End Namespace
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "attr1").WithArguments("attr1", "Beep1"),
                                      Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "attr2").WithArguments("attr2", "Beep2"),
                                      Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "attr3").WithArguments("attr3", "Beep3"),
                                      Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "attr4").WithArguments("attr4", "Beep4"),
                                      Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "attr5").WithArguments("attr5", "Beep5"))

        End Sub

        <WorkItem(538370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538370")>
        <Fact>
        Public Sub BC30663ERR_InvalidMultipleAttributeUsage1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidMultipleAttributeUsage1">
        <file name="a.vb"><![CDATA[
Imports System

<Assembly:clscompliant(true), Assembly:clscompliant(true), Assembly:clscompliant(true)>
<Module:clscompliant(true), Module:clscompliant(true), Module:clscompliant(true)>

Namespace DecAttr
    <AttributeUsageAttribute(AttributeTargets.Property)>
    Class attrProperty
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Parameter)>
    Class attrParameter
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.ReturnValue)>
    Class attrReturnType
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Class)>
    Class attrClass
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Struct)>
    Class attrStruct
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Method)>
    Class attrMethod
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Field)>
    Class attrField
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Enum)>
    Class attrEnum
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Constructor)>
    Class attrCtor
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Delegate)>
    Class attrDelegate
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Interface)>
    Class attrInterface
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Assembly)>
    Class attrAssembly
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Module)>
    Class attrModule
        Inherits Attribute
    End Class


    <attrClass(), attrClass(), attrClass()>
    Module M1
    End Module

    <attrInterface(), attrInterface(), attrInterface()>
    Interface I
    End Interface

    <attrEnum(), attrEnum(), attrEnum()>
    Enum E
        member
    End Enum

    <attrDelegate(), attrDelegate(), attrDelegate()>
    Delegate Sub Del()

    <attrClass(), attrClass()>
    <attrClass()>
    Class scen1(Of T1)
        <attrCtor(), attrCtor(), attrCtor()>
        Public Sub New()
        End Sub

        <attrField(), attrField(), attrField()>
        Public field as Integer

        Private newPropertyValue As String
        <attrProperty()> ' first ok
        <attrProperty()> ' first error
        <attrProperty()> ' second error
        Public Property NewProperty() As String
            Get
                Return newPropertyValue
            End Get
            Set(ByVal value As String)
                newPropertyValue = value
            End Set
        End Property

        <attrMethod(), attrMethod(), attrMethod()>
        Public function Sub1(Of T)(
            <attrParameter(), attrParameter(), attrParameter()> a as Integer,
            b as T) as <attrReturnType(), attrReturnType(), attrReturnType()> Integer
            return 23
        End function

    End Class

    <attrStruct()>
    <attrStruct(), attrStruct()> 
    Structure S1
    End Structure
End Namespace
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "Assembly:clscompliant(true)").WithArguments("CLSCompliantAttribute"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "Assembly:clscompliant(true)").WithArguments("CLSCompliantAttribute"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "Module:clscompliant(true)").WithArguments("CLSCompliantAttribute"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "Module:clscompliant(true)").WithArguments("CLSCompliantAttribute"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrDelegate()").WithArguments("attrDelegate"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrDelegate()").WithArguments("attrDelegate"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrEnum()").WithArguments("attrEnum"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrEnum()").WithArguments("attrEnum"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrInterface()").WithArguments("attrInterface"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrInterface()").WithArguments("attrInterface"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrClass()").WithArguments("attrClass"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrClass()").WithArguments("attrClass"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrStruct()").WithArguments("attrStruct"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrStruct()").WithArguments("attrStruct"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrParameter()").WithArguments("attrParameter"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrParameter()").WithArguments("attrParameter"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrClass()").WithArguments("attrClass"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrClass()").WithArguments("attrClass"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrCtor()").WithArguments("attrCtor"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrCtor()").WithArguments("attrCtor"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrField()").WithArguments("attrField"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrField()").WithArguments("attrField"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrProperty()").WithArguments("attrProperty"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrProperty()").WithArguments("attrProperty"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrReturnType()").WithArguments("attrReturnType"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrReturnType()").WithArguments("attrReturnType"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrMethod()").WithArguments("attrMethod"),
                                        Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "attrMethod()").WithArguments("attrMethod"))
        End Sub

        <Fact()>
        Public Sub BC30663ERR_InvalidMultipleAttributeUsage1a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidMultipleAttributeUsage1a">
        <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class A1
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Class A2
    Inherits Attribute
End Class

Partial Class C1
    <A1(), A2()>
    Partial Private Sub M(i As Integer)
    End Sub
End Class

Partial Class C1
    <A1(), A2()>
    Private Sub M(i As Integer)
        Dim s As New C1
        s.M(i)
    End Sub
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "A1()").WithArguments("A1"))
        End Sub

        <Fact()>
        Public Sub BC30663ERR_InvalidMultipleAttributeUsage1b()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidMultipleAttributeUsage1b">
        <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class A1
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Class A2
    Inherits Attribute
End Class

Partial Class C1
    <A2(), A1(), A2()>
    Partial Private Sub M(i As Integer)
    End Sub
End Class

Partial Class C1
    <A1(), A1(), A2()>
    Private Sub M(i As Integer)
        Dim s As New C1
        s.M(i)
    End Sub
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "A1()").WithArguments("A1"),
                                      Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "A1()").WithArguments("A1"))
        End Sub

        <Fact()>
        Public Sub BC30663ERR_InvalidMultipleAttributeUsage1c()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidMultipleAttributeUsage1c">
        <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class A1
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Class A2
    Inherits Attribute
End Class

Partial Class C1
    Partial Private Sub M(<A1(), A2()>i As Integer)
    End Sub
End Class

Partial Class C1
    Private Sub M(<A1(), A2()>i As Integer)
        Dim s As New C1
        s.M(i)
    End Sub
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "A1()").WithArguments("A1"))
        End Sub


        <Fact()>
        Public Sub BC30663ERR_InvalidMultipleAttributeUsage1d()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidMultipleAttributeUsage1d">
        <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class A1
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Class A2
    Inherits Attribute
End Class

Partial Class C1
    Partial Private Sub M(<A2(), A1(), A2()>i As Integer)
    End Sub
End Class

Partial Class C1
    Private Sub M(<A1(), A1(), A2()>i As Integer)
        Dim s As New C1
        s.M(i)
    End Sub
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "A1()").WithArguments("A1"),
                                      Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "A1()").WithArguments("A1"))
        End Sub

        <Fact>
        Public Sub BC30668ERR_UseOfObsoleteSymbol2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UseOfObsoleteSymbol2">
        <file name="a.vb"><![CDATA[
            Imports System
            Namespace NS
                Class clstest
                    <Obsolete("Scenario6 Message", True)> Protected Friend WriteOnly Property scenario6()
                        Set(ByVal Value)
                        End Set
                    End Property
                    <Obsolete("", True)> Public Shared WriteOnly Property scenario7()
                        Set(ByVal Value)
                        End Set
                    End Property
                    <Obsolete()> Default Property scenario8(ByVal i As Integer) As Integer
                        Get
                            Return 1
                        End Get
                        Set(ByVal Value As Integer)
                        End Set
                    End Property
                End Class
                Class clsTest1
                    <Obsolete("", True)> Default Public ReadOnly Property scenario9(ByVal i As Long) As Long
                        Get
                            Return 1
                        End Get
                    End Property
                End Class
                Friend Module OBS022mod
                    <Obsolete("Scenario1 Message", True)> WriteOnly Property Scenario1a()
                        Set(ByVal Value)
                        End Set
                    End Property
                    <Obsolete("Scenario2  Message", True)> ReadOnly Property scenario2()
                        Get
                            Return 1
                        End Get
                    End Property
                    Sub OBS022()
                        Dim obj As Object = 23%
                        Dim cls1 As New clstest()
                        'COMPILEERROR: BC30668, "Scenario1a"
                        Scenario1a = obj
                        'COMPILEERROR: BC30668, "scenario2"
                        obj = scenario2
                        'COMPILEERROR: BC30668, "cls1.scenario6"
                        cls1.scenario6 = obj
                        'COMPILEERROR: BC30668, "clstest.scenario7"
                        clstest.scenario7 = obj
                        Dim cls2 As New clsTest1()
                        'COMPILEERROR: BC30668, "cls2"
                        obj = cls2(4)
                    End Sub
                End Module
            End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30668: 'Public WriteOnly Property Scenario1a As Object' is obsolete: 'Scenario1 Message'.
                        Scenario1a = obj
                        ~~~~~~~~~~
BC30668: 'Public ReadOnly Property scenario2 As Object' is obsolete: 'Scenario2  Message'.
                        obj = scenario2
                              ~~~~~~~~~
BC30668: 'Protected Friend WriteOnly Property scenario6 As Object' is obsolete: 'Scenario6 Message'.
                        cls1.scenario6 = obj
                        ~~~~~~~~~~~~~~
BC31075: 'Public Shared WriteOnly Property scenario7 As Object' is obsolete.
                        clstest.scenario7 = obj
                        ~~~~~~~~~~~~~~~~~
BC31075: 'Public ReadOnly Default Property scenario9(i As Long) As Long' is obsolete.
                        obj = cls2(4)
                              ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(538173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538173")>
        <Fact>
        Public Sub BC30683ERR_InheritsStmtWrongOrder()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InheritsStmtWrongOrder">
        <file name="a.vb"><![CDATA[
            Class cn1
                Public ss As Long
                'COMPILEERROR: BC30683, "Inherits c2"
                Inherits c2
            End Class
            Class c2
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30683: 'Inherits' statement must precede all declarations in a class.
                Inherits c2
                ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseParseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30688ERR_InterfaceEventCantUse1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceEventCantUse1">
        <file name="a.vb"><![CDATA[
           Interface I 
                Event Goo() Implements I.Goo
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30688: Events in interfaces cannot be declared 'Implements'.
                Event Goo() Implements I.Goo
                            ~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30695ERR_MustShadow2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustShadow2">
        <file name="a.vb"><![CDATA[
           Class c1
                'COMPILEERROR: BC30695, "goo", 
                Sub goo()
                End Sub
                Shadows Function goo(ByVal i As Integer) As Integer
                End Function
            End Class
            Class c2_1
                Public goo As Integer
            End Class
            Class c2_2
                Inherits c2_1
                Shadows Sub goo()
                End Sub
                'COMPILEERROR: BC30695,"goo"
                Sub goo(ByVal c As Char)
                End Sub
                'COMPILEERROR: BC30695,"goo"
                Sub goo(ByVal d As Double)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30695: sub 'goo' must be declared 'Shadows' because another member with this name is declared 'Shadows'.
                Sub goo()
                    ~~~
BC30695: sub 'goo' must be declared 'Shadows' because another member with this name is declared 'Shadows'.
                Sub goo(ByVal c As Char)
                    ~~~
BC40004: sub 'goo' conflicts with variable 'goo' in the base class 'c2_1' and should be declared 'Shadows'.
                Sub goo(ByVal c As Char)
                    ~~~
BC30695: sub 'goo' must be declared 'Shadows' because another member with this name is declared 'Shadows'.
                Sub goo(ByVal d As Double)
                    ~~~
BC40004: sub 'goo' conflicts with variable 'goo' in the base class 'c2_1' and should be declared 'Shadows'.
                Sub goo(ByVal d As Double)
                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30695ERR_MustShadow2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="MustBeOverloads2">
                    <file name="a.vb"><![CDATA[
                    Class Base
                        Sub Method(x As Integer)
                        End Sub
                        Overloads Sub Method(x As String)
                        End Sub
                    End Class
                    Partial Class Derived1
                        Inherits Base
                        Shadows Sub Method(x As String)
                        End Sub
                    End Class
                    ]]></file>
                    <file name="b.vb"><![CDATA[
                    Class Derived1
                        Inherits Base
                        Overrides Sub Method(x As Integer)
                        End Sub
                    End Class
                    Class Derived2
                        Inherits Base
                        Function Method(x As String, y As Integer) As String
                        End Function
                        Overrides Sub Method(x As Integer)
                        End Sub
                    End Class
                    ]]></file>
                </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31409: sub 'Method' must be declared 'Overloads' because another 'Method' is declared 'Overloads' or 'Overrides'.
                        Sub Method(x As Integer)
                            ~~~~~~
BC30695: sub 'Method' must be declared 'Shadows' because another member with this name is declared 'Shadows'.
                        Overrides Sub Method(x As Integer)
                                      ~~~~~~
BC31086: 'Public Overrides Sub Method(x As Integer)' cannot override 'Public Sub Method(x As Integer)' because it is not declared 'Overridable'.
                        Overrides Sub Method(x As Integer)
                                      ~~~~~~
BC31409: function 'Method' must be declared 'Overloads' because another 'Method' is declared 'Overloads' or 'Overrides'.
                        Function Method(x As String, y As Integer) As String
                                 ~~~~~~
BC40003: function 'Method' shadows an overloadable member declared in the base class 'Base'.  If you want to overload the base method, this method must be declared 'Overloads'.
                        Function Method(x As String, y As Integer) As String
                                 ~~~~~~
BC31086: 'Public Overrides Sub Method(x As Integer)' cannot override 'Public Sub Method(x As Integer)' because it is not declared 'Overridable'.
                        Overrides Sub Method(x As Integer)
                                      ~~~~~~
                         ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30696ERR_OverloadWithOptionalTypes2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverloadWithOptionalTypes2">
        <file name="a.vb"><![CDATA[
           Class Cla30696
                'COMPILEERROR: BC30696, "goo"
                Public Function goo(Optional ByVal arg As ULong = 1)
                    Return "BC30696"
                End Function
                Public Function goo(Optional ByVal arg As Integer = 1)
                    Return "BC30696"
                End Function

                'COMPILEERROR: BC30696, "goo3"
                Public Function goo1(ByVal arg As Integer, Optional ByVal arg1 As String = "")
                    Return "BC30696"
                End Function
                Public Function goo1(ByVal arg As Integer, Optional ByVal arg1 As ULong = 1)
                    Return "BC30696"
                End Function
            End Class
            Interface Scen2_1
                'COMPILEERROR: BC30696, "goo"
                Function goo(Optional ByVal arg As Object = Nothing)
                Function goo(Optional ByVal arg As ULong = 1)
                'COMPILEERROR: BC30696, "goo3"
                Function goo1(ByVal arg As Integer, Optional ByVal arg1 As Object = Nothing)
                Function goo1(ByVal arg As Integer, Optional ByVal arg1 As ULong = 1)
            End Interface
        ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, <errors><![CDATA[]]></errors>)
        End Sub

        <WorkItem(529018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529018")>
        <Fact()>
        Public Sub BC30697ERR_OverrideWithOptionalTypes2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverrideWithOptionalTypes2">
        <file name="a.vb"><![CDATA[
            Class Base
                Public Overridable Sub goo(ByVal x As String, Optional ByVal y As String = "hello")
                End Sub
            End Class
            Class C1
                Inherits Base
                Public Overrides Sub goo(ByVal x As String, Optional ByVal y As Integer = 1)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30697: 'Public Overrides Sub goo(x As String, [y As Integer = 1])' cannot override 'Public Overridable Sub goo(x As String, [y As String = "hello"])' because they differ by the types of optional parameters.
                Public Overrides Sub goo(ByVal x As String, Optional ByVal y As Integer = 1)
                                     ~~~                                      
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30728ERR_StructsCannotHandleEvents()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StructsCannotHandleEvents">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Event e()
                Sub goo() Handles c.e
                End Sub
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30728: Methods declared in structures cannot have 'Handles' clauses.
                Sub goo() Handles c.e
                    ~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
                Sub goo() Handles c.e
                                  ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30730ERR_OverridesImpliesOverridable()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverridesImpliesOverridable">
        <file name="a.vb"><![CDATA[
           Class CBase
                overridable function goo
                End function
            End Class
            Class C1
                Inherits CBase
                Overrides public Overridable function goo
                End function
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30730: Methods declared 'Overrides' cannot be declared 'Overridable' because they are implicitly overridable.
                Overrides public Overridable function goo
                                 ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Checks for ERRID_ModuleCantUseMemberSpecifier1
        ' Old name="ModifierErrorsInsideModules"
        <Fact>
        Public Sub BC30735ERR_ModuleCantUseTypeSpecifier1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
        Module m1
            protected Class c
                Protected Enum e1
                    a
                End Enum
            End Class

            Protected Enum e5
                a
            End Enum

            Shadows Enum e6
                a
             End Enum

            protected structure struct1
            end structure

            protected delegate Sub d1(i as integer)

        End Module
        ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30735: Type in a Module cannot be declared 'protected'.
            protected Class c
            ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
            Protected Enum e5
            ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Shadows'.
            Shadows Enum e6
            ~~~~~~~
BC30735: Type in a Module cannot be declared 'protected'.
            protected structure struct1
            ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'protected'.
            protected delegate Sub d1(i as integer)
            ~~~~~~~~~

                       ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC30770ERR_DefaultEventNotFound1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="DefaultEventNotFound1">
        <file name="a.vb"><![CDATA[
        Imports System.ComponentModel

<DefaultEvent("LogonCompleted")> Public Class EventSource1
    Private Event LogonCompleted(ByVal UserName As String)
End Class

<DefaultEvent("LogonCompleteD")> Public Class EventSource2
    Public Event LogonCompleted(ByVal UserName As String)
End Class

<DefaultEvent("LogonCompleteD")> Public Class EventSource22
    Friend Event LogonCompleted(ByVal UserName As String)
End Class

<DefaultEvent("LogonCompleteD")> Public Class EventSource23
    Protected Event LogonCompleted(ByVal UserName As String)
End Class

<DefaultEvent("LogonCompleteD")> Public Class EventSource24
    Protected Friend Event LogonCompleted(ByVal UserName As String)
End Class

<DefaultEvent(Nothing)> Public Class EventSource3
End Class

<DefaultEvent("")> Public Class EventSource4
End Class

<DefaultEvent("  ")> Public Class EventSource5
End Class

Class Base
    Public Event LogonCompleted()
End Class

<DefaultEvent("LogonCompleted")> Class EventSource6
    Inherits Base
    Private Shadows Event LogonCompleted(ByVal UserName As String)
End Class

<DefaultEvent("LogonCompleted")> Interface EventSource7
End Interface

<DefaultEvent("LogonCompleted")> Structure EventSource8
End Structure
        ]]></file>
    </compilation>, {SystemRef}, TestOptions.ReleaseDll)
            Dim expectedErrors = <errors><![CDATA[
BC30770: Event 'LogonCompleted' specified by the 'DefaultEvent' attribute is not a publicly accessible event for this class.
<DefaultEvent("LogonCompleted")> Public Class EventSource1
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30770: Event 'LogonCompleteD' specified by the 'DefaultEvent' attribute is not a publicly accessible event for this class.
<DefaultEvent("LogonCompleteD")> Public Class EventSource23
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30770: Event 'LogonCompleteD' specified by the 'DefaultEvent' attribute is not a publicly accessible event for this class.
<DefaultEvent("LogonCompleteD")> Public Class EventSource24
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30770: Event '  ' specified by the 'DefaultEvent' attribute is not a publicly accessible event for this class.
<DefaultEvent("  ")> Public Class EventSource5
 ~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'DefaultEventAttribute' cannot be applied to 'EventSource7' because the attribute is not valid on this declaration type.
<DefaultEvent("LogonCompleted")> Interface EventSource7
 ~~~~~~~~~~~~
BC30662: Attribute 'DefaultEventAttribute' cannot be applied to 'EventSource8' because the attribute is not valid on this declaration type.
<DefaultEvent("LogonCompleted")> Structure EventSource8
 ~~~~~~~~~~~~
                       ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact, WorkItem(545966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545966")>
        Public Sub BC30772ERR_InvalidNonSerializedUsage()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidNonSerializedUsage">
    <file name="a.vb"><![CDATA[
Imports System

Module M1
    <NonSerialized>
    Dim x = 1

    <NonSerialized> 
    Event E As System.Action
End Module
]]></file>
</compilation>)

            Dim expectedErrors =
<errors><![CDATA[
BC30772: 'NonSerialized' attribute will not have any effect on this member because its containing class is not exposed as 'Serializable'.
    <NonSerialized>
     ~~~~~~~~~~~~~
BC30772: 'NonSerialized' attribute will not have any effect on this member because its containing class is not exposed as 'Serializable'.
    <NonSerialized> 
     ~~~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC30786ERR_ModuleCantUseDLLDeclareSpecifier1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ModuleCantUseDLLDeclareSpecifier1">
        <file name="a.vb"><![CDATA[
        Module M
            Protected Declare Sub Goo Lib "My" ()
        End Module
        ]]></file>
    </compilation>)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ModuleCantUseDLLDeclareSpecifier1, "Protected").WithArguments("Protected"))
        End Sub

        <Fact>
        Public Sub BC30791ERR_StructCantUseDLLDeclareSpecifier1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StructCantUseDLLDeclareSpecifier1">
        <file name="a.vb"><![CDATA[
        Structure M
            Protected Declare Sub Goo Lib "My" ()
        End Structure
        ]]></file>
    </compilation>)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StructCantUseDLLDeclareSpecifier1, "Protected").WithArguments("Protected"))
        End Sub

        <Fact>
        Public Sub BC30795ERR_SharedStructMemberCannotSpecifyNew()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SharedStructMemberCannotSpecifyNew">
        <file name="a.vb"><![CDATA[
        Structure S1
            ' does not work
            Dim structVar1 As New System.ApplicationException

            ' works
            Shared structVar2 As New System.ApplicationException
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC30795: Non-shared members in a Structure cannot be declared 'New'.
            Dim structVar1 As New System.ApplicationException
                              ~~~
                       ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC31049ERR_SharedStructMemberCannotSpecifyInitializers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30795ERR_SharedStructMemberCannotSpecifyInitializers">
        <file name="a.vb"><![CDATA[
        Structure S1
            ' does not work
            Dim structVar1 As System.ApplicationException = New System.ApplicationException()

            ' works
            Shared structVar2 As System.ApplicationException = New System.ApplicationException()
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC31049: Initializers on structure members are valid only for 'Shared' members and constants.
            Dim structVar1 As System.ApplicationException = New System.ApplicationException()
                ~~~~~~~~~~
                                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC30798ERR_InvalidTypeForAliasesImport2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InvalidTypeForAliasesImport2">
        <file name="a.vb"><![CDATA[
Imports aa = System.Action           'BC30798
Imports bb = ns1.Intfc2.intfc2goo    'BC40056

Namespace ns1
    Public Class Intfc2
        Public intfc2goo As Integer
    End Class
End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30798: 'Action' for the Imports alias to 'Action' does not refer to a Namespace, Class, Structure, Interface, Enum or Module.
Imports aa = System.Action           'BC30798
        ~~~~~~~~~~~~~~~~~~
BC40056: Namespace or type specified in the Imports 'ns1.Intfc2.intfc2goo' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports bb = ns1.Intfc2.intfc2goo    'BC40056
             ~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        <WorkItem(13926, "https://github.com/dotnet/roslyn/issues/13926")>
        Public Sub BadAliasTarget()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports BadAlias = unknown

Public Class Class1
    Public Shared Sub Main()
    End Sub

    Function Test() As BadAlias 
        Return Nothing
    End Function
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40056: Namespace or type specified in the Imports 'unknown' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports BadAlias = unknown
                   ~~~~~~~
BC31208: Type or namespace 'unknown' is not defined.
    Function Test() As BadAlias 
                       ~~~~~~~~
                 ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

            Dim test = compilation1.GetTypeByMetadataName("Class1").GetMember(Of MethodSymbol)("Test")

            Assert.True(test.ReturnType.IsErrorType())
            Assert.Equal(DiagnosticSeverity.Error, DirectCast(test.ReturnType, ErrorTypeSymbol).ErrorInfo.Severity)
        End Sub

        <Fact>
        Public Sub BC30828ERR_ObsoleteAsAny()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ObsoleteAsAny">
        <file name="a.vb"><![CDATA[
        Class C1
            Declare Sub goo Lib "a.dll" (ByRef bit As Any)
        End Class
        ]]></file>
    </compilation>)

            compilation1.VerifyDiagnostics(Diagnostic(ERRID.ERR_ObsoleteAsAny, "Any").WithArguments("Any"))
        End Sub

        <Fact>
        Public Sub BC30906ERR_OverrideWithArrayVsParamArray2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverrideWithArrayVsParamArray2">
        <file name="a.vb"><![CDATA[
        Class C1
            Overridable Sub goo(ByVal a As Integer())
            End Sub
        End Class
        Class C2
            Inherits C1
            Overrides Sub goo(ByVal ParamArray a As Integer())
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30906: 'Public Overrides Sub goo(ParamArray a As Integer())' cannot override 'Public Overridable Sub goo(a As Integer())' because they differ by parameters declared 'ParamArray'.
            Overrides Sub goo(ByVal ParamArray a As Integer())
                          ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30907ERR_CircularBaseDependencies4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CircularBaseDependencies4">
        <file name="a.vb"><![CDATA[
        Class A
            Inherits B
        End Class
        Class B
            Inherits C
        End Class
        Class C
            Inherits A.D
        End Class
        Partial Class A
            Class D
            End Class
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30907: This inheritance causes circular dependencies between class 'A' and its nested or base type '
    'A' inherits from 'B'.
    'B' inherits from 'C'.
    'C' inherits from 'A.D'.
    'A.D' is nested in 'A'.'.
            Inherits B
                     ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30908ERR_NestedBase2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NestedBase2">
        <file name="a.vb"><![CDATA[
        NotInheritable Class cls1b
            Inherits cls1a
            MustInherit Class cls1a
                Sub subScen1()
                    gstrexpectedresult = "Scen1"
                End Sub
            End Class
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31446: Class 'cls1b' cannot reference its nested type 'cls1b.cls1a' in Inherits clause.
            Inherits cls1a
                     ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30909ERR_AccessMismatchOutsideAssembly4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Public Class PublicClass
            Protected Friend Structure ProtectedFriendStructure
            End Structure
        End Class
        Friend Class FriendClass
        End Class
        Friend Structure FriendStructure
        End Structure
        Friend Interface FriendInterface
        End Interface
        Friend Enum FriendEnum
            A
        End Enum
        Public Class A
            Inherits PublicClass
            Public F As ProtectedFriendStructure
            Public G As FriendClass
            Public H As FriendStructure
        End Class
        Public Structure B
            Public F As FriendInterface
            Public G As FriendEnum
        End Structure
        Public Class C
            Inherits PublicClass
            Public Function F() As ProtectedFriendStructure
            End Function
            Public Sub M(x As FriendClass)
            End Sub
        End Class
        Public Interface I
            Function F(x As FriendStructure, y As FriendInterface) As FriendEnum
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30909: 'F' cannot expose type 'PublicClass.ProtectedFriendStructure' outside the project through class 'A'.
            Public F As ProtectedFriendStructure
                        ~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'G' cannot expose type 'FriendClass' outside the project through class 'A'.
            Public G As FriendClass
                        ~~~~~~~~~~~
BC30909: 'H' cannot expose type 'FriendStructure' outside the project through class 'A'.
            Public H As FriendStructure
                        ~~~~~~~~~~~~~~~
BC30909: 'F' cannot expose type 'FriendInterface' outside the project through structure 'B'.
            Public F As FriendInterface
                        ~~~~~~~~~~~~~~~
BC30909: 'G' cannot expose type 'FriendEnum' outside the project through structure 'B'.
            Public G As FriendEnum
                        ~~~~~~~~~~
BC30909: 'F' cannot expose type 'PublicClass.ProtectedFriendStructure' outside the project through class 'C'.
            Public Function F() As ProtectedFriendStructure
                                   ~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'x' cannot expose type 'FriendClass' outside the project through class 'C'.
            Public Sub M(x As FriendClass)
                              ~~~~~~~~~~~
BC30909: 'x' cannot expose type 'FriendStructure' outside the project through interface 'I'.
            Function F(x As FriendStructure, y As FriendInterface) As FriendEnum
                            ~~~~~~~~~~~~~~~
BC30909: 'y' cannot expose type 'FriendInterface' outside the project through interface 'I'.
            Function F(x As FriendStructure, y As FriendInterface) As FriendEnum
                                                  ~~~~~~~~~~~~~~~
BC30909: 'F' cannot expose type 'FriendEnum' outside the project through interface 'I'.
            Function F(x As FriendStructure, y As FriendInterface) As FriendEnum
                                                                      ~~~~~~~~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30909ERR_AccessMismatchOutsideAssembly4_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Public Class PublicClass
            Protected Friend Structure ProtectedFriendStructure
            End Structure
        End Class
        Friend Class FriendClass
        End Class
        Friend Structure FriendStructure
        End Structure
        Friend Interface FriendInterface
        End Interface
        Friend Enum FriendEnum
            A
        End Enum
        Public Class A
            Inherits PublicClass
            Property P As ProtectedFriendStructure
            Property Q As FriendClass
            Property R As FriendStructure
        End Class
        Public Structure B
            Property P As FriendInterface
            Property Q As FriendEnum
        End Structure
        Public Class C
            Inherits PublicClass
            ReadOnly Property P(x As FriendClass) As ProtectedFriendStructure
                Get
                    Return Nothing
                End Get
            End Property
        End Class
        Public Interface I
            ReadOnly Property P(x As FriendStructure, y As FriendInterface) As FriendEnum
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30909: 'P' cannot expose type 'PublicClass.ProtectedFriendStructure' outside the project through class 'A'.
            Property P As ProtectedFriendStructure
                          ~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'Q' cannot expose type 'FriendClass' outside the project through class 'A'.
            Property Q As FriendClass
                          ~~~~~~~~~~~
BC30909: 'R' cannot expose type 'FriendStructure' outside the project through class 'A'.
            Property R As FriendStructure
                          ~~~~~~~~~~~~~~~
BC30909: 'P' cannot expose type 'FriendInterface' outside the project through structure 'B'.
            Property P As FriendInterface
                          ~~~~~~~~~~~~~~~
BC30909: 'Q' cannot expose type 'FriendEnum' outside the project through structure 'B'.
            Property Q As FriendEnum
                          ~~~~~~~~~~
BC30909: 'x' cannot expose type 'FriendClass' outside the project through class 'C'.
            ReadOnly Property P(x As FriendClass) As ProtectedFriendStructure
                                     ~~~~~~~~~~~
BC30909: 'P' cannot expose type 'PublicClass.ProtectedFriendStructure' outside the project through class 'C'.
            ReadOnly Property P(x As FriendClass) As ProtectedFriendStructure
                                                     ~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'x' cannot expose type 'FriendStructure' outside the project through interface 'I'.
            ReadOnly Property P(x As FriendStructure, y As FriendInterface) As FriendEnum
                                     ~~~~~~~~~~~~~~~
BC30909: 'y' cannot expose type 'FriendInterface' outside the project through interface 'I'.
            ReadOnly Property P(x As FriendStructure, y As FriendInterface) As FriendEnum
                                                           ~~~~~~~~~~~~~~~
BC30909: 'P' cannot expose type 'FriendEnum' outside the project through interface 'I'.
            ReadOnly Property P(x As FriendStructure, y As FriendInterface) As FriendEnum
                                                                               ~~~~~~~~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528153")>
        <Fact()>
        Public Sub BC30909ERR_AccessMismatchOutsideAssembly4_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Public Class PublicClass
            Protected Friend Interface ProtectedFriendInterface
            End Interface
            Protected Friend Class ProtectedFriendClass
            End Class
        End Class
        Friend Interface FriendInterface
        End Interface
        Friend Class FriendClass
        End Class
        Public Class A
            Inherits PublicClass
            Public Sub M(Of T As ProtectedFriendInterface, U As ProtectedFriendClass)()
            End Sub
        End Class
        Public Structure B
            Public Sub M(Of T As FriendInterface, U As FriendClass)()
            End Sub
        End Structure
        Public Interface I
            Function F(Of T As FriendInterface, U As FriendClass)() As Object
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30909: 'M' cannot expose type 'PublicClass.ProtectedFriendInterface' outside the project through class 'A'.
            Public Sub M(Of T As ProtectedFriendInterface, U As ProtectedFriendClass)()
                                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'M' cannot expose type 'PublicClass.ProtectedFriendClass' outside the project through class 'A'.
            Public Sub M(Of T As ProtectedFriendInterface, U As ProtectedFriendClass)()
                                                                ~~~~~~~~~~~~~~~~~~~~
BC30909: 'M' cannot expose type 'FriendInterface' outside the project through structure 'B'.
            Public Sub M(Of T As FriendInterface, U As FriendClass)()
                                 ~~~~~~~~~~~~~~~
BC30909: 'M' cannot expose type 'FriendClass' outside the project through structure 'B'.
            Public Sub M(Of T As FriendInterface, U As FriendClass)()
                                                       ~~~~~~~~~~~
BC30909: 'F' cannot expose type 'FriendInterface' outside the project through interface 'I'.
            Function F(Of T As FriendInterface, U As FriendClass)() As Object
                               ~~~~~~~~~~~~~~~
BC30909: 'F' cannot expose type 'FriendClass' outside the project through interface 'I'.
            Function F(Of T As FriendInterface, U As FriendClass)() As Object
                                                     ~~~~~~~~~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528153")>
        <Fact()>
        Public Sub BC30909ERR_AccessMismatchOutsideAssembly4_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Namespace N
            Public Interface A
                Friend Interface B
                End Interface
                Public Interface C
                    Function F() As B
                    Public Interface D
                        Sub M(o As B)
                    End Interface
                End Interface
            End Interface
            Public Interface E
                Inherits A
                Function F() As B
            End Interface
            Public Interface F
                Sub M(o As A.B)
            End Interface
        End Namespace
        Public Interface G
            Function F() As N.A.B
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30909: 'F' cannot expose type 'A.B' outside the project through interface 'C'.
                    Function F() As B
                                    ~
BC30909: 'o' cannot expose type 'A.B' outside the project through interface 'D'.
                        Sub M(o As B)
                                   ~
BC30909: 'F' cannot expose type 'A.B' outside the project through interface 'E'.
                Function F() As B
                                ~
BC30909: 'o' cannot expose type 'A.B' outside the project through interface 'F'.
                Sub M(o As A.B)
                           ~~~
BC30909: 'F' cannot expose type 'A.B' outside the project through interface 'G'.
            Function F() As N.A.B
                            ~~~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30909ERR_AccessMismatchOutsideAssembly4_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Friend Enum E
            A
        End Enum
        Public Class C
            Public Const F As E = E.A
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30909: 'F' cannot expose type 'E' outside the project through class 'C'.
            Public Const F As E = E.A
                              ~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30910ERR_InheritanceAccessMismatchOutside3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InheritanceAccessMismatchOutside3">
        <file name="a.vb"><![CDATA[
        Friend Interface I1
        End Interface
        Public Interface I2
            Inherits I1
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30910: 'I2' cannot inherit from interface 'I1' because it expands the access of the base interface outside the assembly.
            Inherits I1
                     ~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30911ERR_UseOfObsoletePropertyAccessor3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UseOfObsoletePropertyAccessor3">
        <file name="a.vb"><![CDATA[
        Imports System
        Class C1
            ReadOnly Property p As String
                <Obsolete("hello", True)>
                Get
                    Return "hello"
                End Get
            End Property
        End Class
        Class C2
            Sub goo()
                Dim s As New C1
                Dim a = s.p
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30911: 'Get' accessor of 'Public ReadOnly Property p As String' is obsolete: 'hello'.
                Dim a = s.p
                        ~~~
]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30912ERR_UseOfObsoletePropertyAccessor2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UseOfObsoletePropertyAccessor2">
        <file name="a.vb"><![CDATA[
        Imports System
        Class C1
            ReadOnly Property p As String
                <Obsolete(nothing,True)>
                Get
                    Return "hello"
                End Get
            End Property
        End Class
        Class C2
            Sub goo()
                Dim s As New C1
                Dim a = s.p
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30912: 'Get' accessor of 'Public ReadOnly Property p As String' is obsolete.
                Dim a = s.p
                        ~~~
]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543640, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543640")>
        Public Sub BC30914ERR_AccessMismatchImplementedEvent6()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AccessMismatchImplementedEvent6">
        <file name="a.vb"><![CDATA[
        Public Class C
            Protected Interface i1
                Event goo()
            End Interface
            Friend Class c1
                Implements i1
                Public Event goo() Implements i1.goo
            End Class
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30914: 'goo' cannot expose the underlying delegate type 'C.i1.gooEventHandler' of the event it is implementing to namespace '<Default>' through class 'c1'.
                Public Event goo() Implements i1.goo
                             ~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543641")>
        Public Sub BC30915ERR_AccessMismatchImplementedEvent4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AccessMismatchImplementedEvent4">
        <file name="a.vb"><![CDATA[
        Interface i1
            Event goo()
        End Interface
        Public Class c1
            Implements i1
            Public Event goo() Implements i1.goo
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30915: 'goo' cannot expose the underlying delegate type 'i1.gooEventHandler' of the event it is implementing outside the project through class 'c1'.
            Public Event goo() Implements i1.goo
                         ~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30916ERR_InheritanceCycleInImportedType1()
            Dim C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1
            Dim C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2

            Dim Comp = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb"><![CDATA[
        Class C
            Class C
                Inherits C1
                implements I1
            End Class
        End Class
    ]]></file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, C1, C2})

            Dim expectedErrors = <errors><![CDATA[
BC30916: Type 'C1' is not supported because it either directly or indirectly inherits from itself.
                Inherits C1
                         ~~
BC30916: Type 'C1' is not supported because it either directly or indirectly inherits from itself.
                implements I1
                           ~~
BC30916: Type 'I1' is not supported because it either directly or indirectly inherits from itself.
                implements I1
                           ~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)

        End Sub

        <Fact>
        Public Sub BC30916ERR_InheritanceCycleInImportedType1_2()
            Dim C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1
            Dim C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2

            Dim Comp = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb"><![CDATA[
        Class C
            Inherits C2
            implements I1
        End Class
    ]]></file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, C1, C2})

            Dim expectedErrors = <errors><![CDATA[
BC30916: Type 'C2' is not supported because it either directly or indirectly inherits from itself.
            Inherits C2
                     ~~
BC30916: Type 'C2' is not supported because it either directly or indirectly inherits from itself.
            implements I1
                       ~~
BC30916: Type 'I1' is not supported because it either directly or indirectly inherits from itself.
            implements I1
                       ~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)

        End Sub

        <Fact>
        Public Sub BC30916ERR_InheritanceCycleInImportedType1_3()
            Dim C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1
            Dim C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2

            Dim Comp = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb"><![CDATA[
        Class C
            Inherits C1
            implements I1
        End Class
    ]]></file>
</compilation>,
{TestReferences.NetFx.v4_0_30319.mscorlib, C1, C2})

            Dim expectedErrors = <errors><![CDATA[
BC30916: Type 'C1' is not supported because it either directly or indirectly inherits from itself.
            Inherits C1
                     ~~
BC30916: Type 'C1' is not supported because it either directly or indirectly inherits from itself.
            implements I1
                       ~~
BC30916: Type 'I1' is not supported because it either directly or indirectly inherits from itself.
            implements I1
                       ~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)

        End Sub

        <Fact>
        Public Sub BC30921ERR_InheritsTypeArgAccessMismatch7()
            Dim Comp = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="InheritsTypeArgAccessMismatch7">
    <file name="a.vb"><![CDATA[
        Public Class containingClass
            Public Class baseClass(Of t)
            End Class
            Friend Class derivedClass
                Inherits baseClass(Of internalStructure)
            End Class
            Private Structure internalStructure
                Dim firstMember As Integer
            End Structure
        End Class
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30921: 'derivedClass' cannot inherit from class 'containingClass.baseClass(Of containingClass.internalStructure)' because it expands the access of type 'containingClass.internalStructure' to namespace '<Default>'.
                Inherits baseClass(Of internalStructure)
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC30922ERR_InheritsTypeArgAccessMismatchOutside5()
            Dim Comp = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="InheritsTypeArgAccessMismatchOutside5">
    <file name="a.vb"><![CDATA[
        Public Class baseClass(Of t)
        End Class
        Public Class derivedClass
            Inherits baseClass(Of restrictedStructure)
        End Class
        Friend Structure restrictedStructure
            Dim firstMember As Integer
        End Structure
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30922: 'derivedClass' cannot inherit from class 'baseClass(Of restrictedStructure)' because it expands the access of type 'restrictedStructure' outside the assembly.
            Inherits baseClass(Of restrictedStructure)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(Comp, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC30925ERR_PartialTypeAccessMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PartialTypeAccessMismatch3">
        <file name="a.vb"><![CDATA[
        Class s1
            Partial Protected Class c1
            End Class
            Partial Private Class c1
            End Class
            Partial Friend Class c1
            End Class

            Partial Protected Interface I1
            End Interface
            Partial Private Interface I1
            End Interface
            Partial Friend Interface I1
            End Interface
        End Class

        Partial Public Module m1
        End Module
        Partial Friend Module m1
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30925: Specified access 'Private' for 'c1' does not match the access 'Protected' specified on one of its other partial types.
            Partial Private Class c1
                                  ~~
BC30925: Specified access 'Friend' for 'c1' does not match the access 'Protected' specified on one of its other partial types.
            Partial Friend Class c1
                                 ~~
BC30925: Specified access 'Private' for 'I1' does not match the access 'Protected' specified on one of its other partial types.
            Partial Private Interface I1
                                      ~~
BC30925: Specified access 'Friend' for 'I1' does not match the access 'Protected' specified on one of its other partial types.
            Partial Friend Interface I1
                                     ~~
BC30925: Specified access 'Friend' for 'm1' does not match the access 'Public' specified on one of its other partial types.
        Partial Friend Module m1
                              ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30926ERR_PartialTypeBadMustInherit1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialTypeBadMustInherit1">
        <file name="a.vb"><![CDATA[
        Partial Class C1
            Partial MustInherit Class C2
            End Class
        End Class
        Partial Class C1
            Partial NotInheritable Class C2
            End Class
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30926: 'MustInherit' cannot be specified for partial type 'C2' because it cannot be combined with 'NotInheritable' specified for one of its other partial types.
            Partial MustInherit Class C2
                                      ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' dup of 30607?
        <Fact>
        Public Sub BC30927ERR_MustOverOnNotInheritPartClsMem1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustOverOnNotInheritPartClsMem1">
        <file name="a.vb"><![CDATA[
        Public Class C1
            MustOverride Sub goo()
        End Class
        Partial Public NotInheritable Class C1
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30927: 'MustOverride' cannot be specified on this member because it is in a partial type that is declared 'NotInheritable' in another partial definition.
            MustOverride Sub goo()
            ~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30928ERR_BaseMismatchForPartialClass3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BaseMismatchForPartialClass3">
        <file name="a.vb"><![CDATA[
        Option Strict On

        Interface I1
        End Interface

        Interface I2
        End Interface

        Partial Interface I3
            Inherits I1
        End Interface

        Partial Interface I3
            Inherits I2
        End Interface

        Class TestModule
            Sub Test(x As I3)
                Dim y As I1 = x
                Dim z As I2 = x
            End Sub
        End Class

        Partial Class Cls2(Of T, U)
            Inherits Class1(Of U, T)
        End Class
        Partial Class Cls2(Of T, U)
            Inherits Class1(Of T, T)
        End Class
        Class Class1(Of X, Y)
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30928: Base class 'Class1(Of T, T)' specified for class 'Cls2' cannot be different from the base class 'Class1(Of U, T)' of one of its other partial types.
            Inherits Class1(Of T, T)
                     ~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30931ERR_PartialTypeTypeParamNameMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A(Of T)
    Partial Class B(Of U As A(Of T), V As A(Of V))
    End Class
    Partial Class B(Of X As A(Of T), Y As A(Of Y))
    End Class

    Partial Interface I(Of U As A(Of T), V As A(Of V))
    End Interface
    Partial Interface I(Of X As A(Of T), Y As A(Of Y))
    End Interface
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30931: Type parameter name 'X' does not match the name 'U' of the corresponding type parameter defined on one of the other partial types of 'B'.
    Partial Class B(Of X As A(Of T), Y As A(Of Y))
                       ~
BC30931: Type parameter name 'Y' does not match the name 'V' of the corresponding type parameter defined on one of the other partial types of 'B'.
    Partial Class B(Of X As A(Of T), Y As A(Of Y))
                                     ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'B'.
    Partial Class B(Of X As A(Of T), Y As A(Of Y))
                                     ~
BC30002: Type 'Y' is not defined.
    Partial Class B(Of X As A(Of T), Y As A(Of Y))
                                               ~
BC30931: Type parameter name 'X' does not match the name 'U' of the corresponding type parameter defined on one of the other partial types of 'I'.
    Partial Interface I(Of X As A(Of T), Y As A(Of Y))
                           ~
BC30931: Type parameter name 'Y' does not match the name 'V' of the corresponding type parameter defined on one of the other partial types of 'I'.
    Partial Interface I(Of X As A(Of T), Y As A(Of Y))
                                         ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'I'.
    Partial Interface I(Of X As A(Of T), Y As A(Of Y))
                                         ~
BC30002: Type 'Y' is not defined.
    Partial Interface I(Of X As A(Of T), Y As A(Of Y))
                                                   ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30932ERR_PartialTypeConstraintMismatch1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IA(Of T)
End Interface
Interface IB
End Interface
' Different constraints.
Partial Class A1(Of T As Structure)
End Class
Partial Class A1(Of T As Class)
End Class
Partial Class A2(Of T As Structure, U As IA(Of T))
End Class
Partial Class A2(Of T As Class, U As IB)
End Class
Partial Class A3(Of T As IA(Of T))
End Class
Partial Class A3(Of T As IA(Of IA(Of T)))
End Class
Partial Class A4(Of T As {Structure, IB})
End Class
Partial Class A4(Of T As {Class, IB})
End Class
Partial Structure A5(Of T As {IA(Of T), New})
End Structure
Partial Structure A5(Of T As {IA(Of T), New})
End Structure
Partial Structure A5(Of T As {IB, New})
End Structure
' Additional constraints.
Partial Class B1(Of T As New)
End Class
Partial Class B1(Of T As {Class, New})
End Class
Partial Class B2(Of T, U As {IA(Of T)})
End Class
Partial Class B2(Of T, U As {IB, IA(Of T)})
End Class
' Missing constraints.
Partial Class C1(Of T As {Class, New})
End Class
Partial Class C1(Of T As {New})
End Class
Partial Structure C2(Of T, U As {IB, IA(Of T)})
End Structure
Partial Structure C2(Of T, U As {IA(Of T)})
End Structure
' Same constraints, different order.
Partial Class D1(Of T As {Structure, IA(Of T), IB})
End Class
Partial Class D1(Of T As {IB, IA(Of T), Structure})
End Class
Partial Class D1(Of T As {Structure, IB, IA(Of T)})
End Class
Partial Class D2(Of T, U, V As {T, U})
End Class
Partial Class D2(Of T, U, V As {U, T})
End Class
' Different constraint clauses.
Partial Class E1(Of T, U As T)
End Class
Partial Class E1(Of T As Class, U)
End Class
Partial Class E1(Of T, U As T)
End Class
Partial Class E2(Of T, U As IB)
End Class
Partial Class E2(Of T As IA(Of U), U)
End Class
Partial Class E2(Of T As IB, U)
End Class
' Additional constraint clause.
Partial Class F1(Of T)
End Class
Partial Class F1(Of T)
End Class
Partial Class F1(Of T As Class)
End Class
Partial Class F2(Of T As Class, U)
End Class
Partial Class F2(Of T As Class, U As T)
End Class
' Missing constraint clause.
Partial Class G1(Of T As {Class})
End Class
Partial Class G1(Of T)
End Class
Partial Structure G2(Of T As {Class}, U As {T})
End Structure
Partial Structure G2(Of T As {Class}, U)
End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'A1'.
Partial Class A1(Of T As Class)
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'A2'.
Partial Class A2(Of T As Class, U As IB)
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'A2'.
Partial Class A2(Of T As Class, U As IB)
                                ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'A3'.
Partial Class A3(Of T As IA(Of IA(Of T)))
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'A4'.
Partial Class A4(Of T As {Class, IB})
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'A5'.
Partial Structure A5(Of T As {IB, New})
                        ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'B1'.
Partial Class B1(Of T As {Class, New})
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'B2'.
Partial Class B2(Of T, U As {IB, IA(Of T)})
                       ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'C1'.
Partial Class C1(Of T As {New})
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'C2'.
Partial Structure C2(Of T, U As {IA(Of T)})
                           ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'E1'.
Partial Class E1(Of T As Class, U)
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'E1'.
Partial Class E1(Of T As Class, U)
                                ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'E2'.
Partial Class E2(Of T As IA(Of U), U)
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'E2'.
Partial Class E2(Of T As IA(Of U), U)
                                   ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'E2'.
Partial Class E2(Of T As IB, U)
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'E2'.
Partial Class E2(Of T As IB, U)
                             ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'F1'.
Partial Class F1(Of T As Class)
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'F2'.
Partial Class F2(Of T As Class, U As T)
                                ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'G1'.
Partial Class G1(Of T)
                    ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'G2'.
Partial Structure G2(Of T As {Class}, U)
                                      ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Unrecognized constraint types should not result
        ' in constraint mismatch errors in partial types.
        <Fact()>
        Public Sub BC30932ERR_PartialTypeConstraintMismatch1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Partial Class C(Of T As {Class, Unknown})
End Class
Partial Class C(Of T As {Unknown, Class})
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30002: Type 'Unknown' is not defined.
Partial Class C(Of T As {Class, Unknown})
                                ~~~~~~~
BC30002: Type 'Unknown' is not defined.
Partial Class C(Of T As {Unknown, Class})
                         ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Duplicate constraints in partial types.
        <Fact()>
        Public Sub BC30932ERR_PartialTypeConstraintMismatch1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Partial Class A(Of T As {New, New, I, I})
End Class
Partial Class A(Of T As {New, I})
End Class
Partial Class B(Of T, U As T)
End Class
Partial Class B(Of T, U As {T, T})
End Class
        ]]></file>
    </compilation>)
            ' Note: Dev10 simply reports the duplicate constraint in each case, even
            ' in subsequent partial declarations. Arguably the Dev10 behavior is better.
            Dim expectedErrors1 = <errors><![CDATA[
BC32081: 'New' constraint cannot be specified multiple times for the same type parameter.
Partial Class A(Of T As {New, New, I, I})
                              ~~~
BC32071: Constraint type 'I' already specified for this type parameter.
Partial Class A(Of T As {New, New, I, I})
                                      ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'A'.
Partial Class A(Of T As {New, I})
                   ~
BC30932: Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of 'B'.
Partial Class B(Of T, U As {T, T})
                      ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30935ERR_AmbiguousOverrides3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AmbiguousOverrides3">
        <file name="a.vb"><![CDATA[
        Public Class baseClass(Of t)
            Public Overridable Sub goo(ByVal inputValue As String)
            End Sub
            Public Overridable Sub goo(ByVal inputValue As t)
            End Sub
        End Class
        Public Class derivedClass
            Inherits baseClass(Of String)
            Overrides Sub goo(ByVal inputValue As String)
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30935: Member 'Public Overridable Sub goo(inputValue As String)' that matches this signature cannot be overridden because the class 'baseClass' contains multiple members with this same name and signature: 
   'Public Overridable Sub goo(inputValue As String)'
   'Public Overridable Sub goo(inputValue As t)'
            Overrides Sub goo(ByVal inputValue As String)
                          ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30937ERR_AmbiguousImplements3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AmbiguousImplements3">
        <file name="a.vb"><![CDATA[
        Public Interface baseInterface(Of t)
            Sub doSomething(ByVal inputValue As String)
            Sub doSomething(ByVal inputValue As t)
        End Interface
        Public Class implementingClass
            Implements baseInterface(Of String)
            Sub doSomething(ByVal inputValue As String) _
                Implements baseInterface(Of String).doSomething
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30149: Class 'implementingClass' must implement 'Sub doSomething(inputValue As String)' for interface 'baseInterface(Of String)'.
            Implements baseInterface(Of String)
                       ~~~~~~~~~~~~~~~~~~~~~~~~
BC30937: Member 'baseInterface(Of String).doSomething' that matches this signature cannot be implemented because the interface 'baseInterface(Of String)' contains multiple members with this same name and signature:
   'Sub doSomething(inputValue As String)'
   'Sub doSomething(inputValue As String)'
                Implements baseInterface(Of String).doSomething
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30972ERR_StructLayoutAttributeNotAllowed()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StructLayoutAttributeNotAllowed">
        <file name="a.vb"><![CDATA[
        Option Strict On
        Imports System.Runtime.InteropServices
        <StructLayout(1)>
        Structure C1(Of T)
            Sub goo(ByVal a As T)
            End Sub
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30972: Attribute 'StructLayout' cannot be applied to a generic type.
        <StructLayout(1)>
         ~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31029ERR_EventHandlerSignatureIncompatible2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventHandlerSignatureIncompatible2">
        <file name="a.vb"><![CDATA[
        option strict on
        Class clsTest1
            Event ev1(ByVal ArgC As Char)
        End Class
        Class clsTest2
            Inherits clsTest1
            Shadows Event ev1(ByVal ArgI As Integer)
        End Class
        Class clsTest3
            Dim WithEvents clsTest As clsTest2
            Private Sub subTest(ByVal ArgC As Char) Handles clsTest.ev1
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31029: Method 'subTest' cannot handle event 'ev1' because they do not have a compatible signature.
            Private Sub subTest(ByVal ArgC As Char) Handles clsTest.ev1
                                                                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31029ERR_EventHandlerSignatureIncompatible2a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventHandlerSignatureIncompatible2a">
        <file name="a.vb"><![CDATA[
        option strict off
        Class clsTest1
            Event ev1(ByVal ArgC As Char)
        End Class
        Class clsTest2
            Inherits clsTest1
            Shadows Event ev1(ByVal ArgI As Integer)
        End Class
        Class clsTest3
            Dim WithEvents clsTest As clsTest2
            Private Sub subTest(ByVal ArgC As Char) Handles clsTest.ev1
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31029: Method 'subTest' cannot handle event 'ev1' because they do not have a compatible signature.
            Private Sub subTest(ByVal ArgC As Char) Handles clsTest.ev1
                                                                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542143")>
        <Fact>
        Public Sub BC31033ERR_InterfaceImplementedTwice1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceImplementedTwice1">
        <file name="a.vb"><![CDATA[
        Class C1
            Implements I1(Of Integer), I1(Of Double), I1(Of Integer)        
        End Class
        Interface I1(Of T)
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31033: Interface 'I1(Of Integer)' can be implemented only once by this type.
            Implements I1(Of Integer), I1(Of Double), I1(Of Integer)        
                                                      ~~~~~~~~~~~~~~                                      
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31033ERR_InterfaceImplementedTwice1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceImplementedTwice1">
        <file name="a.vb"><![CDATA[
        Class c3_1
            Implements i3_1
        End Class
        Class c3_2
            Inherits c3_1
            Implements i3_1, i3_1, i3_1
        End Class
        Interface i3_1
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31033: Interface 'i3_1' can be implemented only once by this type.
            Implements i3_1, i3_1, i3_1
                             ~~~~
BC31033: Interface 'i3_1' can be implemented only once by this type.
            Implements i3_1, i3_1, i3_1
                                   ~~~~                                      
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31035ERR_InterfaceNotImplemented1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceNotImplemented1">
        <file name="a.vb"><![CDATA[
        Interface I
            Sub S()
        End Interface
        Class C1
            Implements I
            Public Sub S() Implements I.S
            End Sub
        End Class
        Class C2
            Inherits C1
            Public Sub F() Implements I.S
            End Sub
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31035: Interface 'I' is not implemented by this class.
            Public Sub F() Implements I.S
                                      ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31041ERR_BadInterfaceMember()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadInterfaceMember">
        <file name="a.vb"><![CDATA[
        Interface Interface1
            Module Module1
            End Module
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30603: Statement cannot appear within an interface body.
            Module Module1
            ~~~~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
            End Module
            ~~~~~~~~~~
BC30622: 'End Module' must be preceded by a matching 'Module'.
            End Module
            ~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseParseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31043ERR_ArrayInitInStruct_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC31043ERR_ArrayInitInStruct_1">
        <file name="a.vb"><![CDATA[
        Public Structure S1
            Public j(10) As Integer
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31043: Arrays declared as structure members cannot be declared with an initial size.
            Public j(10) As Integer
                   ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31043ERR_ArrayInitInStruct_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC31043ERR_ArrayInitInStruct_2">
        <file name="a.vb"><![CDATA[
        Public Structure S1
            Public Shared j(10) As Integer
        End Structure
        ]]></file>
    </compilation>)
            CompilationUtils.AssertNoDeclarationDiagnostics(compilation1)
        End Sub

        <Fact>
        Public Sub BC31043ERR_ArrayInitInStruct_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC31043ERR_ArrayInitInStruct_3">
        <file name="a.vb"><![CDATA[
        Public Structure S1
            Public k(10) As Integer = {1}
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31043: Arrays declared as structure members cannot be declared with an initial size.
            Public k(10) As Integer = {1}
                   ~~~~~
BC31049: Initializers on structure members are valid only for 'Shared' members and constants.
            Public k(10) As Integer = {1}
                   ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31043ERR_ArrayInitInStruct_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ArrayInitInStruct_4">
        <file name="a.vb"><![CDATA[
        Public Structure S1
            Public l(10), m(1), n As Integer
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31043: Arrays declared as structure members cannot be declared with an initial size.
            Public l(10), m(1), n As Integer
                   ~~~~~
BC31043: Arrays declared as structure members cannot be declared with an initial size.
            Public l(10), m(1), n As Integer
                          ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31044ERR_EventTypeNotDelegate()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventTypeNotDelegate">
        <file name="a.vb"><![CDATA[
        Public Class C1
            Public Event E As String
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31044: Events declared with an 'As' clause must have a delegate type.
            Public Event E As String
                              ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31047ERR_ProtectedTypeOutsideClass()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ProtectedTypeOutsideClass">
        <file name="a.vb"><![CDATA[
        Protected Enum Enum11
            Apple
        End Enum
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31047: Protected types can only be declared inside of a class.
Protected Enum Enum11
               ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Checks for ERRID_StructCantUseVarSpecifier1 
        ' Oldname"ModifierErrorsInsideStructures"
        <Fact>
        Public Sub BC31047ERR_ProtectedTypeOutsideClass_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[

            Structure s
                Protected Enum e3
                    a
                End Enum

                Private Enum e4
                    x
                End Enum

               protected delegate Sub d1(i as integer)

               Protected Structure s_s1
               end structure

            End Structure

           ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC31047: Protected types can only be declared inside of a class.
                Protected Enum e3
                               ~~
BC31047: Protected types can only be declared inside of a class.
               protected delegate Sub d1(i as integer)
                                      ~~
BC31047: Protected types can only be declared inside of a class.
               Protected Structure s_s1
                                   ~~~~                                     
                       ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC31048ERR_DefaultPropertyWithNoParams()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class A
            Default Property P()
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
        End Class
        Class B
            Default ReadOnly Property Q(ParamArray x As Object())
                Get
                    Return Nothing
                End Get
            End Property
        End Class
        Interface IA
            Default WriteOnly Property P()
        End Interface
        Interface IB
            Default Property Q(ParamArray x As Object())
        End Interface
        ]]></file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31048: Properties with no required parameters cannot be declared 'Default'.
            Default Property P()
                             ~
BC31048: Properties with no required parameters cannot be declared 'Default'.
            Default ReadOnly Property Q(ParamArray x As Object())
                                      ~
BC31048: Properties with no required parameters cannot be declared 'Default'.
            Default WriteOnly Property P()
                                       ~
BC31048: Properties with no required parameters cannot be declared 'Default'.
            Default Property Q(ParamArray x As Object())
                             ~
                 ]]></errors>)
        End Sub

        <Fact>
        Public Sub BC31048ERR_DefaultPropertyWithNoParams_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IA
    Default Property P(Optional o As Object = Nothing)
    Property Q(Optional o As Object = Nothing)
End Interface
Interface IB
    Default Property P(x As Object, Optional y As Integer = 1)
    Default Property P(Optional x As Integer = 0, Optional y As Integer = 1)
    Property Q(Optional x As Integer = 0, Optional y As Integer = 1)
End Interface
        ]]></file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31048: Properties with no required parameters cannot be declared 'Default'.
    Default Property P(Optional o As Object = Nothing)
                     ~
BC31048: Properties with no required parameters cannot be declared 'Default'.
    Default Property P(Optional x As Integer = 0, Optional y As Integer = 1)
                     ~
                 ]]></errors>)
        End Sub

        <Fact>
        Public Sub BC31049ERR_InitializerInStruct()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InitializerInStruct">
        <file name="a.vb"><![CDATA[
        Structure S1
            ' does not work
            Dim i As Integer = 10

            ' works
            const j as Integer  = 10
            shared k as integer = 10
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31049: Initializers on structure members are valid only for 'Shared' members and constants.
            Dim i As Integer = 10
                ~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31051ERR_DuplicateImport1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateImport1">
        <file name="a.vb"><![CDATA[
        Imports ns1.genclass(Of String)
        Imports ns1.genclass(Of String)
        Namespace ns1
            Class genclass(Of T)
            End Class
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31051: Namespace or type 'genclass(Of String)' has already been imported.
        Imports ns1.genclass(Of String)
                ~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31051ERR_DuplicateImport1_GlobalImports()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateImport1_GlobalImports">
        <file name="a.vb"><![CDATA[
        Imports System.Collections
        Imports System.Collections
        Class C
        End Class
        ]]></file>
    </compilation>, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithGlobalImports(
                                GlobalImport.Parse(
                                    {"System.Collections", "System.Collections"}
                                )
                    ))

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31051: Namespace or type 'System.Collections' has already been imported.
        Imports System.Collections
                ~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31051ERR_DuplicateImport1_GlobalImports_NoErrors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC31051ERR_DuplicateImport1_GlobalImports_NoErrors">
        <file name="a.vb"><![CDATA[
        Imports System.Collections
        Class C
        End Class
        ]]></file>
    </compilation>, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithGlobalImports(
                                GlobalImport.Parse(
                                    {"System.Collections", "System.Collections"}
                                )
                    ))

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, <errors><![CDATA[]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31052ERR_BadModuleFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC31052ERR_BadModuleFlags1">
        <file name="a.vb"><![CDATA[
        NotInheritable Module M1
        End Module
        shared Module M2
        End Module
        readonly Module M3
        End Module
        overridable Module M5
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31052: Modules cannot be declared 'NotInheritable'.
NotInheritable Module M1
~~~~~~~~~~~~~~
BC31052: Modules cannot be declared 'shared'.
        shared Module M2
        ~~~~~~
BC31052: Modules cannot be declared 'readonly'.
        readonly Module M3
        ~~~~~~~~
BC31052: Modules cannot be declared 'overridable'.
        overridable Module M5
        ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31060ERR_SynthMemberClashesWithMember5_1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
        Imports System
        Module M1
            Class Cls1
                Event e()
            End Class
            Event obj1()
            Dim obj1event As [Delegate]
        End Module
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_SynthMemberClashesWithMember5, "obj1").WithArguments("event", "obj1", "obj1Event", "module", "M1"))
        End Sub

        <Fact>
        Public Sub BC31060ERR_SynthMemberClashesWithMember5_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Property P
            Shared Property Q
            Property r
            Shared Property s
            Property T
            Shared Property u
            Function get_p()
                Return Nothing
            End Function
            Shared Function set_p()
                Return Nothing
            End Function
            Shared Sub GET_Q()
            End Sub
            Sub SET_Q()
            End Sub
            Dim get_R
            Shared set_R
            Shared Property get_s
            Property set_s
            Class get_T
            End Class
            Structure set_T
            End Structure
            Enum get_U
                X
            End Enum
        End Class
        ]]></file>
    </compilation>)
            ' Since nested types are bound before members, we report 31061 for
            ' cases where nested types conflict with implicit property members.
            ' This differs from Dev10 which reports 31060 in all these cases.
            Dim expectedErrors1 = <errors><![CDATA[
BC31060: property 'P' implicitly defines 'get_P', which conflicts with a member of the same name in class 'C'.
            Property P
                     ~
BC31060: property 'P' implicitly defines 'set_P', which conflicts with a member of the same name in class 'C'.
            Property P
                     ~
BC31060: property 'Q' implicitly defines 'get_Q', which conflicts with a member of the same name in class 'C'.
            Shared Property Q
                            ~
BC31060: property 'Q' implicitly defines 'set_Q', which conflicts with a member of the same name in class 'C'.
            Shared Property Q
                            ~
BC31060: property 'r' implicitly defines 'get_r', which conflicts with a member of the same name in class 'C'.
            Property r
                     ~
BC31060: property 'r' implicitly defines 'set_r', which conflicts with a member of the same name in class 'C'.
            Property r
                     ~
BC31060: property 's' implicitly defines 'get_s', which conflicts with a member of the same name in class 'C'.
            Shared Property s
                            ~
BC31060: property 's' implicitly defines 'set_s', which conflicts with a member of the same name in class 'C'.
            Shared Property s
                            ~
BC31061: class 'get_T' conflicts with a member implicitly declared for property 'T' in class 'C'.
            Class get_T
                  ~~~~~
BC31061: structure 'set_T' conflicts with a member implicitly declared for property 'T' in class 'C'.
            Structure set_T
                      ~~~~~
BC31061: enum 'get_U' conflicts with a member implicitly declared for property 'u' in class 'C'.
            Enum get_U
                 ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31060ERR_SynthMemberClashesWithMember5_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Property P
                Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
            ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            WriteOnly Property R
                Set
                End Set
            End Property
            Private get_P
            Private set_P
            Private get_Q
            Private set_Q
            Private get_R
            Private set_R
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31060: property 'P' implicitly defines 'get_P', which conflicts with a member of the same name in class 'C'.
            Property P
                     ~
BC31060: property 'P' implicitly defines 'set_P', which conflicts with a member of the same name in class 'C'.
            Property P
                     ~
BC31060: property 'Q' implicitly defines 'get_Q', which conflicts with a member of the same name in class 'C'.
            ReadOnly Property Q
                              ~
BC31060: property 'R' implicitly defines 'set_R', which conflicts with a member of the same name in class 'C'.
            WriteOnly Property R
                               ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31060ERR_SynthMemberClashesWithMember5_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Property P
            Shared Property Q
            Property r
            Shared Property s
            Property T
            Shared Property U
            Property v
            Shared Property w
            Property X
            Function _p()
                Return Nothing
            End Function
            Shared Sub _Q()
            End Sub
            Dim _r
            Shared _S
            Shared Property _t
            Property _U
            Class _v
            End Class
            Structure _W
            End Structure
            Enum _x
                X
            End Enum
        End Class
        ]]></file>
    </compilation>)
            ' Since nested types are bound before members, we report 31061 for
            ' cases where nested types conflict with implicit property members.
            ' This differs from Dev10 which reports 31060 in all these cases.
            Dim expectedErrors1 = <errors><![CDATA[
BC31060: property 'P' implicitly defines '_P', which conflicts with a member of the same name in class 'C'.
            Property P
                     ~
BC31060: property 'Q' implicitly defines '_Q', which conflicts with a member of the same name in class 'C'.
            Shared Property Q
                            ~
BC31060: property 'r' implicitly defines '_r', which conflicts with a member of the same name in class 'C'.
            Property r
                     ~
BC31060: property 's' implicitly defines '_s', which conflicts with a member of the same name in class 'C'.
            Shared Property s
                            ~
BC31060: property 'T' implicitly defines '_T', which conflicts with a member of the same name in class 'C'.
            Property T
                     ~
BC31060: property 'U' implicitly defines '_U', which conflicts with a member of the same name in class 'C'.
            Shared Property U
                            ~
BC31061: class '_v' conflicts with a member implicitly declared for property 'v' in class 'C'.
            Class _v
                  ~~
BC31061: structure '_W' conflicts with a member implicitly declared for property 'w' in class 'C'.
            Structure _W
                      ~~
BC31061: enum '_x' conflicts with a member implicitly declared for property 'X' in class 'C'.
            Enum _x
                 ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31061ERR_MemberClashesWithSynth6_1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
        Module M1
            Class Cls1
                Event e()
            End Class
            Dim WithEvents ObjEvent As Cls1
            Event Obj()
        End Module
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_MemberClashesWithSynth6, "ObjEvent").WithArguments("WithEvents variable", "ObjEvent", "event", "Obj", "module", "M1"))

        End Sub

        <Fact>
        Public Sub BC31061ERR_MemberClashesWithSynth6_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Function get_p()
                Return Nothing
            End Function
            Shared Function set_p()
                Return Nothing
            End Function
            Shared Sub GET_Q()
            End Sub
            Sub SET_Q()
            End Sub
            Dim get_R
            Shared set_R
            Shared Property get_s
            Property set_s
            Class get_T
            End Class
            Structure set_T
            End Structure
            Enum get_U
                X
            End Enum
            Property P
            Shared Property Q
            Property r
            Shared Property s
            Property T
            Shared Property u
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31061: function 'get_p' conflicts with a member implicitly declared for property 'P' in class 'C'.
            Function get_p()
                     ~~~~~
BC31061: function 'set_p' conflicts with a member implicitly declared for property 'P' in class 'C'.
            Shared Function set_p()
                            ~~~~~
BC31061: sub 'GET_Q' conflicts with a member implicitly declared for property 'Q' in class 'C'.
            Shared Sub GET_Q()
                       ~~~~~
BC31061: sub 'SET_Q' conflicts with a member implicitly declared for property 'Q' in class 'C'.
            Sub SET_Q()
                ~~~~~
BC31061: variable 'get_R' conflicts with a member implicitly declared for property 'r' in class 'C'.
            Dim get_R
                ~~~~~
BC31061: variable 'set_R' conflicts with a member implicitly declared for property 'r' in class 'C'.
            Shared set_R
                   ~~~~~
BC31061: property 'get_s' conflicts with a member implicitly declared for property 's' in class 'C'.
            Shared Property get_s
                            ~~~~~
BC31061: property 'set_s' conflicts with a member implicitly declared for property 's' in class 'C'.
            Property set_s
                     ~~~~~
BC31061: class 'get_T' conflicts with a member implicitly declared for property 'T' in class 'C'.
            Class get_T
                  ~~~~~
BC31061: structure 'set_T' conflicts with a member implicitly declared for property 'T' in class 'C'.
            Structure set_T
                      ~~~~~
BC31061: enum 'get_U' conflicts with a member implicitly declared for property 'u' in class 'C'.
            Enum get_U
                 ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31061ERR_MemberClashesWithSynth6_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Private get_P
            Private set_P
            Private get_Q
            Private set_Q
            Private get_R
            Private set_R
            Property P
                Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            WriteOnly Property R
                Set
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31061: variable 'get_P' conflicts with a member implicitly declared for property 'P' in class 'C'.
            Private get_P
                    ~~~~~
BC31061: variable 'set_P' conflicts with a member implicitly declared for property 'P' in class 'C'.
            Private set_P
                    ~~~~~
BC31061: variable 'get_Q' conflicts with a member implicitly declared for property 'Q' in class 'C'.
            Private get_Q
                    ~~~~~
BC31061: variable 'set_R' conflicts with a member implicitly declared for property 'R' in class 'C'.
            Private set_R
                    ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31061ERR_MemberClashesWithSynth6_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Function _p()
                Return Nothing
            End Function
            Shared Sub _Q()
            End Sub
            Dim _r
            Shared _S
            Shared Property _t
            Property _U
            Class _v
            End Class
            Structure _W
            End Structure
            Enum _x
                X
            End Enum
            Property P
            Shared Property Q
            Property r
            Shared Property s
            Property T
            Shared Property U
            Property v
            Shared Property w
            Property X
        End Class
         ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31061: function '_p' conflicts with a member implicitly declared for property 'P' in class 'C'.
            Function _p()
                     ~~
BC31061: sub '_Q' conflicts with a member implicitly declared for property 'Q' in class 'C'.
            Shared Sub _Q()
                       ~~
BC31061: variable '_r' conflicts with a member implicitly declared for property 'r' in class 'C'.
            Dim _r
                ~~
BC31061: variable '_S' conflicts with a member implicitly declared for property 's' in class 'C'.
            Shared _S
                   ~~
BC31061: property '_t' conflicts with a member implicitly declared for property 'T' in class 'C'.
            Shared Property _t
                            ~~
BC31061: property '_U' conflicts with a member implicitly declared for property 'U' in class 'C'.
            Property _U
                     ~~
BC31061: class '_v' conflicts with a member implicitly declared for property 'v' in class 'C'.
            Class _v
                  ~~
BC31061: structure '_W' conflicts with a member implicitly declared for property 'w' in class 'C'.
            Structure _W
                      ~~
BC31061: enum '_x' conflicts with a member implicitly declared for property 'X' in class 'C'.
            Enum _x
                 ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31063ERR_SetHasOnlyOneParam()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SetHasOnlyOneParam">
        <file name="a.vb"><![CDATA[
        Module M
            WriteOnly Property P(ByVal i As Integer) As Integer
                Set(x As Integer, ByVal Value As Integer)
                End Set
            End Property
            WriteOnly Property Q()
                Set() ' No error
                    If value Is Nothing Then
                    End If
                End Set
            End Property
        End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31063: 'Set' method cannot have more than one parameter.
                Set(x As Integer, ByVal Value As Integer)
                ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31064ERR_SetValueNotPropertyType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SetValueNotPropertyType">
        <file name="a.vb"><![CDATA[
        ' Implicit property type and implicit set argument type (same)
        Structure A
            WriteOnly Property P%
                Set(ap%) ' no error
                End Set
            End Property
            WriteOnly Property Q&
                Set(aq&) ' no error
                End Set
            End Property
            WriteOnly Property R@
                Set(ar@) ' no error
                End Set
            End Property
            WriteOnly Property S!
                Set(as!) ' no error
                End Set
            End Property
            WriteOnly Property T#
                Set(at#) ' no error
                End Set
            End Property
            WriteOnly Property U$
                Set(au$) ' no error
                End Set
            End Property
        End Structure
        ' Implicit property type and explicit set argument type (same)
        Class B
            WriteOnly Property P%
                Set(bp As Integer) ' no error
                End Set
            End Property
            WriteOnly Property Q&
                Set(bq As Long) ' no error
                End Set
            End Property
            WriteOnly Property R@
                Set(br As Decimal) ' no error
                End Set
            End Property
            WriteOnly Property S!
                Set(ba As Single) ' no error
                End Set
            End Property
            WriteOnly Property T#
                Set(bt As Double) ' no error
                End Set
            End Property
            WriteOnly Property U$
                Set(bu As String) ' no error
                End Set
            End Property
        End Class
        ' Explicit property type and explicit set argument type (same)
        Structure C
            WriteOnly Property P As Integer
                Set(cp As Integer) ' no error
                End Set
            End Property
            WriteOnly Property Q As Long
                Set(cq As Long) ' no error
                End Set
            End Property
            WriteOnly Property R As Decimal
                Set(cr As Decimal) ' no error
                End Set
            End Property
            WriteOnly Property S As Single
                Set(cs As Single) ' no error
                End Set
            End Property
            WriteOnly Property T As Double
                Set(ct As Double) ' no error
                End Set
            End Property
            WriteOnly Property U As String
                Set(cu As String) ' no error
                End Set
            End Property
        End Structure
        ' Implicit property type and implicit set argument type (different)
        Class D
            WriteOnly Property P%
                Set(ap&) ' BC31064
                End Set
            End Property
            WriteOnly Property Q&
                Set(aq@) ' BC31064
                End Set
            End Property
            WriteOnly Property R@
                Set(ar!) ' BC31064
                End Set
            End Property
            WriteOnly Property S!
                Set(as#) ' BC31064
                End Set
            End Property
            WriteOnly Property T#
                Set(at$) ' BC31064
                End Set
            End Property
            WriteOnly Property U$
                Set(au%) ' BC31064
                End Set
            End Property
        End Class
        ' Implicit property type and explicit set argument type (different)
        Structure E
            WriteOnly Property P%
                Set(bp As Decimal) ' BC31064
                End Set
            End Property
            WriteOnly Property Q&
                Set(bq As Single) ' BC31064
                End Set
            End Property
            WriteOnly Property R@
                Set(br As Double) ' BC31064
                End Set
            End Property
            WriteOnly Property S!
                Set(ba As String) ' BC31064
                End Set
            End Property
            WriteOnly Property T#
                Set(bt As Integer) ' BC31064
                End Set
            End Property
            WriteOnly Property U$
                Set(bu As Long) ' BC31064
                End Set
            End Property
        End Structure
        ' Explicit property type and explicit set argument type (different)
        Class F
            WriteOnly Property P As Integer
                Set(cp As Single) ' BC31064
                End Set
            End Property
            WriteOnly Property Q As Long
                Set(cq As Double) ' BC31064
                End Set
            End Property
            WriteOnly Property R As Decimal
                Set(cr As String) ' BC31064
                End Set
            End Property
            WriteOnly Property S As Single
                Set(cs As Integer) ' BC31064
                End Set
            End Property
            WriteOnly Property T As Double
                Set(ct As Long) ' BC31064
                End Set
            End Property
            WriteOnly Property U As String
                Set(cu As Decimal) ' BC31064
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(ap&) ' BC31064
                    ~~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(aq@) ' BC31064
                    ~~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(ar!) ' BC31064
                    ~~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(as#) ' BC31064
                    ~~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(at$) ' BC31064
                    ~~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(au%) ' BC31064
                    ~~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(bp As Decimal) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(bq As Single) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(br As Double) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(ba As String) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(bt As Integer) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(bu As Long) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(cp As Single) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(cq As Double) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(cr As String) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(cs As Integer) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(ct As Long) ' BC31064
                    ~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(cu As Decimal) ' BC31064
                    ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31065ERR_SetHasToBeByVal1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SetHasToBeByVal1">
        <file name="a.vb"><![CDATA[
        Class C
            WriteOnly Property P
                Set(ByRef value)
                End Set
            End Property
            WriteOnly Property Q
                Set(ByVal ParamArray value())
                End Set
            End Property
            WriteOnly Property R As Integer()
                Set(ParamArray value As Integer())
                End Set
            End Property
            WriteOnly Property S
                Set(Optional value = Nothing)
                End Set
            End Property
            WriteOnly Property T
                Set(ByVal value)
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31065: 'Set' parameter cannot be declared 'ByRef'.
                Set(ByRef value)
                    ~~~~~
BC31065: 'Set' parameter cannot be declared 'ParamArray'.
                Set(ByVal ParamArray value())
                          ~~~~~~~~~~
BC31064: 'Set' parameter must have the same type as the containing property.
                Set(ByVal ParamArray value())
                                     ~~~~~
BC31065: 'Set' parameter cannot be declared 'ParamArray'.
                Set(ParamArray value As Integer())
                    ~~~~~~~~~~
BC31065: 'Set' parameter cannot be declared 'Optional'.
                Set(Optional value = Nothing)
                    ~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31067ERR_StructureCantUseProtected()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StructureCantUseProtected">
        <file name="a.vb"><![CDATA[
        Structure S
            Protected Sub New(o)
            End Sub
            Protected Friend Sub New(x, y)
            End Sub
            Protected Sub M()
            End Sub
            Protected Friend Function F()
                Return Nothing
            End Function
        End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31067: Method in a structure cannot be declared 'Protected', 'Protected Friend', or 'Private Protected'.
            Protected Sub New(o)
            ~~~~~~~~~
BC31067: Method in a structure cannot be declared 'Protected', 'Protected Friend', or 'Private Protected'.
            Protected Friend Sub New(x, y)
            ~~~~~~~~~~~~~~~~
BC31067: Method in a structure cannot be declared 'Protected', 'Protected Friend', or 'Private Protected'.
            Protected Sub M()
            ~~~~~~~~~
BC31067: Method in a structure cannot be declared 'Protected', 'Protected Friend', or 'Private Protected'.
            Protected Friend Function F()
            ~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31068ERR_BadInterfaceDelegateSpecifier1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadInterfaceDelegateSpecifier1">
        <file name="a.vb"><![CDATA[
        Interface i1
            private Delegate Sub goo
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31068: Delegate in an interface cannot be declared 'private'.
            private Delegate Sub goo
            ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31069ERR_BadInterfaceEnumSpecifier1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadInterfaceEnumSpecifier1">
        <file name="a.vb"><![CDATA[
        Interface I1
            Public Enum E
                ONE
            End Enum
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31069: Enum in an interface cannot be declared 'Public'.
            Public Enum E
            ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31070ERR_BadInterfaceClassSpecifier1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadInterfaceClassSpecifier1">
        <file name="a.vb"><![CDATA[
        Interface I1
            Interface I2
                Protected Class C1
                End Class
                Friend Class C2
                End Class
            End Interface
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31070: Class in an interface cannot be declared 'Protected'.
                Protected Class C1
                ~~~~~~~~~
BC31070: Class in an interface cannot be declared 'Friend'.
                Friend Class C2
                ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31071ERR_BadInterfaceStructSpecifier1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadInterfaceStructSpecifier1">
        <file name="a.vb"><![CDATA[
        Interface I1
            Public Structure S1
            End Structure
        End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31071: Structure in an interface cannot be declared 'Public'.
            Public Structure S1
            ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31075ERR_UseOfObsoleteSymbolNoMessage1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UseOfObsoleteSymbolNoMessage1">
        <file name="a.vb"><![CDATA[
            Imports System
            <Obsolete(Nothing, True)> Interface I1
            End Interface
            Class class1
                Implements I1
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31075: 'I1' is obsolete.
                Implements I1
                           ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31083ERR_ModuleMemberCantImplement()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ModuleMemberCantImplement">
        <file name="a.vb"><![CDATA[
            Module class1
                Interface I1
                    Sub goo()
                End Interface
                'COMPILEERROR: BC31083, "Implements"
                Public Sub goo() Implements I1.goo
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31083: Members in a Module cannot implement interface members.
                Public Sub goo() Implements I1.goo
                                 ~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31084ERR_EventDelegatesCantBeFunctions()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventDelegatesCantBeFunctions">
        <file name="a.vb"><![CDATA[
            Imports System
            Class A 
                Event X As Func(Of String)
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31084: Events cannot be declared with a delegate type that has a return type.
                Event X As Func(Of String)
                           ~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31086ERR_CantOverride4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantOverride4">
        <file name="a.vb"><![CDATA[
            Class C1
                Public Sub F1()
                End Sub
            End Class
            Class B
                Inherits C1
                Public Overrides Sub F1()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31086: 'Public Overrides Sub F1()' cannot override 'Public Sub F1()' because it is not declared 'Overridable'.
                Public Overrides Sub F1()
                                     ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        Private Shared ReadOnly s_typeWithMixedProperty As String = <![CDATA[
.class public auto ansi beforefieldinit Base_VirtGet_Set
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname newslot virtual  
          instance int32  get_Prop() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    ldstr      "Base_VirtGet_Set.Get"
    call       void [mscorlib]System.Console::WriteLine(string)
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  }

  .method public hidebysig specialname 
          instance void  set_Prop(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    ldstr      "Base_VirtGet_Set.Set"
    call       void [mscorlib]System.Console::WriteLine(string)
    .maxstack  8
    IL_0000:  ret
  }
  .property instance int32 Prop()
  {
    .get instance int32 Base_VirtGet_Set::get_Prop()
    .set instance void Base_VirtGet_Set::set_Prop(int32)
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}

.class public auto ansi beforefieldinit Base_Get_VirtSet
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname 
          instance int32  get_Prop() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    ldstr      "Base_Get_VirtSet.Get"
    call       void [mscorlib]System.Console::WriteLine(string)
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  }

  .method public hidebysig specialname newslot virtual  
          instance void  set_Prop(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    ldstr      "Base_Get_VirtSet.Set"
    call       void [mscorlib]System.Console::WriteLine(string)
    IL_0000:  ret
  }
  .property instance int32 Prop()
  {
    .get instance int32 Base_Get_VirtSet::get_Prop()
    .set instance void Base_Get_VirtSet::set_Prop(int32)
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}
]]>.Value.Replace(vbLf, vbCrLf)

        <WorkItem(528982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528982")>
        <Fact()>
        Public Sub BC31086ERR_CantOverride5a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="CantOverride5a">
        <file name="a.vb"><![CDATA[
Class VBDerived
    Inherits Base_Get_VirtSet

    Public Overrides Property Prop As Integer
        Get
            System.Console.WriteLine("VBDerived.Get")
            Return MyBase.Prop
        End Get
        Set(value As Integer)
            System.Console.WriteLine("VBDerived.Set")
            MyBase.Prop = value
        End Set
    End Property

    Shared Sub Main()
        Dim o As Base_Get_VirtSet
        o = New Base_Get_VirtSet()
        o.Prop = o.Prop
        o = New VBDerived()
        o.Prop = o.Prop
    End Sub
End Class
        ]]></file>
    </compilation>, s_typeWithMixedProperty, options:=TestOptions.DebugExe)

            ' There are no Errors, but getter is actually not overridden!!!

            Dim validator = Sub(m As ModuleSymbol)
                                Dim p1 = m.GlobalNamespace.GetMember(Of PropertySymbol)("VBDerived.Prop")

                                Assert.True(p1.IsOverrides)

                                Dim baseP1 As PropertySymbol = p1.OverriddenProperty
                                Assert.True(baseP1.IsOverridable)
                                Assert.False(baseP1.GetMethod.IsOverridable)
                                Assert.True(baseP1.SetMethod.IsOverridable)

                                Dim p1Get = p1.GetMethod
                                Dim p1Set = p1.SetMethod

                                Assert.True(p1Get.IsOverrides)
                                Assert.Same(baseP1.GetMethod, p1Get.OverriddenMethod)
                                Assert.True(p1Set.IsOverrides)
                                Assert.Same(baseP1.SetMethod, p1Set.OverriddenMethod)
                            End Sub

            CompileAndVerify(compilation1, expectedOutput:=
"Base_Get_VirtSet.Get
Base_Get_VirtSet.Set
Base_Get_VirtSet.Get
VBDerived.Set
Base_Get_VirtSet.Set", sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <WorkItem(528982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528982")>
        <Fact()>
        Public Sub BC31086ERR_CantOverride5b()
            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="CantOverride5b">
        <file name="a.vb"><![CDATA[
Class VBDerived
    Inherits Base_VirtGet_Set

    Public Overrides Property Prop As Integer
        Get
            System.Console.WriteLine("VBDerived.Get")
            Return MyBase.Prop
        End Get
        Set(value As Integer)
            System.Console.WriteLine("VBDerived.Set")
            MyBase.Prop = value
        End Set
    End Property

    Shared Sub Main()
        Dim o As Base_VirtGet_Set
        o = New Base_VirtGet_Set()
        o.Prop = o.Prop
        o = New VBDerived()
        o.Prop = o.Prop
    End Sub
End Class
        ]]></file>
    </compilation>, s_typeWithMixedProperty, options:=TestOptions.DebugExe)

            ' There are no Errors, but setter is actually not overridden!!!

            Dim validator = Sub(m As ModuleSymbol)
                                Dim p1 = m.GlobalNamespace.GetMember(Of PropertySymbol)("VBDerived.Prop")

                                Assert.True(p1.IsOverrides)

                                Dim baseP1 As PropertySymbol = p1.OverriddenProperty
                                Assert.True(baseP1.IsOverridable)
                                Assert.True(baseP1.GetMethod.IsOverridable)
                                Assert.False(baseP1.SetMethod.IsOverridable)

                                Dim p1Get = p1.GetMethod
                                Dim p1Set = p1.SetMethod

                                Assert.True(p1Get.IsOverrides)
                                Assert.Same(baseP1.GetMethod, p1Get.OverriddenMethod)
                                Assert.True(p1Set.IsOverrides)
                                Assert.Same(baseP1.SetMethod, p1Set.OverriddenMethod)
                            End Sub

            CompileAndVerify(compilation1, expectedOutput:=
"Base_VirtGet_Set.Get
Base_VirtGet_Set.Set
VBDerived.Get
Base_VirtGet_Set.Get
Base_VirtGet_Set.Set", sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact()>
        Public Sub BC31087ERR_CantSpecifyArraysOnBoth()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CantSpecifyArraysOnBoth">
        <file name="a.vb"><![CDATA[
            Module M
                Sub Goo(ByVal x() As String())
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31087: Array modifiers cannot be specified on both a variable and its type.
                Sub Goo(ByVal x() As String())
                                     ~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31087ERR_CantSpecifyArraysOnBoth_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantSpecifyArraysOnBoth">
        <file name="a.vb"><![CDATA[
            Class C
                Public Shared Sub Main()
                    Dim a()() As Integer = nothing
                    For Each x() As Integer() In a
                    Next
                End Sub
            End Class
        ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected><![CDATA[
BC31087: Array modifiers cannot be specified on both a variable and its type.
                    For Each x() As Integer() In a
                                    ~~~~~~~~~
BC30332: Value of type 'Integer()' cannot be converted to 'Integer()()' because 'Integer' is not derived from 'Integer()'.
                    For Each x() As Integer() In a
                                                 ~
]]></expected>)
        End Sub

        <WorkItem(540876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540876")>
        <Fact>
        Public Sub BC31088ERR_NotOverridableRequiresOverrides()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NotOverridableRequiresOverrides">
        <file name="a.vb"><![CDATA[
            Class C1
                NotOverridable Sub Goo()
                End Sub
                Public NotOverridable Property F As Integer
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
                NotOverridable Sub Goo()
                                   ~~~
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
                Public NotOverridable Property F As Integer
                                               ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31089ERR_PrivateTypeOutsideType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PrivateTypeOutsideType">
        <file name="a.vb"><![CDATA[
            Namespace ns1
                Private Module Mod1
                End Module
            End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31089: Types declared 'Private' must be inside another type.
                Private Module Mod1
                               ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31099ERR_BadPropertyAccessorFlags()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadPropertyAccessorFlags">
        <file name="a.vb"><![CDATA[
            Class C
                Property P
                    Static Get
                        Return Nothing
                    End Get
                    Shared Set
                    End Set
                End Property
                Property Q
                    Partial Get
                        Return Nothing
                    End Get
                    Default Set
                    End Set
                End Property
                Property R
                    MustInherit Get
                        Return Nothing
                    End Get
                    NotInheritable Set
                    End Set
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31099: Property accessors cannot be declared 'Static'.
                    Static Get
                    ~~~~~~
BC31099: Property accessors cannot be declared 'Shared'.
                    Shared Set
                    ~~~~~~
BC31099: Property accessors cannot be declared 'Partial'.
                    Partial Get
                    ~~~~~~~
BC31099: Property accessors cannot be declared 'Default'.
                    Default Set
                    ~~~~~~~
BC31099: Property accessors cannot be declared 'MustInherit'.
                    MustInherit Get
                    ~~~~~~~~~~~
BC31099: Property accessors cannot be declared 'NotInheritable'.
                    NotInheritable Set
                    ~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31100ERR_BadPropertyAccessorFlagsRestrict()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadPropertyAccessorFlagsRestrict">
        <file name="a.vb"><![CDATA[
        Class C
            Public Property P1
                Get
                    Return Nothing
                End Get
                Public Set(ByVal value) ' P1 BC31100
                End Set
            End Property
            Public Property P2
                Get
                    Return Nothing
                End Get
                Friend Set(ByVal value)
                End Set
            End Property
            Public Property P3
                Get
                    Return Nothing
                End Get
                Protected Set(ByVal value)
                End Set
            End Property
            Public Property P4
                Get
                    Return Nothing
                End Get
                Protected Friend Set(ByVal value)
                End Set
            End Property
            Public Property P5
                Get
                    Return Nothing
                End Get
                Private Set(ByVal value)
                End Set
            End Property
            Friend Property Q1
                Public Get ' Q1 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Friend Property Q2
                Friend Get ' Q2 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Friend Property Q3
                Protected Get ' Q3 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Friend Property Q4
                Protected Friend Get ' Q4 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Friend Property Q5
                Private Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Protected Property R1
                Get
                    Return Nothing
                End Get
                Public Set(ByVal value) ' R1 BC31100
                End Set
            End Property
            Protected Property R2
                Get
                    Return Nothing
                End Get
                Friend Set(ByVal value) ' R2 BC31100
                End Set
            End Property
            Protected Property R3
                Get
                    Return Nothing
                End Get
                Protected Set(ByVal value) ' R3 BC31100
                End Set
            End Property
            Protected Property R4
                Get
                    Return Nothing
                End Get
                Protected Friend Set(ByVal value) ' R4 BC31100
                End Set
            End Property
            Protected Property R5
                Get
                    Return Nothing
                End Get
                Private Set(ByVal value)
                End Set
            End Property
            Protected Friend Property S1
                Get
                    Return Nothing
                End Get
                Public Set(ByVal value) ' S1 BC31100
                End Set
            End Property
            Protected Friend Property S2
                Get
                    Return Nothing
                End Get
                Friend Set(ByVal value)
                End Set
            End Property
            Protected Friend Property S3
                Get
                    Return Nothing
                End Get
                Protected Set(ByVal value)
                End Set
            End Property
            Protected Friend Property S4
                Get
                    Return Nothing
                End Get
                Protected Friend Set(ByVal value) ' S4 BC31100
                End Set
            End Property
            Protected Friend Property S5
                Get
                    Return Nothing
                End Get
                Private Set(ByVal value)
                End Set
            End Property
            Private Property T1
                Public Get ' T1 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Private Property T2
                Friend Get ' T2 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Private Property T3
                Protected Get ' T3 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Private Property T4
                Protected Friend Get ' T4 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Private Property T5
                Private Get ' T5 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Property U1
                Public Get ' U1 BC31100
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Property U2
                Friend Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Property U3
                Protected Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Property U4
                Protected Friend Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Property U5
                Private Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31100: Access modifier 'Public' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Public Set(ByVal value) ' P1 BC31100
                ~~~~~~
BC31100: Access modifier 'Public' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Public Get ' Q1 BC31100
                ~~~~~~
BC31100: Access modifier 'Friend' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Friend Get ' Q2 BC31100
                ~~~~~~
BC31100: Access modifier 'Protected' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Protected Get ' Q3 BC31100
                ~~~~~~~~~
BC31100: Access modifier 'Protected Friend' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Protected Friend Get ' Q4 BC31100
                ~~~~~~~~~~~~~~~~
BC31100: Access modifier 'Public' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Public Set(ByVal value) ' R1 BC31100
                ~~~~~~
BC31100: Access modifier 'Friend' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Friend Set(ByVal value) ' R2 BC31100
                ~~~~~~
BC31100: Access modifier 'Protected' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Protected Set(ByVal value) ' R3 BC31100
                ~~~~~~~~~
BC31100: Access modifier 'Protected Friend' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Protected Friend Set(ByVal value) ' R4 BC31100
                ~~~~~~~~~~~~~~~~
BC31100: Access modifier 'Public' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Public Set(ByVal value) ' S1 BC31100
                ~~~~~~
BC31100: Access modifier 'Protected Friend' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Protected Friend Set(ByVal value) ' S4 BC31100
                ~~~~~~~~~~~~~~~~
BC31100: Access modifier 'Public' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Public Get ' T1 BC31100
                ~~~~~~
BC31100: Access modifier 'Friend' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Friend Get ' T2 BC31100
                ~~~~~~
BC31100: Access modifier 'Protected' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Protected Get ' T3 BC31100
                ~~~~~~~~~
BC31100: Access modifier 'Protected Friend' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Protected Friend Get ' T4 BC31100
                ~~~~~~~~~~~~~~~~
BC31100: Access modifier 'Private' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Private Get ' T5 BC31100
                ~~~~~~~
BC31100: Access modifier 'Public' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
                Public Get ' U1 BC31100
                ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31101ERR_OnlyOneAccessorForGetSet()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyOneAccessorForGetSet">
        <file name="a.vb"><![CDATA[
            Class C
                Property P
                    Private Get
                        Return Nothing
                    End Get
                    Protected Set(value)
                    End Set
                End Property
                Property Q
                    Friend Set
                    End Set
                    Friend Get
                        Return Nothing
                    End Get
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31101: Access modifier can only be applied to either 'Get' or 'Set', but not both.
                    Protected Set(value)
                    ~~~~~~~~~~~~~~~~~~~~
BC31101: Access modifier can only be applied to either 'Get' or 'Set', but not both.
                    Friend Get
                    ~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31104ERR_WriteOnlyNoAccessorFlag()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Class C1
                WriteOnly Property Goo(ByVal x As Integer) As Integer
                    Private Set(ByVal value As Integer)
                    End Set
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31104: 'WriteOnly' properties cannot have an access modifier on 'Set'.
                    Private Set(ByVal value As Integer)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31105ERR_ReadOnlyNoAccessorFlag()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Class C1
                ReadOnly Property Goo(ByVal x As Integer) As Integer
                    Protected Get
                        Return 1
                    End Get
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31105: 'ReadOnly' properties cannot have an access modifier on 'Get'.
                    Protected Get
                    ~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31106ERR_BadPropertyAccessorFlags1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadPropertyAccessorFlags1">
        <file name="a.vb"><![CDATA[
            Class A
                Overridable Property P
                Overridable Property Q
                    Get
                        Return Nothing
                    End Get
                    Protected Set
                    End Set
                End Property
            End Class
            Class B
                Inherits A
                NotOverridable Overrides Property P
                    Get
                        Return Nothing
                    End Get
                    Private Set
                    End Set
                End Property
                Public Overrides Property Q
                    Get
                        Return Nothing
                    End Get
                    Protected Set
                    End Set
                End Property
            End Class
            MustInherit Class C
                MustOverride Property P
            End Class
            Class D
                Inherits C
                NotOverridable Overrides Property P
                    Private Get
                        Return Nothing
                    End Get
                    Set
                    End Set
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31106: Property accessors cannot be declared 'Private' in a 'NotOverridable' property.
                    Private Set
                    ~~~~~~~
BC30266: 'Private NotOverridable Overrides Property Set P(Value As Object)' cannot override 'Public Overridable Property Set P(AutoPropertyValue As Object)' because they have different access levels.
                    Private Set
                            ~~~
BC31106: Property accessors cannot be declared 'Private' in a 'NotOverridable' property.
                    Private Get
                    ~~~~~~~
BC30266: 'Private NotOverridable Overrides Property Get P() As Object' cannot override 'Public MustOverride Property Get P() As Object' because they have different access levels.
                    Private Get
                            ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31107ERR_BadPropertyAccessorFlags2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadPropertyAccessorFlags2">
        <file name="a.vb"><![CDATA[
            Class C
                Default Property P(o)
                    Private Get
                        Return Nothing
                    End Get
                    Set
                    End Set
                End Property
            End Class
            Structure S
            Default Property P(x, y)
                    Get
                        Return Nothing
                    End Get
                    Private Set
                    End Set
                End Property
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31107: Property accessors cannot be declared 'Private' in a 'Default' property.
                    Private Get
                    ~~~~~~~
BC31107: Property accessors cannot be declared 'Private' in a 'Default' property.
                    Private Set
                    ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31108ERR_BadPropertyAccessorFlags3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadPropertyAccessorFlags3">
        <file name="a.vb"><![CDATA[
            Class C
                Overridable Property P
                    Private Get
                        Return Nothing
                    End Get
                    Set
                    End Set
                End Property
                Overridable Property Q
                    Get
                        Return Nothing
                    End Get
                    Private Set
                    End Set
                End Property
                Overridable Property R
                    Protected Get
                        Return Nothing
                    End Get
                    Set
                    End Set
                End Property
                Overridable Property S
                    Get
                        Return Nothing
                    End Get
                    Friend Set
                    End Set
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31108: Property cannot be declared 'Overridable' because it contains a 'Private' accessor.
                Overridable Property P
                ~~~~~~~~~~~
BC31108: Property cannot be declared 'Overridable' because it contains a 'Private' accessor.
                Overridable Property Q
                ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31127ERR_DuplicateAddHandlerDef()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateAddHandlerDef">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31127: 'AddHandler' is already declared.
                    AddHandler(ByVal value As EventHandler)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31128ERR_DuplicateRemoveHandlerDef()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateRemoveHandlerDef">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31128: 'RemoveHandler' is already declared.
                    RemoveHandler(ByVal value As EventHandler)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31129ERR_DuplicateRaiseEventDef()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateRaiseEventDef">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31129: 'RaiseEvent' is already declared.
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31130ERR_MissingAddHandlerDef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MissingAddHandlerDef1">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31130: 'AddHandler' definition missing for event 'Public Event Click As EventHandler'.
                Public Custom Event Click As EventHandler
                                    ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31131ERR_MissingRemoveHandlerDef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MissingRemoveHandlerDef1">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31131: 'RemoveHandler' definition missing for event 'Public Event Click As EventHandler'.
                Public Custom Event Click As EventHandler
                                    ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31132ERR_MissingRaiseEventDef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MissingRaiseEventDef1">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31132: 'RaiseEvent' definition missing for event 'Public Event Click As EventHandler'.
                Public Custom Event Click As EventHandler
                                    ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31133ERR_EventAddRemoveHasOnlyOneParam()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventAddRemoveHasOnlyOneParam">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler()
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31133: 'AddHandler' and 'RemoveHandler' methods must have exactly one parameter.
                    AddHandler()
                    ~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31134ERR_EventAddRemoveByrefParamIllegal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventAddRemoveByrefParamIllegal">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RemoveHandler(ByRef value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31134: 'AddHandler' and 'RemoveHandler' method parameters cannot be declared 'ByRef'.
                    RemoveHandler(ByRef value As EventHandler)
                                  ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31136ERR_AddRemoveParamNotEventType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AddRemoveParamNotEventType">
        <file name="a.vb"><![CDATA[
            Option Strict Off
            Imports System
            Public Class M
                Custom Event x As Action
                    AddHandler(ByVal value As Action(Of Integer))
                    End AddHandler
                    RemoveHandler(ByVal value)  
                    End RemoveHandler
                    RaiseEvent()
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31136: 'AddHandler' and 'RemoveHandler' method parameters must have the same delegate type as the containing event.
                    AddHandler(ByVal value As Action(Of Integer))
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31136: 'AddHandler' and 'RemoveHandler' method parameters must have the same delegate type as the containing event.
                    RemoveHandler(ByVal value)  
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31137ERR_RaiseEventShapeMismatch1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RaiseEventShapeMismatch1">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByRef sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31137: 'RaiseEvent' method must have the same signature as the containing event's delegate type 'EventHandler'.
                    RaiseEvent(ByRef sender As Object, ByVal e As EventArgs)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31138ERR_EventMethodOptionalParamIllegal1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventMethodOptionalParamIllegal1">
        <file name="a.vb"><![CDATA[
            Imports System
            Public NotInheritable Class ReliabilityOptimizedControl
                Public Custom Event Click As EventHandler
                    AddHandler(Optional ByVal value As EventHandler = Nothing)
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31138: 'AddHandler', 'RemoveHandler' and 'RaiseEvent' method parameters cannot be declared 'Optional'.
                    AddHandler(Optional ByVal value As EventHandler = Nothing)
                               ~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31142ERR_ObsoleteInvalidOnEventMember()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ObsoleteInvalidOnEventMember">
        <file name="a.vb"><![CDATA[
            Imports System
            Public Class C1
                Custom Event x As Action
                    AddHandler(ByVal value As Action)
                    End AddHandler
                    RemoveHandler(ByVal value As Action)
                    End RemoveHandler
                    <Obsolete()>
                    RaiseEvent()
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31142: 'System.ObsoleteAttribute' cannot be applied to the 'AddHandler', 'RemoveHandler', or 'RaiseEvent' definitions. If required, apply the attribute directly to the event.
                    <Obsolete()>
                    ~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31400ERR_BadStaticLocalInStruct()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadStaticLocalInStruct">
        <file name="a.vb"><![CDATA[
            Structure S 
                Sub Goo()
                    Static x As Integer = 1
                End Sub
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31400: Local variables within methods of structures cannot be declared 'Static'.
                    Static x As Integer = 1
                    ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31400ERR_BadStaticLocalInStruct2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadStaticLocalInStruct2">
        <file name="a.vb"><![CDATA[
            Structure S 
                Sub Goo()
                    Static Static x As Integer
                End Sub
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31400: Local variables within methods of structures cannot be declared 'Static'.
                    Static Static x As Integer
                    ~~~~~~
BC30178: Specifier is duplicated.
                    Static Static x As Integer
                           ~~~~~~
BC42024: Unused local variable: 'x'.
                    Static Static x As Integer
                                  ~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BadLocalspecifiers()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC31400ERR_BadLocalspecifiers">
        <file name="a.vb"><![CDATA[
            Class S 
                Sub Goo()
                    Static Static a As Integer
                    Static Dim Dim b As Integer
                    Static Const Dim c As Integer
                    Private d As Integer
                    Private Private e As Integer
                    Private Const f As Integer
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30178: Specifier is duplicated.
                    Static Static a As Integer
                           ~~~~~~
BC42024: Unused local variable: 'a'.
                    Static Static a As Integer
                                  ~
BC30178: Specifier is duplicated.
                    Static Dim Dim b As Integer
                               ~~~
BC42024: Unused local variable: 'b'.
                    Static Dim Dim b As Integer
                                   ~
BC30246: 'Dim' is not valid on a local constant declaration.
                    Static Const Dim c As Integer
                                 ~~~
BC30438: Constants must have a value.
                    Static Const Dim c As Integer
                                     ~
BC30247: 'Private' is not valid on a local variable declaration.
                    Private d As Integer
                    ~~~~~~~
BC42024: Unused local variable: 'd'.
                    Private d As Integer
                            ~
BC30247: 'Private' is not valid on a local variable declaration.
                    Private Private e As Integer
                    ~~~~~~~
BC30247: 'Private' is not valid on a local variable declaration.
                    Private Private e As Integer
                            ~~~~~~~
BC42024: Unused local variable: 'e'.
                    Private Private e As Integer
                                    ~
BC30247: 'Private' is not valid on a local variable declaration.
                    Private Const f As Integer
                    ~~~~~~~
BC30438: Constants must have a value.
                    Private Const f As Integer
                                  ~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31401ERR_DuplicateLocalStatic1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DuplicateLocalStatic1">
        <file name="a.vb"><![CDATA[
            Module M
                Sub Main()
                    If True Then
                        Static x = 1
                    End If
                    If True Then
                        Static x = 1
                    End If
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31401: Static local variable 'x' is already declared.
                        Static x = 1
                               ~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31403ERR_ImportAliasConflictsWithType2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImportAliasConflictsWithType2">
        <file name="a.vb"><![CDATA[
            Imports System = System 
            Module M
                Sub Main()
                    System.Console.WriteLine()
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31403: Imports alias 'System' conflicts with 'System' declared in the root namespace.
Imports System = System 
        ~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31404ERR_CantShadowAMustOverride1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantShadowAMustOverride1">
        <file name="a.vb"><![CDATA[
            MustInherit Class A
                MustOverride Sub Goo(ByVal x As Integer)
            End Class
            MustInherit Class B
                Inherits A
                Shadows Sub Goo()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31404: 'Public Sub Goo()' cannot shadow a method declared 'MustOverride'.
                Shadows Sub Goo()
                            ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31407ERR_MultipleEventImplMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MultipleEventImplMismatch3">
        <file name="a.vb"><![CDATA[
            Interface I1
                Event evtTest1()
                Event evtTest2()
            End Interface
            Class C1
                Implements I1
                Event evtTest3() Implements I1.evtTest1, I1.evtTest2
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31407: Event 'Public Event evtTest3 As I1.evtTest1EventHandler' cannot implement event 'I1.Event evtTest2()' because its delegate type does not match the delegate type of another event implemented by 'Public Event evtTest3 As I1.evtTest1EventHandler'.
                Event evtTest3() Implements I1.evtTest1, I1.evtTest2
                                                         ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31408ERR_BadSpecifierCombo2_0()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
         Partial NotInheritable Class C1
         End Class
         Partial MustInherit Class C1
         End Class
         Partial MustInherit NotInheritable Class C1
         End Class
         Class C1
         End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30926: 'MustInherit' cannot be specified for partial type 'C1' because it cannot be combined with 'NotInheritable' specified for one of its other partial types.
         Partial MustInherit Class C1
                                   ~~
BC31408: 'MustInherit' and 'NotInheritable' cannot be combined.
         Partial MustInherit NotInheritable Class C1
                             ~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(538931, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538931")>
        <Fact>
        Public Sub BC31408ERR_BadSpecifierCombo2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
         MustInherit NotInheritable Class C1
         End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31408: 'MustInherit' and 'NotInheritable' cannot be combined.
MustInherit NotInheritable Class C1
            ~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(538931, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538931")>
        <Fact>
        Public Sub BC31408ERR_BadSpecifierCombo2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
         Class A1
            Private Overridable Sub scen1()
            End Sub
         End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31408: 'Private' and 'Overridable' cannot be combined.
            Private Overridable Sub scen1()
                    ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31408ERR_BadSpecifierCombo2_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="c.vb"><![CDATA[
        Class C
            ReadOnly WriteOnly Property P
                Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
        End Class
        Structure S
            WriteOnly ReadOnly Property Q
                Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
        End Structure
        Interface I
            ReadOnly WriteOnly Property R
        End Interface
        Class D
            Private Overridable Property S
            Overridable Private Property T
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30635: 'ReadOnly' and 'WriteOnly' cannot be combined.
            ReadOnly WriteOnly Property P
                     ~~~~~~~~~
BC30022: Properties declared 'ReadOnly' cannot have a 'Set'.
                Set
                ~~~
BC30635: 'ReadOnly' and 'WriteOnly' cannot be combined.
            WriteOnly ReadOnly Property Q
                      ~~~~~~~~
BC30023: Properties declared 'WriteOnly' cannot have a 'Get'.
                Get
                ~~~
BC30635: 'ReadOnly' and 'WriteOnly' cannot be combined.
            ReadOnly WriteOnly Property R
                     ~~~~~~~~~
BC31408: 'Private' and 'Overridable' cannot be combined.
            Private Overridable Property S
                    ~~~~~~~~~~~
BC31408: 'Private' and 'Overridable' cannot be combined.
            Overridable Private Property T
                        ~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(541025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541025")>
        <Fact>
        Public Sub BC31408ERR_BadSpecifierCombo2_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
         MustInherit Class A1
            Private MustOverride Sub scen1()
         End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31408: 'Private' and 'MustOverride' cannot be combined.
            Private MustOverride Sub scen1()
                    ~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542159")>
        <Fact>
        Public Sub BC31408ERR_BadSpecifierCombo2_5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Option Infer On
Module M1
    Sub Goo()
    End Sub
End Module
MustInherit Class C1
    MustOverride Sub goo()
    Dim s = (New C2)
End Class
Class C2
    Inherits C1
    Overrides Shadows Sub goo()
    End Sub
End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC31408: 'Overrides' and 'Shadows' cannot be combined.
    Overrides Shadows Sub goo()
              ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(837983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837983")>
        <Fact>
        Public Sub BC31408ERR_BadSpecifierCombo2_6()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System.Collections.Generic

Module Module1
    Sub main()
    End Sub
End Module

Public Class Cls
    Public ValueOfProperty1 As IEnumerable(Of String)
    'COMPILEERROR: BC31408, "Iterator"
    Public WriteOnly Iterator Property WriteOnlyPro1() As IEnumerable(Of String)
        Set(value As IEnumerable(Of String))
            ValueOfProperty1 = value
        End Set
    End Property
End Class

        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC31408: 'Iterator' and 'WriteOnly' cannot be combined.
    Public WriteOnly Iterator Property WriteOnlyPro1() As IEnumerable(Of String)
                     ~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(837993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837993")>
        <Fact>
        Public Sub BC36938ERR_BadIteratorReturn_Property()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
        Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Threading
Imports System.Threading.Tasks
' Bug51817: Incorrect Iterator Modifier in Source Code Causes VBC to exit incorrectly when Msbuild Project
Public Class Scenario2
    Implements IEnumerator(Of Integer)
    Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
        Return True
    End Function
    Public Sub Reset() Implements System.Collections.IEnumerator.Reset
    End Sub
    Public ReadOnly Iterator Property Current As Integer Implements IEnumerator(Of Integer).Current
        'COMPILEERROR: BC36938 ,"Get"
        Get
        End Get
    End Property
    Public ReadOnly Iterator Property Current1 As Object Implements IEnumerator.Current
        'COMPILEERROR: BC36938 ,"Get"
        Get
        End Get
    End Property
#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls
    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
            End If
        End If
        Me.disposedValue = True
    End Sub
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class


        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
    Public ReadOnly Iterator Property Current As Integer Implements IEnumerator(Of Integer).Current
                                                 ~~~~~~~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
    Public ReadOnly Iterator Property Current1 As Object Implements IEnumerator.Current
                                                  ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31409ERR_MustBeOverloads2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustBeOverloads2">
        <file name="a.vb"><![CDATA[
            Class C1
                Overloads Sub goo(ByVal i As Integer)
                End Sub
                Sub goo(ByVal s As String)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31409: sub 'goo' must be declared 'Overloads' because another 'goo' is declared 'Overloads' or 'Overrides'.
                Sub goo(ByVal s As String)
                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31409ERR_MustBeOverloads2_Mixed_Properties_And_Methods()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustBeOverloads2">
        <file name="a.vb"><![CDATA[
            Class OverloadedPropertiesBase
                Overridable Overloads ReadOnly Property Prop(x As Integer) As String
                    Get
                        Return Nothing
                    End Get
                End Property
                Overridable ReadOnly Property Prop(x As String) As String
                    Get
                        Return Nothing
                    End Get
                End Property
                Overridable Overloads Function Prop(x As String) As String
                    Return Nothing
                End Function
                Shadows Function Prop(x As Integer) As Integer
                    Return Nothing
                End Function
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31409: property 'Prop' must be declared 'Overloads' because another 'Prop' is declared 'Overloads' or 'Overrides'.
                Overridable ReadOnly Property Prop(x As String) As String
                                              ~~~~
BC30260: 'Prop' is already declared as 'Public Overridable Overloads ReadOnly Property Prop(x As Integer) As String' in this class.
                Overridable Overloads Function Prop(x As String) As String
                                               ~~~~
BC30260: 'Prop' is already declared as 'Public Overridable Overloads ReadOnly Property Prop(x As Integer) As String' in this class.
                Shadows Function Prop(x As Integer) As Integer
                                 ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31409ERR_MustBeOverloads2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="MustBeOverloads2">
                    <file name="a.vb"><![CDATA[
                    Class Base
                        Sub Method(x As Integer)
                        End Sub
                        Overloads Sub Method(x As String)
                        End Sub
                    End Class
                    Partial Class Derived1
                        Inherits Base
                        Shadows Sub Method(x As String)
                        End Sub
                    End Class
                    ]]></file>
                </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31409: sub 'Method' must be declared 'Overloads' because another 'Method' is declared 'Overloads' or 'Overrides'.
                        Sub Method(x As Integer)
                            ~~~~~~
                         ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' spec changed in Roslyn
        <WorkItem(527642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527642")>
        <Fact>
        Public Sub BC31410ERR_CantOverloadOnMultipleInheritance()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantOverloadOnMultipleInheritance">
        <file name="a.vb"><![CDATA[
            Interface IA
                Overloads Sub Goo(ByVal x As Integer)
            End Interface
            Interface IB
                Overloads Sub Goo(ByVal x As String)
            End Interface
            Interface IC
                Inherits IA, IB
                Overloads Sub Goo()
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31411ERR_MustOverridesInClass1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Public Class C1
                MustOverride Sub goo()
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31411: 'C1' must be declared 'MustInherit' because it contains methods declared 'MustOverride'.
Public Class C1
             ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31411ERR_MustOverridesInClass1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Class C
                MustOverride Property P
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31411: 'C' must be declared 'MustInherit' because it contains methods declared 'MustOverride'.
Class C
      ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31413ERR_SynthMemberShadowsMustOverride5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SynthMemberShadowsMustOverride5">
        <file name="a.vb"><![CDATA[
            MustInherit Class clsTest1
                MustOverride Sub add_e(ByVal ArgX As clsTest2.eEventHandler)
            End Class
            MustInherit Class clsTest2
                Inherits clsTest1
                Shadows Event e()
            End Class
        ]]></file>
    </compilation>)
            ' CONSIDER: Dev11 prints "Sub add_E", rather than "AddHandler Event e", but roslyn's behavior seems friendlier
            ' and more consistent with the way property accessors are displayed (in both dev11 and roslyn).
            Dim expectedErrors1 = <errors><![CDATA[
BC31413: 'Public AddHandler Event e(obj As clsTest2.eEventHandler)', implicitly declared for event 'e', cannot shadow a 'MustOverride' method in the base class 'clsTest1'.
                Shadows Event e()
                              ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540613")>
        <Fact>
        Public Sub BC31417ERR_CannotOverrideInAccessibleMember()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CannotOverrideInAccessibleMember">
        <file name="a.vb"><![CDATA[
            Class Cls1
                Private Overridable Sub goo()
                End Sub
            End Class
            Class Cls2
                Inherits Cls1
                Private Overrides Sub goo()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31408: 'Private' and 'Overridable' cannot be combined.
                Private Overridable Sub goo()
                        ~~~~~~~~~~~
BC31417: 'Private Overrides Sub goo()' cannot override 'Private Sub goo()' because it is not accessible in this context.
                Private Overrides Sub goo()
                                      ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31418ERR_HandlesSyntaxInModule()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="HandlesSyntaxInModule">
        <file name="a.vb"><![CDATA[
Option Strict Off
Module M
    Sub Bar() Handles Me.Goo
    End Sub
    Event Goo()
End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31418: 'Handles' in modules must specify a 'WithEvents' variable qualified with a single identifier.
    Sub Bar() Handles Me.Goo
                      ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31420ERR_ClashWithReservedEnumMember1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ClashWithReservedEnumMember1">
        <file name="a.vb"><![CDATA[
Structure S
    Public Enum InterfaceColors
        value__
    End Enum
End Structure
Class c1
    Public Shared Sub Main(args As String())
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31420: 'value__' conflicts with the reserved member by this name that is implicitly declared in all enums.
        value__
        ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(539947, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539947")>
        <Fact>
        Public Sub BC31420ERR_ClashWithReservedEnumMember2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ClashWithReservedEnumMember1">
        <file name="a.vb"><![CDATA[
Module M
    Public Enum InterfaceColors
        Value__
    End Enum
End Module
Class c1
    Public Shared Sub Main(args As String())
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31420: 'Value__' conflicts with the reserved member by this name that is implicitly declared in all enums.
        Value__
        ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31422ERR_BadUseOfVoid_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Imports System
        Class C1
            Function scen1() As Void
            End Function
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC31422: 'System.Void' can only be used in a GetType expression.
            Function scen1() As Void
                                ~~~~
    ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC31422ERR_BadUseOfVoid_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class C
            Property P As System.Void
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC31422: 'System.Void' can only be used in a GetType expression.
            Property P As System.Void
                          ~~~~~~~~~~~
    ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC31423ERR_EventImplMismatch5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventImplMismatch5">
        <file name="a.vb"><![CDATA[
            Imports System
            Public Interface IA
                Event E()
            End Interface
            Public Class A
                Implements IA
                Public Event E As Action Implements IA.E
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31423: Event 'Public Event E As Action' cannot implement event 'Event E()' on interface 'IA' because their delegate types 'Action' and 'IA.EEventHandler' do not match.
                Public Event E As Action Implements IA.E
                                                    ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(539760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539760")>
        <Fact>
        Public Sub BC31429ERR_MetadataMembersAmbiguous3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MetadataMembersAmbiguous3">
        <file name="a.vb"><![CDATA[
            Class Base
                Public Shared Sub Main
                    Dim a As Integer = 5
                    Dim b As Base = New Derived(a)
                End Sub
                Private Class Derived
                    Inherits Base
                    Private a As Integer
                    Public Sub New(a As Integer)
                        Me.a = a
                    End Sub

                    Public Sub New()
                    End Sub
                End Class
            End Class
            Class Base
                Public Shared Sub Main
                    Dim a As Integer = 5
                    Dim b As Base = New Derived(a)
                End Sub
                Private Class Derived
                    Inherits Base
                    Private a As Integer
                    Public Sub New(a As Integer)
                        Me.a = a
                    End Sub

                    Public Sub New()
                    End Sub
                End Class
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30521: Overload resolution failed because no accessible 'New' is most specific for these arguments:
    'Public Sub New(a As Integer)': Not most specific.
    'Public Sub New(a As Integer)': Not most specific.
                    Dim b As Base = New Derived(a)
                                        ~~~~~~~
BC31429: 'a' is ambiguous because multiple kinds of members with this name exist in class 'Base.Derived'.
                        Me.a = a
                        ~~~~
BC30179: class 'Base' and class 'Base' conflict in namespace '<Default>'.
            Class Base
                  ~~~~
BC30521: Overload resolution failed because no accessible 'New' is most specific for these arguments:
    'Public Sub New(a As Integer)': Not most specific.
    'Public Sub New(a As Integer)': Not most specific.
                    Dim b As Base = New Derived(a)
                                        ~~~~~~~
BC30179: class 'Derived' and class 'Derived' conflict in class 'Base'.
                Private Class Derived
                              ~~~~~~~
BC31429: 'a' is ambiguous because multiple kinds of members with this name exist in class 'Base.Derived'.
                        Me.a = a
                        ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31431ERR_OnlyPrivatePartialMethods1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyPrivatePartialMethods1">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Public Sub goo()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31431: Partial methods must be declared 'Private' instead of 'Public'.
                Partial Public Sub goo()
                        ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31431ERR_OnlyPrivatePartialMethods1a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyPrivatePartialMethods1a">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Protected Overridable Sub goo()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31431: Partial methods must be declared 'Private' instead of 'Protected'.
                Partial Protected Overridable Sub goo()
                        ~~~~~~~~~
BC31431: Partial methods must be declared 'Private' instead of 'Overridable'.
                Partial Protected Overridable Sub goo()
                                  ~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31431ERR_OnlyPrivatePartialMethods1b()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyPrivatePartialMethods1b">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Protected Friend Sub M1()
                End Sub
                Partial Friend Protected Overridable Sub M2()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31431: Partial methods must be declared 'Private' instead of 'Protected Friend'.
                Partial Protected Friend Sub M1()
                        ~~~~~~~~~~~~~~~~
BC31431: Partial methods must be declared 'Private' instead of 'Friend Protected'.
                Partial Friend Protected Overridable Sub M2()
                        ~~~~~~~~~~~~~~~~
BC31431: Partial methods must be declared 'Private' instead of 'Overridable'.
                Partial Friend Protected Overridable Sub M2()
                                         ~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31431ERR_OnlyPrivatePartialMethods1c()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyPrivatePartialMethods1c">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Protected Overridable Friend Sub M()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31431: Partial methods must be declared 'Private' instead of 'Protected'.
                Partial Protected Overridable Friend Sub M()
                        ~~~~~~~~~
BC31431: Partial methods must be declared 'Private' instead of 'Overridable'.
                Partial Protected Overridable Friend Sub M()
                                  ~~~~~~~~~~~
BC31431: Partial methods must be declared 'Private' instead of 'Friend'.
                Partial Protected Overridable Friend Sub M()
                                              ~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31432ERR_PartialMethodsMustBePrivate()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialMethodsMustBePrivate">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Sub Goo()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31432: Partial methods must be declared 'Private'.
                Partial Sub Goo()
                ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31433ERR_OnlyOnePartialMethodAllowed2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyOnePartialMethodAllowed2">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Private Sub Goo()
                End Sub
                Partial Private Sub Goo()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31433: Method 'Goo' cannot be declared 'Partial' because only one method 'Goo' can be marked 'Partial'.
                Partial Private Sub Goo()
                                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31433ERR_OnlyOnePartialMethodAllowed2a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyOnePartialMethodAllowed2a">
        <file name="b.vb"><![CDATA[
            Class C1
                Partial Private Sub Goo()
                End Sub
                Partial Private Sub GoO()
                End Sub
            End Class
        ]]></file>
        <file name="a.vb"><![CDATA[
            Partial Class C1
                Partial Private Sub goo()
                End Sub
                Partial Private Sub GOO()
                End Sub
            End Class
        ]]></file>
    </compilation>)

            ' note the exact methods errors are reported on
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31433: Method 'goo' cannot be declared 'Partial' because only one method 'goo' can be marked 'Partial'.
                Partial Private Sub goo()
                                    ~~~
BC31433: Method 'GOO' cannot be declared 'Partial' because only one method 'GOO' can be marked 'Partial'.
                Partial Private Sub GOO()
                                    ~~~
BC31433: Method 'GoO' cannot be declared 'Partial' because only one method 'GoO' can be marked 'Partial'.
                Partial Private Sub GoO()
                                    ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31433ERR_OnlyOnePartialMethodAllowed2b()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyOnePartialMethodAllowed2b">
        <file name="a.vb"><![CDATA[
            Class CLS
                Partial Private Shared Sub PS(a As Integer)
                End Sub
                Partial Private Shared Sub Ps(a As Integer)
                End Sub
                Private Shared Sub pS(a As Integer)
                End Sub
                Private Shared Sub ps(a As Integer)
                End Sub
                Public Sub PS(a As Integer)
                End Sub
            End Class
        ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC30269: 'Private Shared Sub PS(a As Integer)' has multiple definitions with identical signatures.
                Partial Private Shared Sub PS(a As Integer)
                                           ~~
BC31433: Method 'Ps' cannot be declared 'Partial' because only one method 'Ps' can be marked 'Partial'.
                Partial Private Shared Sub Ps(a As Integer)
                                           ~~
BC31434: Method 'ps' cannot implement partial method 'ps' because 'ps' already implements it. Only one method can implement a partial method.
                Private Shared Sub ps(a As Integer)
                                   ~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31433ERR_OnlyOnePartialMethodAllowed2c()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyOnePartialMethodAllowed2c">
        <file name="a.vb"><![CDATA[
            Class CLS
                Partial Private Shared Sub PS(a As Integer)
                End Sub
                Partial Private Overloads Shared Sub Ps(a As Integer)
                End Sub
                Private Overloads Shared Sub ps(a As Integer)
                End Sub
                Private Shared Sub pS(a As Integer)
                End Sub
            End Class
        ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31409: sub 'PS' must be declared 'Overloads' because another 'PS' is declared 'Overloads' or 'Overrides'.
                Partial Private Shared Sub PS(a As Integer)
                                           ~~
BC31433: Method 'Ps' cannot be declared 'Partial' because only one method 'Ps' can be marked 'Partial'.
                Partial Private Overloads Shared Sub Ps(a As Integer)
                                                     ~~
BC31409: sub 'pS' must be declared 'Overloads' because another 'pS' is declared 'Overloads' or 'Overrides'.
                Private Shared Sub pS(a As Integer)
                                   ~~
BC31434: Method 'pS' cannot implement partial method 'pS' because 'pS' already implements it. Only one method can implement a partial method.
                Private Shared Sub pS(a As Integer)
                                   ~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31434ERR_OnlyOneImplementingMethodAllowed3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyOneImplementingMethodAllowed3">
        <file name="a.vb"><![CDATA[
            Public Class C1
                Partial Private Sub GoO2()
                End Sub
            End Class
            Partial Public Class C1
                Private Sub GOo2()
                End Sub
            End Class
            Partial Public Class C1
                Private Sub Goo2()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31434: Method 'Goo2' cannot implement partial method 'Goo2' because 'Goo2' already implements it. Only one method can implement a partial method.
                Private Sub Goo2()
                            ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31434ERR_OnlyOneImplementingMethodAllowed3a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OnlyOneImplementingMethodAllowed3a">
        <file name="b.vb"><![CDATA[
            Public Class C1
                Partial Private Sub GoO2()
                End Sub
            End Class
            Partial Public Class C1
                Private Sub GOo2()
                End Sub
            End Class
            Partial Public Class C1
                Private Sub Goo2()
                End Sub
            End Class
        ]]></file>
        <file name="a.vb"><![CDATA[
            Partial Public Class C1
                Private Sub GOO2()
                End Sub
            End Class
            Partial Public Class C1
                Private Sub GoO2()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31434: Method 'GOO2' cannot implement partial method 'GOO2' because 'GOO2' already implements it. Only one method can implement a partial method.
                Private Sub GOO2()
                            ~~~~
BC31434: Method 'GoO2' cannot implement partial method 'GoO2' because 'GoO2' already implements it. Only one method can implement a partial method.
                Private Sub GoO2()
                            ~~~~
BC31434: Method 'Goo2' cannot implement partial method 'Goo2' because 'Goo2' already implements it. Only one method can implement a partial method.
                Private Sub Goo2()
                            ~~~~
                                  ]]></errors>

            ' note the exact methods errors are reported on

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31437ERR_PartialMethodsMustBeSub1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialMethodsMustBeSub1">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Function Goo() As Boolean
                    Return True
                End Function
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 =
<errors><![CDATA[
BC31437: 'Goo' cannot be declared 'Partial' because partial methods must be Subs.
                Partial Function Goo() As Boolean
                                 ~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31437ERR_PartialMethodsMustBeSub2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialMethodsMustBeSub2">
        <file name="a.vb"><![CDATA[
            Class C1
                Partial Private Sub Goo()
                End Sub
                Partial Function Goo() As Boolean
                    Return True
                End Function
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 =
<errors><![CDATA[
BC30301: 'Private Sub Goo()' and 'Public Function Goo() As Boolean' cannot overload each other because they differ only by return types.
                Partial Private Sub Goo()
                                    ~~~
BC31437: 'Goo' cannot be declared 'Partial' because partial methods must be Subs.
                Partial Function Goo() As Boolean
                                 ~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31438ERR_PartialMethodGenericConstraints2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IA(Of T)
End Interface
Interface IB
End Interface
Class C(Of X)
    ' Different constraints.
    Partial Private Sub A1(Of T As Structure)()
    End Sub
    Private Sub A1(Of T As Class)()
    End Sub
    Partial Private Sub A2(Of T As Structure, U As IA(Of T))()
    End Sub
    Private Sub A2(Of T As Structure, U As IB)()
    End Sub
    Partial Private Sub A3(Of T As IA(Of T))()
    End Sub
    Private Sub A3(Of T As IA(Of IA(Of T)))()
    End Sub
    Partial Private Sub A4(Of T As {Structure, IA(Of T)}, U)()
    End Sub
    Private Sub A4(Of T As {Structure, IA(Of U)}, U)()
    End Sub
    ' Additional constraints.
    Partial Private Sub B1(Of T)()
    End Sub
    Private Sub B1(Of T As New)()
    End Sub
    Partial Private Sub B2(Of T As {X, New})()
    End Sub
    Private Sub B2(Of T As {X, Class, New})()
    End Sub
    Partial Private Sub B3(Of T As IA(Of T), U)()
    End Sub
    Private Sub B3(Of T As {IB, IA(Of T)}, U)()
    End Sub
    ' Missing constraints.
    Partial Private Sub C1(Of T As Class)()
    End Sub
    Private Sub C1(Of T)()
    End Sub
    Partial Private Sub C2(Of T As {Class, New})()
    End Sub
    Private Sub C2(Of T As {Class})()
    End Sub
    Partial Private Sub C3(Of T, U As {IB, IA(Of T)})()
    End Sub
    Private Sub C3(Of T, U As IA(Of T))()
    End Sub
    ' Same constraints, different order.
    Private Sub D1(Of T As {IA(Of T), IB})()
    End Sub
    Partial Private Sub D1(Of T As {IB, IA(Of T)})()
    End Sub
    Private Sub D2(Of T, U, V As {T, U, X})()
    End Sub
    Partial Private Sub D2(Of T, U, V As {U, X, T})()
    End Sub
    ' Different constraint clauses.
    Private Sub E1(Of T, U As T)()
    End Sub
    Partial Private Sub E1(Of T As Class, U)()
    End Sub
    ' Additional constraint clause.
    Private Sub F1(Of T As Class, U)()
    End Sub
    Partial Private Sub F1(Of T As Class, U As T)()
    End Sub
    ' Missing constraint clause.
    Private Sub G1(Of T As Class, U As T)()
    End Sub
    Partial Private Sub G1(Of T As Class, U)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31438: Method 'A1' does not have the same generic constraints as the partial method 'A1'.
    Private Sub A1(Of T As Class)()
                ~~
BC31438: Method 'A2' does not have the same generic constraints as the partial method 'A2'.
    Private Sub A2(Of T As Structure, U As IB)()
                ~~
BC31438: Method 'A3' does not have the same generic constraints as the partial method 'A3'.
    Private Sub A3(Of T As IA(Of IA(Of T)))()
                ~~
BC31438: Method 'A4' does not have the same generic constraints as the partial method 'A4'.
    Private Sub A4(Of T As {Structure, IA(Of U)}, U)()
                ~~
BC31438: Method 'B1' does not have the same generic constraints as the partial method 'B1'.
    Private Sub B1(Of T As New)()
                ~~
BC31438: Method 'B2' does not have the same generic constraints as the partial method 'B2'.
    Private Sub B2(Of T As {X, Class, New})()
                ~~
BC31438: Method 'B3' does not have the same generic constraints as the partial method 'B3'.
    Private Sub B3(Of T As {IB, IA(Of T)}, U)()
                ~~
BC31438: Method 'C1' does not have the same generic constraints as the partial method 'C1'.
    Private Sub C1(Of T)()
                ~~
BC31438: Method 'C2' does not have the same generic constraints as the partial method 'C2'.
    Private Sub C2(Of T As {Class})()
                ~~
BC31438: Method 'C3' does not have the same generic constraints as the partial method 'C3'.
    Private Sub C3(Of T, U As IA(Of T))()
                ~~
BC31438: Method 'E1' does not have the same generic constraints as the partial method 'E1'.
    Private Sub E1(Of T, U As T)()
                ~~
BC31438: Method 'F1' does not have the same generic constraints as the partial method 'F1'.
    Private Sub F1(Of T As Class, U)()
                ~~
BC31438: Method 'G1' does not have the same generic constraints as the partial method 'G1'.
    Private Sub G1(Of T As Class, U As T)()
                ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31438ERR_PartialMethodGenericConstraints2a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections
Class Base(Of T As Class)
    Class Derived
        Inherits Base(Of Base(Of String))

        Partial Private Sub Goo(Of S As {Base(Of String), IComparable})(i As Integer)
        End Sub

        Private Sub GOO(Of S As {IComparable, Base(Of IDisposable)})(i As Integer)
        End Sub

        Private Sub GoO(Of s As {IComparable, Base(Of IEnumerable)})(i As Integer)
        End Sub

        Private Sub gOo(Of s As {IComparable, Base(Of IComparable)})(i As Integer)
        End Sub
    End Class
End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31438: Method 'GOO' does not have the same generic constraints as the partial method 'Goo'.
        Private Sub GOO(Of S As {IComparable, Base(Of IDisposable)})(i As Integer)
                    ~~~
BC31434: Method 'GoO' cannot implement partial method 'GoO' because 'GoO' already implements it. Only one method can implement a partial method.
        Private Sub GoO(Of s As {IComparable, Base(Of IEnumerable)})(i As Integer)
                    ~~~
BC31434: Method 'gOo' cannot implement partial method 'gOo' because 'gOo' already implements it. Only one method can implement a partial method.
        Private Sub gOo(Of s As {IComparable, Base(Of IComparable)})(i As Integer)
                    ~~~
]]></errors>)
            ' NOTE: Dev10 reports three BC31438 in this case
        End Sub

        <Fact()>
        Public Sub BC31438ERR_PartialMethodGenericConstraints2b()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections
Public Class Base(Of T As Class)
    Public Class Derived
        Inherits Base(Of Base(Of String))

        Partial Private Sub Goo(Of S As {Base(Of String), IComparable})(i As Integer)
        End Sub
        Private Sub gOo(Of s As {IComparable, C.I})(i As Integer)
        End Sub

        Partial Private Sub Bar(Of S As {C.I})(i As Integer)
        End Sub
        Private Sub bar(Of s As {C.I})(i As Integer)
        End Sub

        Public Class C
            Interface I
            End Interface
        End Class
    End Class
End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31438: Method 'gOo' does not have the same generic constraints as the partial method 'Goo'.
        Private Sub gOo(Of s As {IComparable, C.I})(i As Integer)
                    ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31439ERR_PartialDeclarationImplements1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialDeclarationImplements1">
        <file name="a.vb"><![CDATA[
           Imports System
            Class A
                Implements IDisposable
                Partial Private Sub Dispose() Implements IDisposable.Dispose
                End Sub
                Private Sub Dispose()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30149: Class 'A' must implement 'Sub Dispose()' for interface 'IDisposable'.
                Implements IDisposable
                           ~~~~~~~~~~~
BC31439: Partial method 'Dispose' cannot use the 'Implements' keyword.
                Partial Private Sub Dispose() Implements IDisposable.Dispose
                                    ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31439ERR_PartialDeclarationImplements1a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialDeclarationImplements1a">
        <file name="a.vb"><![CDATA[
           Imports System
            Class A
                Implements IDisposable
                Partial Private Sub Dispose() Implements IDisposable.Dispose
                End Sub
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC30149: Class 'A' must implement 'Sub Dispose()' for interface 'IDisposable'.
                Implements IDisposable
                           ~~~~~~~~~~~
BC31439: Partial method 'Dispose' cannot use the 'Implements' keyword.
                Partial Private Sub Dispose() Implements IDisposable.Dispose
                                    ~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31441ERR_ImplementationMustBePrivate2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementationMustBePrivate2">
        <file name="a.vb"><![CDATA[
            Partial Class C1
                Partial Private Sub GOO()
                End Sub
            End Class
            Partial Class C1
                Sub GOO()
                    'HELLO
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31441: Method 'GOO' must be declared 'Private' in order to implement partial method 'GOO'.
                Sub GOO()
                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31441ERR_ImplementationMustBePrivate2a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementationMustBePrivate2a">
        <file name="a.vb"><![CDATA[
            Partial Class C1
                Partial Private Sub GOO()
                End Sub
            End Class
            Partial Class C1
                Sub Goo()
                End Sub
                Private Sub gOO()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31441: Method 'Goo' must be declared 'Private' in order to implement partial method 'GOO'.
                Sub Goo()
                    ~~~
BC31434: Method 'gOO' cannot implement partial method 'gOO' because 'gOO' already implements it. Only one method can implement a partial method.
                Private Sub gOO()
                            ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31442ERR_PartialMethodParamNamesMustMatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PartialMethodParamNamesMustMatch3">
        <file name="a.vb"><![CDATA[
            Module M 
                Partial Private Sub Goo(ByVal x As Integer)
                End Sub
                Private Sub Goo(ByVal y As Integer)
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31442: Parameter name 'y' does not match the name of the corresponding parameter, 'x', defined on the partial method declaration 'Goo'.
                Private Sub Goo(ByVal y As Integer)
                                      ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC31442ERR_PartialMethodParamNamesMustMatch3a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PartialMethodParamNamesMustMatch3a">
        <file name="a.vb"><![CDATA[
            Module M 
                Partial Private Sub Goo(ByVal x As Integer, a As Integer)
                End Sub
                Private Sub Goo(ByVal x As Integer, b As Integer)
                End Sub
                Private Sub Goo(ByVal y As Integer, b As Integer)
                End Sub
                Private Sub Goo(ByVal y As Integer, b As Integer)
                End Sub
            End Module
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC31442: Parameter name 'b' does not match the name of the corresponding parameter, 'a', defined on the partial method declaration 'Goo'.
                Private Sub Goo(ByVal x As Integer, b As Integer)
                                                    ~
BC31434: Method 'Goo' cannot implement partial method 'Goo' because 'Goo' already implements it. Only one method can implement a partial method.
                Private Sub Goo(ByVal y As Integer, b As Integer)
                            ~~~
BC31434: Method 'Goo' cannot implement partial method 'Goo' because 'Goo' already implements it. Only one method can implement a partial method.
                Private Sub Goo(ByVal y As Integer, b As Integer)
                            ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31443ERR_PartialMethodTypeParamNameMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PartialMethodTypeParamNameMismatch3">
        <file name="a.vb"><![CDATA[
            Module M
                Partial Private Sub Goo(Of S)()
                End Sub
                Private Sub Goo(Of T)()
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31443: Name of type parameter 'T' does not match 'S', the corresponding type parameter defined on the partial method declaration 'Goo'.
                Private Sub Goo(Of T)()
                                   ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC31503ERR_AttributeMustBeClassNotStruct1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AttributeMustBeClassNotStruct1">
        <file name="a.vb"><![CDATA[
            Structure s1
            End Structure
            <s1()>
            Class C1
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31503: 's1' cannot be used as an attribute because it is not a class.
            <s1()>
             ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540625")>
        <Fact>
        Public Sub BC31504ERR_AttributeMustInheritSysAttr()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AttributeMustInheritSysAttr">
        <file name="at31504.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    'Inherits Attribute
End Class

<MyAttribute()>
Class Test

End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_AttributeMustInheritSysAttr, "MyAttribute").WithArguments("MyAttribute"))
        End Sub

        <WorkItem(540628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540628")>
        <Fact>
        Public Sub BC31506ERR_AttributeCannotBeAbstract()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AttributeCannotBeAbstract">
        <file name="at31506.vb"><![CDATA[
Imports System
<AttributeUsage(AttributeTargets.All)>
Public MustInherit Class MyAttribute
    Inherits Attribute
    Public Sub New()

    End Sub
End Class

<My()>
Class Goo
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_AttributeCannotBeAbstract, "My").WithArguments("MyAttribute"))
        End Sub

        <WorkItem(540628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540628")>
        <Fact>
        Public Sub BC31507ERR_AttributeCannotHaveMustOverride()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AttributeCannotHaveMustOverride">
        <file name="at31500.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public MustInherit Class MyAttribute
    Inherits Attribute
    Public Sub New()
    End Sub
    'MustOverride Property AbsProp As Byte
    Public MustOverride Sub AbsSub()
End Class

<My()>
Class Goo
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_AttributeCannotBeAbstract, "My").WithArguments("MyAttribute"))

        End Sub

        <Fact>
        Public Sub BC31512ERR_STAThreadAndMTAThread0()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="STAThreadAndMTAThread0">
    <file name="a.vb"><![CDATA[
Imports System
Class C1
    <MTAThread>
    <STAThread>
    Sub goo()
    End Sub
End Class
]]></file>
</compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC31512: 'System.STAThreadAttribute' and 'System.MTAThreadAttribute' cannot both be applied to the same method.
    Sub goo()
        ~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' BC31523ERR_DllImportNotLegalOnDeclare 
        ' BC31524ERR_DllImportNotLegalOnGetOrSet
        ' BC31526ERR_DllImportOnGenericSubOrFunction
        ' see AttributeTests

        <Fact()>
        Public Sub BC31527ERR_ComClassOnGeneric()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ComClassOnGeneric">
        <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic

            <ComClass()>
            Class C1(Of T)
                Sub GOO(ByVal s As T)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31527: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to a class that is generic or contained inside a generic type.
            Class C1(Of T)
                  ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' BC31529ERR_DllImportOnInstanceMethod
        ' BC31530ERR_DllImportOnInterfaceMethod
        ' BC31531ERR_DllImportNotLegalOnEventMethod
        ' see AttributeTests

        <Fact, WorkItem(1116455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1116455")>
        Public Sub BC31534ERR_FriendAssemblyBadArguments()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="FriendAssemblyBadArguments">
        <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("Test, Version=*")>                    ' ok
<Assembly: InternalsVisibleTo("Test, PublicKeyToken=*")>             ' ok
<Assembly: InternalsVisibleTo("Test, Culture=*")>                    ' ok
<Assembly: InternalsVisibleTo("Test, Retargetable=*")>               ' ok
<Assembly: InternalsVisibleTo("Test, ContentType=*")>                ' ok
<Assembly: InternalsVisibleTo("Test, Version=.")>                    ' ok
<Assembly: InternalsVisibleTo("Test, Version=..")>                   ' ok
<Assembly: InternalsVisibleTo("Test, Version=...")>                  ' ok
                                                                     
<Assembly: InternalsVisibleTo("Test, Version=1")>                    ' error
<Assembly: InternalsVisibleTo("Test, Version=1.*")>                  ' error
<Assembly: InternalsVisibleTo("Test, Version=1.1.*")>                ' error
<Assembly: InternalsVisibleTo("Test, Version=1.1.1.*")>              ' error
<Assembly: InternalsVisibleTo("Test, ProcessorArchitecture=MSIL")>   ' error
<Assembly: InternalsVisibleTo("Test, CuLTure=EN")>                   ' error
<Assembly: InternalsVisibleTo("Test, PublicKeyToken=null")>          ' ok
        ]]></file>
    </compilation>, {SystemCoreRef})

            ' Tested against Dev12
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_FriendAssemblyBadArguments, "Assembly: InternalsVisibleTo(""Test, Version=1"")").WithArguments("Test, Version=1").WithLocation(11, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyBadArguments, "Assembly: InternalsVisibleTo(""Test, Version=1.*"")").WithArguments("Test, Version=1.*").WithLocation(12, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyBadArguments, "Assembly: InternalsVisibleTo(""Test, Version=1.1.*"")").WithArguments("Test, Version=1.1.*").WithLocation(13, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyBadArguments, "Assembly: InternalsVisibleTo(""Test, Version=1.1.1.*"")").WithArguments("Test, Version=1.1.1.*").WithLocation(14, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyBadArguments, "Assembly: InternalsVisibleTo(""Test, ProcessorArchitecture=MSIL"")").WithArguments("Test, ProcessorArchitecture=MSIL").WithLocation(15, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyBadArguments, "Assembly: InternalsVisibleTo(""Test, CuLTure=EN"")").WithArguments("Test, CuLTure=EN").WithLocation(16, 2))

        End Sub

        <Fact>
        Public Sub BC31537ERR_FriendAssemblyNameInvalid()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="FriendAssemblyNameInvalid">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("'   '")>        ' ok
<Assembly: InternalsVisibleTo("\t\r\n;a")>     ' ok (whitespace escape)
<Assembly: InternalsVisibleTo("\u1234;a")>     ' ok (assembly name Unicode escape)
<Assembly: InternalsVisibleTo("' a '")>        ' ok
<Assembly: InternalsVisibleTo("\u1000000;a")>  ' invalid escape
<Assembly: InternalsVisibleTo("a'b'c")>        ' quotes in the middle
<Assembly: InternalsVisibleTo("Test, PublicKey=Null")>                      
<Assembly: InternalsVisibleTo("Test, Bar")>                                 
<Assembly: InternalsVisibleTo("Test, Version")>                           
]]></file>
</compilation>, {SystemCoreRef})

            ' Tested against Dev12
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_FriendAssemblyNameInvalid, "Assembly: InternalsVisibleTo(""\u1000000;a"")").WithArguments("\u1000000;a").WithLocation(6, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyNameInvalid, "Assembly: InternalsVisibleTo(""a'b'c"")").WithArguments("a'b'c").WithLocation(7, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyNameInvalid, "Assembly: InternalsVisibleTo(""Test, PublicKey=Null"")").WithArguments("Test, PublicKey=Null").WithLocation(8, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyNameInvalid, "Assembly: InternalsVisibleTo(""Test, Bar"")").WithArguments("Test, Bar").WithLocation(9, 2),
                Diagnostic(ERRID.ERR_FriendAssemblyNameInvalid, "Assembly: InternalsVisibleTo(""Test, Version"")").WithArguments("Test, Version").WithLocation(10, 2))
        End Sub

        <Fact()>
        Public Sub BC31549ERR_PIAHasNoAssemblyGuid1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="A">
        <file name="a.vb"><![CDATA[
            Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")> 
]]></file>
    </compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
                <compilation>
                    <file name="a.vb"/>
                </compilation>,
                references:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            compilation2.AssertTheseDeclarationDiagnostics(<errors><![CDATA[
BC31549: Cannot embed interop types from assembly 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing the 'System.Runtime.InteropServices.GuidAttribute' attribute.
                 ]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31553ERR_PIAHasNoTypeLibAttribute1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="A">
        <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.InteropServices.Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")> 
]]></file>
    </compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
                <compilation>
                    <file name="a.vb"/>
                </compilation>,
                references:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            compilation2.AssertTheseDeclarationDiagnostics(<errors><![CDATA[
BC31553: Cannot embed interop types from assembly 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing either the 'System.Runtime.InteropServices.ImportedFromTypeLibAttribute' attribute or the 'System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute' attribute.
                 ]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31553ERR_PIAHasNoTypeLibAttribute1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="A">
        <file name="a.vb"/>
    </compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
                <compilation>
                    <file name="a.vb"/>
                </compilation>,
                references:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            compilation2.AssertTheseDeclarationDiagnostics(<errors><![CDATA[
BC31549: Cannot embed interop types from assembly 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing the 'System.Runtime.InteropServices.GuidAttribute' attribute.
BC31553: Cannot embed interop types from assembly 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing either the 'System.Runtime.InteropServices.ImportedFromTypeLibAttribute' attribute or the 'System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute' attribute.
                 ]]></errors>)
        End Sub

        <Fact>
        Public Sub BC32040ERR_BadFlagsOnNewOverloads()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadFlagsOnNewOverloads">
        <file name="a.vb"><![CDATA[
           class C1
            End class
            Friend Class C2
                Inherits C1
                Public Overloads Sub New(ByVal x As Integer)
                    MyBase.New(CType (x, Short))
                End Sub
                Public Sub New(ByVal x As Date)
                    MyBase.New(1)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32040: The 'Overloads' keyword is used to overload inherited members; do not use the 'Overloads' keyword when overloading 'Sub New'.
                Public Overloads Sub New(ByVal x As Integer)
                       ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32041ERR_TypeCharOnGenericParam()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeCharOnGenericParam">
        <file name="a.vb"><![CDATA[
Structure S(Of T@)
    Sub M(Of U@)()
    End Sub
End Structure
Interface I(Of T#)
    Sub M(Of U#)()
End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32041: Type character cannot be used in a type parameter declaration.
Structure S(Of T@)
               ~~
BC32041: Type character cannot be used in a type parameter declaration.
    Sub M(Of U@)()
             ~~
BC32041: Type character cannot be used in a type parameter declaration.
Interface I(Of T#)
               ~~
BC32041: Type character cannot be used in a type parameter declaration.
    Sub M(Of U#)()
             ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32042ERR_TooFewGenericArguments1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TooFewGenericArguments1">
        <file name="a.vb"><![CDATA[
           Structure S1(Of t)
            End Structure
            Class c1
                Dim x3 As S1
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32042: Too few type arguments to 'S1(Of t)'.
                Dim x3 As S1
                          ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32043ERR_TooManyGenericArguments1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TooManyGenericArguments1">
        <file name="a.vb"><![CDATA[
           Structure S1(Of arg1)
            End Structure
            Class c1
                Dim scen2 As New S1(Of String, Integer, Double, Decimal)
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32043: Too many type arguments to 'S1(Of arg1)'.
                Dim scen2 As New S1(Of String, Integer, Double, Decimal)
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32044ERR_GenericConstraintNotSatisfied2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C(Of T1, T2)
    Sub M(Of U As {T1, T2})()
        M(Of T1)()
        M(Of T2)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32044: Type argument 'T1' does not inherit from or implement the constraint type 'T2'.
        M(Of T1)()
        ~~~~~~~~
BC32044: Type argument 'T2' does not inherit from or implement the constraint type 'T1'.
        M(Of T2)()
        ~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32044ERR_GenericConstraintNotSatisfied2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
            Option Strict On
            Public Module M
                Sub Main()
                    Goo(Function(x As String) x, Function(x As Object) x)
                End Sub
                Sub Goo(Of T, S As T)(ByVal x As T, ByVal y As S)
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32044: Type argument 'Function <generated method>(x As Object) As Object' does not inherit from or implement the constraint type 'Function <generated method>(x As String) As String'.
                    Goo(Function(x As String) x, Function(x As Object) x)
                    ~~~
]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32047ERR_MultipleClassConstraints1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MultipleClassConstraints1">
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
    Inherits A
End Class
Class C
    Inherits B
End Class
Interface IA(Of T, U As {A, T, B})
End Interface
Interface IB
    Sub M(Of T As {A, B, C, New})()
End Interface
MustInherit Class D(Of T, U)
    MustOverride Sub M(Of V As {T, U, C, New})()
End Class
MustInherit Class E
    Inherits D(Of A, B)
    Public Overrides Sub M(Of V As {A, B, C, New})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32047: Type parameter 'U' can only have one constraint that is a class.
Interface IA(Of T, U As {A, T, B})
                               ~
BC32047: Type parameter 'T' can only have one constraint that is a class.
    Sub M(Of T As {A, B, C, New})()
                      ~
BC32047: Type parameter 'T' can only have one constraint that is a class.
    Sub M(Of T As {A, B, C, New})()
                         ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32048ERR_ConstNotClassInterfaceOrTypeParam1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ConstNotClassInterfaceOrTypeParam1">
        <file name="a.vb"><![CDATA[
Class A
End Class
Delegate Sub D()
Structure S
End Structure
Enum E
    A
End Enum
Class C1(Of T1 As A(), T2 As D, T3 As S, T4 As E, T5 As Unknown)
End Class
MustInherit Class C2(Of T1, T2, T3, T4, T5)
    MustOverride Sub M(Of U1 As T1, U2 As T2, U3 As T3, U4 As T4, U5 As T5)()
End Class
MustInherit Class C3
    Inherits C2(Of A(), D, S, E, Unknown)
    MustOverride Overrides Sub M(Of U1 As A(), U2 As D, U3 As S, U4 As E, U5 As Unknown)()
End Class
Class C3(Of T As {S(), E()})
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32048: Type constraint 'A()' must be either a class, interface or type parameter.
Class C1(Of T1 As A(), T2 As D, T3 As S, T4 As E, T5 As Unknown)
                  ~~~
BC32048: Type constraint 'D' must be either a class, interface or type parameter.
Class C1(Of T1 As A(), T2 As D, T3 As S, T4 As E, T5 As Unknown)
                             ~
BC32048: Type constraint 'S' must be either a class, interface or type parameter.
Class C1(Of T1 As A(), T2 As D, T3 As S, T4 As E, T5 As Unknown)
                                      ~
BC32048: Type constraint 'E' must be either a class, interface or type parameter.
Class C1(Of T1 As A(), T2 As D, T3 As S, T4 As E, T5 As Unknown)
                                               ~
BC30002: Type 'Unknown' is not defined.
Class C1(Of T1 As A(), T2 As D, T3 As S, T4 As E, T5 As Unknown)
                                                        ~~~~~~~
BC30002: Type 'Unknown' is not defined.
    Inherits C2(Of A(), D, S, E, Unknown)
                                 ~~~~~~~
BC30002: Type 'Unknown' is not defined.
    MustOverride Overrides Sub M(Of U1 As A(), U2 As D, U3 As S, U4 As E, U5 As Unknown)()
                                                                                ~~~~~~~
BC32048: Type constraint 'S()' must be either a class, interface or type parameter.
Class C3(Of T As {S(), E()})
                  ~~~
BC32048: Type constraint 'E()' must be either a class, interface or type parameter.
Class C3(Of T As {S(), E()})
                       ~~~
BC32119: Constraint 'E()' conflicts with the constraint 'S()' already specified for type parameter 'T'.
Class C3(Of T As {S(), E()})
                       ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Checks for duplicate type parameters
        <Fact>
        Public Sub BC32049ERR_DuplicateTypeParamName1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            class c(of tT, TT, Tt)

            end class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32049: Type parameter already declared with name 'TT'.
class c(of tT, TT, Tt)
               ~~
BC32049: Type parameter already declared with name 'Tt'.
class c(of tT, TT, Tt)
                   ~~                                      
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact>
        Public Sub BC32054ERR_ShadowingGenericParamWithMember1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ShadowingGenericParamWithMember1">
        <file name="a.vb"><![CDATA[
            Partial Structure S1(Of membername)
                Function membername() As String
                End Function
            End Structure
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32054: 'membername' has the same name as a type parameter.
                Function membername() As String
                         ~~~~~~~~~~                                      
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact>
        Public Sub BC32055ERR_GenericParamBase2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="GenericParamBase2">
        <file name="a.vb"><![CDATA[
            Class C1(Of T)
                Class cls3
                    Inherits T
                End Class
            End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32055: Class 'cls3' cannot inherit from a type parameter.
                    Inherits T
                             ~                                      
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact>
        Public Sub BC32055ERR_GenericParamBase2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="GenericParamBase2">
        <file name="a.vb"><![CDATA[
            Interface I1(Of T)
                Class c1
                    Inherits T
                End Class
            End Interface
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32055: Class 'c1' cannot inherit from a type parameter.
                    Inherits T
                             ~                                      
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact>
        Public Sub BC32056ERR_ImplementsGenericParam()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeParamQualifierDisallowed">
        <file name="a.vb"><![CDATA[
            Interface I1(Of T)
                Class c1
                    Implements T
                End Class
            End Interface
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32056: Type parameter not allowed in 'Implements' clause.
                    Implements T
                               ~                                      
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact()>
        Public Sub BC32060ERR_ClassConstraintNotInheritable1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
NotInheritable Class A
End Class
Class B
End Class
Interface IA(Of T As {A, B})
End Interface
Interface IB(Of T)
    Sub M(Of U As T)()
End Interface
Class C
    Implements IB(Of A)
    Private Sub M(Of U As A)() Implements IB(Of A).M
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32060: Type constraint cannot be a 'NotInheritable' class.
Interface IA(Of T As {A, B})
                      ~
BC32047: Type parameter 'T' can only have one constraint that is a class.
Interface IA(Of T As {A, B})
                         ~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32061ERR_ConstraintIsRestrictedType1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Interface I(Of T As Object, U As System.ValueType)
End Interface
Structure S(Of T As System.Enum, U As System.Array)
End Structure
Delegate Sub D(Of T As System.Delegate, U As System.MulticastDelegate)()
Class C(Of T As {Object, A}, U As {A, Object})
End Class
Class A(Of T1, T2, T3, T4, T5, T6)
End Class
Class B
    Inherits A(Of Object, System.ValueType, System.Enum, System.Array, System.Delegate, System.MulticastDelegate)
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32061: 'Object' cannot be used as a type constraint.
Interface I(Of T As Object, U As System.ValueType)
                    ~~~~~~
BC32061: 'ValueType' cannot be used as a type constraint.
Interface I(Of T As Object, U As System.ValueType)
                                 ~~~~~~~~~~~~~~~~
BC32061: '[Enum]' cannot be used as a type constraint.
Structure S(Of T As System.Enum, U As System.Array)
                    ~~~~~~~~~~~
BC32061: 'Array' cannot be used as a type constraint.
Structure S(Of T As System.Enum, U As System.Array)
                                      ~~~~~~~~~~~~
BC32061: '[Delegate]' cannot be used as a type constraint.
Delegate Sub D(Of T As System.Delegate, U As System.MulticastDelegate)()
                       ~~~~~~~~~~~~~~~
BC32061: 'MulticastDelegate' cannot be used as a type constraint.
Delegate Sub D(Of T As System.Delegate, U As System.MulticastDelegate)()
                                             ~~~~~~~~~~~~~~~~~~~~~~~~
BC32061: 'Object' cannot be used as a type constraint.
Class C(Of T As {Object, A}, U As {A, Object})
                 ~~~~~~
BC32047: Type parameter 'T' can only have one constraint that is a class.
Class C(Of T As {Object, A}, U As {A, Object})
                         ~
BC32047: Type parameter 'U' can only have one constraint that is a class.
Class C(Of T As {Object, A}, U As {A, Object})
                                      ~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540653")>
        <Fact>
        Public Sub BC32067ERR_AttrCannotBeGenerics()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AttrCannotBeGenerics">
        <file name="a.vb"><![CDATA[
Imports System

Class Test(Of attributeusageattribute)
    'COMPILEERROR: BC32067,"attributeusageattribute"
    <attributeusageattribute(AttributeTargets.All)> Class c1

    End Class

    Class myattr
        'COMPILEERROR: BC32074,"Attribute"
        Inherits Attribute
    End Class

    'COMPILEERROR: BC32067,"myattr"
    <myattr()> Class test3

    End Class
End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_AttrCannotBeGenerics, "attributeusageattribute").WithArguments("attributeusageattribute"),
                                      Diagnostic(ERRID.ERR_AttrCannotBeGenerics, "myattr").WithArguments("Test(Of attributeusageattribute).myattr"),
                                      Diagnostic(ERRID.ERR_GenericClassCannotInheritAttr, "myattr"))

        End Sub

        <Fact()>
        Public Sub BC32068ERR_BadStaticLocalInGenericMethod()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadStaticLocalInGenericMethod">
        <file name="a.vb"><![CDATA[
            Module M
                Sub Goo(Of T)()
                    Static x = 1
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32068: Local variables within generic methods cannot be declared 'Static'.
                    Static x = 1
                    ~~~~~~                                
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32070ERR_SyntMemberShadowsGenericParam3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SyntMemberShadowsGenericParam3">
        <file name="a.vb"><![CDATA[
            Class C(Of _P, get_P, set_P, _q, get_R, set_R, _R)
                Property P
                Property Q
                Property R
                    Get
                        Return Nothing
                    End Get
                    Set(value)
                    End Set
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32070: property 'P' implicitly defines a member '_P' which has the same name as a type parameter.
                Property P
                         ~
BC32070: property 'Q' implicitly defines a member '_Q' which has the same name as a type parameter.
                Property Q
                         ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32071ERR_ConstraintAlreadyExists1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Class A
End Class
Delegate Sub D(Of T As I, U As {New, T, I, I, A, T, A, I})()
MustInherit Class B(Of T, U)
    MustOverride Sub M1(Of V As {T, U, I})()
End Class
MustInherit Class C
    Inherits B(Of I, A)
    MustOverride Overrides Sub M1(Of V As {I, A, I})()
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32071: Constraint type 'I' already specified for this type parameter.
Delegate Sub D(Of T As I, U As {New, T, I, I, A, T, A, I})()
                                           ~
BC32071: Constraint type 'T' already specified for this type parameter.
Delegate Sub D(Of T As I, U As {New, T, I, I, A, T, A, I})()
                                                 ~
BC32047: Type parameter 'U' can only have one constraint that is a class.
Delegate Sub D(Of T As I, U As {New, T, I, I, A, T, A, I})()
                                                    ~
BC32071: Constraint type 'A' already specified for this type parameter.
Delegate Sub D(Of T As I, U As {New, T, I, I, A, T, A, I})()
                                                    ~
BC32071: Constraint type 'I' already specified for this type parameter.
Delegate Sub D(Of T As I, U As {New, T, I, I, A, T, A, I})()
                                                       ~
BC32071: Constraint type 'I' already specified for this type parameter.
    MustOverride Overrides Sub M1(Of V As {I, A, I})()
                                                 ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Duplicate undefined constraint types.
        <Fact()>
        Public Sub BC32071ERR_ConstraintAlreadyExists1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C(Of T As {A, B, A})
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30002: Type 'A' is not defined.
Class C(Of T As {A, B, A})
                 ~
BC30002: Type 'B' is not defined.
Class C(Of T As {A, B, A})
                    ~
BC30002: Type 'A' is not defined.
Class C(Of T As {A, B, A})
                       ~
BC32071: Constraint type 'A' already specified for this type parameter.
Class C(Of T As {A, B, A})
                       ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        Public Sub BC32072ERR_InterfacePossiblyImplTwice2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfacePossiblyImplTwice2">
        <file name="a.vb"><![CDATA[
            Class C1(Of T As IAsyncResult, u As IComparable)
                Implements intf1(Of T)
                Implements intf1(Of u)
            End Class
            Interface intf1(Of T)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32072: Cannot implement interface 'intf1(Of u)' because its implementation could conflict with the implementation of another implemented interface 'intf1(Of T)' for some type arguments.
                Implements intf1(Of u)
                           ~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540652, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540652")>
        <Fact()>
        Public Sub BC32074ERR_GenericClassCannotInheritAttr()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Imports System.Reflection

Friend Module RegressVSW108431mod

    <AttributeUsage(AttributeTargets.All)>
    Class Attr1(Of A)
        'COMPILEERROR: BC32074, "Attribute"
        Inherits Attribute
    End Class

    Class G(Of T)
        Class Attr2
            'COMPILEERROR: BC32074,"Attribute"
            Inherits Attribute
        End Class
    End Class
End Module
        ]]></file>
        </compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32074: Classes that are generic or contained in a generic type cannot inherit from an attribute class.
    Class Attr1(Of A)
          ~~~~~
BC32074: Classes that are generic or contained in a generic type cannot inherit from an attribute class.
        Class Attr2
              ~~~~~                                              
                                          ]]></errors>)
        End Sub

        <WorkItem(543672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543672")>
        <Fact()>
        Public Sub BC32074ERR_GenericClassCannotInheritAttr_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation>
            <file name="a.vb"><![CDATA[
Class A
    Inherits System.Attribute
End Class
Class B(Of T)
    Inherits A
End Class
Class C(Of T)
    Class B
        Inherits A
    End Class
End Class
        ]]></file>
        </compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32074: Classes that are generic or contained in a generic type cannot inherit from an attribute class.
Class B(Of T)
      ~
BC32074: Classes that are generic or contained in a generic type cannot inherit from an attribute class.
    Class B
          ~
     ]]></errors>)
        End Sub

        <Fact>
        Public Sub BC32075ERR_DeclaresCantBeInGeneric()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="GenericClassCannotInheritAttr">
        <file name="a.vb"><![CDATA[
            Public Class C1(Of t)
                Declare Sub goo Lib "a.dll" ()
            End Class
        ]]></file>
    </compilation>)
            compilation1.VerifyDiagnostics(Diagnostic(ERRID.ERR_DeclaresCantBeInGeneric, "goo"))
        End Sub

        <Fact()>
        Public Sub BC32077ERR_OverrideWithConstraintMismatch2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IA(Of T)
End Interface
Interface IB
End Interface
MustInherit Class A
    ' Different constraints.
    Friend MustOverride Sub A1(Of T As Structure)()
    Friend MustOverride Sub A2(Of T As Structure, U As IA(Of T))()
    Friend MustOverride Sub A3(Of T As IA(Of T))()
    Friend MustOverride Sub A4(Of T As {Structure, IA(Of T)}, U)()
    ' Additional constraints.
    Friend MustOverride Sub B1(Of T)()
    Friend MustOverride Sub B2(Of T As New)()
    Friend MustOverride Sub B3(Of T As IA(Of T), U)()
    ' Missing constraints.
    Friend MustOverride Sub C1(Of T As Class)()
    Friend MustOverride Sub C2(Of T As {Class, New})()
    Friend MustOverride Sub C3(Of T, U As {IB, IA(Of T)})()
    ' Same constraints, different order.
    Friend MustOverride Sub D1(Of T As {IA(Of T), IB})()
    Friend MustOverride Sub D2(Of T, U, V As {T, U})()
    ' Different constraint clauses.
    Friend MustOverride Sub E1(Of T, U As T)()
    ' Different type parameter names.
    Friend MustOverride Sub F1(Of T As Class, U As T)()
    Friend MustOverride Sub F2(Of T As Class, U As T)()
End Class
MustInherit Class B
    Inherits A
    ' Different constraints.
    Friend MustOverride Overrides Sub A1(Of T As Class)()
    Friend MustOverride Overrides Sub A2(Of T As Structure, U As IB)()
    Friend MustOverride Overrides Sub A3(Of T As IA(Of IA(Of T)))()
    Friend MustOverride Overrides Sub A4(Of T As {Structure, IA(Of U)}, U)()
    ' Additional constraints.
    Friend MustOverride Overrides Sub B1(Of T As New)()
    Friend MustOverride Overrides Sub B2(Of T As {Class, New})()
    Friend MustOverride Overrides Sub B3(Of T As {IB, IA(Of T)}, U)()
    ' Missing constraints.
    Friend MustOverride Overrides Sub C1(Of T)()
    Friend MustOverride Overrides Sub C2(Of T As Class)()
    Friend MustOverride Overrides Sub C3(Of T, U As IA(Of T))()
    ' Same constraints, different order.
    Friend MustOverride Overrides Sub D1(Of T As {IB, IA(Of T)})()
    Friend MustOverride Overrides Sub D2(Of T, U, V As {U, T})()
    ' Different constraint clauses.
    Friend MustOverride Overrides Sub E1(Of T As Class, U)()
    ' Different type parameter names.
    Friend MustOverride Overrides Sub F1(Of U As Class, T As U)()
    Friend MustOverride Overrides Sub F2(Of T1 As Class, T2 As T1)()
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32077: 'Friend MustOverride Overrides Sub A1(Of T As Class)()' cannot override 'Friend MustOverride Sub A1(Of T As Structure)()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub A1(Of T As Class)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub A2(Of T As Structure, U As IB)()' cannot override 'Friend MustOverride Sub A2(Of T As Structure, U As IA(Of T))()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub A2(Of T As Structure, U As IB)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub A3(Of T As IA(Of IA(Of T)))()' cannot override 'Friend MustOverride Sub A3(Of T As IA(Of T))()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub A3(Of T As IA(Of IA(Of T)))()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub A4(Of T As {Structure, IA(Of U)}, U)()' cannot override 'Friend MustOverride Sub A4(Of T As {Structure, IA(Of T)}, U)()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub A4(Of T As {Structure, IA(Of U)}, U)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub B1(Of T As New)()' cannot override 'Friend MustOverride Sub B1(Of T)()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub B1(Of T As New)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub B2(Of T As {Class, New})()' cannot override 'Friend MustOverride Sub B2(Of T As New)()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub B2(Of T As {Class, New})()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub B3(Of T As {IB, IA(Of T)}, U)()' cannot override 'Friend MustOverride Sub B3(Of T As IA(Of T), U)()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub B3(Of T As {IB, IA(Of T)}, U)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub C1(Of T)()' cannot override 'Friend MustOverride Sub C1(Of T As Class)()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub C1(Of T)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub C2(Of T As Class)()' cannot override 'Friend MustOverride Sub C2(Of T As {Class, New})()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub C2(Of T As Class)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub C3(Of T, U As IA(Of T))()' cannot override 'Friend MustOverride Sub C3(Of T, U As {IB, IA(Of T)})()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub C3(Of T, U As IA(Of T))()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub E1(Of T As Class, U)()' cannot override 'Friend MustOverride Sub E1(Of T, U As T)()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub E1(Of T As Class, U)()
                                      ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32077ERR_OverrideWithConstraintMismatch2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I
End Interface
Class A
    Implements I
End Class
Structure S
End Structure
MustInherit Class A0(Of T)
    Friend MustOverride Sub AM(Of U As {T, Structure})()
End Class
MustInherit Class A1
    Inherits A0(Of S)
    Friend MustOverride Overrides Sub AM(Of U As {Structure, S})()
End Class
MustInherit Class A2
    Inherits A0(Of S)
    Friend MustOverride Overrides Sub AM(Of U As S)()
End Class
MustInherit Class B0(Of T)
    Friend MustOverride Sub BX(Of U As {T, Class})()
End Class
MustInherit Class B1
    Inherits B0(Of A)
    Friend MustOverride Overrides Sub BX(Of U As {Class, A})()
End Class
MustInherit Class B2
    Inherits B0(Of A)
    Friend MustOverride Overrides Sub BX(Of U As A)()
End Class
MustInherit Class C0(Of T, U)
    Friend MustOverride Sub CM(Of V As {T, U})()
End Class
MustInherit Class C1(Of T)
    Inherits C0(Of T, T)
    Friend MustOverride Overrides Sub CM(Of V As T)()
End Class
MustInherit Class C2(Of T)
    Inherits C0(Of T, T)
    Friend MustOverride Overrides Sub CM(Of V As {T, T})()
End Class
MustInherit Class D0(Of T, U)
    Friend MustOverride Sub DM(Of V As {T, U, A, I})()
End Class
MustInherit Class D1
    Inherits D0(Of I, A)
    Friend MustOverride Overrides Sub DM(Of V As A)()
End Class
MustInherit Class D2
    Inherits D0(Of I, A)
    Friend MustOverride Overrides Sub DM(Of V As {A, I})()
End Class
MustInherit Class D3
    Inherits D0(Of A, I)
    Friend MustOverride Overrides Sub DM(Of V As {A, I, A, I})()
End Class
MustInherit Class E0(Of T, U)
    Friend MustOverride Sub EM(Of V As {T, U, Structure})()
End Class
MustInherit Class E1
    Inherits E0(Of Object, ValueType)
    Friend MustOverride Overrides Sub EM(Of U As Structure)()
End Class
MustInherit Class E2
    Inherits E0(Of Object, ValueType)
    Friend MustOverride Overrides Sub EM(Of U As {Structure, Object, ValueType})()
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32077: 'Friend MustOverride Overrides Sub AM(Of U As S)()' cannot override 'Friend MustOverride Sub AM(Of U As {Structure, S})()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub AM(Of U As S)()
                                      ~~
BC32077: 'Friend MustOverride Overrides Sub BX(Of U As A)()' cannot override 'Friend MustOverride Sub BX(Of U As {Class, A})()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub BX(Of U As A)()
                                      ~~
BC32071: Constraint type 'T' already specified for this type parameter.
    Friend MustOverride Overrides Sub CM(Of V As {T, T})()
                                                     ~
BC32077: 'Friend MustOverride Overrides Sub DM(Of V As A)()' cannot override 'Friend MustOverride Sub DM(Of V As {I, A})()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub DM(Of V As A)()
                                      ~~
BC32071: Constraint type 'A' already specified for this type parameter.
    Friend MustOverride Overrides Sub DM(Of V As {A, I, A, I})()
                                                        ~
BC32071: Constraint type 'I' already specified for this type parameter.
    Friend MustOverride Overrides Sub DM(Of V As {A, I, A, I})()
                                                           ~
BC32077: 'Friend MustOverride Overrides Sub EM(Of U As Structure)()' cannot override 'Friend MustOverride Sub EM(Of V As {Structure, Object, ValueType})()' because they differ by type parameter constraints.
    Friend MustOverride Overrides Sub EM(Of U As Structure)()
                                      ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32078ERR_ImplementsWithConstraintMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I
End Interface
Class A
    Implements I
End Class
Structure S
End Structure
Interface IA(Of T)
    Sub AM(Of U As {T, Structure})()
End Interface
Class A1
    Implements IA(Of S)
    Private Sub AM(Of U As {Structure, S})() Implements IA(Of S).AM
    End Sub
End Class
Class A2
    Implements IA(Of S)
    Private Sub AM(Of U As S)() Implements IA(Of S).AM
    End Sub
End Class
Interface IB(Of T)
    Sub BX(Of U As {T, Class})()
End Interface
Class B1
    Implements IB(Of A)
    Private Sub BX(Of U As {Class, A})() Implements IB(Of A).BX
    End Sub
End Class
Class B2
    Implements IB(Of A)
    Private Sub BX(Of U As A)() Implements IB(Of A).BX
    End Sub
End Class
Interface IC(Of T, U)
    Sub CM(Of V As {T, U})()
End Interface
Class C1(Of T)
    Implements IC(Of T, T)
    Private Sub CM(Of V As T)() Implements IC(Of T, T).CM
    End Sub
End Class
Class C2(Of T)
    Implements IC(Of T, T)
    Private Sub CM(Of V As {T, T})() Implements IC(Of T, T).CM
    End Sub
End Class
Interface ID(Of T, U)
    Sub DM(Of V As {T, U, A, I})()
End Interface
Class D1
    Implements ID(Of I, A)
    Private Sub DM(Of V As A)() Implements ID(Of I, A).DM
    End Sub
End Class
Class D2
    Implements ID(Of I, A)
    Private Sub DM(Of V As {A, I})() Implements ID(Of I, A).DM
    End Sub
End Class
Class D3
    Implements ID(Of A, I)
    Private Sub DM(Of V As {A, I, A, I})() Implements ID(Of A, I).DM
    End Sub
End Class
Interface IE(Of T, U)
    Sub EM(Of V As {T, U, Structure})()
End Interface
Class E1
    Implements IE(Of Object, ValueType)
    Private Sub EM(Of U As Structure)() Implements IE(Of Object, ValueType).EM
    End Sub
End Class
Class E2
    Implements IE(Of Object, ValueType)
    Private Sub EM(Of U As {Structure, Object, ValueType})() Implements IE(Of Object, ValueType).EM
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32078: 'Private Sub AM(Of U As S)()' cannot implement 'IA(Of S).Sub AM(Of U As {Structure, S})()' because they differ by type parameter constraints.
    Private Sub AM(Of U As S)() Implements IA(Of S).AM
                                           ~~~~~~~~~~~
BC32078: 'Private Sub BX(Of U As A)()' cannot implement 'IB(Of A).Sub BX(Of U As {Class, A})()' because they differ by type parameter constraints.
    Private Sub BX(Of U As A)() Implements IB(Of A).BX
                                           ~~~~~~~~~~~
BC32071: Constraint type 'T' already specified for this type parameter.
    Private Sub CM(Of V As {T, T})() Implements IC(Of T, T).CM
                               ~
BC32078: 'Private Sub DM(Of V As A)()' cannot implement 'ID(Of I, A).Sub DM(Of V As {I, A})()' because they differ by type parameter constraints.
    Private Sub DM(Of V As A)() Implements ID(Of I, A).DM
                                           ~~~~~~~~~~~~~~
BC32071: Constraint type 'A' already specified for this type parameter.
    Private Sub DM(Of V As {A, I, A, I})() Implements ID(Of A, I).DM
                                  ~
BC32071: Constraint type 'I' already specified for this type parameter.
    Private Sub DM(Of V As {A, I, A, I})() Implements ID(Of A, I).DM
                                     ~
BC32078: 'Private Sub EM(Of U As Structure)()' cannot implement 'IE(Of Object, ValueType).Sub EM(Of V As {Structure, Object, ValueType})()' because they differ by type parameter constraints.
    Private Sub EM(Of U As Structure)() Implements IE(Of Object, ValueType).EM
                                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Single method implementing multiple interface methods.
        <Fact()>
        Public Sub BC32078ERR_ImplementsWithConstraintMismatch3_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IA(Of T)
    Sub AM(Of U As {T, Structure})()
End Interface
Interface IB(Of T)
    Sub BX(Of U As {T, Class})()
End Interface
Class C(Of T)
    Implements IA(Of T), IB(Of T)
    Public Sub M(Of U As T)() Implements IA(Of T).AM, IB(Of T).BX
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32078: 'Public Sub M(Of U As T)()' cannot implement 'IA(Of T).Sub AM(Of U As {Structure, T})()' because they differ by type parameter constraints.
    Public Sub M(Of U As T)() Implements IA(Of T).AM, IB(Of T).BX
                                         ~~~~~~~~~~~
BC32078: 'Public Sub M(Of U As T)()' cannot implement 'IB(Of T).Sub BX(Of U As {Class, T})()' because they differ by type parameter constraints.
    Public Sub M(Of U As T)() Implements IA(Of T).AM, IB(Of T).BX
                                                      ~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32078ERR_ImplementsWithConstraintMismatch3_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface

Interface II
    Sub m(Of T As I)()
End Interface

Class CLS
    Implements II

    Partial Private Sub X()
    End Sub

    Private Sub X(Of T)() Implements II.m
    End Sub
End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC32078: 'Private Sub X(Of T)()' cannot implement 'II.Sub m(Of T As I)()' because they differ by type parameter constraints.
    Private Sub X(Of T)() Implements II.m
                                     ~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC32078ERR_ImplementsWithConstraintMismatch3_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface

Interface II
    Sub m(Of T As I)()
End Interface

Class CLS
    Implements II

    Partial Private Sub X(Of T)()
    End Sub

    Private Sub X(Of T)() Implements II.m
    End Sub
End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC32078: 'Private Sub X(Of T)()' cannot implement 'II.Sub m(Of T As I)()' because they differ by type parameter constraints.
    Private Sub X(Of T)() Implements II.m
                                     ~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC32080ERR_HandlesInvalidOnGenericMethod()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="HandlesInvalidOnGenericMethod">
        <file name="a.vb"><![CDATA[
            Class A 
                Event X()
                Sub Goo(Of T)() Handles Me.X
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32080: Generic methods cannot use 'Handles' clause.
                Sub Goo(Of T)() Handles Me.X
                    ~~~
BC31029: Method 'Goo' cannot handle event 'X' because they do not have a compatible signature.
                Sub Goo(Of T)() Handles Me.X
                                           ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32081ERR_MultipleNewConstraints()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MultipleNewConstraints">
        <file name="a.vb"><![CDATA[
            Imports System
            Class C(Of T As {New, New})
                Sub M(Of U As {New, Class, New})()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32081: 'New' constraint cannot be specified multiple times for the same type parameter.
            Class C(Of T As {New, New})
                                  ~~~
BC32081: 'New' constraint cannot be specified multiple times for the same type parameter.
                Sub M(Of U As {New, Class, New})()
                                           ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32082ERR_MustInheritForNewConstraint2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustInheritForNewConstraint2">
        <file name="a.vb"><![CDATA[
            Class C1(Of T)
                Dim x As New C2(Of Base).C2Inner(Of Derived)
                Sub New()
                End Sub
            End Class
            Class Base
            End Class
            MustInherit Class Derived
                Inherits Base
                Public Sub New()
                End Sub
            End Class
            Class C2(Of T As New)
                Class C2Inner(Of S As {T, Derived, New})
                End Class
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32082: Type argument 'Derived' is declared 'MustInherit' and does not satisfy the 'New' constraint for type parameter 'S'.
                Dim x As New C2(Of Base).C2Inner(Of Derived)
                                                    ~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32083ERR_NoSuitableNewForNewConstraint2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoSuitableNewForNewConstraint2">
        <file name="a.vb"><![CDATA[
Class [Public]
    Public Sub New()
    End Sub
End Class
Class [Friend]
    Friend Sub New()
    End Sub
End Class
Class ProtectedFriend
    Protected Friend Sub New()
    End Sub
End Class
Class [Protected]
    Protected Sub New()
    End Sub
End Class
Class [Private]
    Private Sub New()
    End Sub
End Class
Interface [Interface]
End Interface
Structure [Structure]
End Structure
Class C
    Function F(Of T As New)() As T
        Return New T
    End Function
    Sub M()
        Dim o
        o = F(Of [Public])()
        o = F(Of [Friend])()
        o = F(Of [ProtectedFriend])()
        o = F(Of [Protected])()
        o = F(Of [Private])()
        o = F(Of [Interface])()
        o = F(Of [Structure])()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32083: Type argument '[Friend]' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = F(Of [Friend])()
            ~~~~~~~~~~~~~~
BC32083: Type argument 'ProtectedFriend' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = F(Of [ProtectedFriend])()
            ~~~~~~~~~~~~~~~~~~~~~~~
BC32083: Type argument '[Protected]' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = F(Of [Protected])()
            ~~~~~~~~~~~~~~~~~
BC32083: Type argument '[Private]' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = F(Of [Private])()
            ~~~~~~~~~~~~~~~
BC32083: Type argument '[Interface]' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = F(Of [Interface])()
            ~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Constructors with optional and params args
        ' should not be considered parameterless.
        <Fact()>
        Public Sub BC32083ERR_NoSuitableNewForNewConstraint2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoSuitableNewForNewConstraint2">
        <file name="a.vb"><![CDATA[
Class A
    Public Sub New(Optional o As Object = Nothing)
    End Sub
End Class
Class B
    Public Sub New(ParamArray o As Object())
    End Sub
End Class
Class C(Of T As New)
    Shared Sub M(Of U As New)()
        Dim o
        o = New A()
        o = New C(Of A)()
        M(Of A)()
        o = New B()
        o = New C(Of B)()
        M(Of B)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = New C(Of A)()
                     ~
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'U'.
        M(Of A)()
        ~~~~~~~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = New C(Of B)()
                     ~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'U'.
        M(Of B)()
        ~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32084ERR_BadGenericParamForNewConstraint2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Structure S
End Structure
Class A
    Shared Sub M(Of T As New)()
    End Sub
End Class
Class B(Of T)
    Overridable Sub M(Of U As T)()
    End Sub
    Shared Sub M(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)()
        A.M(Of T1)()
        A.M(Of T2)()
        A.M(Of T3)()
        A.M(Of T4)()
        A.M(Of T5)()
        A.M(Of T6)()
        A.M(Of T7)()
    End Sub
End Class
Class CI
    Inherits B(Of I)
    Public Overrides Sub M(Of UI As I)()
        A.M(Of UI)()
    End Sub
End Class
Class CS
    Inherits B(Of S)
    Public Overrides Sub M(Of US As S)()
        A.M(Of US)()
    End Sub
End Class
Class CA
    Inherits B(Of A)
    Public Overrides Sub M(Of UA As A)()
        A.M(Of UA)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32084: Type parameter 'T1' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter 'T'.
        A.M(Of T1)()
          ~~~~~~~~
BC32084: Type parameter 'T2' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter 'T'.
        A.M(Of T2)()
          ~~~~~~~~
BC32084: Type parameter 'T5' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter 'T'.
        A.M(Of T5)()
          ~~~~~~~~
BC32084: Type parameter 'T6' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter 'T'.
        A.M(Of T6)()
          ~~~~~~~~
BC32084: Type parameter 'T7' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter 'T'.
        A.M(Of T7)()
          ~~~~~~~~
BC32084: Type parameter 'UI' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter 'T'.
        A.M(Of UI)()
          ~~~~~~~~
BC32084: Type parameter 'UA' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter 'T'.
        A.M(Of UA)()
          ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32086ERR_DuplicateRawGenericTypeImport1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateRawGenericTypeImport1">
        <file name="a.vb"><![CDATA[
            Imports ns1.c1(Of String)
            Imports ns1.c1(Of Integer)
            Namespace ns1
                Public Class c1(Of t)
                End Class
            End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32086: Generic type 'c1(Of t)' cannot be imported more than once.
            Imports ns1.c1(Of Integer)
                    ~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32089ERR_NameSameAsMethodTypeParam1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NameSameAsMethodTypeParam1">
        <file name="a.vb"><![CDATA[
            Class c1
                Function fun(Of T1, T2) (ByVal t1 As T1, ByVal t2 As T2) As Integer
                    Return 5
                End Function
            End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32089: 't1' is already declared as a type parameter of this method.
                Function fun(Of T1, T2) (ByVal t1 As T1, ByVal t2 As T2) As Integer
                                               ~~
BC32089: 't2' is already declared as a type parameter of this method.
                Function fun(Of T1, T2) (ByVal t1 As T1, ByVal t2 As T2) As Integer
                                                               ~~                                      
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32089ERR_NameSameAsMethodTypeParam1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NameSameAsMethodTypeParam1">
        <file name="a.vb"><![CDATA[
            Class c1
                Sub sub1(Of T1, T2 As T1) (ByVal p1 As T1, ByVal t2 As T2)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32089: 't2' is already declared as a type parameter of this method.
                Sub sub1(Of T1, T2 As T1) (ByVal p1 As T1, ByVal t2 As T2)
                                                                 ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact, WorkItem(543642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543642")>
        Public Sub BC32090ERR_TypeParamNameFunctionNameCollision()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeParamNameFunctionNameCollision">
        <file name="a.vb"><![CDATA[
            Module M
                Function Goo(Of Goo)()
                    Return Nothing
                End Function

                ' Allowed for Sub -- should not generate error below.
                Sub Bar(Of Bar)() 
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32090: Type parameter cannot have the same name as its defining function.
                Function Goo(Of Goo)()
                                ~~~                                 
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32101ERR_MultipleReferenceConstraints()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C(Of T As {Class, Class})
    Sub M(Of U As {Class, New, Class})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32101: 'Class' constraint cannot be specified multiple times for the same type parameter.
Class C(Of T As {Class, Class})
                        ~~~~~
BC32101: 'Class' constraint cannot be specified multiple times for the same type parameter.
    Sub M(Of U As {Class, New, Class})()
                               ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32102ERR_MultipleValueConstraints()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Class C(Of T As {Structure, Structure})
    Sub M(Of U As {Structure, I, Structure})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32102: 'Structure' constraint cannot be specified multiple times for the same type parameter.
Class C(Of T As {Structure, Structure})
                            ~~~~~~~~~
BC32102: 'Structure' constraint cannot be specified multiple times for the same type parameter.
    Sub M(Of U As {Structure, I, Structure})()
                                 ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32103ERR_NewAndValueConstraintsCombined()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C(Of T As {Structure, New})
    Sub M(Of U As {New, Structure})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32103: 'New' constraint and 'Structure' constraint cannot be combined.
Class C(Of T As {Structure, New})
                            ~~~
BC32103: 'New' constraint and 'Structure' constraint cannot be combined.
    Sub M(Of U As {New, Structure})()
                        ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32104ERR_RefAndValueConstraintsCombined()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C(Of T As {Structure, Class})
    Sub M(Of U As {Class, Structure})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32104: 'Class' constraint and 'Structure' constraint cannot be combined.
Class C(Of T As {Structure, Class})
                            ~~~~~
BC32104: 'Class' constraint and 'Structure' constraint cannot be combined.
    Sub M(Of U As {Class, Structure})()
                          ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32105ERR_BadTypeArgForStructConstraint2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Class A
End Class
Class B(Of T As Structure)
End Class
Class C
    Shared Sub F(Of U As Structure)()
    End Sub
    Shared Sub M1(Of T1)()
        Dim o = New B(Of T1)()
        F(Of T1)()
    End Sub
    Shared Sub M2(Of T2 As Class)()
        Dim o = New B(Of T2)()
        F(Of T2)()
    End Sub
    Shared Sub M3(Of T3 As Structure)()
        Dim o = New B(Of T3)()
        F(Of T3)()
    End Sub
    Shared Sub M4(Of T4 As New)()
        Dim o = New B(Of T4)()
        F(Of T4)()
    End Sub
    Shared Sub M5(Of T5 As I)()
        Dim o = New B(Of T5)()
        F(Of T5)()
    End Sub
    Shared Sub M6(Of T6 As A)()
        Dim o = New B(Of T6)()
        F(Of T6)()
    End Sub
    Shared Sub M7(Of T7 As U, U)()
        Dim o = New B(Of T7)()
        F(Of T7)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32105: Type argument 'T1' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim o = New B(Of T1)()
                         ~~
BC32105: Type argument 'T1' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(Of T1)()
        ~~~~~~~~
BC32105: Type argument 'T2' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim o = New B(Of T2)()
                         ~~
BC32105: Type argument 'T2' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(Of T2)()
        ~~~~~~~~
BC32105: Type argument 'T4' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim o = New B(Of T4)()
                         ~~
BC32105: Type argument 'T4' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(Of T4)()
        ~~~~~~~~
BC32105: Type argument 'T5' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim o = New B(Of T5)()
                         ~~
BC32105: Type argument 'T5' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(Of T5)()
        ~~~~~~~~
BC32105: Type argument 'T6' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim o = New B(Of T6)()
                         ~~
BC32105: Type argument 'T6' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(Of T6)()
        ~~~~~~~~
BC32105: Type argument 'T7' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim o = New B(Of T7)()
                         ~~
BC32105: Type argument 'T7' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(Of T7)()
        ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32105ERR_BadTypeArgForStructConstraint2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C
    Shared Sub F(Of T As Structure, U As Structure)(a As T, b As U)
    End Sub
    Shared Sub M(a As Integer, b As Object)
        F(b, a)
        F(a, b)
        F(Of Integer, Object)(a, b)
    End Sub
End Class
        ]]></file>
    </compilation>)
            ' TODO: Dev10 highlights the first type parameter or argument that
            ' violates a generic method constraint, not the entire expression.
            Dim expectedErrors1 = <errors><![CDATA[
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T'.
        F(b, a)
        ~
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(a, b)
        ~
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'U'.
        F(Of Integer, Object)(a, b)
        ~~~~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32105ERR_BadTypeArgForStructConstraint2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports N.A(Of Object).B(Of String)
Imports C = N.A(Of Object).B(Of String)
Namespace N
    Class A(Of T As Structure)
        Friend Class B(Of U As Structure)
        End Class
    End Class
End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T'.
Imports N.A(Of Object).B(Of String)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'U'.
Imports N.A(Of Object).B(Of String)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T'.
Imports C = N.A(Of Object).B(Of String)
        ~
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'U'.
Imports C = N.A(Of Object).B(Of String)
        ~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32105ERR_BadTypeArgForStructConstraint2_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I(Of T As Structure)
    Sub M(Of U As I(Of U))()
End Interface
Class A(Of T As Structure)
    Friend Interface I
    End Interface
End Class
Class B(Of T As I(Of T), U As A(Of U).I)
    Sub M(Of V As A(Of V).I)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            ' TODO: Dev10 reports errors on the type argument violating the
            ' constraint rather than the type with type argument (reporting
            ' error in U in I(Of U) rather than entire I(Of U) for instance).
            Dim expectedErrors1 = <errors><![CDATA[
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Sub M(Of U As I(Of U))()
                  ~~~~~~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class B(Of T As I(Of T), U As A(Of U).I)
                ~~~~~~~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class B(Of T As I(Of T), U As A(Of U).I)
                              ~~~~~~~~~
BC32105: Type argument 'V' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Sub M(Of V As A(Of V).I)()
                  ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32105ERR_BadTypeArgForStructConstraint2_4()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"N.A(Of Object).B(Of String)", "C=N.A(Of N.B).B(Of Object)"}))
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace N
    Class A(Of T As Structure)
        Friend Class B(Of U As Structure)
        End Class
    End Class
    Class B
    End Class
End Namespace
    ]]></file>
</compilation>, options)
            Dim expectedErrors = <errors><![CDATA[
BC32105: Error in project-level import 'C=N.A(Of N.B).B(Of Object)' at 'C=N.A(Of N.B).B(Of Object)' : Type argument 'B' does not satisfy the 'Structure' constraint for type parameter 'T'.
BC32105: Error in project-level import 'C=N.A(Of N.B).B(Of Object)' at 'C=N.A(Of N.B).B(Of Object)' : Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'U'.
BC32105: Error in project-level import 'N.A(Of Object).B(Of String)' at 'N.A(Of Object).B(Of String)' : Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T'.
BC32105: Error in project-level import 'N.A(Of Object).B(Of String)' at 'N.A(Of Object).B(Of String)' : Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'U'.
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC32106ERR_BadTypeArgForRefConstraint2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Class A
End Class
Class B(Of T As Class)
End Class
Class C
    Shared Sub F(Of T As Class)()
    End Sub
    Shared Sub M1(Of T1)()
        Dim o = New B(Of T1)()
        F(Of T1)()
    End Sub
    Shared Sub M2(Of T2 As Class)()
        Dim o = New B(Of T2)()
        F(Of T2)()
    End Sub
    Shared Sub M3(Of T3 As Structure)()
        Dim o = New B(Of T3)()
        F(Of T3)()
    End Sub
    Shared Sub M4(Of T4 As New)()
        Dim o = New B(Of T4)()
        F(Of T4)()
    End Sub
    Shared Sub M5(Of T5 As I)()
        Dim o = New B(Of T5)()
        F(Of T5)()
    End Sub
    Shared Sub M6(Of T6 As A)()
        Dim o = New B(Of T6)()
        F(Of T6)()
    End Sub
    Shared Sub M7(Of T7 As U, U)()
        Dim o = New B(Of T7)()
        F(Of T7)()
    End Sub
    Shared Sub M8()
        Dim o = New B(Of Integer?)()
        F(Of Integer?)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32106: Type argument 'T1' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim o = New B(Of T1)()
                         ~~
BC32106: Type argument 'T1' does not satisfy the 'Class' constraint for type parameter 'T'.
        F(Of T1)()
        ~~~~~~~~
BC32106: Type argument 'T3' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim o = New B(Of T3)()
                         ~~
BC32106: Type argument 'T3' does not satisfy the 'Class' constraint for type parameter 'T'.
        F(Of T3)()
        ~~~~~~~~
BC32106: Type argument 'T4' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim o = New B(Of T4)()
                         ~~
BC32106: Type argument 'T4' does not satisfy the 'Class' constraint for type parameter 'T'.
        F(Of T4)()
        ~~~~~~~~
BC32106: Type argument 'T5' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim o = New B(Of T5)()
                         ~~
BC32106: Type argument 'T5' does not satisfy the 'Class' constraint for type parameter 'T'.
        F(Of T5)()
        ~~~~~~~~
BC32106: Type argument 'T7' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim o = New B(Of T7)()
                         ~~
BC32106: Type argument 'T7' does not satisfy the 'Class' constraint for type parameter 'T'.
        F(Of T7)()
        ~~~~~~~~
BC32106: Type argument 'Integer?' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim o = New B(Of Integer?)()
                         ~~~~~~~~
BC32106: Type argument 'Integer?' does not satisfy the 'Class' constraint for type parameter 'T'.
        F(Of Integer?)()
        ~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32107ERR_RefAndClassTypeConstrCombined()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
    Inherits A
End Class
Interface IA(Of T, U As {A, T, Class})
End Interface
Interface IB
    Sub M(Of T, U As {T, Class, A})()
End Interface
MustInherit Class C(Of T, U)
    MustOverride Sub M(Of V As {T, Class, U})()
End Class
MustInherit Class D
    Inherits C(Of A, B)
    Public Overrides Sub M(Of V As {A, Class, B})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32107: 'Class' constraint and a specific class type constraint cannot be combined.
Interface IA(Of T, U As {A, T, Class})
                               ~~~~~
BC32107: 'Class' constraint and a specific class type constraint cannot be combined.
    Sub M(Of T, U As {T, Class, A})()
                                ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32108ERR_ValueAndClassTypeConstrCombined()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
    Inherits A
End Class
Interface IA(Of T, U As {A, T, Structure})
End Interface
Interface IB
    Sub M(Of T, U As {T, Structure, A})()
End Interface
MustInherit Class C(Of T, U)
    MustOverride Sub M(Of V As {T, Structure, U})()
End Class
MustInherit Class D
    Inherits C(Of A, B)
    Public Overrides Sub M(Of V As {A, Structure, B})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32108: 'Structure' constraint and a specific class type constraint cannot be combined.
Interface IA(Of T, U As {A, T, Structure})
                               ~~~~~~~~~
BC32108: 'Structure' constraint and a specific class type constraint cannot be combined.
    Sub M(Of T, U As {T, Structure, A})()
                                    ~
BC32119: Constraint 'Structure' conflicts with the constraint 'Class A' already specified for type parameter 'V'.
    Public Overrides Sub M(Of V As {A, Structure, B})()
                                       ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32109ERR_ConstraintClashIndirectIndirect4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
End Class
Interface IA(Of T As A, U As B, V As U, W As {T, U, V})
End Interface
Interface IB(Of T1 As A, T2 As B)
    Sub M1(Of T3 As {T1, T2})()
    Sub M2(Of T3 As {T2, T1})()
End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32109: Indirect constraint 'Class B' obtained from the type parameter constraint 'U' conflicts with the indirect constraint 'Class A' obtained from the type parameter constraint 'T'.
Interface IA(Of T As A, U As B, V As U, W As {T, U, V})
                                                 ~
BC32109: Indirect constraint 'Class B' obtained from the type parameter constraint 'V' conflicts with the indirect constraint 'Class A' obtained from the type parameter constraint 'T'.
Interface IA(Of T As A, U As B, V As U, W As {T, U, V})
                                                    ~
BC32109: Indirect constraint 'Class B' obtained from the type parameter constraint 'T2' conflicts with the indirect constraint 'Class A' obtained from the type parameter constraint 'T1'.
    Sub M1(Of T3 As {T1, T2})()
                         ~~
BC32109: Indirect constraint 'Class A' obtained from the type parameter constraint 'T1' conflicts with the indirect constraint 'Class B' obtained from the type parameter constraint 'T2'.
    Sub M2(Of T3 As {T2, T1})()
                         ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32110ERR_ConstraintClashDirectIndirect3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
End Class
Interface IA(Of T1 As A, T2 As B, T3 As {Structure, T1, T2})
End Interface
Interface IB(Of T1 As A)
    Sub M(Of T2, T3 As {T2, B, T1})()
End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32110: Constraint 'Structure' conflicts with the indirect constraint 'Class A' obtained from the type parameter constraint 'T1'.
Interface IA(Of T1 As A, T2 As B, T3 As {Structure, T1, T2})
                                         ~~~~~~~~~
BC32110: Constraint 'Structure' conflicts with the indirect constraint 'Class B' obtained from the type parameter constraint 'T2'.
Interface IA(Of T1 As A, T2 As B, T3 As {Structure, T1, T2})
                                         ~~~~~~~~~
BC32110: Constraint 'Class B' conflicts with the indirect constraint 'Class A' obtained from the type parameter constraint 'T1'.
    Sub M(Of T2, T3 As {T2, B, T1})()
                            ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32110ERR_ConstraintClashDirectIndirect3_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
End Class
Class C
End Class
Class D(Of T1 As A, T2 As B)
    Sub M(Of U1 As T1, U2 As T2, V As {C, T1, T2})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32110: Constraint 'Class C' conflicts with the indirect constraint 'Class A' obtained from the type parameter constraint 'T1'.
    Sub M(Of U1 As T1, U2 As T2, V As {C, T1, T2})()
                                       ~
BC32110: Constraint 'Class C' conflicts with the indirect constraint 'Class B' obtained from the type parameter constraint 'T2'.
    Sub M(Of U1 As T1, U2 As T2, V As {C, T1, T2})()
                                       ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32110ERR_ConstraintClashDirectIndirect3_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Enum E
    A
End Enum
MustInherit Class A(Of T1, T2)
    MustOverride Sub M(Of U1 As T1, U2 As {T2, U1})()
End Class
Class B0
    Inherits A(Of Integer, Integer)
    Public Overrides Sub M(Of U1 As Integer, U2 As {Integer, U1})()
    End Sub
End Class
Class B1
    Inherits A(Of Integer, Object)
    Public Overrides Sub M(Of U1 As Integer, U2 As {Object, U1})()
    End Sub
End Class
Class B2
    Inherits A(Of Integer, Short)
    Public Overrides Sub M(Of U1 As Integer, U2 As {Short, U1})()
    End Sub
End Class
Class B3
    Inherits A(Of Integer, Long)
    Public Overrides Sub M(Of U1 As Integer, U2 As {Long, U1})()
    End Sub
End Class
Class B4
    Inherits A(Of Integer, UInteger)
    Public Overrides Sub M(Of U1 As Integer, U2 As {UInteger, U1})()
    End Sub
End Class
Class B5
    Inherits A(Of Integer, E)
    Public Overrides Sub M(Of U1 As Integer, U2 As {E, U1})()
    End Sub
End Class
Class C0
    Inherits A(Of Object(), Object())
    Public Overrides Sub M(Of U1 As Object(), U2 As {Object(), U1})()
    End Sub
End Class
Class C1
    Inherits A(Of Object(), String())
    Public Overrides Sub M(Of U1 As Object(), U2 As {String(), U1})()
    End Sub
End Class
Class C2
    Inherits A(Of Integer(), Integer())
    Public Overrides Sub M(Of U1 As Integer(), U2 As {Integer(), U1})()
    End Sub
End Class
Class C3
    Inherits A(Of Integer(), E())
    Public Overrides Sub M(Of U1 As Integer(), U2 As {E(), U1})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32110: Constraint 'Short' conflicts with the indirect constraint 'Integer' obtained from the type parameter constraint 'U1'.
    Public Overrides Sub M(Of U1 As Integer, U2 As {Short, U1})()
                                                    ~~~~~
BC32110: Constraint 'Long' conflicts with the indirect constraint 'Integer' obtained from the type parameter constraint 'U1'.
    Public Overrides Sub M(Of U1 As Integer, U2 As {Long, U1})()
                                                    ~~~~
BC32110: Constraint 'UInteger' conflicts with the indirect constraint 'Integer' obtained from the type parameter constraint 'U1'.
    Public Overrides Sub M(Of U1 As Integer, U2 As {UInteger, U1})()
                                                    ~~~~~~~~
BC32110: Constraint 'Enum E' conflicts with the indirect constraint 'Integer' obtained from the type parameter constraint 'U1'.
    Public Overrides Sub M(Of U1 As Integer, U2 As {E, U1})()
                                                    ~
BC32110: Constraint 'E()' conflicts with the indirect constraint 'Integer()' obtained from the type parameter constraint 'U1'.
    Public Overrides Sub M(Of U1 As Integer(), U2 As {E(), U1})()
                                                      ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32111ERR_ConstraintClashIndirectDirect3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
End Class
Class C
End Class
Interface IA(Of T As A, U As {T, Structure})
End Interface
Interface IB(Of T1 As A)
    Sub M(Of T2, T3 As {T2, T1, B})()
End Interface
MustInherit Class D(Of T1, T2)
    MustOverride Sub M(Of U As C, V As {U, T1, T2})()
End Class
Class E
    Inherits D(Of A, B)
    Public Overrides Sub M(Of U As C, V As {U, A, B})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            ' Note: Dev10 never seems to generate BC32111. Instead, Dev10 generates
            ' BC32110 in the following cases, which is essentially the same error
            ' with arguments reordered, but with the other constraint highlighted.
            Dim expectedErrors1 = <errors><![CDATA[
BC32111: Indirect constraint 'Class A' obtained from the type parameter constraint 'T' conflicts with the constraint 'Structure'.
Interface IA(Of T As A, U As {T, Structure})
                              ~
BC32111: Indirect constraint 'Class A' obtained from the type parameter constraint 'T1' conflicts with the constraint 'Class B'.
    Sub M(Of T2, T3 As {T2, T1, B})()
                            ~~
BC32111: Indirect constraint 'Class C' obtained from the type parameter constraint 'U' conflicts with the constraint 'Class A'.
    Public Overrides Sub M(Of U As C, V As {U, A, B})()
                                            ~
BC32111: Indirect constraint 'Class C' obtained from the type parameter constraint 'U' conflicts with the constraint 'Class B'.
    Public Overrides Sub M(Of U As C, V As {U, A, B})()
                                            ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32113ERR_ConstraintCycle2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ConstraintCycle2">
        <file name="a.vb"><![CDATA[
Class A(Of T1 As T2, T2 As T3, T3 As T4, T4 As T1)
End Class
Class B
    Sub M(Of T As T)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32113: Type parameter 'T1' cannot be constrained to itself: 
    'T1' is constrained to 'T2'.
    'T2' is constrained to 'T3'.
    'T3' is constrained to 'T4'.
    'T4' is constrained to 'T1'.
Class A(Of T1 As T2, T2 As T3, T3 As T4, T4 As T1)
                                               ~~
BC32113: Type parameter 'T' cannot be constrained to itself: 
    'T' is constrained to 'T'.
    Sub M(Of T As T)()
                  ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32114ERR_TypeParamWithStructConstAsConst()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeParamWithStructConstAsConst">
        <file name="a.vb"><![CDATA[
Interface I
End Interface
Interface IA(Of T As Structure, U As V, V As {T, New})
End Interface
Interface IB
    Sub M(Of T As U, U As {I, V}, V As {I, Structure})()
End Interface
Interface IC(Of T1 As {I, Structure})
    Sub M(Of T2 As T3, T3 As T1)()
End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
Interface IA(Of T As Structure, U As V, V As {T, New})
                                              ~
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
    Sub M(Of T As U, U As {I, V}, V As {I, Structure})()
                              ~
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
    Sub M(Of T2 As T3, T3 As T1)()
                             ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32115ERR_NullableDisallowedForStructConstr1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Imports System
            Class C
                Shared Sub M()
                    Dim n1? As Nullable(Of Integer) = New Nullable(Of Nullable(Of Integer))
                    Dim n2 As Nullable(Of Integer)? = New Nullable(Of Integer)?
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
                    Dim n1? As Nullable(Of Integer) = New Nullable(Of Nullable(Of Integer))
                               ~~~~~~~~~~~~~~~~~~~~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
                    Dim n1? As Nullable(Of Integer) = New Nullable(Of Nullable(Of Integer))
                                                                      ~~~~~~~~~~~~~~~~~~~~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
                    Dim n2 As Nullable(Of Integer)? = New Nullable(Of Integer)?
                              ~~~~~~~~~~~~~~~~~~~~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
                    Dim n2 As Nullable(Of Integer)? = New Nullable(Of Integer)?
                                                          ~~~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32115ERR_NullableDisallowedForStructConstr1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C(Of T As Structure)
    Shared Sub F(Of U As Structure)()
    End Sub
    Shared Sub M()
        Dim o = New C(Of Integer?)()
        F(Of T?)()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
        Dim o = New C(Of Integer?)()
                         ~~~~~~~~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'U'. Only non-nullable 'Structure' types are allowed.
        F(Of T?)()
        ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32115ERR_NullableDisallowedForStructConstr1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class C(Of T As Structure)
    Shared Sub F(Of U As Structure)()
    End Sub
    Shared Function M() As System.Action
        Return AddressOf F(Of T?)
    End Function
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'U'. Only non-nullable 'Structure' types are allowed.
        Return AddressOf F(Of T?)
                         ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32115ERR_NullableDisallowedForStructConstr1_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Structure S
End Structure
MustInherit Class A(Of T)
    Friend MustOverride Sub M(Of U As T)()
End Class
Class B
    Inherits A(Of S?)
    Friend Overrides Sub M(Of U As S?)()
        Dim o1? As U
        Dim o2 As Nullable(Of U) = New Nullable(Of U)
        Dim o3 As U? = New U?
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC42024: Unused local variable: 'o1'.
        Dim o1? As U
            ~~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
        Dim o1? As U
                   ~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
        Dim o2 As Nullable(Of U) = New Nullable(Of U)
                              ~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
        Dim o2 As Nullable(Of U) = New Nullable(Of U)
                                                   ~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
        Dim o3 As U? = New U?
                  ~
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
        Dim o3 As U? = New U?
                           ~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32117ERR_NoAccessibleNonGeneric1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoAccessibleNonGeneric1">
        <file name="a.vb"><![CDATA[
            Module M1
                Dim x As New C1.C2
            End Module

            Public Class C1
                Public Class C2(Of T)
                End Class
                Public Class C2(Of U, V)
                End Class
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32042: Too few type arguments to 'C1.C2(Of T)'.
                Dim x As New C1.C2
                             ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32118ERR_NoAccessibleGeneric1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoAccessibleGeneric1">
        <file name="a.vb"><![CDATA[
            Module M1
                Dim x As New C1(Of Object).C2(Of SByte, Byte)
            End Module
            Public Class C1
                Public Class C2(Of T)
                End Class
                Public Class C2(Of U, V)
                End Class
            End Class
            Public Class C1(Of T)
                Public Class C2
                End Class
                Public Class C2(Of X)
                End Class
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32045: 'C1(Of Object).C2' has no type parameters and so cannot have type arguments.
                Dim x As New C1(Of Object).C2(Of SByte, Byte)
                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32119ERR_ConflictingDirectConstraints3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
MustInherit Class A(Of T1, T2, T3)
    MustOverride Sub M(Of U As {T1, T2, T3, Structure})()
End Class
Class B
    Inherits A(Of C1, C2, C3)
    Public Overrides Sub M(Of U As {C1, C2, C3, Structure})()
    End Sub
End Class
Class C1
End Class
Class C2
End Class
Class C3
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32119: Constraint 'Class C2' conflicts with the constraint 'Class C1' already specified for type parameter 'U'.
    Public Overrides Sub M(Of U As {C1, C2, C3, Structure})()
                                        ~~
BC32119: Constraint 'Class C3' conflicts with the constraint 'Class C1' already specified for type parameter 'U'.
    Public Overrides Sub M(Of U As {C1, C2, C3, Structure})()
                                            ~~
BC32119: Constraint 'Structure' conflicts with the constraint 'Class C1' already specified for type parameter 'U'.
    Public Overrides Sub M(Of U As {C1, C2, C3, Structure})()
                                                ~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32119ERR_ConflictingDirectConstraints3_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class A
End Class
Class B
End Class
MustInherit Class C(Of T1, T2)
    MustOverride Sub M(Of U As {T1, T2})()
End Class
Class D
    Inherits C(Of A, B)
    Public Overrides Sub M(Of U As {A, B})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32119: Constraint 'Class B' conflicts with the constraint 'Class A' already specified for type parameter 'U'.
    Public Overrides Sub M(Of U As {A, B})()
                                       ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32119ERR_ConflictingDirectConstraints3_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Enum E
    A
End Enum
MustInherit Class A(Of T, U)
    MustOverride Sub M(Of V As {T, U})()
End Class
Class B0
    Inherits A(Of Integer, Integer)
    Public Overrides Sub M(Of V As {Integer})()
    End Sub
End Class
Class B1
    Inherits A(Of Integer, Object)
    Public Overrides Sub M(Of V As {Integer, Object})()
    End Sub
End Class
Class B2
    Inherits A(Of Integer, Short)
    Public Overrides Sub M(Of V As {Integer, Short})()
    End Sub
End Class
Class B3
    Inherits A(Of Integer, Long)
    Public Overrides Sub M(Of V As {Integer, Long})()
    End Sub
End Class
Class B4
    Inherits A(Of Integer, UInteger)
    Public Overrides Sub M(Of V As {Integer, UInteger})()
    End Sub
End Class
Class B5
    Inherits A(Of Integer, E)
    Public Overrides Sub M(Of V As {Integer, E})()
    End Sub
End Class
Class C0
    Inherits A(Of Object(), Object())
    Public Overrides Sub M(Of V As {Object()})()
    End Sub
End Class
Class C1
    Inherits A(Of Object(), String())
    Public Overrides Sub M(Of V As {Object(), String()})()
    End Sub
End Class
Class C2
    Inherits A(Of Integer(), Integer())
    Public Overrides Sub M(Of V As {Integer()})()
    End Sub
End Class
Class C3
    Inherits A(Of Integer(), E())
    Public Overrides Sub M(Of V As {Integer(), E()})()
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32119: Constraint 'Short' conflicts with the constraint 'Integer' already specified for type parameter 'V'.
    Public Overrides Sub M(Of V As {Integer, Short})()
                                             ~~~~~
BC32119: Constraint 'Long' conflicts with the constraint 'Integer' already specified for type parameter 'V'.
    Public Overrides Sub M(Of V As {Integer, Long})()
                                             ~~~~
BC32119: Constraint 'UInteger' conflicts with the constraint 'Integer' already specified for type parameter 'V'.
    Public Overrides Sub M(Of V As {Integer, UInteger})()
                                             ~~~~~~~~
BC32119: Constraint 'Enum E' conflicts with the constraint 'Integer' already specified for type parameter 'V'.
    Public Overrides Sub M(Of V As {Integer, E})()
                                             ~
BC32119: Constraint 'E()' conflicts with the constraint 'Integer()' already specified for type parameter 'V'.
    Public Overrides Sub M(Of V As {Integer(), E()})()
                                               ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543643, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543643")>
        Public Sub BC32120ERR_InterfaceUnifiesWithInterface2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceUnifiesWithInterface2">
        <file name="a.vb"><![CDATA[
            Public Interface interfaceA(Of u)
            End Interface
            Public Interface derivedInterface(Of t1, t2)
                Inherits interfaceA(Of t1), interfaceA(Of t2)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32120: Cannot inherit interface 'interfaceA(Of t2)' because it could be identical to interface 'interfaceA(Of t1)' for some type arguments.
                Inherits interfaceA(Of t1), interfaceA(Of t2)
                                            ~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(1042692, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042692")>
        <Fact()>
        Public Sub BC32120ERR_InterfaceUnifiesWithInterface2_SubstituteWithOtherTypeParameter()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IA(Of T, U)
End Interface
Interface IB(Of T, U)
    Inherits IA(Of U, Object), IA(Of T, U)
End Interface
        ]]></file>
    </compilation>)
            compilation1.AssertTheseDeclarationDiagnostics(
                <errors><![CDATA[
BC32120: Cannot inherit interface 'IA(Of T, U)' because it could be identical to interface 'IA(Of U, Object)' for some type arguments.
    Inherits IA(Of U, Object), IA(Of T, U)
                               ~~~~~~~~~~~
     ]]></errors>)
        End Sub

        <Fact(), WorkItem(543726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543726")>
        Public Sub BC32121ERR_BaseUnifiesWithInterfaces3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BaseUnifiesWithInterfaces3">
        <file name="a.vb"><![CDATA[
            Interface I1(Of T)
                Sub goo(Of G As T)(ByVal x As G)
            End Interface
            Interface I2(Of T)
                Inherits I1(Of T)
            End Interface
            Interface I02(Of T, G)
                Inherits I1(Of T), I2(Of G)    
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32121: Cannot inherit interface 'I2(Of G)' because the interface 'I1(Of G)' from which it inherits could be identical to interface 'I1(Of T)' for some type arguments.
                Inherits I1(Of T), I2(Of G)    
                                   ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543727")>
        Public Sub BC32122ERR_InterfaceBaseUnifiesWithBase4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceBaseUnifiesWithBase4">
        <file name="a.vb"><![CDATA[
            Public Interface interfaceA(Of u)
            End Interface
            Public Interface interfaceX(Of v)
                Inherits interfaceA(Of v)
            End Interface
            Public Interface interfaceY(Of w)
                Inherits interfaceA(Of w)
            End Interface
            Public Interface derivedInterface(Of t1, t2)
                Inherits interfaceX(Of t1), interfaceY(Of t2)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32122: Cannot inherit interface 'interfaceY(Of t2)' because the interface 'interfaceA(Of t2)' from which it inherits could be identical to interface 'interfaceA(Of t1)' from which the interface 'interfaceX(Of t1)' inherits for some type arguments.
                Inherits interfaceX(Of t1), interfaceY(Of t2)
                                            ~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543729")>
        Public Sub BC32123ERR_InterfaceUnifiesWithBase3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceUnifiesWithBase3">
        <file name="a.vb"><![CDATA[
            Public Interface interfaceA(Of u)
                Inherits interfaceX(Of u)
            End Interface
            Public Interface interfaceX(Of v)
            End Interface
            Public Interface derivedInterface(Of t1, t2)
                Inherits interfaceA(Of t1), interfaceX(Of t2)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32123: Cannot inherit interface 'interfaceX(Of t2)' because it could be identical to interface 'interfaceX(Of t1)' from which the interface 'interfaceA(Of t1)' inherits for some type arguments.
                Inherits interfaceA(Of t1), interfaceX(Of t2)
                                            ~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543643, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543643")>
        Public Sub BC32072ERR_InterfacePossiblyImplTwice2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfacePossiblyImplTwice2">
        <file name="a.vb"><![CDATA[
            Public Interface interfaceA(Of u)
            End Interface
            Public Class derivedClass(Of t1, t2)
                Implements interfaceA(Of t1), interfaceA(Of t2)
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32072: Cannot implement interface 'interfaceA(Of t2)' because its implementation could conflict with the implementation of another implemented interface 'interfaceA(Of t1)' for some type arguments.
                Implements interfaceA(Of t1), interfaceA(Of t2)
                                              ~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543726")>
        Public Sub BC32131ERR_ClassInheritsBaseUnifiesWithInterfaces3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ClassInheritsBaseUnifiesWithInterfaces3">
        <file name="a.vb"><![CDATA[
            Interface I1(Of T)
            End Interface
            Interface I2(Of T)
                Inherits I1(Of T)
            End Interface
            Class I02(Of T, G)
                Implements I1(Of T), I2(Of G)    
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32131: Cannot implement interface 'I2(Of G)' because the interface 'I1(Of G)' from which it inherits could be identical to implemented interface 'I1(Of T)' for some type arguments.
                Implements I1(Of T), I2(Of G)    
                                     ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543727")>
        Public Sub BC32132ERR_ClassInheritsInterfaceBaseUnifiesWithBase4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ClassInheritsInterfaceBaseUnifiesWithBase4">
        <file name="a.vb"><![CDATA[
            Public Interface interfaceA(Of u)
            End Interface
            Public Interface interfaceX(Of v)
                Inherits interfaceA(Of v)
            End Interface
            Public Interface interfaceY(Of w)
                Inherits interfaceA(Of w)
            End Interface
            Public Class derivedClass(Of t1, t2)
                Implements interfaceX(Of t1), interfaceY(Of t2)
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32132: Cannot implement interface 'interfaceY(Of t2)' because the interface 'interfaceA(Of t2)' from which it inherits could be identical to interface 'interfaceA(Of t1)' from which the implemented interface 'interfaceX(Of t1)' inherits for some type arguments.
                Implements interfaceX(Of t1), interfaceY(Of t2)
                                              ~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543729")>
        Public Sub BC32133ERR_ClassInheritsInterfaceUnifiesWithBase3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ClassInheritsInterfaceUnifiesWithBase3">
        <file name="a.vb"><![CDATA[
            Public Interface interfaceA(Of u)
                Inherits interfaceX(Of u)
            End Interface
            Public Interface interfaceX(Of v)
            End Interface
            Public Class derivedClass(Of t1, t2)
                Implements interfaceA(Of t1), interfaceX(Of t2)
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32133: Cannot implement interface 'interfaceX(Of t2)' because it could be identical to interface 'interfaceX(Of t1)' from which the implemented interface 'interfaceA(Of t1)' inherits for some type arguments.
                Implements interfaceA(Of t1), interfaceX(Of t2)
                                              ~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32125ERR_InterfaceMethodImplsUnify3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InterfaceMethodImplsUnify3">
        <file name="a.vb"><![CDATA[
            Public Interface iFace1(Of t)
                Sub testSub()
            End Interface
            Public Interface iFace2(Of u)
                Inherits iFace1(Of u)
            End Interface
            Public Class testClass(Of y, z)
                Implements iFace1(Of y), iFace2(Of z)
                Public Sub testSuby() Implements iFace1(Of y).testSub
                End Sub
                Public Sub testSubz() Implements iFace1(Of z).testSub
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32131: Cannot implement interface 'iFace2(Of z)' because the interface 'iFace1(Of z)' from which it inherits could be identical to implemented interface 'iFace1(Of y)' for some type arguments.
                Implements iFace1(Of y), iFace2(Of z)
                                         ~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32200ERR_ShadowingTypeOutsideClass1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ShadowingTypeOutsideClass1">
        <file name="a.vb"><![CDATA[
            Public Shadows Class C1
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32200: 'C1' cannot be declared 'Shadows' outside of a class, structure, or interface.
Public Shadows Class C1
                     ~~                                      
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32201ERR_PropertySetParamCollisionWithValue()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PropertySetParamCollisionWithValue">
        <file name="a.vb"><![CDATA[
            Interface IA
                ReadOnly Property Goo(ByVal value As String) As String
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32201: Property parameters cannot have the name 'Value'.
                ReadOnly Property Goo(ByVal value As String) As String
                                            ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32201ERR_PropertySetParamCollisionWithValue_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PropertySetParamCollisionWithValue">
        <file name="a.vb"><![CDATA[
            Structure S
                Property P(Index, Value)
                    Get
                        Return Nothing
                    End Get
                    Set(value)
                    End Set
                End Property
            End Structure
            Class C
                WriteOnly Property P(value)
                    Set(val)
                    End Set
                End Property
                ReadOnly Property Value(Value)
                    Get
                        Return Nothing
                    End Get
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32201: Property parameters cannot have the name 'Value'.
                Property P(Index, Value)
                                  ~~~~~
BC32201: Property parameters cannot have the name 'Value'.
                WriteOnly Property P(value)
                                     ~~~~~
BC32201: Property parameters cannot have the name 'Value'.
                ReadOnly Property Value(Value)
                                        ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC32205ERR_WithEventsNameTooLong()
            Dim witheventname = New String("A"c, 1020)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
        Option Strict On
        Imports System

        Module Program
            ' Declare a WithEvents variable.
            Dim WithEvents <%= witheventname %> As New EventClass

            ' Call the method that raises the object's events.
            Sub TestEvents()
                 <%= witheventname %>.RaiseEvents()
            End Sub

            ' Declare an event handler that handles multiple events.
            Sub EClass_EventHandler() Handles <%= witheventname %>.XEvent, <%= witheventname %>.YEvent
                Console.WriteLine("Received Event.")
            End Sub

            Class EventClass
                Public Event XEvent()
                Public Event YEvent()
                ' RaiseEvents raises both events.
                Sub RaiseEvents()
                    RaiseEvent XEvent()
                    RaiseEvent YEvent()
                End Sub
            End Class
        End Module
        </file>
    </compilation>)
            Dim squiggle As New String("~"c, witheventname.Length())
            Dim expectedErrors1 = <errors>
BC37220: Name 'get_<%= witheventname %>' exceeds the maximum length allowed in metadata.
            Dim WithEvents <%= witheventname %> As New EventClass
                           <%= squiggle %>
BC37220: Name 'set_<%= witheventname %>' exceeds the maximum length allowed in metadata.
            Dim WithEvents <%= witheventname %> As New EventClass
                           <%= squiggle %>
                                  </errors>
            CompilationUtils.AssertNoDeclarationDiagnostics(compilation1)
            CompilationUtils.AssertTheseEmitDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' BC32206ERR_SxSIndirectRefHigherThanDirectRef1: See ReferenceManagerTests.ReferenceBinding_SymbolUsed
        ' BC32208ERR_DuplicateReference2: See ReferenceManagerTests.BC32208ERR_DuplicateReference2
        ' BC32208ERR_DuplicateReference2_2: See ReferenceManagerTests.BC32208ERR_DuplicateReference2_2

        <Fact()>
        Public Sub BC32501ERR_ComClassAndReservedAttribute1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ComClassAndReservedAttribute1">
        <file name="a.vb"><![CDATA[
            Imports Microsoft.VisualBasic
            <ComClass("287E43DD-5282-452C-91AF-8F1B34290CA3"), System.Runtime.InteropServices.ComSourceInterfaces(GetType(a), GetType(a))> _
            Public Class c
                Public Sub GOO()
                End Sub
            End Class
            Public Interface a
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ComSourceInterfacesAttribute' cannot both be applied to the same class.
            Public Class c
                         ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32504ERR_ComClassRequiresPublicClass2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ComClassRequiresPublicClass2">
        <file name="a.vb"><![CDATA[
            Imports Microsoft.VisualBasic
            Class C
                Protected Class C1
                    <ComClass()>
                    Class C2
                    End Class
                End Class
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32504: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to 'C2' because its container 'C1' is not declared 'Public'.
                    Class C2
                          ~~
BC40011: 'Microsoft.VisualBasic.ComClassAttribute' is specified for class 'C2' but 'C2' has no public members that can be exposed to COM; therefore, no COM interfaces are generated.
                    Class C2
                          ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32507ERR_ComClassDuplicateGuids1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ComClassDuplicateGuids1">
        <file name="a.vb"><![CDATA[
            Imports Microsoft.VisualBasic
            <ComClass("22904D63-46C2-47b6-97A7-8970D8EC789A", "22904D63-46C2-47b6-97A7-8970D8EC789A", "22904D63-46C2-47b6-97A7-8970D8EC789A")>
            Public Class Class11
                Sub test()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32507: 'InterfaceId' and 'EventsId' parameters for 'Microsoft.VisualBasic.ComClassAttribute' on 'Class11' cannot have the same value.
            Public Class Class11
                         ~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32508ERR_ComClassCantBeAbstract0()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ComClassCantBeAbstract0">
        <file name="a.vb"><![CDATA[
            Imports Microsoft.VisualBasic
            <ComClass()>
            Public MustInherit Class C
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32508: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to a class that is declared 'MustInherit'.
            Public MustInherit Class C
                                     ~
BC40011: 'Microsoft.VisualBasic.ComClassAttribute' is specified for class 'C' but 'C' has no public members that can be exposed to COM; therefore, no COM interfaces are generated.
            Public MustInherit Class C
                                     ~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32509ERR_ComClassRequiresPublicClass1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ComClassRequiresPublicClass1">
        <file name="a.vb"><![CDATA[
            Imports Microsoft.VisualBasic
            Class C
                <ComClass()>
                Protected Class C1
                End Class
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32509: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to 'C1' because it is not declared 'Public'.
                Protected Class C1
                                ~~
BC40011: 'Microsoft.VisualBasic.ComClassAttribute' is specified for class 'C1' but 'C1' has no public members that can be exposed to COM; therefore, no COM interfaces are generated.
                Protected Class C1
                                ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC33009ERR_ParamArrayIllegal1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ParamArrayIllegal1">
        <file name="a.vb"><![CDATA[
            Class C1
                Delegate Sub Goo(ByVal ParamArray args() As String)
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC33009: 'Delegate' parameters cannot be declared 'ParamArray'.
                Delegate Sub Goo(ByVal ParamArray args() As String)
                                       ~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33010ERR_OptionalIllegal1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OptionalIllegal1">
        <file name="a.vb"><![CDATA[
            Class C1
                Event E(Optional ByVal z As String = "")
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC33010: 'Event' parameters cannot be declared 'Optional'.
                Event E(Optional ByVal z As String = "")
                        ~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33011ERR_OperatorMustBePublic()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OperatorMustBePublic">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Private Shared Operator IsFalse(ByVal z As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
                Public Shared Operator IsTrue(ByVal z As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_OperatorMustBePublic, "Private").WithArguments("Private"))

        End Sub

        <Fact()>
        Public Sub BC33012ERR_OperatorMustBeShared()

            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OperatorMustBeShared">
        <file name="a.vb"><![CDATA[
            Public Structure S1
               Public Operator IsFalse(ByVal z As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
                Public Shared Operator IsTrue(ByVal z As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_OperatorMustBeShared, "IsFalse"))

        End Sub

        <Fact()>
        Public Sub BC33013ERR_BadOperatorFlags1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BadOperatorFlags1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Overridable Shared Operator IsFalse(ByVal z As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
                Public Shared Operator IsTrue(ByVal z As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_BadOperatorFlags1, "Overridable").WithArguments("Overridable"))

        End Sub

        <Fact()>
        Public Sub BC33014ERR_OneParameterRequired1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OneParameterRequired1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Operator IsFalse(ByVal z As S1, ByVal x As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
                Public Shared Operator IsTrue(ByVal z As S1, ByVal x As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
            End Structure

        ]]></file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_OneParameterRequired1, "IsFalse").WithArguments("IsFalse"),
            Diagnostic(ERRID.ERR_OneParameterRequired1, "IsTrue").WithArguments("IsTrue"))

        End Sub

        <Fact()>
        Public Sub BC33015ERR_TwoParametersRequired1()

            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TwoParametersRequired1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
               Public Shared Operator And(ByVal x As S1) As S1
                    Dim r As New S1
                    Return r
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_TwoParametersRequired1, "And").WithArguments("And"))
        End Sub

        ' Roslyn extra errors
        <Fact()>
        Public Sub BC33016ERR_OneOrTwoParametersRequired1()

            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OneOrTwoParametersRequired1">
        <file name="a.vb"><![CDATA[
            Public Class C1
                    Public Shared Operator +
            (ByVal p1 As C1, ByVal p2 As Integer) As Integer
                End Operator
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(
        Diagnostic(ERRID.ERR_ExpectedLparen, ""),
        Diagnostic(ERRID.ERR_ExpectedRparen, ""),
        Diagnostic(ERRID.ERR_Syntax, "("),
        Diagnostic(ERRID.ERR_OneOrTwoParametersRequired1, "+").WithArguments("+"),
        Diagnostic(ERRID.WRN_DefAsgNoRetValOpRef1, "End Operator").WithArguments("+"))
            '            Dim expectedErrors1 = <errors><![CDATA[
            'BC30199: '(' expected.
            '                    Public Shared Operator +
            '                                           ~
            'BC33016: Operator '+' must have either one or two parameters.
            '                    Public Shared Operator +
            '                                           ~
            'BC30035: Syntax error.
            '            (ByVal p1 As C1, ByVal p2 As Integer) As Integer
            '            ~
            '     ]]></errors>
            '            CompilationUtils.AssertTheseDeclarationErrors(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33017ERR_ConvMustBeWideningOrNarrowing()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConvMustBeWideningOrNarrowing">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Operator CType(ByVal x As S1) As Integer
                    Return 1
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConvMustBeWideningOrNarrowing, "CType"))
        End Sub

        <Fact()>
        Public Sub BC33019ERR_InvalidSpecifierOnNonConversion1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidSpecifierOnNonConversion1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Widening Operator And(ByVal x As S1, ByVal y As S1) As Integer
                    Return 1
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidSpecifierOnNonConversion1, "Widening").WithArguments("Widening"))

        End Sub

        <Fact()>
        Public Sub BC33020ERR_UnaryParamMustBeContainingType1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UnaryParamMustBeContainingType1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Operator IsTrue(ByVal x As Integer) As Boolean
                    Return 1
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_UnaryParamMustBeContainingType1, "IsTrue").WithArguments("S1"))

        End Sub

        <Fact()>
        Public Sub BC33021ERR_BinaryParamMustBeContainingType1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BinaryParamMustBeContainingType1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Operator And(ByVal x As Integer, ByVal y As Integer) As Boolean
                    Return 1
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_BinaryParamMustBeContainingType1, "And").WithArguments("S1"))

        End Sub

        <Fact()>
        Public Sub BC33022ERR_ConvParamMustBeContainingType1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConvParamMustBeContainingType1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Widening Operator CType(ByVal x As Integer) As Boolean
                    Return 1
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConvParamMustBeContainingType1, "CType").WithArguments("S1"))

        End Sub

        <Fact()>
        Public Sub BC33023ERR_OperatorRequiresBoolReturnType1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OperatorRequiresBoolReturnType1">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Operator IsTrue(ByVal x As S1) As Integer
                    Return 1
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_OperatorRequiresBoolReturnType1, "IsTrue").WithArguments("IsTrue"))

        End Sub

        <Fact()>
        Public Sub BC33024ERR_ConversionToSameType()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionToSameType">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Widening Operator CType(ByVal x As S1) As S1
                    Return Nothing
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionToSameType, "CType"))
        End Sub

        <Fact()>
        Public Sub BC33025ERR_ConversionToInterfaceType()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionToInterfaceType">
        <file name="a.vb"><![CDATA[
            Interface I1
            End Interface
            Public Structure S1
                Public Shared Widening Operator CType(ByVal x As S1) As I1
                    Return Nothing
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(
        Diagnostic(ERRID.ERR_AccessMismatchOutsideAssembly4, "I1").WithArguments("op_Implicit", "I1", "structure", "S1"),
        Diagnostic(ERRID.ERR_ConversionToInterfaceType, "CType"))

        End Sub

        <Fact()>
        Public Sub BC33026ERR_ConversionToBaseType()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionToBaseType">
        <file name="a.vb"><![CDATA[
            Class C1
            End Class
            Class S1
                Inherits C1
                Public Shared Widening Operator CType(ByVal x As S1) As C1
                    Return Nothing
                End Operator
            End Class	
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionToBaseType, "CType"))

        End Sub

        <Fact()>
        Public Sub BC33027ERR_ConversionToDerivedType()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionToDerivedType">
        <file name="a.vb"><![CDATA[
            Class C1
                Inherits S1
            End Class
            Class S1
                Public Shared Widening Operator CType(ByVal x As S1) As C1
                    Return Nothing
                End Operator
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionToDerivedType, "CType"))

        End Sub

        <Fact()>
        Public Sub BC33028ERR_ConversionToObject()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionToObject">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Public Shared Widening Operator CType(ByVal x As S1) As Object
                    Return Nothing
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionToObject, "CType"))

        End Sub

        <Fact()>
        Public Sub BC33029ERR_ConversionFromInterfaceType()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionFromInterfaceType">
        <file name="a.vb"><![CDATA[
            Interface I1
            End Interface
            Class S1
                Public Shared Widening Operator CType(ByVal x As I1) As S1
                    Return Nothing
                End Operator
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionFromInterfaceType, "CType"))
        End Sub

        <Fact()>
        Public Sub BC33030ERR_ConversionFromBaseType()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionFromBaseType">
        <file name="a.vb"><![CDATA[
            Class C1
            End Class
            Class S1
                Inherits C1
                Public Shared Widening Operator CType(ByVal x As C1) As S1
                    Return Nothing
                End Operator
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionFromBaseType, "CType"))
        End Sub

        <Fact()>
        Public Sub BC33031ERR_ConversionFromDerivedType()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionFromDerivedType">
        <file name="a.vb"><![CDATA[
            Class C1
                Inherits S1
            End Class
            Class S1
                Public Shared Widening Operator CType(ByVal x As C1) As S1
                    Return Nothing
                End Operator
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionFromDerivedType, "CType"))

        End Sub

        <Fact()>
        Public Sub BC33032ERR_ConversionFromObject()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConversionFromObject">
        <file name="a.vb"><![CDATA[
            Class S1
                Public Shared Widening Operator CType(ByVal x As Object) As S1
                    Return Nothing
                End Operator
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ConversionFromObject, "CType"))

        End Sub

        <Fact()>
        Public Sub BC33033ERR_MatchingOperatorExpected2()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MatchingOperatorExpected2">
        <file name="a.vb"><![CDATA[
            Public Structure S1
                Dim d As Date
                Public Shared Operator IsFalse(ByVal z As S1) As Boolean
                    Dim b As Boolean
                    Return b
                End Operator
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_MatchingOperatorExpected2, "IsFalse").WithArguments("IsTrue", "Public Shared Operator IsFalse(z As S1) As Boolean"))
        End Sub

        <Fact()>
        Public Sub BC33041ERR_OperatorRequiresIntegerParameter1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OperatorRequiresIntegerParameter1">
        <file name="a.vb"><![CDATA[
            Class S1
                Public Shared Operator >>(ByVal x As S1, ByVal y As String) As S1
                    Return Nothing
                End Operator
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_OperatorRequiresIntegerParameter1, ">>").WithArguments(">>"))

        End Sub

        <Fact()>
        Public Sub BC33101ERR_BadTypeArgForStructConstraintNull()
            Dim compilation1 = CreateCompilationWithMscorlib40(
    <compilation name="BadTypeArgForStructConstraintNull">
        <file name="a.vb"><![CDATA[
Imports System
Interface I
End Interface
Structure S
End Structure
Class A
End Class
Class B(Of T)
    Shared Sub M1(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As T)()
        Dim o? As Object
        Dim s? As S
        Dim _1? As T1
        Dim _2? As T2
        Dim _3? As T3
        Dim _4? As T4
        Dim _5? As T5
        Dim _6? As T6
        Dim _7? As T7
    End Sub
    Shared Sub M2(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As T)()
        Dim o As Nullable(Of Object) = New Nullable(Of Object)
        Dim s As Nullable(Of S) = New Nullable(Of S)
        Dim _1 As Nullable(Of T1) = New Nullable(Of T1)
        Dim _2 As Nullable(Of T2) = New Nullable(Of T2)
        Dim _3 As Nullable(Of T3) = New Nullable(Of T3)
        Dim _4 As Nullable(Of T4) = New Nullable(Of T4)
        Dim _5 As Nullable(Of T5) = New Nullable(Of T5)
        Dim _6 As Nullable(Of T6) = New Nullable(Of T6)
        Dim _7 As Nullable(Of T7) = New Nullable(Of T7)
    End Sub
    Shared Sub M3(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As T)()
        Dim o As Object? = New Object?
        Dim s As S? = New S?
        Dim _1 As T1? = New T1?
        Dim _2 As T2? = New T2?
        Dim _3 As T3? = New T3?
        Dim _4 As T4? = New T4?
        Dim _5 As T5? = New T5?
        Dim _6 As T6? = New T6?
        Dim _7 As T7? = New T7?
    End Sub
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC42024: Unused local variable: 'o'.
        Dim o? As Object
            ~
BC33101: Type 'Object' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim o? As Object
            ~~
BC42024: Unused local variable: 's'.
        Dim s? As S
            ~
BC42024: Unused local variable: '_1'.
        Dim _1? As T1
            ~~
BC33101: Type 'T1' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _1? As T1
            ~~~
BC42024: Unused local variable: '_2'.
        Dim _2? As T2
            ~~
BC33101: Type 'T2' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _2? As T2
            ~~~
BC42024: Unused local variable: '_3'.
        Dim _3? As T3
            ~~
BC42024: Unused local variable: '_4'.
        Dim _4? As T4
            ~~
BC33101: Type 'T4' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _4? As T4
            ~~~
BC42024: Unused local variable: '_5'.
        Dim _5? As T5
            ~~
BC33101: Type 'T5' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _5? As T5
            ~~~
BC42024: Unused local variable: '_6'.
        Dim _6? As T6
            ~~
BC33101: Type 'T6' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _6? As T6
            ~~~
BC42024: Unused local variable: '_7'.
        Dim _7? As T7
            ~~
BC33101: Type 'T7' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _7? As T7
            ~~~
BC33101: Type 'Object' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim o As Nullable(Of Object) = New Nullable(Of Object)
                             ~~~~~~
BC33101: Type 'Object' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim o As Nullable(Of Object) = New Nullable(Of Object)
                                                       ~~~~~~
BC33101: Type 'T1' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _1 As Nullable(Of T1) = New Nullable(Of T1)
                              ~~
BC33101: Type 'T1' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _1 As Nullable(Of T1) = New Nullable(Of T1)
                                                    ~~
BC33101: Type 'T2' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _2 As Nullable(Of T2) = New Nullable(Of T2)
                              ~~
BC33101: Type 'T2' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _2 As Nullable(Of T2) = New Nullable(Of T2)
                                                    ~~
BC33101: Type 'T4' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _4 As Nullable(Of T4) = New Nullable(Of T4)
                              ~~
BC33101: Type 'T4' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _4 As Nullable(Of T4) = New Nullable(Of T4)
                                                    ~~
BC33101: Type 'T5' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _5 As Nullable(Of T5) = New Nullable(Of T5)
                              ~~
BC33101: Type 'T5' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _5 As Nullable(Of T5) = New Nullable(Of T5)
                                                    ~~
BC33101: Type 'T6' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _6 As Nullable(Of T6) = New Nullable(Of T6)
                              ~~
BC33101: Type 'T6' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _6 As Nullable(Of T6) = New Nullable(Of T6)
                                                    ~~
BC33101: Type 'T7' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _7 As Nullable(Of T7) = New Nullable(Of T7)
                              ~~
BC33101: Type 'T7' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _7 As Nullable(Of T7) = New Nullable(Of T7)
                                                    ~~
BC33101: Type 'Object' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim o As Object? = New Object?
                 ~~~~~~
BC33101: Type 'Object' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim o As Object? = New Object?
                               ~~~~~~
BC33101: Type 'T1' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _1 As T1? = New T1?
                  ~~
BC33101: Type 'T1' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _1 As T1? = New T1?
                            ~~
BC33101: Type 'T2' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _2 As T2? = New T2?
                  ~~
BC33101: Type 'T2' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _2 As T2? = New T2?
                            ~~
BC33101: Type 'T4' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _4 As T4? = New T4?
                  ~~
BC33101: Type 'T4' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _4 As T4? = New T4?
                            ~~
BC33101: Type 'T5' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _5 As T5? = New T5?
                  ~~
BC33101: Type 'T5' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _5 As T5? = New T5?
                            ~~
BC33101: Type 'T6' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _6 As T6? = New T6?
                  ~~
BC33101: Type 'T6' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _6 As T6? = New T6?
                            ~~
BC33101: Type 'T7' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _7 As T7? = New T7?
                  ~~
BC33101: Type 'T7' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Dim _7 As T7? = New T7?
                            ~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33102ERR_CantSpecifyArrayAndNullableOnBoth()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantSpecifyArrayAndNullableOnBoth">
        <file name="a.vb"><![CDATA[
            Class S1
                Sub goo()
                    Dim numbers? As Integer()
                    Dim values() As Integer?
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC42024: Unused local variable: 'numbers'.
                    Dim numbers? As Integer()
                        ~~~~~~~
BC33101: Type 'Integer()' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
                    Dim numbers? As Integer()
                        ~~~~~~~~
BC42024: Unused local variable: 'values'.
                    Dim values() As Integer?
                        ~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33109ERR_CantSpecifyAsNewAndNullable()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantSpecifyAsNewAndNullable">
        <file name="a.vb"><![CDATA[
            Imports System
            Class C
                Shared Sub M()
                    Dim x? As New Guid()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC33109: Nullable modifier cannot be specified in variable declarations with 'As New'.
                    Dim x? As New Guid()
                           ~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC33112ERR_NullableImplicit()
            Dim expectedErrors As New Dictionary(Of String, XElement) From {
            {"On",
<expected><![CDATA[
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Public field1?()
                           ~~~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Public field2?(,)
                           ~~~~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Public field3?()()
                           ~~~~~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Public field4?
                           ~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Dim local1?()
                        ~~~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Dim local2?(,)
                        ~~~~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Dim local3?()()
                        ~~~~~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
                    Dim local4?
                        ~~~~~~~
]]></expected>},
            {"Off",
<expected><![CDATA[
BC36629: Nullable type inference is not supported in this context.
                    Dim local5? = 23 ' this is ok for Option Infer On
                        ~~~~~~~
]]></expected>}}

            For Each infer In {"On", "Off"}
                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="NullableImplicit">
            <file name="a.vb">
            Option Infer <%= infer %>
            Class C1
                    Public field1?()
                    Public field2?(,)
                    Public field3?()()
                    Public field4?
                    'Public field5? = 23 ' this is _NOT_ ok for Option Infer On, but it gives another diagnostic.
                    Public field6?(1)   ' this is ok for Option Infer On
                    Public field7?(1)() ' this is ok for Option Infer On

                Sub goo()
                    Dim local1?()
                    Dim local2?(,)
                    Dim local3?()()
                    Dim local4?
                    Dim local5? = 23 ' this is ok for Option Infer On
                    Dim local6?(1)   ' this is ok for Option Infer On
                    Dim local7?(1)() ' this is ok for Option Infer On

                    local1 = nothing
                    local2 = nothing
                    local3 = nothing
                    local4 = nothing
                End Sub
            End Class
        </file>
        </compilation>)

                CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors(infer))
            Next
        End Sub

        <Fact(), WorkItem(651624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651624")>
        Public Sub NestedNullableWithAttemptedConversion()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="c.vb">
                    Imports System
                    Class C
                        Public Sub Main()
                            Dim x As Nullable(Of Nullable(Of Integer)) = Nothing
                            Dim y As Nullable(Of Integer) = Nothing
                            Console.WriteLine(x Is Nothing)
                            Console.WriteLine(y Is Nothing)
                            Console.WriteLine(x = y)
                        End Sub
                    End Class
                </file>
                </compilation>)
            CompilationUtils.AssertTheseDiagnostics(comp,
<errors><![CDATA[
BC32115: 'System.Nullable' does not satisfy the 'Structure' constraint for type parameter 'T'. Only non-nullable 'Structure' types are allowed.
                            Dim x As Nullable(Of Nullable(Of Integer)) = Nothing
                                                 ~~~~~~~~~~~~~~~~~~~~
BC30452: Operator '=' is not defined for types 'Integer??' and 'Integer?'.
                            Console.WriteLine(x = y)
                                              ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC36015ERR_PropertyNameConflictInMyCollection()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PropertyNameConflictInMyCollection">
        <file name="a.vb"><![CDATA[
            Module module1
                Public f As New GRBB
            End Module
            <Global.Microsoft.VisualBasic.MyGroupCollection("base", "create", "DisposeI", "Module1.f")> _
            Public NotInheritable Class GRBB
                Private Shared Function Create(Of T As {New, base})(ByVal Instance As T) As T
                    Return Nothing
                End Function
                Private Sub DisposeI(Of T As base)(ByRef Instance As T)
                End Sub
                Public m_derived As Short
            End Class
            Public Class base
                Public i As Integer
            End Class
            Public Class disposei
                Inherits base
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36015: 'Private Sub DisposeI(Of T As base)(ByRef Instance As T)' has the same name as a member used for type 'disposei' exposed in a 'My' group. Rename the type or its enclosing namespace.
            <Global.Microsoft.VisualBasic.MyGroupCollection("base", "create", "DisposeI", "Module1.f")> _
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36551ERR_ExtensionMethodNotInModule()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="ExtensionMethodNotInModule">
        <file name="a.vb"><![CDATA[
            Imports System.Runtime.CompilerServices
            Imports System
            Class C1
                <Extension()> _
                Public Sub Print(ByVal str As String)
                End Sub
            End Class
        ]]></file>
    </compilation>, {SystemCoreRef})

            Dim expectedErrors1 = <errors><![CDATA[
BC36551: Extension methods can be defined only in modules.
                <Extension()> _
                 ~~~~~~~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36552ERR_ExtensionMethodNoParams()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ExtensionMethodNoParams">
        <file name="a.vb"><![CDATA[
            Imports System.Runtime.CompilerServices
            Module C1
                <Extension()> _
                Public Sub Print()
                End Sub
            End Module
        ]]></file>
    </compilation>, {SystemCoreRef})
            Dim expectedErrors1 = <errors><![CDATA[
BC36552: Extension methods must declare at least one parameter. The first parameter specifies which type to extend.
                Public Sub Print()
                           ~~~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36553ERR_ExtensionMethodOptionalFirstArg()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
   <compilation name="ExtensionMethodOptionalFirstArg">
       <file name="a.vb"><![CDATA[
            Imports System.Runtime.CompilerServices
            Module C1
                <Extension()> _
                Public Sub Print(Optional ByVal str As String = "hello")
                End Sub
            End Module
        ]]></file>
   </compilation>, {SystemCoreRef})
            Dim expectedErrors1 = <errors><![CDATA[
BC36553: 'Optional' cannot be applied to the first parameter of an extension method. The first parameter specifies which type to extend.
                Public Sub Print(Optional ByVal str As String = "hello")
                                                ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36554ERR_ExtensionMethodParamArrayFirstArg()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ExtensionMethodParamArrayFirstArg">
        <file name="a.vb"><![CDATA[
            Imports System.Runtime.CompilerServices
            Module C1
                <Extension()> _
                Public Sub Print(ByVal ParamArray str() As String)
                End Sub
            End Module
        ]]></file>
    </compilation>, {SystemCoreRef})
            Dim expectedErrors1 = <errors><![CDATA[
BC36554: 'ParamArray' cannot be applied to the first parameter of an extension method. The first parameter specifies which type to extend.
                Public Sub Print(ByVal ParamArray str() As String)
                                                  ~~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36561ERR_ExtensionMethodUncallable1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Interface I(Of T)
End Interface
Module M
    <Extension()>
    Sub M1(Of T, U As T)(o As T)
    End Sub
    <Extension()>
    Sub M2(Of T As I(Of U), U)(o As T)
    End Sub
    <Extension()>
    Sub M3(Of T, U As T)(o As U)
    End Sub
    <Extension()>
    Sub M4(Of T As I(Of U), U)(o As U)
    End Sub
End Module
]]></file>
</compilation>, {SystemCoreRef})
            Dim expectedErrors1 = <errors><![CDATA[
BC36561: Extension method 'M2' has type constraints that can never be satisfied.
    Sub M2(Of T As I(Of U), U)(o As T)
        ~~
BC36561: Extension method 'M3' has type constraints that can never be satisfied.
    Sub M3(Of T, U As T)(o As U)
        ~~
     ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36632ERR_NullableParameterMustSpecifyType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NullableParameterMustSpecifyType">
        <file name="a.vb"><![CDATA[
            Imports System

            Public Module M
                Delegate Sub Del(x?)

                Sub Main()
                    Dim x As Action(Of Integer?) = Sub(y?) Console.WriteLine()
                End Sub

                Sub goo(x?)
                End Sub

                Sub goo2(Of T)(x?)
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36632: Nullable parameters must specify a type.
                Delegate Sub Del(x?)
                                 ~~
BC36632: Nullable parameters must specify a type.
                    Dim x As Action(Of Integer?) = Sub(y?) Console.WriteLine()
                                                       ~~
BC36632: Nullable parameters must specify a type.
                Sub goo(x?)
                        ~~
BC36632: Nullable parameters must specify a type.
                Sub goo2(Of T)(x?)
                               ~~
     ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36634ERR_LambdasCannotHaveAttributes()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdasCannotHaveAttributes">
        <file name="a.vb"><![CDATA[
            Public Module M
                Sub LambdaAttribute()
                    Dim add1 = _
                    Function(<System.Runtime.InteropServices.InAttribute()> m As Integer) _
                                   m + 1
                End Sub
            End Module
        ]]></file>
    </compilation>)

            compilation1.VerifyDiagnostics(Diagnostic(ERRID.ERR_LambdasCannotHaveAttributes, "<System.Runtime.InteropServices.InAttribute()>"))

        End Sub

        <WorkItem(528712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528712")>
        <Fact()>
        Public Sub BC36643ERR_CantSpecifyParamsOnLambdaParamNoType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CantSpecifyParamsOnLambdaParamNoType">
        <file name="a.vb"><![CDATA[
            Imports System
            Public Module M 
                Sub Main()
                    Dim x As Action(Of String()) = Sub(y()) Console.WriteLine()
                End Sub
            End Module
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_CantSpecifyParamsOnLambdaParamNoType, "y()"))
        End Sub

        <Fact()>
        Public Sub BC36713ERR_AutoPropertyInitializedInStructure()
            CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AutoPropertyInitializedInStructure">
        <file name="a.vb"><![CDATA[
            Imports System.Collections.Generic 
            Structure S1(Of T As IEnumerable(Of T))
                Property AP() As New T
            End Structure
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_AutoPropertyInitializedInStructure, "AP"),
    Diagnostic(ERRID.ERR_NewIfNullOnGenericParam, "T"))
        End Sub

        <WorkItem(542471, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542471")>
        <Fact>
        Public Sub BC36713ERR_AutoPropertyInitializedInStructure_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AutoPropertyInitializedInStructure">
        <file name="a.vb"><![CDATA[
            Structure S1
                Public Property age1() As Integer = 10
                Public Shared Property age2() As Integer = 10
            End Structure

            Module M1
                Public Property age3() As Integer = 10
            End Module

            Class C1
                Public Property age4() As Integer = 10
                Public Shared Property age5() As Integer = 10
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36713: Auto-implemented Properties contained in Structures cannot have initializers unless they are marked 'Shared'.
                Public Property age1() As Integer = 10
                                ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540702, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540702")>
        <Fact>
        Public Sub BC36759ERR_AutoPropertyCantHaveParams()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AutoPropertyInitializedInStructure">
        <file name="a.vb"><![CDATA[
Imports System
Class A
    Public Property Item(index As Integer) As String
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36759: Auto-implemented properties cannot have parameters.
    Public Property Item(index As Integer) As String
                        ~~~~~~~~~~~~~~~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540702, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540702")>
        <Fact>
        Public Sub BC36759ERR_AutoPropertyCantHaveParams_MustOverride()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AutoPropertyInitializedInStructure">
        <file name="a.vb"><![CDATA[
Imports System
Class A
    Public MustOverride Property P(index As Integer) As T
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC31411: 'A' must be declared 'MustInherit' because it contains methods declared 'MustOverride'.
Class A
      ~
BC30002: Type 'T' is not defined.
    Public MustOverride Property P(index As Integer) As T
                                                        ~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540702, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540702")>
        <Fact>
        Public Sub BC36759ERR_AutoPropertyCantHaveParams_Default()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AutoPropertyInitializedInStructure">
        <file name="a.vb"><![CDATA[
Class C
    Default Public Property P(a As Integer) As T
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC30025: Property missing 'End Property'.
    Default Public Property P(a As Integer) As T
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.
    Default Public Property P(a As Integer) As T
                            ~
BC30002: Type 'T' is not defined.
    Default Public Property P(a As Integer) As T
                                               ~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36722ERR_VarianceDisallowedHere()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceDisallowedHere">
        <file name="a.vb"><![CDATA[
            Class C1
                Structure Z(Of T, In U, Out V)
                End Structure
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
                Structure Z(Of T, In U, Out V)
                                  ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
                Structure Z(Of T, In U, Out V)
                                        ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36723ERR_VarianceInterfaceNesting()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInterfaceNesting">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Class C : End Class
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
                Class C : End Class
                      ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36724ERR_VarianceOutParamDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutParamDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub GOO(ByVal x As R(Of Tout))
            End Interface
            Interface R(Of Out T) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36724: Type 'Tout' cannot be used in this context because 'Tout' is an 'Out' type parameter.
                Sub GOO(ByVal x As R(Of Tout))
                                   ~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36725ERR_VarianceInParamDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInParamDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As W(Of Tin))
            End Interface
            Interface W(Of In T) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.
                Sub goo(ByVal x As W(Of Tin))
                                   ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36726ERR_VarianceOutParamDisallowedForGeneric3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutParamDisallowedForGeneric3">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As RW(Of Tout, Tout))
            End Interface
            Interface RW(Of Out T1, In T2) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36726: Type 'Tout' cannot be used for the 'T1' in 'RW(Of T1, T2)' in this context because 'Tout' is an 'Out' type parameter.
                Sub goo(ByVal x As RW(Of Tout, Tout))
                                   ~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36727ERR_VarianceInParamDisallowedForGeneric3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInParamDisallowedForGeneric3">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As RW(Of Tin, Tin))
            End Interface
            Interface RW(Of Out T1, In T2) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36727: Type 'Tin' cannot be used for the 'T2' in 'RW(Of T1, T2)' in this context because 'Tin' is an 'In' type parameter.
                Sub goo(ByVal x As RW(Of Tin, Tin))
                                   ~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36728ERR_VarianceOutParamDisallowedHere2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutParamDisallowedHere2">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As R(Of R(Of Tout)))
            End Interface
            Interface R(Of Out T) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36728: Type 'Tout' cannot be used in 'R(Of Tout)' in this context because 'Tout' is an 'Out' type parameter.
                Sub goo(ByVal x As R(Of R(Of Tout)))
                                   ~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36729ERR_VarianceInParamDisallowedHere2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInParamDisallowedHere2">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As W(Of R(Of Tin)))
            End Interface
            Interface R(Of Out T) : End Interface
            Interface W(Of In T) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36729: Type 'Tin' cannot be used in 'R(Of Tin)' in this context because 'Tin' is an 'In' type parameter.
                Sub goo(ByVal x As W(Of R(Of Tin)))
                                   ~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36730ERR_VarianceOutParamDisallowedHereForGeneric4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutParamDisallowedHereForGeneric4">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As RW(Of RW(Of Tout, Tout), RW(Of Tout, Tin)))
            End Interface
            Interface RW(Of Out T1, In T2) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36730: Type 'Tout' cannot be used for the 'T1' of 'RW(Of T1, T2)' in 'RW(Of Tout, Tout)' in this context because 'Tout' is an 'Out' type parameter.
                Sub goo(ByVal x As RW(Of RW(Of Tout, Tout), RW(Of Tout, Tin)))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36731ERR_VarianceInParamDisallowedHereForGeneric4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInParamDisallowedHereForGeneric4">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As RW(Of RW(Of Tin, Tin), RW(Of Tout, Tin)))
            End Interface
            Interface RW(Of Out T1, In T2) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36731: Type 'Tin' cannot be used for the 'T2' of 'RW(Of T1, T2)' in 'RW(Of Tin, Tin)' in this context because 'Tin' is an 'In' type parameter.
                Sub goo(ByVal x As RW(Of RW(Of Tin, Tin), RW(Of Tout, Tin)))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36732ERR_VarianceTypeDisallowed2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceTypeDisallowed2">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Interface J : End Interface
                Sub goo(ByVal x As J)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36732: Type 'J' cannot be used in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
                Sub goo(ByVal x As J)
                                   ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36733ERR_VarianceTypeDisallowedForGeneric4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceTypeDisallowedForGeneric4">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Interface J : End Interface
                Sub goo(ByVal x As RW(Of J, Tout))
            End Interface
            Interface RW(Of Out T1, In T2) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36733: Type 'J' cannot be used for the 'T1' in 'RW(Of T1, T2)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
                Sub goo(ByVal x As RW(Of J, Tout))
                                   ~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36735ERR_VarianceTypeDisallowedHere3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceTypeDisallowedHere3">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Interface J : End Interface
                Sub goo(ByVal x As R(Of R(Of J)))
            End Interface
            Interface R(Of Out T) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36735: Type 'J' cannot be used in 'R(Of I(Of Tout, Tin, TSout, TSin).J)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
                Sub goo(ByVal x As R(Of R(Of J)))
                                   ~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36736ERR_VarianceTypeDisallowedHereForGeneric5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceTypeDisallowedHereForGeneric5">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Interface J : End Interface
                Sub goo(ByVal x As RW(Of RW(Of J, Tout), RW(Of Tout, Tin)))
            End Interface
            Interface RW(Of Out T1, In T2) : End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36736: Type 'J' cannot be used for the 'T1' of 'RW(Of T1, T2)' in 'RW(Of I(Of Tout, Tin, TSout, TSin).J, Tout)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
                Sub goo(ByVal x As RW(Of RW(Of J, Tout), RW(Of Tout, Tin)))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36738ERR_VariancePreventsSynthesizedEvents2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VariancePreventsSynthesizedEvents2">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Event e()
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36738: Event definitions with parameters are not allowed in an interface such as 'I(Of Tout, Tin, TSout, TSin)' that has 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which is not defined within 'I(Of Tout, Tin, TSout, TSin)'. For example, 'Event e As Action(Of ...)'.
                Event e()
                ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36740ERR_VarianceOutNullableDisallowed2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutNullableDisallowed2">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Function goo() As TSout?
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36740: Type 'TSout' cannot be used in 'TSout?' because 'In' and 'Out' type parameters cannot be made nullable, and 'TSout' is an 'Out' type parameter.
                Function goo() As TSout?
                                  ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36741ERR_VarianceInNullableDisallowed2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInNullableDisallowed2">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub goo(ByVal x As TSin?)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36741: Type 'TSin' cannot be used in 'TSin?' because 'In' and 'Out' type parameters cannot be made nullable, and 'TSin' is an 'In' type parameter.
                Sub goo(ByVal x As TSin?)
                                   ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36742ERR_VarianceOutByValDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutByValDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface IVariance(Of Out T)
                Sub Goo(ByVal a As T)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36742: Type 'T' cannot be used as a ByVal parameter type because 'T' is an 'Out' type parameter.
                Sub Goo(ByVal a As T)
                                   ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36743ERR_VarianceInReturnDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInReturnDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface IVariance(Of In T)
                Function Goo() As T
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36743: Type 'T' cannot be used as a return type because 'T' is an 'In' type parameter.
                Function Goo() As T
                                  ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36744ERR_VarianceOutConstraintDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutConstraintDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface IVariance(Of Out T)
                Function Goo(Of U As T)() As T
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36744: Type 'T' cannot be used as a generic type constraint because 'T' is an 'Out' type parameter.
                Function Goo(Of U As T)() As T
                                     ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36745ERR_VarianceInReadOnlyPropertyDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInReadOnlyPropertyDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                ReadOnly Property p() As Tin
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36745: Type 'Tin' cannot be used as a ReadOnly property type because 'Tin' is an 'In' type parameter.
                ReadOnly Property p() As Tin
                                         ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36746ERR_VarianceOutWriteOnlyPropertyDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutWriteOnlyPropertyDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                WriteOnly Property p() As Tout
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36746: Type 'Tout' cannot be used as a WriteOnly property type because 'Tout' is an 'Out' type parameter.
                WriteOnly Property p() As Tout
                                          ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36747ERR_VarianceOutPropertyDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutPropertyDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Property p() As Tout
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36747: Type 'Tout' cannot be used as a property type in this context because 'Tout' is an 'Out' type parameter and the property is not marked ReadOnly.
                Property p() As Tout
                                ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36748ERR_VarianceInPropertyDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInPropertyDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Property p() As Tin
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36748: Type 'Tin' cannot be used as a property type in this context because 'Tin' is an 'In' type parameter and the property is not marked WriteOnly.
                Property p() As Tin
                                ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36749ERR_VarianceOutByRefDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceOutByRefDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub f(ByRef x As Tout)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36749: Type 'Tout' cannot be used in this context because 'In' and 'Out' type parameters cannot be used for ByRef parameter types, and 'Tout' is an 'Out' type parameter.
                Sub f(ByRef x As Tout)
                                 ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36750ERR_VarianceInByRefDisallowed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VarianceInByRefDisallowed1">
        <file name="a.vb"><![CDATA[
            Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
                Sub f(ByRef x As Tin)
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36750: Type 'Tin' cannot be used in this context because 'In' and 'Out' type parameters cannot be used for ByRef parameter types, and 'Tin' is an 'In' type parameter.
                Sub f(ByRef x As Tin)
                                 ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36917ERR_OverloadsModifierInModule()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OverloadsModifierInModule">
        <file name="a.vb"><![CDATA[
            Module M1
                Overloads Function goo(x as integer) as double
                    return nothing
                End Function
                Overloads Function goo(ByVal x As Long) As Double
                    Return Nothing
                End Function
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC36917: Inappropriate use of 'Overloads' keyword in a module.
                Overloads Function goo(x as integer) as double
                ~~~~~~~~~
BC36917: Inappropriate use of 'Overloads' keyword in a module.
                Overloads Function goo(ByVal x As Long) As Double
                ~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Old name"MethodErrorsOverloadsInModule"
        <Fact>
        Public Sub BC36917ERR_OverloadsModifierInModule_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Overloads Sub S()
    End Sub
    Overloads Property P
End Module
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC36917: Inappropriate use of 'Overloads' keyword in a module.
    Overloads Sub S()
    ~~~~~~~~~
BC36917: Inappropriate use of 'Overloads' keyword in a module.
    Overloads Property P
    ~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

#End Region

#Region "Targeted Warning Tests"

        <Fact>
        Public Sub BC40000WRN_UseOfObsoleteSymbol2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UseOfObsoleteSymbol2">
        <file name="a.vb"><![CDATA[
            Imports System.Net
            Module Module1
                Sub Main()
                    Dim RemoteEndPoint As EndPoint = Nothing
                    Dim x As Long
                    x = CType(CType(RemoteEndPoint, IPEndPoint).Address.Address Mod 256, Byte)
                End Sub
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40000: 'Public Overloads Property Address As Long' is obsolete: 'This property has been deprecated. It is address family dependent. Please use IPAddress.Equals method to perform comparisons. http://go.microsoft.com/fwlink/?linkid=14202'.
                    x = CType(CType(RemoteEndPoint, IPEndPoint).Address.Address Mod 256, Byte)
                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40003WRN_MustOverloadBase4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustOverloadBase4">
        <file name="a.vb"><![CDATA[
            Interface I
                Sub S()
            End Interface
            Class C1
                Implements I
                Public Sub S() Implements I.S
                End Sub
            End Class
            Class C2
                Inherits C1
                Public Sub S()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40003: sub 'S' shadows an overloadable member declared in the base class 'C1'.  If you want to overload the base method, this method must be declared 'Overloads'.
                Public Sub S()
                           ~
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40004WRN_OverrideType5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="OverrideType5">
        <file name="a.vb"><![CDATA[
            Public Class Base
                Public i As Integer = 10
            End Class
            Public Class C1
                Inherits Base
                Public i As String = "hi"
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40004: variable 'i' conflicts with variable 'i' in the base class 'Base' and should be declared 'Shadows'.
                Public i As String = "hi"
                       ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40005WRN_MustOverride2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustOverride2">
        <file name="a.vb"><![CDATA[
            Class C1
                Overridable ReadOnly Property Goo(ByVal x As Integer) As Integer
                    Get
                        Return 1
                    End Get
                End Property
            End Class
            Class C2
                Inherits C1
                ReadOnly Property Goo(ByVal x As Integer) As Integer
                    Get
                        Return 1
                    End Get
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40005: property 'Goo' shadows an overridable method in the base class 'C1'. To override the base method, this method must be declared 'Overrides'.
                ReadOnly Property Goo(ByVal x As Integer) As Integer
                                  ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(543734, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543734")>
        <WorkItem(561748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/561748")>
        <Fact>
        Public Sub BC40007WRN_DefaultnessShadowed4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Namespace N1
    Class base
        Default Public ReadOnly Property Item(ByVal i As Integer) As Integer
            Get
                Return i
            End Get
        End Property
    End Class
    Class base2
        Inherits base
    End Class
    Class derive
        Inherits base
        Default Public Overloads ReadOnly Property Item1(ByVal i As Integer) As Integer
            Get
                Return 2 * i
            End Get
        End Property
    End Class
    Class derive2
        Inherits base2
        Default Public Overloads ReadOnly Property Item3(ByVal i As Integer) As Integer ' No warning in Dev11
            Get
                Return 2 * i
            End Get
        End Property
    End Class
End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40007: Default property 'Item1' conflicts with the default property 'Item' in the base class 'base'. 'Item1' will be the default property. 'Item1' should be declared 'Shadows'.
        Default Public Overloads ReadOnly Property Item1(ByVal i As Integer) As Integer
                                                   ~~~~~
BC40007: Default property 'Item3' conflicts with the default property 'Item' in the base class 'base'. 'Item3' will be the default property. 'Item3' should be declared 'Shadows'.
        Default Public Overloads ReadOnly Property Item3(ByVal i As Integer) As Integer ' No warning in Dev11
                                                   ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact, WorkItem(546773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546773")>
        Public Sub BC40007WRN_DefaultnessShadowed4_NoErrors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Class Base
    Default Public ReadOnly Property TeamName(ByVal index As Integer) As String
        Get
            Return ""
        End Get
    End Property
End Class
Class Derived
    Inherits Base
    Default Public Shadows ReadOnly Property TeamProject(ByVal index As Integer) As String
        Get
            Return ""
        End Get
    End Property
End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, <errors><![CDATA[]]></errors>)
        End Sub

        <Fact, WorkItem(546773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546773")>
        Public Sub BC40007WRN_DefaultnessShadowed4_TwoErrors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Class Base
    Default Public ReadOnly Property TeamName(ByVal index As Integer) As String
        Get
            Return ""
        End Get
    End Property
End Class
Class Derived
    Inherits Base
    Default Public ReadOnly Property TeamProject(ByVal index As Integer) As String
        Get
            Return ""
        End Get
    End Property
    Default Public ReadOnly Property TeamProject(ByVal index As String) As String
        Get
            Return ""
        End Get
    End Property
End Class
]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC40007: Default property 'TeamProject' conflicts with the default property 'TeamName' in the base class 'Base'. 'TeamProject' will be the default property. 'TeamProject' should be declared 'Shadows'.
    Default Public ReadOnly Property TeamProject(ByVal index As Integer) As String
                                     ~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact, WorkItem(546773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546773")>
        Public Sub BC40007WRN_DefaultnessShadowed4_MixedErrors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Class Base
    Default Public ReadOnly Property TeamName(ByVal index As Integer) As String
        Get
            Return ""
        End Get
    End Property
End Class
Class Derived
    Inherits Base
    Default Public Shadows ReadOnly Property TeamProject(ByVal index As Integer) As String
        Get
            Return ""
        End Get
    End Property
    Default Public ReadOnly Property TeamProject(ByVal index As String) As String
        Get
            Return ""
        End Get
    End Property
End Class
]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC30695: property 'TeamProject' must be declared 'Shadows' because another member with this name is declared 'Shadows'.
    Default Public ReadOnly Property TeamProject(ByVal index As String) As String
                                     ~~~~~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(543734, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543734")>
        <Fact>
        Public Sub BC40007WRN_DefaultnessShadowed4_DifferentCasing()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Imports System
            Namespace N1
                Class base
                    Default Public ReadOnly Property Item(ByVal i As Integer) As Integer
                        Get
                            Return i
                        End Get
                    End Property
                End Class
                Class derive
                    Inherits base
                    Default Public Overloads ReadOnly Property item(ByVal i As Integer, j as Integer) As Integer
                        Get
                            Return 2 * i
                        End Get
                    End Property
                End Class
            End Namespace
        ]]></file>
    </compilation>)

            ' Differs only by case, shouldn't have errors.
            Dim expectedErrors1 = <errors><![CDATA[
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(543734, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543734")>
        <WorkItem(561748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/561748")>
        <Fact>
        Public Sub BC40007WRN_DefaultnessShadowed4_Interface()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface base1
End Interface
Interface base2
    Default ReadOnly Property Item2(ByVal i As Integer) As Integer
End Interface
Interface base3
    Inherits base2
End Interface
Interface derive1
    Inherits base1, base2
    Default ReadOnly Property Item1(ByVal i As Integer) As Integer
End Interface
Interface derive2
    Inherits base3
    Default ReadOnly Property Item3(ByVal i As Integer) As Integer ' No warning in Dev11
End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40007: Default property 'Item1' conflicts with the default property 'Item2' in the base interface 'base2'. 'Item1' will be the default property. 'Item1' should be declared 'Shadows'.
    Default ReadOnly Property Item1(ByVal i As Integer) As Integer
                              ~~~~~
BC40007: Default property 'Item3' conflicts with the default property 'Item2' in the base interface 'base2'. 'Item3' will be the default property. 'Item3' should be declared 'Shadows'.
    Default ReadOnly Property Item3(ByVal i As Integer) As Integer ' No warning in Dev11
                              ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC40011WRN_ComClassNoMembers1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ComClassNoMembers1">
        <file name="a.vb"><![CDATA[
            <Microsoft.VisualBasic.ComClassAttribute()>
            Public Class C1
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40011: 'Microsoft.VisualBasic.ComClassAttribute' is specified for class 'C1' but 'C1' has no public members that can be exposed to COM; therefore, no COM interfaces are generated.
            Public Class C1
                         ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40012WRN_SynthMemberShadowsMember5_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        MustInherit Class A
            Public Sub get_P()
            End Sub
            Public Sub set_P()
            End Sub
            Public Function get_Q()
                Return Nothing
            End Function
            Public Sub set_Q()
            End Sub
            Public Function get_R(value)
                Return Nothing
            End Function
            Public Sub set_R(value)
            End Sub
            Public Function get_S()
                Return Nothing
            End Function
            Public Function set_S()
                Return Nothing
            End Function
            Public get_T
            Public Interface set_T
            End Interface
            Public Enum get_U
                A
            End Enum
            Public Structure set_U
            End Structure
        End Class
        MustInherit Class B
            Inherits A
            Public Property P
            Public ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            Friend WriteOnly Property R
                Set(value)
                End Set
            End Property
            Protected MustOverride Property S
            Public Property T
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Public Property U
        End Class
        MustInherit Class C
            Inherits A
            Public Shadows Property P
            Public Shadows ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            Friend Shadows WriteOnly Property R
                Set(value)
                End Set
            End Property
            Protected MustOverride Shadows Property S
            Public Shadows Property T
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Public Shadows Property U
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40012: property 'P' implicitly declares 'get_P', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property P
                            ~
BC40012: property 'P' implicitly declares 'set_P', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property P
                            ~
BC40012: property 'Q' implicitly declares 'get_Q', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public ReadOnly Property Q
                                     ~
BC40012: property 'R' implicitly declares 'set_R', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Friend WriteOnly Property R
                                      ~
BC40012: property 'S' implicitly declares 'get_S', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Protected MustOverride Property S
                                            ~
BC40012: property 'S' implicitly declares 'set_S', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Protected MustOverride Property S
                                            ~
BC40012: property 'T' implicitly declares 'get_T', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property T
                            ~
BC40012: property 'T' implicitly declares 'set_T', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property T
                            ~
BC40012: property 'U' implicitly declares 'get_U', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property U
                            ~
BC40012: property 'U' implicitly declares 'set_U', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property U
                            ~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(541355, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541355")>
        <Fact>
        Public Sub BC40012WRN_SynthMemberShadowsMember5_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        MustInherit Class A
            Private _P1
            Protected _P2
            Friend _P3
            Public Sub _Q1()
            End Sub
            Public Interface _Q2
            End Interface
            Public Enum _Q3
                A
            End Enum
            Public _R1
            Public _R2
            Public _R3
            Public _R4
        End Class
        MustInherit Class B
            Inherits A
            Friend Property P1
            Protected Property P2
            Private Property P3
            Public Property Q1
            Public Property Q2
            Public Property Q3
            Public ReadOnly Property R1
                Get
                    Return Nothing
                End Get
            End Property
            Public WriteOnly Property R2
                Set(value)
                End Set
            End Property
            Public MustOverride Property R3
            Public Property R4
        End Class
        MustInherit Class C
            Inherits A
            Friend Shadows Property P1
            Protected Shadows Property P2
            Private Shadows Property P3
            Public Shadows Property Q1
            Public Shadows Property Q2
            Public Shadows Property Q3
            Public Shadows ReadOnly Property R1
                Get
                    Return Nothing
                End Get
            End Property
            Public Shadows WriteOnly Property R2
                Set(value)
                End Set
            End Property
            Public MustOverride Shadows Property R3
            Public Shadows Property R4
        End Class
        MustInherit Class D
            Friend Property P1
            Protected Property P2
            Private Property P3
            Public Property Q1
            Public Property Q2
            Public Property Q3
            Public ReadOnly Property R1
                Get
                    Return Nothing
                End Get
            End Property
            Public WriteOnly Property R2
                Set(value)
                End Set
            End Property
            Public MustOverride Property R3
        End Class
        MustInherit Class E
            Inherits D
            Private _P1
            Protected _P2
            Friend _P3
            Public Sub _Q1()
            End Sub
            Public Interface _Q2
            End Interface
            Public Enum _Q3
                A
            End Enum
            Public _R1
            Public _R2
            Public _R3
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40012: property 'P2' implicitly declares '_P2', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Protected Property P2
                               ~~
BC40012: property 'P3' implicitly declares '_P3', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Private Property P3
                             ~~
BC40012: property 'Q1' implicitly declares '_Q1', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property Q1
                            ~~
BC40012: property 'Q2' implicitly declares '_Q2', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property Q2
                            ~~
BC40012: property 'Q3' implicitly declares '_Q3', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property Q3
                            ~~
BC40012: property 'R4' implicitly declares '_R4', which conflicts with a member in the base class 'A', and so the property should be declared 'Shadows'.
            Public Property R4
                            ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(539827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539827")>
        <Fact>
        Public Sub BC40012WRN_SynthMemberShadowsMember5_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IA
    Function get_Goo() As String
End Interface
Interface IB
    Inherits IA
    ReadOnly Property Goo() As Integer
End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 =
                <errors><![CDATA[
BC40012: property 'Goo' implicitly declares 'get_Goo', which conflicts with a member in the base interface 'IA', and so the property should be declared 'Shadows'.
    ReadOnly Property Goo() As Integer
                      ~~~
                ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC40012WRN_SynthMemberShadowsMember5_4()
            CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class A
            Public add_E
            Public remove_E
            Public EEvent
            Public EEventHandler
        End Class
        Class B
            Inherits A
            Public Event E()
        End Class
        Class C
            Inherits A
            Public Shadows Event E()
        End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.WRN_SynthMemberShadowsMember5, "E").WithArguments("event", "E", "EEventHandler", "class", "A"),
    Diagnostic(ERRID.WRN_SynthMemberShadowsMember5, "E").WithArguments("event", "E", "EEvent", "class", "A"),
    Diagnostic(ERRID.WRN_SynthMemberShadowsMember5, "E").WithArguments("event", "E", "add_E", "class", "A"),
    Diagnostic(ERRID.WRN_SynthMemberShadowsMember5, "E").WithArguments("event", "E", "remove_E", "class", "A"))

        End Sub

        <Fact>
        Public Sub BC40014WRN_MemberShadowsSynthMember6()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        MustInherit Class A
            Public Property P
            Public ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            Friend WriteOnly Property R
                Set(value)
                End Set
            End Property
            Protected MustOverride Property S
        End Class
        MustInherit Class B
            Inherits A
            Public Sub get_P()
            End Sub
            Public Sub set_P()
            End Sub
            Public Function get_Q()
                Return Nothing
            End Function
            Public Sub set_Q()
            End Sub
            Public Function get_R(value)
                Return Nothing
            End Function
            Public Sub set_R(value)
            End Sub
            Public Function get_S()
                Return Nothing
            End Function
            Public Function set_S()
                Return Nothing
            End Function
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40014: sub 'get_P' conflicts with a member implicitly declared for property 'P' in the base class 'A' and should be declared 'Shadows'.
            Public Sub get_P()
                       ~~~~~
BC40014: sub 'set_P' conflicts with a member implicitly declared for property 'P' in the base class 'A' and should be declared 'Shadows'.
            Public Sub set_P()
                       ~~~~~
BC40014: function 'get_Q' conflicts with a member implicitly declared for property 'Q' in the base class 'A' and should be declared 'Shadows'.
            Public Function get_Q()
                            ~~~~~
BC40014: sub 'set_R' conflicts with a member implicitly declared for property 'R' in the base class 'A' and should be declared 'Shadows'.
            Public Sub set_R(value)
                       ~~~~~
BC40014: function 'get_S' conflicts with a member implicitly declared for property 'S' in the base class 'A' and should be declared 'Shadows'.
            Public Function get_S()
                            ~~~~~
BC40014: function 'set_S' conflicts with a member implicitly declared for property 'S' in the base class 'A' and should be declared 'Shadows'.
            Public Function set_S()
                            ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40019WRN_UseOfObsoletePropertyAccessor3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UseOfObsoletePropertyAccessor3">
        <file name="a.vb"><![CDATA[
            Imports System
            Class C1
                ReadOnly Property p As String
                    <Obsolete("hello", False)>
                    Get
                        Return "hello"
                    End Get
                End Property
            End Class
            Class C2
                Sub goo()
                    Dim s As C1 = New C1()
                    Dim a = s.p
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40019: 'Get' accessor of 'Public ReadOnly Property p As String' is obsolete: 'hello'.
                    Dim a = s.p
                            ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40025WRN_FieldNotCLSCompliant1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="FieldNotCLSCompliant1">
        <file name="a.vb"><![CDATA[
            Imports System
            <Assembly: CLSCompliant(True)> 
<CLSCompliant(True)> Public Class GenCompClass(Of T)
    <CLSCompliant(False)> Public Structure NonCompStruct
        Public a As UInteger
            End Structure
            <CLSCompliant(False)> Class cls1
            End Class
        End Class
        Public Class C(Of t)
            Inherits GenCompClass(Of String)
            Public x As GenCompClass(Of String).cls1
            Public y As GenCompClass(Of String).NonCompStruct
        End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40025: Type of member 'x' is not CLS-compliant.
            Public x As GenCompClass(Of String).cls1
                   ~
BC40025: Type of member 'y' is not CLS-compliant.
            Public y As GenCompClass(Of String).NonCompStruct
                   ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40026WRN_BaseClassNotCLSCompliant2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BaseClassNotCLSCompliant2">
        <file name="a.vb"><![CDATA[
            Imports System
            <CLSCompliant(True)>
            Public Class MyCompliantClass
                Inherits BASE
            End Class
            Public Class BASE
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40026: 'MyCompliantClass' is not CLS-compliant because it derives from 'BASE', which is not CLS-compliant.
            Public Class MyCompliantClass
                         ~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40027WRN_ProcTypeNotCLSCompliant1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ProcTypeNotCLSCompliant1">
        <file name="a.vb"><![CDATA[
            Imports System
            <CLSCompliant(True)>
            Public Class MyCompliantClass
                Public Function ChangeValue() As UInt32
                    Return Nothing
                End Function
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40027: Return type of function 'ChangeValue' is not CLS-compliant.
                Public Function ChangeValue() As UInt32
                                ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40028WRN_ParamNotCLSCompliant1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ParamNotCLSCompliant1">
        <file name="a.vb"><![CDATA[
            Imports System
            <CLSCompliant(True)>
            Public Class MyCompliantClass
                Public Sub ChangeValue(ByVal value As UInt32)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40028: Type of parameter 'value' is not CLS-compliant.
                Public Sub ChangeValue(ByVal value As UInt32)
                                             ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40029WRN_InheritedInterfaceNotCLSCompliant2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InheritedInterfaceNotCLSCompliant2">
        <file name="a.vb"><![CDATA[
            Imports System
            <assembly: clscompliant(true)> 
            <CLSCompliant(False)> Public Interface i1
            End Interface
            Public Interface i2
                Inherits i1
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40029: 'i2' is not CLS-compliant because the interface 'i1' it inherits from is not CLS-compliant.
            Public Interface i2
                             ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40030WRN_CLSMemberInNonCLSType3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CLSMemberInNonCLSType3">
        <file name="a.vb"><![CDATA[
            Imports System
            Public Module M1
                <CLSCompliant(True)> Class C1
                End Class
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40030: class 'M1.C1' cannot be marked CLS-compliant because its containing type 'M1' is not CLS-compliant.
                <CLSCompliant(True)> Class C1
                                           ~~

                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40031WRN_NameNotCLSCompliant1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NameNotCLSCompliant1">
        <file name="a.vb"><![CDATA[
            Imports System
            <Assembly: CLSCompliant(True)> 
            Namespace _NS
            End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40031: Name '_NS' is not CLS-compliant.
            Namespace _NS
                      ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40032WRN_EnumUnderlyingTypeNotCLS1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EnumUnderlyingTypeNotCLS1">
        <file name="a.vb"><![CDATA[
            <Assembly: System.CLSCompliant(True)>
            Public Class c1
                Public Enum COLORS As UInteger
                    RED
                    GREEN
                    BLUE
                End Enum
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40032: Underlying type 'UInteger' of Enum is not CLS-compliant.
                Public Enum COLORS As UInteger
                            ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40033WRN_NonCLSMemberInCLSInterface1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NonCLSMemberInCLSInterface1">
        <file name="a.vb"><![CDATA[
            Imports System
            <CLSCompliant(True)> Public Interface IFace
                <CLSCompliant(False)> Property Prop1() As Long
                <CLSCompliant(False)> Function F2() As Integer
                <CLSCompliant(False)> Event EV3(ByVal i3 As Integer)
                <CLSCompliant(False)> Sub Sub4()
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40033: Non CLS-compliant 'Property Prop1 As Long' is not allowed in a CLS-compliant interface.
                <CLSCompliant(False)> Property Prop1() As Long
                                               ~~~~~
BC40033: Non CLS-compliant 'Function F2() As Integer' is not allowed in a CLS-compliant interface.
                <CLSCompliant(False)> Function F2() As Integer
                                               ~~
BC40033: Non CLS-compliant 'Event EV3(i3 As Integer)' is not allowed in a CLS-compliant interface.
                <CLSCompliant(False)> Event EV3(ByVal i3 As Integer)
                                            ~~~
BC40033: Non CLS-compliant 'Sub Sub4()' is not allowed in a CLS-compliant interface.
                <CLSCompliant(False)> Sub Sub4()
                                          ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40034WRN_NonCLSMustOverrideInCLSType1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NonCLSMustOverrideInCLSType1">
        <file name="a.vb"><![CDATA[
            Imports System
            <CLSCompliant(True)> Public MustInherit Class QuiteCompliant
                <CLSCompliant(False)> Public MustOverride Sub Sub1()
                <CLSCompliant(False)> Protected MustOverride Function Fun2() As Integer
                <CLSCompliant(False)> Protected Friend MustOverride Sub Sub3()
                <CLSCompliant(False)> Friend MustOverride Function Fun4(ByVal x As Long) As Long
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'QuiteCompliant'.
                <CLSCompliant(False)> Public MustOverride Sub Sub1()
                                                              ~~~~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'QuiteCompliant'.
                <CLSCompliant(False)> Protected MustOverride Function Fun2() As Integer
                                                                      ~~~~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'QuiteCompliant'.
                <CLSCompliant(False)> Protected Friend MustOverride Sub Sub3()
                                                                        ~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40035WRN_ArrayOverloadsNonCLS2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ArrayOverloadsNonCLS2">
        <file name="a.vb"><![CDATA[
            Imports System
            <CLSCompliant(True)>
            Public MustInherit Class QuiteCompliant
                Public Sub goo(Of t)(ByVal p1()()() As Integer)
                End Sub
                Public Sub goo(Of t)(ByVal p1()()()()() As Integer)
                End Sub
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40035: 'Public Sub goo(Of t)(p1 As Integer()()()()())' is not CLS-compliant because it overloads 'Public Sub goo(Of t)(p1 As Integer()()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
                Public Sub goo(Of t)(ByVal p1()()()()() As Integer)
                           ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40038WRN_RootNamespaceNotCLSCompliant1()
            Dim opt = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("_CLS")
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RootNamespaceNotCLSCompliant1">
        <file name="a.vb"><![CDATA[
            Imports System
            <Assembly: CLSCompliant(True)> 
            Module M1
            End Module
        ]]></file>
    </compilation>, opt)
            Dim expectedErrors1 = <errors><![CDATA[
BC40038: Root namespace '_CLS' is not CLS-compliant.
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40039WRN_RootNamespaceNotCLSCompliant2()
            Dim opt = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("A._B")
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RootNamespaceNotCLSCompliant2">
        <file name="a.vb"><![CDATA[
            Imports System
            <Assembly: CLSCompliant(True)> 
            Module M1
            End Module
        ]]></file>
    </compilation>, opt)
            Dim expectedErrors1 = <errors><![CDATA[
BC40039: Name '_B' in the root namespace 'A._B' is not CLS-compliant.
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40041WRN_TypeNotCLSCompliant1()
            Dim opt = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeNotCLSCompliant1">
        <file name="a.vb"><![CDATA[
            Imports System
            <Assembly: CLSCompliant(True)> 
            <CLSCompliant(True)> Public Class GenCompClass(Of T)
            End Class
            Public Class C(Of t)
                Inherits GenCompClass(Of UInteger)
            End Class
        ]]></file>
    </compilation>, options:=opt)
            Dim expectedErrors1 = <errors><![CDATA[
BC40041: Type 'UInteger' is not CLS-compliant.
            Public Class C(Of t)
                         ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40042WRN_OptionalValueNotCLSCompliant1()
            Dim opt = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OptionalValueNotCLSCompliant1">
        <file name="a.vb"><![CDATA[
            Imports System
            <Assembly: CLSCompliant(True)> 
            Public Module M1
                Public Sub goo(Of t)(Optional ByVal p1 As Object = 3UI)
                End Sub
            End Module
        ]]></file>
    </compilation>, opt)
            Dim expectedErrors1 = <errors><![CDATA[
BC40042: Type of optional value for optional parameter 'p1' is not CLS-compliant.
                Public Sub goo(Of t)(Optional ByVal p1 As Object = 3UI)
                                                    ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40043WRN_CLSAttrInvalidOnGetSet()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CLSAttrInvalidOnGetSet">
        <file name="a.vb"><![CDATA[
            Imports System
            Class C1
                Property PROP As String
                    <CLSCompliant(True)>
                    Get
                        Return Nothing
                    End Get
                    Set(ByVal value As String)
                    End Set
                End Property
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
                    <CLSCompliant(True)>
                     ~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40046WRN_TypeConflictButMerged6()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
            Interface ii
            End Interface
            Structure teststruct
                Implements ii
            End Structure
            Partial Structure teststruct
            End Structure
            Structure teststruct
                Dim a As String
            End Structure
            Partial Interface ii
            End Interface
            Interface ii ' 3
            End Interface

            Module m
            End Module
            Partial Module m
            End Module
            Module m ' 3
            End Module
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40046: interface 'ii' and partial interface 'ii' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
Interface ii
          ~~
BC40046: structure 'teststruct' and partial structure 'teststruct' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
            Structure teststruct
                      ~~~~~~~~~~
BC40046: structure 'teststruct' and partial structure 'teststruct' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
            Structure teststruct
                      ~~~~~~~~~~
BC40046: interface 'ii' and partial interface 'ii' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
            Interface ii ' 3
                      ~~
BC40046: module 'm' and partial module 'm' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
            Module m
                   ~
BC40046: module 'm' and partial module 'm' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
            Module m ' 3
                   ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40046WRN_TypeConflictButMerged6_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Namespace N
            Partial Class C
                Private F
            End Class
            Class C ' Warning 1
                Private G
            End Class
            Class C ' Warning 2
                Private H
            End Class
        End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40046: class 'C' and partial class 'C' conflict in namespace 'N', but are being merged because one of them is declared partial.
            Class C ' Warning 1
                  ~
BC40046: class 'C' and partial class 'C' conflict in namespace 'N', but are being merged because one of them is declared partial.
            Class C ' Warning 2
                  ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40048WRN_ShadowingGenericParamWithParam1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ShadowingGenericParamWithParam1">
        <file name="a.vb"><![CDATA[
            Interface I1(Of V)
                Class C1(Of V)
                End Class
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40048: Type parameter 'V' has the same name as a type parameter of an enclosing type. Enclosing type's type parameter will be shadowed.
                Class C1(Of V)
                            ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(543528, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543528")>
        Public Sub BC40048WRN_ShadowingGenericParamWithParam1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ShadowingGenericParamWithParam1">
        <file name="a.vb"><![CDATA[
            Class base(Of T)
                Function TEST(Of T)(ByRef X As T)
                    Return Nothing
                End Function
            End Class
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.WRN_ShadowingGenericParamWithParam1, "T").WithArguments("T"))

        End Sub

        <Fact>
        Public Sub BC40050WRN_EventDelegateTypeNotCLSCompliant2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EventDelegateTypeNotCLSCompliant2">
        <file name="a.vb"><![CDATA[
            Imports System
            <Assembly: CLSCompliant(True)> 
            Public Class ETester
                <CLSCompliant(False)> Public Delegate Sub abc(ByVal n As Integer)
                Public Custom Event E As abc
                    AddHandler(ByVal Value As abc)
                    End AddHandler
                    RemoveHandler(ByVal Value As abc)
                    End RemoveHandler
                    RaiseEvent(ByVal n As Integer)
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40050: Delegate type 'ETester.abc' of event 'E' is not CLS-compliant.
                Public Custom Event E As abc
                                    ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40053WRN_CLSEventMethodInNonCLSType3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CLSEventMethodInNonCLSType3">
        <file name="a.vb"><![CDATA[
            Imports System
            <CLSCompliant(False)> _
            Public Class cls1
                Delegate Sub del1()
                Custom Event e1 As del1
                    AddHandler(ByVal value As del1)
                    End AddHandler
                    <CLSCompliant(True)> _
                    RemoveHandler(ByVal value As del1)
                    End RemoveHandler
                    RaiseEvent()
                    End RaiseEvent
                End Event
            End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40053: 'RemoveHandler' method for event 'e1' cannot be marked CLS compliant because its containing type 'cls1' is not CLS compliant.
                    <CLSCompliant(True)> _
                     ~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(539496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539496")>
        <Fact>
        Public Sub BC40055WRN_NamespaceCaseMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NamespaceCaseMismatch3">
        <file name="a.vb"><![CDATA[
            Namespace ns1
            End Namespace

            Namespace Ns1
            End Namespace
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC40055: Casing of namespace name 'ns1' does not match casing of namespace name 'Ns1' in 'a.vb'.
Namespace ns1
          ~~~
]]></errors>)

            compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NamespaceCaseMismatch3">
        <file name="a.vb"><![CDATA[
                        Namespace ns1
                        End Namespace
                    ]]></file>

        <file name="b.vb"><![CDATA[
                        Namespace Ns1
                        End Namespace
                    ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC40055: Casing of namespace name 'ns1' does not match casing of namespace name 'Ns1' in 'b.vb'.
Namespace ns1
          ~~~
]]></errors>)


            compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NamespaceCaseMismatch3">
        <file name="a.vb"><![CDATA[
                        Namespace Ns
                        End Namespace

                        Namespace ns.AB
                        End Namespace

                    ]]></file>

        <file name="b.vb"><![CDATA[
                        Namespace NS.Ab
                        End Namespace

                        Namespace Ns
                            Namespace Ab
                            End Namespace
                        End Namespace
                    ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<errors><![CDATA[
BC40055: Casing of namespace name 'Ns' does not match casing of namespace name 'NS' in 'b.vb'.
Namespace Ns
          ~~
BC40055: Casing of namespace name 'Ab' does not match casing of namespace name 'AB' in 'a.vb'.
Namespace NS.Ab
             ~~
]]></errors>)
        End Sub

        <WorkItem(545727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545727")>
        <Fact()>
        Public Sub BC40055_WRN_NamespaceCaseMismatch3_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
                                    Namespace Global
                                        Namespace CONSOLEAPPLICATIONVB
                                            Class H
                                            End Class
                                        End Namespace
                                    End Namespace
                                                        ]]></file>
                </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:="ConsoleApplicationVB"))
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC40055: Casing of namespace name 'CONSOLEAPPLICATIONVB' does not match casing of namespace name 'ConsoleApplicationVB' in '<project settings>'.
                                        Namespace CONSOLEAPPLICATIONVB
                                                  ~~~~~~~~~~~~~~~~~~~~
]]></expected>)

            ' different casing to see that best name has no influence on error
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
                                    Namespace Global
                                        Namespace consoleapplicationvb
                                            Class H
                                            End Class
                                        End Namespace
                                    End Namespace
                                                        ]]></file>
                </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:="CONSOLEAPPLICATIONVB"))
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC40055: Casing of namespace name 'consoleapplicationvb' does not match casing of namespace name 'CONSOLEAPPLICATIONVB' in '<project settings>'.
                                        Namespace consoleapplicationvb
                                                  ~~~~~~~~~~~~~~~~~~~~
]]></expected>)

            ' passing nested ns to rootnamespace
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                            <compilation>
                                <file name="a.vb"><![CDATA[
                        Namespace Global
                            Namespace GOO
                                Namespace BAR
                                    Class H
                                    End Class
                                End Namespace    
                            End Namespace
                        End Namespace
                                            ]]></file>
                            </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:="GOO.bar"))
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC40055: Casing of namespace name 'BAR' does not match casing of namespace name 'bar' in '<project settings>'.
                                Namespace BAR
                                          ~~~
]]></expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                            <compilation>
                                <file name="a.vb"><![CDATA[
                        Namespace Global
                            Namespace GOO
                                Namespace bar
                                    Class H
                                    End Class
                                End Namespace    
                            End Namespace
                        End Namespace
                                            ]]></file>
                            </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:="GOO.BAR"))
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC40055: Casing of namespace name 'bar' does not match casing of namespace name 'BAR' in '<project settings>'.
                                Namespace bar
                                          ~~~
]]></expected>)

            ' passing nested ns to rootnamespace
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                            <compilation>
                                <file name="a.vb"><![CDATA[
Namespace Global
    Namespace goo
        Namespace BAR
            Class H
            End Class
        End Namespace    
    End Namespace
End Namespace
                    ]]></file>
                            </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:="GOO.BAR"))
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC40055: Casing of namespace name 'goo' does not match casing of namespace name 'GOO' in '<project settings>'.
    Namespace goo
              ~~~
]]></expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                            <compilation>
                                <file name="a.vb"><![CDATA[
Namespace Global
    Namespace GOO
        Namespace BAR
            Class H
            End Class
        End Namespace    
    End Namespace
End Namespace
                    ]]></file>
                            </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:="goo.BAR"))
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC40055: Casing of namespace name 'GOO' does not match casing of namespace name 'goo' in '<project settings>'.
    Namespace GOO
              ~~~
]]></expected>)

            ' no warnings if spelling is correct
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Namespace Global
    Namespace CONSOLEAPPLICATIONVB
        Class H
        End Class
    End Namespace
End Namespace
                    ]]></file>
                </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:="CONSOLEAPPLICATIONVB"))
            compilation.AssertNoErrors()

            ' no warnings if no root namespace is given
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Namespace Global
    Namespace CONSOLEAPPLICATIONVB
        Class H
        End Class
    End Namespace
End Namespace
                    ]]></file>
                </compilation>)
            compilation.AssertNoErrors()

            ' no warnings for global namespace itself
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Namespace GLObAl
    Namespace CONSOLEAPPLICATIONVB
        Class H
        End Class
    End Namespace
End Namespace
                    ]]></file>
                </compilation>)
            compilation.AssertNoErrors()

        End Sub

        <WorkItem(528713, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528713")>
        <Fact>
        Public Sub BC40056WRN_UndefinedOrEmptyNamespaceOrClass1()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("BC40056WRN_UndefinedOrEmptyNamespaceOrClass1")
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UndefinedOrEmptyNamespaceOrClass1">
        <file name="a.vb"><![CDATA[
            Imports alias1 = ns1.GenStruct(Of String)
            Structure GenStruct(Of T)
            End Structure
        ]]></file>
    </compilation>,
    options:=options
            )
            Dim expectedErrors1 = <errors><![CDATA[
BC40056: Namespace or type specified in the Imports 'ns1.GenStruct' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports alias1 = ns1.GenStruct(Of String)
                 ~~~~~~~~~~~~~~~~~~~~~~~~                                      
                                  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40056WRN_UndefinedOrEmptyNamespaceOrClass1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UndefinedOrEmptyNamespaceOrClass1">
        <file name="a.vb"><![CDATA[
            Imports ns1.GOO
            Namespace ns1
                Module M1
                    Sub GOO()
                    End Sub
                End Module
            End Namespace
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40056: Namespace or type specified in the Imports 'ns1.GOO' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports ns1.GOO
        ~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40056WRN_UndefinedOrEmptyNamespaceOrClass1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UndefinedOrEmptyNamespaceOrClass1">
        <file name="a.vb"><![CDATA[
            Imports Alias2 = System
            Imports N12 = Alias2
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40056: Namespace or type specified in the Imports 'Alias2' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
            Imports N12 = Alias2
                          ~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC40057WRN_UndefinedOrEmptyProjectNamespaceOrClass1()
            Dim globalImports = GlobalImport.Parse({"Alias2 = System", "N12 = Alias2"})
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(globalImports)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UndefinedOrEmpyProjectNamespaceOrClass1">
        <file name="a.vb"><![CDATA[
        ]]></file>
    </compilation>, options:=options)
            Dim expectedErrors1 = <errors><![CDATA[
BC40057: Namespace or type specified in the project-level Imports 'N12 = Alias2' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(545385, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545385")>
        <Fact>
        Public Sub BC41005WRN_MissingAsClauseinOperator()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
            Class D
                Shared Operator +(ByVal x As D) ' BC41005 -> BC42021. "Operator without an 'As' clause; type of Object assumed."
                    Return Nothing
                End Operator
            End Class
        ]]></file>
    </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optionStrict:=OptionStrict.Custom))

            Dim expectedErrors1 = <errors><![CDATA[
BC42021: Operator without an 'As' clause; type of Object assumed.
                Shared Operator +(ByVal x As D) ' BC41005 -> BC42021. "Operator without an 'As' clause; type of Object assumed."
                                ~
                                  ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528714, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528714"), WorkItem(1070286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070286")>
        <Fact()>
        Public Sub BC42000WRN_MustShadowOnMultipleInheritance2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustShadowOnMultipleInheritance2">
        <file name="a.vb"><![CDATA[
            Interface I1
                Sub goo(ByVal arg As Integer)
            End Interface
            Interface I2
                Sub goo(ByVal arg As Integer)
            End Interface
            Interface I3
                Inherits I1, I2
                Sub goo()
            End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40003: sub 'goo' shadows an overloadable member declared in the base interface 'I1'.  If you want to overload the base method, this method must be declared 'Overloads'.
                Sub goo()
                    ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42014WRN_IndirectlyImplementedBaseMember5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="IndirectlyImplementedBaseMember5">
        <file name="a.vb"><![CDATA[
            Interface I1
                Sub goo()
            End Interface
            Interface I2
                Inherits I1
            End Interface
            Class C1
                Implements I1
                Public Sub goo() Implements I1.goo
                End Sub
            End Class
            Class C2
                Inherits C1
                Implements I2
                Public Shadows Sub goo() Implements I1.goo
                End Sub
            End Class
        ]]></file>
    </compilation>)
            ' BC42014 is deprecated
            Dim expectedErrors1 = <errors></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC42015WRN_ImplementedBaseMember4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementedBaseMember4">
        <file name="a.vb"><![CDATA[
            Imports System
            Class c1
                Implements IDisposable
                Public Sub Dispose1() Implements System.IDisposable.Dispose
                End Sub
            End Class
            Class c2
                Inherits c1
                Implements IDisposable
                Public Sub Dispose1() Implements System.IDisposable.Dispose
                End Sub
            End Class
        ]]></file>
    </compilation>)

            ' BC42015 is deprecated
            Dim expectedErrors1 = <errors><![CDATA[
BC40003: sub 'Dispose1' shadows an overloadable member declared in the base class 'c1'.  If you want to overload the base method, this method must be declared 'Overloads'.
                Public Sub Dispose1() Implements System.IDisposable.Dispose
                           ~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(539499, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539499")>
        <Fact>
        Public Sub BC42020WRN_ObjectAssumedVar1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingAsClauseinVarDecl">
        <file name="a.vb"><![CDATA[
            Module M1
                Sub Main()
                    Dim y() = New Object() {3}
                End Sub

                Public Fld

                Public Function Goo(ByVal x) As Integer
                    Return 1
                End Function
            End Module
        ]]></file>
    </compilation>, TestOptions.ReleaseExe.WithOptionInfer(False).WithOptionStrict(OptionStrict.Custom))

            Dim expectedErrors1 = <errors><![CDATA[
BC42020: Variable declaration without an 'As' clause; type of Object assumed.
                    Dim y() = New Object() {3}
                        ~
BC42020: Variable declaration without an 'As' clause; type of Object assumed.
                Public Fld
                       ~~~
BC42020: Variable declaration without an 'As' clause; type of Object assumed.
                Public Function Goo(ByVal x) As Integer
                                          ~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

            compilation1 = compilation1.WithOptions(TestOptions.ReleaseExe.WithOptionInfer(False).WithOptionStrict(OptionStrict.Off))

            CompilationUtils.AssertNoErrors(compilation1)
        End Sub

        <WorkItem(539499, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539499")>
        <WorkItem(529849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529849")>
        <Fact()>
        Public Sub BC42020WRN_ObjectAssumedVar1WithStaticLocal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingAsClauseinVarDecl">
        <file name="a.vb"><![CDATA[
            Module M1
                Sub Main()
                    Static x = 3
                End Sub
            End Module
        ]]></file>
    </compilation>, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors><![CDATA[
BC42020: Static variable declared without an 'As' clause; type of Object assumed.
                    Static x = 3
                           ~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

            compilation1 = compilation1.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithOptionStrict(OptionStrict.Off))

            CompilationUtils.AssertNoErrors(compilation1)

            compilation1 = compilation1.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected><![CDATA[
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                    Static x = 3
                           ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ValidTypeNameAsVariableNameInAssignment()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
      <compilation name="Compilation">
          <file name="a.vb"><![CDATA[
Imports System

Namespace NS
End Namespace

Module Program2
    Sub Main2(args As System.String())
        DateTime = "hello"
        NS = "hello"
    End Sub
End Module
        ]]></file>
      </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                <errors><![CDATA[
BC30110: 'Date' is a structure type and cannot be used as an expression.
        DateTime = "hello"
        ~~~~~~~~
BC30112: 'NS' is a namespace and cannot be used as an expression.
        NS = "hello"
        ~~
                ]]></errors>)
        End Sub

        <Fact>
        Public Sub BC30209ERR_StrictDisallowImplicitObject()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingAsClauseinVarDecl">
        <file name="a.vb"><![CDATA[
            Module M1

                Public Fld

            End Module
        ]]></file>
    </compilation>, TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors><![CDATA[
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                Public Fld
                       ~~~
]]></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingAsClauseinVarDecl">
        <file name="a.vb"><![CDATA[
            Module M1
                Sub Main()
                    Dim y() = New Object() {3}
                End Sub
            End Module
        ]]></file>
    </compilation>, TestOptions.ReleaseExe.WithOptionInfer(True).WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation2, <errors/>)

            compilation2 = compilation2.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(False).WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<errors><![CDATA[
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                    Dim y() = New Object() {3}
                        ~
]]></errors>)
        End Sub

        <WorkItem(529849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529849")>
        <Fact>
        Public Sub BC30209ERR_StrictDisallowImplicitObjectWithStaticLocals()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30209ERR_StrictDisallowImplicitObjectWithStaticLocals">
        <file name="a.vb"><![CDATA[
            Module M1
                Sub Main()
                    Static x = 3
                End Sub
            End Module
        ]]></file>
    </compilation>, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors><![CDATA[
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                    Static x = 3
                           ~
]]></errors>)
        End Sub

        <WorkItem(539501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539501")>
        <Fact>
        Public Sub BC42021WRN_ObjectAssumed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingAsClauseinVarDecl">
        <file name="a.vb"><![CDATA[
            Module M1
                Function Func1()
                    Return 3
                End Function

                Sub Main()
                End Sub

                Delegate Function Func2()

                ReadOnly Property Prop3
                    Get
                        Return Nothing
                    End Get
                End Property

            End Module
        ]]></file>
    </compilation>, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOptionInfer(True))

            Dim expectedErrors1 = <errors><![CDATA[
BC42021: Function without an 'As' clause; return type of Object assumed.
                Function Func1()
                         ~~~~~
BC42021: Function without an 'As' clause; return type of Object assumed.
                Delegate Function Func2()
                                  ~~~~~
BC42022: Property without an 'As' clause; type of Object assumed.
                ReadOnly Property Prop3
                                  ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

            compilation1 = compilation1.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off).WithOptionInfer(True))
            CompilationUtils.AssertNoErrors(compilation1)
        End Sub

        <Fact>
        Public Sub BC30210ERR_StrictDisallowsImplicitProc_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingAsClauseinVarDecl">
        <file name="a.vb"><![CDATA[
            Module M1
                Function Func1()
                    Return 3
                End Function

                Sub Main()
                End Sub

                Delegate Function Func2()

                ReadOnly Property Prop3
                    Get
                        Return Nothing
                    End Get
                End Property
            End Module
        ]]></file>
    </compilation>, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On).WithOptionInfer(True))
            Dim expectedErrors1 = <errors><![CDATA[
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
                Function Func1()
                         ~~~~~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
                Delegate Function Func2()
                                  ~~~~~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
                ReadOnly Property Prop3
                                  ~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' vbc Module1.vb /target:library /noconfig /optionstrict:custom
        <Fact()>
        Public Sub BC42022WRN_ObjectAssumedProperty1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ObjectAssumedProperty1">
        <file name="a.vb"><![CDATA[
            Module Module1
                ReadOnly Property p()
                    Get
                        Return 2
                    End Get
                End Property
            End Module
        ]]></file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors><![CDATA[
BC42022: Property without an 'As' clause; type of Object assumed.
                ReadOnly Property p()
                                  ~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' EDMAURER: tested elsewhere

        '        <Fact()>
        '        Public Sub BC42102WRN_ComClassPropertySetObject1()
        '            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
        '    <compilation name="ComClassPropertySetObject1">
        '       <file name="a.vb"><![CDATA[
        '            Imports Microsoft.VisualBasic
        '            <ComClass()> Public Class Class1
        '                Public WriteOnly Property prop2(ByVal x As String) As Object
        '                    Set(ByVal Value As Object)
        '                    End Set
        '                End Property
        '            End Class
        '        ]]></file>
        '    </compilation>)
        '            Dim expectedErrors1 = <errors><![CDATA[
        'BC42102: 'Public WriteOnly Property prop2(x As String) As Object' cannot be exposed to COM as a property 'Let'. You will not be able to assign non-object values (such as numbers or strings) to this property from Visual Basic 6.0 using a 'Let' statement.
        '                Public WriteOnly Property prop2(ByVal x As String) As Object
        '                                          ~~~~~
        '                 ]]></errors>
        '            CompilationUtils.AssertTheseDeclarationErrors(compilation1, expectedErrors1)
        '        End Sub

        <Fact()>
        Public Sub BC42309WRN_XMLDocCrefAttributeNotFound1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocCrefAttributeNotFound1">
        <file name="a.vb">
            <![CDATA[
''' <see cref="System.Collections.Generic.List(Of _)"/>
Class E
End Class
]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocCrefAttributeNotFound1">
        <file name="a.vb">
            <![CDATA[
''' <see cref="System.Collections.Generic.List(Of _)"/>
Class E
End Class
]]></file>
    </compilation>, parseOptions:=VisualBasic.VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim expectedErrors = <errors><![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'System.Collections.Generic.List(Of _)' that could not be resolved.
''' <see cref="System.Collections.Generic.List(Of _)"/>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation2, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC42310WRN_XMLMissingFileOrPathAttribute1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLMissingFileOrPathAttribute1">
        <file name="a.vb"><![CDATA[
'''<include/>
Class E
End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLMissingFileOrPathAttribute1">
        <file name="a.vb"><![CDATA[
'''<include/>
Class E
End Class
        ]]></file>
    </compilation>, parseOptions:=VisualBasic.VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim expectedErrors =
<errors>
    <![CDATA[
BC42310: XML comment tag 'include' must have a 'file' attribute. XML comment will be ignored.
'''<include/>
   ~~~~~~~~~~
BC42310: XML comment tag 'include' must have a 'path' attribute. XML comment will be ignored.
'''<include/>
   ~~~~~~~~~~
]]>
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation2, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC42312WRN_XMLDocWithoutLanguageElement()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocWithoutLanguageElement">
        <file name="a.vb"><![CDATA[
            Class E
                ReadOnly Property quoteForTheDay() As String
                    ''' <summary>
                    Get
                        Return "hello"
                    End Get
                End Property
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocWithoutLanguageElement">
        <file name="a.vb"><![CDATA[
            Class E
                ReadOnly Property quoteForTheDay() As String
                    ''' <summary>
                    Get
                        Return "hello"
                    End Get
                End Property
            End Class
        ]]></file>
    </compilation>, parseOptions:=VisualBasic.VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<errors>
    <![CDATA[
BC42312: XML documentation comments must precede member or type declarations.
                    ''' <summary>
                       ~~~~~~~~~~~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
                    ''' <summary>
                        ~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
                    ''' <summary>
                                 ~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
                    ''' <summary>
                                 ~
]]>
</errors>)
        End Sub

        <Fact()>
        Public Sub BC42313WRN_XMLDocReturnsOnWriteOnlyProperty()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocReturnsOnWriteOnlyProperty">
        <file name="a.vb"><![CDATA[
            Class E
                ''' <returns></returns>
                WriteOnly  Property quoteForTheDay() As String
                    set
                    End set
                End Property
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocReturnsOnWriteOnlyProperty">
        <file name="a.vb"><![CDATA[
            Class E
                ''' <returns></returns>
                WriteOnly  Property quoteForTheDay() As String
                    set
                    End set
                End Property
            End Class
        ]]></file>
    </compilation>, parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim expectedErrors = <errors><![CDATA[
BC42313: XML comment tag 'returns' is not permitted on a 'WriteOnly' Property.
                ''' <returns></returns>
                    ~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation2, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC42314WRN_XMLDocOnAPartialType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="XMLDocOnAPartialType">
        <file name="a.vb"><![CDATA[
            ''' <summary>
            ''' </summary>
            ''' <remarks></remarks>
            partial Class E
            End Class
            ''' <summary>
            ''' </summary>
            ''' <remarks></remarks>
            partial Class E
            End Class

            ''' <summary>
            ''' </summary>
            ''' <remarks></remarks>
            partial Interface I
            End Interface
            ''' <summary>
            ''' </summary>
            ''' <remarks></remarks>
            partial Interface I
            End Interface

            ''' <summary>
            ''' </summary>
            ''' <remarks></remarks>
            partial Module M
            End Module
            ''' <summary>
            ''' </summary>
            ''' <remarks></remarks>
            partial Module M
            End Module
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="XMLDocOnAPartialType">
        <file name="a.vb"><![CDATA[
''' <summary>
''' </summary>
''' <remarks></remarks>
partial Class E
End Class
''' <summary>
''' </summary>
''' <remarks></remarks>
partial Class E
End Class

''' <summary> 3
''' </summary>
''' <remarks></remarks>
partial Interface I
End Interface
''' <summary> 4
''' </summary>
''' <remarks></remarks>
partial Interface I
End Interface

''' <summary> 5
''' </summary>
''' <remarks></remarks>
partial Module M
End Module
''' <summary> 6
''' </summary>
''' <remarks></remarks>
partial Module M
End Module
        ]]></file>
    </compilation>, parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim expectedErrors = <errors><![CDATA[
BC42314: XML comment cannot be applied more than once on a partial class. XML comments for this class will be ignored.
''' <summary>
   ~~~~~~~~~~~
BC42314: XML comment cannot be applied more than once on a partial class. XML comments for this class will be ignored.
''' <summary>
   ~~~~~~~~~~~
BC42314: XML comment cannot be applied more than once on a partial interface. XML comments for this interface will be ignored.
''' <summary> 3
   ~~~~~~~~~~~~~
BC42314: XML comment cannot be applied more than once on a partial interface. XML comments for this interface will be ignored.
''' <summary> 4
   ~~~~~~~~~~~~~
BC42314: XML comment cannot be applied more than once on a partial module. XML comments for this module will be ignored.
''' <summary> 5
   ~~~~~~~~~~~~~
BC42314: XML comment cannot be applied more than once on a partial module. XML comments for this module will be ignored.
''' <summary> 6
   ~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation2, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC42317WRN_XMLDocBadGenericParamTag2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocBadGenericParamTag2">
        <file name="a.vb">
            <![CDATA[
Class C1(Of X)
    ''' <typeparam name="X">typeparam E1</typeparam>
    ''' <summary>summary - E1</summary>
    ''' <remarks>remarks - E1</remarks>
    Sub E1(Of T)(ByVal p As X)
    End Sub
End Class
]]>
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocBadGenericParamTag2">
        <file name="a.vb">
            <![CDATA[
Class C1(Of X)
    ''' <typeparam name="X">typeparam E1</typeparam>
    ''' <summary>summary - E1</summary>
    ''' <remarks>remarks - E1</remarks>
    Sub E1(Of T)(ByVal p As X)
    End Sub
End Class
]]>
        </file>
    </compilation>, parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim expectedErrors =
<errors>
    <![CDATA[
BC42317: XML comment type parameter 'X' does not match a type parameter on the corresponding 'sub' statement.
    ''' <typeparam name="X">typeparam E1</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation2, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC42318WRN_XMLDocGenericParamTagWithoutName()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocGenericParamTagWithoutName">
        <file name="a.vb">
            <![CDATA[
Class C1(Of X)
    ''' <typeparam>typeparam E1</typeparam>
    Sub E1(Of T)(ByVal p As X)
    End Sub
End Class
]]>
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocGenericParamTagWithoutName">
        <file name="a.vb">
            <![CDATA[
Class C1(Of X)
    ''' <typeparam>typeparam E1</typeparam>
    Sub E1(Of T)(ByVal p As X)
    End Sub
End Class
]]>
        </file>
    </compilation>, parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim expectedErrors =
<errors>
    <![CDATA[
BC42318: XML comment type parameter must have a 'name' attribute.
    ''' <typeparam>typeparam E1</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation2, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC42319WRN_XMLDocExceptionTagWithoutCRef()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocExceptionTagWithoutCRef">
        <file name="a.vb"><![CDATA[
            Class Myexception
                ''' <exception></exception>
                Sub Test()
                End Sub
            End Class
        ]]></file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="XMLDocExceptionTagWithoutCRef">
        <file name="a.vb"><![CDATA[
Class Myexception
    ''' <exception></exception>
    Sub Test()
    End Sub
End Class
        ]]></file>
    </compilation>, parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim expectedErrors =
<errors>
    <![CDATA[
BC42319: XML comment exception must have a 'cref' attribute.
    ''' <exception></exception>
        ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation2, expectedErrors)
        End Sub

        <WorkItem(541661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541661")>
        <Fact()>
        Public Sub BC42333WRN_VarianceDeclarationAmbiguous3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="VarianceDeclarationAmbiguous3">
        <file name="a.vb"><![CDATA[
Imports System.Xml.Linq
Imports System.Linq
Module M1
    Class C1
        'COMPILEWARNING : BC42333, "System.Collections.Generic.IEnumerable(Of XDocument)", BC42333, "System.Collections.Generic.IEnumerable(Of XElement)"
        Implements System.Collections.Generic.IEnumerable(Of XDocument), System.Collections.Generic.IEnumerable(Of XElement)
        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of System.Xml.Linq.XDocument) Implements System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XDocument).GetEnumerator
            Return Nothing
        End Function
        Public Function GetEnumerator1() As System.Collections.Generic.IEnumerator(Of System.Xml.Linq.XElement) Implements System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement).GetEnumerator
            Return Nothing
        End Function
        Public Function GetEnumerator2() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return Nothing
        End Function
    End Class
End Module
        ]]></file>
    </compilation>, XmlReferences)
            Dim expectedErrors1 = <errors><![CDATA[
BC42333: Interface 'System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)' is ambiguous with another implemented interface 'System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XDocument)' due to the 'In' and 'Out' parameters in 'Interface IEnumerable(Of Out T)'.
        Implements System.Collections.Generic.IEnumerable(Of XDocument), System.Collections.Generic.IEnumerable(Of XElement)
                                                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' vbc Module1.vb /target:library /noconfig /optionstrict:custom
        <Fact()>
        Public Sub BC42347WRN_MissingAsClauseinFunction()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingAsClauseinFunction">
        <file name="a.vb"><![CDATA[
            Module M1
                Function Goo()
                    Return Nothing
                End Function
            End Module
        ]]></file>
    </compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optionStrict:=OptionStrict.Custom))
            Dim expectedErrors1 = <errors><![CDATA[
BC42021: Function without an 'As' clause; return type of Object assumed.
                Function Goo()
                         ~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub
#End Region

        ' Check that errors are reported for type parameters.
        <Fact>
        Public Sub TypeParameterErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Class X(Of T$)
            End Class
            Partial Class Y(Of T, U)
            End Class
            Class Z(Of T, In U, Out V)
            End Class
            Interface IZ(Of T, In U, Out V)
            End Interface
        ]]></file>
        <file name="b.vb"><![CDATA[
            Partial Class Y(Of T, V)
            End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC32041: Type character cannot be used in a type parameter declaration.
Class X(Of T$)
           ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
            Class Z(Of T, In U, Out V)
                          ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
            Class Z(Of T, In U, Out V)
                                ~~~
BC30931: Type parameter name 'V' does not match the name 'U' of the corresponding type parameter defined on one of the other partial types of 'Y'.
Partial Class Y(Of T, V)
                      ~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        'Check that errors are reported for multiple type arguments
        <Fact>
        Public Sub MultipleTypeArgumentErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Imports System
            Imports System.Collections
            Class G(Of T)
            Dim x As G(Of System.Int64,
                     System.Collections.Hashtable)
            End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC32043: Too many type arguments to 'G(Of T)'.
            Dim x As G(Of System.Int64,
                     ~~~~~~~~~~~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        ' Check that errors are reported for duplicate option statements in a single file.
        <Fact>
        Public Sub BC30225ERR_DuplicateOption1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Option Compare Text
Option Infer On
Option Compare On
    ]]></file>
        <file name="b.vb"><![CDATA[
Option Strict On        
Option Infer On
Option Strict On
Option Explicit
Option Explicit Off
    ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30225: 'Option Compare' statement can only appear once per file.
Option Compare On
~~~~~~~~~~~~~~~
BC30207: 'Option Compare' must be followed by 'Text' or 'Binary'.
Option Compare On
               ~~
BC30225: 'Option Strict' statement can only appear once per file.
Option Strict On
~~~~~~~~~~~~~~~~
BC30225: 'Option Explicit' statement can only appear once per file.
Option Explicit Off
~~~~~~~~~~~~~~~~~~~                                     
                                 ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC31393ERR_ExpressionHasTheTypeWhichIsARestrictedType()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub M(tr As System.TypedReference)
        Dim t = tr.GetType()
    End Sub
End Module
    ]]></file>
    </compilation>).VerifyDiagnostics()
        End Sub

        ' Check that errors are reported for import statements in a single file.
        <Fact>
        Public Sub ImportErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports N1(Of String)
Imports A = N1
Imports B = N1
Imports N1.M1
Imports N1.M1
Imports A = N1.N2.C
Imports N3
Imports N1.N2
Imports N1.Gen(Of C)
Imports N1.Gen(Of Integer, String)
Imports System$.Collections%
Imports D$ = System.Collections
Imports N3
Imports System.Cheesecake.Frosting
        ]]></file>
        <file name="b.vb"><![CDATA[
Namespace N1
    Class Gen(Of T)
    End Class
    Module M1
    End Module
End Namespace
Namespace N1.N2
    Class C
    End Class
End Namespace
Namespace N3
    Class D
    End Class
End Namespace
        ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC32045: 'N1' has no type parameters and so cannot have type arguments.
Imports N1(Of String)
        ~~~~~~~~~~~~~
BC31051: Namespace or type 'M1' has already been imported.
Imports N1.M1
        ~~~~~
BC30572: Alias 'A' is already declared.
Imports A = N1.N2.C
        ~
BC30002: Type 'C' is not defined.
Imports N1.Gen(Of C)
                  ~
BC32043: Too many type arguments to 'Gen(Of T)'.
Imports N1.Gen(Of Integer, String)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30468: Type declaration characters are not valid in this context.
Imports System$.Collections%
        ~~~~~~~
BC30468: Type declaration characters are not valid in this context.
Imports System$.Collections%
                ~~~~~~~~~~~~
BC31398: Type characters are not allowed on Imports aliases.
Imports D$ = System.Collections
        ~~
BC31051: Namespace or type 'N3' has already been imported.
Imports N3
        ~~
BC40056: Namespace or type specified in the Imports 'System.Cheesecake.Frosting' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports System.Cheesecake.Frosting
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        ' Check that errors are reported for import statements in a single file.
        <Fact>
        Public Sub ProjectImportErrors()
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            Dim globalImports = GlobalImport.Parse({
                    "N1(Of String)",
                    "A = N1",
                    "B = N1",
                    "N1.M1",
                    "N1.M1",
                    "A = N1.N2.C",
                    "N3",
                    "N1.N2",
                    "N1.Gen(Of C)",
                    "N1.Gen(Of Integer, String)",
                    "System$.Collections%",
                    "D$ = System.Collections",
                    "N3",
                    "System.Cheesecake.Frosting"
                    }, diagnostics)

            Assert.NotEqual(diagnostics, Nothing)

            CompilationUtils.AssertTheseDiagnostics(diagnostics,
<errors><![CDATA[
BC31398: Error in project-level import 'D$ = System.Collections' at 'D$' : Type characters are not allowed on Imports aliases.
]]></errors>)

            ' only include the imports with correct syntax:
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(globalImports)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="b.vb"><![CDATA[
Namespace N1
    Class Gen(Of T)
    End Class
    Module M1
    End Module
End Namespace
Namespace N1.N2
    Class C
    End Class
End Namespace
Namespace N3
    Class D
    End Class
End Namespace
        ]]></file>
    </compilation>, options)

            Dim expectedErrors = <errors><![CDATA[
BC30002: Error in project-level import 'N1.Gen(Of C)' at 'C' : Type 'C' is not defined.
BC30468: Error in project-level import 'System$.Collections%' at 'Collections%' : Type declaration characters are not valid in this context.
BC30468: Error in project-level import 'System$.Collections%' at 'System$' : Type declaration characters are not valid in this context.
BC30572: Error in project-level import 'A = N1.N2.C' at 'A' : Alias 'A' is already declared.
BC32043: Error in project-level import 'N1.Gen(Of Integer, String)' at 'N1.Gen(Of Integer, String)' : Too many type arguments to 'Gen(Of T)'.
BC32045: Error in project-level import 'N1(Of String)' at 'N1(Of String)' : 'N1' has no type parameters and so cannot have type arguments.
BC40057: Namespace or type specified in the project-level Imports 'System.Cheesecake.Frosting' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ModifierErrorsInsideNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[

          Namespace n1
                Shadows Enum e6
                  x
                End Enum

                Private Enum e7
                    x
                End Enum
            End Namespace

            Shadows Enum e8
                x
            End Enum

            Private Enum e9
                x
            End Enum
        ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC32200: 'e6' cannot be declared 'Shadows' outside of a class, structure, or interface.
                Shadows Enum e6
                             ~~
BC31089: Types declared 'Private' must be inside another type.
                Private Enum e7
                             ~~
BC32200: 'e8' cannot be declared 'Shadows' outside of a class, structure, or interface.
            Shadows Enum e8
                         ~~
BC31089: Types declared 'Private' must be inside another type.
            Private Enum e9
                         ~~                                     
                       ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ModifierErrorsInsideInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface i
    Private Class c1

    End Class

    Shared Class c2

    End Class

    MustInherit Class c3

    End Class

    MustOverride Class c4

    End Class
End Interface
        ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC31070: Class in an interface cannot be declared 'Private'.
    Private Class c1
    ~~~~~~~
BC30461: Classes cannot be declared 'Shared'.
    Shared Class c2
    ~~~~~~
BC30461: Classes cannot be declared 'MustOverride'.
    MustOverride Class c4
    ~~~~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        ' Checks for accessibility across partial types
        <Fact>
        Public Sub ModifierErrorsAcrossPartialTypes()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Partial public Class c1
            End Class

            Partial friend Class c1
            End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30925: Specified access 'Friend' for 'c1' does not match the access 'Public' specified on one of its other partial types.
            Partial friend Class c1
                                 ~~
            ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            NotInheritable class A
            end class

            Partial MustInherit Class A
            End class
          ]]></file>
    </compilation>)

            Dim expectedErrors2 = <errors><![CDATA[
BC30926: 'MustInherit' cannot be specified for partial type 'A' because it cannot be combined with 'NotInheritable' specified for one of its other partial types.
            Partial MustInherit Class A
                                      ~

  ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation2, expectedErrors2)

        End Sub

        ' Checks for missing partial on classes
        <Fact>
        Public Sub ModifierWarningsAcrossPartialTypes()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
            Class cc1
            End Class

            Class cC1
            End Class

            partial Class Cc1
            End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC40046: class 'cc1' and partial class 'Cc1' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
Class cc1
      ~~~
BC40046: class 'cC1' and partial class 'Cc1' conflict in namespace '<Default>', but are being merged because one of them is declared partial.
            Class cC1
                  ~~~
  ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class cc1
        End Class

        Class cC1
        End Class

        Class Cc1
        End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors2 = <errors><![CDATA[
BC30179: class 'cC1' and class 'cc1' conflict in namespace '<Default>'.
        Class cC1
              ~~~
BC30179: class 'Cc1' and class 'cc1' conflict in namespace '<Default>'.
        Class Cc1
              ~~~                 
  ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation2, expectedErrors2)

        End Sub

        <Fact>
        Public Sub ErrorTypeNotDefineType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
 <compilation name="ErrorType">
     <file name="a.vb"><![CDATA[
Class A
    Dim n As B
End Class
     ]]></file>
 </compilation>)

            Dim errs = compilation.GetDeclarationDiagnostics()
            Assert.Equal(1, errs.Length())
            Dim err = DirectCast(errs.Single(), Diagnostic)
            Assert.Equal(DirectCast(ERRID.ERR_UndefinedType1, Integer), err.Code)

            Dim classA = DirectCast(compilation.GlobalNamespace.GetTypeMembers("A").Single(), NamedTypeSymbol)
            Dim fsym = DirectCast(classA.GetMembers()(1), FieldSymbol)
            Dim sym = fsym.Type
            Assert.Equal(SymbolKind.ErrorType, sym.Kind)
            Assert.Equal("B", sym.Name)
            Assert.Null(sym.ContainingAssembly)
            Assert.Null(sym.ContainingSymbol)
            Assert.Equal(Accessibility.Public, sym.DeclaredAccessibility)
            Assert.False(sym.IsShared)
            Assert.False(sym.IsMustOverride)
            Assert.False(sym.IsNotOverridable)
            Assert.False(sym.IsValueType)
            Assert.True(sym.IsReferenceType)

            Assert.Equal(0, sym.Interfaces.Length())
            Assert.Null(sym.BaseType)
            Assert.Equal("B", DirectCast(sym, ErrorTypeSymbol).ConstructedFrom.ToTestDisplayString())

            Assert.Equal(0, sym.GetAttributes().Length()) ' Enumerable.Empty<SymbolAttribute>()
            Assert.Equal(0, sym.GetMembers().Length()) ' Enumerable.Empty<Symbol>()
            Assert.Equal(0, sym.GetMembers(String.Empty).Length())
            Assert.Equal(0, sym.GetTypeMembers().Length()) ' Enumerable.Empty<NamedTypeSymbol>()
            Assert.Equal(0, sym.GetTypeMembers(String.Empty).Length())
            Assert.Equal(0, sym.GetTypeMembers(String.Empty, 0).Length())
        End Sub

        <WorkItem(539568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539568")>
        <Fact>
        Public Sub AccessBaseClassThroughNestedClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Public Class X
                        End Class
                    End Class
                    Class B
                        Inherits B.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31447: Class 'B' cannot reference itself in Inherits clause.
                        Inherits B.C.X
                                 ~
]]></errors>)
        End Sub

        <WorkItem(539568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539568")>
        <Fact>
        Public Sub AccessBaseClassThroughNestedClass2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Public Class X
                        End Class
                    End Class
                    Class B
                        Inherits C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31446: Class 'B' cannot reference its nested type 'B.C' in Inherits clause.
                        Inherits C.X
                                 ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClass3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Public Class X
                            Public Class A
                            End Class
                        End Class
                    End Class
                    Class B
                        Inherits B.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31447: Class 'B' cannot reference itself in Inherits clause.
                        Inherits B.C.X
                                 ~
]]></errors>)

        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClass4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Public Class X
                            Public Class A
                            End Class
                        End Class
                    End Class
                    Class B
                        Inherits C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31446: Class 'B' cannot reference its nested type 'B.C' in Inherits clause.
                        Inherits C.X
                                 ~
]]></errors>)

        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClass5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Interface A
                        Class X
                        End Class
                    End Interface
                    Class B
                        Inherits C.X
                        Public Interface C
                            Inherits A
                        End Interface
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31446: Class 'B' cannot reference its nested type 'B.C' in Inherits clause.
                        Inherits C.X
                                 ~
]]></errors>)

        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClass6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Class X
                        End Class
                    End Class
                    Class B
                        Inherits C(Of Integer).X
                        Public Class C(Of T)
                            Inherits A
                        End Class 
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31446: Class 'B' cannot reference its nested type 'B.C(Of Integer)' in Inherits clause.
                        Inherits C(Of Integer).X
                                 ~~~~~~~~~~~~~
]]></errors>)

        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClass7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Class X
                            Class A
                            End Class
                        End Class
                    End Class
                    Class B
                        Inherits D.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                    Class D
                        Inherits B.C
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31449: Inherits clause of class 'B' causes cyclic dependency: 
    'B.C' is nested in 'B'.
    Base type of 'B' needs 'B.C' to be resolved.
                        Inherits D.X
                                 ~~~
]]></errors>)

        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClass8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Class X
                            Class A
                            End Class
                        End Class
                    End Class

                    Class B
                        Inherits D
                        Public Class C
                            Inherits A
                        End Class
                    End Class

                    Class D
                        Inherits B.C.X
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31449: Inherits clause of class 'B.C' causes cyclic dependency: 
    Base type of 'D' needs 'B.C' to be resolved.
    Base type of 'B.C' needs 'D' to be resolved.
                            Inherits A
                                     ~
BC30002: Type 'B.C.X' is not defined.
                        Inherits B.C.X
                                 ~~~~~
]]></errors>)

        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClass9()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Class X
                            Class A
                            End Class
                        End Class
                    End Class

                    Class B
                        Inherits D.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class

                    Class D
                        Inherits B
                    End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31446: Class 'B' cannot reference its nested type 'B.C' in Inherits clause.
                        Inherits D.C.X
                                 ~~~
]]></errors>)

        End Sub

        <Fact>
        Public Sub AccessBaseClassThroughNestedClassA()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
Interface IIndia(Of T)
End Interface

Class Alpha
    Class Bravo
    End Class
End Class

Class Charlie
    Inherits Alpha
    Implements IIndia(Of Charlie.Bravo)
End Class

Class Echo
    Implements IIndia(Of Echo)
End Class

Class Golf
    Implements IIndia(Of Golf.Hotel)
    Class Hotel
    End Class
End Class

Class Juliet
    Implements IIndia(Of Juliet.Kilo.Lima)
    Class Kilo
        Class Lima
        End Class
    End Class
End Class

Class November(Of T)
End Class

Class Oscar
    Inherits November(Of Oscar)
End Class

Class Papa
    Inherits November(Of Papa.Quebec)
    Class Quebec
    End Class
End Class

Class Romeo
    Inherits November(Of Romeo.Sierra.Tango)
    Class Sierra
        Class Tango
        End Class
    End Class
End Class

Class Uniform
    Implements Uniform.IIndigo
    Interface IIndigo
    End Interface
End Class

Interface IBlah
    Inherits Victor.IIdiom
End Interface

Class Victor
    Implements IBlah
    Interface IIdiom
    End Interface
End Class

Class Xray
    Implements Yankee.IIda
    Class Yankee
        Inherits Xray
        Interface IIda
        End Interface
    End Class
End Class

Class Beta
    Inherits Gamma
    Class Gamma
    End Class
End Class

Class Delta
    Inherits Delta.Epsilon
    Class Epsilon
    End Class
End Class

Class Zeta(Of T)
    Inherits Eta(Of Beta)
End Class
Class Eta(Of T)
    Inherits Zeta(Of Delta)
End Class
                ]]></file>
              </compilation>)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31446: Class 'Beta' cannot reference its nested type 'Beta.Gamma' in Inherits clause.
    Inherits Gamma
             ~~~~~
BC31447: Class 'Delta' cannot reference itself in Inherits clause.
    Inherits Delta.Epsilon
             ~~~~~
BC30257: Class 'Zeta(Of T)' cannot inherit from itself: 
    'Zeta(Of T)' inherits from 'Eta(Of Beta)'.
    'Eta(Of Beta)' inherits from 'Zeta(Of T)'.
    Inherits Eta(Of Beta)
             ~~~~~~~~~~~~
]]></errors>)

        End Sub

        <WorkItem(539568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539568")>
        <Fact>
        Public Sub AccessInterfaceThroughNestedClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
              <compilation>
                  <file name="a.vb"><![CDATA[
                    Class A
                        Public Interface X
                        End Interface
                    End Class
                    Class B
                        Implements B.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
              </compilation>
            )
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(539568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539568")>
        <Fact>
        Public Sub AccessBaseClassThroughNestedClassSemantic_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
                    Class A
                        Public Class X
                        End Class
                    End Class
                    Class B
                        Inherits B.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
            </compilation>
            )
            '  resolve X from 'Inherits B.C.X' first, then 'Inherits A'
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 6).Parent, ExpressionSyntax))
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 8).Parent, ExpressionSyntax))
        End Sub

        <WorkItem(539568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539568")>
        <Fact>
        Public Sub AccessBaseClassThroughNestedClassSemantic_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
                    Class A
                        Public Class X
                        End Class
                    End Class
                    Class B
                        Inherits B.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
            </compilation>
            )
            '  resolve 'Inherits A', then X from 'Inherits B.C.X' first
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 8).Parent, ExpressionSyntax))
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 6).Parent, ExpressionSyntax))
        End Sub

        <WorkItem(539568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539568")>
        <Fact>
        Public Sub AccessInterfaceThroughNestedClassSemantic_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
                    Class A
                        Public Interface X
                        End Interface
                    End Class
                    Class B
                        Implements B.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
            </compilation>
            )
            '  resolve X from 'Inherits B.C.X' first, then 'Implements A'
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 6).Parent, ExpressionSyntax))
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 8).Parent, ExpressionSyntax))
        End Sub

        <WorkItem(539568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539568")>
        <Fact>
        Public Sub AccessInterfaceThroughNestedClassSemantic_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
                    Class A
                        Public Interface X
                        End Interface
                    End Class
                    Class B
                        Implements B.C.X
                        Public Class C
                            Inherits A
                        End Class
                    End Class
                ]]></file>
            </compilation>
            )
            '  resolve 'Inherits A', then X from 'Implements B.C.X' first
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 8).Parent, ExpressionSyntax))
            model.GetSemanticInfoSummary(CType(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierToken, 6).Parent, ExpressionSyntax))
        End Sub

        <Fact>
        Public Sub InterfaceModifierErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
        Static Interface i1
        End Interface

        Static Class c1
        End Class

        Static Structure s1
        End Structure

        Static Enum e1
            dummy
        End Enum

        Static Delegate Sub s()

        Static Module m
        End Module
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30397: 'Static' is not valid on an Interface declaration.
Static Interface i1
~~~~~~
BC30461: Classes cannot be declared 'Static'.
        Static Class c1
        ~~~~~~
BC30395: 'Static' is not valid on a Structure declaration.
        Static Structure s1
        ~~~~~~
BC30396: 'Static' is not valid on an Enum declaration.
        Static Enum e1
        ~~~~~~
BC30385: 'Static' is not valid on a Delegate declaration.
        Static Delegate Sub s()
        ~~~~~~
BC31052: Modules cannot be declared 'Static'.
        Static Module m
        ~~~~~~                                     
                       ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BaseClassErrors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="1.vb"><![CDATA[
Option Strict On        

Class C1
    Inherits Object, Object
End Class

Class C11
    Inherits System.Exception
    Inherits System.Exception
End Class

Class C2
    Inherits System.Collections.ArrayList, System.Collections.Generic.List(Of Integer)
End Class 

Class C3(Of T)
    Inherits T
End Class

Partial Class C4
    Inherits System.Collections.ArrayList
End Class
NotInheritable Class NI
End class
    ]]></file>
    <file name="a.vb"><![CDATA[
Option Strict Off        
Public Partial Class C4
    Inherits System.Collections.Generic.List(Of Integer)
End Class
Interface I1
End Interface
Class C5
    Inherits I1
End Class
Class C6
    Inherits System.Guid
End Class
Class C7
    Inherits NI
End Class
Class C8
    Inherits System.Delegate
End Class
Module M1
    Inherits System.Object
End Module
Structure S1
    Inherits System.Collections.Hashtable
End Structure
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30121: 'Inherits' can appear only once within a 'Class' statement and can only specify one class.
    Inherits Object, Object
    ~~~~~~~~~~~~~~~~~~~~~~~
BC30121: 'Inherits' can appear only once within a 'Class' statement and can only specify one class.
    Inherits System.Exception
    ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30121: 'Inherits' can appear only once within a 'Class' statement and can only specify one class.
    Inherits System.Collections.ArrayList, System.Collections.Generic.List(Of Integer)
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32055: Class 'C3' cannot inherit from a type parameter.
    Inherits T
             ~
BC30928: Base class 'List(Of Integer)' specified for class 'C4' cannot be different from the base class 'ArrayList' of one of its other partial types.
    Inherits System.Collections.Generic.List(Of Integer)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30258: Classes can inherit only from other classes.
    Inherits I1
             ~~
BC30258: Classes can inherit only from other classes.
    Inherits System.Guid
             ~~~~~~~~~~~
BC30299: 'C7' cannot inherit from class 'NI' because 'NI' is declared 'NotInheritable'.
    Inherits NI
             ~~
BC30015: Inheriting from '[Delegate]' is not valid.
    Inherits System.Delegate
             ~~~~~~~~~~~~~~~
BC30230: 'Inherits' not valid in Modules.
    Inherits System.Object
    ~~~~~~~~~~~~~~~~~~~~~~
BC30628: Structures cannot have 'Inherits' statements.
    Inherits System.Collections.Hashtable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BaseClassErrors2a()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class B(Of T)
    Class Inner
    End Class
End Class

Class D1
    Inherits B(Of Integer)
    Dim x As B(Of D1.Inner)
End Class

Class D2
    Inherits B(Of D2.Inner)
End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30002: Type 'D2.Inner' is not defined.
    Inherits B(Of D2.Inner)
                  ~~~~~~~~
]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BaseClassErrors2b()

            ' There is a race known in base type detection
            For i = 0 To 50
                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="b.vb"><![CDATA[
Class XB(Of T)
    Class Inner2
    End Class
End Class

Class XC
    Inherits XB(Of XD.Inner2)
End Class

Class XD
    Inherits XB(Of XC.Inner2)
End Class
        ]]></file>
    </compilation>)

                CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC31449: Inherits clause of class 'XC' causes cyclic dependency: 
    Base type of 'XD' needs 'XC' to be resolved.
    Base type of 'XC' needs 'XD' to be resolved.
    Inherits XB(Of XD.Inner2)
             ~~~~~~~~~~~~~~~~
BC30002: Type 'XC.Inner2' is not defined.
    Inherits XB(Of XC.Inner2)
                   ~~~~~~~~~
]]></errors>)
            Next
        End Sub

        <Fact()>
        Public Sub BaseClassErrors2c()

            ' There is a race known in base type detection
            For i = 0 To 50
                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="b.vb"><![CDATA[
Class XB(Of T)
    Class Inner2
    End Class
End Class

Class X0
    Inherits XB(Of X1.Inner2)
End Class

Class X1
    Inherits XB(Of X2.Inner2)
End Class

Class X2
    Inherits XB(Of X3.Inner2)
End Class

Class X3
    Inherits XB(Of X4.Inner2)
End Class

Class X4
    Inherits XB(Of X5.Inner2)
End Class

Class X5
    Inherits XB(Of X6.Inner2)
End Class

Class X6
    Inherits XB(Of X7.Inner2)
End Class

Class X7
    Inherits XB(Of X8.Inner2)
End Class

Class X8
    Inherits XB(Of X9.Inner2)
End Class

Class X9
    Inherits XB(Of X0.Inner2)
End Class
        ]]></file>
    </compilation>)

                Dim expectedErrors = <errors><![CDATA[
BC31449: Inherits clause of class 'X0' causes cyclic dependency: 
    Base type of 'X1' needs 'X2' to be resolved.
    Base type of 'X2' needs 'X3' to be resolved.
    Base type of 'X3' needs 'X4' to be resolved.
    Base type of 'X4' needs 'X5' to be resolved.
    Base type of 'X5' needs 'X6' to be resolved.
    Base type of 'X6' needs 'X7' to be resolved.
    Base type of 'X7' needs 'X8' to be resolved.
    Base type of 'X8' needs 'X9' to be resolved.
    Base type of 'X9' needs 'X0' to be resolved.
    Base type of 'X0' needs 'X1' to be resolved.
    Inherits XB(Of X1.Inner2)
             ~~~~~~~~~~~~~~~~
BC30002: Type 'X0.Inner2' is not defined.
    Inherits XB(Of X0.Inner2)
                   ~~~~~~~~~
                                     ]]></errors>

                CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
            Next
        End Sub

        <Fact()>
        Public Sub BaseClassErrors2d()

            ' There is a race known in base type detection
            For i = 0 To 50
                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="b.vb"><![CDATA[
Class XB(Of T)
    Class Inner2
    End Class
End Class

Class X0
    Inherits XB(Of X9.Inner2) ' X0
End Class

Class X1
    Inherits XB(Of X9.Inner2) ' X1
End Class

Class X2
    Inherits XB(Of X1.Inner2)
End Class

Class X3
    Inherits XB(Of X2.Inner2)
End Class

Class X4
    Inherits XB(Of X3.Inner2)
End Class

Class X5
    Inherits XB(Of X4.Inner2)
End Class

Class X6
    Inherits XB(Of X5.Inner2)
End Class

Class X7
    Inherits XB(Of X6.Inner2)
End Class

Class X8
    Inherits XB(Of X7.Inner2)
End Class

Class X9
    Inherits XB(Of X8.Inner2)
End Class
        ]]></file>
    </compilation>)

                Dim expectedErrors = <errors><![CDATA[
BC31449: Inherits clause of class 'X1' causes cyclic dependency: 
    Base type of 'X9' needs 'X8' to be resolved.
    Base type of 'X8' needs 'X7' to be resolved.
    Base type of 'X7' needs 'X6' to be resolved.
    Base type of 'X6' needs 'X5' to be resolved.
    Base type of 'X5' needs 'X4' to be resolved.
    Base type of 'X4' needs 'X3' to be resolved.
    Base type of 'X3' needs 'X2' to be resolved.
    Base type of 'X2' needs 'X1' to be resolved.
    Base type of 'X1' needs 'X9' to be resolved.
    Inherits XB(Of X9.Inner2) ' X1
             ~~~~~~~~~~~~~~~~
BC30002: Type 'X1.Inner2' is not defined.
    Inherits XB(Of X1.Inner2)
                   ~~~~~~~~~
                                     ]]></errors>

                CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
            Next
        End Sub

        <Fact()>
        Public Sub BaseClassErrors2e()

            ' There is a race known in base type detection
            For i = 0 To 50
                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="b.vb"><![CDATA[
Class XB(Of T)
    Class Inner2
    End Class
End Class

Class XC
    Inherits XB(Of XD.Inner2)
End Class

Class XD
    Inherits XB(Of XC.Inner2)
End Class

Class XE
    Inherits XB(Of XC.Inner2)
End Class
        ]]></file>
    </compilation>)

                Dim expectedErrors = <errors><![CDATA[
BC31449: Inherits clause of class 'XC' causes cyclic dependency: 
    Base type of 'XD' needs 'XC' to be resolved.
    Base type of 'XC' needs 'XD' to be resolved.
    Inherits XB(Of XD.Inner2)
             ~~~~~~~~~~~~~~~~
BC30002: Type 'XC.Inner2' is not defined.
    Inherits XB(Of XC.Inner2)
                   ~~~~~~~~~
BC30002: Type 'XC.Inner2' is not defined.
    Inherits XB(Of XC.Inner2)
                   ~~~~~~~~~
                                    ]]></errors>

                CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
            Next
        End Sub

        <Fact()>
        Public Sub BaseClassErrors2f()

            ' There is a race known in base type detection
            For i = 0 To 50
                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="b.vb"><![CDATA[
Class XC
    Inherits XD.Inner2
End Class

Class XD
    Inherits XC.Inner2
End Class
        ]]></file>
    </compilation>)

                CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC30002: Type 'XD.Inner2' is not defined.
    Inherits XD.Inner2
             ~~~~~~~~~
BC31449: Inherits clause of class 'XC' causes cyclic dependency: 
    Base type of 'XD' needs 'XC' to be resolved.
    Base type of 'XC' needs 'XD' to be resolved.
    Inherits XD.Inner2
             ~~~~~~~~~
BC30002: Type 'XC.Inner2' is not defined.
    Inherits XC.Inner2
             ~~~~~~~~~
]]></errors>)
            Next
        End Sub

        <Fact()>
        Public Sub BaseClassErrors2g()

            ' There is a race known in base type detection
            For i = 0 To 50
                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="b.vb"><![CDATA[
Class XC
    Inherits XD.Inner2
End Class

Class XD
    Inherits XC.Inner2
    Class Inner2
    End Class
End Class
        ]]></file>
    </compilation>)

                CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors><![CDATA[
BC30002: Type 'XC.Inner2' is not defined.
    Inherits XC.Inner2
             ~~~~~~~~~
BC31449: Inherits clause of class 'XD' causes cyclic dependency: 
    'XD.Inner2' is nested in 'XD'.
    Base type of 'XD' needs 'XD.Inner2' to be resolved.
    Inherits XC.Inner2
             ~~~~~~~~~
]]></errors>)
            Next
        End Sub

        <Fact>
        Public Sub FieldErrors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Partial Class C
    Public Public p as Integer
    Public Private q as Integer
    Private MustOverride r as Integer
    Dim s 
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On        
Public Partial Class C
    Dim x% As Integer
    Dim t ' error only if Option Strict is on
    Dim u? as Integer?
End Class
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30178: Specifier is duplicated.
    Public Public p as Integer
           ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Public Private q as Integer
           ~~~~~~~
BC30235: 'MustOverride' is not valid on a member variable declaration.
    Private MustOverride r as Integer
            ~~~~~~~~~~~~
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
    Dim x% As Integer
        ~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
    Dim t ' error only if Option Strict is on
        ~
BC33100: Nullable modifier cannot be specified on both a variable and its type.
    Dim u? as Integer?
           ~~~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub MethodErrors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On        
Public Partial Class C
    Protected Private Sub m1()
    End Sub
    NotOverridable MustOverride Sub m2()
    NotOverridable Overridable Sub m3()
    End Sub
    Overridable Overrides Sub m4()
    End Sub
    Overridable Shared Sub m5()
    End Sub
    Public Function m6()
    End Function
    Public Function m7%() as string
    End Function
    Shadows Overloads Sub m8()
    End Sub
    Sub m9(Of T$)()
    End Sub
    Public MustInherit Sub m10()
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict Off        
Public Partial Class C
    Public Sub x$()
    End Sub
End Class
    ]]></file>
</compilation>, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            Dim expectedErrors = <errors><![CDATA[
BC36716: Visual Basic 15.0 does not support Private Protected.
    Protected Private Sub m1()
              ~~~~~~~
BC30177: Only one of 'NotOverridable', 'MustOverride', or 'Overridable' can be specified.
    NotOverridable MustOverride Sub m2()
                   ~~~~~~~~~~~~
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
    NotOverridable MustOverride Sub m2()
                                    ~~
BC30177: Only one of 'NotOverridable', 'MustOverride', or 'Overridable' can be specified.
    NotOverridable Overridable Sub m3()
                   ~~~~~~~~~~~
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
    NotOverridable Overridable Sub m3()
                                   ~~
BC30730: Methods declared 'Overrides' cannot be declared 'Overridable' because they are implicitly overridable.
    Overridable Overrides Sub m4()
                ~~~~~~~~~
BC30501: 'Shared' cannot be combined with 'Overridable' on a method declaration.
    Overridable Shared Sub m5()
    ~~~~~~~~~~~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
    Public Function m6()
                    ~~
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
    Public Function m7%() as string
                    ~~~
BC31408: 'Overloads' and 'Shadows' cannot be combined.
    Shadows Overloads Sub m8()
            ~~~~~~~~~
BC32041: Type character cannot be used in a type parameter declaration.
    Sub m9(Of T$)()
              ~~
BC30242: 'MustInherit' is not valid on a method declaration.
    Public MustInherit Sub m10()
           ~~~~~~~~~~~
BC30303: Type character cannot be used in a 'Sub' declaration because a 'Sub' doesn't return a value.
    Public Sub x$()
               ~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub MethodErrorsInInNotInheritableClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
NotInheritable Class c
    Overridable Sub s1()
    End Sub

    NotOverridable Sub s2()
    End Sub

    MustOverride Sub s3()

    Default Sub s4()
    End Sub

    Protected Sub s5()
    End Sub

    Protected Friend Sub s6()
    End Sub

    Overridable Sub New()
    End Sub

    NotOverridable Sub New(ByVal i1 As Integer)
    End Sub

    MustOverride Sub New(ByVal i1 As Integer, ByVal i2 As Integer)

    Default Sub s4(ByVal i1 As Integer, ByVal i2 As Integer, ByVal i3 As Integer)
    End Sub

    Protected Sub s5(ByVal i1 As Integer, ByVal i2 As Integer, ByVal i3 As Integer, ByVal i4 As Integer)
    End Sub

    Protected Friend Sub s6(ByVal i1 As Integer, ByVal i2 As Integer, ByVal i3 As Integer, ByVal i4 As Integer, ByVal i5 As Integer)
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30607: 'NotInheritable' classes cannot have members declared 'Overridable'.
    Overridable Sub s1()
    ~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'NotOverridable'.
    NotOverridable Sub s2()
    ~~~~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
    MustOverride Sub s3()
    ~~~~~~~~~~~~
BC30242: 'Default' is not valid on a method declaration.
    Default Sub s4()
    ~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'Overridable'.
    Overridable Sub New()
    ~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'NotOverridable'.
    NotOverridable Sub New(ByVal i1 As Integer)
    ~~~~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
    MustOverride Sub New(ByVal i1 As Integer, ByVal i2 As Integer)
    ~~~~~~~~~~~~
BC30242: 'Default' is not valid on a method declaration.
    Default Sub s4(ByVal i1 As Integer, ByVal i2 As Integer, ByVal i3 As Integer)
    ~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub MethodErrorsInInterfaces1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On        
Imports System.Collections.Generic

Public Interface i1
    Private Sub m1()
    Protected Sub m2()
    Friend Sub m3()
    Static Sub m4()
    Shared Sub m5()
    Shadows Sub m6()
    MustInherit Sub m7()
    Overloads Sub m8()
    NotInheritable Sub m9()
    Overrides Sub m10()
    NotOverridable Sub m11()
    MustOverride Sub m12()
    ReadOnly Sub m13()
    WriteOnly Sub m14()
    Dim Sub m15()
    Const Sub m16()
    Default Sub m17()
    WithEvents Sub m18()
    Widening Sub m19()
    Narrowing Sub m20()
    sub m21 implements IEnumerator.MoveNext
    sub m22 handles DownButton.Click
    Iterator Function I1() as IEnumerable(of Integer)
    End Interface
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[ 
BC30270: 'Private' is not valid on an interface method declaration.
    Private Sub m1()
    ~~~~~~~
BC30270: 'Protected' is not valid on an interface method declaration.
    Protected Sub m2()
    ~~~~~~~~~
BC30270: 'Friend' is not valid on an interface method declaration.
    Friend Sub m3()
    ~~~~~~
BC30242: 'Static' is not valid on a method declaration.
    Static Sub m4()
    ~~~~~~
BC30270: 'Shared' is not valid on an interface method declaration.
    Shared Sub m5()
    ~~~~~~
BC30242: 'MustInherit' is not valid on a method declaration.
    MustInherit Sub m7()
    ~~~~~~~~~~~
BC30242: 'NotInheritable' is not valid on a method declaration.
    NotInheritable Sub m9()
    ~~~~~~~~~~~~~~
BC30270: 'Overrides' is not valid on an interface method declaration.
    Overrides Sub m10()
    ~~~~~~~~~
BC30270: 'NotOverridable' is not valid on an interface method declaration.
    NotOverridable Sub m11()
    ~~~~~~~~~~~~~~
BC30270: 'MustOverride' is not valid on an interface method declaration.
    MustOverride Sub m12()
    ~~~~~~~~~~~~
BC30242: 'ReadOnly' is not valid on a method declaration.
    ReadOnly Sub m13()
    ~~~~~~~~
BC30242: 'WriteOnly' is not valid on a method declaration.
    WriteOnly Sub m14()
    ~~~~~~~~~
BC30242: 'Dim' is not valid on a method declaration.
    Dim Sub m15()
    ~~~
BC30242: 'Const' is not valid on a method declaration.
    Const Sub m16()
    ~~~~~
BC30242: 'Default' is not valid on a method declaration.
    Default Sub m17()
    ~~~~~~~
BC30242: 'WithEvents' is not valid on a method declaration.
    WithEvents Sub m18()
    ~~~~~~~~~~
BC30242: 'Widening' is not valid on a method declaration.
    Widening Sub m19()
    ~~~~~~~~
BC30242: 'Narrowing' is not valid on a method declaration.
    Narrowing Sub m20()
    ~~~~~~~~~
BC30270: 'implements' is not valid on an interface method declaration.
    sub m21 implements IEnumerator.MoveNext
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30270: 'handles' is not valid on an interface method declaration.
    sub m22 handles DownButton.Click
            ~~~~~~~~~~~~~~~~~~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    sub m22 handles DownButton.Click
                    ~~~~~~~~~~
BC30270: 'Iterator' is not valid on an interface method declaration.
    Iterator Function I1() as IEnumerable(of Integer)
    ~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ErrorTypeSymbol_DefaultMember_CodeCoverageItems()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Coverage">
    <file name="a.vb"><![CDATA[
Class A
    Dim n As B
End Class
    ]]></file>
</compilation>)

            Dim errs = compilation.GetDeclarationDiagnostics()

            Assert.Equal(1, Enumerable.Count(Of Diagnostic)(errs))
            Dim err As Diagnostic = Enumerable.Single(Of Diagnostic)(errs)
            Assert.Equal(&H7532, err.Code)
            Dim fieldSymb As FieldSymbol = DirectCast(compilation.GlobalNamespace.GetTypeMembers("A").Single.GetMembers.Item(1), FieldSymbol)
            Dim symbType As TypeSymbol = fieldSymb.Type

            Dim errTypeSym As ErrorTypeSymbol = DirectCast(symbType, ErrorTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, errTypeSym.Kind)
            Assert.Equal("B", errTypeSym.Name)
            Assert.Null(errTypeSym.ContainingAssembly)
            Assert.Null(errTypeSym.ContainingSymbol)
            Assert.Equal(Accessibility.Public, errTypeSym.DeclaredAccessibility)
            Assert.False(errTypeSym.IsShared)
            Assert.False(errTypeSym.IsMustOverride)
            Assert.False(errTypeSym.IsNotOverridable)
            Assert.False(errTypeSym.IsValueType)
            Assert.True(errTypeSym.IsReferenceType)
            Assert.Equal(SpecializedCollections.EmptyCollection(Of String), errTypeSym.MemberNames)
            Assert.Equal(ImmutableArray.Create(Of Symbol)(), errTypeSym.GetMembers)
            Assert.Equal(ImmutableArray.Create(Of Symbol)(), errTypeSym.GetMembers("B"))
            Assert.Equal(ImmutableArray.Create(Of NamedTypeSymbol)(), errTypeSym.GetTypeMembers)
            Assert.Equal(ImmutableArray.Create(Of NamedTypeSymbol)(), errTypeSym.GetTypeMembers("B"))
            Assert.Equal(ImmutableArray.Create(Of NamedTypeSymbol)(), errTypeSym.GetTypeMembers("B", 1))
            Assert.Equal(TypeKind.Error, errTypeSym.TypeKind)
            Assert.Equal(ImmutableArray.Create(Of Location)(), errTypeSym.Locations)
            Assert.Equal(ImmutableArray.Create(Of SyntaxReference)(), errTypeSym.DeclaringSyntaxReferences)
            Assert.Equal(0, errTypeSym.Arity)
            Assert.Null(errTypeSym.EnumUnderlyingType)
            Assert.Equal("B", errTypeSym.Name)
            Assert.Equal(0, errTypeSym.TypeArguments.Length)
            Assert.Equal(0, errTypeSym.TypeParameters.Length)
            Assert.Equal(errTypeSym.CandidateSymbols.Length, errTypeSym.IErrorTypeSymbol_CandidateSymbols.Length)
        End Sub


        <Fact>
        Public Sub ConstructorErrors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On        
Public Partial Class C
    Public Overridable Sub New(x as Integer)
    End Sub
    MustOverride Sub New(y as String)
    NotOverridable Friend Sub New(z as Object)
    End Sub
    Private Shadows Sub New(z as Object, a as integer)
    End Sub
    Protected Overloads Sub New(z as Object, a as string)
    End Sub
    Public Static Sub New(z as string, a as Object)
    End Sub
    Overrides Sub New()
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict Off        
Public Partial Class C
End Class

Public Interface I
    Sub New()
End Interface
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30364: 'Sub New' cannot be declared 'Overridable'.
    Public Overridable Sub New(x as Integer)
           ~~~~~~~~~~~
BC30364: 'Sub New' cannot be declared 'MustOverride'.
    MustOverride Sub New(y as String)
    ~~~~~~~~~~~~
BC30364: 'Sub New' cannot be declared 'NotOverridable'.
    NotOverridable Friend Sub New(z as Object)
    ~~~~~~~~~~~~~~
BC30364: 'Sub New' cannot be declared 'Shadows'.
    Private Shadows Sub New(z as Object, a as integer)
            ~~~~~~~
BC32040: The 'Overloads' keyword is used to overload inherited members; do not use the 'Overloads' keyword when overloading 'Sub New'.
    Protected Overloads Sub New(z as Object, a as string)
              ~~~~~~~~~
BC30242: 'Static' is not valid on a method declaration.
    Public Static Sub New(z as string, a as Object)
           ~~~~~~
BC30283: 'Sub New' cannot be declared 'Overrides'.
    Overrides Sub New()
    ~~~~~~~~~
BC30363: 'Sub New' cannot be declared in an interface.
    Sub New()
    ~~~~~~~~~
                                 ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub SharedConstructorErrors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On        
Public Class C
    Public Shared Sub New() '1
    End Sub
    Private Shared Sub New() '2
    End Sub
    Friend Shared Sub New() '3
    End Sub
    Shared Protected  Sub New() '4
    End Sub
    Shared Protected Friend Sub New() '5
    End Sub
    Shared Sub New(x as integer) '6
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict Off        
Public Module M
    Public Sub New() '8
    End Sub
    Private Sub New() '9
    End Sub
    Friend Sub New() '10
    End Sub
    Protected  Sub New() '11
    End Sub
    Protected Friend Sub New() '12
    End Sub
    Sub New(x as Integer, byref y as string) '13
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim expectedErrors =
<errors><![CDATA[
BC30480: Shared 'Sub New' cannot be declared 'Public'.
    Public Shared Sub New() '1
    ~~~~~~
BC30269: 'Private Shared Sub New()' has multiple definitions with identical signatures.
    Public Shared Sub New() '1
                      ~~~
BC30480: Shared 'Sub New' cannot be declared 'Private'.
    Private Shared Sub New() '2
    ~~~~~~~
BC30269: 'Private Shared Sub New()' has multiple definitions with identical signatures.
    Private Shared Sub New() '2
                       ~~~
BC30480: Shared 'Sub New' cannot be declared 'Friend'.
    Friend Shared Sub New() '3
    ~~~~~~
BC30269: 'Private Shared Sub New()' has multiple definitions with identical signatures.
    Friend Shared Sub New() '3
                      ~~~
BC30480: Shared 'Sub New' cannot be declared 'Protected'.
    Shared Protected  Sub New() '4
           ~~~~~~~~~
BC30269: 'Private Shared Sub New()' has multiple definitions with identical signatures.
    Shared Protected  Sub New() '4
                          ~~~
BC30480: Shared 'Sub New' cannot be declared 'Protected Friend'.
    Shared Protected Friend Sub New() '5
           ~~~~~~~~~~~~~~~~
BC30479: Shared 'Sub New' cannot have any parameters.
    Shared Sub New(x as integer) '6
                  ~~~~~~~~~~~~~~
BC30480: Shared 'Sub New' cannot be declared 'Public'.
    Public Sub New() '8
    ~~~~~~
BC30269: 'Private Sub New()' has multiple definitions with identical signatures.
    Public Sub New() '8
               ~~~
BC30480: Shared 'Sub New' cannot be declared 'Private'.
    Private Sub New() '9
    ~~~~~~~
BC30269: 'Private Sub New()' has multiple definitions with identical signatures.
    Private Sub New() '9
                ~~~
BC30480: Shared 'Sub New' cannot be declared 'Friend'.
    Friend Sub New() '10
    ~~~~~~
BC30269: 'Private Sub New()' has multiple definitions with identical signatures.
    Friend Sub New() '10
               ~~~
BC30433: Methods in a Module cannot be declared 'Protected'.
    Protected  Sub New() '11
    ~~~~~~~~~
BC30269: 'Private Sub New()' has multiple definitions with identical signatures.
    Protected  Sub New() '11
                   ~~~
BC30433: Methods in a Module cannot be declared 'Protected Friend'.
    Protected Friend Sub New() '12
    ~~~~~~~~~~~~~~~~
BC30479: Shared 'Sub New' cannot have any parameters.
    Sub New(x as Integer, byref y as string) '13
           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ParameterErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On        
Public Partial Class C
    Public Sub m1(x)
    End Sub
    Public Sub m2(byref byval k as Integer)
    End Sub
    Public Sub m3(byref byref k as Integer)
    End Sub
    Public Sub m4(ByRef ParamArray x() as string)
    End Sub
    Public Sub m5(ParamArray q as Integer)
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict Off        
Public Partial Class C
    Public Sub m8(x, y$)
    End Sub
    Public Sub m9(x, z As String, w)
    End Sub

End Class
    ]]></file>
</compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30211: Option Strict On requires that all method parameters have an 'As' clause.
    Public Sub m1(x)
                  ~
BC30641: 'ByVal' and 'ByRef' cannot be combined.
    Public Sub m2(byref byval k as Integer)
                        ~~~~~
BC30785: Parameter specifier is duplicated.
    Public Sub m3(byref byref k as Integer)
                        ~~~~~
BC30667: ParamArray parameters must be declared 'ByVal'.
    Public Sub m4(ByRef ParamArray x() as string)
                        ~~~~~~~~~~
BC30050: ParamArray parameter must be an array.
    Public Sub m5(ParamArray q as Integer)
                             ~
BC30529: All parameters must be explicitly typed if any of them are explicitly typed.
    Public Sub m8(x, y$)
                  ~
BC30529: All parameters must be explicitly typed if any of them are explicitly typed.
    Public Sub m9(x, z As String, w)
                  ~
BC30529: All parameters must be explicitly typed if any of them are explicitly typed.
    Public Sub m9(x, z As String, w)
                                  ~
                                 ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub MoreParameterErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MoreParameterErrors">
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function F1(f1 as integer) as Integer
        return 0
    End Function

    Function F1(i1 as integer, I1 as integer) as Integer
        return 0
    End Function

    Function F1(of T1) (i1 as integer, t1 as integer) as Integer
        return 0
    End Function

End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30530: Parameter cannot have the same name as its defining function.
    Function F1(f1 as integer) as Integer
                ~~
BC30237: Parameter already declared with name 'I1'.
    Function F1(i1 as integer, I1 as integer) as Integer
                               ~~
BC32089: 't1' is already declared as a type parameter of this method.
    Function F1(of T1) (i1 as integer, t1 as integer) as Integer
                                       ~~    
]]></expected>)
        End Sub

        <Fact>
        Public Sub LocalDeclarationSameAsFunctionNameError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LocalDeclarationSameAsFunctionNameError">
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function F1() as Integer
        dim f1 as integer = 0
        do
            dim f1 As integer = 0
        loop
        return 0
    End Function

End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30290: Local variable cannot have the same name as the function containing it.
        dim f1 as integer = 0
            ~~
BC30290: Local variable cannot have the same name as the function containing it.
            dim f1 As integer = 0
                ~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub DuplicateLocalDeclarationError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="DuplicateLocalDeclarationError">
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function F1() as Integer
        dim i as long = 0
        dim i as integer = 0
        dim j,j as integer
        return 0
    End Function

End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30288: Local variable 'i' is already declared in the current block.
        dim i as integer = 0
            ~
BC42024: Unused local variable: 'j'.
        dim j,j as integer
            ~
BC30288: Local variable 'j' is already declared in the current block.
        dim j,j as integer
              ~
BC42024: Unused local variable: 'j'.
        dim j,j as integer
              ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub LocalDeclarationTypeParameterError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="DuplicateLocalDeclarationError">
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function F1(of T)() as Integer
        dim t as integer = 0
        do
        dim t as integer = 0
        loop
        return 0
    End Function

End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC32089: 't' is already declared as a type parameter of this method.
        dim t as integer = 0
            ~
BC30616: Variable 't' hides a variable in an enclosing block.
        dim t as integer = 0
            ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub LocalDeclarationParameterError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="DuplicateLocalDeclarationError">
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function F1(p as integer) as Integer
        dim p as integer = 0
        do
            dim p as integer = 0
        loop 
        return 0
    End Function

End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30734: 'p' is already declared as a parameter of this method.
        dim p as integer = 0
            ~
BC30616: Variable 'p' hides a variable in an enclosing block.
            dim p as integer = 0
                ~
]]></expected>)
        End Sub

        ' Checks for duplicate type parameters
        <Fact>
        Public Sub DuplicateMethodTypeParameterErrors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
        Class BB(Of T)

            Class A(Of t1)
                Public t1 As Integer

                Sub f(Of t, t)()
                End Sub

            End Class

        End Class


        Class a(Of t)
            Inherits BB(Of t)
            Class b
                Class c(Of t)

                End Class
            End Class
        End Class

        Class base(Of T)
            Function TEST(Of T)(ByRef X As T)
                Return Nothing
            End Function
        End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32054: 't1' has the same name as a type parameter.
                Public t1 As Integer
                       ~~
BC40048: Type parameter 't' has the same name as a type parameter of an enclosing type. Enclosing type's type parameter will be shadowed.
                Sub f(Of t, t)()
                         ~
BC32049: Type parameter already declared with name 't'.
                Sub f(Of t, t)()
                            ~
BC40048: Type parameter 't' has the same name as a type parameter of an enclosing type. Enclosing type's type parameter will be shadowed.
                Class c(Of t)
                           ~
BC40048: Type parameter 'T' has the same name as a type parameter of an enclosing type. Enclosing type's type parameter will be shadowed.
            Function TEST(Of T)(ByRef X As T)
                             ~
     ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact, WorkItem(527182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527182")>
        Public Sub DuplicatedNameWithDifferentCases()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AAA">
    <file name="a.vb"><![CDATA[
Module Module1
    Dim S1 As String
    Dim s1 As String
    Dim S1 As Integer

    Function MyFunc() As Integer
        Return 0
    End Function
    Function MYFUNC() As Integer
        Return 0
    End Function

    Sub Main()
    End Sub
End Module

Namespace NS
    Partial Public Class AAA
        Public BBB As Integer
        Friend BBb As Integer

        Sub SSS()
        End Sub
    End Class
    Partial Public Class AaA
        Public Structure ST1
            Shared CH1, ch1 As Char
        End Structure
        Structure st1
            Shared ch1, ch2 As Char
        End Structure
    End Class
End Namespace
    ]]></file>
    <file name="b.vb"><![CDATA[
Namespace Ns
    Partial Public Class Aaa
        Private Bbb As Integer

        Sub Sss()
        End Sub
    End Class
End Namespace
    ]]></file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim myMod = DirectCast(globalNS.GetMembers("module1").Single(), NamedTypeSymbol)
            ' no merge for non-namespace/type members
            Dim mem1 = myMod.GetMembers("S1")
            Assert.Equal(3, mem1.Length) ' 3
            mem1 = myMod.GetMembers("myfunc")
            Assert.Equal(2, mem1.Length) ' 2

            Dim myNS = DirectCast(globalNS.GetMembers("ns").Single(), NamespaceSymbol)
            Dim types = myNS.GetMembers("aaa")
            Assert.Equal(1, types.Length)
            Dim type1 = DirectCast(types.First(), NamedTypeSymbol)
            ' no merge for fields
            Dim mem2 = type1.GetMembers("bbb")
            Assert.Equal(3, mem2.Length) ' 3
            Dim mem3 = type1.GetMembers("sss")
            Assert.Equal(2, mem3.Length) ' 2
            Dim mem4 = type1.GetMembers("St1")
            Assert.Equal(1, mem4.Length)
            Dim type2 = DirectCast(mem4.First(), NamedTypeSymbol)
            ' from both St1
            Dim mem5 = type2.GetMembers("Ch1")
            Assert.Equal(3, mem2.Length) ' 3

            Dim errs = compilation.GetDeclarationDiagnostics()
            ' Native compilers 9 errors
            Assert.True(errs.Length > 6, "Contain Decl Errors")
        End Sub

        <WorkItem(537443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537443")>
        <Fact>
        Public Sub DuplicatedTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub sub1()
        Dim element1 = <RootEle>
        </RootEle>
    End Sub
End Module

'COMPILEERROR: BC30179, "Module1"
Module Module1
    Sub sub1()
        Dim element1 = <RootEle>
        </RootEle>
    End Sub
End Module

Namespace GenArityErr001

    Public Structure ga001Str2 (Of T As Integer)
       Dim i As Integer
    End Structure

    ' BC30179: Name conflict
    ' COMPILEERROR: BC30179, "ga001Str2"
    Public Structure ga001Str2 (Of X As New)
       Dim i As Integer
    End Structure
End Namespace
    ]]></file>
</compilation>)

            Dim globalNS = compilation.Assembly.GlobalNamespace
            Dim modOfNS = DirectCast(globalNS.GetMembers("Module1").Single(), NamedTypeSymbol)
            Dim mem1 = DirectCast(modOfNS.GetMembers().First(), MethodSymbol)
            Assert.Equal("sub1", mem1.Name)

            Dim ns = DirectCast(globalNS.GetMembers("GenArityErr001").First(), NamespaceSymbol)
            Dim type1 = DirectCast(ns.GetMembers("ga001Str2").First(), NamedTypeSymbol)
            Assert.NotEmpty(type1.GetMembers().AsEnumerable())

        End Sub

        <WorkItem(537443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537443")>
        <Fact>
        Public Sub InvalidPartialTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit On
Namespace PartialStruct118
    Interface ii
        Sub abc()
    End Interface
    'COMPILEWARNING: BC40046, "teststruct"
    Structure teststruct
        Implements ii

        'COMPILEERROR: BC30269, "abc"
        Public Sub abc() Implements ii.abc
        End Sub
        Public Scen5 As String
        'COMPILEERROR: BC30269, "Scen6"
        Public Sub Scen6(ByVal x As String)
        End Sub
        'COMPILEERROR: BC30269, "New"
        Public Sub New(ByVal x As String)
        End Sub
    End Structure
    'COMPILEERROR: BC32200, "teststruct"
    partial Shadows Structure teststruct

    End Structure
    'COMPILEERROR: BC30395, "MustInherit"
    partial MustInherit Structure teststruct

    End Structure
    'COMPILEERROR: BC30395, "NotInheritable"
    partial NotInheritable Structure teststruct

    End Structure
    'COMPILEERROR: BC30178, "partial"
    partial partial structure teststruct
    End Structure

    partial Structure teststruct
        'COMPILEERROR: BC30260, "Scen5"
        Public Scen5 As String

        Public Sub Scen6(ByVal x As String)
        End Sub
    End Structure

    partial Structure teststruct
        'COMPILEERROR: BC30628, "Inherits Scen7"
        Inherits scen7
    End Structure

    partial Structure teststruct
        Implements ii
        Public Sub New(ByVal x As String)
        End Sub
        Public Sub abc() Implements ii.abc
        End Sub
    End Structure
        'COMPILEWARNING: BC40046, "teststruct"
    Structure teststruct
        Dim a As String
    End Structure

End Namespace

    ]]></file>
</compilation>)

            Dim globalNS = compilation.Assembly.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("PartialStruct118").Single(), NamespaceSymbol)
            Dim type1 = DirectCast(ns.GetTypeMembers("teststruct").First(), NamedTypeSymbol)
            Assert.Equal("teststruct", type1.Name)
            Assert.NotEmpty(type1.GetMembers().AsEnumerable)

        End Sub

        <WorkItem(537680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537680")>
        <Fact>
        Public Sub ModuleWithTypeParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="Err">
                    <file name="a.vb"><![CDATA[
Namespace Regression139822
    'COMPILEERROR: BC32073, "(of T)"
    Module Module1(of T)
    End Module
End Namespace
                    ]]></file>
                </compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("Regression139822").Single(), NamespaceSymbol)
            Dim myMod = DirectCast(ns.GetMembers("Module1").SingleOrDefault(), NamedTypeSymbol)
            Assert.Equal(0, myMod.TypeParameters.Length)
            Assert.Equal(0, myMod.Arity)
        End Sub

        <Fact>
        Public Sub Bug4577()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
               <compilation name="Bug4577">
                   <file name="a.vb"><![CDATA[
Namespace A.B
End Namespace

Namespace A
  Class B
  End Class

  Class B(Of T)
  End Class
End Namespace

Class C
    Class D
    End Class

    Public d As Integer

    Class E(Of T)
    End Class

    Public e As Integer

    Class D '2
    End Class
End Class
                    ]]></file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30179: class 'B' and namespace 'B' conflict in namespace 'A'.
  Class B
        ~
BC30260: 'd' is already declared as 'Class D' in this class.
    Public d As Integer
           ~
BC30260: 'e' is already declared as 'Class E(Of T)' in this class.
    Public e As Integer
           ~
BC30179: class 'D' and class 'D' conflict in class 'C'.
    Class D '2
          ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub Bug4054()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Bug4054">
    <file name="a.vb"><![CDATA[
        Module Program
            Sub Main()
                Dim x(0# To 2)
            End Sub
        End Module        
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected><![CDATA[
        BC32059: Array lower bounds can be only '0'.
                Dim x(0# To 2)
                      ~~
    ]]></expected>)

        End Sub

        <WorkItem(537507, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537507")>
        <Fact>
        Public Sub ReportErrorTypeCharacterInTypeNameDeclaration()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ReportErrorTypeCharacterInTypeNameDeclaration">
    <file name="a.vb"><![CDATA[
Namespace n1#

Class C1#
End Class

Class c2#(of T)
end class

Enum e1#
    dummy
end enum

Structure s1#
End structure

End Namespace

Module Program#
End Module
]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC30468: Type declaration characters are not valid in this context.
Namespace n1#
          ~~~
BC30468: Type declaration characters are not valid in this context.
Class C1#
      ~~~
BC30468: Type declaration characters are not valid in this context.
Class c2#(of T)
      ~~~
BC30468: Type declaration characters are not valid in this context.
Enum e1#
     ~~~
BC30468: Type declaration characters are not valid in this context.
Structure s1#
          ~~~
BC30468: Type declaration characters are not valid in this context.
Module Program#
       ~~~~~~~~                                                           
 ]]></errors>)
        End Sub

        <WorkItem(537507, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537507")>
        <Fact>
        Public Sub ReportErrorTypeCharacterInTypeName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ReportErrorTypeCharacterInTypeName">
    <file name="a.vb"><![CDATA[
Namespace n1

Class C1#
End Class

Class c2#(of T)
    public f1 as c1#
end class

End Namespace

Module Program
    Dim x1 as C1#
    dim x2 as N1.C1#
    dim x3 as n1.c2#(of integer)
End Module
]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC30468: Type declaration characters are not valid in this context.
Class C1#
      ~~~
BC30468: Type declaration characters are not valid in this context.
Class c2#(of T)
      ~~~
BC30468: Type declaration characters are not valid in this context.
    public f1 as c1#
                 ~~~
BC30002: Type 'C1' is not defined.
    Dim x1 as C1#
              ~~~
BC30468: Type declaration characters are not valid in this context.
    Dim x1 as C1#
              ~~~
BC30468: Type declaration characters are not valid in this context.
    dim x2 as N1.C1#
                 ~~~
BC30468: Type declaration characters are not valid in this context.
    dim x3 as n1.c2#(of integer)
                 ~~~                                               
 ]]></errors>)
        End Sub

        <WorkItem(540895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540895")>
        <Fact>
        Public Sub BC31538ERR_FriendAssemblyBadAccessOverride2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ClassLibrary1">
    <file name="a.vb"><![CDATA[
Imports System

Public Class A
    Protected Friend Overridable Sub G()
        Console.WriteLine("A.G")
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="ConsoleApp">
    <file name="c.vb"><![CDATA[
Imports System
Imports ClassLibrary1

MustInherit Class B
    Inherits A
    Protected Friend Overrides Sub G()
    End Sub
End Class
    ]]></file>
</compilation>, {New VisualBasicCompilationReference(compilation1)})

            CompilationUtils.AssertTheseDiagnostics(compilation2, <errors><![CDATA[
BC40056: Namespace or type specified in the Imports 'ClassLibrary1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports ClassLibrary1
        ~~~~~~~~~~~~~
BC31538: Member 'Protected Friend Overrides Sub G()' cannot override member 'Protected Friend Overridable Sub G()' defined in another assembly/project because the access modifier 'Protected Friend' expands accessibility. Use 'Protected' instead.
    Protected Friend Overrides Sub G()
                                   ~
]]></errors>)
        End Sub

        ' Note that the candidate symbols infrastructure on error types is tested heavily in 
        ' the SemanticModel tests. The following tests just make sure the public
        ' API is working correctly.

        Public Sub ErrorTypeCandidateSymbols1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
 <compilation name="ErrorType">
     <file name="a.vb"><![CDATA[
Option Strict On
Class A
    Dim n As B
End Class
     ]]></file>
 </compilation>)

            Dim classA = DirectCast(compilation.GlobalNamespace.GetTypeMembers("A").Single(), NamedTypeSymbol)
            Dim fsym = DirectCast(classA.GetMembers("n").First(), FieldSymbol)
            Dim typ = fsym.Type
            Assert.Equal(SymbolKind.ErrorType, typ.Kind)
            Assert.Equal("B", typ.Name)

            Dim errortype = DirectCast(typ, ErrorTypeSymbol)
            Assert.Equal(CandidateReason.None, errortype.CandidateReason)
            Assert.Equal(0, errortype.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub ErrorTypeCandidateSymbols2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
 <compilation name="ErrorType">
     <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Private Class B
    End Class
End Class
Class A
    Inherits C
    Dim n As B
End Class
     ]]></file>
 </compilation>)

            Dim classA = DirectCast(compilation.GlobalNamespace.GetTypeMembers("A").Single(), NamedTypeSymbol)
            Dim classB = DirectCast(DirectCast(compilation.GlobalNamespace.GetTypeMembers("C").Single(), NamedTypeSymbol).GetTypeMembers("B").Single, NamedTypeSymbol)

            Dim fsym = DirectCast(classA.GetMembers("n").First(), FieldSymbol)
            Dim typ = fsym.Type
            Assert.Equal(SymbolKind.ErrorType, typ.Kind)
            Assert.Equal("B", typ.Name)

            Dim errortyp = DirectCast(typ, ErrorTypeSymbol)
            Assert.Equal(CandidateReason.Inaccessible, errortyp.CandidateReason)
            Assert.Equal(1, errortyp.CandidateSymbols.Length)
            Assert.Equal(classB, errortyp.CandidateSymbols(0))
        End Sub

        <Fact()>
        Public Sub ErrorTypeCandidateSymbols3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
 <compilation name="ErrorType">
     <file name="a.vb"><![CDATA[
Option Strict On
Imports N1, N2
Namespace N1
    Class B
    End Class
End Namespace
Namespace N2
    Class B
    End Class
End Namespace
Class A
    Dim n As B
End Class
     ]]></file>
 </compilation>)

            Dim classA = DirectCast(compilation.GlobalNamespace.GetTypeMembers("A").Single(), NamedTypeSymbol)
            Dim classB1 = DirectCast(DirectCast(compilation.GlobalNamespace.GetMembers("N1").Single(), NamespaceSymbol).GetTypeMembers("B").Single, NamedTypeSymbol)
            Dim classB2 = DirectCast(DirectCast(compilation.GlobalNamespace.GetMembers("N2").Single(), NamespaceSymbol).GetTypeMembers("B").Single, NamedTypeSymbol)

            Dim fsym = DirectCast(classA.GetMembers("n").First(), FieldSymbol)
            Dim typ = fsym.Type
            Assert.Equal(SymbolKind.ErrorType, typ.Kind)
            Assert.Equal("B", typ.Name)

            Dim errortyp = DirectCast(typ, ErrorTypeSymbol)
            Assert.Equal(CandidateReason.Ambiguous, errortyp.CandidateReason)
            Assert.Equal(2, errortyp.CandidateSymbols.Length)
            Assert.True((TypeSymbol.Equals(classB1, TryCast(errortyp.CandidateSymbols(0), TypeSymbol), TypeCompareKind.ConsiderEverything) AndAlso
                        TypeSymbol.Equals(classB2, TryCast(errortyp.CandidateSymbols(1), TypeSymbol), TypeCompareKind.ConsiderEverything)) OrElse
                        (TypeSymbol.Equals(classB2, TryCast(errortyp.CandidateSymbols(0), TypeSymbol), TypeCompareKind.ConsiderEverything) AndAlso
                        TypeSymbol.Equals(classB1, TryCast(errortyp.CandidateSymbols(1), TypeSymbol), TypeCompareKind.ConsiderEverything)), "should have B1 and B2 in some order")
        End Sub

        <Fact()>
        Public Sub TypeArgumentOfFriendType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
 <compilation name="E">
     <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("goo")>
Friend Class ImmutableStack(Of T)
End Class
     ]]></file>
 </compilation>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
 <compilation name="goo">
     <file name="a.vb"><![CDATA[
Friend Class Scanner

  Protected Class ConditionalState
  End Class

  Protected Class PreprocessorState
    Friend ReadOnly _conditionals As ImmutableStack(Of ConditionalState)
  End Class

End Class
     ]]></file>
 </compilation>, {New VisualBasicCompilationReference(compilation)})

            Assert.Empty(compilation2.GetDiagnostics())
        End Sub

        <Fact, WorkItem(544071, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544071")>
        Public Sub ProtectedTypeExposureGeneric()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
 <compilation name="E">
     <file name="a.vb"><![CDATA[
Friend Class B(Of T)
  Protected Enum ENM
       None
  End Enum
End Class

Class D
  Inherits B(Of Integer)
  Protected Sub proc(p1 As ENM)
  End Sub
  Protected Sub proc(p1 As B(Of Long).ENM)
  End Sub
End Class
     ]]></file>
 </compilation>)

            Dim diags = compilation.GetDiagnostics()
            Assert.Empty(diags)

        End Sub

        <Fact, WorkItem(574771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/574771")>
        Public Sub Bug574771()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Interface vbGI6504Int1(Of X)
End Interface
Public Interface vbGI6504Int2(Of T, U)
    Function Fun2(ByVal p1 As T, ByVal t2 As U) As Long
End Interface
Public Interface vbGI6504Int3(Of T)
    Inherits vbGI6504Int1(Of T)
Inherits vbGI6504Int2(O T, T) ' f is removed from Of
End Interface
Public Class vbGI6504Cls1(Of X)
    Implements vbGI6504Int3(Of X)
    Public Function Fun2(ByVal p1 As X, ByVal t2 As X) As Long Implements vbGI6504Int2(Of X, X).Fun2
        Return 12000L
    End Function
End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC32042: Too few type arguments to 'vbGI6504Int2(Of T, U)'.
Inherits vbGI6504Int2(O T, T) ' f is removed from Of
         ~~~~~~~~~~~~~~~
BC32093: 'Of' required when specifying type arguments for a generic type or method.
Inherits vbGI6504Int2(O T, T) ' f is removed from Of
                      ~
BC30002: Type 'O' is not defined.
Inherits vbGI6504Int2(O T, T) ' f is removed from Of
                      ~
BC30198: ')' expected.
Inherits vbGI6504Int2(O T, T) ' f is removed from Of
                        ~
BC32055: Interface 'vbGI6504Int3' cannot inherit from a type parameter.
Inherits vbGI6504Int2(O T, T) ' f is removed from Of
                           ~
BC31035: Interface 'vbGI6504Int2(Of X, X)' is not implemented by this class.
    Public Function Fun2(ByVal p1 As X, ByVal t2 As X) As Long Implements vbGI6504Int2(Of X, X).Fun2
                                                                          ~~~~~~~~~~~~~~~~~~~~~
     ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact, WorkItem(578723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578723")>
        Public Sub Bug578723()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I(Of T)
    Sub Goo(Optional x As Integer = Nothing)
End Interface
 
Class C
    Implements I(Of Integer)
 
    Public Sub Goo(Optional x As Integer = 0) Implements (Of Integer).Goo
    End Sub
End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC30149: Class 'C' must implement 'Sub Goo([x As Integer = 0])' for interface 'I(Of Integer)'.
    Implements I(Of Integer)
               ~~~~~~~~~~~~~
BC30203: Identifier expected.
    Public Sub Goo(Optional x As Integer = 0) Implements (Of Integer).Goo
                                                         ~
                                  ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

        End Sub

        <WorkItem(783920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/783920")>
        <Fact()>
        Public Sub Bug783920()
            Dim comp1 = CreateCompilationWithMscorlib40(
    <compilation name="Bug783920_VB">
        <file name="a.vb"><![CDATA[
Public Class MyAttribute1
    Inherits System.Attribute
End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll)

            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class MyAttribute2
    Inherits MyAttribute1
End Class
        ]]></file>
    </compilation>, {New VisualBasicCompilationReference(comp1)}, TestOptions.ReleaseDll)

            Dim source3 =
    <compilation>
        <file name="a.vb"><![CDATA[
<MyAttribute2>
Public Class Test
End Class
        ]]></file>
    </compilation>

            Dim expected =
<expected><![CDATA[
BC30652: Reference required to assembly 'Bug783920_VB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'MyAttribute1'. Add one to your project.
<MyAttribute2>
 ~~~~~~~~~~~~
]]></expected>

            Dim comp4 = CreateCompilationWithMscorlib40AndReferences(source3, {comp2.EmitToImageReference()}, TestOptions.ReleaseDll)
            AssertTheseDiagnostics(comp4, expected)

            Dim comp3 = CreateCompilationWithMscorlib40AndReferences(source3, {New VisualBasicCompilationReference(comp2)}, TestOptions.ReleaseDll)
            AssertTheseDiagnostics(comp3, expected)
        End Sub

        <Fact()>
        Public Sub BC30166ERR_ExpectedNewableClass1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Friend Module AttrRegress006mod

    ' Need this attribute
    <System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)>
    Class test
    End Class

    Sub AttrRegress006()
        'COMPILEERROR: BC30166, "Test" EDMAURER no longer giving this error.
        Dim c As New test
    End Sub
End Module]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact, WorkItem(528709, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528709")>
        Public Sub Bug528709()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Enum TestEnum
    One
    One
End Enum
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC31421: 'One' is already declared in this enum.
    One
    ~~~
                                  ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact, WorkItem(529327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529327")>
        Public Sub Bug529327()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I1
    Property Bar As Integer
    Sub Goo()
End Interface
Interface I2
    Inherits I1
    Property Bar As Integer
    Sub Goo()
End Interface
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC40003: property 'Bar' shadows an overloadable member declared in the base interface 'I1'.  If you want to overload the base method, this method must be declared 'Overloads'.
    Property Bar As Integer
             ~~~
BC40003: sub 'Goo' shadows an overloadable member declared in the base interface 'I1'.  If you want to overload the base method, this method must be declared 'Overloads'.
    Sub Goo()
        ~~~
                                  ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact, WorkItem(531353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531353")>
        Public Sub Bug531353()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class c1
    Shared Operator +(ByVal x As c1, ByVal y As c2) As Integer
        Return 0
    End Operator
End Class
Class c2
    Inherits c1
    'COMPILEWARNING: BC40003, "+"
    Shared Operator +(ByVal x As c1, ByVal y As c2) As Integer
        Return 0
    End Operator
End Class
        ]]></file>
    </compilation>)

            Dim expectedErrors1 = <errors><![CDATA[
BC40003: operator 'op_Addition' shadows an overloadable member declared in the base class 'c1'.  If you want to overload the base method, this method must be declared 'Overloads'.
    Shared Operator +(ByVal x As c1, ByVal y As c2) As Integer
                    ~
                                  ]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)

        End Sub

        <Fact, WorkItem(1068209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068209")>
        Public Sub Bug1068209_01()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I1
    ReadOnly Property P1 As Integer
    Sub get_P2()

    Event E1 As System.Action
    Sub remove_E2()
End Interface

Interface I3
    Inherits I1
    Overloads Sub get_P1()
    Overloads Property P2 As Integer

    Overloads Sub add_E1()
    Event E2 As System.Action
End Interface
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40014: sub 'get_P1' conflicts with a member implicitly declared for property 'P1' in the base interface 'I1' and should be declared 'Shadows'.
    Overloads Sub get_P1()
                  ~~~~~~
BC40012: property 'P2' implicitly declares 'get_P2', which conflicts with a member in the base interface 'I1', and so the property should be declared 'Shadows'.
    Overloads Property P2 As Integer
                       ~~
BC40014: sub 'add_E1' conflicts with a member implicitly declared for event 'E1' in the base interface 'I1' and should be declared 'Shadows'.
    Overloads Sub add_E1()
                  ~~~~~~
BC40012: event 'E2' implicitly declares 'remove_E2', which conflicts with a member in the base interface 'I1', and so the event should be declared 'Shadows'.
    Event E2 As System.Action
          ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact, WorkItem(1068209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068209")>
        Public Sub Bug1068209_02()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Class I1
    ReadOnly Property P1 As Integer
        Get
            return Nothing
        End Get
    End Property

    Sub get_P2()
    End Sub

    Event E1 As System.Action

    Sub remove_E2()
    End Sub
End Class

Class I3
    Inherits I1

    Overloads Sub get_P1()
    End Sub

    Overloads ReadOnly Property P2 As Integer
        Get
            return Nothing
        End Get
    End Property

    Overloads Sub add_E1()
    End Sub

    Event E2 As System.Action
End Class
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC40014: sub 'get_P1' conflicts with a member implicitly declared for property 'P1' in the base class 'I1' and should be declared 'Shadows'.
    Overloads Sub get_P1()
                  ~~~~~~
BC40012: property 'P2' implicitly declares 'get_P2', which conflicts with a member in the base class 'I1', and so the property should be declared 'Shadows'.
    Overloads ReadOnly Property P2 As Integer
                                ~~
BC40014: sub 'add_E1' conflicts with a member implicitly declared for event 'E1' in the base class 'I1' and should be declared 'Shadows'.
    Overloads Sub add_E1()
                  ~~~~~~
BC40012: event 'E2' implicitly declares 'remove_E2', which conflicts with a member in the base class 'I1', and so the event should be declared 'Shadows'.
    Event E2 As System.Action
          ~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub NoObsoleteDiagnosticsForProjectLevelImports_01()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"GlobEnumsClass"}))
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
<System.Serializable><System.Obsolete()> 
Class GlobEnumsClass

    Public Enum xEmailMsg
        Option1
        Option2
    End Enum

End Class

Class Account
    Property Status() As xEmailMsg
End Class
        ]]></file>
    </compilation>, options:=options)

            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub NoObsoleteDiagnosticsForProjectLevelImports_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports GlobEnumsClass

<System.Serializable><System.Obsolete()> 
Class GlobEnumsClass

    Public Enum xEmailMsg
        Option1
        Option2
    End Enum

End Class

Class Account
    Property Status() As xEmailMsg
End Class
        ]]></file>
    </compilation>, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(<expected>
BC40008: 'GlobEnumsClass' is obsolete.
Imports GlobEnumsClass
        ~~~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact>
        Public Sub MustOverrideInScript()
            Dim source = <![CDATA[
Friend MustOverride Function F() As Object
Friend MustOverride ReadOnly Property P
]]>
            Dim comp = CreateCompilationWithMscorlib45(
                {VisualBasicSyntaxTree.ParseText(source.Value, TestOptions.Script)},
                references:={SystemCoreRef})
            comp.AssertTheseDiagnostics(<expected>
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
Friend MustOverride Function F() As Object
       ~~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
Friend MustOverride ReadOnly Property P
       ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub MustOverrideInInteractive()
            Dim source = <![CDATA[
Friend MustOverride Function F() As Object
Friend MustOverride ReadOnly Property P
]]>
            Dim submission = VisualBasicCompilation.CreateScriptCompilation(
                "s0.dll",
                syntaxTree:=Parse(source.Value, TestOptions.Script),
                references:={MscorlibRef, SystemCoreRef})
            submission.AssertTheseDiagnostics(<expected>
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
Friend MustOverride Function F() As Object
       ~~~~~~~~~~~~
BC30607: 'NotInheritable' classes cannot have members declared 'MustOverride'.
Friend MustOverride ReadOnly Property P
       ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub MultipleForwardsOfATypeToDifferentAssembliesWithoutUsingItShouldNotReportAnError()
            Dim forwardingIL = "
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly Forwarding
{
}

.module Forwarding.dll

.assembly extern Destination1
{
}
.assembly extern Destination2
{
}

.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}

.class public auto ansi beforefieldinit TestSpace.ExistingReference
       extends [mscorlib]System.Object
{
  .field public static literal string Value = ""TEST VALUE""
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
        {
            // Code size       8 (0x8)
            .maxstack  8
            IL_0000:  ldarg.0
            IL_0001:  call instance void[mscorlib] System.Object::.ctor()
            IL_0006:  nop
            IL_0007:  ret
        }
}"
            Dim ilReference = CompileIL(forwardingIL, prependDefaultHeader:=False)

            Dim code =
    <compilation>
        <file name="a.vb"><![CDATA[
Imports TestSpace
Namespace UserSpace
    Public Class Program
        Public Shared Sub Main()
            System.Console.WriteLine(ExistingReference.Value)
        End Sub
    End Class
End Namespace
        ]]></file>
    </compilation>

            CompileAndVerify(
                source:=code,
                references:={ilReference},
                expectedOutput:="TEST VALUE")
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub MultipleForwardsOfFullyQualifiedTypeToDifferentAssembliesWhileReferencingItShouldErrorOut()
            Dim userCode =
    <compilation>
        <file name="a.vb"><![CDATA[
Namespace ForwardingNamespace
    Public Class Program
        Public Shared Sub Main()
            Dim obj = New Destination.TestClass()
        End Sub
    End Class
End Namespace
        ]]></file>
    </compilation>

            Dim forwardingIL = "
.assembly extern Destination1
{
    .ver 1:0:0:0
}
.assembly extern Destination2
{
    .ver 1:0:0:0
}
.assembly Forwarder
{
    .ver 1:0:0:0
}
.module ForwarderModule.dll
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}"
            Dim compilation = CreateCompilationWithCustomILSource(userCode, forwardingIL, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC30002: Type 'Destination.TestClass' is not defined.
            Dim obj = New Destination.TestClass()
                          ~~~~~~~~~~~~~~~~~~~~~
BC37208: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Destination.TestClass' to multiple assemblies: 'Destination1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' and 'Destination2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
            Dim obj = New Destination.TestClass()
                          ~~~~~~~~~~~~~~~~~~~~~
 ]]></errors>)
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub MultipleForwardsToManyAssembliesShouldJustReportTheFirstTwo()

            Dim userCode =
    <compilation>
        <file name="a.vb"><![CDATA[
Namespace ForwardingNamespace
    Public Class Program
        Public Shared Sub Main()
            Dim obj = New Destination.TestClass()
        End Sub
    End Class
End Namespace
        ]]></file>
    </compilation>

            Dim forwardingIL = "
.assembly Forwarder
{
}
.module ForwarderModule.dll
.assembly extern Destination1 { }
.assembly extern Destination2 { }
.assembly extern Destination3 { }
.assembly extern Destination4 { }
.assembly extern Destination5 { }
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination3
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination4
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination5
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}"

            Dim compilation = CreateCompilationWithCustomILSource(userCode, forwardingIL, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC30002: Type 'Destination.TestClass' is not defined.
            Dim obj = New Destination.TestClass()
                          ~~~~~~~~~~~~~~~~~~~~~
BC37208: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Destination.TestClass' to multiple assemblies: 'Destination1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'Destination2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            Dim obj = New Destination.TestClass()
                          ~~~~~~~~~~~~~~~~~~~~~
 ]]></errors>)
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub RequiredExternalTypesForAMethodSignatureWillReportErrorsIfForwardedToMultipleAssemblies()
            ' The scenario Is that assembly A Is calling a method from assembly B. This method has a parameter of a type that lives
            ' in assembly C. If A Is compiled against B And C, it should compile successfully.
            ' Now if assembly C Is replaced with assembly C2, that forwards the type to both D1 And D2, it should fail with the appropriate error.

            Dim codeC = "
Namespace C
    Public Class ClassC
    End Class
End Namespace"

            Dim referenceC = CreateCompilationWithMscorlib40(
                source:=codeC,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="C").EmitToImageReference()

            Dim codeB = "
Imports C

Namespace B
    Public Class ClassB
        Public Shared Sub MethodB(obj As ClassC)
            System.Console.WriteLine(obj.GetHashCode())
        End Sub
    End Class
End Namespace"

            Dim compilationB = CreateCompilationWithMscorlib40(
                source:=codeB,
                references:={referenceC},
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="B")

            Dim referenceB = compilationB.EmitToImageReference()

            Dim codeA = "
Imports B

Namespace A
    Public Class ClassA
        Public Sub MethodA()
            ClassB.MethodB(Nothing)
        End Sub
    End Class
End Namespace"

            Dim compilation = CreateCompilationWithMscorlib40(
                source:=codeA,
                references:={referenceB, referenceC},
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="A")

            compilation.VerifyDiagnostics() ' No Errors

            Dim codeC2 = "
.assembly C { }
.module CModule.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder C.ClassC
{
	.assembly extern D1
}
.class extern forwarder C.ClassC
{
	.assembly extern D2
}"

            Dim referenceC2 = CompileIL(codeC2, prependDefaultHeader:=False)

            compilation = CreateCompilationWithMscorlib40(
                source:=codeA,
                references:={referenceB, referenceC2},
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="A")

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC37208: Module 'CModule.dll' in assembly 'C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'C.ClassC' to multiple assemblies: 'D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            ClassB.MethodB(Nothing)
            ~~~~~~~~~~~~~~~~~~~~~~~
 ]]></errors>)
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub MultipleTypeForwardersToTheSameAssemblyShouldNotResultInMultipleForwardError()
            Dim codeC = "
Namespace C
    Public Class ClassC
    End Class
End Namespace"
            Dim compilationC = CreateCompilationWithMscorlib40(
                source:=codeC,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="C")

            Dim referenceC = compilationC.EmitToImageReference()

            Dim codeB = "
Imports C

Namespace B
    Public Class ClassB
        Public Shared Function MethodB(obj As ClassC) As String
            Return ""obj is "" + If(obj Is Nothing, ""nothing"", obj.ToString())
        End Function
    End Class
End Namespace"
            Dim compilationB = CreateCompilationWithMscorlib40(
                source:=codeB,
                references:={referenceC},
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="B")

            Dim referenceB = compilationB.EmitToImageReference()

            Dim codeA =
    <compilation>
        <file name="a.vb"><![CDATA[
Imports B

Namespace A
    Public Class ClassA
        Public Shared Sub Main()
            System.Console.WriteLine(ClassB.MethodB(Nothing))
        End Sub
    End Class
End Namespace
        ]]></file>
    </compilation>

            CompileAndVerify(
                source:=codeA,
                references:={referenceB, referenceC},
                expectedOutput:="obj is nothing")

            Dim codeC2 = "
.assembly C
{
	.ver 0:0:0:0
}
.module C.dll
.assembly extern D { }
.class extern forwarder C.ClassC
{
	.assembly extern D
}
.class extern forwarder C.ClassC
{
	.assembly extern D
}"

            Dim referenceC2 = CompileIL(codeC2, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40(codeA, references:={referenceB, referenceC2})

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC30652: Reference required to assembly 'D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'ClassC'. Add one to your project.
            System.Console.WriteLine(ClassB.MethodB(Nothing))
                                     ~~~~~~~~~~~~~~~~~~~~~~~
 ]]></errors>)

            Dim codeD = "
Namespace C
    Public Class ClassC
    End Class
End Namespace"
            Dim referenceD = CreateCompilationWithMscorlib40(
                source:=codeD,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="D").EmitToImageReference()

            CompileAndVerify(
                source:=codeA,
                references:={referenceB, referenceC2, referenceD},
                expectedOutput:="obj is nothing")
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub CompilingModuleWithMultipleForwardersToDifferentAssembliesShouldErrorOut()
            Dim ilSource = "
.module ForwarderModule.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D1
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D2
}"

            Dim ilModule = GetILModuleReference(ilSource, prependDefaultHeader:=False)
            Dim compilation = CreateCompilationWithMscorlib40(
                source:=String.Empty,
                references:={ilModule},
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="Forwarder")

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC37208: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Testspace.TestType' to multiple assemblies: 'D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
]]></errors>)
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub CompilingModuleWithMultipleForwardersToTheSameAssemblyShouldNotProduceMultipleForwardingErrors()
            Dim ilSource = "
.assembly extern D { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D
}"

            Dim ilModule = GetILModuleReference(ilSource, prependDefaultHeader:=False)
            Dim compilation = CreateCompilationWithMscorlib40(String.Empty, references:={ilModule}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors><![CDATA[
BC30652: Reference required to assembly 'D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'TestType'. Add one to your project.
]]></errors>)

            Dim dCode = "
Namespace Testspace
    Public Class TestType
    End Class
End Namespace"
            Dim dReference = CreateCompilationWithMscorlib40(
                source:=dCode,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="D").EmitToImageReference()

            ' Now compilation succeeds
            CreateCompilationWithMscorlib40(
                source:=String.Empty,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                references:={ilModule, dReference}).VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub LookingUpATypeForwardedTwiceInASourceCompilationReferenceShouldFail()
            ' This test specifically tests that SourceAssembly symbols also produce this error (by using a CompilationReference instead of the usual PEAssembly symbol)

            Dim ilSource = "
.module ForwarderModule.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D1
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D2
}"

            Dim ilModuleReference = GetILModuleReference(ilSource, prependDefaultHeader:=False)
            Dim forwarderCompilation = CreateEmptyCompilation(
                source:=String.Empty,
                references:={ilModuleReference},
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="Forwarder")

            Dim vbSource = "
Namespace UserSpace
    Public Class UserClass
        Public Shared Sub Main()
            Dim obj = new Testspace.TestType()
        End Sub
    End Class
End Namespace"

            Dim userCompilation = CreateCompilationWithMscorlib40(
                source:=vbSource,
                references:={forwarderCompilation.ToMetadataReference()},
                assemblyName:="UserAssembly")

            CompilationUtils.AssertTheseDiagnostics(userCompilation, <errors><![CDATA[
BC30002: Type 'Testspace.TestType' is not defined.
            Dim obj = new Testspace.TestType()
                          ~~~~~~~~~~~~~~~~~~
BC37208: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Testspace.TestType' to multiple assemblies: 'D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            Dim obj = new Testspace.TestType()
                          ~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub ForwardingErrorsInLaterModulesAlwaysOverwriteOnesInEarlierModules()
            Dim module1IL = "
.module module1IL.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D1
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D2
}"

            Dim module1Reference = GetILModuleReference(module1IL, prependDefaultHeader:=False)

            Dim module2IL = "
.module module12L.dll
.assembly extern D3 { }
.assembly extern D4 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D3
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D4
}"

            Dim module2Reference = GetILModuleReference(module2IL, prependDefaultHeader:=False)

            Dim forwarderCompilation = CreateEmptyCompilation(
                source:=String.Empty,
                references:={module1Reference, module2Reference},
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="Forwarder")

            Dim vbSource = "
Namespace UserSpace
    Public Class UserClass
        Public Shared Sub Main()
            Dim obj = new Testspace.TestType()
        End Sub
    End Class
End Namespace"

            Dim userCompilation = CreateCompilationWithMscorlib40(
                source:=vbSource,
                references:={forwarderCompilation.ToMetadataReference()},
                assemblyName:="UserAssembly")

            CompilationUtils.AssertTheseDiagnostics(userCompilation, <errors><![CDATA[
BC30002: Type 'Testspace.TestType' is not defined.
            Dim obj = new Testspace.TestType()
                          ~~~~~~~~~~~~~~~~~~
BC37208: Module 'module12L.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Testspace.TestType' to multiple assemblies: 'D3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            Dim obj = new Testspace.TestType()
                          ~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        <WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")>
        Public Sub MultipleForwardsThatChainResultInTheSameAssemblyShouldStillProduceAnError()
            ' The scenario Is that assembly A Is calling a method from assembly B. This method has a parameter of a type that lives
            ' in assembly C. Now if assembly C Is replaced with assembly C2, that forwards the type to both D And E, And D fowards it to E,
            ' it should fail with the appropriate error.

            Dim codeC = "
Namespace C
    Public Class ClassC
    End Class
End Namespace"
            Dim referenceC = CreateCompilationWithMscorlib40(
                source:=codeC,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="C").EmitToImageReference()

            Dim codeB = "
Imports C

Namespace B
    Public Class ClassB
        Public Shared Sub MethodB(obj As ClassC)
            System.Console.WriteLine(obj.GetHashCode())
        End Sub
    End Class
End Namespace"
            Dim referenceB = CreateCompilationWithMscorlib40(
                source:=codeB,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                references:={referenceC},
                assemblyName:="B").EmitToImageReference()

            Dim codeC2 = "
.assembly C { }
.module C.dll
.assembly extern D { }
.assembly extern E { }
.class extern forwarder C.ClassC
{
	.assembly extern D
}
.class extern forwarder C.ClassC
{
	.assembly extern E
}"

            Dim referenceC2 = CompileIL(codeC2, prependDefaultHeader:=False)

            Dim codeD = "
.assembly D { }
.assembly extern E { }
.class extern forwarder C.ClassC
{
	.assembly extern E
}"

            Dim referenceD = CompileIL(codeD, prependDefaultHeader:=False)
            Dim referenceE = CreateCompilationWithMscorlib40(
                source:=codeC,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="E").EmitToImageReference()

            Dim codeA = "
Imports B
Imports C

Namespace A
    Public Class ClassA
        Public Sub MethodA(obj As ClassC)
            ClassB.MethodB(obj)
        End Sub
    End Class
End Namespace"

            Dim userCompilation = CreateCompilationWithMscorlib40(
                source:=codeA,
                options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                references:={referenceB, referenceC2, referenceD, referenceE},
                assemblyName:="A")

            CompilationUtils.AssertTheseDiagnostics(userCompilation, <errors><![CDATA[
BC37208: Module 'C.dll' in assembly 'C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'C.ClassC' to multiple assemblies: 'D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'E, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            ClassB.MethodB(obj)
            ~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

    End Class
End Namespace
