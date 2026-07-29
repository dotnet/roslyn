using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MSBuildWorkspaceTester.Framework;
using MSBuildWorkspaceTester.Services;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class MainWindowViewModel : ViewModel<Window>
    {
        private readonly ILogger _logger;
        private readonly WorkspaceService _workspaceService;
        private readonly OpenDocumentService _openDocumentService;

        public ICommand OpenProjectCommand { get; }
        public ICommand OpenFileCommand { get; }

        public ObservableCollection<SolutionViewModel> Solution { get; }
        public ObservableCollection<PageViewModel> Pages => _openDocumentService.Pages;

        public MainWindowViewModel(IServiceProvider serviceProvider)
            : base("MainWindowView", serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<MainWindowViewModel>();

            _workspaceService = serviceProvider.GetRequiredService<WorkspaceService>();
            _openDocumentService = serviceProvider.GetRequiredService<OpenDocumentService>();

            Solution = new ObservableCollection<SolutionViewModel>();
            Pages.Add(new LogViewModel(serviceProvider));

            OpenProjectCommand = RegisterCommand(
                text: "Open Project/Solution",
                name: "Open Project/Solution",
                executed: OpenProjectExecuted,
                canExecute: CanOpenProjectExecute);

            OpenFileCommand = RegisterCommand<HierarchyItemViewModel>(
                text: "Open File",
                name: "Open File",
                executed: OpenFileExecuted,
                canExecute: CanOpenFileExecute);
        }

        protected override void OnViewCreated(Window view)
        {
            TabControl tabControl = view.FindName<TabControl>("Tabs");
            tabControl.SelectedIndex = 0;

            _openDocumentService.SelectedPageChanged += (_, page) =>
            {
                tabControl.SelectedItem = page;
            };
        }

        private bool CanOpenProjectExecute() => true;

        private async void OpenProjectExecuted()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Open Project or Solution",
                Filter = "Supported Files (*.sln,*.csproj,*.vbproj)|*.sln;*.csproj;*.vbproj|" +
                         "Solution Files (*.sln)||*.sln|" +
                         "Project Files (*.csproj,*.vbproj)|*.csproj|*.vbproj|" +
                         "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog(View) == true)
            {
                string fileName = dialog.FileName;
                string extension = Path.GetExtension(fileName);

                _logger.LogInformation($"\r\nLoading {fileName}...\r\n");

                switch (extension)
                {
                    case ".sln":
                        await _workspaceService.OpenSolutionAsync(fileName);
                        break;

                    default:
                        await _workspaceService.OpenProjectAsync(fileName);
                        break;
                }

                Solution.Clear();
                Solution.Add(new SolutionViewModel(_workspaceService.Workspace));
            }
        }

        private bool CanOpenFileExecute(HierarchyItemViewModel viewModel) => true;

        private void OpenFileExecuted(HierarchyItemViewModel viewModel)
        {
            if (viewModel is DocumentViewModel documentViewModel)
            {
                _openDocumentService.ActivateOrOpenDocument(documentViewModel);
            }
        }
    }
}
