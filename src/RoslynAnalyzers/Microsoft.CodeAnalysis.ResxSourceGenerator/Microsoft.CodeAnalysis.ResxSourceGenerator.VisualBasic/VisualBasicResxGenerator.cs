// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
