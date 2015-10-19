// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Roslyn.Test.Utilities;
using Xunit;
using SyntaxUtilities = Microsoft.CodeAnalysis.CSharp.EditAndContinue.SyntaxUtilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue
{
    public class SyntaxUtilitiesTests
    {
        private void VerifySyntaxMap(string oldSource, string newSource)
        {
            var oldRoot = SyntaxFactory.ParseSyntaxTree(oldSource).GetRoot();
            var newRoot = SyntaxFactory.ParseSyntaxTree(newSource).GetRoot();

            foreach (var oldNode in oldRoot.DescendantNodes().Where(n => n.FullSpan.Length > 0))
            {
                var newNode = SyntaxUtilities.FindPartner(oldRoot, newRoot, oldNode);
                Assert.True(SyntaxFactory.AreEquivalent(oldNode, newNode), $"Node '{oldNode}' not equivalent to '{newNode}'.");
            }
        }

        [WpfFact]
        public void FindPartner1()
        {
            string source1 = @"
using System;

class C
{
    static void Main(string[] args)
    {

        // sdasd
        var b = true;
        do
        {
            Console.WriteLine(""hi"");
        } while (b == true);
    }
}
";

            string source2 = @"
using System;

class C
{
    static void Main(string[] args)
    {
        var b = true;
        do
        {
            Console.WriteLine(""hi"");
        } while (b == true);
    }
}
";
            VerifySyntaxMap(source1, source2);
        }
    }
}
