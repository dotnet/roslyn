// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options;

public partial class AutomationObject
{
    public int SortUsings_SeparateImportDirectiveGroups
    {
        get { return GetBooleanOption(GenerationOptions.SeparateImportDirectiveGroups); }
        set { SetBooleanOption(GenerationOptions.SeparateImportDirectiveGroups, value); }
    }

    public int SortUsings_PlaceSystemFirst
    {
        get { return GetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst); }
        set { SetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst, value); }
    }
}
