// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [Guid(Guids.CSharpOptionPageNamingStyleIdString)]
    internal class NamingStylesOptionPage : AbstractOptionPage
    {
        private IServiceProvider _provider;

        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider)
        {
            _provider = serviceProvider;
            return new NamingStyleOptionGrid(serviceProvider, LanguageNames.CSharp);
        }

        protected override void OnDeactivate(CancelEventArgs e)
        {
            var foundErrors = ((NamingStyleOptionGrid)_pageControl).ContainsErrors();
            if (foundErrors)
            {
                e.Cancel = true;
                MessageBox.Show(CSharpVSResources.Some_naming_rules_are_incomplete);
            }

            base.OnDeactivate(e);
        }
    }
}
