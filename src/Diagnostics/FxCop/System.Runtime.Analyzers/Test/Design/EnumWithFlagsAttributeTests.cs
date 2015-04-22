// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public class EnumWithFlagsAttributeTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new EnumWithFlagsAttributeAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EnumWithFlagsAttributeAnalyzer();
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

            // Verify CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyCSharp(codeWithoutFlags,
                GetCA1027CSharpResultAt(2, 13, "SimpleFlagsEnumClass"),
                GetCA1027CSharpResultAt(11, 13, "HexFlagsEnumClass"));

            // Verify no CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyCSharp(codeWithFlags);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_SimpleCaseWithScope()
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
[|public enum HexFlagsEnumClass
{{
    One = 0x1,
    Two = 0x2,
    Four = 0x4,
    All = 0x7
}}|]";

            // Verify CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyCSharp(codeWithoutFlags,
                GetCA1027CSharpResultAt(11, 13, "HexFlagsEnumClass"));
        }

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

            // Verify CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyBasic(codeWithoutFlags,
                GetCA1027BasicResultAt(2, 13, "SimpleFlagsEnumClass"),
                GetCA1027BasicResultAt(10, 13, "HexFlagsEnumClass"));

            // Verify no CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyBasic(codeWithFlags);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_SimpleCaseWithScope()
        {
            var code = @"{0}
Public Enum SimpleFlagsEnumClass
    Zero = 0
    One = 1
    Two = 2
    Four = 4
End Enum

{0}
[|Public Enum HexFlagsEnumClass
    One = &H1
    Two = &H2
    Four = &H4
    All = &H7
End Enum|]";

            // Verify CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyBasic(codeWithoutFlags,
                GetCA1027BasicResultAt(10, 13, "HexFlagsEnumClass"));
        }

        [WorkItem(823796, "DevDiv")]
        [Fact(Skip = "823796"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
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

            // Verify CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyCSharp(codeWithoutFlags,
                GetCA1027CSharpResultAt(2, 13, "DuplicateValuesEnumClass"));

            // Verify no CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyCSharp(codeWithFlags);
        }

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

            // Verify CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyBasic(codeWithoutFlags,
                GetCA1027BasicResultAt(2, 13, "DuplicateValuesEnumClass"));

            // Verify no CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyBasic(codeWithFlags);
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

            // Verify no CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyCSharp(codeWithoutFlags);

            // Verify CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyCSharp(codeWithFlags,
                GetCA2217CSharpResultAt(3, 13, "MissingPowerOfTwoEnumClass", "8"),
                GetCA2217CSharpResultAt(13, 13, "MultipleMissingPowerOfTwoEnumClass", "8, 16"),
                GetCA2217CSharpResultAt(23, 13, "AnotherTestValue", "2"));
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

            // Verify no CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyBasic(codeWithoutFlags);

            // Verify CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyBasic(codeWithFlags,
                GetCA2217BasicResultAt(3, 13, "MissingPowerOfTwoEnumClass", "8"),
                GetCA2217BasicResultAt(12, 13, "MultipleMissingPowerOfTwoEnumClass", "8, 16"),
                GetCA2217BasicResultAt(21, 13, "AnotherTestValue", "2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_ContiguousValues()
        {
            var code = @"
{0}
public enum ContiguousEnumClass
{{
    Zero = 0,
    One = 1,
    Two = 2
}}

{0}
public enum ContiguousEnumClass2
{{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5
}}

{0}
public enum ValuesNotDeclaredEnumClass
{{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five
}}

{0}
public enum ShortUnderlyingType: short
{{
    Zero = 0,
    One,
    Two,
    Three,
    Four,
    Five
}}";

            // Verify no CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyCSharp(codeWithoutFlags);

            // Verify no CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyCSharp(codeWithFlags);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_ContiguousValues()
        {
            var code = @"
{0}
Public Enum ContiguousEnumClass
	Zero = 0
	One = 1
	Two = 2
End Enum

{0}
Public Enum ContiguousEnumClass2
	Zero = 0
	One = 1
	Two = 2
	Three = 3
	Four = 4
	Five = 5
End Enum

{0}
Public Enum ValuesNotDeclaredEnumClass
	Zero
	One
	Two
	Three
	Four
	Five
End Enum

{0}
Public Enum ShortUnderlyingType As Short
	Zero = 0
	One
	Two
	Three
	Four
	Five
End Enum
";

            // Verify no CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyBasic(codeWithoutFlags);

            // Verify no CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyBasic(codeWithFlags);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_NonSimpleFlags()
        {
            var code = @"
{0}
public enum NonSimpleFlagEnumClass
{{
    Zero = 0x0,      // 0000
    One = 0x1,      // 0001
    Two = 0x2,      // 0010
    Eight = 0x8,    // 1000
    Twelve = 0xC,   // 1100
    HighValue = -1    // will be cast to UInt32.MaxValue, then zero-extended to UInt64
}}

{0}
public enum BitValuesClass
{{
    None = 0x0,
    One = 0x1,      // 0001
    Two = 0x2,      // 0010
    Eight = 0x8,    // 1000
    Twelve = 0xC,   // 1100
}}

{0}
public enum LabelsClass
{{
    None = 0,
    One = 1,
    Four = 4,
    Six = 6,
    Seven = 7
}}";

            // Verify no CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyCSharp(codeWithoutFlags);

            // Verify CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetCSharpCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyCSharp(codeWithFlags,
                GetCA2217CSharpResultAt(3, 13, "NonSimpleFlagEnumClass", "4, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824, 2147483648, 4294967296, 8589934592, 17179869184, 34359738368, 68719476736, 137438953472, 274877906944, 549755813888, 1099511627776, 2199023255552, 4398046511104, 8796093022208, 17592186044416, 35184372088832, 70368744177664, 140737488355328, 281474976710656, 562949953421312, 1125899906842624, 2251799813685248, 4503599627370496, 9007199254740992, 18014398509481984, 36028797018963968, 72057594037927936, 144115188075855872, 288230376151711744, 576460752303423488, 1152921504606846976, 2305843009213693952, 4611686018427387904, 9223372036854775808"),
                GetCA2217CSharpResultAt(14, 13, "BitValuesClass", "4"),
                GetCA2217CSharpResultAt(24, 13, "LabelsClass", "2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_NonSimpleFlags()
        {
            var code = @"
{0}
Public Enum NonSimpleFlagEnumClass
	Zero = &H0     ' 0000
	One = &H1      ' 0001
	Two = &H2      ' 0010
	Eight = &H8    ' 1000
	Twelve = &Hc   ' 1100
	HighValue = -1 ' will be cast to UInt32.MaxValue, then zero-extended to UInt64
End Enum

{0}
Public Enum BitValuesClass
	None = &H0
	One = &H1    ' 0001
	Two = &H2    ' 0010
	Eight = &H8  ' 1000
	Twelve = &Hc ' 1100
End Enum

{0}
Public Enum LabelsClass
	None = 0
	One = 1
	Four = 4
	Six = 6
	Seven = 7
End Enum
";

            // Verify no CA1027: Mark enums with FlagsAttribute
            var codeWithoutFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: false);
            VerifyBasic(codeWithoutFlags);

            // Verify CA2217: Do not mark enums with FlagsAttribute
            var codeWithFlags = GetBasicCode_EnumWithFlagsAttributes(code, hasFlags: true);
            VerifyBasic(codeWithFlags,
                GetCA2217BasicResultAt(3, 13, "NonSimpleFlagEnumClass", "4, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824, 2147483648, 4294967296, 8589934592, 17179869184, 34359738368, 68719476736, 137438953472, 274877906944, 549755813888, 1099511627776, 2199023255552, 4398046511104, 8796093022208, 17592186044416, 35184372088832, 70368744177664, 140737488355328, 281474976710656, 562949953421312, 1125899906842624, 2251799813685248, 4503599627370496, 9007199254740992, 18014398509481984, 36028797018963968, 72057594037927936, 144115188075855872, 288230376151711744, 576460752303423488, 1152921504606846976, 2305843009213693952, 4611686018427387904, 9223372036854775808"),
                GetCA2217BasicResultAt(13, 13, "BitValuesClass", "4"),
                GetCA2217BasicResultAt(22, 13, "LabelsClass", "2"));
        }

        private static DiagnosticResult GetCA1027CSharpResultAt(int line, int column, string enumTypeName)
        {
            return GetCSharpResultAt(line, column, EnumWithFlagsAttributeAnalyzer.RuleIdMarkEnumsWithFlags, string.Format(SystemRuntimeAnalyzersResources.MarkEnumsWithFlagsMessage, enumTypeName));
        }

        private static DiagnosticResult GetCA1027BasicResultAt(int line, int column, string enumTypeName)
        {
            return GetBasicResultAt(line, column, EnumWithFlagsAttributeAnalyzer.RuleIdMarkEnumsWithFlags, string.Format(SystemRuntimeAnalyzersResources.MarkEnumsWithFlagsMessage, enumTypeName));
        }

        private static DiagnosticResult GetCA2217CSharpResultAt(int line, int column, string enumTypeName, string missingValuesString)
        {
            return GetCSharpResultAt(line, column, EnumWithFlagsAttributeAnalyzer.RuleIdDoNotMarkEnumsWithFlags, string.Format(SystemRuntimeAnalyzersResources.DoNotMarkEnumsWithFlagsMessage, enumTypeName, missingValuesString));
        }

        private static DiagnosticResult GetCA2217BasicResultAt(int line, int column, string enumTypeName, string missingValuesString)
        {
            return GetBasicResultAt(line, column, EnumWithFlagsAttributeAnalyzer.RuleIdDoNotMarkEnumsWithFlags, string.Format(SystemRuntimeAnalyzersResources.DoNotMarkEnumsWithFlagsMessage, enumTypeName, missingValuesString));
        }
    }
}
