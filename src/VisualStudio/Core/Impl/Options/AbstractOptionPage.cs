// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [System.ComponentModel.DesignerCategory("code")] // this must be fully qualified
    internal abstract class AbstractOptionPage : UIElementDialogPage
    {
        protected abstract AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider);

        private AbstractOptionPageControl _pageControl;
        private bool _needsLoadOnNextActivate = true;

        private void EnsureOptionPageCreated()
        {
            if (_pageControl == null)
            {
                _pageControl = CreateOptionPage(this.Site);
            }
        }

        protected override System.Windows.UIElement Child
        {
            get
            {
                EnsureOptionPageCreated();
                return _pageControl;
            }
        }

        protected override void OnActivate(System.ComponentModel.CancelEventArgs e)
        {
            if (_needsLoadOnNextActivate)
            {
                EnsureOptionPageCreated();
                _pageControl.LoadSettings();

                _needsLoadOnNextActivate = false;
            }
        }

        public override void LoadSettingsFromStorage()
        {
            // This gets called in two situations:
            //
            // 1) during the initial page load when you first activate the page, before OnActivate
            //    is called.
            // 2) during the closing of the dialog via Cancel/close when options don't need to be
            //    saved. The intent here is the settings get reloaded so the next time you open the
            //    page they are properly populated.
            //
            // This second one is tricky, because we don't actually want to update our controls
            // right then, because they'd be wrong the next time the page opens -- it's possible
            // they may have been changed programmatically. Therefore, we'll set a flag so we load
            // next time
            _needsLoadOnNextActivate = true;
        }

        public override void SaveSettingsToStorage()
        {
            EnsureOptionPageCreated();
            _pageControl.SaveSettings();

            // Make sure we load the next time the page is activated, in case if options changed
            // programmatically between now and the next time the page is activated
            _needsLoadOnNextActivate = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (_pageControl != null)
            {
                _pageControl.Close();
            }
        }
    }
}
