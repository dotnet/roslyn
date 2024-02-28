// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

internal class IgnoredFrameViewModel : FrameViewModel
{
    private readonly IgnoredFrame _frame;

    public IgnoredFrameViewModel(IgnoredFrame frame, IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        : base(formatMap, typeMap)
    {
        _frame = frame;
    }

    public override bool ShowMouseOver => false;

    protected override IEnumerable<Inline> CreateInlines()
    {
        var run = MakeClassifiedRun(ClassificationTypeNames.ExcludedCode, _frame.ToString());
        yield return run;
    }
}
