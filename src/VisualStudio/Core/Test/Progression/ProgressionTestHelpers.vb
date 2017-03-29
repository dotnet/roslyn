' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.CSharp.Progression
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Progression
Imports <xmlns="http://schemas.microsoft.com/vs/2009/dgml">

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Friend Module ProgressionTestHelpers
        Public ReadOnly CompositionCatalog As ComposableCatalog =
            TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(
                GetType(CSharpProgressionLanguageService),
                GetType(VisualBasicProgressionLanguageService))

        <Extension>
        Public Function ToSimplifiedXDocument(graph As Graph) As XDocument
            Dim document = XDocument.Parse(graph.ToXml(graphNodeIdAliasThreshold:=1000000))

            document.Root.<Categories>.Remove()
            document.Root.<Properties>.Remove()
            document.Root.<QualifiedNames>.Remove()

            For Each node In document.Descendants(XName.Get("Node", "http://schemas.microsoft.com/vs/2009/dgml"))
                Dim attribute = node.Attribute("SourceLocation")
                If attribute IsNot Nothing Then
                    attribute.Remove()
                End If
            Next

            Return document
        End Function

        Public Sub AssertSimplifiedGraphIs(graph As Graph, xml As XElement)
            Dim graphXml = graph.ToSimplifiedXDocument()
            If Not XNode.DeepEquals(graphXml.Root, xml) Then
                ' They aren't equal, so therefore the text representations definitely aren't equal.
                ' We'll Assert.Equal those, so that way xunit will show nice before/after text
                'Assert.Equal(xml.ToString(), graphXml.ToString())

                ' In an attempt to diagnose some flaky tests, the whole contents of both objects will be output
                Throw New Exception($"Graph XML was not equal, check for out-of-order elements.
Expected:
{xml.ToString()}

Actual:
{graphXml.ToString()}
")
            End If
        End Sub
    End Module
End Namespace
