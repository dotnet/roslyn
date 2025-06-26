' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class NamespaceGlobalTests

        ' Global is the root of all namespace even set root namespace of compilation
        <Fact>
        Public Sub RootNSForGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace NS1
                            Public Class Class1	'Global.NS1.Class1
                            End Class
                        End Namespace
                        Class C1		'Global.C1 
                        End Class
                    </file>
                </compilation>)
            Dim opt = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("RootNS")
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp2">
                    <file name="a.vb">
                        Namespace Global.Global.ns1
                            Class C2
                            End Class
                        End Namespace
                        Namespace [Global].ns1
                            Class C1
                            End Class
                        End Namespace
                        Class C1        'RootNS.C1
                        End Class 
                    </file>
                </compilation>, options:=opt)

            ' While the root namespace is empty it means Global is the container
            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "Class1", "NS1.Class1")
            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1", "C1")

            ' While set the root namespace of compilation to "RootNS" ,'RootNS' is inside global namespace
            Dim globalNS2 = compilation2.GlobalNamespace
            Dim rootNS2 = DirectCast(globalNS2.GetMembers("RootNS").Single(), NamespaceSymbol)
            CompilationUtils.VerifyIsGlobal(rootNS2.ContainingSymbol)
            ' The container of C1
            Dim typeSymbol2C1 = CompilationUtils.VerifyGlobalNamespace(compilation2, "a.vb", "C1", "RootNS.Global.ns1.C1", "RootNS.C1")
            ' The container of C1            
            Dim symbolC2 = CompilationUtils.VerifyGlobalNamespace(compilation2, "a.vb", "C2", "[Global].ns1.C2")
        End Sub

        ' Empty Name Space is equal to Global while  Root namespace is empty
        <Fact>
        Public Sub BC30179ERR_TypeConflict6_RootNSIsEmpty()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Class A
                        End Class
                        Namespace Global
                            Class A 'invalid
                            End Class
                        End Namespace
                    </file>
                </compilation>)

            ' While the root namespace is empty it means Global is the container
            Dim symbolA = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "A", {"A", "A"}, False)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, <errors>
BC30179: class 'A' and class 'A' conflict in namespace '&lt;Default&gt;'.
Class A
      ~
     </errors>)
        End Sub

        ' Set the root namespace of compilation to 'Global'
        <Fact>
        Public Sub RootNSIsGlobal()
            Dim opt = TestOptions.ReleaseDll.WithRootNamespace("Global")
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Class A 
                        End Class
                        Namespace Global
                            Class A 
                                Dim s As Global.Global.A
                                Dim s1 As [Global].A
                                Dim s2 As Global.A
                            End Class
                        End Namespace
                    </file>
                </compilation>, options:=opt)

            ' While the root namespace is Global it means [Global] 
            Dim globalNS = compilation1.SourceModule.GlobalNamespace
            Dim nsGlobal = CompilationUtils.VerifyIsGlobal(globalNS.GetMembers("Global").Single, False)
            Assert.Equal("[Global]", nsGlobal.ToDisplayString())

            Dim symbolA = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "A", "[Global].A", "A")
            CompilationUtils.AssertNoErrors(compilation1)
        End Sub

        <WorkItem(527731, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527731")>
        <Fact>
        Public Sub GlobalInSourceVsGlobalInOptions()
            Dim source = <compilation name="comp1">
                             <file name="a.vb">
                                 Namespace [Global]
                                 End Namespace
                             </file>
                         </compilation>
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(source)
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("Global"))
            Dim globalNS1 = compilation1.SourceModule.GlobalNamespace.GetMembers().Single()
            Dim globalNS2 = compilation2.SourceModule.GlobalNamespace.GetMembers().Single()
            Assert.Equal("Global", globalNS1.Name)
            Assert.Equal("Global", globalNS2.Name)
            Assert.Single(compilation1.GlobalNamespace.GetMembers("Global").AsEnumerable())
            Assert.Single(compilation2.GlobalNamespace.GetMembers("Global").AsEnumerable())
            Assert.Empty(compilation1.GlobalNamespace.GetMembers("[Global]").AsEnumerable())
            Assert.Empty(compilation2.GlobalNamespace.GetMembers("[Global]").AsEnumerable())
        End Sub

        ' Global for Partial class
        <Fact>
        Public Sub PartialInGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Partial Class Class1
                        End Class
                    </file>
                    <file name="b.vb">
                        Namespace Global
                            Public Class Class1
                            End Class
                        End Namespace
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "Class1", {"Class1"}, False)
            CompilationUtils.VerifyGlobalNamespace(compilation1, "b.vb", "Class1", {"Class1"}, False)
            Dim symbolClass = compilation1.GlobalNamespace.GetMembers("Class1").Single()
            Assert.Equal(2, DirectCast(symbolClass, NamedTypeSymbol).Locations.Length)
        End Sub

        ' Using escaped names for Global 
        <WorkItem(527731, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527731")>
        <Fact>
        Public Sub EscapedGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace [Global]
                            Public Class Class1
                            End Class
                        End Namespace
                        Namespace Global
                            Public Class Class1	'valid
                            End Class
                        End Namespace
                    </file>
                </compilation>)

            Assert.Equal(1, compilation1.SourceModule.GlobalNamespace.GetMembers("Global").Length)
            Dim symbolClass = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "Class1", {"Class1", "[Global].Class1"}, False)
        End Sub

        ' Global is Not Case sensitive  
        <Fact>
        Public Sub BC30179ERR_TypeConflict6_CaseSenGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace GLOBAL
                            Class C1
                            End Class
                        End Namespace
                        Namespace global
                            Class C1	'invalid 
                            End Class
                        End Namespace
                    </file>
                </compilation>)

            Dim symbolClass = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1", {"C1", "C1"}, False)
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, <errors>
BC30179: class 'C1' and class 'C1' conflict in namespace '&lt;Default&gt;'.
                            Class C1	'invalid 
                                  ~~
     </errors>)
        End Sub

        ' Global for Imports   
        <Fact>
        Public Sub BC36001ERR_NoGlobalExpectedIdentifier_ImportsGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Imports Global.[global]'invalid
                        Imports Global.goo'invalid
                        Imports Global 'invalid
                        Imports a = [Global]   'valid

                        Namespace [global]
                        End Namespace
                        Namespace goo
                        End Namespace
                    </file>
                </compilation>)

            Dim GlobalNSMember = compilation1.SourceModule.GlobalNamespace.GetMembers()
            Assert.True(GlobalNSMember(0).ContainingNamespace.IsGlobalNamespace)
            Assert.True(GlobalNSMember(1).ContainingNamespace.IsGlobalNamespace)
            CompilationUtils.AssertTheseParseDiagnostics(compilation1, <errors>
BC36001: 'Global' not allowed in this context; identifier expected.
Imports Global.[global]'invalid
        ~~~~~~
BC36001: 'Global' not allowed in this context; identifier expected.
                        Imports Global.goo'invalid
                                ~~~~~~
BC36001: 'Global' not allowed in this context; identifier expected.
                        Imports Global 'invalid
                                ~~~~~~
     </errors>)
        End Sub

        ' Global for Alias name   
        <Fact>
        Public Sub BC36001ERR_NoGlobalExpectedIdentifier_ImportsAliasGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Imports Global = System	'invalid 
                        Imports Global.[Global] = System   'invalid
                        Imports [Global] = System   'valid
                    </file>
                </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors>
BC36001: 'Global' not allowed in this context; identifier expected.
Imports Global = System	'invalid 
        ~~~~~~
BC36001: 'Global' not allowed in this context; identifier expected.
                        Imports Global.[Global] = System   'invalid
                                ~~~~~~
BC40056: Namespace or type specified in the Imports 'Global.Global' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
                        Imports Global.[Global] = System   'invalid
                                ~~~~~~~~~~~~~~~
     </errors>)
        End Sub

        ' Global can't be used as type 
        <WorkItem(527728, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527728")>
        <Fact>
        Public Sub BC30183ERR_InvalidUseOfKeyword_GlobalAsType()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Imports System
                        Class C1(Of T As Global)
                        End Class

                        Class C2
                            Inherits Global
                        End Class

                        Structure [Global]
                        End Structure

                        Class Global
                        End Class
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "Global", {"[Global]", "[Global]", "C1(Of T As [Global])"}, False)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors>
BC30182: Type expected.
                        Class C1(Of T As Global)
                                         ~~~~~~
BC30182: Type expected.
                            Inherits Global
                                     ~~~~~~
BC30179: structure '[Global]' and class 'Global' conflict in namespace '&lt;Default&gt;'.
                        Structure [Global]
                                  ~~~~~~~~
BC30179: class 'Global' and structure 'Global' conflict in namespace '&lt;Default&gt;'.
                        Class Global
                              ~~~~~~
BC30183: Keyword is not valid as an identifier.
                        Class Global
                              ~~~~~~
     </errors>)
        End Sub

        ' Global can't be used as identifier 
        <WorkItem(527728, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527728")>
        <Fact>
        Public Sub BC30183ERR_InvalidUseOfKeyword_GlobalAsIdentifier()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Class Global(Of T As Class)
                        End Class
                        Class C1(Of Global As Class)
                        End Class
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "Global", "[Global](Of T As Class)", "C1(Of [Global] As Class)")
            CompilationUtils.AssertTheseParseDiagnostics(compilation1, <errors>
BC30183: Keyword is not valid as an identifier.
Class Global(Of T As Class)
      ~~~~~~
BC30183: Keyword is not valid as an identifier.
                        Class C1(Of Global As Class)
                                    ~~~~~~
     </errors>)
        End Sub

        ' Global can't be used as Access Modifier 
        <Fact>
        Public Sub BC30035ERR_Syntax_AccessModifier()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Global Class C1(of T as class)
                        End Class
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1", "C1(Of T As Class)")
            CompilationUtils.AssertTheseParseDiagnostics(compilation1, <errors>
BC30188: Declaration expected.
Global Class C1(of T as class)
~~~~~~                                      
     </errors>)
        End Sub

        ' Global namespace may not be nested in another namespace 
        <WorkItem(539076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539076")>
        <Fact>
        Public Sub BC31544ERR_NestedGlobalNamespace_NestedGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace Global
                            Namespace Global ' invalid
                                Public Class c1
                                End Class
                            End Namespace
                        End Namespace
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1")
            Assert.Equal("[Global]", compilation1.SourceModule.GlobalNamespace.GetMembers().Single().ToDisplayString())
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, <errors>
BC31544: Global namespace may not be nested in another namespace.
                            Namespace Global ' invalid
                                      ~~~~~~
     </errors>)
        End Sub

        ' [Global] namespace could be nested in another namespace 
        <Fact>
        Public Sub NestedEscapedGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace Global
                            Namespace [Global] ' valid
                                Public Class C1
                                End Class
                            End Namespace
                        End Namespace
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1", "[Global].C1")
            CompilationUtils.AssertNoDeclarationDiagnostics(compilation1)
        End Sub

        ' Global in Fully qualified names 
        <Fact>
        Public Sub FullyQualifiedOfGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Imports [Global].ns1
                        Namespace Global.Global.ns1
                            Class C1
                            End Class
                        End Namespace
                        Namespace [Global].ns1
                            Class C1 
                            End Class
                        End Namespace
                    </file>
                    <file name="b.vb">
                        Imports NS1.Global'valid NS1.Global considered NS1.[Global]   
                        Namespace NS1
                            Namespace [Global]
                                Public Class C2
                                End Class
                            End Namespace
                        End Namespace
                    </file>
                    <file name="c.vb">
                        Namespace ns1.Global	'valid considered NS1.[Global]   
                                Public Class C2
                                End Class
                        End Namespace
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1", {"[Global].ns1.C1", "[Global].ns1.C1"}, False)
            CompilationUtils.VerifyGlobalNamespace(compilation1, "b.vb", "C2", "NS1.Global.C2")
            CompilationUtils.VerifyGlobalNamespace(compilation1, "c.vb", "C2", "NS1.Global.C2")
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30179: class 'C1' and class 'C1' conflict in namespace '[Global].ns1'.
                            Class C1 
                                  ~~
BC40055: Casing of namespace name 'ns1' does not match casing of namespace name 'NS1' in 'b.vb'.
Namespace ns1.Global	'valid considered NS1.[Global]   
          ~~~
BC30179: class 'C2' and class 'C2' conflict in namespace 'NS1.Global'.
                                Public Class C2
                                             ~~
</errors>)
        End Sub

        ' Different types in global namespace 
        <Fact>
        Public Sub DiffTypeInGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace Global
                            Class [Global]
                                Class UserdefCls
                                End Class
                                Structure UserdefStruct
                                End Structure
                            End Class
                            Module M1
                            End Module
                            Enum E1
                                ONE
                            End Enum
                        End Namespace
                    </file>
                </compilation>)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "UserdefCls", "[Global].UserdefCls")
            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "UserdefStruct", "[Global].UserdefStruct")
            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "M1", "M1")
            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "E1", "E1")
            CompilationUtils.AssertNoErrors(compilation1)
        End Sub

        ' Access different fields with different access modifiers in Global 
        <Fact>
        Public Sub DiffAccessInGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace Global
                            Public Class C1
                                Private Class C2
                                End Class
                                Friend Class C3
                                End Class
                            End Class
                        End Namespace
                    </file>
                </compilation>)

            Dim symbolC1 = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1", "C1")
            Dim symbolC2 = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C2", "C1.C2")
            Dim symbolC3 = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C3", "C1.C3")
            Assert.Equal(Accessibility.Public, symbolC1(0).DeclaredAccessibility)
            Assert.Equal(Accessibility.Private, symbolC2(0).DeclaredAccessibility)
            Assert.Equal(Accessibility.Friend, symbolC3(0).DeclaredAccessibility)

        End Sub

        ' Global works on Compilation 
        <WorkItem(539077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539077")>
        <Fact>
        Public Sub BC30554ERR_AmbiguousInUnnamedNamespace1_GlobalOnCompilation()
            Dim opt1 = TestOptions.ReleaseDll.WithRootNamespace("NS1")
            Dim opt2 = TestOptions.ReleaseDll.WithRootNamespace("NS2")
            Dim opt3 = TestOptions.ReleaseDll.WithRootNamespace("NS3")
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace Global
                            Public Class C1
                                Private Class C2
                                End Class
                                Public Class C3
                                End Class
                            End Class                        
                        End Namespace 
                    </file>
                </compilation>, options:=opt1)
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp2">
                    <file name="a.vb">
                        Namespace Global
                            Public Class C1
                                Private Class C2
                                End Class
                                Public Class C3
                                End Class
                            End Class
                        End Namespace 
                    </file>
                </compilation>, options:=opt2)
            Dim ref1 = New VisualBasicCompilationReference(compilation1)
            Dim ref2 = New VisualBasicCompilationReference(compilation2)
            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp3">
                    <file name="a.vb">
                        Namespace NS1
                            Structure S1
                                Dim A As Global.C1.C2	' invalid
                                Dim B As Global.C1.C3	' invalid
                            End Structure
                        End Namespace
                    </file>
                </compilation>, options:=opt3)
            compilation3 = compilation3.AddReferences(ref1, ref2)

            CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C2", "C1.C2")
            CompilationUtils.VerifyGlobalNamespace(compilation2, "a.vb", "C3", "C1.C3")
            CompilationUtils.AssertTheseDiagnostics(compilation3,
<errors>
BC30554: 'C1' is ambiguous.
                                Dim A As Global.C1.C2	' invalid
                                         ~~~~~~~~~
BC30554: 'C1' is ambiguous.
                                Dim B As Global.C1.C3	' invalid
                                         ~~~~~~~~~    
</errors>)
        End Sub

        ' Define customer namespace same as namespace of the .NET Framework in Global 
        <Fact>
        Public Sub DefSystemNSInGlobal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="comp1">
                    <file name="a.vb">
                        Namespace Global
                            Namespace System
                                Class C1
                                    Dim A As System.Int32	' valid 
                                End Class
                            End Namespace
                        End Namespace
                    </file>
                </compilation>)

            Dim symbolC1 = CompilationUtils.VerifyGlobalNamespace(compilation1, "a.vb", "C1", "System.C1")
            CompilationUtils.AssertNoErrors(compilation1)
        End Sub

        <Fact>
        <WorkItem(545787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545787")>
        Public Sub NestedGlobalNS()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NestedGlobalNS">
        <file name="a.vb">
Imports System            
Namespace N
   Namespace Global.M
        Class X 'BIND:"Class X"
        End Class
   End Namespace
End Namespace
    </file>
    </compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim typeStatementSyntax = CompilationUtils.FindBindingText(Of TypeStatementSyntax)(compilation, "a.vb", 0)
            Dim cls = DirectCast(model.GetDeclaredSymbol(typeStatementSyntax), NamedTypeSymbol)
            Assert.Equal("N.Global.M.X", cls.ToDisplayString())
        End Sub

        <Fact>
        Public Sub Bug529716()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vb">
Namespace Global
    Class C

    End Class
End Namespace

Namespace Global.Ns1
    Class D

    End Class
End Namespace
                    </file>
                </compilation>)

            Dim [global] = compilation1.SourceModule.GlobalNamespace
            Dim classC = [global].GetTypeMembers("C").Single()
            Dim ns1 = DirectCast([global].GetMembers("Ns1").Single(), NamespaceSymbol)
            Dim classD = ns1.GetTypeMembers("D").Single()

            Assert.False(ns1.IsImplicitlyDeclared)

            For Each ref In [global].DeclaringSyntaxReferences
                Dim node = ref.GetSyntax()
                Assert.Equal(SyntaxKind.CompilationUnit, node.Kind)
            Next

            ' Since we never return something other than CompilationUnit as a declaring syntax for a Global namespace,
            ' the following assert should succeed.
            Assert.True([global].IsImplicitlyDeclared)
        End Sub

    End Class

End Namespace
