// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;

namespace Microsoft.VisualStudio.LanguageServices.ExtractInterface
{
    internal static class ExtractInterfaceOptionStorage
    {
        public static PerLanguageOption2<NewTypeDestination> ExtractInterfaceDestination = new("dotnet_extract_interface_destination", defaultValue: NewTypeDestination.NewFile);
    }
}
