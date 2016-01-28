' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    Public Class AliasTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleSpecialTypeAliasVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Foo = System.Int32
                            Class C
                                Sub Main(args As String())
                                    Dim [|$$x|] As Foo = 23
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleSpecialTypeDoubleAliasVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Foo = System.Int32
                            Imports Bar = System.Int32
                            Class C
                                Sub Main(args As String())
                                    Dim [|$$x|] As Bar = 23
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleTypeAliasVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Foo = C

                            Class C
                                Public Sub Foo()
                                    Dim [|$$x|] As Foo = Nothing
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasNoConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports [|Foo|] = C3

                            Namespace N1
                                Class C1
                                    Public Sub Foo()
                                        Dim f As {|stmt1:$$Foo|} = Nothing
                                        Dim c As C1 = Nothing
                                    End Sub
                                End Class
                            End Namespace

                            Public Class C3

                            End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="C1")

                result.AssertLabeledSpansAre("stmt1", "Dim f As C3 = Nothing", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToSameNameNoConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports [|Foo|] = N1.C1

                            Namespace N1
                                Class C1
                                    Public Sub Foo()
                                        Dim f As [|$$Foo|] = Nothing
                                        Dim c As C1 = Nothing
                                    End Sub
                                End Class
                            End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="C1")

            End Using
        End Sub

        <Fact>
        <WorkItem(586743)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOneDuplicateAliasToNoConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports Foo = System.Int32
                            Imports [|Bar|] = System.Int32

                            Class C1
                                Public Sub Foo()
                                    Dim f As Foo = 1
                                    Dim b As [|$$Bar|] = 2
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(541393)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespaceAlias()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(545614)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConstructedTypeAliasFromUse()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(545614)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConstructedTypeAliasFromDeclaration()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub


        <WorkItem(545614)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleTypeAliasFromUse()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(545614)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleTypeAliasFromDeclaration()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="BarBaz")
            End Using
        End Sub

        <WorkItem(546084)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictWhenRenamingAliasToSameAsGlobalTypeName()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="Something")

                result.AssertLabeledSpansAre("Conflict", "Something", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(633860)>
        <WorkItem(632303)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttribute()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports [|$$FooAttribute|] = System.ObsoleteAttribute

<{|long:FooAttribute|}>
class C
End class

<{|short:Foo|}>
class D
end class

<{|long:FooAttribute|}()>
class B
end class

<{|short:Foo|}()]> 
class Program
end class
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short", "Bar", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(633860)>
        <WorkItem(632303)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttributeNoConflict1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports [|$$FooAttribute|] = System.ObsoleteAttribute
Imports Bar = System.ContextStaticAttribute

<{|long:FooAttribute|}>
class C
End class

<{|short:Foo|}>
class D
end class

<{|long:FooAttribute|}()>
class B
end class

<{|short:Foo|}()]> 
class Program
end class
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short", "Bar", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(633860)>
        <WorkItem(632303)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttributeWithConflict1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports [|$$FooAttribute|] = System.ObsoleteAttribute
Imports BarAttribute = System.ContextStaticAttribute

<{|long:FooAttribute|}>
class C
End class

<{|short:Foo|}>
class D
end class

<{|long:FooAttribute|}()>
class B
end class

<{|short:Foo|}()> 
class Program
end class
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("short", "Obsolete", type:=RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("long", "Obsolete", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

    End Class
End Namespace
