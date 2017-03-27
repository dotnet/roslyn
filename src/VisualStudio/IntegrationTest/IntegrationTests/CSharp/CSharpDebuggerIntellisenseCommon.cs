// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Debugger;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.ImmediateWindow;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    public class CSharpDebuggerIntellisenseCommon : AbstractEditorTest
    {
        public CSharpDebuggerIntellisenseCommon(VisualStudioInstanceFactory instanceFactory, string projectTemplate)
            : base(instanceFactory, nameof(CSharpDebuggerIntellisenseCommon), projectTemplate)
        {
            SetUpEditor(@"$$using System;
      namespace ConsoleApplication1
      {
          class Program
          {
              static void Main(string[] args)
              {
                  AA objectAA = new AA();
                  for (int i = 0; i < 1; i++)
                  {
                      int p = 60;
                  }

                  foreach (var item in new int[]{3,4,5})
                  {
                      Console.WriteLine(item);
                  }
              }
          }

      class AA
      {
          public static void foo(string x)
          { }
      }
      }");
            this.SetBreakpoint("AA objectAA = new AA()");
            this.SetBreakpoint("int p = 60");
            this.SetBreakpoint("Console.WriteLine(item)");
            this.StartDebugging(waitForBreakMode: true);
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public virtual void StartDebuggingAndVerifyBreakpoints()
        {
            this.VerifyCaretPosition(181);
            this.ContinueExecution(waitForBreakMode: true);
            this.VerifyCaretPosition(296);
            this.ContinueExecution(waitForBreakMode: true);
            this.VerifyCaretPosition(433);
        }

        public virtual void DoNotCrashOnSemicolon()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("int abc=9;");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
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
            this.SendKeysToImmediateWindow("?A");
            this.VerifyCompletionItemExistsInImmediateWindow("AA");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void CompletionAfterOpenParenInMethodCall()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("AA.foo{(} a");
            this.VerifyCompletionItemExistsInImmediateWindow("AA");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void CompletionInAnExpression()
        {
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("new a");
            this.VerifyCompletionItemExistsInImmediateWindow("AA");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }

        public virtual void LocalsFromPreviousBlockAreNotVisibleInTheCurrentBlock()
        {
            this.ContinueExecution(waitForBreakMode: true);
            this.ContinueExecution(waitForBreakMode: true);
            this.ShowImmediateWindow();
            this.ClearImmediateWindow();
            this.SendKeysToImmediateWindow("?i");
            this.VerifyCompletionItemDoesNotExistInImmediateWindow("i");
            this.VerifyCompletionItemExistsInImmediateWindow("item");
            this.SendKeysToImmediateWindow(VirtualKey.Enter);
        }
    }
}
