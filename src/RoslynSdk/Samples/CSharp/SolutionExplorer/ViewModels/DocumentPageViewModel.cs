using System;
using System.Windows.Controls;
using MSBuildWorkspaceTester.Framework;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class DocumentPageViewModel : PageViewModel<UserControl>
    {
        private readonly DocumentViewModel _documentViewModel;

        public NotifyTaskCompletion<string> SourceText { get; }

        public DocumentPageViewModel(IServiceProvider serviceProvider, DocumentViewModel documentViewModel)
            : base("DocumentView", serviceProvider)
        {
            _documentViewModel = documentViewModel;

            Caption = _documentViewModel.DisplayName;

            SourceText = new NotifyTaskCompletion<string>(_documentViewModel.GetSourceTextAsync());
        }
    }
}
