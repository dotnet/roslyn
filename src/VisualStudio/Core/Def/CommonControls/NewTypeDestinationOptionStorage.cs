// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;

namespace Microsoft.VisualStudio.LanguageServices.CommonControls;

internal class NewTypeDestinationOptionStorage
{
    public static PerLanguageOption2<NewTypeDestination> ExtractClassDestination = new("dotnet_extract_class_destination", defaultValue: NewTypeDestination.NewFile);

    public static PerLanguageOption2<NewTypeDestination> ExtractInterfaceDestination = new("dotnet_extract_interface_destination", defaultValue: NewTypeDestination.NewFile);
}
