' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.MoveToTopOfFile
    Public Class MoveToTopOfFileTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New MoveToTopOfFileCodeFixProvider())
        End Function

#Region "Imports Tests"

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub TestImportsMissing()
            TestMissing(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n [|Imports Microsoft|] \n Module Program \n Sub Main(args As String()) \n  \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub ImportsInsideDeclaration()
            Test(
NewLines("Module Program \n [|Imports System|] \n Sub Main(args As String()) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub ImportsAfterDeclarations()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n End Sub \n End Module \n [|Imports System|]"),
NewLines("Imports System Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub ImportsMovedNextToOtherImports()
            Dim text = <File>
Imports Microsoft

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Imports System|]</File>

            Dim expected = <File>
Imports Microsoft
Imports System

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub ImportsMovedAfterOptions()
            Dim text = <File>
Option Explicit Off

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Imports System|]</File>

            Dim expected = <File>
Option Explicit Off
Imports System

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub ImportsWithTriviaMovedNextToOtherImports()
            Dim text = <File>
Imports Microsoft

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Imports System|] 'Comment</File>

            Dim expected = <File>
Imports Microsoft
Imports System 'Comment

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub ImportsWithTriviaMovedNextToOtherImportsWithTrivia()
            Dim text = <File>
Imports Microsoft 'C1

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Imports System|] 'Comment</File>

            Dim expected = <File>
Imports Microsoft 'C1
Imports System 'Comment

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <WorkItem(601222)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub OnlyMoveOptions()
            Dim text = <File>
Imports Sys = System
Option Infer Off
[|Imports System.IO|]</File>

            TestMissing(text.ConvertTestSourceTag())
        End Sub
#End Region

#Region "Option Tests"
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub TestOptionsMissing()
            TestMissing(
NewLines("[|Option Explicit Off|] \n Module Program \n Sub Main(args As String()) \n  \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub OptionsInsideDeclaration()
            Test(
NewLines("Module Program \n [|Option Explicit Off|] \n Sub Main(args As String()) \n End Sub \n End Module"),
NewLines("Option Explicit Off \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub OptionsAfterDeclarations()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n End Sub \n End Module \n [|Option Explicit Off|]"),
NewLines("Option Explicit Off \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub OptionsMovedNextToOtherOptions()
            Dim text = <File>
Option Explicit Off

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Compare Text|]</File>

            Dim expected = <File>
Option Explicit Off
Option Compare Text

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub OptionsWithTriviaMovedNextToOtherOptions()
            Dim text = <File>
Imports Microsoft

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Imports System|] 'Comment</File>

            Dim expected = <File>
Imports Microsoft
Imports System 'Comment

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub OptionsWithTriviaMovedNextToOtherOptionsWithTrivia()
            Dim text = <File>
Option Explicit Off'C1

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Compare Binary|] 'Comment</File>

            Dim expected = <File>
Option Explicit Off'C1
Option Compare Binary 'Comment

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub
#End Region

#Region "Attribute Tests"
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub AttributeNoAction1()
            Dim text = <File>
[|&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;|]
Imports Microsoft

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            TestMissing(text.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub AttributeNoAction2()
            Dim text = <File>
[|&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;|]

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            TestMissing(text.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub AttributeAfterDeclaration()
            Dim text = <File>
Module Program
    Sub Main(args As String())

    End Sub
End Module
&lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt;
</File>

            Dim expected = <File>
&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;                               
Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>
            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub AttributeInsideDeclaration()
            Dim text = <File>
Module Program
    Sub Main(args As String())

    End Sub
    &lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt;
End Module 
</File>

            Dim expected = <File>
&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;
Module Program
    Sub Main(args As String())
    End Sub
End Module
</File>
            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub AttributePreserveTrivia()
            Dim text = <File>
&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt; 'Comment
Module Program
    Sub Main(args As String())

    End Sub
    &lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt; 'Another Comment
End Module 
</File>

            Dim expected = <File>
&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt; 'Comment
&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt; 'Another Comment
Module Program
    Sub Main(args As String())
    End Sub
End Module
</File>
            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Sub

        <WorkItem(600949)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub RemoveAttribute()
            Dim text = <File>
Class C
    &lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt;
End Class
</File>

            Dim expected = <File>
Class C
End Class
</File>
            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=1)
        End Sub

        <WorkItem(606857)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub MoveImportBeforeAttribute()
            Dim text = <File>
&lt;Assembly:CLSCompliant(True)&gt;
[|Imports System|]</File>

            Dim expected = <File>
[|Imports System|]
&lt;Assembly:CLSCompliant(True)&gt;</File>
            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=0)
        End Sub

        <WorkItem(606877)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub NewLineWhenMovingFromEOF()
            Dim text = <File>Imports System
&lt;Assembly:CLSCompliant(True)&gt;
[|Option Strict On|]</File>

            Dim expected = <File>Option Strict On
Imports System
&lt;Assembly:CLSCompliant(True)&gt;
</File>
            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=0)
        End Sub

        <WorkItem(606851)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Sub DoNotMoveLeadingWhitespace()
            Dim text = <File>Imports System
 
[|Option Strict On|]
</File>

            Dim expected = <File>Option Strict On
Imports System
 
</File>
            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=0)
        End Sub
#End Region

        <WorkItem(632305)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestHiddenRegion()
            Dim code =
<File>
#ExternalSource ("Foo", 1)
    Imports System
#End ExternalSource

Class C
    [|Imports Microsoft|]
End Class
</File>

            TestMissing(code)
        End Sub

    End Class
End Namespace
