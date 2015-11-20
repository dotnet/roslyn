' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class RegionDirectiveOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of RegionDirectiveTriviaSyntax)

        Friend Overrides Function GetRegions(node As RegionDirectiveTriviaSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.RegionDirectiveOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub FileHeader()
            Dim code =
<code><![CDATA[#Region "Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
' C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#End Region]]></code>

            Dim tree = ParseCode(code.Value)
            Dim trivia = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(0)
            Dim directiveTrivia = TryCast(trivia.GetStructure(), DirectiveTriviaSyntax)

            Assert.NotNull(directiveTrivia)

            Dim regionDirective = TryCast(directiveTrivia, RegionDirectiveTriviaSyntax)

            Assert.NotNull(regionDirective)

            Dim actualOutliningSpan = GetRegion(regionDirective)
            Dim expectedOutliningSpan = New OutliningSpan(
                TextSpan.FromBounds(0, 204),
                bannerText:="Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                autoCollapse:=True)

            AssertRegion(expectedOutliningSpan, actualOutliningSpan)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub EmptyFileHeader()
            Dim code =
<code><![CDATA[#Region ""
' C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#End Region]]></code>

            Dim tree = ParseCode(code.Value)
            Dim trivia = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(0)
            Dim directiveTrivia = TryCast(trivia.GetStructure(), DirectiveTriviaSyntax)

            Assert.NotNull(directiveTrivia)

            Dim regionDirective = TryCast(directiveTrivia, RegionDirectiveTriviaSyntax)

            Assert.NotNull(regionDirective)

            Dim actualOutliningSpan = GetRegion(regionDirective)
            Dim expectedOutliningSpan = New OutliningSpan(
                TextSpan.FromBounds(0, 120),
                bannerText:="#Region",
                autoCollapse:=True)

            AssertRegion(expectedOutliningSpan, actualOutliningSpan)
        End Sub

    End Class
End Namespace
