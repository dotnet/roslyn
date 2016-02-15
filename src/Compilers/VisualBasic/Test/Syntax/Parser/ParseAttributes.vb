' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class Attributes
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseAssemblyAttribute()
        ParseModuleOrAssemblyAttribute(<![CDATA[
            <Assembly:clscompliant(true)>
            Module Module1
            End Module
        ]]>.Value, isFullWidth:=False)
    End Sub

    <Fact>
    Public Sub ParseModuleAttribute()
        ParseModuleOrAssemblyAttribute(<![CDATA[
            <Module:clscompliant(true)>
            Module Module1
            End Module
        ]]>.Value, isFullWidth:=False)
    End Sub

    <WorkItem(570756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570756")>
    <Fact()>
    Public Sub ParseFullWidthModuleAndAssemblyAttributes()
        ParseModuleOrAssemblyAttribute("<Assembly:A>".ToFullWidth(), isFullWidth:=True)
        ParseModuleOrAssemblyAttribute("<Module:A>".ToFullWidth(), isFullWidth:=True)
    End Sub

    Private Sub ParseModuleOrAssemblyAttribute(source As String, isFullWidth As Boolean)
        Dim tree = ParseAndVerify(source)
        Dim root = tree.GetRoot
        Dim attrStmt = DirectCast(root.ChildNodes(0), AttributesStatementSyntax)
        Dim attrList = attrStmt.AttributeLists(0)
        Dim attr = attrList.Attributes(0)
        Dim target = attr.Target
        Assert.Equal(If(isFullWidth, FULLWIDTH_COLON_STRING, ":"), target.ColonToken.ValueText)
    End Sub

    <Fact>
    Public Sub ParseModuleAndAssemblyAttributesWithTrivia()
        ParseAndVerify(<![CDATA[
<Assembly : A>
]]>)
        ParseAndVerify(<![CDATA[
<Module _
: _
A>
]]>)
        ParseAndVerify(<![CDATA[
<Assembly : A>
]]>.Value.Replace(":"c, FULLWIDTH_COLON))
    End Sub

    <Fact>
    Public Sub BC30183ERR_InvalidUseOfKeyword_FileNotAssemblyOrModuleAttribute()
        Dim tree = ParseAndVerify(<![CDATA[
            <Dim:clscompliant(true)>
            Module Module1
            End Module
        ]]>,
            Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Dim"),
            Diagnostic(ERRID.ERR_ExpectedGreater, ""),
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "clscompliant(true)"),
            Diagnostic(ERRID.ERR_ExpectedEOS, ">"))

        Dim root As CompilationUnitSyntax = tree.GetCompilationUnitRoot()
        Dim incomplete As IncompleteMemberSyntax = DirectCast(root.ChildNodes(0), IncompleteMemberSyntax)
        Dim attrList = incomplete.AttributeLists(0)
        Assert.True(attrList.GreaterThanToken.IsMissing)
        Assert.Equal(":", attrList.GreaterThanToken.ToFullString) ' ":" is trivia on the missing greater than
    End Sub

    <Fact>
    Public Sub Bug862162()
        ParseAndVerify(<![CDATA[
            Imports System.Runtime.InteropServices
            Structure Struct1
              <MarshalAs(UnmanagedType.ByValArray, sizeconst:=4)> Public A() As Integer
            End Structure
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862165()
        ParseAndVerify(<![CDATA[
            Public Class Attr
                Inherits Attribute
            End Class
            <Attr(CByte(2))> Class Class1
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862181()
        ParseAndVerify(<![CDATA[
            <AttributeUsageAttribute(System.AttributeTargets.Class), Obsolete()> Public Class Attr
                                          Inherits Attribute
                  End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862462()
        ParseAndVerify(<![CDATA[
            <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
            Partial Class UserControl2
            End Class
        ]]>)
    End Sub

    <WorkItem(863443, "DevDiv/Personal")>
    <Fact>
    Public Sub BC32017ERR_ArgumentSyntax()
        ParseAndVerify(<![CDATA[
            <attr(i+=1)> Class c9
            <attr(l-=1)> Sub test()
                         End Sub
            End Class
        ]]>,
        <errors>
            <error id="32017"/>
            <error id="32017"/>
        </errors>)
    End Sub

    <WorkItem(877883, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseAssemblyAttributeFollowingComment()
        ParseAndVerify(<![CDATA[
            <Assembly: CLSCompliant(True)> 

            'The following GUID is for the ID of the typelib if this project is exposed to COM
            <Assembly: Guid("2AFE637A-E02A-4693-BC30-6FFFAE6B415A")> 
        ]]>)
    End Sub

    <WorkItem(877924, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30188_ParseAttribute_StandaloneAttribute()
        ParseAndVerify(<![CDATA[
            <CLSCompliant(True)> 

            Class Class1
            End Class
        ]]>, <errors>
                 <error id="32035" message="Attribute specifier is not a complete statement. Use a line continuation to apply the attribute to the following statement." start="13" end="33"/>
             </errors>)
    End Sub

    <WorkItem(779720, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30182ERR_UnrecognizedType_AttributeBeforeInherits()
        ParseAndVerify(<![CDATA[
            Interface IMammals : End interface
            Interface ICarnivore : <SomeAttribute()> Inherits : IMammals : End interface
            Module Module1
            Sub Main
            End Sub
            End module
        ]]>,
            Diagnostic(ERRID.ERR_SpecifiersInvalidOnInheritsImplOpt, "<SomeAttribute()>"),
            Diagnostic(ERRID.ERR_UnrecognizedType, ""),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "IMammals"))
    End Sub

    <WorkItem(881442, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseAttributeImplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            <
                Assembly: CLSCompliant(
                True
            )>
        ]]>)
    End Sub

    <WorkItem(894094, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC32015ERR_FileAttributeNotAssemblyOrModule()
        ParseAndVerify(<![CDATA[
            Option Strict On

            <Assembly: InternalsVisibleTo("FriendAsmMisc03-ClassLibrary1"), AssemblyName("MyFineAssembly")>
            
            Module M1
            End Module
        ]]>, Diagnostic(ERRID.ERR_FileAttributeNotAssemblyOrModule, ""))
    End Sub

    <WorkItem(894094, "DevDiv/Personal")>
    <Fact>
    Public Sub BC32015ERR_FileAttributeNotAssemblyOrModule_ParseIncompleteAssemblyAttrib()
        ParseAndVerify(<![CDATA[
<Assembly: InternalsVisibleTo("FriendAsmMisc03-ClassLibrary1"),
        ]]>, Diagnostic(ERRID.ERR_FileAttributeNotAssemblyOrModule, ""),
             Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
             Diagnostic(ERRID.ERR_ExpectedGreater, ""))
    End Sub

    <WorkItem(527027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527027")>
    <Fact>
    Public Sub BC30203_ParseMoreErrorStandaloneAttribute()
        ParseAndVerify(<![CDATA[
                      <Obsolete(),
                           >
                      Public Class c6
                      End Class
            ]]>,
            <errors>
                <error id="30203"/>
            </errors>)
    End Sub

    <WorkItem(887804, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30188_ParseMoreErrorsStandaloneAttributeAndExpectedIdentifier()
        ParseAndVerify(<![CDATA[
                       Class c1
                        <new()>
                       End Class
            ]]>,
            <errors>
                <error id="30183"/>
            </errors>)
    End Sub

    <WorkItem(537226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537226")>
    <Fact>
    Public Sub BC40008WRN_UseOfObsoleteSymbolNoMessage1()
        Dim code = <![CDATA[
Friend Module Obsolete001mod
    
        Sub Obsolete001()
        <
        Obsolete()
        >
        Static Dim i As Integer

        'COMPILEWARNING : BC40008, "x" 
        <Obsolete()> Static Dim x As XElement = <x>
                                                    <x><%= Nothing %></x>
                                                </x>
    End Sub
End Module
]]>.Value

        ParseAndVerify(code).VerifySpanOfChildWithinSpanOfParent()
    End Sub

    <WorkItem(537226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537226")>
    <Fact>
    Public Sub ParseAttributeBeforeStatic()
        ParseAndVerify(<![CDATA[
            <AttributeUsage(AttributeTargets.All)> _
Class xml
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.All)> _
Class CDATA
    Inherits Attribute
End Class

Friend Module Attribute004mod
    Sub Attribute004()
        '---------------------------------------------------------------------------------------
        <xml()> _
        Dim x1 As Object

        '---------------------------------------------------------------------------------------
        <[CDATA]()> _
        Static x3 As Object
    End Sub
End Module
        ]]>, <errors>
                 <error id="30660"/>
             </errors>)
    End Sub

    <WorkItem(569310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569310")>
    <Fact()>
    Public Sub ParseFileLevelAttributesWithExtraColon()
        ParseAndVerify(<![CDATA[
<Assembly::
]]>,
            <errors>
                <error id="30203" message="Identifier expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
<Assembly:A,:
]]>,
            <errors>
                <error id="32015" message="'Assembly' or 'Module' expected."/>
                <error id="30203" message="Identifier expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
<Assembly:A, _
:
]]>,
            <errors>
                <error id="32015" message="'Assembly' or 'Module' expected."/>
                <error id="30203" message="Identifier expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
    End Sub

    <WorkItem(638911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638911")>
    <Fact(Skip:="638911")>
    Public Sub ParseFileLevelAttributesWithExtraColon_2()
        ParseAndVerify(<![CDATA[
<Assembly::A>
]]>,
            <errors>
                <error id="30203" message="Identifier expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
<Module : : A>
]]>.Value.Replace(":"c, FULLWIDTH_COLON),
            <errors>
                <error id="30203" message="Identifier expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
    End Sub

    <WorkItem(570808, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570808")>
    <Fact()>
    Public Sub ParseAttributeTargetOtherContextualKeyword()
        ParseAndVerify(<![CDATA[
<Assembly:A,Async:B>
]]>,
            <errors>
                <error id="32015" message="'Assembly' or 'Module' expected."/>
                <error id="30636" message="'>' expected."/>
                <error id="30689" message="Statement cannot appear outside of a method body."/>
                <error id="30800" message="Method arguments must be enclosed in parentheses."/>
                <error id="30201" message="Expression expected."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
<Assembly:A,Other:B>
]]>,
            <errors>
                <error id="32015" message="'Assembly' or 'Module' expected."/>
                <error id="30636" message="'>' expected."/>
                <error id="30689" message="Statement cannot appear outside of a method body."/>
                <error id="30800" message="Method arguments must be enclosed in parentheses."/>
                <error id="30201" message="Expression expected."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
    End Sub

    ''' <summary>
    ''' &lt;&gt; should be treated as an empty attributes list.
    ''' </summary>
    <WorkItem(668159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/668159")>
    <Fact()>
    Public Sub EmptyAttributesList()
        ParseAndVerify(<![CDATA[
<>
]]>,
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
<>
Friend Class C
End Class
]]>,
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
<
>
Class C
End Class
]]>,
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
< >
Class C
End Class
]]>.Value.Replace("<"c, FULLWIDTH_LESS_THAN_SIGN).Replace(">"c, FULLWIDTH_GREATER_THAN_SIGN),
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Class C

    <{0}{1}{2}>
    Sub M()
    End Sub
End Class
]]>.Value, " "c, vbTab, NO_BREAK_SPACE),
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Sub M()
        <>
    End Sub
End Class
]]>,
            <errors>
                <error id="30035" message="Syntax error."/>
            </errors>)
    End Sub

End Class
