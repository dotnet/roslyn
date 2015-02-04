// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    [Guid(Guids.RoslynOptionPageFeatureManagerFeaturesIdString)]
    internal class InternalFeaturesOnOffPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider)
        {
            return new InternalOptionsControl(InternalFeatureOnOffOptions.OptionName, serviceProvider);
        }
    }
}
