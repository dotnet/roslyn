// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;

namespace Microsoft.VisualStudio.LanguageServices.CommonControls;

internal class PersistNewTypeDestinationValueSource : INewTypeDestinationValueSource
{
    private readonly IGlobalOptionService _globalOptionService;

    private readonly PerLanguageOption2<NewTypeDestination> _option;

    private readonly string _languageName;

    public PersistNewTypeDestinationValueSource(
        IGlobalOptionService globalOptionService,
        PerLanguageOption2<NewTypeDestination> option,
        string languageName)
    {
        _globalOptionService = globalOptionService;
        _option = option;
        _languageName = languageName;
    }

    public NewTypeDestination NewTypeDestination
    {
        get => _globalOptionService.GetOption(_option, _languageName);
        set => _globalOptionService.SetGlobalOption(_option, _languageName, value);
    }
}
