﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
