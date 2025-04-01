// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class EditAndContinueTestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public EditAndContinueTestAnalyzerConfigOptionsProvider(IEnumerable<(string, string)> options)
        => GlobalOptions = new EditAndContinueTestAnalyzerConfigOptions(options);

    public override AnalyzerConfigOptions GlobalOptions { get; }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        => GlobalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        => GlobalOptions;
}
