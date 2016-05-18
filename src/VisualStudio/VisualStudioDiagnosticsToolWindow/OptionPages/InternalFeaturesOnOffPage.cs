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
                base.AddOptions(panel);

                // add force low memory mode option
                var group = new WrapPanel();
                var lowMemoryMode = ForceLowMemoryMode.Instance;
                var cb = CreateBoundCheckBox("Forced Low Memory Mode", lowMemoryMode, "Enabled");
                group.Children.Add(cb);
                var tb = CreateBoundTextBox("", lowMemoryMode, "Size");
                group.Children.Add(tb);
                var text = new TextBlock() { Text = "MB" };
                group.Children.Add(text);
                panel.Children.Add(group);
            }

            private CheckBox CreateBoundCheckBox(string content, object source, string sourcePropertyName)
            {
                var cb = new CheckBox { Content = content };

                var binding = new Binding()
                {
                    Source = source,
                    Path = new PropertyPath(sourcePropertyName)
                };

                base.AddBinding(cb.SetBinding(CheckBox.IsCheckedProperty, binding));

                return cb;
            }

            private TextBox CreateBoundTextBox(string content, object source, string sourcePropertyName)
            {
                var tb = new TextBox { Text = content };

                var binding = new Binding()
                {
                    Source = source,
                    Path = new PropertyPath(sourcePropertyName)
                };

                base.AddBinding(tb.SetBinding(TextBox.TextProperty, binding));

                return tb;
            }
        }
    }
}
