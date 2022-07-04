// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ColorSchemes;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int ColorScheme
        {
            get { return (int)GetOption(ColorSchemeOptions.ColorScheme); }
            set { SetOption(ColorSchemeOptions.ColorScheme, (ColorSchemeName)value); }
        }
    }
}
