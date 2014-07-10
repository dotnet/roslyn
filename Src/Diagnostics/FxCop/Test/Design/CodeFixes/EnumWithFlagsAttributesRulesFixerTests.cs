// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EnumWithFlagsAttributesRulesFixerTests : CodeFixTestBase
    {
        protected override ICodeFixProvider GetBasicCodeFixProvider()
        {
            return new EnumWithFlagsBasicCodeFixProvider();
        }

        protected override IDiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicEnumWithFlagsDiagnosticAnalyzer();
        }

        protected override ICodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EnumWithFlagsCSharpCodeFixProvider();
        }

        protected override IDiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpEnumWithFlagsDiagnosticAnalyzer();
        }

        private static string GetCSharpCode_EnumWithFlagsAttributes(string code, bool hasFlags)
        {
            var stringToReplace = hasFlags ? "[System.Flags]" : "";
            return string.Format(code, stringToReplace);
        }

        private static string GetBasicCode_EnumWithFlagsAttributes(string code, bool hasFlags)
        {
            var stringToReplace = hasFlags ? "<System.Flags>" : "";
            return string.Format(code, stringToReplace);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_SimpleCase()
        {
            var code = @"{0}
public enum SimpleFlagsEnumClass
{{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4
}}

{0}
public enum HexFlagsEnumClass
{{
    One = 0x1,
    Two = 0x2,
    Four = 0x4,
    All = 0x7
}}";

            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);
            
            // Verify fixes for CA1027
            VerifyCSharpFix(codeWithoutFlags, codeWithFlags);
        }

        [WorkItem(902707)]
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_SimpleCase()
        {
            var code = @"{0}
Public Enum SimpleFlagsEnumClass
    Zero = 0
    One = 1
    Two = 2
    Four = 4
End Enum

{0}
Public Enum HexFlagsEnumClass
    One = &H1
    Two = &H2
    Four = &H4
    All = &H7
End Enum";

            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);
            
            // Verify fixes for CA1027
            VerifyBasicFix(codeWithoutFlags, codeWithFlags);
        }

        [WorkItem(823796)]
        [Fact(Skip = "Bug 823796"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_DuplicateValues()
        {
            string code = @"{0}
public enum DuplicateValuesEnumClass
{{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    AnotherFour = 4,
    ThreePlusOne = Two + One + One
}}
";

            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);
            
            // Verify fixes for CA1027
            VerifyCSharpFix(codeWithoutFlags, codeWithFlags);
        }

        [WorkItem(902707)]
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_DuplicateValues()
        {
            string code = @"{0}
Public Enum DuplicateValuesEnumClass
    Zero = 0
    One = 1
    Two = 2
    Four = 4
    AnotherFour = 4
    ThreePlusOne = Two + One + One
End Enum
";

            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);
            
            // Verify fixes for CA1027
            VerifyBasicFix(codeWithoutFlags, codeWithFlags);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_MissingPowerOfTwo()
        {
            string code = @"
{0}
public enum MissingPowerOfTwoEnumClass
{{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    Sixteen = 16
}}

{0}
public enum MultipleMissingPowerOfTwoEnumClass
{{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    ThirtyTwo = 32
}}

{0}
public enum AnotherTestValue
{{
    Value1 = 0,
    Value2 = 1,
    Value3 = 1,
    Value4 = 3
}}";

            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);

            var codeWithFlagsFix = @"
public enum MissingPowerOfTwoEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    Sixteen = 16
}

public enum MultipleMissingPowerOfTwoEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    ThirtyTwo = 32
}

public enum AnotherTestValue
{
    Value1 = 0,
    Value2 = 1,
    Value3 = 1,
    Value4 = 3
}";

            // Verify fixes for CA2217
            VerifyCSharpFix(codeWithFlags, codeWithFlagsFix);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_MissingPowerOfTwo()
        {
            string code = @"
{0}
Public Enum MissingPowerOfTwoEnumClass
	Zero = 0
	One = 1
	Two = 2
	Four = 4
	Sixteen = 16
End Enum

{0}
Public Enum MultipleMissingPowerOfTwoEnumClass
	Zero = 0
	One = 1
	Two = 2
	Four = 4
	ThirtyTwo = 32
End Enum

{0}
Public Enum AnotherTestValue
	Value1 = 0
	Value2 = 1
	Value3 = 1
	Value4 = 3
End Enum
";

            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);

            string codeWithFlagsFix = @"
Public Enum MissingPowerOfTwoEnumClass
	Zero = 0
	One = 1
	Two = 2
	Four = 4
	Sixteen = 16
End Enum

Public Enum MultipleMissingPowerOfTwoEnumClass
	Zero = 0
	One = 1
	Two = 2
	Four = 4
	ThirtyTwo = 32
End Enum

Public Enum AnotherTestValue
	Value1 = 0
	Value2 = 1
	Value3 = 1
	Value4 = 3
End Enum
";

            // Verify fixes for CA2217
            VerifyBasicFix(codeWithFlags, codeWithFlagsFix);
        }
    }
}
