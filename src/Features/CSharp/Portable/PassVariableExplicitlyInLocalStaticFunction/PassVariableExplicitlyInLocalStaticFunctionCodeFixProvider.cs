using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.GetCapturedVariables;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace PassVariableExplicitlyInLocalStaticFunction
{
    //interface IMyService : ILanguageService
    //{
    //    Task<Document> FixDocument(Document document, LocalFunctionStatementSyntax localfunction, CancellationToken cancellationToken);
    //}

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PassVariableExplicitlyInLocalStaticFunctionCodeFixProvider)), Shared]
    internal class PassVariableExplicitlyInLocalStaticFunctionCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Pass variable explicitly";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create("CS8421"); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<LocalFunctionStatementSyntax>().First();
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            //var service = context.Document.GetLanguageService<IMyService>();

            var service = document.GetLanguageService<GetCaptures>(); //how does it perform what we want if we aren't passing in any parameters inside GetCaptures?




            // Register a new code action that will invoke the fix.
            context.RegisterCodeFix(
                new MyCodeAction(c => service.CreateParameterSymbolAsync(context.Document, declaration, c)),
                context.Diagnostics);



        }


        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base("Pass variable explicitly in local static function", createChangedSolution, "Pass variable explicitly in local static function")
            {
            }
        }

    }



}
