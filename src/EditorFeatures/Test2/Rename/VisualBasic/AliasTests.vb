' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class AliasTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleSpecialTypeAliasVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Goo = System.Int32
                            Class C
                                Sub Main(args As String())
                                    Dim [|$$x|] As Goo = 23
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleSpecialTypeDoubleAliasVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Goo = System.Int32
                            Imports Bar = System.Int32
                            Class C
                                Sub Main(args As String())
                                    Dim [|$$x|] As Bar = 23
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleTypeAliasVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Goo = C

                            Class C
                                Public Sub Goo()
                                    Dim [|$$x|] As Goo = Nothing
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameAliasNoConflict(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports [|Goo|] = C3

                            Namespace N1
                                Class C1
                                    Public Sub Goo()
                                        Dim f As {|stmt1:$$Goo|} = Nothing
                                        Dim c As C1 = Nothing
                                    End Sub
                                End Class
                            End Namespace

                            Public Class C3

                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="C1")

                result.AssertLabeledSpansAre("stmt1", "Dim f As C3 = Nothing", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameAliasToSameNameNoConflict(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports [|Goo|] = N1.C1

                            Namespace N1
                                Class C1
                                    Public Sub Goo()
                                        Dim f As [|$$Goo|] = Nothing
                                        Dim c As C1 = Nothing
                                    End Sub
                                End Class
                            End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="C1")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586743")>
        <CombinatorialData>
        Public Sub RenameOneDuplicateAliasToNoConflict(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Goo = System.Int32
                            Imports [|Bar|] = System.Int32

                            Class C1
                                Public Sub Goo()
                                    Dim f As Goo = 1
                                    Dim b As [|$$Bar|] = 2
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541393")>
        Public Sub RenameNamespaceAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports System
                            Imports System.Collections.Generic
                            Imports [|$$A1|] = System.Linq
                            
                            Module Program
                                Sub Main(args As String())
                                    [|A1|].Enumerable.Empty(Of String)()
                                End Sub
                            End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545614")>
        Public Sub RenameConstructedTypeAliasFromUse(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports [|alias1|] = cls1(Of Integer)
Class cls1(Of T)
End Class
 
Module Module1
    Sub Main()
        Dim a1 As New [|$$alias1|]()
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545614")>
        Public Sub RenameConstructedTypeAliasFromDeclaration(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports [|$$alias1|] = cls1(Of Integer)
Class cls1(Of T)
End Class
 
Module Module1
    Sub Main()
        Dim a1 As New [|alias1|]()
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545614")>
        Public Sub RenameSimpleTypeAliasFromUse(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports [|alias1|] = cls1
Class cls1
End Class
 
Module Module1
    Sub Main()
        Dim a1 As New [|$$alias1|]()
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545614")>
        Public Sub RenameSimpleTypeAliasFromDeclaration(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports [|$$alias1|] = cls1
Class cls1
End Class
 
Module Module1
    Sub Main()
        Dim a1 As New [|alias1|]()
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546084")>
        Public Sub ConflictWhenRenamingAliasToSameAsGlobalTypeName(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports {|Conflict:$$A|} = Something
 
Class Something
    Sub Something()
 
    End Sub
End Class
 
Class Program
    Dim a As {|Conflict:A|}
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Something")

                result.AssertLabeledSpansAre("Conflict", "Something", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633860")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632303")>
        <CombinatorialData>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttribute(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports [|$$GooAttribute|] = System.ObsoleteAttribute

<{|long:GooAttribute|}>
class C
End class

<{|short:Goo|}>
class D
end class

<{|long:GooAttribute|}()>
class B
end class

<{|short:Goo|}()]> 
class Program
end class
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short", "Bar", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633860")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632303")>
        <CombinatorialData>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttributeNoConflict1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports [|$$GooAttribute|] = System.ObsoleteAttribute
Imports Bar = System.ContextStaticAttribute

<{|long:GooAttribute|}>
class C
End class

<{|short:Goo|}>
class D
end class

<{|long:GooAttribute|}()>
class B
end class

<{|short:Goo|}()]> 
class Program
end class
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short", "Bar", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633860")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632303")>
        <CombinatorialData>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttributeWithConflict1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports [|$$GooAttribute|] = System.ObsoleteAttribute
Imports BarAttribute = System.ContextStaticAttribute

<{|long:GooAttribute|}>
class C
End class

<{|short:Goo|}>
class D
end class

<{|long:GooAttribute|}()>
class B
end class

<{|short:Goo|}()> 
class Program
end class
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("short", "Obsolete", type:=RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("long", "Obsolete", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

    End Class
End Namespace
