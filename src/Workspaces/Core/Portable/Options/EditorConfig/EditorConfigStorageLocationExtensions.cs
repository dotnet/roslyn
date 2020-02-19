// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.Internal.Options
#else
namespace Microsoft.CodeAnalysis.Options
#endif
{
    internal static class EditorConfigStorageLocationExtensions
    {
        public static bool TryGetOption(this IEditorConfigStorageLocation editorConfigStorageLocation, AnalyzerConfigOptions analyzerConfigOptions, Type type, out object value)
        {
#if CODE_STYLE
            // TODO: "AnalyzerConfigOptions.Keys" is not yet available in CodeStyle layer.
            //       Remove this #if once it is available.
            value = default;
            return false;
#else
            var optionDictionary = analyzerConfigOptions.Keys.ToImmutableDictionary(
                key => key,
                key =>
                {
                    analyzerConfigOptions.TryGetValue(key, out var optionValue);
                    return optionValue;
                });

            return editorConfigStorageLocation.TryGetOption(optionDictionary, type, out value);
#endif
        }
    }
}
