// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    public class MiscTests
    {
        private static ISyntaxTriviaService GetSyntaxTriviaService()
        {
            var languageService = new MockCSharpLanguageServiceProvider();
            var service = (ISyntaxTriviaService)new CSharpSyntaxTriviaServiceFactory().CreateLanguageService(languageService);

            return service;
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ServiceTest1()
        {
            var service = GetSyntaxTriviaService();

            var markupCode = @"class A
{
    /* test */ [|public|] void Test(int i, int b, int c)
    {

    }
}";

            var code = default(string);
            var span = default(TextSpan);
            MarkupTestFile.GetSpan(markupCode, out code, out span);

            var root = SyntaxFactory.ParseCompilationUnit(code);
            var result = service.SaveTriviaAroundSelection(root, span);

            var rootWithAnnotation = result.Root;

            // find token to replace
            var publicToken = rootWithAnnotation.DescendantTokens().First(t => t.Kind() == SyntaxKind.PublicKeyword);

            // replace the token with new one
            var newRoot = rootWithAnnotation.ReplaceToken(publicToken, SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            // restore trivia around it
            var rootWithTriviaRestored = result.RestoreTrivia(newRoot);

            var expected = @"class A
{
    /* test */ private void Test(int i, int b, int c)
    {

    }
}";

            Assert.Equal(expected, rootWithTriviaRestored.ToFullString());
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ServiceTest2()
        {
            var service = GetSyntaxTriviaService();

            var markupCode = @"class A
{

#if true
    [|/* test */ public|] void Test(int i, int b, int c)
    {

    }
#endif

}";

            var code = default(string);
            var span = default(TextSpan);
            MarkupTestFile.GetSpan(markupCode, out code, out span);

            var root = SyntaxFactory.ParseCompilationUnit(code);
            var result = service.SaveTriviaAroundSelection(root, span);

            var rootWithAnnotation = result.Root;

            // find token to replace
            var publicToken = rootWithAnnotation.DescendantTokens().First(t => t.Kind() == SyntaxKind.PublicKeyword);

            // replace the token with new one
            var newRoot = rootWithAnnotation.ReplaceToken(publicToken, SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            // restore trivia around it
            var rootWithTriviaRestored = result.RestoreTrivia(newRoot);

            var expected = @"class A
{

#if true
    private void Test(int i, int b, int c)
    {

    }
#endif

}";

            Assert.Equal(expected, rootWithTriviaRestored.ToFullString());
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestExtractMethodCommandHandlerErrorMessage()
        {
            var markupCode = @"class A
{
    [|void Method() {}|]
}";

            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(new[] { markupCode }))
            {
                var testDocument = workspace.Documents.Single();
                var container = testDocument.GetOpenTextContainer();

                var view = testDocument.GetTextView();
                view.Selection.Select(new SnapshotSpan(
                    view.TextBuffer.CurrentSnapshot, testDocument.SelectedSpans[0].Start, testDocument.SelectedSpans[0].Length), isReversed: false);

                var callBackService = workspace.Services.GetService<INotificationService>() as INotificationServiceCallback;
                var called = false;
                callBackService.NotificationCallback = (t, m, s) => called = true;

                var handler = new ExtractMethodCommandHandler(
                    workspace.ExportProvider.GetExportedValue<ITextBufferUndoManagerProvider>(),
                    workspace.ExportProvider.GetExportedValue<IEditorOperationsFactoryService>(),
                    workspace.ExportProvider.GetExportedValue<IInlineRenameService>(),
                    workspace.ExportProvider.GetExportedValue<IWaitIndicator>());

                handler.ExecuteCommand(new Commands.ExtractMethodCommandArgs(view, view.TextBuffer), () => { });

                Assert.True(called);
            }
        }

        /// <summary>
        /// mock for the unit test. can't use Mock type since ICSharpLanguageServiceProvider is a internal type.
        /// </summary>
        private class MockCSharpLanguageServiceProvider : HostLanguageServices
        {
            public override HostWorkspaceServices WorkspaceServices
            {
                get
                {
                    throw new System.NotImplementedException();
                }
            }

            public override string Language
            {
                get { return LanguageNames.CSharp; }
            }

            public override TLanguageService GetService<TLanguageService>()
            {
                Assert.Equal(typeof(TLanguageService), typeof(ISyntaxFactsService));

                return (TLanguageService)((object)new CSharpSyntaxFactsService());
            }
        }
    }
}
