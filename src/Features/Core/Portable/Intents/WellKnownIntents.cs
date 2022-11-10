// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Features.Intents
{
    /// <summary>
    /// The set of well known intents that Roslyn can calculate edits for.
    /// </summary>
    internal static class WellKnownIntents
    {
        public const string GenerateConstructor = nameof(GenerateConstructor);
        public const string AddConstructorParameter = nameof(AddConstructorParameter);
        public const string Rename = nameof(Rename);
        public const string DeleteParameter = nameof(DeleteParameter);
    }
}
