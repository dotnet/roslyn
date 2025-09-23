' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Copilot
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Copilot
    <UseExportProvider>
    Public NotInheritable Class RoslynProposalAdjusterTests
        Private Shared ReadOnly s_composition As TestComposition = FeaturesTestCompositions.Features

        Private Shared Sub AllSettingsOff(options As IGlobalOptionService)
            options.SetGlobalOption(CopilotOptions.FixAddMissingTokens, False)
            options.SetGlobalOption(CopilotOptions.FixAddMissingImports, False)
            options.SetGlobalOption(CopilotOptions.FixCodeFormat, False)
        End Sub

        Private Shared Async Function Test(
                code As String,
                expected As String,
                language As String,
                Optional setOptions As Action(Of IGlobalOptionService) = Nothing,
                Optional compilationOptions As CompilationOptions = Nothing) As Task
            Using workspace = If(language Is LanguageNames.CSharp,
                    EditorTestWorkspace.CreateCSharp(code, compilationOptions:=compilationOptions, composition:=s_composition),
                    EditorTestWorkspace.CreateVisualBasic(code, compilationOptions:=compilationOptions, composition:=s_composition))
                Dim documentId = workspace.Documents.First().Id
                Dim proposalSpans = workspace.Documents.First().SelectedSpans

                Dim sourceText = Await workspace.CurrentSolution.GetDocument(documentId).GetTextAsync()

                ' Get the original document without the proposal edit in it.
                Dim originalDocument = workspace.CurrentSolution.GetDocument(documentId).WithText(
                    sourceText.WithChanges(proposalSpans.Select(Function(s) New TextChange(s, newText:=""))))

                Dim changes = New List(Of TextChange)()
                Dim delta = 0
                For Each selectionSpan In proposalSpans
                    changes.Add(New TextChange(
                        New TextSpan(selectionSpan.Start + delta, 0), newText:=sourceText.ToString(selectionSpan)))

                    delta -= selectionSpan.Length
                Next

                ' Enable code fixer feature flags. They are off by default.
                Dim options = workspace.GetService(Of IGlobalOptionService)

                ' Default to all the flags on if a setter is not specified. 
                If setOptions Is Nothing Then
                    options.SetGlobalOption(CopilotOptions.FixAddMissingTokens, True)
                    options.SetGlobalOption(CopilotOptions.FixAddMissingImports, True)
                    options.SetGlobalOption(CopilotOptions.FixCodeFormat, True)
                Else
                    setOptions(options)
                End If

                Dim service = originalDocument.GetRequiredLanguageService(Of ICopilotProposalAdjusterService)
                Dim tuple = Await service.TryAdjustProposalAsync(
                    originalDocument, CopilotUtilities.TryNormalizeCopilotTextChanges(changes), CancellationToken.None)

                Dim adjustedChanges = tuple.TextChanges
                Dim format = tuple.Format
                Dim originalDocumentText = Await originalDocument.GetTextAsync()
                Dim adjustedDocumentTextAndFinalSpans = CopilotUtilities.GetNewTextAndChangedSpans(originalDocumentText, adjustedChanges)
                Dim adjustedDocumentText = adjustedDocumentTextAndFinalSpans.newText
                Dim finalSpans = adjustedDocumentTextAndFinalSpans.newSpans

                If format Then
                    Dim adjustedDocument = originalDocument.WithText(adjustedDocumentText)
                    Dim formattedDocument = Await Formatter.FormatAsync(adjustedDocument, finalSpans)
                    Dim formattedText = Await formattedDocument.GetTextAsync()
                    adjustedDocumentText = formattedText
                End If

                AssertEx.Equal(expected, adjustedDocumentText.ToString())
            End Using
        End Function

#Region "C#"

        Private Shared Async Function TestCSharp(code As String, expected As String, Optional setOptions As Action(Of IGlobalOptionService) = Nothing) As Task
            Await Test(code, expected, LanguageNames.CSharp, setOptions)
        End Function

        <WpfFact>
        Public Async Function TestCSharp1() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_ExistingUsingAfter() As Task
            Await TestCSharp("
using Test;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;
using Test;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_ExistingUsingBefore() As Task
            Await TestCSharp("
using System;

class C
{
    void M()
    {
        [|Task.Yield();|]
    }
}", "
using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Task.Yield();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_PartiallyWritten() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        Con[|sole.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AddMultiple_Different() As Task
            Await TestCSharp("
using System.Collections.Generic;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
        if (true) { }
        [|Task.Yield();|]
    }
}", "
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true) { }
        Task.Yield();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AddMultiple_Same() As Task
            Await TestCSharp("
using System.Collections.Generic;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
        if (true) { }
        [|Console.WriteLine();|]
    }
}", "
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true) { }
        Console.WriteLine();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBrace1() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console.WriteLine(1);|]
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBrace2() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console.WriteLine(1);|]
", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBrace3() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console.WriteLine(1);|]

    public void N() { }
}
", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_RequiresFormatting() As Task
            Await TestCSharp("
using System;

class C
{
    void M()
    {
            [| Console  .  WriteLine ( 1 )   ;|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_RequiresUsingAndFormatting() As Task
            Await TestCSharp("
class C
{
    void M()
    {
            [| Console  .  WriteLine ( 1 )   ;|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBraceUsingAndFormatting() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console  . WriteLine( 1 )  ;|]

    public void N() { }
}
", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBraceAndFormattingPlusWhiteSpaceAfter() As Task
            ' Note that the trailing whitespace after the proposal causes the AddMissingTokens fixer
            ' to not add the closing brace. This could be improved in the future.
            Await TestCSharp("
class C
{
    void M()
    [|{
        System.Console  . WriteLine( 1 )  ; |]

    public void N() { }
}
", "
class C
{
    void M()
    {
        System.Console.WriteLine(1);

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBraceAndFormatting() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        System.Console  . WriteLine( 1 )  ;|]

    public void N() { }
}
", "
class C
{
    void M()
    {
        System.Console.WriteLine(1);
    }

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_Multi_Line_Formatting() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [| if (false) {
System.Console  .  WriteLine ( 1 )   ; } |]
    }
}", "
class C
{
    void M()
    {
        if (false)
        {
            System.Console.WriteLine(1);
        }
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_Formatting_Outside_Proposal() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [| Console  .  WriteLine ( 1 )   ;|]
            if (    true    ) {
            [| Console  .  WriteLine ( 1 )   ;|]
            }
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true)
        {
            Console.WriteLine(1);
        }
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_Partial_Formatting() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [| System . Console  .  Writ|]
    }
}", "
class C
{
    void M()
    {
        System.Console.Writ
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AnalyzersOff() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", AddressOf AllSettingsOff)
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingImportsOnly() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", Sub(options)
        options.SetGlobalOption(CopilotOptions.FixAddMissingImports, True)
    End Sub)
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingTokensOnly() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", Sub(options)
        options.SetGlobalOption(CopilotOptions.FixAddMissingTokens, True)
    End Sub)
        End Function

        <WpfFact>
        Public Async Function TestCSharp_FormatOnly() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", Sub(options)
        options.SetGlobalOption(CopilotOptions.FixCodeFormat, True)
    End Sub)
        End Function

#End Region

#Region "Visual Basic"

        Private Shared Async Function TestVisualBasic(code As String, expected As String, Optional setOptions As Action(Of IGlobalOptionService) = Nothing) As Task
            Await Test(code, expected, LanguageNames.VisualBasic, setOptions, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic1() As Task
            Await TestVisualBasic("
class C
    sub M()
        [|Console.WriteLine(1)|]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_ExistingImportsAfter() As Task
            Await TestVisualBasic("
Imports Test

class C
    sub M()
        [|Console.WriteLine(1)|]
    end sub
end class", "
Imports System
Imports Test

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_ExistingImportsBefore() As Task
            Await TestVisualBasic("
Imports System

class C
    sub M()
        [|Task.Yield()|]
    end sub
end class", "
Imports System
Imports System.Threading.Tasks

class C
    sub M()
        Task.Yield()
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_PartiallyWritten() As Task
            Await TestVisualBasic("
class C
    sub M()
        Con[|sole.WriteLine(1)|]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_AddMultiple_Different() As Task
            Await TestVisualBasic("
Imports System.Collections.Generic

class C
    sub M()
        [|Console.WriteLine(1)|]
        if (true)
        end if
        [|Task.Yield()|]
    end sub
end class", "
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

class C
    sub M()
        Console.WriteLine(1)
        if (true)
        end if
        Task.Yield()
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_AddMultiple_Same() As Task
            Await TestVisualBasic("
Imports System.Collections.Generic

class C
    sub M()
        [|Console.WriteLine(1)|]
        if (true)
        end if
        [|Console.WriteLine()|]
    end sub
end class", "
Imports System
Imports System.Collections.Generic

class C
    sub M()
        Console.WriteLine(1)
        if (true)
        end if
        Console.WriteLine()
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_RequiresFormatting() As Task
            Await TestVisualBasic("
Imports System

class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_RequiresUsingAndFormatting() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_Multi_Line_Formatting() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
        if (false)
        end if
        [| Console . WriteLine ( 1 )   |]
    end sub
end class
", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
        if (false)
        end if
        Console.WriteLine(1)
    end sub
end class
")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_Formatting_Outside_Proposal() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console  .  WriteLine ( 1 )  |]
            if (    true    )
            [| Console  .  WriteLine ( 1 )   |]
            end if
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
        if (true)
            Console.WriteLine(1)
        end if
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_Partial_Formatting() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| System .  Console  .  Writ|]
    end sub
end class", "
class C
    sub M()
        System.Console.Writ
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_AnalyzersOff() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
    end sub
end class", "
class C
    sub M()
         Console . WriteLine ( 1 )   
    end sub
end class", AddressOf AllSettingsOff)
        End Function

#End Region
    End Class
End Namespace
