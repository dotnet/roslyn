' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    Public Class EscapingTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeTypeWhenRenamingToKeyword()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module {|Escape:$$Foo|}
    Sub Blah()
        {|stmt1:Foo|}.Blah()
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("stmt1", "[If]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DoNotEscapeMethodAfterDotWhenRenamingToKeyword()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Foo
    Sub {|Escape:$$Blah|}()
        Foo.{|stmt1:Blah|}()
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="If")


                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "If", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeAttributeWhenRenamingToRegularKeyword()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports System

Class {|Escape:$$C|}
    Inherits Attribute
End Class

<{|Escape:C|}>
Module Program
    Sub Main()
    End Sub
End Module
                               ]]></Document>
                    </Project>
                </Workspace>, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeAttributeUsageWhenRenamingToAssembly()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports System

Class [|$$C|]
    Inherits Attribute
End Class

<{|Escape:C|}>
Module Program
    Sub Main()
    End Sub
End Module
                               ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Assembly")


                result.AssertLabeledSpecialSpansAre("Escape", "[Assembly]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeAttributeUsageWhenRenamingToModule()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports System

Class {|Escape:$$C|}
    Inherits Attribute
End Class

<{|Escape:C|}>
Module Program
    Sub Main()
    End Sub
End Module
                               ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Module")


                result.AssertLabeledSpecialSpansAre("Escape", "[Module]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingMethodToNew()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class Derived
    Inherits Base

    Sub New()
        MyBase.{|stmt1:Blah|}()
    End Sub
End Class

Class Base
    Sub {|Escape:$$Blah|}()

    End Sub
End Class
                           </Document>
                    </Project>
                </Workspace>, renameTo:="New")

                result.AssertLabeledSpecialSpansAre("Escape", "[New]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("stmt1", "[New]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingMethodToRem()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Foo
    Sub {|Escape:$$Blah|}()
        Foo.{|stmt1:Blah|}()
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="Rem")


                result.AssertLabeledSpecialSpansAre("Escape", "[Rem]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("stmt1", "[Rem]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542104")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingPropertyToMid()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                               Module M
                                   Sub Main
                                       {|stmt1:Foo|}(1) = 1
                                   End Sub
                                
                                   WriteOnly Property [|$$Foo|](ParamArray x As Integer()) As Integer
                                       Set(ByVal value As Integer)
                                       End Set
                                   End Property
                               End Module
                           </Document>
                    </Project>
                </Workspace>, renameTo:="Mid")


                result.AssertLabeledSpecialSpansAre("stmt1", "[Mid]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542104")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingPropertyToStrangelyCasedMid()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                       Module M
                                           Sub Main
                                               {|stmt1:Foo|}(1) = 1
                                           End Sub
                                        
                                           WriteOnly Property [|$$Foo|](ParamArray x As Integer()) As Integer
                                               Set(ByVal value As Integer)
                                               End Set
                                           End Property
                                       End Module
                                   </Document>
                    </Project>
                </Workspace>, renameTo:="mId")


                result.AssertLabeledSpecialSpansAre("stmt1", "[mId]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542166")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingToMidWithTypeCharacters1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                   Module M
                                       Sub Main
                                           {|escaped:Foo|}(1) = ""
                                       End Sub
                                    
                                       WriteOnly Property {|stmt:$$Foo$|}(x%)
                                           Set(ByVal value$)
                                           End Set
                                       End Property
                                   End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="Mid")

                result.AssertLabeledSpansAre("escaped", "[Mid]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "Mid$", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542166")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeWhenRenamingToMidWithTypeCharacters2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                   Module M
                                       Sub Main
                                           {|unresolved:Foo|}(1) = ""
                                       End Sub
                                    
                                       WriteOnly Property {|unresolved2:$$Foo$|}(x%)
                                           Set(ByVal value$)
                                           End Set
                                       End Property
                                   End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="Mid$")

                result.AssertLabeledSpansAre("unresolved", "Mid$", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolved2", "Mid$$", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapePreserveKeywordWhenRenamingWithRedim()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                        Module M
                                            Sub Main()
                                                Dim {|stmt1:$$x|}
                                                ReDim {|stmt2:x|}(1)
                                            End Sub
                                        End Module
                                   </Document>
                    </Project>
                </Workspace>, renameTo:="preserve")

                result.AssertLabeledSpansAre("stmt1", "preserve", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "[preserve]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub EscapeRemKeywordWhenDoingTypeNameQualification()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M
    Sub Main
        Dim [Rem]
        {|Resolve:Foo|}
    End Sub
 
    Sub {|Escape:Foo$$|}
    End Sub
End Module
                                   </Document>
                    </Project>
                </Workspace>, renameTo:="Rem")

                result.AssertLabeledSpecialSpansAre("Escape", "[Rem]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Resolve", "M.[Rem]", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub EscapeNewKeywordWhenDoingTypeNameQualification()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M
    Sub Main
        Dim [New]
        {|Resolve:Foo|}
    End Sub
 
    Sub {|Escape:Foo$$|}
    End Sub
End Module
                           </Document>
                    </Project>
                </Workspace>, renameTo:="New")

                result.AssertLabeledSpecialSpansAre("Escape", "[New]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Resolve", "M.[New]", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub EscapeRemKeywordWhenDoingMeQualification()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class M
    Sub Main()
        Dim [Rem]
        {|Replacement:Foo|}()
    End Sub
 
    Sub {|Escaped:$$Foo|}()
    End Sub
End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Rem")

                result.AssertLabeledSpecialSpansAre("Escaped", "[Rem]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Replacement", "Me.[Rem]()", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub DoNotEscapeIfKeywordWhenDoingMeQualification()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class M
    Sub Main
        Dim [If]
        {|Resolve:Foo|}
    End Sub
 
    Sub {|Escape:Foo$$|}
    End Sub
End Class
                               </Document>
                    </Project>
                </Workspace>, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Resolve", "Me.If", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(529935, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529935")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeIdentifierWhenRenamingToRemKeyword()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()
        Dim {|stmt:$$x|} As String ' Rename x to [ＲＥＭ]
        Dim y = {|stmt:x|}
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="ＲＥＭ")

                result.AssertLabeledSpansAre("stmt", "[ＲＥＭ]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529935, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529935")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeIdentifierWhenRenamingToRemKeyword2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()
        Dim {|stmt:$$x|} As String ' Rename x to [ＲＥＭ]
        Dim y = {|conflict:x$|}
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="ＲＥＭ")

                result.AssertLabeledSpansAre("stmt", "[ＲＥＭ]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", "ＲＥＭ$", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(529938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529938")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingToEscapedIdentifierWithFullwidthSquareBracket()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()
        Dim {|First:$$x|} As String ' Rename x to [String］
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="[String］")

                result.AssertLabeledSpansAre("First", "[String]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529932")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeContextualKeywordsInQuery1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Imports System
Imports System.Linq
Imports System.Collections.Generic

Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Class Or AttributeTargets.Assembly)>
    Public Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace

Class Program
    Private Shared Sub Main(args As String())
        Dim q = From x In new List(of char)() Group By x Into {|escaped:ExtensionMethod|}
    End Sub
End Class

Module MyExtension
    <System.Runtime.CompilerServices.Extension()> _
    Public Function [|$$ExtensionMethod|](x As IEnumerable(Of Char)) As Integer
        Return 23
    End Function
End Module
                               ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Group")


                result.AssertLabeledSpecialSpansAre("escaped", "[Group]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(530805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530805")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EscapeMidIfNeeded()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Module M
    Sub Main()
        {|stmt1:Foo|}()
    End Sub
    Sub [|$$Foo|]() ' Rename Foo to Mid
    End Sub
End Module
                               ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Mid")


                result.AssertLabeledSpecialSpansAre("stmt1", "[Mid]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(607067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607067")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingToRemAndUsingTypeCharactersIsNotAllowed()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Module Program
    Sub Main()
        Dim {|typechar:x%|} = 1
        Console.WriteLine({|escaped:$$x|})
    End Sub
End Module
                               ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Rem")

                result.AssertLabeledSpansAre("typechar", "Rem%", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("escaped", "[Rem]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameIdentifierBracketed()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Module Program
    Sub Main()
        Dim {|Stmt2:[x]|} = 1
        {|Stmt1:$$x|} = 2 ' Rename x to y here
    End Sub
End Module
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameToIdentifierBracketed()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Module Program
    Sub Main()
        Dim {|Stmt2:[x]|} = 1
        {|Stmt1:$$x|} = 2 ' Rename x to y here
    End Sub
End Module
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="[y]")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameToIdentifierBracketed_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Class A
    Class {|decl:$$B|}
    End Class
End Class

Module Program
    Sub Main()
        Dim x = new [A].{|stmt1:[B]|}
    End Sub
End Module
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="[C]")

                result.AssertLabeledSpansAre("decl", "C", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "C", RelatedLocationType.NoConflict)
            End Using
        End Sub

    End Class
End Namespace
