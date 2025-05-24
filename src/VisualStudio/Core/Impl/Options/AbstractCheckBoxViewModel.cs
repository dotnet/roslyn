// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal abstract class AbstractCheckBoxViewModel : AbstractNotifyPropertyChanged
{
    private readonly string _truePreview;
    private readonly string _falsePreview;
    protected bool _isChecked;

    protected AbstractOptionPreviewViewModel Info { get; }
    public IOption2 Option { get; }
    public string Description { get; set; }

    internal virtual string GetPreview() => _isChecked ? _truePreview : _falsePreview;

    public AbstractCheckBoxViewModel(IOption2 option, string description, string preview, AbstractOptionPreviewViewModel info)
        : this(option, description, preview, preview, info)
    {
    }

    public AbstractCheckBoxViewModel(IOption2 option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info)
    {
        _truePreview = truePreview;
        _falsePreview = falsePreview;

        Info = info;
        Option = option;
        Description = description;
    }

    public abstract bool IsChecked { get; set; }
}
