// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Debugger;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.ImmediateWindow;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicDebuggerIntellisenseCommon : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicDebuggerIntellisenseCommon(VisualStudioInstanceFactory instanceFactory, string projectTemplate)
            : base(instanceFactory, nameof(BasicDebuggerIntellisenseCommon), projectTemplate)
        {
            SetUpEditor(@"$$Imports System

  Module Module1

    Sub Main()
      Dim objectAA As AA = New AA
      For i As Integer = 0 To 0
        Dim p As Integer = 89   
      Next
      For Each x As Integer In {3, 4, 5}
        Console.WriteLine(x)  
      Next

    End Sub

    Class AA
        Shared Sub foo(x As String)

        End Sub
        Shared Sub foo(y As Action(Of String))

        End Sub
    End Class
  End Module");
            this.SetBreakpoint("Dim p As Integer = 89");
            this.SetBreakpoint("Console.WriteLine(x)");
            this.StartDebugging(waitForBreakMode: true);
        }

        public virtual void StartDebuggingAndVerifyBreakpoints()
        {
            this.VerifyCaretPosition(134);
            this.ContinueExecution(waitForBreakMode: true);
            this.VerifyCaretPosition(218);
        }

        public virtual void CompletionOnFirstCharacter()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("A");
            this.VerifyCompletionItemExistsInImmediateWindow("AA");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void CompletionAfterDot()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("AA.");
            this.VerifyCompletionItemExistsInImmediateWindow("foo");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void CompletionAfterQuestionMark()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("?");
            this.VerifyCompletionItemExistsInImmediateWindow("AA");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void CompletionAfterOpenParenInMethodCall()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("AA.foo( ");
            this.VerifyCompletionItemExistsInImmediateWindow("AA");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void CompletionInAnExpression()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("?new ");
            this.VerifyCompletionItemExistsInImmediateWindow("AA");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void LocalsFromPreviousBlockAreNotVisibleInTheCurrentBlock()
        {
            this.ContinueExecution(waitForBreakMode: true);
            this.ContinueExecution(waitForBreakMode: true);
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("?");
            this.VerifyCompletionItemDoesNotExistInImmediateWindow("p");
            this.VerifyCompletionItemExistsInImmediateWindow("x");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }
    }
}
