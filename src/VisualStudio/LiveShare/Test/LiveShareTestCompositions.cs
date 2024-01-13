// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    internal static class LiveShareTestCompositions
    {
        public static readonly TestComposition Features = EditorTestCompositions.LanguageServerProtocol
            .AddAssemblies(
                typeof(LiveShareResources).Assembly);
    }
}
