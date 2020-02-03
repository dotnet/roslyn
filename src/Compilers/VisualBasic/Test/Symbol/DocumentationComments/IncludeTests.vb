' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class IncludeTests
        <Theory>
        <InlineData("F", "F:Acme.Widget.F")>
        <InlineData(WellKnownMemberNames.StaticConstructorName, "M:Acme.Widget.#cctor")>
        <InlineData("E", "E:Acme.Widget.E")>
        <InlineData("P", "P:Acme.Widget.P")>
        <InlineData("M", "M:Acme.Widget.M")>
        <InlineData("NamedType", "T:Acme.Widget.NamedType")>
        Public Sub TestDocumentationCaching(symbolName As String, documentationId As String)
            Using New EnsureEnglishUICulture()
                Dim compilation = CreateCompilationWithMscorlib40(
                <compilation name="ConstructorDocumentationCommentTests">
                    <file name="a.vb">
                    Namespace Acme
                        Class Widget
                            ''' &lt;include file="NonExistent.xml" /&gt;
                            Dim F As Integer

                            ''' &lt;include file="NonExistent.xml" /&gt;
                            Shared Sub New()
                            End Sub

                            ''' &lt;include file="NonExistent.xml" /&gt;
                            Event E As EventHandler

                            ''' &lt;include file="NonExistent.xml" /&gt;
                            Property P As Integer

                            ''' &lt;include file="NonExistent.xml" /&gt;
                            Sub M()
                            End Sub

                            ''' &lt;include file="NonExistent.xml" /&gt;
                            Class NamedType
                            End Class
                        End Class
                    End Namespace
                    </file>
                </compilation>)

                Dim acmeNamespace = DirectCast(compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
                Dim widgetClass = acmeNamespace.GetTypeMembers("Widget").Single()

                Dim symbol = widgetClass.GetMembers(symbolName).Single()
                Assert.Equal(documentationId, symbol.GetDocumentationCommentId())
                Assert.Equal(
$"<member name=""{documentationId}"">
 <!--warning BC42310: XML comment tag 'include' must have a 'path' attribute. XML comment will be ignored.-->
</member>", symbol.GetDocumentationCommentXml(expandIncludes:=True))
                Assert.Equal(
$"<member name=""{documentationId}"">
 <include file=""NonExistent.xml"" />
</member>", symbol.GetDocumentationCommentXml(expandIncludes:=False))
                Assert.Equal(
$"<member name=""{documentationId}"">
 <!--warning BC42310: XML comment tag 'include' must have a 'path' attribute. XML comment will be ignored.-->
</member>", symbol.GetDocumentationCommentXml(expandIncludes:=True))
                Assert.Equal(
$"<member name=""{documentationId}"">
 <include file=""NonExistent.xml"" />
</member>", symbol.GetDocumentationCommentXml(expandIncludes:=False))
            End Using
        End Sub
    End Class
End Namespace
