// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Options;

[EditorConfigOptionsEnumerator(LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEditorConfigOptionsEnumerator() : IEditorConfigOptionsEnumerator
{
    public IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetOptions(bool includeUndocumented)
    {
        foreach (var entry in EditorConfigOptionsEnumerator.GetLanguageAgnosticEditorConfigOptions(includeUndocumented))
        {
            yield return entry;
        }

        yield return (CSharpWorkspaceResources.CSharp_Coding_Conventions, CSharpCodeStyleOptions.EditorConfigOptions);
        yield return (CSharpWorkspaceResources.CSharp_Formatting_Rules, CSharpFormattingOptions2.EditorConfigOptions);

        if (includeUndocumented)
        {
            yield return (CSharpWorkspaceResources.CSharp_Formatting_Rules, CSharpFormattingOptions2.UndocumentedOptions);
        }
    }
}
