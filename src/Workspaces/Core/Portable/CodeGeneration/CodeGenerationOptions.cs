// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// Context and preferences.
    /// </summary>
    internal abstract class CodeGenerationOptions
    {
        public readonly CodeGenerationContext Context;

        protected CodeGenerationOptions(CodeGenerationContext context)
        {
            Context = context;
        }

        public static async ValueTask<CodeGenerationOptions> FromDocumentAsync(CodeGenerationContext context, Document document, CancellationToken cancellationToken)
        {
            var preferences = await CodeGenerationPreferences.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            return preferences.GetOptions(context);
        }

        public CodeGenerationOptions WithContext(CodeGenerationContext value)
            => WithContextImpl(value);

        public CodeGenerationPreferences Preferences => PreferencesImpl;

        protected abstract CodeGenerationPreferences PreferencesImpl { get; }
        protected abstract CodeGenerationOptions WithContextImpl(CodeGenerationContext value);
    }
}
