// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.UnitTests;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Symbols;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.LanguageServices
{
    public class SyntaxTreeFactoryServiceTests
    {
        [Fact]
        public void CSharpSyntaxTreeFactoryServiceTest()
        {
            var workspace = new CustomWorkspace(TestHost.Services, "Test");
            var service = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ISyntaxTreeFactoryService>();

            var text = @"/// <summary>XML</summary>
class C { }";
            var fileName = @"C:\1.cs";

            TestSyntaxTreeFactoryService(service, text, fileName);
        }

        [Fact]
        public void VisualBasicSyntaxTreeFactoryServiceTest()
        {
            var workspace = new CustomWorkspace(TestHost.Services, "Test");
            var service = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<ISyntaxTreeFactoryService>();

            var text = @"''' <summary>XML</summary>
Class C
End Class";
            var fileName = @"C:\1.vb";

            TestSyntaxTreeFactoryService(service, text, fileName);
        }

        private static void TestSyntaxTreeFactoryService(ISyntaxTreeFactoryService service, string text, string fileName)
        {
            var parseOptions = service.GetDefaultParseOptions();
            var workspace = new CustomWorkspace(TestHost.Services, "Test");

            var tree = service.ParseSyntaxTree(
                fileName,
                parseOptions,
                SourceText.From(text),
                CancellationToken.None);

            var textAndVersion = TextAndVersion.Create(
                tree.GetText(),
                VersionStamp.Create(),
                fileName);

            var valueSource = new AsyncLazy<TextAndVersion>(textAndVersion);

            var recoverableTree = service.CreateRecoverableTree(
                tree.FilePath,
                tree.Options,
                valueSource,
                tree.GetRoot());

            workspace.Services.GetService<ISyntaxTreeCacheService>().Clear();

            var trivia = tree.GetRoot().GetLeadingTrivia().First();
            var actualTrivia = recoverableTree.GetRoot().GetLeadingTrivia().First();

            Assert.Equal(trivia.ToFullString(), actualTrivia.ToFullString());
        }
    }
}
