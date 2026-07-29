using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSBuildWorkspaceTester.Logging;
using MSBuildWorkspaceTester.Services;
using MSBuildWorkspaceTester.ViewModels;

namespace MSBuildWorkspaceTester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ServiceCollection serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            MSBuildService msbuildService = serviceProvider.GetRequiredService<MSBuildService>();
            msbuildService.Initialize();

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(serviceProvider);

            MainWindow = mainWindowViewModel.CreateView();
            MainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            OutputService outputService = new OutputService();
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddOutput(outputService);

            serviceCollection.AddSingleton(loggerFactory);
            serviceCollection.AddSingleton(outputService);
            serviceCollection.AddSingleton<MSBuildService>();
            serviceCollection.AddSingleton<WorkspaceService>();
            serviceCollection.AddSingleton<OpenDocumentService>();
        }
    }
}
