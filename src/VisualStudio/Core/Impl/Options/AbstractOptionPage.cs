// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [System.ComponentModel.DesignerCategory("code")] // this must be fully qualified
    internal abstract class AbstractOptionPage : UIElementDialogPage
    {
        private static IOptionService s_optionService;
        private static OptionStore s_optionStore;
        private static bool s_needsLoadOnNextActivate = true;

        protected abstract AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore);

        protected AbstractOptionPageControl pageControl;

        private void EnsureOptionPageCreated()
        {
            if (s_optionStore == null)
            {
                var componentModel = (IComponentModel)this.Site.GetService(typeof(SComponentModel));
                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                s_optionService = workspace.Services.GetService<IOptionService>();
                s_optionStore = new OptionStore(s_optionService.GetOptions(), s_optionService.GetRegisteredOptions());
            }

            if (pageControl == null)
            {
                // Use a single option store for all option pages so that changes are accumulated
                // together and, in the case of the same option appearing on two pages, the changes
                // are kept in sync.
                pageControl = CreateOptionPage(this.Site, s_optionStore);
            }
        }

        protected override System.Windows.UIElement Child
        {
            get
            {
                EnsureOptionPageCreated();
                return pageControl;
            }
        }

        protected override void OnActivate(System.ComponentModel.CancelEventArgs e)
        {
            EnsureOptionPageCreated();

            if (s_needsLoadOnNextActivate)
            {
                // Reset the option store to the current state of the options.
                s_optionStore.SetOptions(s_optionService.GetOptions());
                s_optionStore.SetRegisteredOptions(s_optionService.GetRegisteredOptions());

                s_needsLoadOnNextActivate = false;
            }

            pageControl.LoadSettings();
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
            // We need to ignore the first case since the option store is static and shared among 
            // pages. Each page will get this same call so we should ensure that our page has been
            // created first.
            //
            // This second one is tricky, because we don't actually want to update our controls
            // right then, because they'd be wrong the next time the page opens -- it's possible
            // they may have been changed programmatically. Therefore, we'll set a flag so we load
            // next time

            s_needsLoadOnNextActivate = pageControl != null;
        }

        public override void SaveSettingsToStorage()
        {
            EnsureOptionPageCreated();

            // Save the changes that were accumulated in the option store.
            s_optionService.SetOptions(s_optionStore.GetOptions());

            // Make sure we load the next time a page is activated, in case that options changed
            // programmatically between now and the next time the page is activated
            s_needsLoadOnNextActivate = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (pageControl != null)
            {
                pageControl.Close();
            }
        }
    }
}
