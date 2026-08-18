using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using MSBuildWorkspaceTester.ViewModels;

namespace MSBuildWorkspaceTester.Services
{
    internal class OpenDocumentService : BaseService
    {
        private readonly Dictionary<DocumentId, PageViewModel> _openDocumentTabs;
        public ObservableCollection<PageViewModel> Pages { get; }

        public OpenDocumentService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _openDocumentTabs = new Dictionary<DocumentId, PageViewModel>();
            Pages = new ObservableCollection<PageViewModel>();
        }

        public void ActivateOrOpenDocument(DocumentViewModel documentViewModel)
        {
            if (!_openDocumentTabs.TryGetValue(documentViewModel.DocumentId, out PageViewModel pageViewModel))
            {
                pageViewModel = new DocumentPageViewModel(ServiceProvider, documentViewModel);
                _openDocumentTabs.Add(documentViewModel.DocumentId, pageViewModel);
                Pages.Add(pageViewModel);
            }

            SelectedPageChanged?.Invoke(this, pageViewModel);
        }

        public event EventHandler<PageViewModel> SelectedPageChanged;
    }
}
