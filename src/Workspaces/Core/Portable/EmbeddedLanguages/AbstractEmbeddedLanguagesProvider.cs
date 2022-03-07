// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguagesProvider : IEmbeddedLanguagesProvider
    {
        public virtual ImmutableArray<IEmbeddedLanguage> Languages { get; }

        protected AbstractEmbeddedLanguagesProvider()
        {
            Languages = ImmutableArray<IEmbeddedLanguage>.Empty;
        }
    }
}
