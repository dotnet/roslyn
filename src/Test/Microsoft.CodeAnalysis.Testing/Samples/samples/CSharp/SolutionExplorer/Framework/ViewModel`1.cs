using System;
using System.Windows;
using System.Windows.Controls;

namespace MSBuildWorkspaceTester.Framework
{
    internal abstract class ViewModel<TView> : ViewModel
        where TView : ContentControl
    {
        private readonly string _viewName;

        protected IServiceProvider ServiceProvider { get; }
        protected TView View { get; private set; }

        protected ViewModel(string viewName, IServiceProvider serviceProvider)
        {
            _viewName = viewName ?? throw new ArgumentNullException(nameof(viewName));
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected virtual void OnViewCreated(TView view)
        {
            // Decendents can override
        }

        private string GetViewUriString()
            => $"/{GetType().Assembly.GetName().Name};component/Views/{_viewName}.xaml";

        public TView CreateView()
        {
            Uri uri = new Uri(GetViewUriString(), UriKind.Relative);
            View = (TView)Application.LoadComponent(uri);
            View.DataContext = this;

            OnViewCreated(View);

            return View;
        }
    }
}
