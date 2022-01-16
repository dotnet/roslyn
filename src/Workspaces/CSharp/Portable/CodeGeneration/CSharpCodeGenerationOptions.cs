// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal sealed class CSharpCodeGenerationOptions : CodeGenerationOptions
    {
        private readonly CSharpCodeGenerationPreferences _preferences;

        public CSharpCodeGenerationOptions(CodeGenerationContext Context, CSharpCodeGenerationPreferences Preferences)
            : base(Context)
        {
            _preferences = Preferences;
        }

        public new CSharpCodeGenerationPreferences Preferences
            => _preferences;

        protected override CodeGenerationPreferences PreferencesImpl
            => _preferences;

        public new CSharpCodeGenerationOptions WithContext(CodeGenerationContext value)
            => (Context == value) ? this : new(value, Preferences);

        protected override CodeGenerationOptions WithContextImpl(CodeGenerationContext value)
            => WithContext(value);

        public static new async ValueTask<CSharpCodeGenerationOptions> FromDocumentAsync(CodeGenerationContext context, Document document, CancellationToken cancellationToken)
            => new(context, await CSharpCodeGenerationPreferences.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false));
    }
}
