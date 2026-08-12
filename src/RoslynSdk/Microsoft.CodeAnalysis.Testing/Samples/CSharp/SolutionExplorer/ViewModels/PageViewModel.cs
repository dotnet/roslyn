using System;
using System.Windows;
using System.Windows.Controls;
using MSBuildWorkspaceTester.Framework;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal abstract class PageViewModel : ViewModel
    {
        private string _caption;
        private FrameworkElement _content;

        public string Caption
        {
            get => _caption;
            protected set => SetValue(ref _caption, value);
        }

        public FrameworkElement Content
        {
            get => _content;
            protected set => SetValue(ref _content, value);
        }
    }

    internal abstract class PageViewModel<TContent> : PageViewModel
        where TContent : ContentControl
    {
        private readonly string _contentName;

        protected IServiceProvider ServiceProvider { get; }

        protected PageViewModel(string contentName, IServiceProvider serviceProvider)
        {
            _contentName = contentName ?? throw new ArgumentNullException(nameof(contentName));
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            CreateContent();
        }

        protected virtual void OnContentCreated(TContent content)
        {
            // Decendents can override
        }

        private string GetViewUriString()
            => $"/{GetType().Assembly.GetName().Name};component/Views/{_contentName}.xaml";

        public TContent CreateContent()
        {
            Uri uri = new Uri(GetViewUriString(), UriKind.Relative);
            TContent content = (TContent)Application.LoadComponent(uri);
            content.DataContext = this;
            Content = content;

            OnContentCreated(content);

            return content;
        }
    }
}
