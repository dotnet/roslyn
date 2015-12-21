' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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
        Public Async Function TestTestImportsMissing() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n [|Imports Microsoft|] \n Module Program \n Sub Main(args As String()) \n  \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsInsideDeclaration() As Task
            Await TestAsync(
NewLines("Module Program \n [|Imports System|] \n Sub Main(args As String()) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsAfterDeclarations() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n End Sub \n End Module \n [|Imports System|]"),
NewLines("Imports System Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsMovedNextToOtherImports() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsMovedAfterOptions() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsWithTriviaMovedNextToOtherImports() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsWithTriviaMovedNextToOtherImportsWithTrivia() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <WorkItem(601222)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOnlyMoveOptions() As Task
            Dim text = <File>
Imports Sys = System
Option Infer Off
[|Imports System.IO|]</File>

            Await TestMissingAsync(text.ConvertTestSourceTag())
        End Function
#End Region

#Region "Option Tests"
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestTestOptionsMissing() As Task
            Await TestMissingAsync(
NewLines("[|Option Explicit Off|] \n Module Program \n Sub Main(args As String()) \n  \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsInsideDeclaration() As Task
            Await TestAsync(
NewLines("Module Program \n [|Option Explicit Off|] \n Sub Main(args As String()) \n End Sub \n End Module"),
NewLines("Option Explicit Off \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsAfterDeclarations() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n End Sub \n End Module \n [|Option Explicit Off|]"),
NewLines("Option Explicit Off \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsMovedNextToOtherOptions() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsWithTriviaMovedNextToOtherOptions() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsWithTriviaMovedNextToOtherOptionsWithTrivia() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function
#End Region

#Region "Attribute Tests"
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeNoAction1() As Task
            Dim text = <File>
[|&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;|]
Imports Microsoft

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Await TestMissingAsync(text.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeNoAction2() As Task
            Dim text = <File>
[|&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;|]

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Await TestMissingAsync(text.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeAfterDeclaration() As Task
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
            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeInsideDeclaration() As Task
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
            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributePreserveTrivia() As Task
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
            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <WorkItem(600949)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestRemoveAttribute() As Task
            Dim text = <File>
Class C
    &lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt;
End Class
</File>

            Dim expected = <File>
Class C
End Class
</File>
            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=1)
        End Function

        <WorkItem(606857)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestMoveImportBeforeAttribute() As Task
            Dim text = <File>
&lt;Assembly:CLSCompliant(True)&gt;
[|Imports System|]</File>

            Dim expected = <File>
[|Imports System|]
&lt;Assembly:CLSCompliant(True)&gt;</File>
            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=0)
        End Function

        <WorkItem(606877)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestNewLineWhenMovingFromEOF() As Task
            Dim text = <File>Imports System
&lt;Assembly:CLSCompliant(True)&gt;
[|Option Strict On|]</File>

            Dim expected = <File>Option Strict On
Imports System
&lt;Assembly:CLSCompliant(True)&gt;
</File>
            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=0)
        End Function

        <WorkItem(606851)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestDoNotMoveLeadingWhitespace() As Task
            Dim text = <File>Imports System
 
[|Option Strict On|]
</File>

            Dim expected = <File>Option Strict On
Imports System
 
</File>
            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=0)
        End Function
#End Region

        <WorkItem(632305)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestTestHiddenRegion() As Task
            Dim code =
<File>
#ExternalSource ("Foo", 1)
    Imports System
#End ExternalSource

Class C
    [|Imports Microsoft|]
End Class
</File>

            Await TestMissingAsync(code)
        End Function

    End Class
End Namespace
