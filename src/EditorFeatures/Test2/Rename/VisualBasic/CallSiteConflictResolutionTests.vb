' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
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
        Return {|Replacement:Me.{|Resolved:Foo|}(1).{|Resolved:Foo|}(2)|}
    End Function
End Class

Module M
    <Extension()>
    Function [|$$Foo|](x As C, tag As Integer) As C
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
        Return {|Replacement:Me.{|Resolved:Foo|}(Of Integer)()|}
    End Function
End Class

Module M
    <Extension()>
    Function [|$$Foo|](Of T)(x As C) As C
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
        Return {|Replacement:Me.{|Resolved:Foo|}(42)|}
    End Function
End Class

Module M
    <Extension()>
    Function [|$$Foo|](Of T)(x As C, y As T) As C
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
