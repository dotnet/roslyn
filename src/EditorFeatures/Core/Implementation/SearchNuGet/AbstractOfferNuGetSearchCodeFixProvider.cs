using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Implementation.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SearchNuGet
{
    internal abstract class AbstractOfferNuGetSearchCodeFixProvider : CodeFixProvider
    {
        private readonly ILightBulbBroker _lightBulbBroker;

        protected AbstractOfferNuGetSearchCodeFixProvider(ILightBulbBroker lightBulbBroker)
        {
            _lightBulbBroker = lightBulbBroker;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var workspace = context.Document.Project.Solution.Workspace;
            var options = workspace.Options;
            var searchNuGet = options.GetOption(NuGetSearchOptions.SearchNuGet);

            // Check if the user has enabled or disabled this feature.  If so, do nothing.
            if (!searchNuGet.HasValue)
            {
                context.RegisterCodeFix(new MyCodeAction(workspace, _lightBulbBroker), context.Diagnostics);
            }

            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction
        {
            private readonly Workspace _workspace;
            private readonly ILightBulbBroker _lightBulbBroker;

            public override string Title => "Search NuGet.org for types";

            public MyCodeAction(Workspace workspace, ILightBulbBroker lightBulbBroker)
            {
                _workspace = workspace;
                _lightBulbBroker = lightBulbBroker;
            }

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(SpecializedCollections.SingletonEnumerable<CodeActionOperation>(
                    new SearchNuGetOptionOperation(_workspace, _lightBulbBroker)));
            }
        }

        private class SearchNuGetOptionOperation : PreviewOperationWithTextView
        {
            private readonly Workspace _workspace;
            private readonly ILightBulbBroker _lightBulbBroker;

            internal override bool HideDefaultPreviewChrome => true;

            public SearchNuGetOptionOperation(Workspace workspace, ILightBulbBroker lightBulbBroker)
            {
                _workspace = workspace;
                _lightBulbBroker = lightBulbBroker;
            }

            internal override Task<object> GetPreviewAsync(ITextView textView, CancellationToken cancellationToken)
            {
                var panel = new OfferNuGetSearchPanel();
                panel.yesButton.Click += (s, e) => OnYesButtonClicked(textView);
                panel.noButton.Click += (s, e) => OnNoButtonClicked(textView);
                return Task.FromResult<object>(panel);
            }

            private void OnNoButtonClicked(ITextView textView)
            {
                DismissLightBulb(textView);
            }

            private void OnYesButtonClicked(ITextView textView)
            {
                DismissLightBulb(textView);
            }

            private void DismissLightBulb(ITextView textView)
            {
                _lightBulbBroker.DismissSession(textView);
            }
        }
    }

    internal static class NuGetSearchOptions
    {
        public const string OptionName = "FeatureManager/Features";

        [ExportOption]
        public static readonly Option<bool?> SearchNuGet = new Option<bool?>(OptionName, "Search NuGet.org", defaultValue: null);
    }
}
