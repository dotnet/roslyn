// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml;

internal static class StringConstants
{
    public const string XamlLanguageName = "XAML";

    public const string XamlFileExtension = ".xaml";

    public const string CreateEventHandlerCommand = "Xaml.CreateEventHandler";

    public const string RetriggerCompletionCommand = "editor.action.triggerSuggest";

    public const string XamlLspLanguagesContract = "XamlLspLanguages";

    public const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
    public const string FactoryMethodMessage = "This factory method only provides services for the MEF export provider.";
}
