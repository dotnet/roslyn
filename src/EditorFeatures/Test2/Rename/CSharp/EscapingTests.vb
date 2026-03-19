' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class EscapingTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingToEscapedKeyword1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class [|$$Goo|]
{
    [|Goo|] goo;
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="@if")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingToEscapedKeyword2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class {|escaped:$$Goo|}
{
    {|escaped:Goo|} goo;
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub UseFullAttributeNameWhenShortNameIsKeyword(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class C
{
    [{|resolved:Main|}()]
    static void Test() { }
}

class [|$$MainAttribute|] : System.Attribute
{
    static void Main() { }
}

                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="ifAttribute")

                result.AssertLabeledSpecialSpansAre("resolved", "ifAttribute()", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeAttributeIfKeyword(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class C
{
    [{|escaped:Main|}()]
    static void Test() { }
}

class {|escaped:$$MainAttribute|} : System.Attribute
{
    static void Main() { }
}

                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527603")>
        <CombinatorialData>
        Public Sub DoNotStickTokensTogetherForRefParameter_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class[|$$@class|]
                            {
                              static void Goo(ref{|escaped:@class|}@c) { }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="type")

                result.AssertLabeledSpansAre("escaped", "type", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527603")>
        <CombinatorialData>
        Public Sub DoNotStickTokensTogetherForRefParameter_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class{|escaped:$$@class|}
                            {
                              static void Goo(ref{|escaped:@class|}@c) { }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameEscapedIdentifierUnescapes(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class A
                            {
                                static void Goo() 
                                { 
                                    var [|$$@a|] = 12;
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="b")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameEscapedIdentifierUnescapes_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class A
                            {
                                static void Goo() 
                                { 
                                    var {|stmt1:$$@a|} = 12;
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="@b")

                result.AssertLabeledSpansAre("stmt1", "b", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameEscapedIdentifierUnescapes_3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class B
                            {
                                public class {|decl:C|}
                                {
                                }
                            }

                            class A
                            {
                                static void Goo() 
                                { 
                                    var x = new @B.{|stmt1:$$@C|}();
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="@D")

                result.AssertLabeledSpansAre("decl", "D", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "D", RelatedLocationType.NoConflict)
            End Using
        End Sub

    End Class
End Namespace
