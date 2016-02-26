' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports System.Xml.Linq
Imports System.Text
Imports System.IO
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class DocCommentTests
        Inherits BasicTestBase

        Private Shared ReadOnly s_optionsDiagnoseDocComments As VisualBasicParseOptions = VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose)

        <Fact>
        Public Sub NoXmlResolver()
            Dim sources =
<compilation name="DocumentationMode">
    <file name="a.vb">
        <![CDATA[
''' <summary> <include file='abc' path='def' /> </summary>
Class C
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(
                sources,
                options:=TestOptions.ReleaseDll.WithXmlReferenceResolver(Nothing),
                parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse))

            compilation.VerifyDiagnostics()

            CheckXmlDocument(compilation, expectedDocXml:=
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
DocumentationMode
</name>
</assembly>
<members>
<member name="T:C">
 <summary> <!--warning BC42321: Unable to include XML fragment 'def' of file 'abc'. References to XML documents are not supported.--> </summary>
</member>
</members>
</doc>
]]>
</xml>, ensureEnglishUICulture:=True)

        End Sub

        <Fact>
        Public Sub DocumentationMode_None()
            Dim sources =
<compilation name="DocumentationMode">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> </summary
Module Module0
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                sources, parseOptions:=(New VisualBasicParseOptions()).WithDocumentationMode(DocumentationMode.None))

            Dim tree = compilation.SyntaxTrees(0)
            Dim moduleStatement = tree.FindNodeOrTokenByKind(SyntaxKind.ModuleStatement)
            Assert.True(moduleStatement.IsNode)

            Dim node = moduleStatement.AsNode()
            Dim trivia = node.GetLeadingTrivia().ToArray()

            Assert.True(trivia.Any(Function(x) x.Kind = SyntaxKind.CommentTrivia))
            Assert.False(trivia.Any(Function(x) x.Kind = SyntaxKind.DocumentationCommentTrivia))

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(tree).GetDiagnostics(), <errors></errors>)
        End Sub

        <Fact>
        Public Sub DocumentationMode_Parse()
            Dim sources =
<compilation name="DocumentationMode">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> </summary
Module Module0
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                sources, parseOptions:=(New VisualBasicParseOptions()).WithDocumentationMode(DocumentationMode.Parse))

            Dim tree = compilation.SyntaxTrees(0)
            Dim moduleStatement = tree.FindNodeOrTokenByKind(SyntaxKind.ModuleStatement)
            Assert.True(moduleStatement.IsNode)

            Dim node = moduleStatement.AsNode()
            Dim trivia = node.GetLeadingTrivia().ToArray()

            Assert.False(trivia.Any(Function(x) x.Kind = SyntaxKind.CommentTrivia))
            Assert.True(trivia.Any(Function(x) x.Kind = SyntaxKind.DocumentationCommentTrivia))

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(tree).GetDiagnostics(), <errors></errors>)
        End Sub

        <Fact>
        Public Sub DocumentationMode_ParseAndDiagnose()
            Dim sources =
<compilation name="DocumentationMode">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> </summary
Module Module0
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                sources, parseOptions:=s_optionsDiagnoseDocComments)

            Dim tree = compilation.SyntaxTrees(0)
            Dim moduleStatement = tree.FindNodeOrTokenByKind(SyntaxKind.ModuleStatement)
            Assert.True(moduleStatement.IsNode)

            Dim node = moduleStatement.AsNode()
            Dim trivia = node.GetLeadingTrivia().ToArray()

            Assert.False(trivia.Any(Function(x) x.Kind = SyntaxKind.CommentTrivia))
            Assert.True(trivia.Any(Function(x) x.Kind = SyntaxKind.DocumentationCommentTrivia))

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(tree).GetDiagnostics(),
<errors>
    <![CDATA[
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
''' <summary> </summary
                       ~
]]>
</errors>)
        End Sub

        <Fact>
        Public Sub DocCommentOnUnsupportedSymbol()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Class E
    ReadOnly Property quoteForTheDay() As String
        ''' <summary></summary>
        Get
            Return "hello"
        End Get
    End Property
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42312: XML documentation comments must precede member or type declarations.
        ''' <summary></summary>
           ~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(720931, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720931")>
        <Fact>
        Public Sub Bug720931()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <see cref="Integer"/>
''' <see cref="UShort"/>
''' <see cref="Object"/>
''' <see cref="Date"/>
Public Class CLAZZ
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:CLAZZ">
 <see cref="T:System.Int32"/>
 <see cref="T:System.UInt16"/>
 <see cref="T:System.Object"/>
 <see cref="T:System.DateTime"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(705788, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705788")>
        <Fact>
        Public Sub Bug705788()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="Bug705788">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <c name="Scenario1"/>
''' <code name="Scenario1"/>
''' <example name="Scenario1"/>
''' <list name="Scenario1"/>
''' <paramref name="Scenario1"/>
''' <remarks name="Scenario1"/>
''' <summary name="Scenario1"/>
Module Scenario1
    ''' <para name="Scenario2"/>
    ''' <paramref name="Scenario2"/>
    ''' <permission cref="Scenario2" name="Scenario2"/>
    ''' <see cref="Scenario2" name="Scenario2"/>
    ''' <seealso cref="Scenario2" name="Scenario2"/>
    Class Scenario2
    End Class

    Sub Main()
    End Sub
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42306: XML comment tag 'paramref' is not permitted on a 'module' language element.
''' <paramref name="Scenario1"/>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.
    ''' <paramref name="Scenario2"/>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
Bug705788
</name>
</assembly>
<members>
<member name="T:Scenario1">
 <c name="Scenario1"/>
 <code name="Scenario1"/>
 <example name="Scenario1"/>
 <list name="Scenario1"/>
 <paramref name="Scenario1"/>
 <remarks name="Scenario1"/>
 <summary name="Scenario1"/>
</member>
<member name="T:Scenario1.Scenario2">
 <para name="Scenario2"/>
 <paramref name="Scenario2"/>
 <permission cref="T:Scenario1.Scenario2" name="Scenario2"/>
 <see cref="T:Scenario1.Scenario2" name="Scenario2"/>
 <seealso cref="T:Scenario1.Scenario2" name="Scenario2"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(658453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658453")>
        <Fact>
        Public Sub Bug658453()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Namespace Microsoft.VisualBasic
    ''' <summary>
    ''' Provides core iterator implementation.
    ''' </summary>
    ''' <typeparam name="TState">Type of iterator state data.</typeparam>
    ''' <typeparam name="TItem">Type of items returned from the iterator.</typeparam>
    ''' <param name="state">Iteration data.</param>
    ''' <param name="item">Element produced at this step.</param>
    ''' <returns>Whether the step was successful.</returns>
    Friend Delegate Function IteratorStep(Of TState, TItem)(
        ByRef state As TState,
        ByRef item As TItem) As Boolean
End Namespace
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:Microsoft.VisualBasic.IteratorStep`2">
 <summary>
 Provides core iterator implementation.
 </summary>
 <typeparam name="TState">Type of iterator state data.</typeparam>
 <typeparam name="TItem">Type of items returned from the iterator.</typeparam>
 <param name="state">Iteration data.</param>
 <param name="item">Element produced at this step.</param>
 <returns>Whether the step was successful.</returns>
</member>
</members>
</doc>
]]>
</xml>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "TState").ToArray()
            Assert.Equal(2, names.Length)

            CheckSymbolInfoAndTypeInfo(model, names(0), "TState")
        End Sub

        <WorkItem(762687, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762687")>
        <Fact>
        Public Sub Bug762687a()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="Bug762687">
    <file name="a.vb">
        <![CDATA[
Imports System

Class B
    Public Property System As Object
End Class

Class D 
    Inherits B

    ''' <see cref="System.Console"/>
    Public X As Integer
End Class 
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
Bug762687
</name>
</assembly>
<members>
<member name="F:D.X">
 <see cref="T:System.Console"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(762687, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762687")>
        <Fact>
        Public Sub Bug762687b()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="Bug762687">
    <file name="a.vb">
        <![CDATA[
Imports System

Class B
    Public Property System As Object
End Class

Class D 
    Inherits B

    ''' <see cref="System.Console.WriteLine()"/>
    Public X As Integer
End Class 
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
Bug762687
</name>
</assembly>
<members>
<member name="F:D.X">
 <see cref="M:System.Console.WriteLine"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(664943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/664943")>
        <Fact>
        Public Sub Bug664943()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

  ''' <summary></summary>
  '''
Class E
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:E">
 <summary></summary>

</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(679833, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/679833")>
        <Fact>
        Public Sub Bug679833_DontCrash()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Public 
    Sub New()
d Sub
    Public    ''' As String
    '''
summary>
End Enum
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC30203: Identifier expected.
Public 
       ~
BC30026: 'End Sub' expected.
    Sub New()
    ~~~~~~~~~
BC30451: 'd' is not declared. It may be inaccessible due to its protection level.
d Sub
~
BC36673: Multiline lambda expression is missing 'End Sub'.
d Sub
  ~~~
BC30800: Method arguments must be enclosed in parentheses.
d Sub
  ~~~~
BC30198: ')' expected.
d Sub
     ~
BC30199: '(' expected.
d Sub
     ~
BC30203: Identifier expected.
    Public    ''' As String
              ~
BC42302: XML comment must be the first statement on a line. XML comment will be ignored.
    Public    ''' As String
              ~~~~~~~~~~~~~
BC42303: XML comment cannot appear within a method or a property. XML comment will be ignored.
    '''
       ~
BC30201: Expression expected.
summary>
       ~
BC30800: Method arguments must be enclosed in parentheses.
summary>
       ~
BC30201: Expression expected.
summary>
        ~
BC30184: 'End Enum' must be preceded by a matching 'Enum'.
End Enum
~~~~~~~~
]]>
</error>)
        End Sub

        <WorkItem(665883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665883")>
        <Fact>
        Public Sub Bug665883()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <see cref="Console.WriteLine"/>
Module M
End Module
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:M">
 <see cref="M:System.Console.WriteLine"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(666241, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666241")>
        <Fact>
        Public Sub Bug666241()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System
Namespace System.Drawing
    ''' <summary>
    ''' Opt-In flag to look for resources in the another assembly 
    ''' with the "bitmapSuffix" config setting 
    ''' </summary>
    <AttributeUsage(AttributeTargets.Assembly)>
    Friend Class BitmapSuffixInSatelliteAssemblyAttribute
        Inherits Attribute
    End Class
End Namespace
]]>
    </file>
    <file name="b.vb">
        <![CDATA[
Imports System.Diagnostics.CodeAnalysis

Namespace Microsoft.VisualBasic.PowerPacks.Printing.Compatibility.VB6
    Public Module SystemColorConstants
        ''' <include file='doc\Constants.uex' path='docs/doc[@for="SystemColorConstants.vbScrollBars"]/*' />
        <SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")> _
        <SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")> _
        Public Const vbScrollBars As Integer = &H80000000
    End Module
End Namespace
]]>
    </file>
</compilation>,
<error></error>)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            CompilationUtils.AssertTheseDiagnostics(model.GetDiagnostics(), <error></error>)

            model = compilation.GetSemanticModel(compilation.SyntaxTrees(1))
            CompilationUtils.AssertTheseDiagnostics(model.GetDiagnostics(), <error></error>)
        End Sub

        <WorkItem(658793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658793")>
        <Fact>
        Public Sub Bug658793()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary cref="(" />
'''
Class E
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute '(' that could not be resolved.
''' <summary cref="(" />
             ~~~~~~~~
]]>
</error>)
        End Sub

        <WorkItem(721582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/721582")>
        <Fact>
        Public Sub Bug721582()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <see cref="object"/>
''' <see cref="object.tostring"/>
''' <see cref="system.object"/>
''' <see cref="system.object.tostring"/>
''' <see cref="object.tostring()"/>
''' <see cref="system.object.tostring()"/>
Class E
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:E">
 <see cref="T:System.Object"/>
 <see cref="T:System.Object"/>
 <see cref="T:System.Object"/>
 <see cref="M:System.Object.ToString"/>
 <see cref="T:System.Object"/>
 <see cref="M:System.Object.ToString"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(657426, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657426")>
        <Fact>
        Public Sub Bug657426()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
'''   <see 
'''     cref="Int32"/>
''' </summary>
Class E
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:E">
 <summary>
   <see 
     cref="T:System.Int32"/>
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(658322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658322")>
        <Fact>
        Public Sub Bug658322a()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Class E
    ''' <param name="next">The next binder.</param>
    Public Sub New([next] As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="M:E.#ctor(System.Int32)">
 <param name="next">The next binder.</param>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(658322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658322")>
        <Fact>
        Public Sub Bug658322b()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Namespace Roslyn.Compilers.VisualBasic

    Partial Class BoundAddressOfOperator

        ''' <returns>The <see cref="Binder.DelegateResolutionResult">Binder.DelegateResolutionResult</see> for the conversion </returns>
        Friend Function GetDelegateResolutionResult(ByRef delegateResolutionResult As Binder.DelegateResolutionResult) As Boolean
            Return Nothing
        End Function

        Public Property Binder As Binder
    End Class
End Namespace

]]>
    </file>
    <file name="b.vb">
        <![CDATA[
Imports System

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend Class Binder
        Friend Structure DelegateResolutionResult
        End Structure
    End Class
End Namespace

]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="M:Roslyn.Compilers.VisualBasic.BoundAddressOfOperator.GetDelegateResolutionResult(Roslyn.Compilers.VisualBasic.Binder.DelegateResolutionResult@)">
 <returns>The <see cref="T:Roslyn.Compilers.VisualBasic.Binder.DelegateResolutionResult">Binder.DelegateResolutionResult</see> for the conversion </returns>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(658322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658322")>
        <Fact>
        Public Sub Bug658322c()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Namespace Roslyn.Compilers.VisualBasic

    Partial Class BoundAddressOfOperator

        ''' <returns>The <see cref="Binder.DelegateResolutionResult">Binder.DelegateResolutionResult</see> for the conversion </returns>
        Friend Function GetDelegateResolutionResult(ByRef delegateResolutionResult As Binder.DelegateResolutionResult) As Boolean
            Return Nothing
        End Function

        Public Binder As Binder
    End Class
End Namespace

]]>
    </file>
    <file name="b.vb">
        <![CDATA[
Imports System

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend Class Binder
        Friend Structure DelegateResolutionResult
        End Structure
    End Class
End Namespace

]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="M:Roslyn.Compilers.VisualBasic.BoundAddressOfOperator.GetDelegateResolutionResult(Roslyn.Compilers.VisualBasic.Binder.DelegateResolutionResult@)">
 <returns>The <see cref="T:Roslyn.Compilers.VisualBasic.Binder.DelegateResolutionResult">Binder.DelegateResolutionResult</see> for the conversion </returns>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(658322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658322")>
        <Fact>
        Public Sub Bug658322d()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Namespace Roslyn.Compilers.VisualBasic

    Partial Class BoundAddressOfOperator

        ''' <returns>The <see cref="Binder.DelegateResolutionResult">Binder.DelegateResolutionResult</see> for the conversion </returns>
        Friend Function GetDelegateResolutionResult(ByRef delegateResolutionResult As Binder.DelegateResolutionResult) As Boolean
            Return Nothing
        End Function

        Public Function Binder() As Binder
            Return Nothing
        End Function
    End Class
End Namespace

]]>
    </file>
    <file name="b.vb">
        <![CDATA[
Imports System

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend Class Binder
        Friend Structure DelegateResolutionResult
        End Structure
    End Class
End Namespace

]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="M:Roslyn.Compilers.VisualBasic.BoundAddressOfOperator.GetDelegateResolutionResult(Roslyn.Compilers.VisualBasic.Binder.DelegateResolutionResult@)">
 <returns>The <see cref="T:Roslyn.Compilers.VisualBasic.Binder.DelegateResolutionResult">Binder.DelegateResolutionResult</see> for the conversion </returns>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(658322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658322")>
        <Fact()>
        Public Sub Bug658322e()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

Public Class TAttribute : Inherits Attribute
End Class

''' <remarks cref="TAttribute">Clazz</remarks>
<TAttribute>
Public Class Clazz
    ''' <remarks cref="TAttribute">Clazz</remarks>
    <TAttribute>
    Public d As Integer
End Class

''' <remarks cref="TAttribute">Clazz</remarks>
<TAttribute>
Public Enum E1
    ''' <remarks cref="TAttribute">Clazz</remarks>
    <TAttribute> Any
End Enum
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <remarks cref="T:TAttribute">Clazz</remarks>
</member>
<member name="F:Clazz.d">
 <remarks cref="T:TAttribute">Clazz</remarks>
</member>
<member name="T:E1">
 <remarks cref="T:TAttribute">Clazz</remarks>
</member>
<member name="F:E1.Any">
 <remarks cref="T:TAttribute">Clazz</remarks>
</member>
</members>
</doc>
]]>
</xml>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "TAttribute").ToArray()
            Assert.Equal(8, names.Length)

            CheckSymbolInfoAndTypeInfo(model, names(0), "TAttribute")
            CheckSymbolInfoAndTypeInfo(model, names(2), "TAttribute")
            CheckSymbolInfoAndTypeInfo(model, names(4), "TAttribute")
            CheckSymbolInfoAndTypeInfo(model, names(6), "TAttribute")
        End Sub

        <WorkItem(665961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665961")>
        <Fact()>
        Public Sub Bug665961()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Module M
    Sub Main()
        ''' <see cref="x"/>
        Dim x
    End Sub
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42303: XML comment cannot appear within a method or a property. XML comment will be ignored.
        ''' <see cref="x"/>
           ~~~~~~~~~~~~~~~~~
BC42024: Unused local variable: 'x'.
        Dim x
            ~
]]>
</error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "x").ToArray()
            Assert.Equal(1, names.Length)

            CheckSymbolInfoAndTypeInfo(model, names(0))

            Assert.Equal("Public Sub Main()", TryCast(model, SemanticModel).GetEnclosingSymbol(names(0).SpanStart).ToDisplayString())
        End Sub

        <WorkItem(685473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685473")>
        <Fact()>
        Public Sub Bug685473()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System.CodeDom

Namespace ABCD.PRODUCT.Current.SDK.Legacy.PackageGenerator
    '''------------</------------------------------------------------
    '''	Project		: ABCD.PRODUCT.Current.SDK.Legacy
    '''	Class		: ProvideAutomationObject
    '''
    '''------------------------------------------------
    ''' <summary>
    ''' This class models the ProvideAutomationObject attribute
    ''' in	Project		: ABCD.PRODUCT.Current.SDK.Legacyry>
    '''   [user]    11/17/2004    Created
    ''' </history>
    '''-------If---------------------------------------
    P
lic Class ProvideAutomationObject : Inherits VsipCodeAttributeGenerator
        Public ObjectName As String = Nothing
        Public Description As String = Nothing

        '''------------------------------------------------
        ''' <summary>
        ''' Generates the code for this element
        ''' </summary>
        ''' <returns>A string representing the code</returns>
        '''------------------------------------------------
        Public Overrides Function Generate() As String
        ObjectNametr As New CodeAttributeDeclaration(Me.GetAttributeName())
            attr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpressi=n(ObjectName)))
            If Not Description = Nothing
hen
                attr.Arguments.Add(New C------------------------------------------------itiveExpression(Description)))
            End If
            Return PackageCodeGenerator.GetAttributeCode(attr)
        End Functio 
    End Class
End Namespace
]]>
    </file>
</compilation>,
<error>
    <![CDATA[BC42312: XML documentation comments must precede member or type declarations.
    '''------------</------------------------------------------------
       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: XML end element must be preceded by a matching start element. XML comment will be ignored.
    '''------------</------------------------------------------------
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: Character '-' (&H2D) is not allowed at the beginning of an XML name. XML comment will be ignored.
    '''------------</------------------------------------------------
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    '''------------</------------------------------------------------
                                                                     ~
BC42304: XML documentation parse error: Syntax error. XML comment will be ignored.
    '''	Project		: ABCD.PRODUCT.Current.SDK.Legacy
        ~~~~~~~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <summary>
        ~~~~~~~~~
BC42304: XML documentation parse error: End tag </summary> expected. XML comment will be ignored.
    ''' </history>
        ~~~~~~~~~~
BC30188: Declaration expected.
lic Class ProvideAutomationObject : Inherits VsipCodeAttributeGenerator
~~~
BC30002: Type 'VsipCodeAttributeGenerator' is not defined.
lic Class ProvideAutomationObject : Inherits VsipCodeAttributeGenerator
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30027: 'End Function' expected.
        Public Overrides Function Generate() As String
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30284: function 'Generate' cannot be declared 'Overrides' because it does not override a function in a base class.
        Public Overrides Function Generate() As String
                                  ~~~~~~~~
BC30451: 'ObjectNametr' is not declared. It may be inaccessible due to its protection level.
        ObjectNametr As New CodeAttributeDeclaration(Me.GetAttributeName())
        ~~~~~~~~~~~~
BC30201: Expression expected.
        ObjectNametr As New CodeAttributeDeclaration(Me.GetAttributeName())
                     ~
BC30800: Method arguments must be enclosed in parentheses.
        ObjectNametr As New CodeAttributeDeclaration(Me.GetAttributeName())
                     ~
BC30451: 'attr' is not declared. It may be inaccessible due to its protection level.
            attr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpressi=n(ObjectName)))
            ~~~~
BC30002: Type 'CodePrimitiveExpressi' is not defined.
            attr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpressi=n(ObjectName)))
                                                             ~~~~~~~~~~~~~~~~~~~~~
BC30451: 'n' is not declared. It may be inaccessible due to its protection level.
            attr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpressi=n(ObjectName)))
                                                                                   ~
BC30451: 'hen' is not declared. It may be inaccessible due to its protection level.
hen
~~~
BC30451: 'attr' is not declared. It may be inaccessible due to its protection level.
                attr.Arguments.Add(New C------------------------------------------------itiveExpression(Description)))
                ~~~~
BC30002: Type 'C' is not defined.
                attr.Arguments.Add(New C------------------------------------------------itiveExpression(Description)))
                                       ~
BC30451: 'itiveExpression' is not declared. It may be inaccessible due to its protection level.
                attr.Arguments.Add(New C------------------------------------------------itiveExpression(Description)))
                                                                                        ~~~~~~~~~~~~~~~
BC30205: End of statement expected.
                attr.Arguments.Add(New C------------------------------------------------itiveExpression(Description)))
                                                                                                                     ~
BC30451: 'PackageCodeGenerator' is not declared. It may be inaccessible due to its protection level.
            Return PackageCodeGenerator.GetAttributeCode(attr)
                   ~~~~~~~~~~~~~~~~~~~~
BC30451: 'attr' is not declared. It may be inaccessible due to its protection level.
            Return PackageCodeGenerator.GetAttributeCode(attr)
                                                         ~~~~
BC30615: 'End' statement cannot be used in class library projects.
        End Functio 
        ~~~
BC30678: 'End' statement not valid.
        End Functio 
        ~~~
]]>
</error>)
        End Sub

        <Fact>
        Public Sub DocCommentOnUnsupportedSymbol_ParseOnly()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Class E
    ReadOnly Property quoteForTheDay() As String
        ''' <summary></summary>
        Get
            Return "hello"
        End Get
    End Property
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
</members>
</doc>
]]>
</xml>, withDiagnostics:=False)
        End Sub

        <Fact>
        Public Sub EmptyCref()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' See <see cref=""/>.
''' </summary>
''' <remarks></remarks>
Module Module0
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute '' that could not be resolved.
''' See <see cref=""/>.
             ~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:Module0">
 <summary>
 See <see cref="!:"/>.
 </summary>
 <remarks></remarks>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Cref_Error()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' See <see cref="Module0."/>.
''' See <see cref="Module0.
''' "/>.
''' See <see cref="Module0
''' "/>.
''' See <see cref="Module0.'
''' "/>.
''' See <see cref="Module0. _
''' "/>.
''' </summary>
''' <remarks></remarks>
Module Module0
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Module0.' that could not be resolved.
''' See <see cref="Module0."/>.
             ~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Module0.
'''' that could not be resolved.
''' See <see cref="Module0.
             ~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Module0.'' that could not be resolved.
''' See <see cref="Module0.'
             ~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Module0. _' that could not be resolved.
''' See <see cref="Module0. _
             ~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:Module0">
 <summary>
 See <see cref="!:Module0."/>.
 See <see cref="!:Module0.
 "/>.
 See <see cref="T:Module0"/>.
 See <see cref="!:Module0.'
 "/>.
 See <see cref="!:Module0. _
 "/>.
 </summary>
 <remarks></remarks>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Cref_Me_MyBase_MyClass()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class BaseClass
    Public Overridable Sub S()
    End Sub
End Class

Public Class DerivedClass : Inherits BaseClass
    Public Overrides Sub S()
    End Sub

    ''' <summary>
    ''' <see cref="Me.S"/>
    ''' <see cref="MyClass.S"/>
    ''' <see cref="MyBase.S"/>
    ''' </summary>
    Public F As Integer
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Me.S' that could not be resolved.
    ''' <see cref="Me.S"/>
             ~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'MyClass.S' that could not be resolved.
    ''' <see cref="MyClass.S"/>
             ~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'MyBase.S' that could not be resolved.
    ''' <see cref="MyBase.S"/>
             ~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="F:DerivedClass.F">
 <summary>
 <see cref="!:Me.S"/>
 <see cref="!:MyClass.S"/>
 <see cref="!:MyBase.S"/>
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Cref_Type_Namespace_Alias()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports ABC = System.Collections.Generic
Imports ABCD = System.Collections.Generic.IList(Of Integer)

Public Class BaseClass
    ''' <summary>
    ''' <see cref="System.Collections.Generic"/>
    ''' <see cref="System.Collections.Generic.IList(Of Integer)"/>
    ''' <see cref="ABC"/>
    ''' <see cref="ABC.IList(Of Integer)"/>
    ''' <see cref="ABCD"/>
    ''' </summary>
    Public F As Integer
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="F:BaseClass.F">
 <summary>
 <see cref="N:System.Collections.Generic"/>
 <see cref="T:System.Collections.Generic.IList`1"/>
 <see cref="N:System.Collections.Generic"/>
 <see cref="T:System.Collections.Generic.IList`1"/>
 <see cref="T:System.Collections.Generic.IList`1"/>
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Name_Error()
            ' NOTE: the first error is a breaking change
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <typeparam name="X
''' "/>
''' <typeparam name="X 'abc
''' "/>
Class Clazz(Of X)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42317: XML comment type parameter 'X  ' does not match a type parameter on the corresponding 'class' statement.
''' <typeparam name="X
               ~~~~~~~~
BC42317: XML comment type parameter 'X 'abc  ' does not match a type parameter on the corresponding 'class' statement.
''' <typeparam name="X 'abc
               ~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:Clazz`1">
 <typeparam name="X
 "/>
 <typeparam name="X 'abc
 "/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Cref_Error_ParseOnly()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' See <see cref="Module0."/>.
''' See <see cref="Module0.
''' "/>.
''' See <see cref="Module0
''' "/>.
''' See <see cref="Module0.'
''' "/>.
''' See <see cref="Module0. _
''' "/>.
''' </summary>
''' <remarks></remarks>
Module Module0
End Module
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:Module0">
 <summary>
 See <see cref="!:Module0."/>.
 See <see cref="!:Module0.
 "/>.
 See <see cref="T:Module0"/>.
 See <see cref="!:Module0.'
 "/>.
 See <see cref="!:Module0. _
 "/>.
 </summary>
 <remarks></remarks>
</member>
</members>
</doc>
]]>
</xml>, withDiagnostics:=False)
        End Sub

        <Fact>
        Public Sub Name_Error_ParseOnly()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <typeparam name="X
''' "/>
''' <typeparam name="X 'abc
''' "/>
Class Clazz(Of X)
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:Clazz`1">
 <typeparam name="X
 "/>
 <typeparam name="X 'abc
 "/>
</member>
</members>
</doc>
]]>
</xml>, withDiagnostics:=False)
        End Sub

        <Fact>
        Public Sub DiagnosticsWithoutEmit()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="DiagnosticsWithoutEmit">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' See <see cref=""/>.
''' </summary>
''' <remarks></remarks>
Module Module0
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute '' that could not be resolved.
''' See <see cref=""/>.
             ~~~~~~~
]]>
</error>, Nothing)
        End Sub

        <Fact>
        Public Sub GeneralDocCommentOnTypes()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="GeneralDocCommentOnTypes">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' Module M
'''    commented
''' </summary>
Module Module0
End Module

''' <summary>
''' Enum
'''    ---======7777777%%%
''' </summary>
Enum E123
    E1
End Enum

''' <summary>
''' Structure
'''    <a></a> iusgdfas
'''ciii######
''' </summary>
Structure STR
End Structure

''' <summary>
'''    ------ Class --------
'''    With nested structure
'''    ---------------------
''' </summary>
Class Clazz
    ''' <summary>
    ''' NestedStr
    '''   sadjghfcasl
    '''   asdf
    '''   21398470912
    '''ciii######
    ''' </summary>
    Public Structure NestedStr
    End Structure
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
GeneralDocCommentOnTypes
</name>
</assembly>
<members>
<member name="T:Module0">
 <summary>
 Module M
    commented
 </summary>
</member>
<member name="T:E123">
 <summary>
 Enum
    ---======7777777%%%
 </summary>
</member>
<member name="T:STR">
 <summary>
 Structure
    <a></a> iusgdfas
ciii######
 </summary>
</member>
<member name="T:Clazz">
 <summary>
    ------ Class --------
    With nested structure
    ---------------------
 </summary>
</member>
<member name="T:Clazz.NestedStr">
 <summary>
 NestedStr
   sadjghfcasl
   asdf
   21398470912
ciii######
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub MultipartDocCommentOnTypes()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
'''    Class Part #1
'''    -=-=-=-=-=-=-  <aaa> ' Error -- unended tag
'''        (o)
''' </summary>
Public Partial Class Clazz
End Class 

''' <summary>
'''        (o)
'''    Class Part #2
'''    -=-=-=-=-=-=-
''' </summary>
Public Partial Class Clazz
End Class 
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42314: XML comment cannot be applied more than once on a partial class. XML comments for this class will be ignored.
''' <summary>
   ~~~~~~~~~~~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
'''    -=-=-=-=-=-=-  <aaa> ' Error -- unended tag
                      ~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
''' </summary>
    ~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
''' </summary>
    ~
BC42304: XML documentation parse error: XML name expected. XML comment will be ignored.
''' </summary>
    ~
BC42314: XML comment cannot be applied more than once on a partial class. XML comments for this class will be ignored.
''' <summary>
   ~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub DocCommentAndAccessibility()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' 
'''   -=( Clazz(Of X, Y) )=-
''' 
''' </summary>
Public Class Clazz(Of X, Y)
    ''' <summary>
    '''   -=( Clazz(Of X, Y).PublicClazz )=-
    ''' </summary>
    Public Class PublicClazz
    End Class

    ''' <summary>
    '''   -=( Clazz(Of X, Y).PrivateClazz )=-
    ''' </summary>
    Private Class PrivateClazz
    End Class
End Class

''' <summary>
''' 
'''   -=( Clazz(Of X) )=-
''' 
''' </summary>
Friend Class Clazz(Of X)
    ''' <summary>
    '''   -=( Clazz(Of X).PublicClazz )=-
    ''' </summary>
    Public Class PublicClazz
    End Class

    ''' <summary>
    '''   -=( Clazz(Of X).PrivateClazz )=-
    ''' </summary>
    Private Class PrivateClazz
    End Class
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz`2">
 <summary>
 
   -=( Clazz(Of X, Y) )=-
 
 </summary>
</member>
<member name="T:Clazz`2.PublicClazz">
 <summary>
   -=( Clazz(Of X, Y).PublicClazz )=-
 </summary>
</member>
<member name="T:Clazz`2.PrivateClazz">
 <summary>
   -=( Clazz(Of X, Y).PrivateClazz )=-
 </summary>
</member>
<member name="T:Clazz`1">
 <summary>
 
   -=( Clazz(Of X) )=-
 
 </summary>
</member>
<member name="T:Clazz`1.PublicClazz">
 <summary>
   -=( Clazz(Of X).PublicClazz )=-
 </summary>
</member>
<member name="T:Clazz`1.PrivateClazz">
 <summary>
   -=( Clazz(Of X).PrivateClazz )=-
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub IllegalXmlInDocComment()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' 
'''   -=( <a> )=-
''' 
''' </summary>
Public Class Clazz(Of X, Y)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
'''   -=( <a> )=-
          ~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
''' </summary>
    ~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
''' </summary>
    ~
BC42304: XML documentation parse error: XML name expected. XML comment will be ignored.
''' </summary>
    ~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub IllegalXmlInDocComment_Schema()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' 
'''   -=( <a x="1" x="2"/> )=-
''' 
''' </summary>
Public Class Clazz(Of X, Y)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42304: XML documentation parse error: 'x' is a duplicate attribute name. XML comment will be ignored.
''' <summary>
   ~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
</members>
</doc>
]]>
</xml>,
ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub GeneralDocCommentOnFields()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' Class 
'''   Clazz(Of X, Y)
'''     Comment
''' </summary>
Public Class Clazz(Of X, Y)
    ''' <summary>    (*  F1  *)     </summary>
    Public F1 As Integer
    ''' <summary>    
    ''' F@ 2 %
    ''' </summary>
    Private F2 As Integer
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz`2">
 <summary>
 Class 
   Clazz(Of X, Y)
     Comment
 </summary>
</member>
<member name="F:Clazz`2.F1">
 <summary>    (*  F1  *)     </summary>
</member>
<member name="F:Clazz`2.F2">
 <summary>    
 F@ 2 %
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub GeneralDocCommentOnEnumConstants()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' Some 
'''     documentation
'''                  comment
''' </summary>
''' <remarks></remarks>
Public Enum En
    ''' <summary> Just the first value </summary>
    First
    ''' <summary>
    ''' Another value
    ''' </summary> ''' <remarks></remarks>
    Second
End Enum
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:En">
 <summary>
 Some 
     documentation
                  comment
 </summary>
 <remarks></remarks>
</member>
<member name="F:En.First">
 <summary> Just the first value </summary>
</member>
<member name="F:En.Second">
 <summary>
 Another value
 </summary> ''' <remarks></remarks>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub GeneralDocCommentOnEvents()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> Class Clazz(Of X, Y)
''' </summary>
Public Class ubClazz(Of X, Y)
    ''' <summary>
    ''' (*  E(X)  </summary>
    ''' <param name="f1"></param>
    Public Event E(f1 As X)
    ''' <summary>   Sub P(X,Y)  </summary>
    Private Shared Event P As Action(Of X, Y)
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:ubClazz`2">
 <summary> Class Clazz(Of X, Y)
 </summary>
</member>
<member name="E:ubClazz`2.E">
 <summary>
 (*  E(X)  </summary>
 <param name="f1"></param>
</member>
<member name="E:ubClazz`2.P">
 <summary>   Sub P(X,Y)  </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub GeneralDocCommentOnProperties()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> Class Clazz(Of X, Y) </summary>
Public Class ubClazz(Of X, Y)
    ''' <summary>
    ''' P1</summary>
    Public Shared Property P1 As Integer
    ''' <summary>   
    ''' S P(X,Y)  
    ''' </summary>
    ''' <param name="A"></param>
    Private ReadOnly Property P2(a As Integer) As String
        Get
            Return Nothing
        End Get
    End Property
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:ubClazz`2">
 <summary> Class Clazz(Of X, Y) </summary>
</member>
<member name="P:ubClazz`2.P1">
 <summary>
 P1</summary>
</member>
<member name="P:ubClazz`2.P2(System.Int32)">
 <summary>   
 S P(X,Y)  
 </summary>
 <param name="A"></param>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub GeneralDocCommentOnMethods()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> Class Clazz(Of X, Y) </summary>
Partial Public Class Clazz(Of X, Y)
    ''' <summary>.cctor()</summary>
    Shared Sub New()
    End Sub

    ''' <summary> F32(Integer) As Integer </summary>
    ''' <param name="a"></param>
    ''' <returns></returns>
    Protected Function F32(a As Integer) As Integer
        Return Nothing
    End Function

    ''' <summary> a*b </summary>
    Public Shared Operator *(a As Integer, b As Clazz(Of X, Y)) As Clazz(Of Integer, Integer)
        Return Nothing
    End Operator

    ''' <summary>DECL: Priv1(a As Integer)</summary>
    Partial Private Sub Priv1(a As Integer)
    End Sub

    Partial Private Sub Priv2(a As Integer)
    End Sub

    ''' <summary>DECL: Priv3(a As Integer)</summary>
    Partial Private Sub Priv3(a As Integer)
    End Sub

    ''' <summary>DECL: Priv4(a As Integer)</summary>
    Partial Private Sub Priv4(a As Integer)
    End Sub
End Class

Partial Public Class Clazz(Of X, Y)
    ''' <summary>.ctor()</summary>
    Public Sub New()
    End Sub

    ''' <summary> integer -> Clazz(Of X, Y) </summary>
    Public Shared Narrowing Operator CType(a As Integer) As Clazz(Of X, Y)
        Return Nothing
    End Operator

    ''' <summary>IMPL: Priv1(a As Integer)</summary>
    Private Sub Priv1(a As Integer)
    End Sub

    ''' <summary>IMPL: Priv2(a As Integer)</summary>
    Private Sub Priv2(a As Integer)
    End Sub

    Private Sub Priv3(a As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz`2">
 <summary> Class Clazz(Of X, Y) </summary>
</member>
<member name="M:Clazz`2.#cctor">
 <summary>.cctor()</summary>
</member>
<member name="M:Clazz`2.F32(System.Int32)">
 <summary> F32(Integer) As Integer </summary>
 <param name="a"></param>
 <returns></returns>
</member>
<member name="M:Clazz`2.op_Multiply(System.Int32,Clazz{`0,`1})">
 <summary> a*b </summary>
</member>
<member name="M:Clazz`2.Priv1(System.Int32)">
 <summary>IMPL: Priv1(a As Integer)</summary>
</member>
<member name="M:Clazz`2.Priv2(System.Int32)">
 <summary>IMPL: Priv2(a As Integer)</summary>
</member>
<member name="M:Clazz`2.Priv3(System.Int32)">
 <summary>DECL: Priv3(a As Integer)</summary>
</member>
<member name="M:Clazz`2.Priv4(System.Int32)">
 <summary>DECL: Priv4(a As Integer)</summary>
</member>
<member name="M:Clazz`2.#ctor">
 <summary>.ctor()</summary>
</member>
<member name="M:Clazz`2.op_Explicit(System.Int32)~Clazz{`0,`1}">
 <summary> integer -> Clazz(Of X, Y) </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub GeneralDocCommentOnDeclMethods()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> Class [[Clazz]] </summary>
Public Class Clazz
    ''' <summary>
    ''' Declared function DeclareFtn
    ''' </summary>
    Public Declare Function DeclareFtn Lib "bar" () As Integer
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <summary> Class [[Clazz]] </summary>
</member>
<member name="M:Clazz.DeclareFtn">
 <summary>
 Declared function DeclareFtn
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_Summary_C_Code_Example()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary> 
''' Some comment here
'''   and here
''' 
''' <example> e.g. 
'''     <code>
'''        ' No further processing
'''        If docCommentXml Is Nothing Then
'''            Debug.Assert(documentedParameters Is Nothing)
'''            Debug.Assert(documentedTypeParameters Is Nothing)
'''            Return False
'''        End If
'''     </code>
''' Returns <c>False</c> in the statement above.
''' </example>
''' 
''' Done.
''' </summary>
''' <summary a="1"> 
''' </summary>
''' <code>
''' If docCommentXml Is Nothing Then
'''     Return False
''' End If
''' </code>
''' <example> e.g. </example>
''' Returns <c>False</c> in the statement above.
Public Class Clazz
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <summary> 
 Some comment here
   and here
 
 <example> e.g. 
     <code>
        ' No further processing
        If docCommentXml Is Nothing Then
            Debug.Assert(documentedParameters Is Nothing)
            Debug.Assert(documentedTypeParameters Is Nothing)
            Return False
        End If
     </code>
 Returns <c>False</c> in the statement above.
 </example>
 
 Done.
 </summary>
 <summary a="1"> 
 </summary>
 <code>
 If docCommentXml Is Nothing Then
     Return False
 End If
 </code>
 <example> e.g. </example>
 Returns <c>False</c> in the statement above.
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_Exception_Errors()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <exception cref="Exception">Module0</exception>
Public Module Module0
End Module

''' <summary><exception cref="Exception">E inside summary tag</exception></summary>
''' <exception cref="Exception">Clazz</exception>
Public Class Clazz(Of X)
    ''' <summary></summary>
    ''' <exception cref="11111">X1</exception>
    Public Sub X1()
    End Sub
    ''' <summary><exception cref="Exception">E inside summary tag</exception></summary>
    ''' <exception cref="Exception">E</exception>
    Public Event E As Action
    ''' <summary></summary>
    ''' <exception cref="X">X2</exception>
    Public Sub X2()
    End Sub
    ''' <summary></summary>
    ''' <exception cref="Exception">F</exception>
    Public F As Integer
    ''' <summary></summary>
    ''' <exception cref="Exception">P</exception>
    Public Property P As Integer
    ''' <summary></summary>
    ''' <exception cref="Exception">FDelegate</exception>
    Public Delegate Function FDelegate(a As Integer) As String
    ''' <summary></summary>
    ''' <exception cref="Exception">En</exception>
    Public Enum En : A : End Enum
    ''' <summary></summary>
    ''' <exception cref="Exception">STR</exception>
    Public Structure STR : End Structure
    ''' <summary></summary>
    ''' <exception cref="Exception">STR</exception>
    Public ReadOnly Property A(x As String) As String
        Get
            Return x
        End Get
    End Property
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42306: XML comment tag 'exception' is not permitted on a 'module' language element.
''' <exception cref="Exception">Module0</exception>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'exception' is not permitted on a 'class' language element.
''' <summary><exception cref="Exception">E inside summary tag</exception></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'exception' is not permitted on a 'class' language element.
''' <exception cref="Exception">Clazz</exception>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute '11111' that could not be resolved.
    ''' <exception cref="11111">X1</exception>
                   ~~~~~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <exception cref="X">X2</exception>
                   ~~~~~~~~
BC42306: XML comment tag 'exception' is not permitted on a 'variable' language element.
    ''' <exception cref="Exception">F</exception>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'exception' is not permitted on a 'delegate' language element.
    ''' <exception cref="Exception">FDelegate</exception>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'exception' is not permitted on a 'enum' language element.
    ''' <exception cref="Exception">En</exception>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'exception' is not permitted on a 'structure' language element.
    ''' <exception cref="Exception">STR</exception>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Module0">
 <exception cref="T:System.Exception">Module0</exception>
</member>
<member name="T:Clazz`1">
 <summary><exception cref="T:System.Exception">E inside summary tag</exception></summary>
 <exception cref="T:System.Exception">Clazz</exception>
</member>
<member name="M:Clazz`1.X1">
 <summary></summary>
 <exception cref="!:11111">X1</exception>
</member>
<member name="E:Clazz`1.E">
 <summary><exception cref="T:System.Exception">E inside summary tag</exception></summary>
 <exception cref="T:System.Exception">E</exception>
</member>
<member name="M:Clazz`1.X2">
 <summary></summary>
 <exception cref="!:X">X2</exception>
</member>
<member name="F:Clazz`1.F">
 <summary></summary>
 <exception cref="T:System.Exception">F</exception>
</member>
<member name="P:Clazz`1.P">
 <summary></summary>
 <exception cref="T:System.Exception">P</exception>
</member>
<member name="T:Clazz`1.FDelegate">
 <summary></summary>
 <exception cref="T:System.Exception">FDelegate</exception>
</member>
<member name="T:Clazz`1.En">
 <summary></summary>
 <exception cref="T:System.Exception">En</exception>
</member>
<member name="T:Clazz`1.STR">
 <summary></summary>
 <exception cref="T:System.Exception">STR</exception>
</member>
<member name="P:Clazz`1.A(System.String)">
 <summary></summary>
 <exception cref="T:System.Exception">STR</exception>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub QualifiedCref()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz(Of X, Y)
    Public Class MyException : Inherits Exception
        ''' <summary> </summary>
        ''' <exception cref="MyException">Clazz(Of Integer).MyException::S</exception>
        Public Sub S()
        End Sub
    End Class
End Class

Public Class Clazz
    Public Class MyException : Inherits Exception
        ''' <summary> </summary>
        ''' <exception cref="MyException">Clazz(Of Integer).MyException::S</exception>
        Public Sub S()
        End Sub
    End Class
End Class

Public Module Module0
    ''' <summary> </summary>
    ''' <exception cref="Clazz.MyException">Module0::S0</exception>
    Public Sub S0()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="Clazz(Of ).MyException">Module0::S1</exception>
    Public Sub S1()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="Clazz(Of X).MyException">Module0::S2</exception>
    Public Sub S2()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="Clazz(Of X, Y).MyException">Module0::S2</exception>
    Public Sub S2a()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="Global">Module0::S3</exception>
    ''' <exception cref="oBjeCt">Module0::S3:OBJECT</exception>
    Public Sub S3()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="MyOuterException">Module0::S4</exception>
    Public Sub S4()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="MyOuterException(Of )">Module0::S5</exception>
    Public Sub S5()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="MyOuterException(Of T)">Module0::S6</exception>
    Public Sub S6()
    End Sub
    ''' <summary> </summary>
    ''' <exception cref="MyOuterException(Of T, Y)">Module0::S7</exception>
    Public Sub S7()
    End Sub
End Module

Public Class MyOuterException(Of T) : Inherits Exception
    ''' <summary> </summary>
    ''' <exception cref="MyOuterException">MyOuterException(Of )::S</exception>
    Public Sub S()
    End Sub
End Class

Public Class MyOuterException : Inherits Exception
    ''' <summary> </summary>
    ''' <exception cref="MyOuterException(Of X)">MyOuterException::S</exception>
    Public Sub S()
    End Sub
End Class

''' <summary><exception cref="Exception">E inside summary tag</exception></summary>
Public Class Clazz(Of X)

    ''' <summary> </summary>
    ''' <exception cref="MyException">Clazz::S1</exception>
    Public Sub S1()
    End Sub

    ''' <summary> </summary>
    ''' <exception cref="System.Exception">Clazz::S2</exception>
    Public Sub S2()
    End Sub

    ''' <summary> </summary>
    ''' <exception cref="Global.System.Exception">Clazz::S3</exception>
    Public Sub S3()
    End Sub

    Public Class MyException : Inherits Exception
        ''' <summary> </summary>
        ''' <exception cref="MyException">MyException::S</exception>
        Public Sub S()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz(Of ).MyException' that could not be resolved.
    ''' <exception cref="Clazz(Of ).MyException">Module0::S1</exception>
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Global' that could not be resolved.
    ''' <exception cref="Global">Module0::S3</exception>
                   ~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'MyOuterException(Of )' that could not be resolved.
    ''' <exception cref="MyOuterException(Of )">Module0::S5</exception>
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'MyOuterException(Of T, Y)' that could not be resolved.
    ''' <exception cref="MyOuterException(Of T, Y)">Module0::S7</exception>
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'exception' is not permitted on a 'class' language element.
''' <summary><exception cref="Exception">E inside summary tag</exception></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="M:Clazz`2.MyException.S">
 <summary> </summary>
 <exception cref="T:Clazz`2.MyException">Clazz(Of Integer).MyException::S</exception>
</member>
<member name="M:Clazz.MyException.S">
 <summary> </summary>
 <exception cref="T:Clazz.MyException">Clazz(Of Integer).MyException::S</exception>
</member>
<member name="M:Module0.S0">
 <summary> </summary>
 <exception cref="T:Clazz.MyException">Module0::S0</exception>
</member>
<member name="M:Module0.S1">
 <summary> </summary>
 <exception cref="!:Clazz(Of ).MyException">Module0::S1</exception>
</member>
<member name="M:Module0.S2">
 <summary> </summary>
 <exception cref="T:Clazz`1.MyException">Module0::S2</exception>
</member>
<member name="M:Module0.S2a">
 <summary> </summary>
 <exception cref="T:Clazz`2.MyException">Module0::S2</exception>
</member>
<member name="M:Module0.S3">
 <summary> </summary>
 <exception cref="!:Global">Module0::S3</exception>
 <exception cref="T:System.Object">Module0::S3:OBJECT</exception>
</member>
<member name="M:Module0.S4">
 <summary> </summary>
 <exception cref="T:MyOuterException">Module0::S4</exception>
</member>
<member name="M:Module0.S5">
 <summary> </summary>
 <exception cref="!:MyOuterException(Of )">Module0::S5</exception>
</member>
<member name="M:Module0.S6">
 <summary> </summary>
 <exception cref="T:MyOuterException`1">Module0::S6</exception>
</member>
<member name="M:Module0.S7">
 <summary> </summary>
 <exception cref="!:MyOuterException(Of T, Y)">Module0::S7</exception>
</member>
<member name="M:MyOuterException`1.S">
 <summary> </summary>
 <exception cref="T:MyOuterException">MyOuterException(Of )::S</exception>
</member>
<member name="M:MyOuterException.S">
 <summary> </summary>
 <exception cref="T:MyOuterException`1">MyOuterException::S</exception>
</member>
<member name="T:Clazz`1">
 <summary><exception cref="T:System.Exception">E inside summary tag</exception></summary>
</member>
<member name="M:Clazz`1.S1">
 <summary> </summary>
 <exception cref="T:Clazz`1.MyException">Clazz::S1</exception>
</member>
<member name="M:Clazz`1.S2">
 <summary> </summary>
 <exception cref="T:System.Exception">Clazz::S2</exception>
</member>
<member name="M:Clazz`1.S3">
 <summary> </summary>
 <exception cref="T:System.Exception">Clazz::S3</exception>
</member>
<member name="M:Clazz`1.MyException.S">
 <summary> </summary>
 <exception cref="T:Clazz`1.MyException">MyException::S</exception>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub QualifiedCref_More()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1
    Public Sub Main()
    End Sub
    Public Property PRST As String
    Public Event EVNT As action
End Module

Public Class BaseClass
    ''' <summary>
    ''' <reference cref="Module1"/>
    ''' <reference cref="Module1.PRST"/>
    ''' <reference cref="Module1.get_PRST"/>
    ''' <reference cref="Module1.EVNT"/>
    ''' <reference cref="Module1.add_EVNT"/>
    ''' <reference cref="BaseClass.New"/>
    ''' <reference cref="BaseClass.op_multiply"/>
    ''' <reference cref="BaseClass.op_explicit"/>
    ''' </summary>
    Public F As Integer

    Public Shared Operator *(bc As BaseClass, i As Integer) As BaseClass
        Return bc
    End Operator
    Public Shared Narrowing Operator CType(bc As BaseClass) As String
        Return Nothing
    End Operator
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Module1.get_PRST' that could not be resolved.
    ''' <reference cref="Module1.get_PRST"/>
                   ~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Module1.add_EVNT' that could not be resolved.
    ''' <reference cref="Module1.add_EVNT"/>
                   ~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'BaseClass.New' that could not be resolved.
    ''' <reference cref="BaseClass.New"/>
                   ~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:BaseClass.F">
 <summary>
 <reference cref="T:Module1"/>
 <reference cref="P:Module1.PRST"/>
 <reference cref="!:Module1.get_PRST"/>
 <reference cref="E:Module1.EVNT"/>
 <reference cref="!:Module1.add_EVNT"/>
 <reference cref="!:BaseClass.New"/>
 <reference cref="M:BaseClass.op_Multiply(BaseClass,System.Int32)"/>
 <reference cref="M:BaseClass.op_Explicit(BaseClass)~System.String"/>
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub QualifiedCref_GenericMethod()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' 1) <see cref="Foo.Method"/>
''' 2) <see cref="Foo.Method(Of T)"/>
''' 3) <see cref="Foo.Method(Of T, U)"/>
''' 4) <see cref="Foo.Method(Of )"/>
''' 5) <see cref="Foo.Method(Of ,)"/>
''' </summary>
Public Class Foo
Public Sub Method()
End Sub
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Foo.Method(Of T)' that could not be resolved.
''' 2) <see cref="Foo.Method(Of T)"/>
            ~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Foo.Method(Of T, U)' that could not be resolved.
''' 3) <see cref="Foo.Method(Of T, U)"/>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Foo.Method(Of )' that could not be resolved.
''' 4) <see cref="Foo.Method(Of )"/>
            ~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Foo.Method(Of ,)' that could not be resolved.
''' 5) <see cref="Foo.Method(Of ,)"/>
            ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Foo">
 <summary>
 1) <see cref="M:Foo.Method"/>
 2) <see cref="!:Foo.Method(Of T)"/>
 3) <see cref="!:Foo.Method(Of T, U)"/>
 4) <see cref="!:Foo.Method(Of )"/>
 5) <see cref="!:Foo.Method(Of ,)"/>
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Cref_Scopes()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
' NOTE: The first "tostring" did not resolve in dev11.

''' <see cref="c.tostring"/>
''' <see cref="tostring"/>
Public Class C(Of X, Y)
    ''' <see cref="c.tostring"/>
    ''' <see cref="tostring"/>
    Public Sub New()
    End Sub
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C`2">
 <see cref="M:System.Object.ToString"/>
 <see cref="M:System.Object.ToString"/>
</member>
<member name="M:C`2.#ctor">
 <see cref="M:System.Object.ToString"/>
 <see cref="M:System.Object.ToString"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_Summary_Permission_See_SeeAlso_List_Para()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary>
''' This is the entry point of the Point class testing program.
''' <para>This program tests each method and operator, and
''' is intended to be run after any non-trivial maintenance has
''' been performed on the Point class.</para>
''' </summary>
''' <permission cref="System.Security.PermissionSet">
''' Everyone can access this class.<see cref="List(Of X)"/>
''' </permission>
Public Class TestClass
    ''' <remarks>
    ''' Here is an example of a bulleted list:
    ''' <list type="bullet">
    '''     <listheader>
    '''         <term>term</term>
    '''         <description>description</description>
    '''     </listheader>
    '''     <item>
    '''         <term>A</term>
    '''         <description>Item 1.</description>
    '''     </item>
    '''     <item>
    '''         <description>Item 2.</description>
    '''     </item>
    ''' </list>
    ''' </remarks>
    ''' <list type="bullet">
    '''     <item>
    '''        <description>Item 1.</description>
    '''        <seealso cref="TestClass"/>
    '''     </item>
    ''' </list>
    Public Shared Sub Main()
        Dim a As TestClass = Nothing
    End Sub
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <summary>
 This is the entry point of the Point class testing program.
 <para>This program tests each method and operator, and
 is intended to be run after any non-trivial maintenance has
 been performed on the Point class.</para>
 </summary>
 <permission cref="T:System.Security.PermissionSet">
 Everyone can access this class.<see cref="T:System.Collections.Generic.List`1"/>
 </permission>
</member>
<member name="M:TestClass.Main">
 <remarks>
 Here is an example of a bulleted list:
 <list type="bullet">
     <listheader>
         <term>term</term>
         <description>description</description>
     </listheader>
     <item>
         <term>A</term>
         <description>Item 1.</description>
     </item>
     <item>
         <description>Item 2.</description>
     </item>
 </list>
 </remarks>
 <list type="bullet">
     <item>
        <description>Item 1.</description>
        <seealso cref="T:TestClass"/>
     </item>
 </list>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_ParamRef()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary>
''' <paramref name="P1"></paramref>
''' </summary>
''' <paramref name="P2"></paramref>
Public Class TestClass
    ''' <summary>
    ''' <paramref name="P3"></paramref>
    ''' </summary>
    ''' <paramref name="P4"></paramref>
    ''' <paramref></paramref>
    Public Shared Sub M(p3 As Integer, p4 As String)
        Dim a As TestClass = Nothing
    End Sub

    ''' <summary>
    ''' <paramref name="P5"></paramref>
    ''' </summary>
    ''' <paramref name="P6"></paramref>
    Public F As Integer

    ''' <summary>
    ''' <paramref name="P7"></paramref>
    ''' </summary>
    ''' <paramref name="P8"></paramref>
    Public Property P As Integer

    ''' <summary>
    ''' <paramref name="P9"></paramref>
    ''' </summary>
    ''' <paramref name="P10"></paramref>
    Public ReadOnly Property P(P9 As String) As Integer
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary>
    ''' <paramref name="P11"></paramref>
    ''' </summary>
    ''' <paramref name="P12"></paramref>
    Public Event EE(p11 As String)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.
''' <paramref name="P1"></paramref>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.
''' <paramref name="P2"></paramref>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.
    ''' <paramref name="P5"></paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.
    ''' <paramref name="P6"></paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'P7' does not match a parameter on the corresponding 'property' statement.
    ''' <paramref name="P7"></paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'P8' does not match a parameter on the corresponding 'property' statement.
    ''' <paramref name="P8"></paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'P10' does not match a parameter on the corresponding 'property' statement.
    ''' <paramref name="P10"></paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'P12' does not match a parameter on the corresponding 'event' statement.
    ''' <paramref name="P12"></paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <summary>
 <paramref name="P1"></paramref>
 </summary>
 <paramref name="P2"></paramref>
</member>
<member name="M:TestClass.M(System.Int32,System.String)">
 <summary>
 <paramref name="P3"></paramref>
 </summary>
 <paramref name="P4"></paramref>
 <paramref></paramref>
</member>
<member name="F:TestClass.F">
 <summary>
 <paramref name="P5"></paramref>
 </summary>
 <paramref name="P6"></paramref>
</member>
<member name="P:TestClass.P">
 <summary>
 <paramref name="P7"></paramref>
 </summary>
 <paramref name="P8"></paramref>
</member>
<member name="P:TestClass.P(System.String)">
 <summary>
 <paramref name="P9"></paramref>
 </summary>
 <paramref name="P10"></paramref>
</member>
<member name="E:TestClass.EE">
 <summary>
 <paramref name="P11"></paramref>
 </summary>
 <paramref name="P12"></paramref>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_ParamRef_NoErrors()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary>
''' <paramref name="P1"></paramref>
''' </summary>
''' <paramref name="P2"></paramref>
Public Class TestClass
    ''' <summary>
    ''' <paramref name="P3"></paramref>
    ''' </summary>
    ''' <paramref name="P4"></paramref>
    ''' <paramref></paramref>
    Public Shared Sub M(p3 As Integer, p4 As String)
        Dim a As TestClass = Nothing
    End Sub

    ''' <summary>
    ''' <paramref name="P5"></paramref>
    ''' </summary>
    ''' <paramref name="P6"></paramref>
    Public F As Integer

    ''' <summary>
    ''' <paramref name="P7"></paramref>
    ''' </summary>
    ''' <paramref name="P8"></paramref>
    Public Property P As Integer

    ''' <summary>
    ''' <paramref name="P9"></paramref>
    ''' </summary>
    ''' <paramref name="P10"></paramref>
    Public ReadOnly Property P(P9 As String) As Integer
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary>
    ''' <paramref name="P11"></paramref>
    ''' </summary>
    ''' <paramref name="P12"></paramref>
    Public Event EE(p11 As String)
End Class
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <summary>
 <paramref name="P1"></paramref>
 </summary>
 <paramref name="P2"></paramref>
</member>
<member name="M:TestClass.M(System.Int32,System.String)">
 <summary>
 <paramref name="P3"></paramref>
 </summary>
 <paramref name="P4"></paramref>
 <paramref></paramref>
</member>
<member name="F:TestClass.F">
 <summary>
 <paramref name="P5"></paramref>
 </summary>
 <paramref name="P6"></paramref>
</member>
<member name="P:TestClass.P">
 <summary>
 <paramref name="P7"></paramref>
 </summary>
 <paramref name="P8"></paramref>
</member>
<member name="P:TestClass.P(System.String)">
 <summary>
 <paramref name="P9"></paramref>
 </summary>
 <paramref name="P10"></paramref>
</member>
<member name="E:TestClass.EE">
 <summary>
 <paramref name="P11"></paramref>
 </summary>
 <paramref name="P12"></paramref>
</member>
</members>
</doc>
]]>
</xml>, withDiagnostics:=False)
        End Sub

        <Fact>
        Public Sub Tags_Returns()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary>
''' <paramref name="P1"></paramref>
''' </summary>
''' <returns>TestClass</returns>
Public Class TestClass

    ''' <returns>EN</returns>
    Public Enum EN : A : End Enum

    ''' <returns>DelSub</returns>
    Public Delegate Sub DelSub(a As Integer)

    ''' <returns>DelFunc</returns>
    Public Delegate Function DelFunc(a As Integer) As Integer

    ''' <returns>MSub</returns>
    Public Shared Sub MSub(p3 As Integer, p4 As String)
    End Sub

    ''' <returns>MFunc</returns>
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function

    ''' <summary><returns nested="true">Field</returns></summary>
    ''' <returns>Field</returns>
    Public Field As Integer

    ''' <returns>FieldWE</returns>
    WithEvents FieldWE As TestClass

    ''' <returns>DeclareFtn</returns>
    Public Declare Function DeclareFtn Lib "bar" () As Integer

    ''' <returns>DeclareSub</returns>
    Public Declare Sub DeclareSub Lib "bar" ()

    ''' <returns>PReadOnly</returns>
    Public ReadOnly Property PReadOnly As Integer
        Get
            Return Nothing
        End Get
    End Property

    ''' <returns>PReadWrite</returns>
    Public Property PReadWrite As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)
        End Set
    End Property

    ''' <returns>PWriteOnly</returns>
    Public WriteOnly Property PWriteOnly As Integer
        Set(value As Integer)
        End Set
    End Property

    ''' <returns>EE</returns>
    Public Event EE(p11 As String)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.
''' <paramref name="P1"></paramref>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'class' language element.
''' <returns>TestClass</returns>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'enum' language element.
    ''' <returns>EN</returns>
        ~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'delegate sub' language element.
    ''' <returns>DelSub</returns>
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'sub' language element.
    ''' <returns>MSub</returns>
        ~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'variable' language element.
    ''' <summary><returns nested="true">Field</returns></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'variable' language element.
    ''' <returns>Field</returns>
        ~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'WithEvents variable' language element.
    ''' <returns>FieldWE</returns>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42315: XML comment tag 'returns' is not permitted on a 'declare sub' language element.
    ''' <returns>DeclareSub</returns>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42313: XML comment tag 'returns' is not permitted on a 'WriteOnly' Property.
    ''' <returns>PWriteOnly</returns>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'returns' is not permitted on a 'event' language element.
    ''' <returns>EE</returns>
        ~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <summary>
 <paramref name="P1"></paramref>
 </summary>
 <returns>TestClass</returns>
</member>
<member name="T:TestClass.EN">
 <returns>EN</returns>
</member>
<member name="T:TestClass.DelSub">
 <returns>DelSub</returns>
</member>
<member name="T:TestClass.DelFunc">
 <returns>DelFunc</returns>
</member>
<member name="M:TestClass.MSub(System.Int32,System.String)">
 <returns>MSub</returns>
</member>
<member name="M:TestClass.MFunc(System.Int32,System.String)">
 <returns>MFunc</returns>
</member>
<member name="F:TestClass.Field">
 <summary><returns nested="true">Field</returns></summary>
 <returns>Field</returns>
</member>
<member name="F:TestClass._FieldWE">
 <returns>FieldWE</returns>
</member>
<member name="M:TestClass.DeclareFtn">
 <returns>DeclareFtn</returns>
</member>
<member name="M:TestClass.DeclareSub">
 <returns>DeclareSub</returns>
</member>
<member name="P:TestClass.PReadOnly">
 <returns>PReadOnly</returns>
</member>
<member name="P:TestClass.PReadWrite">
 <returns>PReadWrite</returns>
</member>
<member name="P:TestClass.PWriteOnly">
 <returns>PWriteOnly</returns>
</member>
<member name="E:TestClass.EE">
 <returns>EE</returns>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_Param()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary><param name="P_outer + aaa">@TestClass</param></summary>
''' <param name="P">@TestClass</param>
Public Class TestClass
    ''' <param name="P">@EN</param>
    Public Enum EN : A : End Enum

    ''' <param name="a">@DelSub</param>
    Public Delegate Sub DelSub(a As Integer)

    ''' <param name="a">@DelFunc</param>
    ''' <summary><param name="P_outer + aaa">@TestClass</param></summary>
    Public Delegate Function DelFunc(a As Integer) As Integer

    ''' <param name="a">@MSub</param>
    Public Shared Sub MSub(p3 As Integer, p4 As String)
    End Sub

    ''' <param name="">@MSubWithErrors1</param>
    Public Shared Sub MSubWithErrors1(p3 As Integer, p4 As String)
    End Sub

    ''' <param name="1">@MSubWithErrors2</param>
    Public Shared Sub MSubWithErrors2(p3 As Integer, p4 As String)
    End Sub

    ''' <param>@MSubWithErrors3</param>
    Public Shared Sub MSubWithErrors3(p3 As Integer, p4 As String)
    End Sub

    ''' <param name="p3">@MFunc</param>
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function

    ''' <param name="p3">@Field</param>
    Public Field As Integer

    ''' <param name="p3">@DeclareFtn</param>
    Public Declare Function DeclareFtn Lib "bar" (p3 As Integer) As Integer

    ''' <param name="p">@PReadOnly</param>
    Public ReadOnly Property PReadOnly(p As Integer) As Integer
        Get
            Return Nothing
        End Get
    End Property

    ''' <param name="p">@PReadWrite</param>
    Public Property PReadWrite As Integer

    ''' <param name="ppp">@EVE</param>
    Public Event EVE(ppp As Integer)

    ''' <param name="paramName">@EVE2</param>
    Public Event EVE2 As Action(Of Integer)

    ''' <param name="arg1">@EVE3</param>
    ''' <param name="arg2">@EVE3</param>
    Public Event EVE3 As Action(Of Integer, Integer)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42306: XML comment tag 'param' is not permitted on a 'class' language element.
''' <summary><param name="P_outer + aaa">@TestClass</param></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'class' language element.
''' <param name="P">@TestClass</param>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'enum' language element.
    ''' <param name="P">@EN</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'P_outer + aaa' does not match a parameter on the corresponding 'function' statement.
    ''' <summary><param name="P_outer + aaa">@TestClass</param></summary>
                        ~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'a' does not match a parameter on the corresponding 'sub' statement.
    ''' <param name="a">@MSub</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter '' does not match a parameter on the corresponding 'sub' statement.
    ''' <param name="">@MSubWithErrors1</param>
               ~~~~~~~
BC42307: XML comment parameter '1' does not match a parameter on the corresponding 'sub' statement.
    ''' <param name="1">@MSubWithErrors2</param>
               ~~~~~~~~
BC42308: XML comment parameter must have a 'name' attribute.
    ''' <param>@MSubWithErrors3</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.
    ''' <param name="p3">@Field</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'p' does not match a parameter on the corresponding 'property' statement.
    ''' <param name="p">@PReadWrite</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'paramName' does not match a parameter on the corresponding 'event' statement.
    ''' <param name="paramName">@EVE2</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <summary><param name="P_outer + aaa">@TestClass</param></summary>
 <param name="P">@TestClass</param>
</member>
<member name="T:TestClass.EN">
 <param name="P">@EN</param>
</member>
<member name="T:TestClass.DelSub">
 <param name="a">@DelSub</param>
</member>
<member name="T:TestClass.DelFunc">
 <param name="a">@DelFunc</param>
 <summary><param name="P_outer + aaa">@TestClass</param></summary>
</member>
<member name="M:TestClass.MSub(System.Int32,System.String)">
 <param name="a">@MSub</param>
</member>
<member name="M:TestClass.MSubWithErrors1(System.Int32,System.String)">
 <param name="">@MSubWithErrors1</param>
</member>
<member name="M:TestClass.MSubWithErrors2(System.Int32,System.String)">
 <param name="1">@MSubWithErrors2</param>
</member>
<member name="M:TestClass.MSubWithErrors3(System.Int32,System.String)">
 <param>@MSubWithErrors3</param>
</member>
<member name="M:TestClass.MFunc(System.Int32,System.String)">
 <param name="p3">@MFunc</param>
</member>
<member name="F:TestClass.Field">
 <param name="p3">@Field</param>
</member>
<member name="M:TestClass.DeclareFtn(System.Int32)">
 <param name="p3">@DeclareFtn</param>
</member>
<member name="P:TestClass.PReadOnly(System.Int32)">
 <param name="p">@PReadOnly</param>
</member>
<member name="P:TestClass.PReadWrite">
 <param name="p">@PReadWrite</param>
</member>
<member name="E:TestClass.EVE">
 <param name="ppp">@EVE</param>
</member>
<member name="E:TestClass.EVE2">
 <param name="paramName">@EVE2</param>
</member>
<member name="E:TestClass.EVE3">
 <param name="arg1">@EVE3</param>
 <param name="arg2">@EVE3</param>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_Param_10Plus()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

Public Class TestClass
    ''' <param name="a1"/>
    ''' <param name="a14"/>
    Private Sub PS(a0 As Integer, a1 As Integer, a2 As Integer, a3 As Integer, a4 As Integer,
                   a5 As Integer, a6 As Integer, a7 As Integer, a8 As Integer, a9 As Integer,
                   a10 As Integer, a11 As Integer, a12 As Integer, a13 As Integer, a14 As Integer)
    End Sub
End Class

]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="M:TestClass.PS(System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)">
 <param name="a1"/>
 <param name="a14"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_Value()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary><param name="P_outer + aaa"/>@TestClass</summary>
''' <value>@TestClass</value>
Public Class TestClass
    ''' <value>@EN</value>
    Public Enum EN : A : End Enum

    ''' <value>@STR</value>
    Public Structure STR : End Structure

    ''' <value>@INTERF</value>
    Public Interface INTERF : End Interface

    ''' <value>@DelSub</value>
    Public Delegate Sub DelSub(a As Integer)

    ''' <value>@DelFunc</value>
    Public Delegate Function DelFunc(a As Integer) As Integer

    ''' <value>@MSub</value>
    Public Shared Sub MSub(p3 As Integer, p4 As String)
    End Sub

    ''' <value>@MFunc</value>
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function

    ''' <value>@DeclareFtn</value>
    Public Declare Function DeclareFtn Lib "bar" (p3 As Integer) As Integer

    ''' <value>@Field</value>
    Public Field As Integer

    ''' <value>@PWriteOnly</value>
    Public WriteOnly Property PWriteOnly(p As Integer) As Integer
        Set(value As Integer)
        End Set
    End Property

    ''' <value>@PReadWrite</value>
    Public Property PReadWrite As Integer

    ''' <value>@EVE</value>
    Public Event EVE(ppp As Integer)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42306: XML comment tag 'param' is not permitted on a 'class' language element.
''' <summary><param name="P_outer + aaa"/>@TestClass</summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'class' language element.
''' <value>@TestClass</value>
    ~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'enum' language element.
    ''' <value>@EN</value>
        ~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'structure' language element.
    ''' <value>@STR</value>
        ~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'interface' language element.
    ''' <value>@INTERF</value>
        ~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'delegate' language element.
    ''' <value>@DelSub</value>
        ~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'delegate' language element.
    ''' <value>@DelFunc</value>
        ~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'sub' language element.
    ''' <value>@MSub</value>
        ~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'function' language element.
    ''' <value>@MFunc</value>
        ~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'declare' language element.
    ''' <value>@DeclareFtn</value>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'variable' language element.
    ''' <value>@Field</value>
        ~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'value' is not permitted on a 'event' language element.
    ''' <value>@EVE</value>
        ~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <summary><param name="P_outer + aaa"/>@TestClass</summary>
 <value>@TestClass</value>
</member>
<member name="T:TestClass.EN">
 <value>@EN</value>
</member>
<member name="T:TestClass.STR">
 <value>@STR</value>
</member>
<member name="T:TestClass.INTERF">
 <value>@INTERF</value>
</member>
<member name="T:TestClass.DelSub">
 <value>@DelSub</value>
</member>
<member name="T:TestClass.DelFunc">
 <value>@DelFunc</value>
</member>
<member name="M:TestClass.MSub(System.Int32,System.String)">
 <value>@MSub</value>
</member>
<member name="M:TestClass.MFunc(System.Int32,System.String)">
 <value>@MFunc</value>
</member>
<member name="M:TestClass.DeclareFtn(System.Int32)">
 <value>@DeclareFtn</value>
</member>
<member name="F:TestClass.Field">
 <value>@Field</value>
</member>
<member name="P:TestClass.PWriteOnly(System.Int32)">
 <value>@PWriteOnly</value>
</member>
<member name="P:TestClass.PReadWrite">
 <value>@PReadWrite</value>
</member>
<member name="E:TestClass.EVE">
 <value>@EVE</value>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub Tags_TypeParam()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <typeparam name="X">@Module0</typeparam>
Public Module Module0
End Module

''' <summary><param name="P_outer + aaa"/>@TestClass</summary>
''' <typeparam>@TestClass</typeparam>
Public Class TestClass
    ''' <typeparam name="X">@EN</typeparam>
    Public Enum EN : A : End Enum

    ''' <typeparam name="X">@STR</typeparam>
    Public Structure STR(Of X) : End Structure

    ''' <typeparam name="Y">@INTERF</typeparam>
    Public Interface INTERF(Of X, Y) : End Interface

    ''' <typeparam name="W">@DelSub</typeparam>
    Public Delegate Sub DelSub(Of W)(a As Integer)

    ''' <typeparam name="UV">@DelFunc</typeparam>
    Public Delegate Function DelFunc(Of W)(a As Integer) As Integer

    ''' <typeparam name="TT">@MSub</typeparam>
    Public Shared Sub MSub(Of TT)(p3 As Integer, p4 As String)
    End Sub

    ''' <typeparam name="TT">@MFunc</typeparam>
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function

    ''' <typeparam name="TT">@Field</typeparam>
    Public Field As Integer

    ''' <typeparam name="TT">@DeclareFtn</typeparam>
    Public Declare Function DeclareFtn Lib "bar" (p3 As Integer) As Integer

    ''' <typeparam name="TT">@PWriteOnly</typeparam>
    Public WriteOnly Property PWriteOnly(p As Integer) As Integer
        Set(value As Integer)
        End Set
    End Property

    ''' <typeparam name="TT">@PReadWrite</typeparam>
    Public Property PReadWrite As Integer

    ''' <typeparam name="TT">@EVE</typeparam>
    Public Event EVE(ppp As Integer)
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42306: XML comment tag 'typeparam' is not permitted on a 'module' language element.
''' <typeparam name="X">@Module0</typeparam>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'class' language element.
''' <summary><param name="P_outer + aaa"/>@TestClass</summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42318: XML comment type parameter must have a 'name' attribute.
''' <typeparam>@TestClass</typeparam>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'enum' language element.
    ''' <typeparam name="X">@EN</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42317: XML comment type parameter 'UV' does not match a type parameter on the corresponding 'delegate' statement.
    ''' <typeparam name="UV">@DelFunc</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42317: XML comment type parameter 'TT' does not match a type parameter on the corresponding 'function' statement.
    ''' <typeparam name="TT">@MFunc</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'variable' language element.
    ''' <typeparam name="TT">@Field</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'declare' language element.
    ''' <typeparam name="TT">@DeclareFtn</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
    ''' <typeparam name="TT">@PWriteOnly</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
    ''' <typeparam name="TT">@PReadWrite</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'event' language element.
    ''' <typeparam name="TT">@EVE</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Module0">
 <typeparam name="X">@Module0</typeparam>
</member>
<member name="T:TestClass">
 <summary><param name="P_outer + aaa"/>@TestClass</summary>
 <typeparam>@TestClass</typeparam>
</member>
<member name="T:TestClass.EN">
 <typeparam name="X">@EN</typeparam>
</member>
<member name="T:TestClass.STR`1">
 <typeparam name="X">@STR</typeparam>
</member>
<member name="T:TestClass.INTERF`2">
 <typeparam name="Y">@INTERF</typeparam>
</member>
<member name="T:TestClass.DelSub`1">
 <typeparam name="W">@DelSub</typeparam>
</member>
<member name="T:TestClass.DelFunc`1">
 <typeparam name="UV">@DelFunc</typeparam>
</member>
<member name="M:TestClass.MSub``1(System.Int32,System.String)">
 <typeparam name="TT">@MSub</typeparam>
</member>
<member name="M:TestClass.MFunc(System.Int32,System.String)">
 <typeparam name="TT">@MFunc</typeparam>
</member>
<member name="F:TestClass.Field">
 <typeparam name="TT">@Field</typeparam>
</member>
<member name="M:TestClass.DeclareFtn(System.Int32)">
 <typeparam name="TT">@DeclareFtn</typeparam>
</member>
<member name="P:TestClass.PWriteOnly(System.Int32)">
 <typeparam name="TT">@PWriteOnly</typeparam>
</member>
<member name="P:TestClass.PReadWrite">
 <typeparam name="TT">@PReadWrite</typeparam>
</member>
<member name="E:TestClass.EVE">
 <typeparam name="TT">@EVE</typeparam>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub BC42300WRN_XMLDocBadXMLLine()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System
Class C1
    '''<remarks>this XML comment does not immediately appear before any type</remarks>

    '''<remarks>Line#2</remarks>
    ' this is a regular comment
    Interface I1
    End Interface
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42301: Only one XML comment block is allowed per language element.
    '''<remarks>this XML comment does not immediately appear before any type</remarks>
       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42300: XML comment block must immediately precede the language element to which it applies. XML comment will be ignored.
    '''<remarks>Line#2</remarks>
       ~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub BC42301WRN_XMLDocMoreThanOneCommentBlock()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

'''<remarks>Line#1</remarks>
'comment
'''<remarks>Line#2</remarks>
Class C1
    ' this is a regular comment
    '''<remarks>this XML comment does not immediately appear before any type</remarks>

    '''<remarks>Line#2</remarks>
    Interface I1
    End Interface
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42301: Only one XML comment block is allowed per language element.
'''<remarks>Line#1</remarks>
   ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42301: Only one XML comment block is allowed per language element.
    '''<remarks>this XML comment does not immediately appear before any type</remarks>
       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C1">
<remarks>Line#2</remarks>
</member>
<member name="T:C1.I1">
<remarks>Line#2</remarks>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub WRN_XMLDocInsideMethod()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

Module Module11
    Public x As Object = Function()
                                '''
                                Return 1
                            End Function

    Public y As Object = Function() _
                                ''' _

    Sub Main2()
        '''
    End Sub
    Public Property PPP As Object = Function() 1 ''' 1
    Public Property PPP2 As Object = Function()
                                            ''' 
                                            Return 1
                                        End Function
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42303: XML comment cannot appear within a method or a property. XML comment will be ignored.
                                '''
                                   ~
BC36674: Multiline lambda expression is missing 'End Function'.
    Public y As Object = Function() _
                         ~~~~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    Public y As Object = Function() _
                                    ~
BC42303: XML comment cannot appear within a method or a property. XML comment will be ignored.
        '''
           ~
BC42302: XML comment must be the first statement on a line. XML comment will be ignored.
    Public Property PPP As Object = Function() 1 ''' 1
                                                 ~~~~~
BC42303: XML comment cannot appear within a method or a property. XML comment will be ignored.
                                            ''' 
                                               ~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="M:Module11.Main2">
 _
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Public Sub WRN_XMLDocInsideMethod_NoError()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

Module Module11
    Public x As Object = Function()
                                '''
                                Return 1
                            End Function

    Public y As Object = Function() _
                                ''' _

    Sub Main2()
        '''
    End Sub
    Public Property PPP As Object = Function() 1 ''' 1
    Public Property PPP2 As Object = Function()
                                            ''' 
                                            Return 1
                                        End Function
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC36674: Multiline lambda expression is missing 'End Function'.
    Public y As Object = Function() _
                         ~~~~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    Public y As Object = Function() _
                                    ~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="M:Module11.Main2">
 _
</member>
</members>
</doc>
]]>
</xml>,
withDiagnostics:=False)
        End Sub

        <Fact()>
        Public Sub BC42305WRN_XMLDocDuplicateXMLNode()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

''' <summary cref="a b">
''' </summary>
''' <typeparam name="X"></typeparam>
''' <typeparam name=" X " />
''' <typeparam name="Y"></typeparam>
''' <typeparam name="X"></typeparam>
''' <summary cref="a B "/>
''' <summary cref="     a b "/>
Public Class C(Of X, Y)
    ''' <include file=" a.vb" path=" c:\ww "/>
    ''' <include  path="c:\ww" file="a.vb"/>
    Public FLD As String

    ''' <mysummary cref="SSS"></mysummary>
    Public FLD2 As String

    ''' <param name="x"></param>
    ''' <param name="x"></param>
    Public Sub SSS(x As Integer)
    End Sub

    ''' <remarks x=" A" y="" z = "B"></remarks>
    ''' <remarks  y=""  z = "B" x="A   "/>
    ''' <remarks  y="  "  z = "B" x="a"/>
    ''' <remarks  y="  "   x="A" z = "B"/>
    Public F As Integer

    ''' <returns what="a"></returns>
    ''' <returns  what="b"></returns>
    ''' <returns  what=" b "/>
    Public Shared Operator -(a As C(Of X, Y), b As Integer) As C(Of X, Y)
        Return Nothing
    End Operator

    ''' <permission cref="System.Security.PermissionSet"/>
    ''' <permission cref="System.Security.PermissionSet  "></permission>
    ''' <permission cref="System.Security. PermissionSet"></permission>
    Public Shared Narrowing Operator CType(a As C(Of X, Y)) As Integer
        Return Nothing
    End Operator
End Class

''' <remarks x=" A" y=""></remarks>
''' <remarks  y="" x="A   "/>
Module M
    ''' <remarks></remarks>
    ''' <remarks/>
    ''' <param name="x"></param>
    ''' <param name="x"></param>
    Public Event A(x As Integer, x As Integer)

    ''' <param name="a" noname="b"></param>
    ''' <param  noname="   b   " name="a"></param>
    ''' <value></value>
    ''' <value/>
    Public WriteOnly Property PROP(a As String) As String
        Set(value As String)

        End Set
    End Property
End Module
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'a b' that could not be resolved.
''' <summary cref="a b">
             ~~~~~~~~~~
BC42305: XML comment tag 'typeparam' appears with identical attributes more than once in the same XML comment block.
''' <typeparam name=" X " />
    ~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'typeparam' appears with identical attributes more than once in the same XML comment block.
''' <typeparam name="X"></typeparam>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'a B' that could not be resolved.
''' <summary cref="a B "/>
             ~~~~~~~~~~~
BC42305: XML comment tag 'summary' appears with identical attributes more than once in the same XML comment block.
''' <summary cref="     a b "/>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute '     a b' that could not be resolved.
''' <summary cref="     a b "/>
             ~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'include' appears with identical attributes more than once in the same XML comment block.
    ''' <include  path="c:\ww" file="a.vb"/>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'param' appears with identical attributes more than once in the same XML comment block.
    ''' <param name="x"></param>
        ~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'remarks' appears with identical attributes more than once in the same XML comment block.
    ''' <remarks  y=""  z = "B" x="A   "/>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'remarks' appears with identical attributes more than once in the same XML comment block.
    ''' <remarks  y="  "   x="A" z = "B"/>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'returns' appears with identical attributes more than once in the same XML comment block.
    ''' <returns  what=" b "/>
        ~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'permission' appears with identical attributes more than once in the same XML comment block.
    ''' <permission cref="System.Security.PermissionSet  "></permission>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'remarks' appears with identical attributes more than once in the same XML comment block.
''' <remarks  y="" x="A   "/>
    ~~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'remarks' appears with identical attributes more than once in the same XML comment block.
    ''' <remarks/>
        ~~~~~~~~~~
BC42305: XML comment tag 'param' appears with identical attributes more than once in the same XML comment block.
    ''' <param  noname="   b   " name="a"></param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42305: XML comment tag 'value' appears with identical attributes more than once in the same XML comment block.
    ''' <value/>
        ~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C`2">
 <summary cref="!:a b">
 </summary>
 <typeparam name="X"></typeparam>
 <typeparam name=" X " />
 <typeparam name="Y"></typeparam>
 <typeparam name="X"></typeparam>
 <summary cref="!:a B "/>
 <summary cref="!:     a b "/>
</member>
<member name="F:C`2.FLD">
 <!--warning BC42321: Unable to include XML fragment ' c:\ww ' of file ' a.vb'. File not found.-->
 <!--warning BC42321: Unable to include XML fragment 'c:\ww' of file 'a.vb'. File not found.-->
</member>
<member name="F:C`2.FLD2">
 <mysummary cref="M:C`2.SSS(System.Int32)"></mysummary>
</member>
<member name="M:C`2.SSS(System.Int32)">
 <param name="x"></param>
 <param name="x"></param>
</member>
<member name="F:C`2.F">
 <remarks x=" A" y="" z = "B"></remarks>
 <remarks  y=""  z = "B" x="A   "/>
 <remarks  y="  "  z = "B" x="a"/>
 <remarks  y="  "   x="A" z = "B"/>
</member>
<member name="M:C`2.op_Subtraction(C{`0,`1},System.Int32)">
 <returns what="a"></returns>
 <returns  what="b"></returns>
 <returns  what=" b "/>
</member>
<member name="M:C`2.op_Explicit(C{`0,`1})~System.Int32">
 <permission cref="T:System.Security.PermissionSet"/>
 <permission cref="T:System.Security.PermissionSet"></permission>
 <permission cref="T:System.Security.PermissionSet"></permission>
</member>
<member name="T:M">
 <remarks x=" A" y=""></remarks>
 <remarks  y="" x="A   "/>
</member>
<member name="E:M.A">
 <remarks></remarks>
 <remarks/>
 <param name="x"></param>
 <param name="x"></param>
</member>
<member name="P:M.PROP(System.String)">
 <param name="a" noname="b"></param>
 <param  noname="   b   " name="a"></param>
 <value></value>
 <value/>
</member>
</members>
</doc>
]]>
</xml>, ensureEnglishUICulture:=True)
        End Sub

        <Fact()>
        Public Sub BC42305WRN_XMLDocDuplicateXMLNode_NoError()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

''' <summary cref="a b">
''' </summary>
''' <typeparam name="X"></typeparam>
''' <typeparam name=" X " />
''' <typeparam name="Y"></typeparam>
''' <typeparam name="X"></typeparam>
''' <summary cref="a B "/>
''' <summary cref="     a b "/>
Public Class C(Of X, Y)
    ''' <include file=" a.vb" path=" c:\ww "/>
    ''' <include  path="c:\ww" file="a.vb"/>
    Public FLD As String

    ''' <mysummary cref="SSS"></mysummary>
    Public FLD2 As String

    ''' <param name="x"></param>
    ''' <param name="x"></param>
    Public Sub SSS(x As Integer)
    End Sub

    ''' <remarks x=" A" y="" z = "B"></remarks>
    ''' <remarks  y=""  z = "B" x="A   "/>
    ''' <remarks  y="  "  z = "B" x="a"/>
    ''' <remarks  y="  "   x="A" z = "B"/>
    Public F As Integer

    ''' <returns what="a"></returns>
    ''' <returns  what="b"></returns>
    ''' <returns  what=" b "/>
    Public Shared Operator -(a As C(Of X, Y), b As Integer) As C(Of X, Y)
        Return Nothing
    End Operator

    ''' <permission cref="System.Security.PermissionSet"/>
    ''' <permission cref="System.Security.PermissionSet  "></permission>
    ''' <permission cref="System.Security. PermissionSet"></permission>
    Public Shared Narrowing Operator CType(a As C(Of X, Y)) As Integer
        Return Nothing
    End Operator
End Class

''' <remarks x=" A" y=""></remarks>
''' <remarks  y="" x="A   "/>
Module M
    ''' <remarks></remarks>
    ''' <remarks/>
    ''' <param name="x"></param>
    ''' <param name="x"></param>
    Public Event A(x As Integer, x As Integer)

    ''' <param name="a" noname="b"></param>
    ''' <param  noname="   b   " name="a"></param>
    ''' <value></value>
    ''' <value/>
    Public WriteOnly Property PROP(a As String) As String
        Set(value As String)

        End Set
    End Property
End Module
]]>
    </file>
</compilation>,
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C`2">
 <summary cref="!:a b">
 </summary>
 <typeparam name="X"></typeparam>
 <typeparam name=" X " />
 <typeparam name="Y"></typeparam>
 <typeparam name="X"></typeparam>
 <summary cref="!:a B "/>
 <summary cref="!:     a b "/>
</member>
<member name="F:C`2.FLD">
 <!--warning BC42321: Unable to include XML fragment ' c:\ww ' of file ' a.vb'. File not found.-->
 <!--warning BC42321: Unable to include XML fragment 'c:\ww' of file 'a.vb'. File not found.-->
</member>
<member name="F:C`2.FLD2">
 <mysummary cref="M:C`2.SSS(System.Int32)"></mysummary>
</member>
<member name="M:C`2.SSS(System.Int32)">
 <param name="x"></param>
 <param name="x"></param>
</member>
<member name="F:C`2.F">
 <remarks x=" A" y="" z = "B"></remarks>
 <remarks  y=""  z = "B" x="A   "/>
 <remarks  y="  "  z = "B" x="a"/>
 <remarks  y="  "   x="A" z = "B"/>
</member>
<member name="M:C`2.op_Subtraction(C{`0,`1},System.Int32)">
 <returns what="a"></returns>
 <returns  what="b"></returns>
 <returns  what=" b "/>
</member>
<member name="M:C`2.op_Explicit(C{`0,`1})~System.Int32">
 <permission cref="T:System.Security.PermissionSet"/>
 <permission cref="T:System.Security.PermissionSet"></permission>
 <permission cref="T:System.Security.PermissionSet"></permission>
</member>
<member name="T:M">
 <remarks x=" A" y=""></remarks>
 <remarks  y="" x="A   "/>
</member>
<member name="E:M.A">
 <remarks></remarks>
 <remarks/>
 <param name="x"></param>
 <param name="x"></param>
</member>
<member name="P:M.PROP(System.String)">
 <param name="a" noname="b"></param>
 <param  noname="   b   " name="a"></param>
 <value></value>
 <value/>
</member>
</members>
</doc>
]]>
</xml>, withDiagnostics:=False, ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub ByRefByValOverloading()
            CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Structure TestStruct
    ''' <see cref="S1(ByVal TestStruct)"/>
    ''' <see cref="S1(ByRef TestStruct)"/>
    ''' <see cref="S2(ByVal TestStruct)"/>
    ''' <see cref="S2(ByRef TestStruct)"/>
    Public Shared field As Integer

    Public Sub S1(i As TestStruct)
    End Sub
    Public Sub S2(ByRef i As TestStruct)
    End Sub
End Structure
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'S1(ByRef TestStruct)' that could not be resolved.
    ''' <see cref="S1(ByRef TestStruct)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'S2(ByVal TestStruct)' that could not be resolved.
    ''' <see cref="S2(ByVal TestStruct)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:TestStruct.S1(TestStruct)"/>
 <see cref="!:S1(ByRef TestStruct)"/>
 <see cref="!:S2(ByVal TestStruct)"/>
 <see cref="M:TestStruct.S2(TestStruct@)"/>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <WorkItem(751828, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751828")>
        <Fact()>
        Public Sub GetSymbolInfo_Bug_751828()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System
Imports <xmlns="http://www.w3.org/2005/Atom">

Public Class C
End Class
]]>
    </file>
</compilation>, <error></error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of XmlStringSyntax)(tree, "http://www.w3.org/2005/Atom").ToArray()
            Assert.Equal(1, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            Dim expSymInfo1 = model.GetSymbolInfo(names(0))
            Assert.True(expSymInfo1.IsEmpty)
        End Sub

        <WorkItem(768639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768639")>
        <Fact()>
        Public Sub GetSymbolInfo_Bug_768639a()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Interface I
    Sub Bar()
End Interface
MustInherit Class C
    Public MustOverride Sub Bar()
End Class
Class B : Inherits C : Implements I
    ''' <see cref="Bar"/>
    Public Overrides Sub Bar() Implements I.Bar
    End Sub
End Class
]]>
    </file>
</compilation>, <error></error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "Bar").ToArray()
            Assert.Equal(2, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            Dim expSymInfo1 = model.GetSymbolInfo(names(0))
            Assert.NotNull(expSymInfo1.Symbol)
        End Sub

        <WorkItem(768639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768639")>
        <Fact()>
        Public Sub GetSymbolInfo_Bug_768639b()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
MustInherit Class C
    Public MustOverride Property PPP
End Class
Class B : Inherits C
    ''' <see cref="PPP"/>
    Public Overrides Property PPP As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class
]]>
    </file>
</compilation>, <error></error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "PPP").ToArray()
            Assert.Equal(1, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            Dim expSymInfo1 = model.GetSymbolInfo(names(0))
            Assert.NotNull(expSymInfo1.Symbol)
        End Sub

        <WorkItem(768639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768639")>
        <Fact()>
        Public Sub GetSymbolInfo_Bug_768639c()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Interface I
    Sub Bar()
End Interface
MustInherit Class C
    Public MustOverride Sub Bar()
End Class
Class B : Inherits C : Implements I
    ''' <see cref="Bar()"/>
    Public Overrides Sub Bar() Implements I.Bar
    End Sub
End Class
]]>
    </file>
</compilation>, <error></error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "Bar").ToArray()
            Assert.Equal(2, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            Dim expSymInfo1 = model.GetSymbolInfo(names(0))
            Assert.NotNull(expSymInfo1.Symbol)
        End Sub

        <WorkItem(768639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768639")>
        <Fact()>
        Public Sub GetSymbolInfo_Bug_768639d()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
MustInherit Class C
    Public MustOverride Property PPP
End Class
Class B : Inherits C
    ''' <see cref="PPP()"/>
    Public Overrides Property PPP As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class
]]>
    </file>
</compilation>, <error></error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "PPP").ToArray()
            Assert.Equal(1, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            Dim expSymInfo1 = model.GetSymbolInfo(names(0))
            Assert.NotNull(expSymInfo1.Symbol)
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_PredefinedTypeSyntax_UShort()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

''' <see cref="UShort"/>
''' <see cref="UShort.ToString()S"/>
Public Class C
End Class
]]>
    </file>
</compilation>,
<error></error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of PredefinedTypeSyntax)(tree, "UShort").ToArray()
            Assert.Equal(2, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            TestSymbolAndTypeInfoForType(model, names(0), compilation.GetSpecialType(SpecialType.System_UInt16))
            TestSymbolAndTypeInfoForType(model, names(1), compilation.GetSpecialType(SpecialType.System_UInt16))
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_PredefinedTypeSyntax_String()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

''' <see cref="String"/>
''' <see cref="String.GetHashCode()S"/>
Public Class C
End Class
]]>
    </file>
</compilation>,
<error></error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of PredefinedTypeSyntax)(tree, "String").ToArray()
            Assert.Equal(2, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            TestSymbolAndTypeInfoForType(model, names(0), compilation.GetSpecialType(SpecialType.System_String))
            TestSymbolAndTypeInfoForType(model, names(1), compilation.GetSpecialType(SpecialType.System_String))
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_NameSyntax_Type()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

Public Class C(Of X)
End Class

''' <see cref="Y"/> ' failed in dev11
''' <see cref="S"/> ' failed in dev11
Public Class C(Of X, Y)
    Public FLD As String

    ''' <see cref="X"/>
    ''' <see cref="T"/>
    ''' <see cref="C"/>
    ''' <see cref="C(of x)"/>
    ''' <see cref="C(of x, y)"/>
    ''' <see cref="C(of x, y).s"/>
    Public Shared Sub S(Of T)()
        C(Of X, Y).S(Of Integer)()
        Dim a As C(Of X, Y) = Nothing
        Dim b As C(Of X) = Nothing
    End Sub
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42375: XML comment has a tag with a 'cref' attribute 'Y' that bound to a type parameter.  Use the <typeparamref> tag instead.
''' <see cref="Y"/> ' failed in dev11
         ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X"/>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T"/>
             ~~~~~~~~
]]>
</error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "C").ToArray()
            Assert.Equal(7, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            Dim expSymInfo1 = model.GetSymbolInfo(names(4))
            Assert.NotNull(expSymInfo1.Symbol)

            TestSymbolAndTypeInfoForType(model, names(5), expSymInfo1.Symbol.OriginalDefinition)

            Dim expSymInfo3 = model.GetSymbolInfo(names(6))
            Assert.NotNull(expSymInfo3.Symbol)
            Assert.NotSame(expSymInfo1.Symbol.OriginalDefinition, expSymInfo3.Symbol.OriginalDefinition)

            Dim actSymInfo1 = model.GetSymbolInfo(names(0))
            Assert.Equal(CandidateReason.Ambiguous, actSymInfo1.CandidateReason)
            Assert.Equal(2, actSymInfo1.CandidateSymbols.Length)

            Dim list = actSymInfo1.CandidateSymbols.ToArray()
            Array.Sort(list, Function(x As ISymbol, y As ISymbol) compilation.CompareSourceLocations(x.Locations(0), y.Locations(0)))
            Assert.Same(expSymInfo3.Symbol.OriginalDefinition, list(0).OriginalDefinition)
            Assert.Same(expSymInfo1.Symbol.OriginalDefinition, list(1).OriginalDefinition)

            TestSymbolAndTypeInfoForType(model, names(1), expSymInfo3.Symbol.OriginalDefinition)

            TestSymbolAndTypeInfoForType(model, names(2), expSymInfo1.Symbol.OriginalDefinition)
            TestSymbolAndTypeInfoForType(model, names(3), expSymInfo1.Symbol.OriginalDefinition)

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "X").ToArray()
            Assert.Equal(4, names.Length)

            Dim typeParamSymInfo = model.GetSymbolInfo(names(0))
            Assert.Null(typeParamSymInfo.Symbol)
            Assert.Equal(SymbolKind.TypeParameter, typeParamSymInfo.CandidateSymbols.Single().Kind)
            Assert.Equal(CandidateReason.NotReferencable, typeParamSymInfo.CandidateReason)
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_NameSyntax()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

Class OuterClass
    Public Class C(Of X)
    End Class

    Public Class C(Of X, Y)
        Public F As Integer
    End Class
End Class

Public Class OtherClass
    ''' <see cref="OuterClass.C"/>
    ''' <see cref="OuterClass.C(of x)"/>
    ''' <see cref="OuterClass.C(of x, y)"/>
    ''' <see cref="OuterClass.C(of x, y).f"/>
    ''' <see cref="OuterClass.C(of x, y).X"/>
    Public Shared Sub S(Of T)()
    End Sub
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'OuterClass.C(of x, y).X' that could not be resolved.
    ''' <see cref="OuterClass.C(of x, y).X"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "C").ToArray()
            Assert.Equal(5, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            CheckSymbolInfoAndTypeInfo(model, names(0), "OuterClass.C(Of X)", "OuterClass.C(Of X, Y)")
            CheckSymbolInfoAndTypeInfo(model, names(1), "OuterClass.C(Of x)")
            CheckSymbolInfoAndTypeInfo(model, names(2), "OuterClass.C(Of x, y)")
            CheckSymbolInfoAndTypeInfo(model, names(3), "OuterClass.C(Of x, y)")
            CheckSymbolInfoAndTypeInfo(model, names(4), "OuterClass.C(Of x, y)")
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_LegacyMode_1()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

Public Class OtherClass
    ''' <see cref="New"/>
    ''' <see cref="OtherClass.New"/>
    ''' <see cref="Operator"/>
    ''' <see cref="Operator+"/>
    ''' <see cref="OtherClass.Operator"/>
    ''' <see cref="OtherClass.Operator+"/>
    Public Shared Sub S(Of T)()
    End Sub
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'New' that could not be resolved.
    ''' <see cref="New"/>
             ~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'OtherClass.New' that could not be resolved.
    ''' <see cref="OtherClass.New"/>
             ~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator' that could not be resolved.
    ''' <see cref="Operator"/>
             ~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator+' that could not be resolved.
    ''' <see cref="Operator+"/>
             ~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'OtherClass.Operator' that could not be resolved.
    ''' <see cref="OtherClass.Operator"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'OtherClass.Operator+' that could not be resolved.
    ''' <see cref="OtherClass.Operator+"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "New").ToArray()
            Assert.Equal(2, names.Length)

            CheckSymbolInfoAndTypeInfo(model, names(0))
            CheckSymbolInfoAndTypeInfo(model, names(1))

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "OtherClass").ToArray()
            Assert.Equal(3, names.Length)

            CheckSymbolInfoAndTypeInfo(model, names(0), "OtherClass")
            CheckSymbolInfoAndTypeInfo(model, names(1), "OtherClass")
            CheckSymbolInfoAndTypeInfo(model, names(2), "OtherClass")

            Dim crefOperator = FindNodesOfTypeFromText(Of CrefOperatorReferenceSyntax)(tree, "Operator").ToArray()
            Assert.Equal(4, crefOperator.Length)

            CheckSymbolInfoAndTypeInfo(model, crefOperator(0))
            CheckSymbolInfoAndTypeInfo(model, crefOperator(1))
            CheckSymbolInfoAndTypeInfo(model, crefOperator(2))
            CheckSymbolInfoAndTypeInfo(model, crefOperator(3))
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_LegacyMode_2()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System

Public Class OtherClass
    ''' <see cref="New
    Public Shared Sub S0(Of T)()
    End Sub
    ''' <see cref="OtherClass.New
    Public Shared Sub S1(Of T)()
    End Sub
    ''' <see cref="Operator
    Public Shared Sub S2(Of T)()
    End Sub
    ''' <see cref="Operator+
    Public Shared Sub S3(Of T)()
    End Sub
    ''' <see cref="OtherClass.Operator
    Public Shared Sub S4(Of T)()
    End Sub
    ''' <see cref="OtherClass.Operator+
    Public Shared Sub S5(Of T)()
    End Sub
End Class
]]>
    </file>
</compilation>,
<error>
    <![CDATA[
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <see cref="New
        ~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S0(Of T)()
~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S0(Of T)()
~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    Public Shared Sub S0(Of T)()
~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <see cref="OtherClass.New
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S1(Of T)()
~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S1(Of T)()
~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    Public Shared Sub S1(Of T)()
~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <see cref="Operator
        ~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S2(Of T)()
~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S2(Of T)()
~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    Public Shared Sub S2(Of T)()
~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <see cref="Operator+
        ~~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S3(Of T)()
~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S3(Of T)()
~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    Public Shared Sub S3(Of T)()
~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <see cref="OtherClass.Operator
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S4(Of T)()
~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S4(Of T)()
~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    Public Shared Sub S4(Of T)()
~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <see cref="OtherClass.Operator+
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S5(Of T)()
~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    Public Shared Sub S5(Of T)()
~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    Public Shared Sub S5(Of T)()
~
]]>
</error>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "New").ToArray()
            Assert.Equal(2, names.Length)

            CheckSymbolInfoAndTypeInfo(model, names(0))
            CheckSymbolInfoAndTypeInfo(model, names(1))

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "OtherClass").ToArray()
            Assert.Equal(3, names.Length)

            CheckSymbolInfoAndTypeInfo(model, names(0), "OtherClass")
            CheckSymbolInfoAndTypeInfo(model, names(1), "OtherClass")
            CheckSymbolInfoAndTypeInfo(model, names(2), "OtherClass")

            Dim crefOperator = FindNodesOfTypeFromText(Of CrefOperatorReferenceSyntax)(tree, "Operator").ToArray()
            Assert.Equal(4, crefOperator.Length)

            CheckSymbolInfoAndTypeInfo(model, crefOperator(0))
            CheckSymbolInfoAndTypeInfo(model, crefOperator(1))
            CheckSymbolInfoAndTypeInfo(model, crefOperator(2))
            CheckSymbolInfoAndTypeInfo(model, crefOperator(3))
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_NameSyntax_Method_1()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class C(Of X)
    Public Shared Sub Sub1(a As Integer)
    End Sub
    Public Shared Sub Sub1(a As Integer, b As Integer)
    End Sub
End Class

''' <see cref="C.Sub1"/>
''' <see cref="C.Sub1(Of A)"/>
''' <see cref="C.Sub1(Of A, B)"/>
Public Class OtherClass
End Class
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'C.Sub1(Of A)' that could not be resolved.
''' <see cref="C.Sub1(Of A)"/>
         ~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'C.Sub1(Of A, B)' that could not be resolved.
''' <see cref="C.Sub1(Of A, B)"/>
         ~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "C").ToArray()
            Assert.Equal(3, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            CheckSymbolInfoOnly(model,
                                DirectCast(names(0).Parent, ExpressionSyntax),
                                "Sub C(Of X).Sub1(a As System.Int32)",
                                "Sub C(Of X).Sub1(a As System.Int32, b As System.Int32)")

            CheckSymbolInfoOnly(model, DirectCast(names(1).Parent, ExpressionSyntax))

            CheckSymbolInfoOnly(model, DirectCast(names(2).Parent, ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_NameSyntax_Method_2()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class C(Of X)
    Public Shared Sub Sub1(a As Integer)
    End Sub
    Public Shared Sub Sub1(Of Y)(a As Integer)
    End Sub
End Class

''' <see cref="C.Sub1"/>
''' <see cref="C.Sub1(Of A)"/>
''' <see cref="C.Sub1(Of A, B)"/>
Public Class OtherClass
End Class
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'C.Sub1(Of A, B)' that could not be resolved.
''' <see cref="C.Sub1(Of A, B)"/>
         ~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "C").ToArray()
            Assert.Equal(3, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            CheckSymbolInfoOnly(model,
                                DirectCast(names(0).Parent, ExpressionSyntax),
                                "Sub C(Of X).Sub1(a As System.Int32)")

            CheckSymbolInfoOnly(model,
                                DirectCast(names(1).Parent, ExpressionSyntax),
                                "Sub C(Of X).Sub1(Of A)(a As System.Int32)")

            CheckSymbolInfoOnly(model, DirectCast(names(2).Parent, ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_NameSyntax_Method_3()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class C(Of X)
    Public Shared Sub Sub1(Of Y, Z)(a As Integer)
    End Sub
    Public Shared Sub Sub1(Of Y)(a As Integer)
    End Sub
End Class

''' <see cref="C.Sub1"/>
''' <see cref="C.Sub1(Of A)"/>
''' <see cref="C.Sub1(Of A, B)"/>
Public Class OtherClass
End Class
]]>
    </file>
</compilation>,
<errors></errors>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "C").ToArray()
            Assert.Equal(3, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            CheckSymbolInfoOnly(model,
                                DirectCast(names(0).Parent, ExpressionSyntax),
                                "Sub C(Of X).Sub1(Of Y)(a As System.Int32)",
                                "Sub C(Of X).Sub1(Of Y, Z)(a As System.Int32)")

            CheckSymbolInfoOnly(model,
                                DirectCast(names(1).Parent, ExpressionSyntax),
                                "Sub C(Of X).Sub1(Of A)(a As System.Int32)")

            CheckSymbolInfoOnly(model,
                                DirectCast(names(2).Parent, ExpressionSyntax),
                                "Sub C(Of X).Sub1(Of A, B)(a As System.Int32)")
        End Sub

        <Fact()>
        Public Sub GetSymbolInfo_NameSyntax_Event_Field_Property()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class C(Of X)
    Public ReadOnly Property Prop1(a As Integer) As String
        Get
            Return Nothing
        End Get
    End Property
    Public Property Prop1 As String
    Public Event Ev1 As Action
    Public Dim Fld As String
End Class

''' <see cref="C.Fld"/>
''' <see cref="C.Fld(Of Integer)"/>
''' <see cref="C.Ev1"/>
''' <see cref="C.Ev1(Of X)"/>
''' <see cref="C.Prop1"/>
''' <see cref="C.Prop1(Of A)"/>
Public Class OtherClass
End Class
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'C.Fld(Of Integer)' that could not be resolved.
''' <see cref="C.Fld(Of Integer)"/>
         ~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'C.Ev1(Of X)' that could not be resolved.
''' <see cref="C.Ev1(Of X)"/>
         ~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'C.Prop1(Of A)' that could not be resolved.
''' <see cref="C.Prop1(Of A)"/>
         ~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "C")
            Assert.Equal(6, names.Length)

            Dim model = compilation.GetSemanticModel(tree)

            CheckSymbolInfoOnly(model,
                                DirectCast(names(0).Parent, ExpressionSyntax),
                                "C(Of X).Fld As System.String")

            CheckSymbolInfoOnly(model, DirectCast(names(1).Parent, ExpressionSyntax))

            CheckSymbolInfoOnly(model,
                                DirectCast(names(2).Parent, ExpressionSyntax),
                                "Event C(Of X).Ev1 As System.Action")

            CheckSymbolInfoOnly(model, DirectCast(names(3).Parent, ExpressionSyntax))

            CheckSymbolInfoOnly(model,
                                DirectCast(names(4).Parent, ExpressionSyntax),
                                "Property C(Of X).Prop1 As System.String",
                                "ReadOnly Property C(Of X).Prop1(a As System.Int32) As System.String")

            CheckSymbolInfoOnly(model, DirectCast(names(5).Parent, ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub SemanticInfo_InsideCref()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz(Of T)
    ''' <see cref="X(Of T, T(Of T, X, InnerClazz(Of X)))"/>
    Public Class InnerClazz(Of X)
    End Class
End Class
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'X(Of T, T(Of T, X, InnerClazz(Of X)))' that could not be resolved.
    ''' <see cref="X(Of T, T(Of T, X, InnerClazz(Of X)))"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "InnerClazz")
            Assert.Equal(1, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "Clazz(Of T).InnerClazz(Of X)")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "T")
            Assert.Equal(3, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax), "T")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "X")
            Assert.Equal(3, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "X") ' Did not bind in dev11.
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax), "X") ' Did not bind in dev11.
        End Sub

        <Fact()>
        Public Sub SemanticInfo_InsideParam()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary><param name="a" nested="true">@OuterClass(Of X)</param></summary>
''' <param name="a">@OuterClass(Of X)</param>
Public MustInherit Class OuterClass(Of X)
    ''' <summary><param name="a" nested="true">@F</param></summary>
    ''' <param name="a">@F</param>
    Public F As String

    ''' <summary><param name="a" nested="true">@S(Of T)</param></summary>
    ''' <param name="a">@S(Of T)</param>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub

    ''' <summary><param name="a" nested="true">@FUN(Of T)</param></summary>
    ''' <param name="a">@FUN(Of T)</param>
    Public MustOverride Function FUN(Of T)(a As T) As String

    ''' <summary><param name="a" nested="true">@Operator +</param></summary>
    ''' <param name="a">@Operator +</param>
    Public Shared Operator +(a As OuterClass(Of X), b As Integer) As Integer
        Return Nothing
    End Operator

    ''' <summary><param name="a" nested="true">@Operator CType</param></summary>
    ''' <param name="a">@Operator CType</param>
    Public Shared Narrowing Operator CType(a As Integer) As OuterClass(Of X)
        Return Nothing
    End Operator

    ''' <summary><param name="obj" nested="true">@E</param></summary>
    ''' <param name="obj">@E</param>
    Public Event E As Action(Of Integer)

    ''' <summary><param name="a" nested="true">@E2</param></summary>
    ''' <param name="a">@E2</param>
    Public Event E2(a As Integer)

    ''' <summary><param name="a" nested="true">@P</param></summary>
    ''' <param name="a">@P</param>
    Property P As String

    ''' <summary><param name="a" nested="true">@P(a As String)</param></summary>
    ''' <param name="a">@P(a As String)</param>
    ReadOnly Property P(a As String) As String
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary><param name="a" nested="true">@D(a As Integer)</param></summary>
    ''' <param name="a">@D(a As Integer)</param>
    Public Delegate Function D(a As Integer) As String

    ''' <summary><param name="a" nested="true">@SD(a As Integer)</param></summary>
    ''' <param name="a">@SD(a As Integer)</param>
    Public Delegate Sub SD(a As Integer)

    ''' <summary><param name="a" nested="true">@ENM</param></summary>
    ''' <param name="a">@ENM</param>
    Public Enum ENM
        ''' <summary><param name="a" nested="true">@DefaultValue</param></summary>
        ''' <param name="a">@DefaultValue</param>
        DefaultValue
    End Enum

    ''' <summary><param name="a" nested="true">@INT(Of INTT)</param></summary>
    ''' <param name="a">@INT(Of INTT)</param>
    Public Interface INT(Of INTT)
        ''' <summary><param name="a" nested="true">@INTS(a As Integer)</param></summary>
        ''' <param name="a">@INTS(a As Integer)</param>
        Sub INTS(a As Integer)
    End Interface
End Class

''' <param name="a" nested="true">@M0</param>
''' <summary><param name="a">@M0</param></summary>
Public Module M0
    Public a As Integer
End Module
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42306: XML comment tag 'param' is not permitted on a 'class' language element.
''' <summary><param name="a" nested="true">@OuterClass(Of X)</param></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'class' language element.
''' <param name="a">@OuterClass(Of X)</param>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.
    ''' <summary><param name="a" nested="true">@F</param></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.
    ''' <param name="a">@F</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'a' does not match a parameter on the corresponding 'property' statement.
    ''' <summary><param name="a" nested="true">@P</param></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'a' does not match a parameter on the corresponding 'property' statement.
    ''' <param name="a">@P</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'enum' language element.
    ''' <summary><param name="a" nested="true">@ENM</param></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'enum' language element.
    ''' <param name="a">@ENM</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.
        ''' <summary><param name="a" nested="true">@DefaultValue</param></summary>
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.
        ''' <param name="a">@DefaultValue</param>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'interface' language element.
    ''' <summary><param name="a" nested="true">@INT(Of INTT)</param></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'interface' language element.
    ''' <param name="a">@INT(Of INTT)</param>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'module' language element.
''' <param name="a" nested="true">@M0</param>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'param' is not permitted on a 'module' language element.
''' <summary><param name="a">@M0</param></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "obj")
            Assert.Equal(2, names.Length)
            CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax), "obj As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(1), ExpressionSyntax), "obj As System.Int32")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "a")
            Assert.Equal(32, names.Length)
            CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(1), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(2), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(3), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(4), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(5), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(6), ExpressionSyntax), "a As T")
            CheckSymbolInfoOnly(model, DirectCast(names(7), ExpressionSyntax), "a As T")
            CheckSymbolInfoOnly(model, DirectCast(names(8), ExpressionSyntax), "a As OuterClass(Of X)")
            CheckSymbolInfoOnly(model, DirectCast(names(9), ExpressionSyntax), "a As OuterClass(Of X)")
            CheckSymbolInfoOnly(model, DirectCast(names(10), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(11), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(12), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(13), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(14), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(15), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(16), ExpressionSyntax), "a As System.String")
            CheckSymbolInfoOnly(model, DirectCast(names(17), ExpressionSyntax), "a As System.String")
            CheckSymbolInfoOnly(model, DirectCast(names(18), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(19), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(20), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(21), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(22), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(23), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(24), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(25), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(26), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(27), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(28), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(29), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(30), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(31), ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub SemanticInfo_InsideParamRef()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary><paramref name="a" nested="true">@OuterClass(Of X)</paramref></summary>
''' <paramref name="a">@OuterClass(Of X)</paramref>
Public MustInherit Class OuterClass(Of X)
    ''' <summary><paramref name="a" nested="true">@F</paramref></summary>
    ''' <paramref name="a">@F</paramref>
    Public F As String

    ''' <summary><paramref name="a" nested="true">@S(Of T)</paramref></summary>
    ''' <paramref name="a">@S(Of T)</paramref>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub

    ''' <summary><paramref name="a" nested="true">@FUN(Of T)</paramref></summary>
    ''' <paramref name="a">@FUN(Of T)</paramref>
    Public MustOverride Function FUN(Of T)(a As T) As String

    ''' <summary><paramref name="a" nested="true">@Operator +</paramref></summary>
    ''' <paramref name="a">@Operator +</paramref>
    Public Shared Operator +(a As OuterClass(Of X), b As Integer) As Integer
        Return Nothing
    End Operator

    ''' <summary><paramref name="a" nested="true">@Operator CType</paramref></summary>
    ''' <paramref name="a">@Operator CType</paramref>
    Public Shared Narrowing Operator CType(a As Integer) As OuterClass(Of X)
        Return Nothing
    End Operator

    ''' <summary><paramref name="obj" nested="true">@E</paramref></summary>
    ''' <paramref name="obj">@E</paramref>
    Public Event E As Action(Of Integer)

    ''' <summary><paramref name="a" nested="true">@E2</paramref></summary>
    ''' <paramref name="a">@E2</paramref>
    Public Event E2(a As Integer)

    ''' <summary><paramref name="a" nested="true">@P</paramref></summary>
    ''' <paramref name="a">@P</paramref>
    Property P As String

    ''' <summary><paramref name="a" nested="true">@P(a As String)</paramref></summary>
    ''' <paramref name="a">@P(a As String)</paramref>
    ReadOnly Property P(a As String) As String
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary><paramref name="a" nested="true">@D(a As Integer)</paramref></summary>
    ''' <paramref name="a">@D(a As Integer)</paramref>
    Public Delegate Function D(a As Integer) As String

    ''' <summary><paramref name="a" nested="true">@SD(a As Integer)</paramref></summary>
    ''' <paramref name="a">@SD(a As Integer)</paramref>
    Public Delegate Sub SD(a As Integer)

    ''' <summary><paramref name="a" nested="true">@ENM</paramref></summary>
    ''' <paramref name="a">@ENM</paramref>
    Public Enum ENM
        ''' <summary><paramref name="a" nested="true">@DefaultValue</paramref></summary>
        ''' <paramref name="a">@DefaultValue</paramref>
        DefaultValue
    End Enum

    ''' <summary><paramref name="a" nested="true">@INT(Of INTT)</paramref></summary>
    ''' <paramref name="a">@INT(Of INTT)</paramref>
    Public Interface INT(Of INTT)
        ''' <summary><paramref name="a" nested="true">@INTS(a As Integer)</paramref></summary>
        ''' <paramref name="a">@INTS(a As Integer)</paramref>
        Sub INTS(a As Integer)
    End Interface
End Class

''' <paramref name="a" nested="true">@M0</paramref>
''' <summary><paramref name="a">@M0</paramref></summary>
Public Module M0
    Public a As Integer
End Module
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.
''' <summary><paramref name="a" nested="true">@OuterClass(Of X)</paramref></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.
''' <paramref name="a">@OuterClass(Of X)</paramref>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.
    ''' <summary><paramref name="a" nested="true">@F</paramref></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.
    ''' <paramref name="a">@F</paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'a' does not match a parameter on the corresponding 'property' statement.
    ''' <summary><paramref name="a" nested="true">@P</paramref></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42307: XML comment parameter 'a' does not match a parameter on the corresponding 'property' statement.
    ''' <paramref name="a">@P</paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'enum' language element.
    ''' <summary><paramref name="a" nested="true">@ENM</paramref></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'enum' language element.
    ''' <paramref name="a">@ENM</paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.
        ''' <summary><paramref name="a" nested="true">@DefaultValue</paramref></summary>
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.
        ''' <paramref name="a">@DefaultValue</paramref>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'interface' language element.
    ''' <summary><paramref name="a" nested="true">@INT(Of INTT)</paramref></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'interface' language element.
    ''' <paramref name="a">@INT(Of INTT)</paramref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'module' language element.
''' <paramref name="a" nested="true">@M0</paramref>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'paramref' is not permitted on a 'module' language element.
''' <summary><paramref name="a">@M0</paramref></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "obj")
            Assert.Equal(2, names.Length)
            CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax), "obj As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax), "obj As System.Int32")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "a")
            Assert.Equal(32, names.Length)
            CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(1), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(2), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(3), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(4), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(5), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(6), ExpressionSyntax), "a As T")
            CheckSymbolInfoOnly(model, DirectCast(names(7), ExpressionSyntax), "a As T")
            CheckSymbolInfoOnly(model, DirectCast(names(8), ExpressionSyntax), "a As OuterClass(Of X)")
            CheckSymbolInfoOnly(model, DirectCast(names(9), ExpressionSyntax), "a As OuterClass(Of X)")
            CheckSymbolInfoOnly(model, DirectCast(names(10), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(11), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(12), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(13), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(14), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(15), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(16), ExpressionSyntax), "a As System.String")
            CheckSymbolInfoOnly(model, DirectCast(names(17), ExpressionSyntax), "a As System.String")
            CheckSymbolInfoOnly(model, DirectCast(names(18), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(19), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(20), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(21), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(22), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(23), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(24), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(25), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(26), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(27), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(28), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(29), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(30), ExpressionSyntax))
            CheckSymbolInfoOnly(model, DirectCast(names(31), ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub SemanticInfo_InsideTypeParam()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary><typeparam name="x" nested="true">@OuterClass(Of X)</typeparam></summary>
''' <typeparam name="x">@OuterClass(Of X)</typeparam>
Public MustInherit Class OuterClass(Of X)
    ''' <summary><typeparam name="x" nested="true">@F</typeparam></summary>
    ''' <typeparam name="x">@F</typeparam>
    Public F As String

    ''' <summary><typeparam name="t" nested="true">@S(Of T)</typeparam></summary>
    ''' <typeparam name="t">@S(Of T)</typeparam>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub

    ''' <summary><typeparam name="tt" nested="true">@FUN(Of T)</typeparam></summary>
    ''' <typeparam name="tt">@FUN(Of T)</typeparam>
    Public MustOverride Function FUN(Of TT)(a As Integer) As String

    ''' <summary><typeparam name="x" nested="true">@Operator +</typeparam></summary>
    ''' <typeparam name="x">@Operator +</typeparam>
    Public Shared Operator +(a As OuterClass(Of X), b As Integer) As Integer
        Return Nothing
    End Operator

    ''' <summary><typeparam name="x" nested="true">@Operator CType</typeparam></summary>
    ''' <typeparam name="x">@Operator CType</typeparam>
    Public Shared Narrowing Operator CType(a As Integer) As OuterClass(Of X)
        Return Nothing
    End Operator

    ''' <summary><typeparam name="t" nested="true">@E</typeparam></summary>
    ''' <typeparam name="t">@E</typeparam>
    Public Event E As Action(Of Integer)

    ''' <summary><typeparam name="x" nested="true">@E2</typeparam></summary>
    ''' <typeparam name="x">@E2</typeparam>
    Public Event E2(a As Integer)

    ''' <summary><typeparam name="x" nested="true">@P</typeparam></summary>
    ''' <typeparam name="x">@P</typeparam>
    Property P As String

    ''' <summary><typeparam name="x" nested="true">@P(a As String)</typeparam></summary>
    ''' <typeparam name="x">@P(a As String)</typeparam>
    ReadOnly Property P(a As String) As String
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary><typeparam name="tt" nested="true">@D(a As Integer)</typeparam></summary>
    ''' <typeparam name="tt">@D(a As Integer)</typeparam>
    Public Delegate Function D(Of TT)(a As Integer) As String

    ''' <summary><typeparam name="t" nested="true">@SD(a As Integer)</typeparam></summary>
    ''' <typeparam name="t">@SD(a As Integer)</typeparam>
    Public Delegate Sub SD(Of T)(a As Integer)

    ''' <summary><typeparam name="x" nested="true">@ENM</typeparam></summary>
    ''' <typeparam name="x">@ENM</typeparam>
    Public Enum ENM
        ''' <summary><typeparam name="x" nested="true">@DefaultValue</typeparam></summary>
        ''' <typeparam name="x">@DefaultValue</typeparam>
        DefaultValue
    End Enum

    ''' <summary><typeparam name="tt" nested="true">@INT(Of TT)</typeparam></summary>
    ''' <typeparam name="tt">@INT(Of TT)</typeparam>
    Public Interface INT(Of TT)
        ''' <summary><typeparam name="t" nested="true">@INTS(a As Integer)</typeparam></summary>
        ''' <typeparam name="t">@INTS(a As Integer)</typeparam>
        Sub INTS(Of T)(a As Integer)
    End Interface
End Class

''' <typeparam name="x" nested="true">@M0</typeparam>
''' <summary><typeparam name="x">@M0</typeparam></summary>
Public Module M0
    Public a As Integer
End Module
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42306: XML comment tag 'typeparam' is not permitted on a 'variable' language element.
    ''' <summary><typeparam name="x" nested="true">@F</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'variable' language element.
    ''' <typeparam name="x">@F</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'operator' language element.
    ''' <summary><typeparam name="x" nested="true">@Operator +</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'operator' language element.
    ''' <typeparam name="x">@Operator +</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42317: XML comment type parameter 'x' does not match a type parameter on the corresponding 'operator' statement.
    ''' <summary><typeparam name="x" nested="true">@Operator CType</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42317: XML comment type parameter 'x' does not match a type parameter on the corresponding 'operator' statement.
    ''' <typeparam name="x">@Operator CType</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'event' language element.
    ''' <summary><typeparam name="t" nested="true">@E</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'event' language element.
    ''' <typeparam name="t">@E</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'event' language element.
    ''' <summary><typeparam name="x" nested="true">@E2</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'event' language element.
    ''' <typeparam name="x">@E2</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
    ''' <summary><typeparam name="x" nested="true">@P</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
    ''' <typeparam name="x">@P</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
    ''' <summary><typeparam name="x" nested="true">@P(a As String)</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
    ''' <typeparam name="x">@P(a As String)</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'enum' language element.
    ''' <summary><typeparam name="x" nested="true">@ENM</typeparam></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'enum' language element.
    ''' <typeparam name="x">@ENM</typeparam>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'variable' language element.
        ''' <summary><typeparam name="x" nested="true">@DefaultValue</typeparam></summary>
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'variable' language element.
        ''' <typeparam name="x">@DefaultValue</typeparam>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'module' language element.
''' <typeparam name="x" nested="true">@M0</typeparam>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparam' is not permitted on a 'module' language element.
''' <summary><typeparam name="x">@M0</typeparam></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "t")
            Assert.Equal(8, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(4), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(5), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(6), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(7), ExpressionSyntax), "T")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "tt")
            Assert.Equal(6, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(4), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(5), ExpressionSyntax), "TT")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "x")
            Assert.Equal(20, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(4), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(5), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(6), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(7), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(8), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(9), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(10), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(11), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(12), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(13), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(14), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(15), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(16), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(17), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(18), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(19), ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub SemanticInfo_InsideTypeParamRef()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary><typeparamref name="x" nested="true">@OuterClass(Of X)</typeparamref></summary>
''' <typeparamref name="x">@OuterClass(Of X)</typeparamref>
Public MustInherit Class OuterClass(Of X)
    ''' <summary><typeparamref name="x" nested="true">@F</typeparamref></summary>
    ''' <typeparamref name="x">@F</typeparamref>
    Public F As String

    ''' <summary><typeparamref name="t" nested="true">@S(Of T)</typeparamref></summary>
    ''' <typeparamref name="t">@S(Of T)</typeparamref>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub

    ''' <summary><typeparamref name="tt" nested="true">@FUN(Of T)</typeparamref></summary>
    ''' <typeparamref name="tt">@FUN(Of T)</typeparamref>
    Public MustOverride Function FUN(Of TT)(a As Integer) As String

    ''' <summary><typeparamref name="x" nested="true">@Operator +</typeparamref></summary>
    ''' <typeparamref name="x">@Operator +</typeparamref>
    Public Shared Operator +(a As OuterClass(Of X), b As Integer) As Integer
        Return Nothing
    End Operator

    ''' <summary><typeparamref name="x" nested="true">@Operator CType</typeparamref></summary>
    ''' <typeparamref name="x">@Operator CType</typeparamref>
    Public Shared Narrowing Operator CType(a As Integer) As OuterClass(Of X)
        Return Nothing
    End Operator

    ''' <summary><typeparamref name="t" nested="true">@E</typeparamref></summary>
    ''' <typeparamref name="t">@E</typeparamref>
    Public Event E As Action(Of Integer)

    ''' <summary><typeparamref name="x" nested="true">@E2</typeparamref></summary>
    ''' <typeparamref name="x">@E2</typeparamref>
    Public Event E2(a As Integer)

    ''' <summary><typeparamref name="x" nested="true">@P</typeparamref></summary>
    ''' <typeparamref name="x">@P</typeparamref>
    Property P As String

    ''' <summary><typeparamref name="x" nested="true">@P(a As String)</typeparamref></summary>
    ''' <typeparamref name="x">@P(a As String)</typeparamref>
    ReadOnly Property P(a As String) As String
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary><typeparamref name="tt" nested="true">@D(a As Integer)</typeparamref></summary>
    ''' <typeparamref name="tt">@D(a As Integer)</typeparamref>
    Public Delegate Function D(Of TT)(a As Integer) As String

    ''' <summary><typeparamref name="t" nested="true">@SD(a As Integer)</typeparamref></summary>
    ''' <typeparamref name="t">@SD(a As Integer)</typeparamref>
    Public Delegate Sub SD(Of T)(a As Integer)

    ''' <summary><typeparamref name="x" nested="true">@ENM</typeparamref></summary>
    ''' <typeparamref name="x">@ENM</typeparamref>
    Public Enum ENM
        ''' <summary><typeparamref name="x" nested="true">@DefaultValue</typeparamref></summary>
        ''' <typeparamref name="x">@DefaultValue</typeparamref>
        DefaultValue
    End Enum

    ''' <summary><typeparamref name="tt" nested="true">@INT(Of TT)</typeparamref></summary>
    ''' <typeparamref name="tt">@INT(Of TT)</typeparamref>
    Public Interface INT(Of TT)
        ''' <summary><typeparamref name="t" nested="true">@INTS(a As Integer)</typeparamref></summary>
        ''' <typeparamref name="t">@INTS(a As Integer)</typeparamref>
        Sub INTS(Of T)(a As Integer)
    End Interface
End Class

''' <typeparamref name="x" nested="true">@M0</typeparamref>
''' <summary><typeparamref name="x">@M0</typeparamref></summary>
Public Module M0
    ''' <typeparamref name="x" nested="true">@M0.a</typeparamref>
    ''' <summary><typeparamref name="x">@M0.a</typeparamref></summary>
    ''' <typeparamref>@M0.a -- no-name</typeparamref>
    Public a As Integer
End Module
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42317: XML comment type parameter 't' does not match a type parameter on the corresponding 'event' statement.
    ''' <summary><typeparamref name="t" nested="true">@E</typeparamref></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42317: XML comment type parameter 't' does not match a type parameter on the corresponding 'event' statement.
    ''' <typeparamref name="t">@E</typeparamref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparamref' is not permitted on a 'module' language element.
''' <typeparamref name="x" nested="true">@M0</typeparamref>
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42306: XML comment tag 'typeparamref' is not permitted on a 'module' language element.
''' <summary><typeparamref name="x">@M0</typeparamref></summary>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42317: XML comment type parameter 'x' does not match a type parameter on the corresponding 'variable' statement.
    ''' <typeparamref name="x" nested="true">@M0.a</typeparamref>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42317: XML comment type parameter 'x' does not match a type parameter on the corresponding 'variable' statement.
    ''' <summary><typeparamref name="x">@M0.a</typeparamref></summary>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "t")
            Assert.Equal(8, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(4), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(5), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(6), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(7), ExpressionSyntax), "T")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "tt")
            Assert.Equal(6, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(4), ExpressionSyntax), "TT")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(5), ExpressionSyntax), "TT")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "x")
            Assert.Equal(22, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(4), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(5), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(6), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(7), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(8), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(9), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(10), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(11), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(12), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(13), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(14), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(15), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(16), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(17), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(18), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(19), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(20), ExpressionSyntax))
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(21), ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub SemanticInfo_RightBinderAndSymbol()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <see cref="X">@OuterClass</see> ' Failed in dev11.
''' <see cref="S">@OuterClass</see> ' Failed in dev11.
Public MustInherit Class OuterClass(Of X)
    ''' <see cref="X">@F</see>
    ''' <see cref="S">@F</see>
    ''' <see cref="T">@F</see>
    Public F As String

    ''' <see cref="X">@S</see>
    ''' <see cref="F">@S</see>
    ''' <see cref="a">@S</see>
    ''' <see cref="T">@S</see>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub

    ''' <see cref="X">@FUN</see>
    ''' <see cref="F">@FUN</see>
    ''' <see cref="a">@FUN</see>
    ''' <see cref="T">@FUN</see>
    Public MustOverride Function FUN(Of T)(a As Integer) As String

    ''' <see cref="X">@InnerClass</see>
    ''' <see cref="F">@InnerClass</see>
    ''' <see cref="T">@InnerClass</see>
    ''' <see cref="Y">@InnerClass</see> ' Failed in dev11.
    Public Class InnerClass(Of Y)
    End Class

    ''' <see cref="X">@E</see>
    ''' <see cref="F">@E</see>
    ''' <see cref="T">@E</see>
    ''' <see cref="obj">@E</see>
    Public Event E As Action(Of Integer)

    ''' <see cref="X">@E2</see>
    ''' <see cref="F">@E2</see>
    ''' <see cref="a">@E2</see>
    ''' <see cref="T">@E2</see>
    Public Event E2(a As Integer)

    ''' <see cref="X">@P</see>
    ''' <see cref="F">@P</see>
    ''' <see cref="T">@P</see>
    Property P As String

    ''' <see cref="X">@P(a)</see>
    ''' <see cref="F">@P(a)</see>
    ''' <see cref="a">@P(a)</see>
    ''' <see cref="T">@P(a)</see>
    ReadOnly Property P(a As String) As String
        Get
            Return Nothing
        End Get
    End Property

    ''' <see cref="X">@D</see>
    ''' <see cref="F">@D</see>
    ''' <see cref="a">@D</see>
    ''' <see cref="T">@D</see>
    Public Delegate Function D(a As Integer) As String

    ''' <see cref="X">@SD</see>
    ''' <see cref="F">@SD</see>
    ''' <see cref="a">@SD</see>
    ''' <see cref="T">@SD</see>
    Public Delegate Sub SD(a As Integer)

    ''' <see cref="X">@ENM</see>
    ''' <see cref="F">@ENM</see>
    ''' <see cref="DefaultValue">@ENM</see> ' Failed in dev11.
    Public Enum ENM
        ''' <see cref="F">@DefaultValue</see>
        DefaultValue
    End Enum

    ''' <see cref="X">@INT</see>
    ''' <see cref="F">@INT</see>
    ''' <see cref="INTT">@INT</see> ' Failed in dev11.
    ''' <see cref="INTS">@INT</see> ' Failed in dev11.
    Public Interface INT(Of INTT)
        ''' <see cref="F">@INTS</see>
        Sub INTS(a As Integer)
    End Interface
End Class

''' <see cref="Fun02">@M0</see>
Public Module M0
    ''' <see cref="Fun02">@Fun02</see>
    Public Function Fun02() As Integer
        Return Nothing
    End Function 
End Module 
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
''' <see cref="X">@OuterClass</see> ' Failed in dev11.
         ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@F</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@F</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@S</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'a' that could not be resolved.
    ''' <see cref="a">@S</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@S</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@FUN</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'a' that could not be resolved.
    ''' <see cref="a">@FUN</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@FUN</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@InnerClass</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@InnerClass</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'Y' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="Y">@InnerClass</see> ' Failed in dev11.
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@E</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@E</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'obj' that could not be resolved.
    ''' <see cref="obj">@E</see>
             ~~~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@E2</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'a' that could not be resolved.
    ''' <see cref="a">@E2</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@E2</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@P</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@P</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@P(a)</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'a' that could not be resolved.
    ''' <see cref="a">@P(a)</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@P(a)</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@D</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'a' that could not be resolved.
    ''' <see cref="a">@D</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@D</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@SD</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'a' that could not be resolved.
    ''' <see cref="a">@SD</see>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
    ''' <see cref="T">@SD</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@ENM</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'X' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="X">@INT</see>
             ~~~~~~~~
BC42375: XML comment has a tag with a 'cref' attribute 'INTT' that bound to a type parameter.  Use the <typeparamref> tag instead.
    ''' <see cref="INTT">@INT</see> ' Failed in dev11.
             ~~~~~~~~~~~
]]>
</errors>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "X")
            Assert.Equal(13, names.Length)
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(0), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(1), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(2), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(3), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(4), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(5), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(6), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(7), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(8), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(9), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(10), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(11), "X")
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(12), "X")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "S")
            Assert.Equal(2, names.Length)
            CheckSymbolInfoOnly(model, names(0), "Sub OuterClass(Of X).S(Of T)(a As System.Int32)")
            CheckSymbolInfoOnly(model, names(1), "Sub OuterClass(Of X).S(Of T)(a As System.Int32)")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "T")
            Assert.Equal(10, names.Length)
            CheckSymbolInfoOnly(model, names(0))
            CheckSymbolInfoOnly(model, names(1))
            CheckSymbolInfoOnly(model, names(2))
            CheckSymbolInfoOnly(model, names(3))
            CheckSymbolInfoOnly(model, names(4))
            CheckSymbolInfoOnly(model, names(5))
            CheckSymbolInfoOnly(model, names(6))
            CheckSymbolInfoOnly(model, names(7))
            CheckSymbolInfoOnly(model, names(8))
            CheckSymbolInfoOnly(model, names(9))

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "F")
            Assert.Equal(13, names.Length)
            CheckSymbolInfoOnly(model, names(0), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(1), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(2), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(3), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(4), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(5), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(6), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(7), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(8), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(9), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(10), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(11), "OuterClass(Of X).F As System.String")
            CheckSymbolInfoOnly(model, names(12), "OuterClass(Of X).F As System.String")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "a")
            Assert.Equal(6, names.Length)
            CheckSymbolInfoOnly(model, names(0))
            CheckSymbolInfoOnly(model, names(1))
            CheckSymbolInfoOnly(model, names(2))
            CheckSymbolInfoOnly(model, names(3))
            CheckSymbolInfoOnly(model, names(4))
            CheckSymbolInfoOnly(model, names(5))

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "obj")
            Assert.Equal(1, names.Length)
            CheckSymbolInfoOnly(model, names(0))

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "DefaultValue")
            Assert.Equal(1, names.Length)
            CheckSymbolInfoOnly(model, names(0), "OuterClass(Of X).ENM.DefaultValue") ' Did not bind in dev11.

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "Fun02")
            Assert.Equal(2, names.Length)
            CheckSymbolInfoOnly(model, names(0), "Function M0.Fun02() As System.Int32")
            CheckSymbolInfoOnly(model, names(0), "Function M0.Fun02() As System.Int32")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "INTT")
            Assert.Equal(1, names.Length)
            CheckTypeParameterCrefSymbolInfoAndTypeInfo(model, names(0), "INTT")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "INTS")
            Assert.Equal(1, names.Length)
            CheckSymbolInfoOnly(model, names(0), "Sub OuterClass(Of X).INT(Of INTT).INTS(a As System.Int32)")
        End Sub

        <Fact()>
        Public Sub SemanticInfo_SquareBrackets()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <typeparam name="X"></typeparam>
''' <typeparam name="[X]"></typeparam>
''' <typeparamref name="X"></typeparamref>
''' <typeparamref name="[X]"></typeparamref>
Public MustInherit Class OuterClass(Of X)

    ''' <typeparamref name="X"></typeparamref>
    ''' <typeparamref name="[X]"></typeparamref>
    ''' <typeparam name="t"></typeparam>
    ''' <typeparam name="[t]"></typeparam>
    ''' <typeparamref name="t"></typeparamref>
    ''' <typeparamref name="[t]"></typeparamref>
    ''' <param name="a"></param>
    ''' <param name="[a]"></param>
    ''' <paramref name="A"></paramref>
    ''' <paramref name="[A]"></paramref>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub
End Class 
]]>
    </file>
</compilation>,
<errors></errors>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:OuterClass`1">
 <typeparam name="X"></typeparam>
 <typeparam name="[X]"></typeparam>
 <typeparamref name="X"></typeparamref>
 <typeparamref name="[X]"></typeparamref>
</member>
<member name="M:OuterClass`1.S``1(System.Int32)">
 <typeparamref name="X"></typeparamref>
 <typeparamref name="[X]"></typeparamref>
 <typeparam name="t"></typeparam>
 <typeparam name="[t]"></typeparam>
 <typeparamref name="t"></typeparamref>
 <typeparamref name="[t]"></typeparamref>
 <param name="a"></param>
 <param name="[a]"></param>
 <paramref name="A"></paramref>
 <paramref name="[A]"></paramref>
</member>
</members>
</doc>
]]>
</xml>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "X")
            Assert.Equal(6, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(4), ExpressionSyntax), "X")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(5), ExpressionSyntax), "X")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "t")
            Assert.Equal(4, names.Length)
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(1), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(2), ExpressionSyntax), "T")
            CheckSymbolInfoAndTypeInfo(model, DirectCast(names(3), ExpressionSyntax), "T")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "a")
            Assert.Equal(2, names.Length)
            CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(1), ExpressionSyntax), "a As System.Int32")

            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "A")
            Assert.Equal(2, names.Length)
            CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax), "a As System.Int32")
            CheckSymbolInfoOnly(model, DirectCast(names(1), ExpressionSyntax), "a As System.Int32")
        End Sub

        <Fact()>
        Public Sub SemanticModel_Accessibility()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass
    ''' <see cref="Other.S" />
    ''' <see cref="c.n1.n2.t.C.c" />
    Public Shared Sub Su(Of T)(a As Integer)
    End Sub
End Class

Public Class Other(Of OT)
    Private Shared Sub S(a As Integer)
    End Sub
End Class

Public Class C(Of T)
    Private Class N1
        Private Class N2
            Public Class T
                Public Class C
                    ''' <see cref="t" />
                    Private C As T

                    ''' <typeparamref name="t"/>
                    Public Shared Sub XYZ(Of T)(a As Integer)
                    End Sub
                End Class
            End Class
        End Class
    End Class
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            ' Other.S
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "Other")
            Assert.Equal(1, names.Length)

            Dim symbols = CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax),
                                                     "Other(Of OT)")

            ' BREAK: dev11 includes "Sub Other(Of OT).S(a As System.Int32)"
            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("Other.S""", StringComparison.Ordinal) + 5, container:=DirectCast(symbols(0), NamedTypeSymbol)),
                                    SymbolKind.Method),
                               "Function System.Object.Equals(obj As System.Object) As System.Boolean",
                               "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
                               "Function System.Object.GetHashCode() As System.Int32",
                               "Function System.Object.GetType() As System.Type",
                               "Function System.Object.MemberwiseClone() As System.Object",
                               "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
                               "Function System.Object.ToString() As System.String",
                               "Sub System.Object.Finalize()")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("Other.S""", StringComparison.Ordinal) + 5,
                                                        container:=DirectCast(symbols(0), NamedTypeSymbol),
                                                        name:="S"),
                                    SymbolKind.Method))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("Other.S""", StringComparison.Ordinal) + 5,
                                                        container:=DirectCast(symbols(0), NamedTypeSymbol),
                                                        name:="GetHashCode"),
                                    SymbolKind.Method),
                               "Function System.Object.GetHashCode() As System.Int32")


            ' c.n1.n2.t.C
            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "C")
            Assert.Equal(1, names.Length)

            ' BREAK: works in dev11.
            symbols = CheckSymbolInfoAndTypeInfo(model,
                                                 DirectCast(names(0), ExpressionSyntax),
                                                 "C(Of T).N1.N2.T.C")
            Assert.Equal(1, symbols.Length)


            ' "t"
            names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "t")
            Assert.Equal(3, names.Length)

            ' cref="t"
            symbols = CheckSymbolInfoAndTypeInfo(model,
                                                 DirectCast(names(1), ExpressionSyntax),
                                                 "C(Of T).N1.N2.T")

            Dim firstIndex = text.IndexOf("""t""", StringComparison.Ordinal) + 1
            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(firstIndex,
                                                        name:="T"),
                                    SymbolKind.NamedType, SymbolKind.TypeParameter),
                               "C(Of T).N1.N2.T")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(firstIndex,
                                                        name:="T",
                                                        container:=symbols(0).ContainingType),
                                    SymbolKind.NamedType, SymbolKind.TypeParameter),
                               "C(Of T).N1.N2.T")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(firstIndex,
                                                        name:="T",
                                                        container:=symbols(0).ContainingType.ContainingType),
                                    SymbolKind.NamedType, SymbolKind.TypeParameter))

            ' name="t"
            Dim secondSymbols = CheckSymbolInfoAndTypeInfo(model,
                                                           DirectCast(names(2), ExpressionSyntax),
                                                           "T")

            Dim secondIndex = text.IndexOf("""t""", firstIndex + 5, StringComparison.Ordinal) + 1

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(secondIndex,
                                                        name:="T"),
                                    SymbolKind.NamedType, SymbolKind.TypeParameter),
                               "T")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(secondIndex,
                                                        name:="T",
                                                        container:=symbols(0).ContainingType),
                                    SymbolKind.NamedType, SymbolKind.TypeParameter),
                               "C(Of T).N1.N2.T")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(secondIndex,
                                                        name:="T",
                                                        container:=secondSymbols(0).ContainingType),
                                    SymbolKind.NamedType, SymbolKind.TypeParameter))

        End Sub

        <Fact(Skip:="1104815")>
        Public Sub CrefLookup()
            Dim source =
                <compilation name="AssemblyName">
                    <file name="a.vb">
                        <![CDATA[
''' <summary>
''' See <see cref="C(Of U)" />
''' </summary>
Class C(Of T)
    Sub M()
    End Sub
End Class

Class Outer
    Private Class Inner
    End Class
End Class
]]>
                    </file>
                </compilation>
            Dim comp = CompileCheckDiagnosticsAndXmlDocument(source, <errors/>)
            Dim syntaxTree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(syntaxTree)

            Dim outer = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Outer")
            Dim inner = outer.GetMember(Of NamedTypeSymbol)("Inner")

            Dim position = syntaxTree.ToString().IndexOf("(Of U)", StringComparison.Ordinal)
            Assert.Equal(inner, model.LookupSymbols(position, outer, inner.Name).Single())
        End Sub

        <Fact()>
        Public Sub SemanticInfo_ErrorsInXmlGenerating_NoneInSemanticMode()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <summary>
''' <a>sss
''' </summary>
Public Class TestClass
    Dim x As TestClass
End Class
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
''' <a>sss
    ~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
''' </summary>
    ~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
''' </summary>
    ~
BC42304: XML documentation parse error: XML name expected. XML comment will be ignored.
''' </summary>
    ~
]]>
</errors>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
</members>
</doc>
]]>
</xml>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "TestClass")
            Assert.Equal(1, names.Length)
            Dim symbols = CheckSymbolInfoAndTypeInfo(model, DirectCast(names(0), ExpressionSyntax), "TestClass")

            Assert.Equal(1, symbols.Length)
            Dim type = symbols(0)
            Assert.Equal(SymbolKind.NamedType, type.Kind)

            Dim docComment = type.GetDocumentationCommentXml()
            Assert.False(String.IsNullOrWhiteSpace(docComment))
            Assert.Equal(
            <![CDATA[
<member name="T:TestClass">
 <summary>
 <a>sss
 </summary>
</member>
]]>.Value.Trim().Replace(vbLf, "").Replace(vbCr, ""),
docComment.Trim().Replace(vbLf, "").Replace(vbCr, ""))

        End Sub

        <Fact()>
        Public Sub SemanticInfo_ErrorsInXmlGenerating_NoneInSemanticMode_PartialMethod()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Partial Public Class TestClass
    ''' <summary> Declaration </summary>
    Partial Private Sub PS()
    End Sub
End Class

Partial Public Class TestClass
    ''' <summary> Implementation
    Private Sub PS()
        PS()
    End Sub
End Class
]]>
    </file>
</compilation>,
<errors>
    <![CDATA[
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    ''' <summary> Implementation
        ~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    ''' <summary> Implementation
                                ~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    ''' <summary> Implementation
                                ~
]]>
</errors>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="M:TestClass.PS">
 <summary> Declaration </summary>
</member>
</members>
</doc>
]]>
</xml>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "PS")
            Assert.Equal(1, names.Length)
            Dim symbols = CheckSymbolInfoOnly(model, DirectCast(names(0), ExpressionSyntax), "Sub TestClass.PS()")

            Assert.Equal(1, symbols.Length)
            Dim method = symbols(0)
            Assert.Equal(SymbolKind.Method, method.Kind)

            Dim docComment = method.GetDocumentationCommentXml()
            Assert.False(String.IsNullOrWhiteSpace(docComment))
            Assert.Equal("<member name=""M:TestClass.PS""> <summary> Implementation</member>".Trim(), docComment.Trim().Replace(vbLf, "").Replace(vbCr, ""))
        End Sub

        <Fact()>
        Public Sub Lookup_InsideParam()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass
    ''' <param name="a."/>
    ''' <param name="a"/>
    ''' <param name="[a]"/>
    ''' <typeparam name="[b]"/>
    ''' <see name="b"/>
    ''' <see cref="c"/>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a.""", StringComparison.Ordinal) + 2),
                                    SymbolKind.Parameter),
                               "a As System.Int32")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter),
                               "a As System.Int32")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter),
                               "a As System.Int32")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[b]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""b""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""c""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))
        End Sub

        <Fact()>
        Public Sub Lookup_InsideParamRef()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass
    ''' <paramref name="a."></paramref>
    ''' <paramref name="a"></paramref>
    ''' <paramref name="[a]"></paramref>
    ''' <typeparam name="[b]"/>
    ''' <see name="b"/>
    ''' <see cref="c"/>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a.""", StringComparison.Ordinal) + 2),
                                    SymbolKind.Parameter),
                               "a As System.Int32")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter),
                               "a As System.Int32")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter),
                               "a As System.Int32")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[b]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""b""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""c""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))
        End Sub

        <Fact()>
        Public Sub Lookup_InsideTypeParam()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass(Of X)
    ''' <typeparam name="a."></typeparam>
    ''' <typeparam name="a"></typeparam>
    ''' <typeparam name="[a]"></typeparam>
    ''' <param name="[b]"/>
    ''' <see name="b"/>
    ''' <see cref="c"/>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a.""", StringComparison.Ordinal) + 2),
                                    SymbolKind.TypeParameter),
                               "T", "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "T", "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "T", "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[b]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""b""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""b""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "X")
        End Sub

        <Fact()>
        Public Sub Lookup_InsideTypeParamRef()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass(Of X)
    ''' <typeparamref name="a."/>
    ''' <typeparamref name="a"/>
    ''' <typeparamref name="[a]"/>
    ''' <param name="[b]"/>
    ''' <see name="b"/>
    ''' <see cref="c"/>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a.""", StringComparison.Ordinal) + 2),
                                    SymbolKind.TypeParameter),
                               "T", "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""a""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "T", "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "T", "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[a]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.Parameter))

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[b]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""b""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""b""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter),
                               "X")
        End Sub

        <Fact()>
        Public Sub Lookup_Cref()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <see cref="d"/>
''' <see cref="d."/>
Public Class OuterClass(Of X)
    ''' <param name="[b]"/>
    ''' <see name="b"/>
    ''' <see cref="c"/>
    ''' <see cref="c."/>
    Public Shared Sub S(Of T)(a As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""d.""", StringComparison.Ordinal) + 2),
                                    SymbolKind.TypeParameter, SymbolKind.Parameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""d""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter, SymbolKind.Parameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""[b]""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter, SymbolKind.Parameter),
                               "a As System.Int32", "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""b""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter, SymbolKind.Parameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""c""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter, SymbolKind.Parameter),
                               "X")

            AssertLookupResult(FilterOfSymbolKindOnly(
                                    model.LookupSymbols(text.IndexOf("""c.""", StringComparison.Ordinal) + 1),
                                    SymbolKind.TypeParameter, SymbolKind.Parameter),
                               "X")
        End Sub

        <Fact()>
        Public Sub Lookup_ParameterAndFieldConflict()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass
    Public X As String

    ''' <param name="X" cref="X"></param>
    Public Sub SSS(x As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            ' X
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "X")
            Assert.Equal(2, names.Length)

            CheckSymbolInfoOnly(model,
                                DirectCast(names(0), ExpressionSyntax),
                                "x As System.Int32")
            CheckSymbolInfoOnly(model,
                                DirectCast(names(1), ExpressionSyntax),
                                "OuterClass.X As System.String")

            AssertLookupResult(
                FilterOfSymbolKindOnly(
                    model.LookupSymbols(
                        text.IndexOf("name=""X""", StringComparison.Ordinal) + 6), SymbolKind.Field, SymbolKind.Parameter),
                    "x As System.Int32")

            AssertLookupResult(
                FilterOfSymbolKindOnly(
                    model.LookupSymbols(
                        text.IndexOf("cref=""X""", StringComparison.Ordinal) + 6), SymbolKind.Field, SymbolKind.Parameter),
                    "OuterClass.X As System.String")
        End Sub

        <Fact()>
        Public Sub Lookup_TypeParameterAndFieldConflict()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass
    Public X As String

    ''' <typeparamref name="X" cref="X"/>
    Public Sub SSS(Of X)()
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            ' X
            Dim names = FindNodesOfTypeFromText(Of NameSyntax)(tree, "X")
            Assert.Equal(2, names.Length)

            CheckSymbolInfoAndTypeInfo(model,
                                       DirectCast(names(0), ExpressionSyntax),
                                       "X")
            CheckSymbolInfoOnly(model,
                                DirectCast(names(1), ExpressionSyntax),
                                "OuterClass.X As System.String")

            AssertLookupResult(
                FilterOfSymbolKindOnly(
                    model.LookupSymbols(
                        text.IndexOf("name=""X""", StringComparison.Ordinal) + 6), SymbolKind.Field, SymbolKind.TypeParameter),
                    "X")

            AssertLookupResult(
                FilterOfSymbolKindOnly(
                    model.LookupSymbols(
                        text.IndexOf("cref=""X""", StringComparison.Ordinal) + 6), SymbolKind.Field, SymbolKind.TypeParameter),
                    "OuterClass.X As System.String")
        End Sub

        <Fact()>
        Public Sub Lookup_DoesNotDependOnContext()
            ' This test just proves that lookup result does not depend on 
            ' context and returns, for example, fields in places where only 
            ' type is expected

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class OuterClass(Of W)
    Public X As String

    Public Sub SSS()
        Dim a As OuterClass(Of Integer) = Nothing
    End Sub
End Class
]]>
    </file>
</compilation>, Nothing)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim text = tree.ToString()

            AssertLookupResult(
                FilterOfSymbolKindOnly(
                    model.LookupSymbols(
                        text.IndexOf("Of Integer", StringComparison.Ordinal) + 3), SymbolKind.Field),
                    "OuterClass(Of W).X As System.String")

            Dim symInteger =
                FilterOfSymbolKindOnly(
                    model.LookupSymbols(
                        text.IndexOf("Of Integer", StringComparison.Ordinal) + 3,
                        name:="Int32"), SymbolKind.NamedType)

            AssertLookupResult(symInteger, "System.Int32")

            AssertLookupResult(
                FilterOfSymbolKindOnly(
                    model.LookupSymbols(
                        text.IndexOf("Of Integer", StringComparison.Ordinal) + 3,
                        name:="Parse",
                        container:=DirectCast(symInteger(0), NamedTypeSymbol)), SymbolKind.Method),
                    "Function System.Int32.Parse(s As System.String) As System.Int32",
                    "Function System.Int32.Parse(s As System.String, provider As System.IFormatProvider) As System.Int32",
                    "Function System.Int32.Parse(s As System.String, style As System.Globalization.NumberStyles) As System.Int32",
                    "Function System.Int32.Parse(s As System.String, style As System.Globalization.NumberStyles, provider As System.IFormatProvider) As System.Int32")
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Include_XPathNotFound_WRN_XMLDocInvalidXMLFragment()
            Dim xmlText = <root/>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}' path='//target' />
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <!--warning BC42320: Unable to include XML fragment '//target' of file '**FILE**'.-->
 </summary>
</member>
</members>
</doc>
]]>
</xml>,
            stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <WorkItem(684184, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/684184")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Bug684184()
            Dim xmlText =
<docs>
    <doc for="DataRepeaterLayoutStyles">
        <summary></summary>
    </doc>
</docs>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' <include file='{0}' path='docs2/doc[@for="DataRepeater"]/*' />
Public Class Clazz
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <!--warning BC42320: Unable to include XML fragment 'docs2/doc[@for="DataRepeater"]/*' of file '**FILE**'.-->
</member>
</members>
</doc>
]]>
</xml>,
            stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Include_FileNotFound_WRN_XMLDocBadFormedXML()
            Dim xmlText = <root/>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}5' path='//target' />
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
<error></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <!--warning BC42321: Unable to include XML fragment '//target' of file '**FILE**5'. File not found.-->
 </summary>
</member>
</members>
</doc>
]]>
</xml>,
            stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Include_IOError_WRN_XMLDocBadFormedXML()
            Dim xmlText = <root>
                              <target>Included</target>
                          </root>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}' path='//target' />
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            Using _stream = New FileStream(xmlFile.Path, FileMode.Open, FileAccess.ReadWrite)

                CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error></error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <!--warning BC42321: Unable to include XML fragment '//target' of file '**FILE**'. The process cannot access the file '**FILE**' because it is being used by another process.-->
 </summary>
</member>
</members>
</doc>
]]>
    </xml>,
                stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"),
                ensureEnglishUICulture:=True)
            End Using
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Include_XmlError_WRN_XMLDocBadFormedXML()
            Dim xmlText =
            <![CDATA[
<root>
    <target>Included<target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}' path='//target' />
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error></error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <!--warning BC42320: Unable to include XML fragment '//target' of file '**FILE**'.-->
 </summary>
</member>
</members>
</doc>
]]>
    </xml>,
                stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Include_XDocument_WRN_XMLDocInvalidXMLFragment()
            Dim xmlText =
            <![CDATA[
<root>
    <target>Included</target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}' path='//target/../..' />
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error></error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <!--warning BC42320: Unable to include XML fragment '//target/../..' of file '**FILE**'.-->
 </summary>
</member>
</members>
</doc>
]]>
    </xml>,
                stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Include_Cycle_WRN_XMLDocInvalidXMLFragment()
            Dim xmlText =
<root>
    <target>
        <nested>
            <include file='{0}' path='//target'/>
        </nested>
    </target>
</root>

            Dim xmlFile = Temp.CreateFile(extension:=".xml")
            xmlFile.WriteAllText(String.Format(xmlText.ToString, xmlFile.ToString))

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}' path='//target' />
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error><%= $"BC42320: Unable to include XML fragment '{xmlFile.ToString()}' of file '//target'." %></error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <target>
    <nested>
      <target>
    <nested>
      <!--warning BC42320: Unable to include XML fragment '**FILE**' of file '//target'.-->
    </nested>
  </target>
    </nested>
  </target>
 </summary>
</member>
</members>
</doc>
]]>
    </xml>,
                stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8807")>
        Public Sub Include_XPathError_WRN_XMLDocBadFormedXML()
            Dim xmlText =
            <![CDATA[
<root>
    <target>Included</target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}' path='//target/%^' />
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error></error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <!--warning BC42320: Unable to include XML fragment '//target/%^' of file '**FILE**'.-->
 </summary>
</member>
</members>
</doc>
]]>
    </xml>,
                stringMapper:=Function(o) StringReplace(o, AsXmlCommentText(xmlFile), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_CrefHandling()
            Dim xmlText =
            <![CDATA[
<root>
    <target>
    Included section
    <summary>
      See <see cref="Module0"/>.
      See <see cref="Module0."/>.
      See <see cref="Module0.
                       "/>.
      See <see cref="Module0
                       "/>.
    </summary>
    <remarks></remarks>
    </target>
    <target>
    Included section
    <summary>
      See <see cref="T:A.B.C"/>.
      See <see cref="Module1"/>.
      See <see cref="Module0.'
                       "/>.
      See <see cref="Module0. _
                       "/>.
    </summary>
    <remarks></remarks>
    </target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='{0}' path='//target' />
''' </summary>
Class Module0
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Module0. _' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute 'Module0.' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute 'Module0.' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute 'Module0.'' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute 'Module1' that could not be resolved.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Module0">
 <summary> 
 <target>
    Included section
    <summary>
      See <see cref="T:Module0" />.
      See <see cref="?:Module0." />.
      See <see cref="?:Module0.                        " />.
      See <see cref="T:Module0" />.
    </summary>
    <remarks />
    </target><target>
    Included section
    <summary>
      See <see cref="T:A.B.C" />.
      See <see cref="?:Module1" />.
      See <see cref="?:Module0.'                        " />.
      See <see cref="?:Module0. _                        " />.
    </summary>
    <remarks />
    </target>
 </summary>
</member>
</members>
</doc>
]]>
    </xml>)
        End Sub

        <Fact>
        Public Sub Include_ParamAndParamRefHandling()
            Dim xmlText =
            <![CDATA[
<root>
    <target>
    Included section
    <summary>
      See <param/>.
      See <param name="PARAMA"/>.
      See <param name="PARAMb"/>.
      See <param name="b"/>.
      See <param name="B' comment
                       "/>.
      See <param name="Parama
                       "/>.
    </summary>
    <remarks></remarks>
    </target>
    <target>
    Included section
    <summary>
      See <paramref/>.
      See <paramref name="PARAMA"/>.
      See <paramref name="PARAMb"/>.
      See <paramref name="b"/>.
      See <paramref name="B' comment
                       "/>.
      See <paramref name="Parama
                       "/>.
    </summary>
    <remarks></remarks>
    </target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

Class Module0
    ''' <summary> 
    ''' <include file='{0}' path='//target' />
    ''' </summary>
    Sub S(paramA As String, B As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42307: XML comment parameter 'B' comment' does not match a parameter on the corresponding 'sub' statement.
BC42307: XML comment parameter 'B' comment' does not match a parameter on the corresponding 'sub' statement.
BC42307: XML comment parameter 'PARAMb' does not match a parameter on the corresponding 'sub' statement.
BC42307: XML comment parameter 'PARAMb' does not match a parameter on the corresponding 'sub' statement.
BC42308: XML comment parameter must have a 'name' attribute.
BC42308: XML comment parameter must have a 'name' attribute.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="M:Module0.S(System.String,System.Int32)">
 <summary> 
 <target>
    Included section
    <summary>
      See <!--warning BC42308: XML comment parameter must have a 'name' attribute.--><param />.
      See <param name="PARAMA" />.
      See <!--warning BC42307: XML comment parameter 'PARAMb' does not match a parameter on the corresponding 'sub' statement.--><param name="PARAMb" />.
      See <param name="b" />.
      See <!--warning BC42307: XML comment parameter 'B' comment' does not match a parameter on the corresponding 'sub' statement.--><param name="B' comment                        " />.
      See <param name="Parama                        " />.
    </summary>
    <remarks />
    </target><target>
    Included section
    <summary>
      See <!--warning BC42308: XML comment parameter must have a 'name' attribute.--><paramref />.
      See <paramref name="PARAMA" />.
      See <!--warning BC42307: XML comment parameter 'PARAMb' does not match a parameter on the corresponding 'sub' statement.--><paramref name="PARAMb" />.
      See <paramref name="b" />.
      See <!--warning BC42307: XML comment parameter 'B' comment' does not match a parameter on the corresponding 'sub' statement.--><paramref name="B' comment                        " />.
      See <paramref name="Parama                        " />.
    </summary>
    <remarks />
    </target>
 </summary>
</member>
</members>
</doc>
]]>
    </xml>, ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_TypeParamAndTypeParamRefHandling()
            Dim xmlText =
            <![CDATA[
<root>
    <target>
    Included section
    <summary>
      See <typeparam/>.
      See <typeparam name="X"/>.
      See <typeparam name="Y"/>.
      See <typeparam name="XY"/>.
      See <typeparam name="Y' comment
                       "/>.
      See <typeparam name="Y
                       "/>.
    </summary>
    <remarks></remarks>
    </target>
    <target>
    Included section
    <summary>
      See <typeparamref/>.
      See <typeparamref name="X"/>.
      See <typeparamref name="Y"/>.
      See <typeparamref name="XY"/>.
      See <typeparamref name="Y' comment
                       "/>.
      See <typeparamref name="Y
                       "/>.
    </summary>
    <remarks></remarks>
    </target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

Class OuterClass(Of X)
    ''' <summary> 
    ''' <include file='{0}' path='//target' />
    ''' </summary>
    Class InnerClass(Of Y)
    End Class
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42317: XML comment type parameter 'X' does not match a type parameter on the corresponding 'class' statement.
BC42317: XML comment type parameter 'XY' does not match a type parameter on the corresponding 'class' statement.
BC42317: XML comment type parameter 'XY' does not match a type parameter on the corresponding 'class' statement.
BC42317: XML comment type parameter 'Y' comment' does not match a type parameter on the corresponding 'class' statement.
BC42317: XML comment type parameter 'Y' comment' does not match a type parameter on the corresponding 'class' statement.
BC42318: XML comment type parameter must have a 'name' attribute.
BC42318: XML comment type parameter must have a 'name' attribute.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:OuterClass`1.InnerClass`1">
 <summary> 
 <target>
    Included section
    <summary>
      See <!--warning BC42318: XML comment type parameter must have a 'name' attribute.--><typeparam />.
      See <!--warning BC42317: XML comment type parameter 'X' does not match a type parameter on the corresponding 'class' statement.--><typeparam name="X" />.
      See <typeparam name="Y" />.
      See <!--warning BC42317: XML comment type parameter 'XY' does not match a type parameter on the corresponding 'class' statement.--><typeparam name="XY" />.
      See <!--warning BC42317: XML comment type parameter 'Y' comment' does not match a type parameter on the corresponding 'class' statement.--><typeparam name="Y' comment                        " />.
      See <typeparam name="Y                        " />.
    </summary>
    <remarks />
    </target><target>
    Included section
    <summary>
      See <!--warning BC42318: XML comment type parameter must have a 'name' attribute.--><typeparamref />.
      See <typeparamref name="X" />.
      See <typeparamref name="Y" />.
      See <!--warning BC42317: XML comment type parameter 'XY' does not match a type parameter on the corresponding 'class' statement.--><typeparamref name="XY" />.
      See <!--warning BC42317: XML comment type parameter 'Y' comment' does not match a type parameter on the corresponding 'class' statement.--><typeparamref name="Y' comment                        " />.
      See <typeparamref name="Y                        " />.
    </summary>
    <remarks />
    </target>
 </summary>
</member>
</members>
</doc>
]]>
    </xml>, ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_Exception()
            Dim xmlText =
            <![CDATA[
<root>
    <target>
      <exception cref="Exception"/>
      <exception cref=""/>
      <exception/>
    </target>
    <targeterror>
      <exception cref="Exception"/>
    </targeterror>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <include file='{0}' path='//targeterror' />
Public Module Module0
End Module

''' <include file='{0}' path='//targeterror' />
Public Class Clazz(Of X)
    ''' <include file='{0}' path='//target' />
    Public Sub X1()
    End Sub
    ''' <include file='{0}' path='//target' />
    Public Event E As Action
    ''' <include file='{0}' path='//targeterror' />
    Public F As Integer
    ''' <include file='{0}' path='//target' />
    Public Property P As Integer
    ''' <include file='{0}' path='//targeterror' />
    Public Delegate Function FDelegate(a As Integer) As String
    ''' <include file='{0}' path='//targeterror' />
    Public Enum En : A : End Enum
    ''' <include file='{0}' path='//targeterror' />
    Public Structure STR : End Structure
    ''' <include file='{0}' path='//target' />
    Public ReadOnly Property A(x As String) As String
        Get
            Return x
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42306: XML comment tag 'exception' is not permitted on a 'class' language element.
BC42306: XML comment tag 'exception' is not permitted on a 'delegate' language element.
BC42306: XML comment tag 'exception' is not permitted on a 'enum' language element.
BC42306: XML comment tag 'exception' is not permitted on a 'module' language element.
BC42306: XML comment tag 'exception' is not permitted on a 'structure' language element.
BC42306: XML comment tag 'exception' is not permitted on a 'variable' language element.
BC42309: XML comment has a tag with a 'cref' attribute '' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute '' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute '' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute '' that could not be resolved.
BC42319: XML comment exception must have a 'cref' attribute.
BC42319: XML comment exception must have a 'cref' attribute.
BC42319: XML comment exception must have a 'cref' attribute.
BC42319: XML comment exception must have a 'cref' attribute.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Module0">
 <targeterror>
      <!--warning BC42306: XML comment tag 'exception' is not permitted on a 'module' language element.--><exception cref="Exception" />
    </targeterror>
</member>
<member name="T:Clazz`1">
 <targeterror>
      <!--warning BC42306: XML comment tag 'exception' is not permitted on a 'class' language element.--><exception cref="Exception" />
    </targeterror>
</member>
<member name="M:Clazz`1.X1">
 <target>
      <exception cref="T:System.Exception" />
      <exception cref="?:" />
      <!--warning BC42319: XML comment exception must have a 'cref' attribute.--><exception />
    </target>
</member>
<member name="E:Clazz`1.E">
 <target>
      <exception cref="T:System.Exception" />
      <exception cref="?:" />
      <!--warning BC42319: XML comment exception must have a 'cref' attribute.--><exception />
    </target>
</member>
<member name="F:Clazz`1.F">
 <targeterror>
      <!--warning BC42306: XML comment tag 'exception' is not permitted on a 'variable' language element.--><exception cref="Exception" />
    </targeterror>
</member>
<member name="P:Clazz`1.P">
 <target>
      <exception cref="T:System.Exception" />
      <exception cref="?:" />
      <!--warning BC42319: XML comment exception must have a 'cref' attribute.--><exception />
    </target>
</member>
<member name="T:Clazz`1.FDelegate">
 <targeterror>
      <!--warning BC42306: XML comment tag 'exception' is not permitted on a 'delegate' language element.--><exception cref="Exception" />
    </targeterror>
</member>
<member name="T:Clazz`1.En">
 <targeterror>
      <!--warning BC42306: XML comment tag 'exception' is not permitted on a 'enum' language element.--><exception cref="Exception" />
    </targeterror>
</member>
<member name="T:Clazz`1.STR">
 <targeterror>
      <!--warning BC42306: XML comment tag 'exception' is not permitted on a 'structure' language element.--><exception cref="Exception" />
    </targeterror>
</member>
<member name="P:Clazz`1.A(System.String)">
 <target>
      <exception cref="T:System.Exception" />
      <exception cref="?:" />
      <!--warning BC42319: XML comment exception must have a 'cref' attribute.--><exception />
    </target>
</member>
</members>
</doc>
]]>
    </xml>, stringMapper:=Function(o) StringReplace(o, xmlFile.ToString(), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_Returns()
            Dim xmlText =
            <![CDATA[
<root>
    <target>
      <returns/>
    </target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <include file='{0}' path='//target' />
Public Class TestClass
''' <include file='{0}' path='//target' />
    Public Enum EN : A : End Enum
    ''' <include file='{0}' path='//target' />
    Public Delegate Sub DelSub(a As Integer)
    ''' <include file='{0}' path='//target' />
    Public Delegate Function DelFunc(a As Integer) As Integer
    ''' <include file='{0}' path='//target' />
    Public Shared Sub MSub(p3 As Integer, p4 As String)
    End Sub
    ''' <include file='{0}' path='//target' />
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function
    ''' <include file='{0}' path='//target' />
    Public Field As Integer
    ''' <include file='{0}' path='//target' />
    WithEvents FieldWE As TestClass
    ''' <include file='{0}' path='//target' />
    Public Declare Function DeclareFtn Lib "bar" () As Integer
    ''' <include file='{0}' path='//target' />
    Public Declare Sub DeclareSub Lib "bar" ()
    ''' <include file='{0}' path='//target' />
    Public ReadOnly Property PReadOnly As Integer
        Get
            Return Nothing
        End Get
    End Property
    ''' <include file='{0}' path='//target' />
    Public Property PReadWrite As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)
        End Set
    End Property
    ''' <include file='{0}' path='//target' />
    Public WriteOnly Property PWriteOnly As Integer
        Set(value As Integer)
        End Set
    End Property
    ''' <include file='{0}' path='//target' />
    Public Event EE(p11 As String)
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42306: XML comment tag 'returns' is not permitted on a 'class' language element.
BC42306: XML comment tag 'returns' is not permitted on a 'delegate sub' language element.
BC42306: XML comment tag 'returns' is not permitted on a 'enum' language element.
BC42306: XML comment tag 'returns' is not permitted on a 'event' language element.
BC42306: XML comment tag 'returns' is not permitted on a 'sub' language element.
BC42306: XML comment tag 'returns' is not permitted on a 'variable' language element.
BC42306: XML comment tag 'returns' is not permitted on a 'WithEvents variable' language element.
BC42313: XML comment tag 'returns' is not permitted on a 'WriteOnly' Property.
BC42315: XML comment tag 'returns' is not permitted on a 'declare sub' language element.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <target>
      <!--warning BC42306: XML comment tag 'returns' is not permitted on a 'class' language element.--><returns />
    </target>
</member>
<member name="T:TestClass.EN">
 <target>
      <!--warning BC42306: XML comment tag 'returns' is not permitted on a 'enum' language element.--><returns />
    </target>
</member>
<member name="T:TestClass.DelSub">
 <target>
      <!--warning BC42306: XML comment tag 'returns' is not permitted on a 'delegate sub' language element.--><returns />
    </target>
</member>
<member name="T:TestClass.DelFunc">
 <target>
      <returns />
    </target>
</member>
<member name="M:TestClass.MSub(System.Int32,System.String)">
 <target>
      <!--warning BC42306: XML comment tag 'returns' is not permitted on a 'sub' language element.--><returns />
    </target>
</member>
<member name="M:TestClass.MFunc(System.Int32,System.String)">
 <target>
      <returns />
    </target>
</member>
<member name="F:TestClass.Field">
 <target>
      <!--warning BC42306: XML comment tag 'returns' is not permitted on a 'variable' language element.--><returns />
    </target>
</member>
<member name="F:TestClass._FieldWE">
 <target>
      <!--warning BC42306: XML comment tag 'returns' is not permitted on a 'WithEvents variable' language element.--><returns />
    </target>
</member>
<member name="M:TestClass.DeclareFtn">
 <target>
      <returns />
    </target>
</member>
<member name="M:TestClass.DeclareSub">
 <target>
      <!--warning BC42315: XML comment tag 'returns' is not permitted on a 'declare sub' language element.--><returns />
    </target>
</member>
<member name="P:TestClass.PReadOnly">
 <target>
      <returns />
    </target>
</member>
<member name="P:TestClass.PReadWrite">
 <target>
      <returns />
    </target>
</member>
<member name="P:TestClass.PWriteOnly">
 <target>
      <!--warning BC42313: XML comment tag 'returns' is not permitted on a 'WriteOnly' Property.--><returns />
    </target>
</member>
<member name="E:TestClass.EE">
 <target>
      <!--warning BC42306: XML comment tag 'returns' is not permitted on a 'event' language element.--><returns />
    </target>
</member>
</members>
</doc>
]]>
    </xml>, stringMapper:=Function(o) StringReplace(o, xmlFile.ToString(), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_Value()
            Dim xmlText =
            <![CDATA[
<root>
    <target>
      <value/>
    </target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <include file='{0}' path='//target' />
Public Class TestClass
    ''' <include file='{0}' path='//target' />
    Public Enum EN : A : End Enum
    ''' <include file='{0}' path='//target' />
    Public Structure STR : End Structure
    ''' <include file='{0}' path='//target' />
    Public Interface INTERF : End Interface
    ''' <include file='{0}' path='//target' />
    Public Delegate Sub DelSub(a As Integer)
    ''' <include file='{0}' path='//target' />
    Public Delegate Function DelFunc(a As Integer) As Integer
    ''' <include file='{0}' path='//target' />
    Public Shared Sub MSub(p3 As Integer, p4 As String)
    End Sub
    ''' <include file='{0}' path='//target' />
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function
    ''' <include file='{0}' path='//target' />
    Public Declare Function DeclareFtn Lib "bar" (p3 As Integer) As Integer
    ''' <include file='{0}' path='//target' />
    Public Field As Integer
    ''' <include file='{0}' path='//target' />
    Public WriteOnly Property PWriteOnly(p As Integer) As Integer
        Set(value As Integer)
        End Set
    End Property
    ''' <include file='{0}' path='//target' />
    Public Property PReadWrite As Integer
    ''' <include file='{0}' path='//target' />
    Public Event EVE(ppp As Integer)
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42306: XML comment tag 'value' is not permitted on a 'class' language element.
BC42306: XML comment tag 'value' is not permitted on a 'declare' language element.
BC42306: XML comment tag 'value' is not permitted on a 'delegate' language element.
BC42306: XML comment tag 'value' is not permitted on a 'delegate sub' language element.
BC42306: XML comment tag 'value' is not permitted on a 'enum' language element.
BC42306: XML comment tag 'value' is not permitted on a 'event' language element.
BC42306: XML comment tag 'value' is not permitted on a 'function' language element.
BC42306: XML comment tag 'value' is not permitted on a 'interface' language element.
BC42306: XML comment tag 'value' is not permitted on a 'structure' language element.
BC42306: XML comment tag 'value' is not permitted on a 'sub' language element.
BC42306: XML comment tag 'value' is not permitted on a 'variable' language element.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'class' language element.--><value />
    </target>
</member>
<member name="T:TestClass.EN">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'enum' language element.--><value />
    </target>
</member>
<member name="T:TestClass.STR">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'structure' language element.--><value />
    </target>
</member>
<member name="T:TestClass.INTERF">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'interface' language element.--><value />
    </target>
</member>
<member name="T:TestClass.DelSub">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'delegate sub' language element.--><value />
    </target>
</member>
<member name="T:TestClass.DelFunc">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'delegate' language element.--><value />
    </target>
</member>
<member name="M:TestClass.MSub(System.Int32,System.String)">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'sub' language element.--><value />
    </target>
</member>
<member name="M:TestClass.MFunc(System.Int32,System.String)">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'function' language element.--><value />
    </target>
</member>
<member name="M:TestClass.DeclareFtn(System.Int32)">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'declare' language element.--><value />
    </target>
</member>
<member name="F:TestClass.Field">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'variable' language element.--><value />
    </target>
</member>
<member name="P:TestClass.PWriteOnly(System.Int32)">
 <target>
      <value />
    </target>
</member>
<member name="P:TestClass.PReadWrite">
 <target>
      <value />
    </target>
</member>
<member name="E:TestClass.EVE">
 <target>
      <!--warning BC42306: XML comment tag 'value' is not permitted on a 'event' language element.--><value />
    </target>
</member>
</members>
</doc>
]]>
    </xml>, stringMapper:=Function(o) StringReplace(o, xmlFile.ToString(), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_ParamAndParamRef()
            Dim xmlText =
            <![CDATA[
<root>
<target>
<param name="P9"/>
<paramref name="P9"/>
</target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <include file='{0}' path='//target' />
Public Class TestClass
    ''' <include file='{0}' path='//target' />
    Public Shared Sub M(P9 As Integer, p4 As String)
        Dim a As TestClass = Nothing
    End Sub
    ''' <include file='{0}' path='//target' />
    Public F As Integer
    ''' <include file='{0}' path='//target' />
    Public Property P As Integer
    ''' <include file='{0}' path='//target' />
    Public ReadOnly Property P(P9 As String) As Integer
        Get
            Return Nothing
        End Get
    End Property
    ''' <include file='{0}' path='//target' />
    Public Event EE(P9 As String)
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42306: XML comment tag 'param' is not permitted on a 'class' language element.
BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.
BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.
BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.
BC42307: XML comment parameter 'P9' does not match a parameter on the corresponding 'property' statement.
BC42307: XML comment parameter 'P9' does not match a parameter on the corresponding 'property' statement.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:TestClass">
 <target>
<!--warning BC42306: XML comment tag 'param' is not permitted on a 'class' language element.--><param name="P9" />
<!--warning BC42306: XML comment tag 'paramref' is not permitted on a 'class' language element.--><paramref name="P9" />
</target>
</member>
<member name="M:TestClass.M(System.Int32,System.String)">
 <target>
<param name="P9" />
<paramref name="P9" />
</target>
</member>
<member name="F:TestClass.F">
 <target>
<!--warning BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.--><param name="P9" />
<!--warning BC42306: XML comment tag 'paramref' is not permitted on a 'variable' language element.--><paramref name="P9" />
</target>
</member>
<member name="P:TestClass.P">
 <target>
<!--warning BC42307: XML comment parameter 'P9' does not match a parameter on the corresponding 'property' statement.--><param name="P9" />
<!--warning BC42307: XML comment parameter 'P9' does not match a parameter on the corresponding 'property' statement.--><paramref name="P9" />
</target>
</member>
<member name="P:TestClass.P(System.String)">
 <target>
<param name="P9" />
<paramref name="P9" />
</target>
</member>
<member name="E:TestClass.EE">
 <target>
<param name="P9" />
<paramref name="P9" />
</target>
</member>
</members>
</doc>
]]>
    </xml>, stringMapper:=Function(o) StringReplace(o, xmlFile.ToString(), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_TypeParamAndTypeParamRef()
            Dim xmlText =
            <![CDATA[
<root>
<target>
<typeparam name="P9"/>
<typeparamref name="P9"/>
</target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <include file='{0}' path='//target' />
Public Module Module0
    ''' <include file='{0}' path='//target' />
    Public Declare Function DeclareFtn Lib "bar" (p3 As Integer) As Integer
End Module

''' <include file='{0}' path='//target' />
Public Class TestClass(Of P9)
    ''' <include file='{0}' path='//target' />
    Public Enum EN : A : End Enum

    ''' <include file='{0}' path='//target' />
    Public Structure STR(Of X) : End Structure

    ''' <include file='{0}' path='//target' />
    Public Interface INTERF(Of X, Y) : End Interface

    ''' <include file='{0}' path='//target' />
    Public Delegate Sub DelSub(Of W)(a As Integer)

    ''' <include file='{0}' path='//target' />
    Public Delegate Function DelFunc(Of W)(a As Integer) As Integer

    ''' <include file='{0}' path='//target' />
    Public Shared Sub MSub(Of TT)(p3 As Integer, p4 As String)
    End Sub

    ''' <include file='{0}' path='//target' />
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function

    ''' <include file='{0}' path='//target' />
    Public Field As Integer

    ''' <include file='{0}' path='//target' />
    Public WriteOnly Property PWriteOnly(p As Integer) As Integer
        Set(value As Integer)
        End Set
    End Property

    ''' <include file='{0}' path='//target' />
    Public Property PReadWrite As Integer

    ''' <include file='{0}' path='//target' />
    Public Event EVE(ppp As Integer)
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42306: XML comment tag 'typeparam' is not permitted on a 'declare' language element.
BC42306: XML comment tag 'typeparam' is not permitted on a 'enum' language element.
BC42306: XML comment tag 'typeparam' is not permitted on a 'event' language element.
BC42306: XML comment tag 'typeparam' is not permitted on a 'module' language element.
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.
BC42306: XML comment tag 'typeparam' is not permitted on a 'variable' language element.
BC42306: XML comment tag 'typeparamref' is not permitted on a 'module' language element.
BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'declare' statement.
BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'delegate' statement.
BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'delegate sub' statement.
BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'function' statement.
BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'interface' statement.
BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'structure' statement.
BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'sub' statement.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Module0">
 <target>
<!--warning BC42306: XML comment tag 'typeparam' is not permitted on a 'module' language element.--><typeparam name="P9" />
<!--warning BC42306: XML comment tag 'typeparamref' is not permitted on a 'module' language element.--><typeparamref name="P9" />
</target>
</member>
<member name="M:Module0.DeclareFtn(System.Int32)">
 <target>
<!--warning BC42306: XML comment tag 'typeparam' is not permitted on a 'declare' language element.--><typeparam name="P9" />
<!--warning BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'declare' statement.--><typeparamref name="P9" />
</target>
</member>
<member name="T:TestClass`1">
 <target>
<typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="T:TestClass`1.EN">
 <target>
<!--warning BC42306: XML comment tag 'typeparam' is not permitted on a 'enum' language element.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="T:TestClass`1.STR`1">
 <target>
<!--warning BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'structure' statement.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="T:TestClass`1.INTERF`2">
 <target>
<!--warning BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'interface' statement.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="T:TestClass`1.DelSub`1">
 <target>
<!--warning BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'delegate sub' statement.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="T:TestClass`1.DelFunc`1">
 <target>
<!--warning BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'delegate' statement.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="M:TestClass`1.MSub``1(System.Int32,System.String)">
 <target>
<!--warning BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'sub' statement.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="M:TestClass`1.MFunc(System.Int32,System.String)">
 <target>
<!--warning BC42317: XML comment type parameter 'P9' does not match a type parameter on the corresponding 'function' statement.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="F:TestClass`1.Field">
 <target>
<!--warning BC42306: XML comment tag 'typeparam' is not permitted on a 'variable' language element.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="P:TestClass`1.PWriteOnly(System.Int32)">
 <target>
<!--warning BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="P:TestClass`1.PReadWrite">
 <target>
<!--warning BC42306: XML comment tag 'typeparam' is not permitted on a 'property' language element.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
<member name="E:TestClass`1.EVE">
 <target>
<!--warning BC42306: XML comment tag 'typeparam' is not permitted on a 'event' language element.--><typeparam name="P9" />
<typeparamref name="P9" />
</target>
</member>
</members>
</doc>
]]>
    </xml>, stringMapper:=Function(o) StringReplace(o, xmlFile.ToString(), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub Include_TypeParameterCref()
            Dim xmlText =
            <![CDATA[
<root>
<target>
<see cref="P9"/>
</target>
</root>
]]>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <include file='{0}' path='//target' />
Public Module Module0
    ''' <include file='{0}' path='//target' />
    Public Declare Function DeclareFtn Lib "bar" (p3 As Integer) As Integer
End Module

''' <include file='{0}' path='//target' />
Public Class TestClass(Of P9)
    ''' <include file='{0}' path='//target' />
    Public Enum EN : A : End Enum

    ''' <include file='{0}' path='//target' />
    Public Structure STR(Of X) : End Structure

    ''' <include file='{0}' path='//target' />
    Public Interface INTERF(Of X, Y) : End Interface

    ''' <include file='{0}' path='//target' />
    Public Delegate Sub DelSub(Of W)(a As Integer)

    ''' <include file='{0}' path='//target' />
    Public Delegate Function DelFunc(Of W)(a As Integer) As Integer

    ''' <include file='{0}' path='//target' />
    Public Shared Sub MSub(Of TT)(p3 As Integer, p4 As String)
    End Sub

    ''' <include file='{0}' path='//target' />
    Public Shared Function MFunc(p3 As Integer, p4 As String) As Integer
        Return Nothing
    End Function

    ''' <include file='{0}' path='//target' />
    Public Field As Integer

    ''' <include file='{0}' path='//target' />
    Public WriteOnly Property PWriteOnly(p As Integer) As Integer
        Set(value As Integer)
        End Set
    End Property

    ''' <include file='{0}' path='//target' />
    Public Property PReadWrite As Integer

    ''' <include file='{0}' path='//target' />
    Public Event EVE(ppp As Integer)
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'P9' that could not be resolved.
BC42309: XML comment has a tag with a 'cref' attribute 'P9' that could not be resolved.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
BC42375: XML comment has a tag with a 'cref' attribute 'P9' that bound to a type parameter.  Use the <typeparamref> tag instead.
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Module0">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="M:Module0.DeclareFtn(System.Int32)">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="T:TestClass`1">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="T:TestClass`1.EN">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="T:TestClass`1.STR`1">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="T:TestClass`1.INTERF`2">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="T:TestClass`1.DelSub`1">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="T:TestClass`1.DelFunc`1">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="M:TestClass`1.MSub``1(System.Int32,System.String)">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="M:TestClass`1.MFunc(System.Int32,System.String)">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="F:TestClass`1.Field">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="P:TestClass`1.PWriteOnly(System.Int32)">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="P:TestClass`1.PReadWrite">
 <target>
<see cref="?:P9" />
</target>
</member>
<member name="E:TestClass`1.EVE">
 <target>
<see cref="?:P9" />
</target>
</member>
</members>
</doc>
]]>
    </xml>, stringMapper:=Function(o) StringReplace(o, xmlFile.ToString(), "**FILE**"))
        End Sub

        <Fact>
        Private Sub ExtendedCref_IdentifierInName_IdentifierAndGenericInSignature()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

Public Structure TestStruct
    ''' <see cref="T(List(Of Int32), TestStruct)"/>
    Public Shared field As Integer

    Sub T(p As List(Of Int32), i as TestStruct)
    End Sub
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:TestStruct.T(System.Collections.Generic.List{System.Int32},TestStruct)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(1, crefNodes.Count)

            Dim info = model.GetSymbolInfo(crefNodes(0))
            Assert.NotNull(info.Symbol)
            Assert.Equal("Sub TestStruct.T(p As System.Collections.Generic.List(Of System.Int32), i As TestStruct)", info.Symbol.ToTestDisplayString())

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("T", {"Sub TestStruct.T(p As System.Collections.Generic.List(Of System.Int32), i As TestStruct)"}, {}),
                          New NameSyntaxInfo("List(Of Int32)", {"System.Collections.Generic.List(Of System.Int32)"}, {"System.Collections.Generic.List(Of System.Int32)"}),
                          New NameSyntaxInfo("Int32", {"System.Int32"}, {"System.Int32"}),
                          New NameSyntaxInfo("TestStruct", {"TestStruct"}, {"TestStruct"}))
        End Sub

        <WorkItem(703587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703587")>
        <Fact()>
        Private Sub ObjectMemberViaInterfaceA()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' Comment
Public Class C
    Implements IEquatable(Of C)

    ''' Implements <see cref="IEquatable(Of T).Equals"/>.
    ''' Implements <see cref="IEquatable(Of T).GetHashCode"/>.
    ''' Implements <see cref="IEquatable(Of T).Equals(T)"/>.
    ''' Implements <see cref="IEquatable(Of T).GetHashCode()"/>.
    Public Function IEquals(c As C) As Boolean Implements IEquatable(Of C).Equals
        Return False
    End Function
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 Comment
</member>
<member name="M:C.IEquals(C)">
 Implements <see cref="M:System.IEquatable`1.Equals(`0)"/>.
 Implements <see cref="!:IEquatable(Of T).GetHashCode"/>.
 Implements <see cref="M:System.IEquatable`1.Equals(`0)"/>.
 Implements <see cref="!:IEquatable(Of T).GetHashCode()"/>.
</member>
</members>
</doc>  
]]>
</xml>

            CompileCheckDiagnosticsAndXmlDocument(xmlSource,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'IEquatable(Of T).GetHashCode' that could not be resolved.
    ''' Implements <see cref="IEquatable(Of T).GetHashCode"/>.
                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'IEquatable(Of T).GetHashCode()' that could not be resolved.
    ''' Implements <see cref="IEquatable(Of T).GetHashCode()"/>.
                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>,
xmlDoc)
        End Sub

        <WorkItem(703587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703587")>
        <Fact()>
        Private Sub ObjectMemberViaInterfaceB()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

''' Comment
Public Interface INT
    Inherits IEquatable(Of Integer)

    ''' Implements <see cref="Equals"/>.
    ''' Implements <see cref="IEquals(Integer)"/>.
    ''' Implements <see cref="Equals(Object)"/>.
    ''' Implements <see cref="Equals(Integer)"/>.
    ''' Implements <see cref="GetHashCode"/>.
    ''' Implements <see cref="GetHashCode()"/>.
    Function IEquals(c As Integer) As Boolean
End Interface
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:INT">
 Comment
</member>
<member name="M:INT.IEquals(System.Int32)">
 Implements <see cref="M:System.IEquatable`1.Equals(`0)"/>.
 Implements <see cref="M:INT.IEquals(System.Int32)"/>.
 Implements <see cref="!:Equals(Object)"/>.
 Implements <see cref="M:System.IEquatable`1.Equals(`0)"/>.
 Implements <see cref="!:GetHashCode"/>.
 Implements <see cref="!:GetHashCode()"/>.
</member>
</members>
</doc>  
]]>
</xml>

            CompileCheckDiagnosticsAndXmlDocument(xmlSource,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Equals(Object)' that could not be resolved.
    ''' Implements <see cref="Equals(Object)"/>.
                        ~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'GetHashCode' that could not be resolved.
    ''' Implements <see cref="GetHashCode"/>.
                        ~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'GetHashCode()' that could not be resolved.
    ''' Implements <see cref="GetHashCode()"/>.
                        ~~~~~~~~~~~~~~~~~~~~
]]>
</errors>,
xmlDoc)
        End Sub

        <Fact>
        Private Sub ExtendedCref_GenericInName_IdentifierInSignature()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Structure TestStruct
    ''' <see cref="T(Of T)(T, TestStruct)"/>
    Public Shared field As Integer

    Sub T(Of T)(p As T, i as TestStruct)
    End Sub
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:TestStruct.T``1(``0,TestStruct)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(1, crefNodes.Count)

            Dim info = model.GetSymbolInfo(crefNodes(0))
            Assert.NotNull(info.Symbol)
            Assert.Equal("Sub TestStruct.T(Of T)(p As T, i As TestStruct)", info.Symbol.ToTestDisplayString())

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("T(Of T)", {"Sub TestStruct.T(Of T)(p As T, i As TestStruct)"}, {}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("TestStruct", {"TestStruct"}, {"TestStruct"}))
        End Sub

        <Fact>
        Private Sub ExtendedCref_GenericInName_GlobalInSignature()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Structure TestStruct
    ''' <see cref="T(Of T)(T, Global.TestStruct)"/>
    Public Shared field As Integer

    Sub T(Of T)(p As T, i as TestStruct)
    End Sub
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:TestStruct.T``1(``0,TestStruct)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(1, crefNodes.Count)

            Dim info = model.GetSymbolInfo(crefNodes(0))
            Assert.NotNull(info.Symbol)
            Assert.Equal("Sub TestStruct.T(Of T)(p As T, i As TestStruct)", info.Symbol.ToTestDisplayString())

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("T(Of T)", {"Sub TestStruct.T(Of T)(p As T, i As TestStruct)"}, {}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("Global.TestStruct", {"TestStruct"}, {"TestStruct"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("TestStruct", {"TestStruct"}, {"TestStruct"}))
        End Sub

        <Fact>
        Private Sub ExtendedCref_GlobalAndQualifiedInName_TypeParamInSignature()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Structure TestStruct
    ''' <see cref="Global.TestStruct.T(Of T)(T, TestStruct)"/>
    Public Shared field As Integer

    Sub T(Of T)(p As T, i as TestStruct)
    End Sub
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:TestStruct.T``1(``0,TestStruct)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(1, crefNodes.Count)

            Dim info = model.GetSymbolInfo(crefNodes(0))
            Assert.NotNull(info.Symbol)
            Assert.Equal("Sub TestStruct.T(Of T)(p As T, i As TestStruct)", info.Symbol.ToTestDisplayString())

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("Global.TestStruct.T(Of T)", {"Sub TestStruct.T(Of T)(p As T, i As TestStruct)"}, {}),
                          New NameSyntaxInfo("Global.TestStruct", {"TestStruct"}, {"TestStruct"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("TestStruct", {"TestStruct"}, {"TestStruct"}),
                          New NameSyntaxInfo("T(Of T)", {"Sub TestStruct.T(Of T)(p As T, i As TestStruct)"}, {}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("TestStruct", {"TestStruct"}, {"TestStruct"}))
        End Sub

        <Fact>
        Private Sub ExtendedCref_QualifiedOperatorReference()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Structure TestStruct(Of X)
    ''' <see cref="Global.TestStruct(Of KKK).  operator+(integer, TestStruct(Of kkk))"/>
    Public Shared field As Integer

    Public Shared Operator +(a As Integer, b As TestStruct(Of X)) As String
        Return Nothing
    End Operator
End Structure]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct`1.field">
 <see cref="M:TestStruct`1.op_Addition(System.Int32,TestStruct{`0})"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(1, crefNodes.Count)

            Dim info = model.GetSymbolInfo(crefNodes(0))
            Assert.NotNull(info.Symbol)
            Assert.Equal("Function TestStruct(Of KKK).op_Addition(a As System.Int32, b As TestStruct(Of KKK)) As System.String", info.Symbol.ToTestDisplayString())

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("Global.TestStruct(Of KKK).  operator+", {"Function TestStruct(Of KKK).op_Addition(a As System.Int32, b As TestStruct(Of KKK)) As System.String"}, {}),
                          New NameSyntaxInfo("Global.TestStruct(Of KKK)", {"TestStruct(Of KKK)"}, {"TestStruct(Of KKK)"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("TestStruct(Of KKK)", {"TestStruct(Of KKK)"}, {"TestStruct(Of KKK)"}),
                          New NameSyntaxInfo("KKK", {"KKK"}, {"KKK"}),
                          New NameSyntaxInfo("operator+", {"Function TestStruct(Of KKK).op_Addition(a As System.Int32, b As TestStruct(Of KKK)) As System.String"}, {}),
                          New NameSyntaxInfo("TestStruct(Of kkk)", {"TestStruct(Of KKK)"}, {"TestStruct(Of KKK)"}),
                          New NameSyntaxInfo("kkk", {"KKK"}, {"KKK"}))
        End Sub

        <Fact>
        Private Sub ExtendedCref_OperatorReference()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz
    Public Structure TestStruct
        ''' <see cref="operator+(integer, Global.Clazz.TestStruct)"/>
        Public Shared field As Integer

        Public Shared Operator +(a As Integer, b As TestStruct) As String
            Return Nothing
        End Operator
    End Structure
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:Clazz.TestStruct.field">
 <see cref="M:Clazz.TestStruct.op_Addition(System.Int32,Clazz.TestStruct)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(1, crefNodes.Count)

            Dim info = model.GetSymbolInfo(crefNodes(0))
            Assert.NotNull(info.Symbol)
            Assert.Equal("Function Clazz.TestStruct.op_Addition(a As System.Int32, b As Clazz.TestStruct) As System.String", info.Symbol.ToTestDisplayString())

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("operator+", {"Function Clazz.TestStruct.op_Addition(a As System.Int32, b As Clazz.TestStruct) As System.String"}, {}),
                          New NameSyntaxInfo("Global.Clazz.TestStruct", {"Clazz.TestStruct"}, {"Clazz.TestStruct"}),
                          New NameSyntaxInfo("Global.Clazz", {"Clazz"}, {"Clazz"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("Clazz", {"Clazz"}, {"Clazz"}),
                          New NameSyntaxInfo("TestStruct", {"Clazz.TestStruct"}, {"Clazz.TestStruct"}))
        End Sub

        <Fact>
        Private Sub ExtendedCref_ReturnType()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class GenericClazz(Of T)
End Class

Public Class Clazz(Of X)
    Public Structure TestStruct
        ''' <see cref="operator ctype(Integer)"/>
        ''' <see cref="operator ctype(Integer) As TestStruct"/>
        ''' <see cref="Global.Clazz(Of A)  . TestStruct. operator ctype(Clazz(Of A).TestStruct) As A"/>
        ''' <see cref="Global.Clazz(Of B)  . TestStruct. operator ctype(B)"/>
        ''' <see cref="operator ctype(TestStruct) As Global.Clazz(Of Integer)"/>
        ''' <see cref="Clazz(Of C).TestStruct.operator ctype(Clazz(Of C). TestStruct) As GenericClazz(Of C)"/>
        Public Shared field As Integer

        Public Shared Narrowing Operator CType(a As Integer) As TestStruct
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(a As TestStruct) As X
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(a As TestStruct) As GenericClazz(Of X)
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(a As X) As TestStruct
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(a As TestStruct) As Global.Clazz(Of Integer)
            Return Nothing
        End Operator
    End Structure
End Class

]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:Clazz`1.TestStruct.field">
 <see cref="M:Clazz`1.TestStruct.op_Explicit(System.Int32)~Clazz{`0}.TestStruct"/>
 <see cref="M:Clazz`1.TestStruct.op_Explicit(System.Int32)~Clazz{`0}.TestStruct"/>
 <see cref="M:Clazz`1.TestStruct.op_Explicit(Clazz{`0}.TestStruct)~`0"/>
 <see cref="M:Clazz`1.TestStruct.op_Explicit(`0)~Clazz{`0}.TestStruct"/>
 <see cref="M:Clazz`1.TestStruct.op_Explicit(Clazz{`0}.TestStruct)~Clazz{System.Int32}"/>
 <see cref="M:Clazz`1.TestStruct.op_Explicit(Clazz{`0}.TestStruct)~GenericClazz{`0}"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(6, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {"Function Clazz(Of X).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of X).TestStruct",
                              "Function Clazz(Of X).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of X).TestStruct",
                              "Function Clazz(Of A).TestStruct.op_Explicit(a As Clazz(Of A).TestStruct) As A",
                              "Function Clazz(Of B).TestStruct.op_Explicit(a As B) As Clazz(Of B).TestStruct",
                              "Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As Clazz(Of System.Int32)",
                              "Function Clazz(Of C).TestStruct.op_Explicit(a As Clazz(Of C).TestStruct) As GenericClazz(Of C)"}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                If name Is Nothing Then
                    Assert.True(info.IsEmpty)
                Else
                    Assert.NotNull(info.Symbol)
                    Assert.Equal(name, info.Symbol.ToTestDisplayString())
                End If

                index += 1
            Next

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As GenericClazz(Of X)",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As X",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of X).TestStruct",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As X) As Clazz(Of X).TestStruct"},
                                             {}))

            CheckAllNames(model, crefNodes(1),
                          New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As GenericClazz(Of X)",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As X",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of X).TestStruct",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As X) As Clazz(Of X).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("TestStruct", {"Clazz(Of X).TestStruct"}, {"Clazz(Of X).TestStruct"}))

            CheckAllNames(model, crefNodes(2),
                          New NameSyntaxInfo("Global.Clazz(Of A)  . TestStruct. operator ctype",
                                             {"Function Clazz(Of A).TestStruct.op_Explicit(a As Clazz(Of A).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As Clazz(Of A).TestStruct) As GenericClazz(Of A)",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As Clazz(Of A).TestStruct) As A",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of A).TestStruct",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As A) As Clazz(Of A).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("Global.Clazz(Of A)  . TestStruct", {"Clazz(Of A).TestStruct"}, {"Clazz(Of A).TestStruct"}),
                          New NameSyntaxInfo("Global.Clazz(Of A)", {"Clazz(Of A)"}, {"Clazz(Of A)"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("Clazz(Of A)", {"Clazz(Of A)"}, {"Clazz(Of A)"}),
                          New NameSyntaxInfo("A", {"A"}, {"A"}),
                          New NameSyntaxInfo("TestStruct", {"Clazz(Of A).TestStruct"}, {"Clazz(Of A).TestStruct"}),
                          New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz(Of A).TestStruct.op_Explicit(a As Clazz(Of A).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As Clazz(Of A).TestStruct) As GenericClazz(Of A)",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As Clazz(Of A).TestStruct) As A",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of A).TestStruct",
                                              "Function Clazz(Of A).TestStruct.op_Explicit(a As A) As Clazz(Of A).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("Clazz(Of A).TestStruct", {"Clazz(Of A).TestStruct"}, {"Clazz(Of A).TestStruct"}),
                          New NameSyntaxInfo("Clazz(Of A)", {"Clazz(Of A)"}, {"Clazz(Of A)"}),
                          New NameSyntaxInfo("A", {"A"}, {"A"}),
                          New NameSyntaxInfo("TestStruct", {"Clazz(Of A).TestStruct"}, {"Clazz(Of A).TestStruct"}),
                          New NameSyntaxInfo("A", {"A"}, {"A"}))

            CheckAllNames(model, crefNodes(3),
                          New NameSyntaxInfo("Global.Clazz(Of B)  . TestStruct. operator ctype",
                                             {"Function Clazz(Of B).TestStruct.op_Explicit(a As Clazz(Of B).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As Clazz(Of B).TestStruct) As GenericClazz(Of B)",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As Clazz(Of B).TestStruct) As B",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of B).TestStruct",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As B) As Clazz(Of B).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("Global.Clazz(Of B)  . TestStruct", {"Clazz(Of B).TestStruct"}, {"Clazz(Of B).TestStruct"}),
                          New NameSyntaxInfo("Global.Clazz(Of B)", {"Clazz(Of B)"}, {"Clazz(Of B)"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("Clazz(Of B)", {"Clazz(Of B)"}, {"Clazz(Of B)"}),
                          New NameSyntaxInfo("B", {"B"}, {"B"}),
                          New NameSyntaxInfo("TestStruct", {"Clazz(Of B).TestStruct"}, {"Clazz(Of B).TestStruct"}),
                          New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz(Of B).TestStruct.op_Explicit(a As Clazz(Of B).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As Clazz(Of B).TestStruct) As GenericClazz(Of B)",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As Clazz(Of B).TestStruct) As B",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of B).TestStruct",
                                              "Function Clazz(Of B).TestStruct.op_Explicit(a As B) As Clazz(Of B).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("B", {"B"}, {"B"}))

            CheckAllNames(model, crefNodes(4),
                          New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As GenericClazz(Of X)",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As Clazz(Of X).TestStruct) As X",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of X).TestStruct",
                                              "Function Clazz(Of X).TestStruct.op_Explicit(a As X) As Clazz(Of X).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("TestStruct", {"Clazz(Of X).TestStruct"}, {"Clazz(Of X).TestStruct"}),
                          New NameSyntaxInfo("Global.Clazz(Of Integer)", {"Clazz(Of System.Int32)"}, {"Clazz(Of System.Int32)"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("Clazz(Of Integer)", {"Clazz(Of System.Int32)"}, {"Clazz(Of System.Int32)"}))

            CheckAllNames(model, crefNodes(5),
                          New NameSyntaxInfo("Clazz(Of C).TestStruct.operator ctype",
                                             {"Function Clazz(Of C).TestStruct.op_Explicit(a As Clazz(Of C).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As Clazz(Of C).TestStruct) As GenericClazz(Of C)",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As Clazz(Of C).TestStruct) As C",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of C).TestStruct",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As C) As Clazz(Of C).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("Clazz(Of C).TestStruct", {"Clazz(Of C).TestStruct"}, {"Clazz(Of C).TestStruct"}),
                          New NameSyntaxInfo("Clazz(Of C)", {"Clazz(Of C)"}, {"Clazz(Of C)"}),
                          New NameSyntaxInfo("C", {"C"}, {"C"}),
                          New NameSyntaxInfo("TestStruct", {"Clazz(Of C).TestStruct"}, {"Clazz(Of C).TestStruct"}),
                          New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz(Of C).TestStruct.op_Explicit(a As Clazz(Of C).TestStruct) As Clazz(Of System.Int32)",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As Clazz(Of C).TestStruct) As GenericClazz(Of C)",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As Clazz(Of C).TestStruct) As C",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As System.Int32) As Clazz(Of C).TestStruct",
                                              "Function Clazz(Of C).TestStruct.op_Explicit(a As C) As Clazz(Of C).TestStruct"},
                                             {}),
                          New NameSyntaxInfo("Clazz(Of C). TestStruct", {"Clazz(Of C).TestStruct"}, {"Clazz(Of C).TestStruct"}),
                          New NameSyntaxInfo("Clazz(Of C)", {"Clazz(Of C)"}, {"Clazz(Of C)"}),
                          New NameSyntaxInfo("C", {"C"}, {"C"}),
                          New NameSyntaxInfo("TestStruct", {"Clazz(Of C).TestStruct"}, {"Clazz(Of C).TestStruct"}),
                          New NameSyntaxInfo("GenericClazz(Of C)", {"GenericClazz(Of C)"}, {"GenericClazz(Of C)"}),
                          New NameSyntaxInfo("C", {"C"}, {"C"}))
        End Sub

        <Fact>
        Private Sub ExtendedCref_ColorColor()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class SomeClass
    Public Sub SB()
    End Sub
End Class

Public Class Clazz

    Public someClass As SomeClass

    Public Structure TestStruct
        ''' <see cref="someclass.sb()"/>
        ''' <see cref="operator ctype(SomeClass) As TestStruct"/>
        ''' <see cref="operator ctype(TestStruct) As SomeClass"/>
        Public Shared field As Integer

        Public Shared Narrowing Operator CType(a As TestStruct) As SomeClass
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(a As SomeClass) As TestStruct
            Return Nothing
        End Operator
    End Structure
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:Clazz.TestStruct.field">
 <see cref="M:SomeClass.SB"/>
 <see cref="M:Clazz.TestStruct.op_Explicit(SomeClass)~Clazz.TestStruct"/>
 <see cref="M:Clazz.TestStruct.op_Explicit(Clazz.TestStruct)~SomeClass"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(3, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {"Sub SomeClass.SB()",
                              "Function Clazz.TestStruct.op_Explicit(a As SomeClass) As Clazz.TestStruct",
                              "Function Clazz.TestStruct.op_Explicit(a As Clazz.TestStruct) As SomeClass"}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                If name Is Nothing Then
                    Assert.True(info.IsEmpty)
                Else
                    Assert.NotNull(info.Symbol)
                    Assert.Equal(name, info.Symbol.ToTestDisplayString())
                End If

                index += 1
            Next

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("someclass.sb", {"Sub SomeClass.SB()"}, {}),
                          New NameSyntaxInfo("someclass", {"SomeClass"}, {"SomeClass"}),
                          New NameSyntaxInfo("sb", {"Sub SomeClass.SB()"}, {}))

            CheckAllNames(model, crefNodes(1),
                          New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz.TestStruct.op_Explicit(a As SomeClass) As Clazz.TestStruct",
                                              "Function Clazz.TestStruct.op_Explicit(a As Clazz.TestStruct) As SomeClass"},
                                             {}),
                          New NameSyntaxInfo("SomeClass", {"SomeClass"}, {"SomeClass"}),
                          New NameSyntaxInfo("TestStruct", {"Clazz.TestStruct"}, {"Clazz.TestStruct"}))

            CheckAllNames(model, crefNodes(2),
                         New NameSyntaxInfo("operator ctype",
                                             {"Function Clazz.TestStruct.op_Explicit(a As SomeClass) As Clazz.TestStruct",
                                              "Function Clazz.TestStruct.op_Explicit(a As Clazz.TestStruct) As SomeClass"},
                                             {}),
                          New NameSyntaxInfo("TestStruct", {"Clazz.TestStruct"}, {"Clazz.TestStruct"}),
                          New NameSyntaxInfo("SomeClass", {"SomeClass"}, {"SomeClass"}))
        End Sub

        <Fact>
        Private Sub ExtendedCref_ReferencingConversionByMethodName()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class SomeClass
End Class

Public Structure TestStruct
    ''' <see cref="op_Implicit(SomeClass)"/>
    ''' <see cref="op_Explicit(TestStruct)"/>
    Public Shared field As Integer

    Public Shared Narrowing Operator CType(a As TestStruct) As SomeClass
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(a As SomeClass) As TestStruct
        Return Nothing
    End Operator
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:TestStruct.op_Implicit(SomeClass)~TestStruct"/>
 <see cref="M:TestStruct.op_Explicit(TestStruct)~SomeClass"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(2, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {"Function TestStruct.op_Implicit(a As SomeClass) As TestStruct",
                              "Function TestStruct.op_Explicit(a As TestStruct) As SomeClass"}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                If name Is Nothing Then
                    Assert.True(info.IsEmpty)
                Else
                    Assert.NotNull(info.Symbol)
                    Assert.Equal(name, info.Symbol.ToTestDisplayString())
                End If

                index += 1
            Next
        End Sub

        <Fact>
        Private Sub ExtendedCref_ReferencingConversionByMethodName_ReturnTypeOverloading()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class SomeClass
End Class

Public Structure TestStruct
    ''' <see cref="op_Implicit(TestStruct) As SomeClass"/>
    ''' <see cref="op_Implicit(TestStruct) As String"/>
    Public Shared field As Integer

    Public Shared Widening Operator CType(a As TestStruct) As SomeClass
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(a As TestStruct) As String
        Return Nothing
    End Operator
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:TestStruct.op_Implicit(TestStruct)~SomeClass"/>
 <see cref="M:TestStruct.op_Implicit(TestStruct)~System.String"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(2, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {"Function TestStruct.op_Implicit(a As TestStruct) As SomeClass",
                              "Function TestStruct.op_Implicit(a As TestStruct) As System.String"}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                If name Is Nothing Then
                    Assert.True(info.IsEmpty)
                Else
                    Assert.NotNull(info.Symbol)
                    Assert.Equal(name, info.Symbol.ToTestDisplayString())
                End If

                index += 1
            Next
        End Sub

        <Fact>
        Private Sub ExtendedCref_MemberAndTypeParamConflict()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System


Public Structure T(Of X)
    Class T(Of Y)
        Sub T(Of T)(p As T, i as TestStruct)
        End Sub
    End Class
End Structure

Public Structure TestStruct
    ''' <see cref="Global.T(Of T).T(Of T).T(Of T)(T,  TestStruct)"/>
    Public Shared field As Integer
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:T`1.T`1.T``1(``0,TestStruct)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(1, crefNodes.Count)

            Dim info = model.GetSymbolInfo(crefNodes(0))
            Assert.NotNull(info.Symbol)
            Assert.Equal("Sub T(Of T).T(Of T).T(Of T)(p As T, i As TestStruct)", info.Symbol.ToTestDisplayString())

            CheckAllNames(model, crefNodes(0),
                          New NameSyntaxInfo("Global.T(Of T).T(Of T).T(Of T)", {"Sub T(Of T).T(Of T).T(Of T)(p As T, i As TestStruct)"}, {}),
                          New NameSyntaxInfo("Global.T(Of T).T(Of T)", {"T(Of T).T(Of T)"}, {"T(Of T).T(Of T)"}),
                          New NameSyntaxInfo("Global.T(Of T)", {"T(Of T)"}, {"T(Of T)"}),
                          New NameSyntaxInfo("Global", {"Global"}, {}),
                          New NameSyntaxInfo("T(Of T)", {"T(Of T)"}, {"T(Of T)"}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("T(Of T)", {"T(Of T).T(Of T)"}, {"T(Of T).T(Of T)"}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("T(Of T)", {"Sub T(Of T).T(Of T).T(Of T)(p As T, i As TestStruct)"}, {}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("T", {"T"}, {"T"}),
                          New NameSyntaxInfo("TestStruct", {"TestStruct"}, {"TestStruct"}))
        End Sub

        Private Structure NameSyntaxInfo
            Public ReadOnly Syntax As String
            Public ReadOnly Symbols As String()
            Public ReadOnly Types As String()

            Public Sub New(syntax As String, symbols As String(), types As String())
                Me.Syntax = syntax
                Me.Symbols = symbols
                Me.Types = types
            End Sub
        End Structure

        Private Sub CheckAllNames(model As SemanticModel, cref As CrefReferenceSyntax, ParamArray expected As NameSyntaxInfo())
            Dim names = NameSyntaxFinder.FindNames(cref)
            Assert.Equal(expected.Length, names.Count)

            For i = 0 To names.Count - 1
                Dim e = expected(i)
                Dim sym = names(i)

                Assert.Equal(e.Syntax, sym.ToString().Trim())

                Dim actual = model.GetSymbolInfo(sym)

                If e.Symbols.Length = 0 Then
                    Assert.True(actual.IsEmpty)
                ElseIf e.Symbols.Length = 1 Then
                    Assert.NotNull(actual.Symbol)
                    Assert.Equal(e.Symbols(0), actual.Symbol.ToTestDisplayString)
                Else
                    Assert.Equal(CandidateReason.Ambiguous, actual.CandidateReason)
                    AssertStringArraysEqual(e.Symbols, (From s In actual.CandidateSymbols Select s.ToTestDisplayString()).ToArray)
                End If

                Dim typeInfo = model.GetTypeInfo(sym)

                If e.Types.Length = 0 Then
                    Assert.Null(typeInfo.Type)

                ElseIf e.Types.Length = 1 Then
                    Assert.NotNull(typeInfo.Type)
                    Assert.Equal(e.Types(0), typeInfo.Type.ToTestDisplayString())

                Else
                    Assert.Null(typeInfo.Type)
                End If
            Next
        End Sub

        <Fact>
        Private Sub ExtendedCref_UnaryOperatorsAndConversion()

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz
    Public Shared Operator Like(a As Clazz, b As Integer) As Clazz
        Return Nothing
    End Operator
    Public Shared Operator And(a As Clazz, b As Integer) As Clazz
        Return Nothing
    End Operator
    Public Shared Operator Or(a As Clazz, b As Integer) As Clazz
        Return Nothing
    End Operator
    Public Shared Operator Xor(a As Clazz, b As Integer) As Clazz
        Return Nothing
    End Operator
    Public Shared Operator Mod(a As Clazz, b As Integer) As Clazz
        Return Nothing
    End Operator

    Public Shared Operator Not(a As Clazz) As Boolean
        Return Nothing
    End Operator
    Public Shared Operator IsTrue(a As Clazz) As Boolean
        Return Nothing
    End Operator
    Public Shared Operator IsFalse(a As Clazz) As Boolean
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(a As Clazz) As String
        Return Nothing
    End Operator
    Public Shared Widening Operator CType(a As Clazz) As Integer?
        Return Nothing
    End Operator
    Public Shared Widening Operator CType(a As Integer?) As Clazz
        Return Nothing
    End Operator
    Public Shared Narrowing Operator CType(a As Integer) As Clazz
        Return Nothing
    End Operator

    ''' <see cref="operator Like (Clazz, Int32)"/>
    ''' <see cref="Clazz. operator And (Clazz, Int32)"/>
    ''' <see cref="operator Or (Clazz, Int32)"/>
    ''' <see cref=" Clazz. operator Xor (Clazz, Int32)"/>
    ''' <see cref="operator Mod (Clazz, Int32)"/>
    ''' <see cref="Global . Clazz. operator  Not (Clazz)"/>
    ''' <see cref=" operator istrue (Clazz)"/>
    ''' <see cref="  Clazz. operator isfalse (Clazz)"/>
    ''' <see cref=" operator ctype (Clazz) as string"/>
    ''' <see cref=" Clazz. operator ctype (Clazz) as integer?"/>
    ''' <see cref=" operator ctype (integer?) as clazz"/>
    ''' <see cref=" operator ctype (integer) as clazz"/>
    ''' <see cref=" Global . Clazz. operator ctype (integer) as clazz"/>
    Public Shared field As Integer
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:Clazz.field">
 <see cref="M:Clazz.op_Like(Clazz,System.Int32)"/>
 <see cref="M:Clazz.op_BitwiseAnd(Clazz,System.Int32)"/>
 <see cref="M:Clazz.op_BitwiseOr(Clazz,System.Int32)"/>
 <see cref="M:Clazz.op_ExclusiveOr(Clazz,System.Int32)"/>
 <see cref="M:Clazz.op_Modulus(Clazz,System.Int32)"/>
 <see cref="M:Clazz.op_OnesComplement(Clazz)"/>
 <see cref="M:Clazz.op_True(Clazz)"/>
 <see cref="M:Clazz.op_False(Clazz)"/>
 <see cref="M:Clazz.op_Explicit(Clazz)~System.String"/>
 <see cref="M:Clazz.op_Implicit(Clazz)~System.Nullable{System.Int32}"/>
 <see cref="M:Clazz.op_Implicit(System.Nullable{System.Int32})~Clazz"/>
 <see cref="M:Clazz.op_Explicit(System.Int32)~Clazz"/>
 <see cref="M:Clazz.op_Explicit(System.Int32)~Clazz"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(13, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {"Function Clazz.op_Like(a As Clazz, b As System.Int32) As Clazz",
                              "Function Clazz.op_BitwiseAnd(a As Clazz, b As System.Int32) As Clazz",
                              "Function Clazz.op_BitwiseOr(a As Clazz, b As System.Int32) As Clazz",
                              "Function Clazz.op_ExclusiveOr(a As Clazz, b As System.Int32) As Clazz",
                              "Function Clazz.op_Modulus(a As Clazz, b As System.Int32) As Clazz",
                              "Function Clazz.op_OnesComplement(a As Clazz) As System.Boolean",
                              "Function Clazz.op_True(a As Clazz) As System.Boolean",
                              "Function Clazz.op_False(a As Clazz) As System.Boolean",
                              "Function Clazz.op_Explicit(a As Clazz) As System.String",
                              "Function Clazz.op_Implicit(a As Clazz) As System.Nullable(Of System.Int32)",
                              "Function Clazz.op_Implicit(a As System.Nullable(Of System.Int32)) As Clazz",
                              "Function Clazz.op_Explicit(a As System.Int32) As Clazz",
                              "Function Clazz.op_Explicit(a As System.Int32) As Clazz"}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                Assert.NotNull(info.Symbol)
                Assert.Equal(name, info.Symbol.ToTestDisplayString())

                index += 1
            Next
        End Sub

        <Fact>
        Private Sub ExtendedCref_StandaloneSimpleName()

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz

    Public Structure STR
    End Structure

    Public Sub New()
    End Sub

    Public Sub New(s As Integer)
    End Sub

    Public Sub New(s As Clazz)
    End Sub

    Public Sub [New](Of T)(s As Integer)
    End Sub

    Public Sub [New](Of T)(s As Clazz)
    End Sub

    Public Sub [New](Of T)(s As T)
    End Sub

    Public Sub S0(s As Integer)
    End Sub

    Public Sub S0(s As String)
    End Sub

    Public Sub S0(s As STR)
    End Sub

    ''' <see cref="New(Clazz)"/>
    ''' <see cref="New(Int32)"/>
    ''' <see cref="New()"/>
    ''' <see cref="New(STR)"/>
    ''' <see cref="[New](Clazz)"/>
    ''' <see cref="[New](Of T)(Clazz)"/>
    ''' <see cref="[New](Of T)(T)"/>
    ''' <see cref="[New](Of T, W)(T)"/>
    ''' <see cref="S0(Of T)(T)"/>
    ''' <see cref="S0(STR)"/>
    Public Shared field As Integer
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:Clazz.field">
 <see cref="M:Clazz.#ctor(Clazz)"/>
 <see cref="M:Clazz.#ctor(System.Int32)"/>
 <see cref="M:Clazz.#ctor"/>
 <see cref="!:New(STR)"/>
 <see cref="!:[New](Clazz)"/>
 <see cref="M:Clazz.New``1(Clazz)"/>
 <see cref="M:Clazz.New``1(``0)"/>
 <see cref="!:[New](Of T, W)(T)"/>
 <see cref="!:S0(Of T)(T)"/>
 <see cref="M:Clazz.S0(Clazz.STR)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'New(STR)' that could not be resolved.
    ''' <see cref="New(STR)"/>
             ~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute '[New](Clazz)' that could not be resolved.
    ''' <see cref="[New](Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute '[New](Of T, W)(T)' that could not be resolved.
    ''' <see cref="[New](Of T, W)(T)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'S0(Of T)(T)' that could not be resolved.
    ''' <see cref="S0(Of T)(T)"/>
             ~~~~~~~~~~~~~~~~~~
]]>
</errors>,
xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(10, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {"Sub Clazz..ctor(s As Clazz)",
                              "Sub Clazz..ctor(s As System.Int32)",
                              "Sub Clazz..ctor()",
                              Nothing,
                              Nothing,
                              "Sub Clazz.[New](Of T)(s As Clazz)",
                              "Sub Clazz.[New](Of T)(s As T)",
                              Nothing,
                              Nothing,
                              "Sub Clazz.S0(s As Clazz.STR)"}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                If name Is Nothing Then
                    Assert.True(info.IsEmpty)
                Else
                    Assert.NotNull(info.Symbol)
                    Assert.Equal(name, info.Symbol.ToTestDisplayString())
                End If

                index += 1
            Next
        End Sub

        <Fact>
        Private Sub ExtendedCref_QualifiedName_A()

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz
    Public Structure STR
    End Structure

    Public Sub New()
    End Sub

    Public Sub New(s As Integer)
    End Sub

    Public Sub New(s As Clazz)
    End Sub

    Public Sub [New](Of T)(s As Integer)
    End Sub

    Public Sub [New](Of T)(s As Clazz)
    End Sub

    Public Sub [New](Of T)(s As T)
    End Sub

    Public Sub S0(s As Integer)
    End Sub

    Public Sub S0(s As String)
    End Sub

    Public Sub S0(s As STR)
    End Sub
End Class

Public Structure TestStruct
    ''' <see cref="Clazz.New(Clazz)"/>
    ''' <see cref="Global.Clazz.New(Int32)"/>
    ''' <see cref="Clazz.New()"/>
    ''' <see cref="Clazz.New(Clazz.STR)"/>
    ''' <see cref="Clazz.[New](Clazz)"/>
    ''' <see cref="Clazz.[New](Of T)(Clazz)"/>
    ''' <see cref="Global.Clazz.[New](Of T)(T)"/>
    ''' <see cref="Clazz.[New](Of T, W)(T)"/>
    ''' <see cref="Clazz.S0(Of T)(T)"/>
    ''' <see cref="Global.Clazz.S0(Clazz.STR)"/>
    Public Shared field As Integer
End Structure
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="M:Clazz.#ctor(Clazz)"/>
 <see cref="M:Clazz.#ctor(System.Int32)"/>
 <see cref="M:Clazz.#ctor"/>
 <see cref="!:Clazz.New(Clazz.STR)"/>
 <see cref="!:Clazz.[New](Clazz)"/>
 <see cref="M:Clazz.New``1(Clazz)"/>
 <see cref="M:Clazz.New``1(``0)"/>
 <see cref="!:Clazz.[New](Of T, W)(T)"/>
 <see cref="!:Clazz.S0(Of T)(T)"/>
 <see cref="M:Clazz.S0(Clazz.STR)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz.New(Clazz.STR)' that could not be resolved.
    ''' <see cref="Clazz.New(Clazz.STR)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz.[New](Clazz)' that could not be resolved.
    ''' <see cref="Clazz.[New](Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz.[New](Of T, W)(T)' that could not be resolved.
    ''' <see cref="Clazz.[New](Of T, W)(T)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz.S0(Of T)(T)' that could not be resolved.
    ''' <see cref="Clazz.S0(Of T)(T)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>,
xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(10, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {"Sub Clazz..ctor(s As Clazz)",
                              "Sub Clazz..ctor(s As System.Int32)",
                              "Sub Clazz..ctor()",
                              Nothing,
                              Nothing,
                              "Sub Clazz.[New](Of T)(s As Clazz)",
                              "Sub Clazz.[New](Of T)(s As T)",
                              Nothing,
                              Nothing,
                              "Sub Clazz.S0(s As Clazz.STR)"}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                If name Is Nothing Then
                    Assert.True(info.IsEmpty)
                Else
                    Assert.NotNull(info.Symbol)
                    Assert.Equal(name, info.Symbol.ToTestDisplayString())
                End If

                index += 1
            Next
        End Sub

        <Fact>
        Private Sub ExtendedCref_QualifiedName_B()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Structure Struct
End Structure

Public Class Clazz(Of A, B)
    Public Sub New()
    End Sub
    Public Sub New(s As Integer)
    End Sub
    Public Sub New(s As Clazz(Of Integer, Struct))
    End Sub
    Public Sub New(s As Clazz(Of A, B))
    End Sub

    Public Sub [New](Of T)(s As Integer)
    End Sub
    Public Sub [New](Of T)(s As Clazz(Of T, T))
    End Sub
    Public Sub [New](Of T)(s As Clazz(Of T, B))
    End Sub
    Public Sub [New](Of T)(s As T)
    End Sub

    Public Sub S0(s As Integer)
    End Sub
    Public Sub S0(s As String)
    End Sub
    Public Sub S0(Of X, Y)(a As A, b As B, c As X, d As Y)
    End Sub
End Class

Public Structure TestStruct
    ''' <see cref="Clazz.New(Integer)"/>
    ''' <see cref="Global.Clazz(Of X, Y).New(Int32)"/>
    ''' <see cref="Global.Clazz(Of X, Y).New(Clazz(Of X, Y))"/>
    ''' <see cref="Global.Clazz(Of X, Y).New(Clazz(Of Int32, Y))"/>
    ''' <see cref="Global.Clazz(Of X, Y).New(Clazz(Of Int32, Struct))"/>
    ''' <see cref="Clazz(Of X, Y).New()"/>
    ''' <see cref="Clazz(Of [Integer], Y).New([Integer])"/>
    ''' <see cref="Clazz(Of X, Y).[New](Clazz)"/>
    ''' <see cref="Clazz(Of X(Of D), Y).[New](Of T)(Int32)"/>
    ''' <see cref="Clazz(Of X, Y).[New](Of X)(X)"/>
    ''' <see cref="Clazz(Of X, Y).[New](Of X)(Y)"/>
    ''' <see cref="Clazz(Of X, Y).[New](Of T)(T)"/>
    ''' <see cref="Clazz(Of X, Y).[New](Of X)(Clazz(Of X, X))"/>
    ''' <see cref="Clazz(Of X, Y).[New](Of X)(Clazz(Of X, Y))"/>
    ''' <see cref="Clazz(Of X, Y).S0(Of T, U)(X, Y, T, U)"/>
    ''' <see cref="Global.Clazz(Of X, Y).S0(Of X, Y)(X, Y, X, Y)"/>
    Public Shared field As Integer
End Structure

]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:TestStruct.field">
 <see cref="!:Clazz.New(Integer)"/>
 <see cref="M:Clazz`2.#ctor(System.Int32)"/>
 <see cref="M:Clazz`2.#ctor(Clazz{`0,`1})"/>
 <see cref="!:Global.Clazz(Of X, Y).New(Clazz(Of Int32, Y))"/>
 <see cref="M:Clazz`2.#ctor(Clazz{System.Int32,Struct})"/>
 <see cref="M:Clazz`2.#ctor"/>
 <see cref="!:Clazz(Of [Integer], Y).New([Integer])"/>
 <see cref="!:Clazz(Of X, Y).[New](Clazz)"/>
 <see cref="!:Clazz(Of X(Of D), Y).[New](Of T)(Int32)"/>
 <see cref="M:Clazz`2.New``1(``0)"/>
 <see cref="!:Clazz(Of X, Y).[New](Of X)(Y)"/>
 <see cref="M:Clazz`2.New``1(``0)"/>
 <see cref="M:Clazz`2.New``1(Clazz{``0,``0})"/>
 <see cref="M:Clazz`2.New``1(Clazz{``0,`1})"/>
 <see cref="M:Clazz`2.S0``2(`0,`1,``0,``1)"/>
 <see cref="!:Global.Clazz(Of X, Y).S0(Of X, Y)(X, Y, X, Y)"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource,
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz.New(Integer)' that could not be resolved.
    ''' <see cref="Clazz.New(Integer)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Global.Clazz(Of X, Y).New(Clazz(Of Int32, Y))' that could not be resolved.
    ''' <see cref="Global.Clazz(Of X, Y).New(Clazz(Of Int32, Y))"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz(Of [Integer], Y).New([Integer])' that could not be resolved.
    ''' <see cref="Clazz(Of [Integer], Y).New([Integer])"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz(Of X, Y).[New](Clazz)' that could not be resolved.
    ''' <see cref="Clazz(Of X, Y).[New](Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz(Of X(Of D), Y).[New](Of T)(Int32)' that could not be resolved.
    ''' <see cref="Clazz(Of X(Of D), Y).[New](Of T)(Int32)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Clazz(Of X, Y).[New](Of X)(Y)' that could not be resolved.
    ''' <see cref="Clazz(Of X, Y).[New](Of X)(Y)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Global.Clazz(Of X, Y).S0(Of X, Y)(X, Y, X, Y)' that could not be resolved.
    ''' <see cref="Global.Clazz(Of X, Y).S0(Of X, Y)(X, Y, X, Y)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>,
xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNodes = CrefFinder.FindAllCrefs(compilation.SyntaxTrees(0))
            Assert.Equal(16, crefNodes.Count)

            Dim index As Integer = 0
            For Each name In {Nothing,
                              "Sub Clazz(Of X, Y)..ctor(s As System.Int32)",
                              "Sub Clazz(Of X, Y)..ctor(s As Clazz(Of X, Y))",
                              Nothing,
                              "Sub Clazz(Of X, Y)..ctor(s As Clazz(Of System.Int32, Struct))",
                              "Sub Clazz(Of X, Y)..ctor()",
                              Nothing,
                              Nothing,
                              Nothing,
                              "Sub Clazz(Of X, Y).[New](Of X)(s As X)",
                              Nothing,
                              "Sub Clazz(Of X, Y).[New](Of T)(s As T)",
                              "Sub Clazz(Of X, Y).[New](Of X)(s As Clazz(Of X, X))",
                              "Sub Clazz(Of X, Y).[New](Of X)(s As Clazz(Of X, Y))",
                              "Sub Clazz(Of X, Y).S0(Of T, U)(a As X, b As Y, c As T, d As U)",
                              Nothing}

                Dim info = model.GetSymbolInfo(crefNodes(index))
                If name Is Nothing Then
                    Assert.True(info.IsEmpty)
                Else
                    Assert.NotNull(info.Symbol)
                    Assert.Equal(name, info.Symbol.ToTestDisplayString())
                End If

                index += 1
            Next
        End Sub

        <Fact>
        Public Sub Include_AttrMissing_XMLMissingFileOrPathAttribute1()
            Dim xmlText = <root><target>Included</target></root>
            Dim xmlFile = Temp.CreateFile(extension:=".xml").WriteAllText(xmlText.Value.ToString)

            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

''' <summary> 
''' <include file='**FILE**' />
''' <include path='//target' />
''' <include/>
''' </summary>
Class C
End Class
]]>
    </file>
</compilation>

            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, xmlFile),
    <error>
        <![CDATA[
BC42310: XML comment tag 'include' must have a 'path' attribute. XML comment will be ignored.
''' <include file='**FILE**' />
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42310: XML comment tag 'include' must have a 'file' attribute. XML comment will be ignored.
''' <include path='//target' />
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42310: XML comment tag 'include' must have a 'file' attribute. XML comment will be ignored.
''' <include/>
    ~~~~~~~~~~
BC42310: XML comment tag 'include' must have a 'path' attribute. XML comment will be ignored.
''' <include/>
    ~~~~~~~~~~
]]>
    </error>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <summary> 
 <!--warning BC42310: XML comment tag 'include' must have a 'path' attribute. XML comment will be ignored.-->
 <!--warning BC42310: XML comment tag 'include' must have a 'file' attribute. XML comment will be ignored.-->
 <!--warning BC42310: XML comment tag 'include' must have a 'file' attribute. XML comment will be ignored. warning BC42310: XML comment tag 'include' must have a 'path' attribute. XML comment will be ignored.-->
 </summary>
</member>
</members>
</doc>
]]>
    </xml>, ensureEnglishUICulture:=True)
        End Sub

        <Fact>
        Public Sub ExtendedCref_BinaryOperator()

            ExtendedCref_BinaryOperatorCore(" &", "op_Concatenate", <errors></errors>)

            ExtendedCref_BinaryOperatorCore("+", "op_Addition",
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator+(Clazz, String)' that could not be resolved.
    ''' <see cref="Operator+(Clazz, String)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator+(Clazz)' that could not be resolved.
    ''' <see cref="Operator+(Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("-", "op_Subtraction",
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator-(Clazz, String)' that could not be resolved.
    ''' <see cref="Operator-(Clazz, String)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator-(Clazz)' that could not be resolved.
    ''' <see cref="Operator-(Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("*", "op_Multiply",
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator*(Clazz, String)' that could not be resolved.
    ''' <see cref="Operator*(Clazz, String)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator*(Clazz)' that could not be resolved.
    ''' <see cref="Operator*(Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("/", "op_Division",
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator/(Clazz, String)' that could not be resolved.
    ''' <see cref="Operator/(Clazz, String)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator/(Clazz)' that could not be resolved.
    ''' <see cref="Operator/(Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("\", "op_IntegerDivision",
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator\(Clazz, String)' that could not be resolved.
    ''' <see cref="Operator\(Clazz, String)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator\(Clazz)' that could not be resolved.
    ''' <see cref="Operator\(Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("^", "op_Exponent",
<errors>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator^(Clazz, String)' that could not be resolved.
    ''' <see cref="Operator^(Clazz, String)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator^(Clazz)' that could not be resolved.
    ''' <see cref="Operator^(Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("<<", "op_LeftShift", <errors></errors>)
            ExtendedCref_BinaryOperatorCore(">>", "op_RightShift", <errors></errors>)
            ExtendedCref_BinaryOperatorCore("=", "op_Equality",
<errors>
    <![CDATA[
BC33033: Matching '<>' operator is required for 'Public Shared Operator =(a As Clazz, b As Integer) As Clazz'.
    Public Shared Operator =(a As Clazz, b As Integer) As Clazz
                           ~
BC33033: Matching '<>' operator is required for 'Public Shared Operator =(a As Clazz, b As Integer?) As Clazz'.
    Public Shared Operator =(a As Clazz, b As Integer?) As Clazz
                           ~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator=(Clazz, String)' that could not be resolved.
    ''' <see cref="Operator=(Clazz, String)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Operator=(Clazz)' that could not be resolved.
    ''' <see cref="Operator=(Clazz)"/>
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("<>", "op_Inequality",
<errors>
    <![CDATA[
BC33033: Matching '=' operator is required for 'Public Shared Operator <>(a As Clazz, b As Integer) As Clazz'.
    Public Shared Operator <>(a As Clazz, b As Integer) As Clazz
                           ~~
BC33033: Matching '=' operator is required for 'Public Shared Operator <>(a As Clazz, b As Integer?) As Clazz'.
    Public Shared Operator <>(a As Clazz, b As Integer?) As Clazz
                           ~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("<", "op_LessThan",
<errors>
    <![CDATA[
BC33033: Matching '>' operator is required for 'Public Shared Operator <(a As Clazz, b As Integer) As Clazz'.
    Public Shared Operator <(a As Clazz, b As Integer) As Clazz
                           ~
BC33033: Matching '>' operator is required for 'Public Shared Operator <(a As Clazz, b As Integer?) As Clazz'.
    Public Shared Operator <(a As Clazz, b As Integer?) As Clazz
                           ~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore(">", "op_GreaterThan",
<errors>
    <![CDATA[
BC33033: Matching '<' operator is required for 'Public Shared Operator >(a As Clazz, b As Integer) As Clazz'.
    Public Shared Operator >(a As Clazz, b As Integer) As Clazz
                           ~
BC33033: Matching '<' operator is required for 'Public Shared Operator >(a As Clazz, b As Integer?) As Clazz'.
    Public Shared Operator >(a As Clazz, b As Integer?) As Clazz
                           ~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore("<=", "op_LessThanOrEqual",
<errors>
    <![CDATA[
BC33033: Matching '>=' operator is required for 'Public Shared Operator <=(a As Clazz, b As Integer) As Clazz'.
    Public Shared Operator <=(a As Clazz, b As Integer) As Clazz
                           ~~
BC33033: Matching '>=' operator is required for 'Public Shared Operator <=(a As Clazz, b As Integer?) As Clazz'.
    Public Shared Operator <=(a As Clazz, b As Integer?) As Clazz
                           ~~
]]>
</errors>)
            ExtendedCref_BinaryOperatorCore(">=", "op_GreaterThanOrEqual",
<errors>
    <![CDATA[
BC33033: Matching '<=' operator is required for 'Public Shared Operator >=(a As Clazz, b As Integer) As Clazz'.
    Public Shared Operator >=(a As Clazz, b As Integer) As Clazz
                           ~~
BC33033: Matching '<=' operator is required for 'Public Shared Operator >=(a As Clazz, b As Integer?) As Clazz'.
    Public Shared Operator >=(a As Clazz, b As Integer?) As Clazz
                           ~~
]]>
</errors>)

        End Sub

        Private Sub ExtendedCref_BinaryOperatorCore(op As String, opName As String, errors As XElement)
            Dim invalidChars = op.Contains("<") OrElse op.Contains(">") OrElse op.Contains("&")
            Dim xmlSource =
                If(Not invalidChars,
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz
    Public Shared Operator {0}(a As Clazz, b As Integer) As Clazz
        Return Nothing
    End Operator
    Public Shared Operator {0}(a As Clazz, b As Integer?) As Clazz
        Return Nothing
    End Operator

    ''' <see cref="{1}(Clazz, Integer) As Clazz"/>
    ''' <see cref="Operator{0}(Clazz, Integer)"/>
    ''' <see cref="Operator{0}(Clazz, String)"/>
    ''' <see cref="Operator{0}(Clazz, Int32?)"/>
    ''' <see cref="Operator{0}(Clazz)"/>
    ''' <see cref="Clazz.Operator{0}(Clazz, Integer)"/>
    ''' <see cref="Global.Clazz.Operator{0}(Clazz, Integer?)"/>
    Public Shared field As Integer
End Class
]]>
    </file>
</compilation>,
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class Clazz
    Public Shared Operator {0}(a As Clazz, b As Integer) As Clazz
        Return Nothing
    End Operator
    Public Shared Operator {0}(a As Clazz, b As Integer?) As Clazz
        Return Nothing
    End Operator

    ''' <see cref="{1}(Clazz, Integer) As Clazz"/>
    ''' <see cref="Operator{0}(Clazz, Integer)"/>
    ''' <see cref="Operator{0}(Clazz, Int32?)"/>
    ''' <see cref="Clazz.Operator{0}(Clazz, Integer)"/>
    ''' <see cref="Global.Clazz.Operator{0}(Clazz, Integer?)"/>
    Public Shared field As Integer
End Class
]]>
    </file>
</compilation>)


            Dim xmlDoc = If(Not invalidChars,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:Clazz.field">
 <see cref="M:Clazz.{0}(Clazz,System.Int32)"/>
 <see cref="M:Clazz.{0}(Clazz,System.Int32)"/>
 <see cref="!:Operator{1}(Clazz, String)"/>
 <see cref="M:Clazz.{0}(Clazz,System.Nullable{{System.Int32}})"/>
 <see cref="!:Operator{1}(Clazz)"/>
 <see cref="M:Clazz.{0}(Clazz,System.Int32)"/>
 <see cref="M:Clazz.{0}(Clazz,System.Nullable{{System.Int32}})"/>
</member>
</members>
</doc>
]]>
</xml>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="F:Clazz.field">
 <see cref="M:Clazz.{0}(Clazz,System.Int32)"/>
 <see cref="M:Clazz.{0}(Clazz,System.Int32)"/>
 <see cref="M:Clazz.{0}(Clazz,System.Nullable{{System.Int32}})"/>
 <see cref="M:Clazz.{0}(Clazz,System.Int32)"/>
 <see cref="M:Clazz.{0}(Clazz,System.Nullable{{System.Int32}})"/>
</member>
</members>
</doc>
]]>
</xml>)

            Dim compilation =
                CompileCheckDiagnosticsAndXmlDocument(
                    FormatSourceXml(xmlSource, op, opName),
                    FormatXmlSimple(errors, op, If(op.Length = 1, "~", "~~")),
                    FormatXmlSimple(xmlDoc, opName, op))

            Dim crefNode = CrefFinder.FindCref(compilation.SyntaxTrees(0))

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))

            Assert.NotNull(crefNode)
            Dim info = model.GetSymbolInfo(crefNode)
            Assert.NotNull(info.Symbol)
            Assert.Equal(String.Format("Function Clazz.{0}(a As Clazz, b As System.Int32) As Clazz", opName),
                        info.Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub ExtendedCrefParsingTest()

            ParseExtendedCref("Int32.ToString", forceNoErrors:=True)
            ParseExtendedCref("Int32.ToString()", forceNoErrors:=False)
            ParseExtendedCref("Int32.ToString(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Int32.ToString(ByVal String, ByRef Integer)", forceNoErrors:=False)

            ParseExtendedCref("Int32.ToString(ByVal ByRef String, Integer) As Integer", checkErrors:=
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Int32.ToString(ByVal ByRef String, Integer) As Integer' that could not be resolved.
''' <see cref="Int32.ToString(ByVal ByRef String, Integer) As Integer"/> 
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>)

            ParseExtendedCref("Int32.ToString(String, Integer) As Integer", checkErrors:=
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Int32.ToString(String, Integer) As Integer' that could not be resolved.
''' <see cref="Int32.ToString(String, Integer) As Integer"/> 
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>)

            ParseExtendedCref("Operator IsTrue(String)", forceNoErrors:=False)
            ParseExtendedCref("Operator IsFalse(String)", forceNoErrors:=False)
            ParseExtendedCref("Operator Not(String)", forceNoErrors:=False)
            ParseExtendedCref("Operator+(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator -(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator*(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator /(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator^(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator \(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator&(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator<<(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator >> (String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator Mod(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator  Or(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator Xor(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator And(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator Like(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator =(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator <>(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator <(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator <=(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator  >(String, Integer)", forceNoErrors:=False)
            ParseExtendedCref("Operator  >=(String, Integer)", forceNoErrors:=False)

            ParseExtendedCref("Operator \(String, Integer) As Integer", checkErrors:=
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator \(String, Integer) As Integer' that could not be resolved.
''' <see cref="Operator \(String, Integer) As Integer"/> 
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>)

            ParseExtendedCref("Operator  Or(String, Integer).Name", checkErrors:=
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Operator  Or(String, Integer).Name' that could not be resolved.
''' <see cref="Operator  Or(String, Integer).Name"/> 
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</error>, overrideCrefText:="Operator  Or(String, Integer)")

            ParseExtendedCref("Clazz.Operator  >=(String, Integer)", forceNoErrors:=False)

            ParseExtendedCref("Clazz.Operator ctype(String)", forceNoErrors:=False)
            ParseExtendedCref("Clazz.Operator ctype(String) as Integer", forceNoErrors:=False)

            ParseExtendedCref("New", forceNoErrors:=False)
            ParseExtendedCref("[new]", forceNoErrors:=False)
            ParseExtendedCref("New()", forceNoErrors:=False)
            ParseExtendedCref("[new]()", forceNoErrors:=False)
            ParseExtendedCref("Clazz.New", forceNoErrors:=False)
            ParseExtendedCref("Clazz.[new]", forceNoErrors:=False)
            ParseExtendedCref("Clazz.New()", forceNoErrors:=False)
            ParseExtendedCref("Clazz.[new]()", forceNoErrors:=False)
            ParseExtendedCref("Clazz.[new]() As Integer", forceNoErrors:=False)

            ParseExtendedCref("String.ToString", overrideCrefText:="String", forceNoErrors:=True)
            ParseExtendedCref("String.ToString()", overrideCrefText:="String", forceNoErrors:=True)
            ParseExtendedCref("String.ToString(String, Integer)", overrideCrefText:="String", forceNoErrors:=True)

            ParseExtendedCref("sYSTEM.String")
            ParseExtendedCref("sYSTEM.String.", checkErrors:=
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'sYSTEM.String.' that could not be resolved.
''' <see cref="sYSTEM.String."/> 
         ~~~~~~~~~~~~~~~~~~~~~
]]>
</error>)

            ParseExtendedCref("Global.sYSTEM.String")
            ParseExtendedCref("Global", checkErrors:=
<error>
    <![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'Global' that could not be resolved.
''' <see cref="Global"/> 
         ~~~~~~~~~~~~~
]]>
</error>)
        End Sub

        Private Sub ParseExtendedCref(cref As String,
                                      Optional checkErrors As XElement = Nothing,
                                      Optional overrideCrefText As String = Nothing,
                                      Optional forceNoErrors As Boolean = False)
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
''' <see cref="{0}"/> 
Module Program
End Module
]]>
    </file>
</compilation>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(xmlSource, cref), If(forceNoErrors, <errors></errors>, checkErrors))

            Dim crefNode = CrefFinder.FindCref(compilation.SyntaxTrees(0))
            Assert.NotNull(crefNode)
            Assert.Equal(If(overrideCrefText, cref.Trim).Trim, crefNode.ToString())
        End Sub

        <Fact>
        Private Sub GetAliasInfo_Namespace()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports aNamespace = System.Collections.Generic

''' <see cref="aNamespace"/>
Public Class Clazz
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <see cref="N:System.Collections.Generic"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNode = CrefFinder.FindCref(compilation.SyntaxTrees(0))
            Assert.NotNull(crefNode)

            Dim info = model.GetSymbolInfo(crefNode)
            Assert.NotNull(info.Symbol)
            Assert.Equal("System.Collections.Generic", info.Symbol.ToTestDisplayString())

            CheckAllAliases(model, crefNode,
                            New AliasInfo("aNamespace", "System.Collections.Generic"))
        End Sub

        <WorkItem(757110, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/757110")>
        <Fact>
        Public Sub NoAssemblyElementForNetModule()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
                <compilation name="EmptyCref">
                    <file name="a.vb">
                        <![CDATA[
Imports System

''' <summary>
''' Test
''' </summary>
Class E
End Class
]]>
                    </file>
                </compilation>,
                TestOptions.ReleaseModule)
            CheckXmlDocument(comp,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<members>
<member name="T:E">
 <summary>
 Test
 </summary>
</member>
</members>
</doc>
]]>
</xml>)
        End Sub

        <Fact>
        Private Sub GetAliasInfo_Type()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports aType = System.Collections.Generic.List(Of Integer)

''' <see cref="aType"/>
Public Class Clazz
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <see cref="T:System.Collections.Generic.List`1"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNode = CrefFinder.FindCref(compilation.SyntaxTrees(0))
            Assert.NotNull(crefNode)

            Dim info = model.GetSymbolInfo(crefNode)
            Assert.NotNull(info.Symbol)
            Assert.Equal("System.Collections.Generic.List(Of System.Int32)", info.Symbol.ToTestDisplayString())

            CheckAllAliases(model, crefNode,
                            New AliasInfo("aType", "System.Collections.Generic.List(Of Integer)"))
        End Sub

        <Fact>
        Private Sub GetAliasInfo_NamespaceAndType()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports aNamespace = System.Collections.Generic

''' <see cref="aNamespace.List(Of T)"/>
Public Class Clazz
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <see cref="T:System.Collections.Generic.List`1"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNode = CrefFinder.FindCref(compilation.SyntaxTrees(0))
            Assert.NotNull(crefNode)

            Dim info = model.GetSymbolInfo(crefNode)
            Assert.NotNull(info.Symbol)
            Assert.Equal("System.Collections.Generic.List(Of T)", info.Symbol.ToTestDisplayString())

            CheckAllAliases(model, crefNode,
                            New AliasInfo("aNamespace", "System.Collections.Generic"),
                            New AliasInfo("T", Nothing))
        End Sub

        <Fact>
        Private Sub GetAliasInfo_TypeAndMethodWithSignature()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports aType = System.Collections.Generic.List(Of Integer)

''' <see cref="aType.ToString()"/>
Public Class Clazz
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <see cref="M:System.Object.ToString"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNode = CrefFinder.FindCref(compilation.SyntaxTrees(0))
            Assert.NotNull(crefNode)

            Dim info = model.GetSymbolInfo(crefNode)
            Assert.NotNull(info.Symbol)
            Assert.Equal("Function System.Object.ToString() As System.String", info.Symbol.ToTestDisplayString())

            CheckAllAliases(model, crefNode,
                            New AliasInfo("aType", "System.Collections.Generic.List(Of Integer)"),
                            New AliasInfo("ToString", Nothing))
        End Sub

        <Fact>
        Private Sub GetAliasInfo_TypeAndMethodCompat()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports aType = System.Collections.Generic.List(Of Integer)

''' <see cref="aType.ToString"/>
Public Class Clazz
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:Clazz">
 <see cref="M:System.Object.ToString"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim crefNode = CrefFinder.FindCref(compilation.SyntaxTrees(0))
            Assert.NotNull(crefNode)

            Dim info = model.GetSymbolInfo(crefNode)
            Assert.NotNull(info.Symbol)
            Assert.Equal("Function System.Object.ToString() As System.String", info.Symbol.ToTestDisplayString())

            CheckAllAliases(model, crefNode,
                            New AliasInfo("aType", "System.Collections.Generic.List(Of Integer)"),
                            New AliasInfo("ToString", Nothing))
        End Sub

        <WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")>
        <Fact>
        Public Sub Inaccessible1()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
''' <summary>
''' See <see cref="C.M"/>.
''' </summary>
Class A
End Class

Class C
    Private Sub M()
    End Sub
End Class
]]>
    </file>
</compilation>


            Dim compilation = CreateCompilationWithMscorlib(source, parseOptions:=s_optionsDiagnoseDocComments)

            ' Compat fix: match dev11 with inaccessible lookup
            compilation.AssertNoDiagnostics()
        End Sub

        <WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")>
        <Fact>
        Public Sub Inaccessible2()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
''' <summary>
''' See <see cref="C.Inner.M"/>.
''' </summary>
Class A
End Class

Class C
    Private Class Inner
        Private Sub M()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>


            Dim compilation = CreateCompilationWithMscorlib(source, parseOptions:=s_optionsDiagnoseDocComments)

            ' Compat fix: match dev11 with inaccessible lookup
            compilation.AssertNoDiagnostics()
        End Sub

        <WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")>
        <Fact>
        Public Sub Inaccessible3()
            Dim lib1Source =
<compilation name="A">
    <file name="a.vb">
Friend Class C
End Class
    </file>
</compilation>

            Dim lib2Source =
<compilation name="B">
    <file name="b.vb">
Public Class C
End Class
    </file>
</compilation>

            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
''' <summary>
''' See <see cref="C"/>.
''' </summary>
Public Class Test
End Class
]]>
    </file>
</compilation>


            Dim lib1Ref = CreateCompilationWithMscorlib(lib1Source).EmitToImageReference()
            Dim lib2Ref = CreateCompilationWithMscorlib(lib2Source).EmitToImageReference()

            Dim compilation = CreateCompilationWithMscorlibAndReferences(source, {lib1Ref, lib2Ref}, parseOptions:=s_optionsDiagnoseDocComments)
            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim crefSyntax = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).Single()

            ' Break: In dev11 the accessible symbol is preferred. We produce an ambiguity
            Dim symbolInfo = model.GetSymbolInfo(crefSyntax)
            Dim symbols = symbolInfo.CandidateSymbols
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason)
            Assert.Equal(2, symbols.Length)
            Assert.Equal("A", symbols(0).ContainingAssembly.Name)
            Assert.Equal("B", symbols(1).ContainingAssembly.Name)
        End Sub

        <WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")>
        <WorkItem(709199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709199")>
        <Fact>
        Public Sub ProtectedInstanceBaseMember()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
Class Base
    Protected F As Integer
End Class

''' Accessible: <see cref="Base.F"/>
Class Derived : Inherits Base
End Class

''' Inaccessible: <see cref="Base.F"/>
Class Other
End Class
]]>
    </file>
</compilation>


            Dim compilation = CreateCompilationWithMscorlib(source, parseOptions:=s_optionsDiagnoseDocComments)
            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim crefSyntax = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).First()

            Dim expectedSymbol = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Base").GetMember(Of FieldSymbol)("F")
            Dim actualSymbol = model.GetSymbolInfo(crefSyntax).Symbol
            Assert.Equal(expectedSymbol, actualSymbol)
        End Sub

        <WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")>
        <WorkItem(709199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709199")>
        <Fact>
        Public Sub ProtectedSharedBaseMember()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
Class Base
    Protected Shared F As Integer
End Class

''' Accessible: <see cref="Base.F"/>
Class Derived : Inherits Base
End Class

''' Inaccessible: <see cref="Base.F"/>
Class Other
End Class
]]>
    </file>
</compilation>


            Dim compilation = CreateCompilationWithMscorlib(source, parseOptions:=s_optionsDiagnoseDocComments)
            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim crefSyntax = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).First()

            Dim expectedSymbol = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Base").GetMember(Of FieldSymbol)("F")
            Dim actualSymbol = model.GetSymbolInfo(crefSyntax).Symbol
            Assert.Equal(expectedSymbol, actualSymbol)
        End Sub

        <WorkItem(768624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768624")>
        <Fact>
        Public Sub CrefsOnDelegate()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
''' <see cref="T"/>
''' <see cref="p"/>
''' <see cref="Invoke"/>
''' <see cref="ToString"/>
Delegate Sub D(Of T)(p As T)
]]>
    </file>
</compilation>


            Dim compilation = CreateCompilationWithMscorlib(source, parseOptions:=s_optionsDiagnoseDocComments)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'T' that could not be resolved.
''' <see cref="T"/>
         ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'p' that could not be resolved.
''' <see cref="p"/>
         ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'Invoke' that could not be resolved.
''' <see cref="Invoke"/>
         ~~~~~~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'ToString' that could not be resolved.
''' <see cref="ToString"/>
         ~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(768624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768624")>
        <Fact>
        Public Sub TypeParametersOfAssociatedSymbol()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
''' <see cref='T'/>
Class C(Of T)
    ''' <see cref='U'/>
    Sub M(Of U)()
    End Sub
End Class

''' <see cref='V'/>
Delegate Sub D(Of V)()
]]>
    </file>
</compilation>

            ' NOTE: Unlike C#, VB allows crefs to type parameters.
            Dim compilation = CreateCompilationWithMscorlib(source, parseOptions:=s_optionsDiagnoseDocComments)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42375: XML comment has a tag with a 'cref' attribute 'T' that bound to a type parameter.  Use the <typeparamref> tag instead.
''' <see cref='T'/>
         ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'U' that could not be resolved.
    ''' <see cref='U'/>
             ~~~~~~~~
BC42309: XML comment has a tag with a 'cref' attribute 'V' that could not be resolved.
''' <see cref='V'/>
         ~~~~~~~~
]]></errors>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim crefSyntaxes = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).ToArray()

            Dim [class] = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim method = [class].GetMember(Of MethodSymbol)("M")
            Dim [delegate] = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("D")

            Dim info0 As SymbolInfo = model.GetSymbolInfo(crefSyntaxes(0))
            Assert.Null(info0.Symbol) ' As in dev11.
            Assert.Equal([class].TypeParameters.Single(), info0.CandidateSymbols.Single())
            Assert.Equal(CandidateReason.NotReferencable, info0.CandidateReason)
            Assert.True(model.GetSymbolInfo(crefSyntaxes(1)).IsEmpty)
            Assert.True(model.GetSymbolInfo(crefSyntaxes(2)).IsEmpty)
        End Sub

        <WorkItem(768624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768624")>
        <Fact>
        Public Sub MembersOfAssociatedSymbol()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
''' <see cref='F'/>
Class C
    Private F As Integer
End Class

''' <see cref='F'/>
Structure S
    Private F As Integer
End Structure

''' <see cref='P'/>
Interface I
    Property P As Integer
End Interface

''' <see cref='F'/>
Module M
    Private F As Integer
End Module

''' <see cref='F'/>
Enum E
    F
End Enum
]]>
    </file>
</compilation>

            ' None of these work in dev11.
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=s_optionsDiagnoseDocComments)
            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim crefSyntaxes = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).ToArray()

            Assert.Equal("C.F As System.Int32", model.GetSymbolInfo(crefSyntaxes(0)).Symbol.ToTestDisplayString())
            Assert.Equal("S.F As System.Int32", model.GetSymbolInfo(crefSyntaxes(1)).Symbol.ToTestDisplayString())
            Assert.Equal("Property I.P As System.Int32", model.GetSymbolInfo(crefSyntaxes(2)).Symbol.ToTestDisplayString())
            Assert.Equal("M.F As System.Int32", model.GetSymbolInfo(crefSyntaxes(3)).Symbol.ToTestDisplayString())
            Assert.Equal("E.F", model.GetSymbolInfo(crefSyntaxes(4)).Symbol.ToTestDisplayString())
        End Sub

        <WorkItem(768624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768624")>
        <Fact>
        Public Sub InnerVersusOuter()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
Class Outer
    Private F As Integer

    ''' <see cref='F'/>
    Class Inner
        Private F As Integer
    End Class
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=s_optionsDiagnoseDocComments)
            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim crefSyntax = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).Single()

            ' BREAK: In dev11, it refers to Outer.F.
            Assert.Equal("Outer.Inner.F As System.Int32", model.GetSymbolInfo(crefSyntax).Symbol.ToTestDisplayString())
        End Sub

        <WorkItem(531505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531505")>
        <Fact>
        Private Sub Pia()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
''' <see cref='FooStruct'/>
''' <see cref='FooStruct.NET'/>
Public Class C
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:C">
 <see cref='T:FooStruct'/>
 <see cref='F:FooStruct.NET'/>
</member>
</members>
</doc>
]]>
</xml>
            Dim reference1 = TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(False)
            Dim reference2 = TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(True)

            Dim comp1 = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc, additionalRefs:={reference1})
            Dim comp2 = CompileCheckDiagnosticsAndXmlDocument(xmlSource, <errors></errors>, xmlDoc, additionalRefs:={reference2})

            Dim validator As Action(Of ModuleSymbol) =
                Sub(m As ModuleSymbol)
                    DirectCast(m, PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()

                    ' No reference added.
                    AssertEx.None(m.GetReferencedAssemblies(), Function(id) id.Name.Contains("GeneralPia"))

                    ' No type embedded.
                    Assert.Equal(0, m.GlobalNamespace.GetMembers("FooStruct").Length)
                End Sub

            CompileAndVerify(comp1, symbolValidator:=validator)
            CompileAndVerify(comp2, symbolValidator:=validator)
        End Sub

        <WorkItem(790978, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/790978")>
        <Fact>
        Public Sub SingleSymbol()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
''' <summary>
''' summary information
''' </summary>
''' <remarks>nothing</remarks>
Public Class C
End Class

]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=s_optionsDiagnoseDocComments)
            comp.VerifyDiagnostics()

            Dim expectedXmlText = <![CDATA[
<member name="T:C">
 <summary>
 summary information
 </summary>
 <remarks>nothing</remarks>
</member>
]]>.Value.Replace(vbLf, vbCrLf).Trim

            Dim sourceSymbol = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.Equal(expectedXmlText, sourceSymbol.GetDocumentationCommentXml())

            Dim metadataRef = comp.EmitToImageReference()
            Dim comp2 = CreateCompilationWithReferences(<source/>, {metadataRef})

            Dim metadataSymbol = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.Equal(expectedXmlText, metadataSymbol.GetDocumentationCommentXml())
        End Sub

        <Fact, WorkItem(908893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908893")>
        Private Sub GenericTypeWithinGenericType()
            Dim xmlSource =
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Imports System

Public Class ClazzA(Of A)
    ''' <see cref="Test"/>
    Public Class ClazzB(Of B)
        Public Sub Test(x as ClazzB(Of B))
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>

            Dim xmlDoc =
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
AssemblyName
</name>
</assembly>
<members>
<member name="T:ClazzA`1.ClazzB`1">
 <see cref="M:ClazzA`1.ClazzB`1.Test(ClazzA{`0}.ClazzB{`1})"/>
</member>
</members>
</doc>
]]>
</xml>

            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(xmlSource,
<errors>
</errors>,
xmlDoc)
        End Sub

#Region "Helpers"

        Private Structure AliasInfo
            Public ReadOnly Name As String
            Public ReadOnly Target As String

            Public Sub New(name As String, target As String)
                Me.Name = name
                Me.Target = target
            End Sub
        End Structure

        Private Sub CheckAllAliases(model As SemanticModel, cref As CrefReferenceSyntax, ParamArray expected As AliasInfo())
            Dim names = SyntaxNodeFinder.FindNodes(Of IdentifierNameSyntax)(cref, SyntaxKind.IdentifierName)
            Assert.Equal(expected.Length, names.Count)

            For i = 0 To names.Count - 1
                Dim e = expected(i)
                Dim sym = names(i)

                Assert.Equal(e.Name, sym.ToString().Trim())

                Dim actual = model.GetAliasInfo(sym)

                If e.Target Is Nothing Then
                    Assert.Null(actual)
                Else
                    Assert.Equal(e.Target, actual.Target.ToDisplayString)
                End If
            Next
        End Sub

        Private Class CrefFinder
            Public Shared Function FindCref(tree As SyntaxTree) As CrefReferenceSyntax
                Dim crefs = SyntaxNodeFinder.FindNodes(Of CrefReferenceSyntax)(tree.GetRoot(), SyntaxKind.CrefReference)
                Return If(crefs.Count > 0, crefs(0), Nothing)
            End Function

            Public Shared Function FindAllCrefs(tree As SyntaxTree) As List(Of CrefReferenceSyntax)
                Return SyntaxNodeFinder.FindNodes(Of CrefReferenceSyntax)(tree.GetRoot(), SyntaxKind.CrefReference)
            End Function
        End Class

        Private Shared Function StringReplace(obj As Object, what As String, [with] As String) As Object
            Dim str = TryCast(obj, String)
            Return If(str Is Nothing, obj, str.Replace(what, [with]))
        End Function

        Private Shared Function AsXmlCommentText(file As TempFile) As String
            Return TestHelpers.AsXmlCommentText(file.ToString())
        End Function

        Private Function FormatSourceXml(xml As XElement, ParamArray obj() As Object) As XElement
            For Each file In xml.<file>
                file.Value = String.Format(file.Value, obj)
            Next
            Return xml
        End Function

        Private Function FormatXmlSimple(xml As XElement, ParamArray obj() As Object) As XElement
            xml.Value = String.Format(xml.Value, obj)
            Return xml
        End Function

        Friend Function FilterOfSymbolKindOnly(symbols As ImmutableArray(Of ISymbol), ParamArray kinds() As SymbolKind) As ImmutableArray(Of ISymbol)
            Dim filter As New HashSet(Of SymbolKind)(kinds)
            Return (From s In symbols
                    Where filter.Contains(s.Kind)
                    Select s).AsImmutable()
        End Function

        Friend Sub AssertLookupResult(actual As ImmutableArray(Of ISymbol), ParamArray expected() As String)
            AssertStringArraysEqual(expected, (From s In actual Select s.ToTestDisplayString()).ToArray)
        End Sub

        Friend Function CheckSymbolInfoOnly(model As SemanticModel, syntax As ExpressionSyntax, ParamArray expected() As String) As ImmutableArray(Of ISymbol)
            EnsureSymbolInfoOnCrefReference(model, syntax)

            Dim actual = model.GetSymbolInfo(syntax)

            If expected.Length = 0 Then
                Assert.True(actual.IsEmpty)
            ElseIf expected.Length = 1 Then
                Assert.NotNull(actual.Symbol)
                Assert.Equal(expected(0), actual.Symbol.ToTestDisplayString)
            Else
                Assert.Equal(CandidateReason.Ambiguous, actual.CandidateReason)
                AssertStringArraysEqual(expected, (From s In actual.CandidateSymbols Select s.ToTestDisplayString()).ToArray)
            End If

            Dim typeInfo = model.GetTypeInfo(syntax)
            If actual.Symbol IsNot Nothing AndAlso actual.Symbol.Kind = SymbolKind.TypeParameter Then ' Works everywhere since we want it to work in name attributes.
                Assert.Equal(actual.Symbol, typeInfo.Type)
            Else
                Assert.Null(typeInfo.Type)
            End If

            Return actual.GetAllSymbols()

        End Function

        Private Function GetEnclosingCrefReference(syntax As ExpressionSyntax) As CrefReferenceSyntax
            Dim node As VisualBasicSyntaxNode = syntax
            While node IsNot Nothing AndAlso node.Kind <> SyntaxKind.CrefReference
                node = node.Parent
            End While
            Return DirectCast(node, CrefReferenceSyntax)
        End Function

        Private Sub EnsureSymbolInfoOnCrefReference(model As SemanticModel, syntax As ExpressionSyntax)
            Dim cref = GetEnclosingCrefReference(syntax)
            If cref Is Nothing Then
                Return
            End If

            Debug.Assert(cref.Signature IsNot Nothing OrElse cref.AsClause Is Nothing)
            If cref.Signature IsNot Nothing Then
                Return
            End If

            Dim fromName = model.GetSymbolInfo(cref.Name)
            Dim fromCref = model.GetSymbolInfo(cref)

            Assert.Equal(fromCref.CandidateReason, fromName.CandidateReason)

            AssertStringArraysEqual((From s In fromName.GetAllSymbols() Select s.ToTestDisplayString()).ToArray,
                                    (From s In fromCref.GetAllSymbols() Select s.ToTestDisplayString()).ToArray)

        End Sub

        Friend Function CheckTypeParameterCrefSymbolInfoAndTypeInfo(model As SemanticModel, syntax As ExpressionSyntax, Optional expected As String = Nothing) As ImmutableArray(Of Symbol)
            EnsureSymbolInfoOnCrefReference(model, syntax)

            Dim actual = model.GetSymbolInfo(syntax)
            Dim typeInfo = model.GetTypeInfo(syntax)

            If expected Is Nothing Then
                Assert.True(actual.IsEmpty)
                Assert.Null(typeInfo.Type)
                Return ImmutableArray.Create(Of Symbol)()
            Else
                Assert.Equal(CandidateReason.NotReferencable, actual.CandidateReason)
                Dim symbol = actual.CandidateSymbols.Single()
                Assert.NotNull(symbol)
                Assert.Equal(expected, symbol.ToTestDisplayString)

                Assert.NotNull(typeInfo.Type)
                Assert.Equal(typeInfo.Type, symbol)

                Return ImmutableArray.Create(DirectCast(symbol, Symbol))
            End If
        End Function

        Friend Function CheckSymbolInfoAndTypeInfo(model As SemanticModel, syntax As ExpressionSyntax, ParamArray expected() As String) As ImmutableArray(Of Symbol)
            EnsureSymbolInfoOnCrefReference(model, syntax)

            Dim actual = model.GetSymbolInfo(syntax)
            Dim typeInfo = model.GetTypeInfo(syntax)

            If expected.Length = 0 Then
                Assert.True(actual.IsEmpty)
                Assert.Null(typeInfo.Type)
                Return ImmutableArray.Create(Of Symbol)()
            ElseIf expected.Length = 1 Then
                Assert.NotNull(actual.Symbol)
                Assert.Equal(expected(0), actual.Symbol.ToTestDisplayString)

                Assert.NotNull(typeInfo.Type)
                Assert.Equal(typeInfo.Type, actual.Symbol)

                Return ImmutableArray.Create(Of Symbol)(DirectCast(actual.Symbol, Symbol))
            Else
                Assert.Equal(CandidateReason.Ambiguous, actual.CandidateReason)
                AssertStringArraysEqual(expected, (From s In actual.CandidateSymbols Select s.ToTestDisplayString()).ToArray)
                Assert.Null(typeInfo.Type)
                Return actual.CandidateSymbols.Cast(Of Symbol).ToImmutableArray()
            End If
        End Function

        Friend Sub AssertStringArraysEqual(a() As String, b() As String)
            Assert.NotNull(a)
            Assert.NotNull(b)
            Assert.Equal(StringArraysToSortedString(a), StringArraysToSortedString(b))
        End Sub

        Friend Function StringArraysToSortedString(a() As String) As String
            Dim builder As New StringBuilder
            Array.Sort(a)
            For Each s In a
                builder.AppendLine(s)
            Next
            Return builder.ToString()
        End Function

        Friend Sub TestSymbolAndTypeInfoForType(model As SemanticModel, syntax As TypeSyntax, expected As ISymbol)
            EnsureSymbolInfoOnCrefReference(model, syntax)

            Dim expSymInfo = model.GetSymbolInfo(syntax)
            Assert.NotNull(expSymInfo.Symbol)
            Assert.Same(expected, expSymInfo.Symbol.OriginalDefinition)
            Dim expTypeInfo = model.GetTypeInfo(syntax)
            Assert.Equal(expected, expTypeInfo.Type.OriginalDefinition)
            Dim conversion = model.GetConversion(syntax)
            Assert.Equal(ConversionKind.Identity, conversion.Kind)
        End Sub

        Friend Shared Function FindNodesOfTypeFromText(Of TNode As VisualBasicSyntaxNode)(tree As SyntaxTree, textToFind As String) As TNode()
            Dim text As String = tree.GetText().ToString()
            Dim list As New List(Of TNode)

            Dim position As Integer = text.IndexOf(textToFind, StringComparison.Ordinal)
            While position >= 0
                Dim token As SyntaxToken = tree.GetRoot().FindToken(position, True)
                If token.ValueText = textToFind Then
                    Dim node = TryCast(token.Parent, TNode)
                    If node IsNot Nothing Then
                        list.Add(node)
                    End If
                End If
                position = text.IndexOf(textToFind, position + 1, StringComparison.Ordinal)
            End While

            Return list.ToArray()
        End Function

        Friend Shared Function CompileCheckDiagnosticsAndXmlDocument(
            sources As XElement,
            errors As XElement,
            Optional expectedDocXml As XElement = Nothing,
            Optional withDiagnostics As Boolean = True,
            Optional stringMapper As Func(Of Object, Object) = Nothing,
            Optional additionalRefs As MetadataReference() = Nothing,
            Optional ensureEnglishUICulture As Boolean = False
        ) As VisualBasicCompilation

            Dim parseOptions As VisualBasicParseOptions =
                VisualBasicParseOptions.Default.WithDocumentationMode(
                    If(withDiagnostics,
                       DocumentationMode.Diagnose,
                       DocumentationMode.Parse))

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(sources,
                                                                        additionalRefs,
                                                                        TestOptions.ReleaseDll.WithXmlReferenceResolver(XmlFileResolver.Default),
                                                                        parseOptions)
            If errors IsNot Nothing Then
                Dim diagnostics As Diagnostic()
                Dim saveUICulture As Globalization.CultureInfo = Nothing

                If ensureEnglishUICulture Then
                    Dim preferred = Roslyn.Test.Utilities.EnsureEnglishUICulture.PreferredOrNull

                    If preferred Is Nothing Then
                        ensureEnglishUICulture = False
                    Else
                        saveUICulture = Threading.Thread.CurrentThread.CurrentUICulture
                        Threading.Thread.CurrentThread.CurrentUICulture = preferred
                    End If
                End If

                Try
                    diagnostics = compilation.GetDiagnostics(CompilationStage.Compile).ToArray()
                Finally
                    If ensureEnglishUICulture Then
                        Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
                    End If
                End Try

                If stringMapper IsNot Nothing Then
                    For i = 0 To diagnostics.Count - 1
                        Dim info = DirectCast(diagnostics(i), DiagnosticWithInfo).Info
                        info = If(info.Arguments Is Nothing, ErrorFactory.ErrorInfo(CType(info.Code, ERRID)), ErrorFactory.ErrorInfo(CType(info.Code, ERRID), (From a In info.Arguments Select stringMapper(a)).ToArray()))
                        diagnostics(i) = New VBDiagnostic(info, NoLocation.Singleton)
                    Next
                End If

                CompilationUtils.AssertTheseDiagnostics(diagnostics.AsImmutable(), errors)
            End If

            If expectedDocXml IsNot Nothing Then
                CheckXmlDocument(compilation, expectedDocXml, stringMapper, ensureEnglishUICulture)
            End If
            Return compilation
        End Function

        Private Shared Sub CheckXmlDocument(
            compilation As VisualBasicCompilation,
            expectedDocXml As XElement,
            Optional stringMapper As Func(Of Object, Object) = Nothing,
            Optional ensureEnglishUICulture As Boolean = False
        )
            Assert.NotNull(expectedDocXml)

            Using output = New MemoryStream()
                Using xml = New MemoryStream()
                    Dim emitResult As CodeAnalysis.Emit.EmitResult
                    Dim saveUICulture As Globalization.CultureInfo = Nothing

                    If ensureEnglishUICulture Then
                        Dim preferred = Roslyn.Test.Utilities.EnsureEnglishUICulture.PreferredOrNull

                        If preferred Is Nothing Then
                            ensureEnglishUICulture = False
                        Else
                            saveUICulture = Threading.Thread.CurrentThread.CurrentUICulture
                            Threading.Thread.CurrentThread.CurrentUICulture = preferred
                        End If
                    End If

                    Try
                        emitResult = compilation.Emit(output, xmlDocumentationStream:=xml)
                    Finally
                        If ensureEnglishUICulture Then
                            Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
                        End If
                    End Try

                    xml.Seek(0, SeekOrigin.Begin)
                    Dim xmlDoc = New StreamReader(xml).ReadToEnd().Trim()

                    If stringMapper IsNot Nothing Then
                        xmlDoc = CStr(stringMapper(xmlDoc))
                    End If

                    Assert.Equal(expectedDocXml.Value.Replace(vbLf, vbCrLf).Trim(), xmlDoc)
                End Using
            End Using
        End Sub

#End Region

        <WorkItem(1087447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087447"), WorkItem(436, "CodePlex")>
        <Fact>
        Public Sub Bug1087447_01()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
''' <summary>
''' <see cref="C(Of Integer).f()"/>
''' </summary>
Class C(Of T)
    Sub f()
    End Sub
End Class

]]>
    </file>
</compilation>,
<error><![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'C(Of Integer).f()' that could not be resolved.
''' <see cref="C(Of Integer).f()"/>
         ~~~~~~~~~~~~~~~~~~~~~~~~
]]></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:C`1">
 <summary>
 <see cref="!:C(Of Integer).f()"/>
 </summary>
</member>
</members>
</doc>
]]>
</xml>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node1 = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "f").Single()

            Dim symbolInfo1 = model.GetSymbolInfo(node1.Parent)

            Assert.Equal("Sub C(Of ?).f()", symbolInfo1.Symbol.ToTestDisplayString())

            Dim node = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of TypeSyntax)().Where(Function(n) n.ToString() = "Integer").Single()

            Dim symbolInfo = model.GetSymbolInfo(node)

            Assert.Equal("?", symbolInfo.Symbol.ToTestDisplayString())

        End Sub

        <WorkItem(1087447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087447"), WorkItem(436, "CodePlex")>
        <Fact>
        Public Sub Bug1087447_02()
            Dim compilation = CompileCheckDiagnosticsAndXmlDocument(
<compilation name="EmptyCref">
    <file name="a.vb">
        <![CDATA[
''' <summary>
''' <see cref="C(Of System.Int32).f()"/>
''' </summary>
Class C(Of T)
    Sub f()
    End Sub
End Class

]]>
    </file>
</compilation>,
<error><![CDATA[
BC42309: XML comment has a tag with a 'cref' attribute 'C(Of System.Int32).f()' that could not be resolved.
''' <see cref="C(Of System.Int32).f()"/>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></error>,
<xml>
    <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
EmptyCref
</name>
</assembly>
<members>
<member name="T:C`1">
 <summary>
 <see cref="!:C(Of System.Int32).f()"/>
 </summary>
</member>
</members>
</doc>
]]>
</xml>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node1 = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "f").Single()

            Dim symbolInfo1 = model.GetSymbolInfo(node1.Parent)

            Assert.Equal("Sub C(Of ?).f()", symbolInfo1.Symbol.ToTestDisplayString())

            Dim node = tree.GetRoot().DescendantNodes(descendIntoTrivia:=True).OfType(Of TypeSyntax)().Where(Function(n) n.ToString() = "System.Int32").Single()

            Dim symbolInfo = model.GetSymbolInfo(node)

            Assert.Equal("?", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(1115058, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115058")>
        Public Sub UnterminatedElement()
            Dim sources =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1
    '''<summary>
    ''' Something
    '''<summary>
    Sub Main()
        System.Console.WriteLine("Here")
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                sources,
                options:=TestOptions.ReleaseExe,
                parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose))

            ' Compilation should succeed with warnings
            AssertTheseDiagnostics(CompileAndVerify(compilation, expectedOutput:="Here").Diagnostics, <![CDATA[
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    '''<summary>
       ~~~~~~~~~
BC42304: XML documentation parse error: Element is missing an end tag. XML comment will be ignored.
    '''<summary>
       ~~~~~~~~~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    '''<summary>
                ~
BC42304: XML documentation parse error: '>' expected. XML comment will be ignored.
    '''<summary>
                ~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    '''<summary>
                ~
BC42304: XML documentation parse error: Expected beginning '<' for an XML tag. XML comment will be ignored.
    '''<summary>
                ~
]]>)
        End Sub

        ''' <summary>
        ''' "--" is not valid within an XML comment.
        ''' </summary>
        <WorkItem(8807, "https://github.com/dotnet/roslyn/issues/8807")>
        <Fact>
        Public Sub IncludeErrorDashDashInName()
            Dim dir = Temp.CreateDirectory()
            Dim path = dir.Path
            Dim xmlFile = dir.CreateFile("---.xml").WriteAllText("<summary attrib="""" attrib=""""/>")
            Dim source =
<compilation name="DashDash">
    <file name="a.vb">
        <![CDATA[
''' <include file='{0}' path='//param'/>
Class C
End Class
]]>
    </file>
</compilation>
            CompileCheckDiagnosticsAndXmlDocument(FormatSourceXml(source, System.IO.Path.Combine(path, "---.xml")),
    <error/>,
    <xml>
        <![CDATA[
<?xml version="1.0"?>
<doc>
<assembly>
<name>
DashDash
</name>
</assembly>
<members>
<member name="T:C">
 <!--warning BC42320: Unable to include XML fragment '//param' of file '**FILE**'.-->
</member>
</members>
</doc>
]]>
    </xml>,
                stringMapper:=Function(o) StringReplace(o, System.IO.Path.Combine(TestHelpers.AsXmlCommentText(path), "- - -.xml"), "**FILE**"), ensureEnglishUICulture:=True)
        End Sub

    End Class
End Namespace
