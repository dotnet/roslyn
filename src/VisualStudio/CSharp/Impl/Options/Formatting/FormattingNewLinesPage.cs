// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    internal class FormattingNewLinesPage : AbstractOptionPage
    {
        public FormattingNewLinesPage()
        {
        }

        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            return new OptionPreviewControl(serviceProvider, optionStore, (o, s) => new NewLinesViewModel(o, s));
        }
    }
}
