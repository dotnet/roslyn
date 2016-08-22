// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    [Guid(Guids.RoslynOptionPageFeatureManagerFeaturesIdString)]
    internal class InternalFeaturesOnOffPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider)
        {
            return new InternalFeaturesOptionsControl(InternalFeatureOnOffOptions.OptionName, serviceProvider);
        }

        internal class InternalFeaturesOptionsControl : InternalOptionsControl
        {
            public InternalFeaturesOptionsControl(string featureOptionName, IServiceProvider serviceProvider)
                : base(featureOptionName, serviceProvider)
            {
            }

            protected override void AddOptions(Panel panel)
            {
                // add force low memory mode option
                var group = new WrapPanel();

                var cb = new CheckBox { Content = "Forced Low Memory Mode: allocate" };
                BindToOption(cb, ForceLowMemoryMode.Enabled);
                group.Children.Add(cb);

                var textBox = new TextBox { MinWidth = 60 };
                BindToOption(textBox, ForceLowMemoryMode.SizeInMegabytes);
                group.Children.Add(textBox);

                group.Children.Add(new TextBlock { Text = "megabytes of extra memory in devenv.exe" });

                panel.Children.Add(group);

                // and add the rest of the options
                base.AddOptions(panel);
            }
        }
    }
}
