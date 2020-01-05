// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Guid(Guids.RoslynOptionPageFeatureManagerComponentsIdString)]
    internal class InternalComponentsOnOffPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            return new InternalOptionsControl(nameof(EditorComponentOnOffOptions), optionStore);
        }
    }
}
