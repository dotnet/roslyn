' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class EscapingTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeTypeWhenRenamingToKeyword(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module {|Escape:$$Goo|}
    Sub Blah()
        {|stmt1:Goo|}.Blah()
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("stmt1", "[If]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub DoNotEscapeMethodAfterDotWhenRenamingToKeyword(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Goo
    Sub {|Escape:$$Blah|}()
        Goo.{|stmt1:Blah|}()
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "If", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeAttributeWhenRenamingToRegularKeyword(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeAttributeUsageWhenRenamingToAssembly(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Assembly")

                result.AssertLabeledSpecialSpansAre("Escape", "[Assembly]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeAttributeUsageWhenRenamingToModule(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Module")

                result.AssertLabeledSpecialSpansAre("Escape", "[Module]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingMethodToNew(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="New")

                result.AssertLabeledSpecialSpansAre("Escape", "[New]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("stmt1", "[New]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingMethodToRem(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Goo
    Sub {|Escape:$$Blah|}()
        Goo.{|stmt1:Blah|}()
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Rem")

                result.AssertLabeledSpecialSpansAre("Escape", "[Rem]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("stmt1", "[Rem]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542104")>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingPropertyToMid(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                               Module M
                                   Sub Main
                                       {|stmt1:Goo|}(1) = 1
                                   End Sub
                                
                                   WriteOnly Property [|$$Goo|](ParamArray x As Integer()) As Integer
                                       Set(ByVal value As Integer)
                                       End Set
                                   End Property
                               End Module
                           </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Mid")

                result.AssertLabeledSpecialSpansAre("stmt1", "[Mid]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542104")>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingPropertyToStrangelyCasedMid(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                       Module M
                                           Sub Main
                                               {|stmt1:Goo|}(1) = 1
                                           End Sub
                                        
                                           WriteOnly Property [|$$Goo|](ParamArray x As Integer()) As Integer
                                               Set(ByVal value As Integer)
                                               End Set
                                           End Property
                                       End Module
                                   </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="mId")

                result.AssertLabeledSpecialSpansAre("stmt1", "[mId]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542166")>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingToMidWithTypeCharacters1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                   Module M
                                       Sub Main
                                           {|escaped:Goo|}(1) = ""
                                       End Sub
                                    
                                       WriteOnly Property {|stmt:$$Goo$|}(x%)
                                           Set(ByVal value$)
                                           End Set
                                       End Property
                                   End Module
                               </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Mid")

                result.AssertLabeledSpansAre("escaped", "[Mid]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "Mid$", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542166")>
        <CombinatorialData>
        Public Sub EscapeWhenRenamingToMidWithTypeCharacters2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                   Module M
                                       Sub Main
                                           {|unresolved:Goo|}(1) = ""
                                       End Sub
                                    
                                       WriteOnly Property {|unresolved2:$$Goo$|}(x%)
                                           Set(ByVal value$)
                                           End Set
                                       End Property
                                   End Module
                               </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Mid$")

                result.AssertLabeledSpansAre("unresolved", "Mid$", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolved2", "Mid$$", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EscapePreserveKeywordWhenRenamingWithRedim(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="preserve")

                result.AssertLabeledSpansAre("stmt1", "preserve", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "[preserve]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub EscapeRemKeywordWhenDoingTypeNameQualification(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M
    Sub Main
        Dim [Rem]
        {|Resolve:Goo|}
    End Sub
 
    Sub {|Escape:Goo$$|}
    End Sub
End Module
                                   </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Rem")

                result.AssertLabeledSpecialSpansAre("Escape", "[Rem]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Resolve", "M.[Rem]", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub EscapeNewKeywordWhenDoingTypeNameQualification(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M
    Sub Main
        Dim [New]
        {|Resolve:Goo|}
    End Sub
 
    Sub {|Escape:Goo$$|}
    End Sub
End Module
                           </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="New")

                result.AssertLabeledSpecialSpansAre("Escape", "[New]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Resolve", "M.[New]", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub EscapeRemKeywordWhenDoingMeQualification(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class M
    Sub Main()
        Dim [Rem]
        {|Replacement:Goo|}()
    End Sub
 
    Sub {|Escaped:$$Goo|}()
    End Sub
End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Rem")

                result.AssertLabeledSpecialSpansAre("Escaped", "[Rem]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Replacement", "Me.[Rem]()", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
        Public Sub DoNotEscapeIfKeywordWhenDoingMeQualification(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class M
    Sub Main
        Dim [If]
        {|Resolve:Goo|}
    End Sub
 
    Sub {|Escape:Goo$$|}
    End Sub
End Class
                               </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Escape", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Resolve", "Me.If", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529935")>
        <CombinatorialData>
        Public Sub EscapeIdentifierWhenRenamingToRemKeyword(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="ＲＥＭ")

                result.AssertLabeledSpansAre("stmt", "[ＲＥＭ]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529935")>
        <CombinatorialData>
        Public Sub EscapeIdentifierWhenRenamingToRemKeyword2(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="ＲＥＭ")

                result.AssertLabeledSpansAre("stmt", "[ＲＥＭ]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", "ＲＥＭ$", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529938")>
        <CombinatorialData>
        Public Sub RenamingToEscapedIdentifierWithFullwidthSquareBracket(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="[String］")

                result.AssertLabeledSpansAre("First", "[String]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529932")>
        <CombinatorialData>
        Public Sub EscapeContextualKeywordsInQuery1(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Group")

                result.AssertLabeledSpecialSpansAre("escaped", "[Group]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530805")>
        <CombinatorialData>
        Public Sub EscapeMidIfNeeded(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Module M
    Sub Main()
        {|stmt1:Goo|}()
    End Sub
    Sub [|$$Goo|]() ' Rename Goo to Mid
    End Sub
End Module
                               ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Mid")

                result.AssertLabeledSpecialSpansAre("stmt1", "[Mid]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607067")>
        <CombinatorialData>
        Public Sub RenamingToRemAndUsingTypeCharactersIsNotAllowed(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Rem")

                result.AssertLabeledSpansAre("typechar", "Rem%", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("escaped", "[Rem]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        Public Sub RenameIdentifierBracketed(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameToIdentifierBracketed(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="[y]")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameToIdentifierBracketed_2(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="[C]")

                result.AssertLabeledSpansAre("decl", "C", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "C", RelatedLocationType.NoConflict)
            End Using
        End Sub

    End Class
End Namespace
