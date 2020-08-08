// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [Fact]
        public void CreateExportBraceMatcherAttributeWithNullArg()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ExportBraceMatcherAttribute(null));
        }

        [Fact]
        public void CreateExportCompletionProviderAttributeWithNullArg()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ExportCompletionProviderMef1Attribute("name", null));
            Assert.Throws<ArgumentNullException>(() =>
                new ExportCompletionProviderMef1Attribute(null, "language"));
        }
    }
}
