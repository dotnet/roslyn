' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class EscapingTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingToEscapedKeyword1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class [|$$Foo|]
{
    [|Foo|] foo;
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="@if")

            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingToEscapedKeyword2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class {|escaped:$$Foo|}
{
    {|escaped:Foo|} foo;
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub UseFullAttributeNameWhenShortNameIsKeyword()
            Using result = RenameEngineResult.Create(
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

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeAttributeIfKeyword()
            Using result = RenameEngineResult.Create(
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

        <WorkItem(527603)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DoNotStickTokensTogetherForRefParameter_1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class[|$$@class|]
                            {
                              static void Foo(ref{|escaped:@class|}@c) { }
                            }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="type")

                result.AssertLabeledSpansAre("escaped", "type", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(527603)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DoNotStickTokensTogetherForRefParameter_2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class{|escaped:$$@class|}
                            {
                              static void Foo(ref{|escaped:@class|}@c) { }
                            }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("escaped", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierUnescapes()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class A
                            {
                                static void Foo() 
                                { 
                                    var [|$$@a|] = 12;
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="b")

            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierUnescapes_2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class A
                            {
                                static void Foo() 
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

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierUnescapes_3()
            Using result = RenameEngineResult.Create(
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
                                static void Foo() 
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
