// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public class EnumWithFlagsAttributeFixerTests : CodeFixTestBase
    {
        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new EnumWithFlagsAttributeFixer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new EnumWithFlagsAttributeAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EnumWithFlagsAttributeFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EnumWithFlagsAttributeAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_SimpleCase()
        {
            var code = @"
public enum SimpleFlagsEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4
}

public enum HexFlagsEnumClass
{
    One = 0x1,
    Two = 0x2,
    Four = 0x4,
    All = 0x7
}";

            var expected = @"
[System.Flags]
public enum SimpleFlagsEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4
}

[System.Flags]
public enum HexFlagsEnumClass
{
    One = 0x1,
    Two = 0x2,
    Four = 0x4,
    All = 0x7
}";

            // Verify fixes for CA1027
            VerifyCSharpFix(code, expected);
        }

        [WorkItem(902707)]
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_SimpleCase()
        {
            var code = @"
Public Enum SimpleFlagsEnumClass
    Zero = 0
    One = 1
    Two = 2
    Four = 4
End Enum

Public Enum HexFlagsEnumClass
    One = &H1
    Two = &H2
    Four = &H4
    All = &H7
End Enum";

            var expected = @"
<System.Flags>
Public Enum SimpleFlagsEnumClass
    Zero = 0
    One = 1
    Two = 2
    Four = 4
End Enum

<System.Flags>
Public Enum HexFlagsEnumClass
    One = &H1
    Two = &H2
    Four = &H4
    All = &H7
End Enum";

            // Verify fixes for CA1027
            VerifyBasicFix(code, expected);
        }

        [WorkItem(823796)]
        [Fact /*(Skip = "Bug 823796")*/, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_DuplicateValues()
        {
            string code = @"
public enum DuplicateValuesEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    AnotherFour = 4,
    ThreePlusOne = Two + One + One
}
";

            string expected = @"
[System.Flags]
public enum DuplicateValuesEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    AnotherFour = 4,
    ThreePlusOne = Two + One + One
}
";

            // Verify fixes for CA1027
            VerifyCSharpFix(code, expected);
        }

        [WorkItem(902707)]
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_DuplicateValues()
        {
            string code = @"
Public Enum DuplicateValuesEnumClass
    Zero = 0
    One = 1
    Two = 2
    Four = 4
    AnotherFour = 4
    ThreePlusOne = Two + One + One
End Enum
";

            string expected = @"
<System.Flags>
Public Enum DuplicateValuesEnumClass
    Zero = 0
    One = 1
    Two = 2
    Four = 4
    AnotherFour = 4
    ThreePlusOne = Two + One + One
End Enum
";

            // Verify fixes for CA1027
            VerifyBasicFix(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumWithFlagsAttributes_MissingPowerOfTwo()
        {
            string code = @"
[System.Flags]
public enum MissingPowerOfTwoEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    Sixteen = 16
}

[System.Flags]
public enum MultipleMissingPowerOfTwoEnumClass
{
    Zero = 0,
    One = 1,
    Two = 2,
    Four = 4,
    ThirtyTwo = 32
}

[System.Flags]
public enum AnotherTestValue
{
    Value1 = 0,
    Value2 = 1,
    Value3 = 1,
    Value4 = 3
}";

            var expected = @"
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
            VerifyCSharpFix(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumWithFlagsAttributes_MissingPowerOfTwo()
        {
            string code = @"
<System.Flags>
Public Enum MissingPowerOfTwoEnumClass
	Zero = 0
	One = 1
	Two = 2
	Four = 4
	Sixteen = 16
End Enum

<System.Flags>
Public Enum MultipleMissingPowerOfTwoEnumClass
	Zero = 0
	One = 1
	Two = 2
	Four = 4
	ThirtyTwo = 32
End Enum

<System.Flags>
Public Enum AnotherTestValue
	Value1 = 0
	Value2 = 1
	Value3 = 1
	Value4 = 3
End Enum
";

            string expected = @"
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
            VerifyBasicFix(code, expected);
        }
    }
}
