// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.RemoteHost;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.DesignerAttributes
{
    [UseExportProvider]
    public class DesignerAttributeServiceTests
    {
        [Fact]
        public async Task NoDesignerTest()
        {
            var code = @"class Test { }";

            await TestAsync(code, designer: false);
        }

        [Fact]
        public async Task SimpleDesignerTest()
        {
            var code = @"[System.ComponentModel.DesignerCategory(""Form"")]
                class Test { }";

            await TestAsync(code, designer: true);
        }

        private static async Task TestAsync(string codeWithMarker, bool designer)
        {
            await TestAsync(codeWithMarker, designer, remote: false);
            await TestAsync(codeWithMarker, designer, remote: true);
        }

        private static async Task TestAsync(string codeWithMarker, bool designer, bool remote)
        {
            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker, openDocuments: false);
            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, remote);

            var hostDocument = workspace.Documents.First();
            var documentId = hostDocument.Id;
            var document = workspace.CurrentSolution.GetDocument(documentId);

            var service = document.GetLanguageService<IDesignerAttributeService>();
            var result = await service.ScanDesignerAttributesAsync(document, CancellationToken.None);

            var argumentIsNull = result.DesignerAttributeArgument == null;
            Assert.Equal(designer, !argumentIsNull);
        }
    }
}
