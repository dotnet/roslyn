// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    public abstract class AbstractContextTests
    {
        protected abstract void CheckResult(bool validLocation, int position, SyntaxTree syntaxTree);

        private void VerifyWorker(string markup, bool validLocation, CSharpParseOptions options = null)
        {
            MarkupTestFile.GetPosition(markup, out var code, out int position);

            VerifyAtPosition(code, position, validLocation, options: options);
            VerifyInFrontOfComment(code, position, validLocation, options: options);
            VerifyAtEndOfFile(code, position, validLocation, options: options);
            VerifyAtPosition_TypePartiallyWritten(code, position, validLocation, options: options);
            VerifyInFrontOfComment_TypePartiallyWritten(code, position, validLocation, options: options);
            VerifyAtEndOfFile_TypePartiallyWritten(code, position, validLocation, options: options);
        }

        private void VerifyInFrontOfComment(
            string text,
            int position,
            bool validLocation,
            string insertText,
            CSharpParseOptions options)
        {
            text = text.Substring(0, position) + insertText + "/**/" + text.Substring(position);

            position += insertText.Length;

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: options);

            CheckResult(validLocation, position, tree);
        }

        private void VerifyInFrontOfComment(string text, int position, bool validLocation, CSharpParseOptions options)
        {
            VerifyInFrontOfComment(text, position, validLocation, string.Empty, options: options);
        }

        private void VerifyInFrontOfComment_TypePartiallyWritten(string text, int position, bool validLocation, CSharpParseOptions options)
        {
            VerifyInFrontOfComment(text, position, validLocation, "Str", options: options);
        }

        private void VerifyAtPosition(
            string text,
            int position,
            bool validLocation,
            string insertText,
            CSharpParseOptions options)
        {
            text = text.Substring(0, position) + insertText + text.Substring(position);

            position += insertText.Length;

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: options);
            CheckResult(validLocation, position, tree);
        }

        private void VerifyAtPosition(string text, int position, bool validLocation, CSharpParseOptions options)
        {
            VerifyAtPosition(text, position, validLocation, string.Empty, options: options);
        }

        private void VerifyAtPosition_TypePartiallyWritten(string text, int position, bool validLocation, CSharpParseOptions options)
        {
            VerifyAtPosition(text, position, validLocation, "Str", options: options);
        }

        private void VerifyAtEndOfFile(
            string text,
            int position,
            bool validLocation,
            string insertText,
            CSharpParseOptions options)
        {
            // only do this if the placeholder was at the end of the text.
            if (text.Length != position)
            {
                return;
            }

            text = text.Substring(startIndex: 0, length: position) + insertText;

            position += insertText.Length;

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: options);
            CheckResult(validLocation, position, tree);
        }

        private void VerifyAtEndOfFile(string text, int position, bool validLocation, CSharpParseOptions options)
        {
            VerifyAtEndOfFile(text, position, validLocation, string.Empty, options: options);
        }

        private void VerifyAtEndOfFile_TypePartiallyWritten(string text, int position, bool validLocation, CSharpParseOptions options)
        {
            VerifyAtEndOfFile(text, position, validLocation, "Str", options: options);
        }

        protected void VerifyTrue(string text)
        {
            // run the verification in both context(normal and script)
            VerifyWorker(text, validLocation: true);
            VerifyWorker(text, validLocation: true, options: Options.Script);
        }

        protected void VerifyOnlyInScript(string text)
        {
            // run the verification in both context(normal and script)
            VerifyWorker(text, validLocation: false);
            VerifyWorker(text, validLocation: true, options: Options.Script);
        }

        protected void VerifyFalse(string text)
        {
            // run the verification in both context(normal and script)
            VerifyWorker(text, validLocation: false);
            VerifyWorker(text, validLocation: false, options: Options.Script);
        }

        protected string AddInsideMethod(string text)
        {
            return
@"class C
{
  void F()
  {
    " + text +
@"  }
}";
        }

        protected string AddInsideClass(string text)
        {
            return
@"class C
{
    " + text +
@"}";
        }
    }
}
