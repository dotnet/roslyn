' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    Public Class EscapingTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingToEscapedKeyword1()
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
                </Workspace>, renameTo:="@if")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingToEscapedKeyword2()
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
                </Workspace>, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub UseFullAttributeNameWhenShortNameIsKeyword()
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
                </Workspace>, renameTo:="ifAttribute")

                result.AssertLabeledSpecialSpansAre("resolved", "ifAttribute()", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeAttributeIfKeyword()
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
                </Workspace>, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(527603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527603")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DoNotStickTokensTogetherForRefParameter_1()
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
                </Workspace>, renameTo:="type")

                result.AssertLabeledSpansAre("escaped", "type", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(527603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527603")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DoNotStickTokensTogetherForRefParameter_2()
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
                </Workspace>, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierUnescapes()
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
                </Workspace>, renameTo:="b")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierUnescapes_2()
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
                </Workspace>, renameTo:="@b")

                result.AssertLabeledSpansAre("stmt1", "b", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierUnescapes_3()
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
                </Workspace>, renameTo:="@D")

                result.AssertLabeledSpansAre("decl", "D", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "D", RelatedLocationType.NoConflict)
            End Using
        End Sub

    End Class
End Namespace
