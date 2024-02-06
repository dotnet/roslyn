' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.MoveToTopOfFile
    Public Class MoveToTopOfFileTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New MoveToTopOfFileCodeFixProvider())
        End Function

#Region "Imports Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestTestImportsMissing() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
[|Imports Microsoft|]
Module Program
    Sub Main(args As String())

    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsInsideDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    [|Imports System|]
    Sub Main(args As String())
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestImportsAfterDeclarations() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
    End Sub
End Module
[|Imports System|]",
"Imports System
Module Program
    Sub Main(args As String())
    End Sub
End Module
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
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
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
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
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
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
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
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
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601222")>
        Public Async Function TestOnlyMoveOptions() As Task
            Dim text = <File>
Imports Sys = System
Option Infer Off
[|Imports System.IO|]</File>

            Await TestMissingInRegularAndScriptAsync(text.ConvertTestSourceTag())
        End Function
#End Region

#Region "Option Tests"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestTestOptionsMissing() As Task
            Await TestMissingInRegularAndScriptAsync(
"[|Option Explicit Off|]
Module Program
    Sub Main(args As String())

    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsInsideDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    [|Option Explicit Off|]
    Sub Main(args As String())
    End Sub
End Module",
"Option Explicit Off
Module Program
    Sub Main(args As String())
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsAfterDeclarations() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
    End Sub
End Module
[|Option Explicit Off|]",
"Option Explicit Off
Module Program
    Sub Main(args As String())
    End Sub
End Module
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
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
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestOptionsWithTriviaMovedNextToOtherOptions() As Task
            Dim text = <File>
Option Explicit Off

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Compare Binary|] 'Comment</File>

            Dim expected = <File>
Option Explicit Off
Option Compare Binary 'Comment

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
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
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7117")>
        Public Async Function TestOptionsMovedAfterBannerText() As Task
            Dim text = <File>
' Copyright

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Explicit Off|]</File>

            Dim expected = <File>
' Copyright
Option Explicit Off

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7117")>
        Public Async Function TestOptionsMovedAfterBannerTextThatFollowsEndOfLineTrivia() As Task
            Dim text = <File>

' Copyright

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Explicit Off|]</File>

            Dim expected = <File>

' Copyright
Option Explicit Off

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7117")>
        Public Async Function TestOptionsMovedAfterBannerTextFollowedByOtherOptions() As Task
            Dim text = <File>
' Copyright
Option Explicit Off

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Compare Binary|]</File>

            Dim expected = <File>
' Copyright
Option Explicit Off
Option Compare Binary

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7117")>
        Public Async Function TestOptionsMovedToTopWithLeadingTriviaButNoBannerText() As Task
            Dim text = <File>
#Const A = 5

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Compare Binary|]</File>

            Dim expected = <File>Option Compare Binary

#Const A = 5

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7117")>
        Public Async Function TestOptionsMovedAfterBannerTextWithImports() As Task
            Dim text = <File>

' Copyright
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())

    End Sub
End Module
[|Option Compare Binary|]</File>

            Dim expected = <File>

' Copyright
Option Compare Binary
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

#End Region

#Region "Attribute Tests"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeNoAction1() As Task
            Dim text = <File>
[|&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;|]
Imports Microsoft

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Await TestMissingInRegularAndScriptAsync(text.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeNoAction2() As Task
            Dim text = <File>
[|&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;|]

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Await TestMissingInRegularAndScriptAsync(text.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeAfterDeclaration() As Task
            Dim text = <File>
Module Program
    Sub Main(args As String())

    End Sub
End Module
&lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt;
</File>

            Dim expected = <File>&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        Public Async Function TestAttributeInsideDeclaration() As Task
            Dim text = <File>
Module Program
    Sub Main(args As String())

    End Sub
    &lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt;
End Module 
</File>

            Dim expected = <File>&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;

Module Program
    Sub Main(args As String())

    End Sub
End Module 
</File>
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
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
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7117")>
        Public Async Function TestAttributeMovedAfterBannerText() As Task
            Dim text = <File>
' Copyright
' License information.

Module Program
    Sub Main(args As String())

    End Sub
End Module
&lt;[|Assembly:|] Reflection.AssemblyCultureAttribute("de")&gt;
</File>

            Dim expected = <File>&lt;Assembly: Reflection.AssemblyCultureAttribute("de")&gt;

' Copyright
' License information.

Module Program
    Sub Main(args As String())

    End Sub
End Module
</File>
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/600949")>
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
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606857")>
        Public Async Function TestMoveImportBeforeAttribute() As Task
            Dim text = <File>
&lt;Assembly:CLSCompliant(True)&gt;
[|Imports System|]</File>

            Dim expected = <File>[|Imports System|]

&lt;Assembly:CLSCompliant(True)&gt;
</File>
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606877")>
        Public Async Function TestNewLineWhenMovingFromEOF() As Task
            Dim text = <File>Imports System
&lt;Assembly:CLSCompliant(True)&gt;
[|Option Strict On|]</File>

            Dim expected = <File>Option Strict On
Imports System
&lt;Assembly:CLSCompliant(True)&gt;
</File>
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToTopOfFile)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606851")>
        Public Async Function TestDoNotMoveLeadingWhitespace() As Task
            Dim text = <File>Imports System
 
[|Option Strict On|]
</File>

            Dim expected = <File>Option Strict On
Imports System
 
</File>
            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function
#End Region

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632305")>
        Public Async Function TestTestHiddenRegion() As Task
            Dim code =
<File>
#ExternalSource ("Goo", 1)
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
