// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest
{
    public class WordSimilarityCheckerTests
    {
        [Fact]
        public void TestCloseMatch()
        {
            Assert.False(WordSimilarityChecker.AreSimilar("variabledeclaratorsyntax", "variabledeclaratorsyntaxextensions"));
            Assert.True(WordSimilarityChecker.AreSimilar("variabledeclaratorsyntax", "variabledeclaratorsyntaxextensions", substringsAreSimilar: true));

            Assert.False(WordSimilarityChecker.AreSimilar("expressionsyntax", "expressionsyntaxextensions"));
            Assert.True(WordSimilarityChecker.AreSimilar("expressionsyntax", "expressionsyntaxextensions", substringsAreSimilar: true));

            Assert.False(WordSimilarityChecker.AreSimilar("expressionsyntax", "expressionsyntaxgeneratorvisitor"));
            Assert.True(WordSimilarityChecker.AreSimilar("expressionsyntax", "expressionsyntaxgeneratorvisitor", substringsAreSimilar: true));
        }

        [Fact]
        public void TestNotCloseMatch()
        {
            Assert.False(WordSimilarityChecker.AreSimilar("propertyblocksyntax", "ipropertysymbol"));
            Assert.False(WordSimilarityChecker.AreSimilar("propertyblocksyntax", "ipropertysymbolextensions"));
            Assert.False(WordSimilarityChecker.AreSimilar("propertyblocksyntax", "typeblocksyntaxextensions"));

            Assert.False(WordSimilarityChecker.AreSimilar("fielddeclarationsyntax", "declarationinfo"));
            Assert.False(WordSimilarityChecker.AreSimilar("fielddeclarationsyntax", "declarationcomputer"));
            Assert.False(WordSimilarityChecker.AreSimilar("fielddeclarationsyntax", "filelinepositionspan"));

            Assert.False(WordSimilarityChecker.AreSimilar("variabledeclaratorsyntax", "visualbasicdeclarationcomputer"));
            Assert.False(WordSimilarityChecker.AreSimilar("variabledeclaratorsyntax", "ilineseparatorservice"));

            Assert.False(WordSimilarityChecker.AreSimilar("expressionsyntax", "awaitexpressioninfo"));
        }
    }
}