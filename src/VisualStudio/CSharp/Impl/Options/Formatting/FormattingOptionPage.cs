// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    [GuidAttribute(Guids.CSharpOptionPageFormattingGeneralIdString)]
    internal class FormattingOptionPage : AbstractOptionPage
    {
        private FormattingOptionPageControl _optionPageControl;

        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            _optionPageControl = new FormattingOptionPageControl(optionStore);
            return _optionPageControl;
        }
    }
}
