// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    [Guid(Guids.CSharpOptionPageFormattingSpacingIdString)]
    internal class FormattingSpacingPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            return new OptionPreviewControl(serviceProvider, optionStore, (o, s) => new SpacingViewModel(o, s));
        }
    }
}
