// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class NamingStylePreferenceEditorConfigStorageLocation : OptionStorageLocation2, IEditorConfigStorageLocation
    {
        public bool TryGetOption(StructuredAnalyzerConfigOptions options, Type type, out object result)
        {
            if (type == typeof(NamingStylePreferences))
            {
                var preferences = options.GetNamingStylePreferences();
                result = preferences;
                return !preferences.IsEmpty;
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }
    }
}
