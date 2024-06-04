// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Options;

[Export(typeof(EditorConfigOptionsEnumerator)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorConfigOptionsEnumerator(
    [ImportMany] IEnumerable<Lazy<IEditorConfigOptionsEnumerator, LanguageMetadata>> optionEnumerators)
{
    public IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetOptions(string language, bool includeUndocumented = false)
        => optionEnumerators
            .Where(e => e.Metadata.Language == language)
            .SelectMany(e => e.Value.GetOptions(includeUndocumented));

    internal static IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetLanguageAgnosticEditorConfigOptions(bool includeUndocumented)
    {
        yield return (WorkspacesResources.Core_EditorConfig_Options, FormattingOptions2.EditorConfigOptions);

        if (includeUndocumented)
        {
            yield return (WorkspacesResources.Core_EditorConfig_Options, FormattingOptions2.UndocumentedOptions);
        }

        yield return (WorkspacesResources.dot_NET_Coding_Conventions, GenerationOptions.EditorConfigOptions.AddRange(CodeStyleOptions2.EditorConfigOptions));
    }
}
