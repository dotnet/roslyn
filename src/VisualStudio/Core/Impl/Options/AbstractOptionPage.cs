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
        private static bool s_needsToUpdateOptionStore = true;

        private bool _needsLoadOnNextActivate = true;

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

            if (s_needsToUpdateOptionStore)
            {
                // Reset the option store to the current state of the options.
                s_optionStore.SetOptions(s_optionService.GetOptions());
                s_optionStore.SetRegisteredOptions(s_optionService.GetRegisteredOptions());

                s_needsToUpdateOptionStore = false;
            }

            if (_needsLoadOnNextActivate)
            {
                // For pages that don't use option bindings we need to load setting changes.
                pageControl.OnLoad();

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
            // We need to ignore the first case since the option store is static and shared among 
            // pages. Each page will get this same call so we should ensure that our page has been
            // created first.
            //
            // This second one is tricky, because we don't actually want to update our controls
            // right then, because they'd be wrong the next time the page opens -- it's possible
            // they may have been changed programmatically. Therefore, we'll set a flag so we load
            // next time

            // When pageControl is null we know that Activation has not happened for this page.
            // We only need to update the OptionStore after a cancel or close click (2).
            s_needsToUpdateOptionStore = pageControl != null;

            // For pages that don't use option bindings we need to load settings when it is
            // activated next.
            _needsLoadOnNextActivate = true;
        }

        public override void SaveSettingsToStorage()
        {
            EnsureOptionPageCreated();

            // Allow page controls to perist their settings to the option store before updating the
            // option service.
            pageControl.OnSave();

            // Save the changes that were accumulated in the option store.
            var oldOptions = s_optionService.GetOptions();
            var newOptions = s_optionStore.GetOptions();

            // Must log the option change before setting the new option values via s_optionService,
            // otherwise oldOptions and newOptions would be identical and nothing will be logged.
            OptionLogger.Log(oldOptions, newOptions);
            s_optionService.SetOptions(newOptions);

            // Make sure we load the next time a page is activated, in case that options changed
            // programmatically between now and the next time the page is activated
            s_needsToUpdateOptionStore = true;

            // For pages that don't use option bindings we need to load settings when it is
            // activated next.
            _needsLoadOnNextActivate = true;
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
