// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Test.Utilities
{
    public static partial class VisualBasicCodeRefactoringVerifier<TRefactoring>
        where TRefactoring : CodeRefactoringProvider, new()
    {
        public class Test : VisualBasicCodeRefactoringTest<TRefactoring, DefaultVerifier>
        {
            public Test()
            {
                ReferenceAssemblies = AdditionalMetadataReferences.Default;
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.VisualBasic15_5;

            protected override ParseOptions CreateParseOptions()
            {
                return ((VisualBasicParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }
        }
    }
}
