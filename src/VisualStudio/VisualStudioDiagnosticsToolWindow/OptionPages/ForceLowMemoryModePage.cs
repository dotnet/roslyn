// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages;

[Guid(Guids.RoslynOptionPageFeatureManagerFeaturesIdString)]
internal sealed class ForceLowMemoryModePage : AbstractOptionPage
{
    protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        => new Control(optionStore);

    internal sealed class Control : InternalOptionsControl
    {
        public Control(OptionStore optionStore)
            : base([], optionStore)
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
        }
    }
}
