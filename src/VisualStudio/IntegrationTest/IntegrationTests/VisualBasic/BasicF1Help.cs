// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
    public class BasicF1Help : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicF1Help() : base(nameof(BasicF1Help))
        {
        }

        [TestMethod, TestCategory(Traits.Features.F1Help)]
        void F1Help()
        {
            var text = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program$$
    Sub Main(args As String())
        Dim query = From arg In args
                    Select args.Any(Function(a) a.Length > 5)
        Dim x = 0
        x += 1
    End Sub
    Public Function F() As Object
        Return Nothing
    End Function
End Module";

            SetUpEditor(text);
            VerifyF1Keyword("Linq", "System.Linq");
            VerifyF1Keyword("String", "vb.String");
            VerifyF1Keyword("Any", "System.Linq.Enumerable.Any");
            VerifyF1Keyword("From", "vb.QueryFrom");
            VerifyF1Keyword("+=", "vb.+=");
            VerifyF1Keyword("Nothing", "vb.Nothing");

        }

        private void VerifyF1Keyword(string word, string expectedKeyword)
        {
            VisualStudioInstance.Editor.PlaceCaret(word, charsOffset: -1);
            ExtendedAssert.Contains(expectedKeyword, VisualStudioInstance.Editor.GetF1Keyword().ToArray());
        }
    }
}
