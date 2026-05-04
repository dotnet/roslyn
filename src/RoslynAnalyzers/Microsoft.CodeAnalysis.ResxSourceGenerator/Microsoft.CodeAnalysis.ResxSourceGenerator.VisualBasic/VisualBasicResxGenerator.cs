// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.VisualBasic
{
    [Generator(LanguageNames.VisualBasic)]
    internal sealed class VisualBasicResxGenerator : AbstractResxGenerator
    {
        protected override bool SupportsNullable(Compilation compilation)
        {
            return false;
        }
    }
}
