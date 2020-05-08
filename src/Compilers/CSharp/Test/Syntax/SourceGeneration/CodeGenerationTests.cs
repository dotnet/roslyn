// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        private static ITypeSymbol Int32 = SpecialType(SpecialType.System_Int32);
        private static ITypeSymbol Boolean = SpecialType(SpecialType.System_Boolean);
        private static ITypeSymbol Void = SpecialType(SpecialType.System_Void);
    }
}
