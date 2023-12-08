// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Export(typeof(IWpfThemeService)), Shared]
    internal class VSWpfThemeService : IWpfThemeService
    {
        private readonly ResourceDictionary _themeDictionary = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSWpfThemeService()
        {
            _themeDictionary.Source = new Uri("/Microsoft.VisualStudio.LanguageServices;component/VSThemeDictionary.xaml", UriKind.Relative);
        }

        public void ApplyThemeToElement(FrameworkElement frameworkElement)
        {
            frameworkElement.Resources.MergedDictionaries.Add(_themeDictionary);
        }

        public Color GetThemeColor(ThemeResourceKey themeResourceKey)
        {
            var color = VSColorTheme.GetThemedColor(themeResourceKey);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
