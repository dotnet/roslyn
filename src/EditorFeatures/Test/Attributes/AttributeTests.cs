// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Attributes
{
    public class AttributeTests
    {
#if false
        [WpfFact]
        public void CreateExportSyntaxTokenCodeIssueProviderAttributeWithNullArg()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ExportSyntaxTokenCodeIssueProviderAttribute("name", null));
            Assert.Throws<ArgumentNullException>(() =>
                new ExportSyntaxTokenCodeIssueProviderAttribute(null, "language"));
            new ExportSyntaxTokenCodeIssueProviderAttribute("name", "language");
        }

        [WpfFact]
        public void CreateExportSyntaxTriviaCodeIssueProviderAttributeWithNullArg()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ExportSyntaxTriviaCodeIssueProviderAttribute("name", null));
            Assert.Throws<ArgumentNullException>(() =>
                new ExportSyntaxTriviaCodeIssueProviderAttribute(null, "language"));
            new ExportSyntaxTriviaCodeIssueProviderAttribute("name", "language");
        }
#endif

        [WpfFact]
        public void CreateExportBraceMatcherAttributeWithNullArg()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ExportBraceMatcherAttribute(null));
        }

        [WpfFact]
        public void CreateExportCompletionProviderAttributeWithNullArg()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ExportCompletionProviderAttribute("name", null));
            Assert.Throws<ArgumentNullException>(() =>
                new ExportCompletionProviderAttribute(null, "language"));
        }
    }
}
