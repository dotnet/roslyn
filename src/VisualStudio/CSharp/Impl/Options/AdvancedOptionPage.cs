// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [Guid(Guids.CSharpOptionPageAdvancedIdString)]
    internal class AdvancedOptionPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            var componentModel = (IComponentModel)this.Site.GetService(typeof(SComponentModel));
            return new AdvancedOptionPageControl(optionStore, componentModel);
        }
    }
}
