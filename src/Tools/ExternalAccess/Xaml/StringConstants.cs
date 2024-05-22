// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal static class StringConstants
{
    public const string XamlLanguageName = "XAML";

    public const string XamlFileExtension = ".xaml";

    public const string ImportingConstructorMessage = MefConstruction.ImportingConstructorMessage;
    public const string FactoryMethodMessage = MefConstruction.FactoryMethodMessage;
}
