// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Formatting
{
    internal class XamlFormattingOptions
    {
        public bool InsertSpaces { get; set; }
        public int TabSize { get; set; }
        public IDictionary<string, object> OtherOptions { get; set; }
    }
}
