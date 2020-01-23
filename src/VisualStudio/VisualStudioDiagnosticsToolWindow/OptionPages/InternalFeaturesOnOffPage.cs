// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    [Guid(Guids.RoslynOptionPageFeatureManagerFeaturesIdString)]
    internal class InternalFeaturesOnOffPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            return new InternalFeaturesOptionsControl(nameof(InternalFeatureOnOffOptions), optionStore);
        }

        internal class InternalFeaturesOptionsControl : InternalOptionsControl
        {
            public InternalFeaturesOptionsControl(string featureOptionName, OptionStore optionStore)
                : base(featureOptionName, optionStore)
            {
            }

            protected override void AddOptions(Panel panel)
            {
                // add force low memory mode option
                var lowMemoryGroup = new WrapPanel();

                var cb = new CheckBox { Content = "Forced Low Memory Mode: allocate" };
                BindToOption(cb, ForceLowMemoryMode.Enabled);
                lowMemoryGroup.Children.Add(cb);

                var textBox = new TextBox { MinWidth = 60 };
                BindToOption(textBox, ForceLowMemoryMode.SizeInMegabytes);
                lowMemoryGroup.Children.Add(textBox);

                lowMemoryGroup.Children.Add(new TextBlock { Text = "megabytes of extra memory in devenv.exe" });

                panel.Children.Add(lowMemoryGroup);

                // add OOP feature options
                var oopFeatureGroup = new StackPanel();

                panel.Children.Add(oopFeatureGroup);

                // and add the rest of the options
                base.AddOptions(panel);
            }
        }
    }
}
