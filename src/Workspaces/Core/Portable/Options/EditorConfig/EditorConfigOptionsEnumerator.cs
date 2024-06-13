// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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
    public ImmutableArray<(string feature, ImmutableArray<IOption2> options)> GetOptions(string language)
    {
        var builder = ArrayBuilder<(string, ImmutableArray<IOption2>)>.GetInstance();

        foreach (var generator in optionEnumerators)
        {
            if (generator.Metadata.Language == language)
            {
                builder.AddRange(generator.Value.GetOptions());
            }
        }

        return builder.ToImmutableAndFree();
    }

    internal static IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetLanguageAgnosticEditorConfigOptions()
    {
        yield return (WorkspacesResources.Core_EditorConfig_Options, FormattingOptions2.Options);
        yield return (WorkspacesResources.dot_NET_Coding_Conventions, GenerationOptions.AllOptions.AddRange(CodeStyleOptions2.AllOptions));
    }
}
