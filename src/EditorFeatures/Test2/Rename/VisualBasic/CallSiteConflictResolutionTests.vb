﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    <[UseExportProvider]>
    Public Class CallSiteConflictResolutionTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542103")>
        <WorkItem(535068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535068")>
        Public Sub RewriteConflictingExtensionMethodCallSite()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports System.Runtime.CompilerServices

Class C
    Function Bar(tag As Integer) As C
        Return {|Replacement:Me.{|Resolved:Goo|}(1).{|Resolved:Goo|}(2)|}
    End Function
End Class

Module M
    <Extension()>
    Function [|$$Goo|](x As C, tag As Integer) As C
        Return New C()
    End Function
End Module
                            ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("Resolved", type:=RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Replacement", "M.Bar(M.Bar(Me, 1), 2)")
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542821")>
        <WorkItem(535068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535068")>
        Public Sub RewriteConflictingExtensionMethodCallSiteRequiringTypeArguments()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports System.Runtime.CompilerServices

Class C
    Function Bar(Of T)() As C
        Return {|Replacement:Me.{|Resolved:Goo|}(Of Integer)()|}
    End Function
End Class

Module M
    <Extension()>
    Function [|$$Goo|](Of T)(x As C) As C
        Return New C()
    End Function
End Module
                            ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("Resolved", type:=RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Replacement", "M.Bar(Of Integer)(Me)")
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542821")>
        <WorkItem(535068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535068")>
        Public Sub RewriteConflictingExtensionMethodCallSiteInferredTypeArguments()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports System.Runtime.CompilerServices

Class C
    Function Bar(Of T)(y As T) As C
        Return {|Replacement:Me.{|Resolved:Goo|}(42)|}
    End Function
End Class

Module M
    <Extension()>
    Function [|$$Goo|](Of T)(x As C, y As T) As C
        Return New C()
    End Function
End Module
                            ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("Resolved", type:=RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Replacement", "M.Bar(Me, 42)")
            End Using
        End Sub

        <Fact>
        <WorkItem(539636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539636")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub QualifyConflictingMethodInvocation()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub F()
    End Sub
End Module
Public Class C
    Sub [|$$M|]()
        {|stmt1:F|}()
    End Sub
End Class
                               </Document>
                    </Project>
                </Workspace>, renameTo:="F")


                result.AssertLabeledSpansAre("stmt1", "Program.F()", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub
    End Class
End Namespace
