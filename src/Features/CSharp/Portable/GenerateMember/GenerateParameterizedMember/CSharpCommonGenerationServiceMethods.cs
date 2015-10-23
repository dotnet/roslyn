// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateParameterizedMember
{
    internal static class CSharpCommonGenerationServiceMethods
    {
        public static bool AreSpecialOptionsActive(SemanticModel semanticModel)
        {
            return false;
        }

        public static bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel)
        {
            return false;
        }
    }
}
