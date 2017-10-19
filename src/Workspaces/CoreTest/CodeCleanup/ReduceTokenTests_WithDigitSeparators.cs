// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    public class ReduceTokenTests_WithDigitSeparators
    {
        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceSingleLiterals_LessThan8Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 8 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 5 significant digits
        Const f_5_1 As Single = .149_95F         ' Dev11 & Roslyn: Pretty listed to 0.14995F
        Const f_5_2 As Single = 0.149_95f        ' Dev11 & Roslyn: Unchanged
        Const f_5_3 As Single = 1.499_5F         ' Dev11 & Roslyn: Unchanged
        Const f_5_4 As Single = 149.95f          ' Dev11 & Roslyn: Unchanged
        Const f_5_5 As Single = 1_499.5F         ' Dev11 & Roslyn: Unchanged
        Const f_5_6 As Single = 14_995.0f        ' Dev11 & Roslyn: Unchanged

        ' 7 significant digits
        Const f_7_1 As Single = .149_999_5F      ' Dev11 & Roslyn: Pretty listed to 0.1499995F
        Const f_7_2 As Single = 0.149_999_5f     ' Dev11 & Roslyn: Unchanged
        Const f_7_3 As Single = 1.499_995F       ' Dev11 & Roslyn: Unchanged
        Const f_7_4 As Single = 1_499.995f       ' Dev11 & Roslyn: Unchanged
        Const f_7_5 As Single = 149_999.5F       ' Dev11 & Roslyn: Unchanged
        Const f_7_6 As Single = 1_499_995.0f     ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_5_1)
        Console.WriteLine(f_5_2)
        Console.WriteLine(f_5_3)
        Console.WriteLine(f_5_4)
        Console.WriteLine(f_5_5)
        Console.WriteLine(f_5_6)

        Console.WriteLine(f_7_1)
        Console.WriteLine(f_7_2)
        Console.WriteLine(f_7_3)
        Console.WriteLine(f_7_4)
        Console.WriteLine(f_7_5)
        Console.WriteLine(f_7_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 8 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 5 significant digits
        Const f_5_1 As Single = .149_95F         ' Dev11 & Roslyn: Pretty listed to 0.14995F
        Const f_5_2 As Single = 0.149_95F        ' Dev11 & Roslyn: Unchanged
        Const f_5_3 As Single = 1.499_5F         ' Dev11 & Roslyn: Unchanged
        Const f_5_4 As Single = 149.95F          ' Dev11 & Roslyn: Unchanged
        Const f_5_5 As Single = 1_499.5F         ' Dev11 & Roslyn: Unchanged
        Const f_5_6 As Single = 14_995.0F        ' Dev11 & Roslyn: Unchanged

        ' 7 significant digits
        Const f_7_1 As Single = .149_999_5F      ' Dev11 & Roslyn: Pretty listed to 0.1499995F
        Const f_7_2 As Single = 0.149_999_5F     ' Dev11 & Roslyn: Unchanged
        Const f_7_3 As Single = 1.499_995F       ' Dev11 & Roslyn: Unchanged
        Const f_7_4 As Single = 1_499.995F       ' Dev11 & Roslyn: Unchanged
        Const f_7_5 As Single = 149_999.5F       ' Dev11 & Roslyn: Unchanged
        Const f_7_6 As Single = 1_499_995.0F     ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_5_1)
        Console.WriteLine(f_5_2)
        Console.WriteLine(f_5_3)
        Console.WriteLine(f_5_4)
        Console.WriteLine(f_5_5)
        Console.WriteLine(f_5_6)

        Console.WriteLine(f_7_1)
        Console.WriteLine(f_7_2)
        Console.WriteLine(f_7_3)
        Console.WriteLine(f_7_4)
        Console.WriteLine(f_7_5)
        Console.WriteLine(f_7_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceSingleLiterals_LessThan8Digits_WithTypeCharacterSingle()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 8 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 5 significant digits
        Const f_5_1 As Single = .149_95!         ' Dev11 & Roslyn: Pretty listed to 0.14995!
        Const f_5_2 As Single = 0.149_95!        ' Dev11 & Roslyn: Unchanged
        Const f_5_3 As Single = 1.499_5!         ' Dev11 & Roslyn: Unchanged
        Const f_5_4 As Single = 149.95!          ' Dev11 & Roslyn: Unchanged
        Const f_5_5 As Single = 1_499.5!         ' Dev11 & Roslyn: Unchanged
        Const f_5_6 As Single = 14_995.0!        ' Dev11 & Roslyn: Unchanged

        ' 7 significant digits
        Const f_7_1 As Single = .149_999_5!      ' Dev11 & Roslyn: Pretty listed to 0.1499995!
        Const f_7_2 As Single = 0.149_999_5!     ' Dev11 & Roslyn: Unchanged
        Const f_7_3 As Single = 1.499_995!       ' Dev11 & Roslyn: Unchanged
        Const f_7_4 As Single = 1_499.995!       ' Dev11 & Roslyn: Unchanged
        Const f_7_5 As Single = 149_999.5!       ' Dev11 & Roslyn: Unchanged
        Const f_7_6 As Single = 1_499_995.0!     ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_5_1)
        Console.WriteLine(f_5_2)
        Console.WriteLine(f_5_3)
        Console.WriteLine(f_5_4)
        Console.WriteLine(f_5_5)
        Console.WriteLine(f_5_6)

        Console.WriteLine(f_7_1)
        Console.WriteLine(f_7_2)
        Console.WriteLine(f_7_3)
        Console.WriteLine(f_7_4)
        Console.WriteLine(f_7_5)
        Console.WriteLine(f_7_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 8 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 5 significant digits
        Const f_5_1 As Single = .149_95!         ' Dev11 & Roslyn: Pretty listed to 0.14995!
        Const f_5_2 As Single = 0.149_95!        ' Dev11 & Roslyn: Unchanged
        Const f_5_3 As Single = 1.499_5!         ' Dev11 & Roslyn: Unchanged
        Const f_5_4 As Single = 149.95!          ' Dev11 & Roslyn: Unchanged
        Const f_5_5 As Single = 1_499.5!         ' Dev11 & Roslyn: Unchanged
        Const f_5_6 As Single = 14_995.0!        ' Dev11 & Roslyn: Unchanged

        ' 7 significant digits
        Const f_7_1 As Single = .149_999_5!      ' Dev11 & Roslyn: Pretty listed to 0.1499995!
        Const f_7_2 As Single = 0.149_999_5!     ' Dev11 & Roslyn: Unchanged
        Const f_7_3 As Single = 1.499_995!       ' Dev11 & Roslyn: Unchanged
        Const f_7_4 As Single = 1_499.995!       ' Dev11 & Roslyn: Unchanged
        Const f_7_5 As Single = 149_999.5!       ' Dev11 & Roslyn: Unchanged
        Const f_7_6 As Single = 1_499_995.0!     ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_5_1)
        Console.WriteLine(f_5_2)
        Console.WriteLine(f_5_3)
        Console.WriteLine(f_5_4)
        Console.WriteLine(f_5_5)
        Console.WriteLine(f_5_6)

        Console.WriteLine(f_7_1)
        Console.WriteLine(f_7_2)
        Console.WriteLine(f_7_3)
        Console.WriteLine(f_7_4)
        Console.WriteLine(f_7_5)
        Console.WriteLine(f_7_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceSingleLiterals_8Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

        Const f_8_1 As Single = .149_997_95F      ' (01) Dev11 & Roslyn: 0.14999795F
        Const f_8_2 As Single = .149_997_97f      ' (02) Dev11 & Roslyn: 0.149997965F

        Const f_8_3 As Single = 0.149_979_7F      ' (03) Dev11 & Roslyn: Unchanged

        Const f_8_4 As Single = 1.499_979_4f      ' (04) Dev11 & Roslyn: 1.49997938F
        Const f_8_5 As Single = 1.499_979_7F      ' (05) Dev11 & Roslyn: 1.49997973F

        Const f_8_6 As Single = 1_499.979_4f      ' (06) Dev11 & Roslyn: 1499.97937F

        Const f_8_7 As Single = 1_499_979.7F      ' (07) Dev11 & Roslyn: 1499979.75F

        Const f_8_8 As Single = 14_999_797.0F     ' (08) Dev11 & Roslyn: unchanged

        Console.WriteLine(f_8_1)
        Console.WriteLine(f_8_2)
        Console.WriteLine(f_8_3)
        Console.WriteLine(f_8_4)
        Console.WriteLine(f_8_5)
        Console.WriteLine(f_8_6)
        Console.WriteLine(f_8_7)
        Console.WriteLine(f_8_8)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

        Const f_8_1 As Single = .149_997_95F      ' (01) Dev11 & Roslyn: 0.14999795F
        Const f_8_2 As Single = .149_997_97F      ' (02) Dev11 & Roslyn: 0.149997965F

        Const f_8_3 As Single = 0.149_979_7F      ' (03) Dev11 & Roslyn: Unchanged

        Const f_8_4 As Single = 1.499_979_4F      ' (04) Dev11 & Roslyn: 1.49997938F
        Const f_8_5 As Single = 1.499_979_7F      ' (05) Dev11 & Roslyn: 1.49997973F

        Const f_8_6 As Single = 1_499.979_4F      ' (06) Dev11 & Roslyn: 1499.97937F

        Const f_8_7 As Single = 1_499_979.7F      ' (07) Dev11 & Roslyn: 1499979.75F

        Const f_8_8 As Single = 14_999_797.0F     ' (08) Dev11 & Roslyn: unchanged

        Console.WriteLine(f_8_1)
        Console.WriteLine(f_8_2)
        Console.WriteLine(f_8_3)
        Console.WriteLine(f_8_4)
        Console.WriteLine(f_8_5)
        Console.WriteLine(f_8_6)
        Console.WriteLine(f_8_7)
        Console.WriteLine(f_8_8)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        public async Task ReduceSingleLiterals_8Digits_WithTypeCharacterSingle()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

        Const f_8_1 As Single = .149_997_95!      ' (01) Dev11 & Roslyn: 0.14999795F
        Const f_8_2 As Single = .149_997_97!      ' (02) Dev11 & Roslyn: 0.149997965F

        Const f_8_3 As Single = 0.149_979_7!      ' (03) Dev11 & Roslyn: Unchanged

        Const f_8_4 As Single = 1.499_979_4!      ' (04) Dev11 & Roslyn: 1.49997938F
        Const f_8_5 As Single = 1.499_979_7!      ' (05) Dev11 & Roslyn: 1.49997973F

        Const f_8_6 As Single = 1_499.979_4!      ' (06) Dev11 & Roslyn: 1499.97937F

        Const f_8_7 As Single = 1_499_979.7!      ' (07) Dev11 & Roslyn: 1499979.75F

        Const f_8_8 As Single = 14_999_797.0!     ' (08) Dev11 & Roslyn: unchanged

        Console.WriteLine(f_8_1)
        Console.WriteLine(f_8_2)
        Console.WriteLine(f_8_3)
        Console.WriteLine(f_8_4)
        Console.WriteLine(f_8_5)
        Console.WriteLine(f_8_6)
        Console.WriteLine(f_8_7)
        Console.WriteLine(f_8_8)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

        Const f_8_1 As Single = .149_997_95!      ' (01) Dev11 & Roslyn: 0.14999795F
        Const f_8_2 As Single = .149_997_97!      ' (02) Dev11 & Roslyn: 0.149997965F

        Const f_8_3 As Single = 0.149_979_7!      ' (03) Dev11 & Roslyn: Unchanged

        Const f_8_4 As Single = 1.499_979_4!      ' (04) Dev11 & Roslyn: 1.49997938F
        Const f_8_5 As Single = 1.499_979_7!      ' (05) Dev11 & Roslyn: 1.49997973F

        Const f_8_6 As Single = 1_499.979_4!      ' (06) Dev11 & Roslyn: 1499.97937F

        Const f_8_7 As Single = 1_499_979.7!      ' (07) Dev11 & Roslyn: 1499979.75F

        Const f_8_8 As Single = 14_999_797.0!     ' (08) Dev11 & Roslyn: unchanged

        Console.WriteLine(f_8_1)
        Console.WriteLine(f_8_2)
        Console.WriteLine(f_8_3)
        Console.WriteLine(f_8_4)
        Console.WriteLine(f_8_5)
        Console.WriteLine(f_8_6)
        Console.WriteLine(f_8_7)
        Console.WriteLine(f_8_8)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        public async Task ReduceSingleLiterals_GreaterThan8Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits
        
        ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
        Const f_9_1 As Single = .149_997_938F      ' (01) Dev11 & Roslyn: 0.149997935F
        Const f_9_2 As Single = 0.149_997_931f     ' (02) Dev11 & Roslyn: 0.149997935F
        Const f_9_3 As Single = 1.499_979_65F      ' (03) Dev11 & Roslyn: 1.49997962F

        Const f_10_1 As Single = 14_999.796_52f    ' (04) Dev11 & Roslyn: 14999.7969F

        ' (b) > 8 significant digits before decimal point.
        Const f_10_2 As Single = 149_997_965.2F    ' (05) Dev11 & Roslyn: 149997968.0F
        Const f_10_3 As Single = 1_499_979_652.0f  ' (06) Dev11 & Roslyn: 1.49997965E+9F

        Const f_24_1 As Single = 111_111_149_999_124_689_999.499F      ' (07) Dev11 & Roslyn: 1.11111148E+20F

        ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
        '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
        '     from 1.401298E-45 through 3.4028235E+38 for positive values.
        
        Const f_overflow_1 As Single = -3.402_823_5E+39F          ' (08) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Single = 3.402_823_5E+39F           ' (09) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Single = -1.401_298E-47F           ' (10) Dev11: -0.0F, Roslyn: Unchanged
        Const f_underflow_2 As Single = 1.401_298E-47F            ' (11) Dev11: 0.0F, Roslyn: Unchanged
        
        Console.WriteLine(f_9_1)
        Console.WriteLine(f_9_2)
        Console.WriteLine(f_9_3)
        Console.WriteLine(f_10_1)
        Console.WriteLine(f_10_2)
        Console.WriteLine(f_10_3)
        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

        ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
        Const f_9_1 As Single = .149_997_938F      ' (01) Dev11 & Roslyn: 0.149997935F
        Const f_9_2 As Single = 0.149_997_931F     ' (02) Dev11 & Roslyn: 0.149997935F
        Const f_9_3 As Single = 1.499_979_65F      ' (03) Dev11 & Roslyn: 1.49997962F

        Const f_10_1 As Single = 14_999.796_52F    ' (04) Dev11 & Roslyn: 14999.7969F

        ' (b) > 8 significant digits before decimal point.
        Const f_10_2 As Single = 149_997_965.2F    ' (05) Dev11 & Roslyn: 149997968.0F
        Const f_10_3 As Single = 1_499_979_652.0F  ' (06) Dev11 & Roslyn: 1.49997965E+9F

        Const f_24_1 As Single = 111_111_149_999_124_689_999.499F      ' (07) Dev11 & Roslyn: 1.11111148E+20F

        ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
        '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
        '     from 1.401298E-45 through 3.4028235E+38 for positive values.

        Const f_overflow_1 As Single = -3.402_823_5E+39F          ' (08) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Single = 3.402_823_5E+39F           ' (09) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Single = -1.401_298E-47F           ' (10) Dev11: -0.0F, Roslyn: Unchanged
        Const f_underflow_2 As Single = 1.401_298E-47F            ' (11) Dev11: 0.0F, Roslyn: Unchanged

        Console.WriteLine(f_9_1)
        Console.WriteLine(f_9_2)
        Console.WriteLine(f_9_3)
        Console.WriteLine(f_10_1)
        Console.WriteLine(f_10_2)
        Console.WriteLine(f_10_3)
        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceSingleLiterals_GreaterThan8Digits_WithTypeCharacterSingle()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits
        
        ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
        Const f_9_1 As Single = .149_997_938!           ' (01) Dev11 & Roslyn: 0.149997935F
        Const f_9_2 As Single = 0.149_997_931!          ' (02) Dev11 & Roslyn: 0.149997935F
        Const f_9_3 As Single = 1.499_979_65!           ' (03) Dev11 & Roslyn: 1.49997962F

        Const f_10_1 As Single = 14_999.796_52!         ' (04) Dev11 & Roslyn: 14999.7969F

        ' (b) > 8 significant digits before decimal point.
        Const f_10_2 As Single = 149_997_965.2!         ' (05) Dev11 & Roslyn: 149997968.0F
        Const f_10_3 As Single = 1_499_979_652.0!       ' (06) Dev11 & Roslyn: 1.49997965E+9F

        Const f_24_1 As Single = 111_111_149_999_124_689_999.499!      ' (07) Dev11 & Roslyn: 1.11111148E+20F

        ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
        '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
        '     from 1.401298E-45 through 3.4028235E+38 for positive values.
        
        Const f_overflow_1 As Single = -3.402_823_5E+39!          ' (08) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Single = 3.402_823_5E+39!           ' (09) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Single = -1.401_298E-47!           ' (10) Dev11: -0.0F, Roslyn: Unchanged
        Const f_underflow_2 As Single = 1.401_298E-47!            ' (11) Dev11: 0.0F, Roslyn: Unchanged

        Console.WriteLine(f_9_1)
        Console.WriteLine(f_9_2)
        Console.WriteLine(f_9_3)
        Console.WriteLine(f_10_1)
        Console.WriteLine(f_10_2)
        Console.WriteLine(f_10_3)
        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 8 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

        ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
        Const f_9_1 As Single = .149_997_938!           ' (01) Dev11 & Roslyn: 0.149997935F
        Const f_9_2 As Single = 0.149_997_931!          ' (02) Dev11 & Roslyn: 0.149997935F
        Const f_9_3 As Single = 1.499_979_65!           ' (03) Dev11 & Roslyn: 1.49997962F

        Const f_10_1 As Single = 14_999.796_52!         ' (04) Dev11 & Roslyn: 14999.7969F

        ' (b) > 8 significant digits before decimal point.
        Const f_10_2 As Single = 149_997_965.2!         ' (05) Dev11 & Roslyn: 149997968.0F
        Const f_10_3 As Single = 1_499_979_652.0!       ' (06) Dev11 & Roslyn: 1.49997965E+9F

        Const f_24_1 As Single = 111_111_149_999_124_689_999.499!      ' (07) Dev11 & Roslyn: 1.11111148E+20F

        ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
        '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
        '     from 1.401298E-45 through 3.4028235E+38 for positive values.

        Const f_overflow_1 As Single = -3.402_823_5E+39!          ' (08) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Single = 3.402_823_5E+39!           ' (09) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Single = -1.401_298E-47!           ' (10) Dev11: -0.0F, Roslyn: Unchanged
        Const f_underflow_2 As Single = 1.401_298E-47!            ' (11) Dev11: 0.0F, Roslyn: Unchanged

        Console.WriteLine(f_9_1)
        Console.WriteLine(f_9_2)
        Console.WriteLine(f_9_3)
        Console.WriteLine(f_10_1)
        Console.WriteLine(f_10_2)
        Console.WriteLine(f_10_3)
        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDoubleLiterals_LessThan16Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 16 significant digits precision,
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 13 significant digits
        Const f_13_1 As Double = .149_959_999_999_9         ' Dev11 & Roslyn: Pretty listed to 0.1499599999999
        Const f_13_2 As Double = 0.149_959_999_999          ' Dev11 & Roslyn: Unchanged
        Const f_13_3 As Double = 1.499_599_999_999          ' Dev11 & Roslyn: Unchanged
        Const f_13_4 As Double = 1_499_599.999_999          ' Dev11 & Roslyn: Unchanged
        Const f_13_5 As Double = 149_959_999_999.9          ' Dev11 & Roslyn: Unchanged
        Const f_13_6 As Double = 1_499_599_999_999.0        ' Dev11 & Roslyn: Unchanged

        ' 15 significant digits
        Const f_15_1 As Double = .149_999_999_999_995       ' Dev11 & Roslyn: Pretty listed to 0.149999999999995
        Const f_15_2 As Double = 0.149_999_999_999_95       ' Dev11 & Roslyn: Unchanged
        Const f_15_3 As Double = 1.499_999_999_999_95       ' Dev11 & Roslyn: Unchanged
        Const f_15_4 As Double = 14_999_999.999_999_5       ' Dev11 & Roslyn: Unchanged
        Const f_15_5 As Double = 14_999_999_999_999.5       ' Dev11 & Roslyn: Unchanged
        Const f_15_6 As Double = 149_999_999_999_995.0      ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_13_1)
        Console.WriteLine(f_13_2)
        Console.WriteLine(f_13_3)
        Console.WriteLine(f_13_4)
        Console.WriteLine(f_13_5)
        Console.WriteLine(f_13_6)

        Console.WriteLine(f_15_1)
        Console.WriteLine(f_15_2)
        Console.WriteLine(f_15_3)
        Console.WriteLine(f_15_4)
        Console.WriteLine(f_15_5)
        Console.WriteLine(f_15_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 16 significant digits precision,
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 13 significant digits
        Const f_13_1 As Double = .149_959_999_999_9         ' Dev11 & Roslyn: Pretty listed to 0.1499599999999
        Const f_13_2 As Double = 0.149_959_999_999          ' Dev11 & Roslyn: Unchanged
        Const f_13_3 As Double = 1.499_599_999_999          ' Dev11 & Roslyn: Unchanged
        Const f_13_4 As Double = 1_499_599.999_999          ' Dev11 & Roslyn: Unchanged
        Const f_13_5 As Double = 149_959_999_999.9          ' Dev11 & Roslyn: Unchanged
        Const f_13_6 As Double = 1_499_599_999_999.0        ' Dev11 & Roslyn: Unchanged

        ' 15 significant digits
        Const f_15_1 As Double = .149_999_999_999_995       ' Dev11 & Roslyn: Pretty listed to 0.149999999999995
        Const f_15_2 As Double = 0.149_999_999_999_95       ' Dev11 & Roslyn: Unchanged
        Const f_15_3 As Double = 1.499_999_999_999_95       ' Dev11 & Roslyn: Unchanged
        Const f_15_4 As Double = 14_999_999.999_999_5       ' Dev11 & Roslyn: Unchanged
        Const f_15_5 As Double = 14_999_999_999_999.5       ' Dev11 & Roslyn: Unchanged
        Const f_15_6 As Double = 149_999_999_999_995.0      ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_13_1)
        Console.WriteLine(f_13_2)
        Console.WriteLine(f_13_3)
        Console.WriteLine(f_13_4)
        Console.WriteLine(f_13_5)
        Console.WriteLine(f_13_6)

        Console.WriteLine(f_15_1)
        Console.WriteLine(f_15_2)
        Console.WriteLine(f_15_3)
        Console.WriteLine(f_15_4)
        Console.WriteLine(f_15_5)
        Console.WriteLine(f_15_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDoubleLiterals_LessThan16Digits_WithTypeCharacter()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 16 significant digits precision,
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 13 significant digits
        Const f_13_1 As Double = .149_959_999_999_9R         ' (01) Dev11 & Roslyn: Pretty listed to 0.1499599999999
        Const f_13_2 As Double = 0.149_959_999_999r          ' (01) Dev11 & Roslyn: Unchanged
        Const f_13_3 As Double = 1.499_599_999_999#          ' (02) Dev11 & Roslyn: Unchanged
        Const f_13_4 As Double = 1_499_599.999_999#          ' (03) Dev11 & Roslyn: Unchanged
        Const f_13_5 As Double = 149_959_999_999.9r          ' (04) Dev11 & Roslyn: Unchanged
        Const f_13_6 As Double = 1_499_599_999_999.0R        ' (05) Dev11 & Roslyn: Unchanged

        ' 15 significant digits
        Const f_15_1 As Double = .149_999_999_999_995R       ' (06) Dev11 & Roslyn: Pretty listed to 0.149999999999995
        Const f_15_2 As Double = 0.149_999_999_999_95r       ' (07) Dev11 & Roslyn: Unchanged
        Const f_15_3 As Double = 1.499_999_999_9999_5#       ' (08) Dev11 & Roslyn: Unchanged
        Const f_15_4 As Double = 14_999_999.999_9995#        ' (09) Dev11 & Roslyn: Unchanged
        Const f_15_5 As Double = 14_999_999_999_999.5r       ' (10) Dev11 & Roslyn: Unchanged
        Const f_15_6 As Double = 149_999_999_999_995.0R      ' (11) Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_13_1)
        Console.WriteLine(f_13_2)
        Console.WriteLine(f_13_3)
        Console.WriteLine(f_13_4)
        Console.WriteLine(f_13_5)
        Console.WriteLine(f_13_6)

        Console.WriteLine(f_15_1)
        Console.WriteLine(f_15_2)
        Console.WriteLine(f_15_3)
        Console.WriteLine(f_15_4)
        Console.WriteLine(f_15_5)
        Console.WriteLine(f_15_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 16 significant digits precision,
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 13 significant digits
        Const f_13_1 As Double = .149_959_999_999_9R         ' (01) Dev11 & Roslyn: Pretty listed to 0.1499599999999
        Const f_13_2 As Double = 0.149_959_999_999R          ' (01) Dev11 & Roslyn: Unchanged
        Const f_13_3 As Double = 1.499_599_999_999#          ' (02) Dev11 & Roslyn: Unchanged
        Const f_13_4 As Double = 1_499_599.999_999#          ' (03) Dev11 & Roslyn: Unchanged
        Const f_13_5 As Double = 149_959_999_999.9R          ' (04) Dev11 & Roslyn: Unchanged
        Const f_13_6 As Double = 1_499_599_999_999.0R        ' (05) Dev11 & Roslyn: Unchanged

        ' 15 significant digits
        Const f_15_1 As Double = .149_999_999_999_995R       ' (06) Dev11 & Roslyn: Pretty listed to 0.149999999999995
        Const f_15_2 As Double = 0.149_999_999_999_95R       ' (07) Dev11 & Roslyn: Unchanged
        Const f_15_3 As Double = 1.499_999_999_9999_5#       ' (08) Dev11 & Roslyn: Unchanged
        Const f_15_4 As Double = 14_999_999.999_9995#        ' (09) Dev11 & Roslyn: Unchanged
        Const f_15_5 As Double = 14_999_999_999_999.5R       ' (10) Dev11 & Roslyn: Unchanged
        Const f_15_6 As Double = 149_999_999_999_995.0R      ' (11) Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_13_1)
        Console.WriteLine(f_13_2)
        Console.WriteLine(f_13_3)
        Console.WriteLine(f_13_4)
        Console.WriteLine(f_13_5)
        Console.WriteLine(f_13_6)

        Console.WriteLine(f_15_1)
        Console.WriteLine(f_15_2)
        Console.WriteLine(f_15_3)
        Console.WriteLine(f_15_4)
        Console.WriteLine(f_15_5)
        Console.WriteLine(f_15_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDoubleLiterals_16Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        Const f_16_1 As Double = .149_999_999_979_999_3     ' Dev11 & Roslyn: 0.1499999999799993
        Const f_16_2 As Double = .149_999_999_979_999_7     ' Dev11 & Roslyn: 0.14999999997999969

        Const f_16_3 As Double = 0.149_999_999_799_995      ' Dev11 & Roslyn: Unchanged

        Const f_16_4 As Double = 1.499_999_999_799_994      ' Dev11 & Roslyn: Unchanged
        Const f_16_5 As Double = 1.499_999_999_799_995      ' Dev11 & Roslyn: 1.4999999997999951

        Const f_16_6 As Double = 14_999_999.997_999_94      ' Dev11 & Roslyn: Unchanged
        Const f_16_7 As Double = 14_999_999.997_999_95      ' Dev11 & Roslyn: 14999999.997999949

        Const f_16_8 As Double = 149_999_999_997_999.2      ' Dev11 & Roslyn: 149999999997999.19
        Const f_16_9 As Double = 149_999_999_997_999.8      ' Dev11 & Roslyn: 149999999997999.81

        Const f_16_10 As Double = 1_499_999_999_979_995.0   ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_16_1)
        Console.WriteLine(f_16_2)
        Console.WriteLine(f_16_3)
        Console.WriteLine(f_16_4)
        Console.WriteLine(f_16_5)
        Console.WriteLine(f_16_6)
        Console.WriteLine(f_16_7)
        Console.WriteLine(f_16_8)
        Console.WriteLine(f_16_9)
        Console.WriteLine(f_16_10)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        Const f_16_1 As Double = .149_999_999_979_999_3     ' Dev11 & Roslyn: 0.1499999999799993
        Const f_16_2 As Double = .149_999_999_979_999_7     ' Dev11 & Roslyn: 0.14999999997999969

        Const f_16_3 As Double = 0.149_999_999_799_995      ' Dev11 & Roslyn: Unchanged

        Const f_16_4 As Double = 1.499_999_999_799_994      ' Dev11 & Roslyn: Unchanged
        Const f_16_5 As Double = 1.499_999_999_799_995      ' Dev11 & Roslyn: 1.4999999997999951

        Const f_16_6 As Double = 14_999_999.997_999_94      ' Dev11 & Roslyn: Unchanged
        Const f_16_7 As Double = 14_999_999.997_999_95      ' Dev11 & Roslyn: 14999999.997999949

        Const f_16_8 As Double = 149_999_999_997_999.2      ' Dev11 & Roslyn: 149999999997999.19
        Const f_16_9 As Double = 149_999_999_997_999.8      ' Dev11 & Roslyn: 149999999997999.81

        Const f_16_10 As Double = 1_499_999_999_979_995.0   ' Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_16_1)
        Console.WriteLine(f_16_2)
        Console.WriteLine(f_16_3)
        Console.WriteLine(f_16_4)
        Console.WriteLine(f_16_5)
        Console.WriteLine(f_16_6)
        Console.WriteLine(f_16_7)
        Console.WriteLine(f_16_8)
        Console.WriteLine(f_16_9)
        Console.WriteLine(f_16_10)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDoubleLiterals_16Digits_WithTypeCharacter()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        Const f_16_1 As Double = .149_999_999_979_999_3R      ' (00) Dev11 & Roslyn: 0.149999999979999399799993
        Const f_16_2 As Double = .149_999_999_979_999_7r      ' (01) Dev11 & Roslyn: 0.14999999997999969997999969

        Const f_16_3 As Double = 0.149_999_999_799_995#       ' (03) Dev11 & Roslyn: Unchanged

        Const f_16_4 As Double = 1.499_999_999_799_994R       ' (04) Dev11 & Roslyn: Unchanged
        Const f_16_5 As Double = 1.499_999_999_799_995r       ' (05) Dev11 & Roslyn: 1.4999999997999951951

        Const f_16_6 As Double = 14_999_999.997_999_94#       ' (06) Dev11 & Roslyn: Unchanged
        Const f_16_7 As Double = 14_999_999.997_999_95R       ' (07) Dev11 & Roslyn: 14999999.99799994997999949

        Const f_16_8 As Double = 149_999_999_997_999.2r       ' (08) Dev11 & Roslyn: 149999999997999.1997999.19
        Const f_16_9 As Double = 149_999_999_997_999.8#       ' (09) Dev11 & Roslyn: 149999999997999.8197999.81

        Const f_16_10 As Double = 1_499_999_999_979_995.0R    ' (10) Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_16_1)
        Console.WriteLine(f_16_2)
        Console.WriteLine(f_16_3)
        Console.WriteLine(f_16_4)
        Console.WriteLine(f_16_5)
        Console.WriteLine(f_16_6)
        Console.WriteLine(f_16_7)
        Console.WriteLine(f_16_8)
        Console.WriteLine(f_16_9)
        Console.WriteLine(f_16_10)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        Const f_16_1 As Double = .149_999_999_979_999_3R      ' (00) Dev11 & Roslyn: 0.149999999979999399799993
        Const f_16_2 As Double = .149_999_999_979_999_7R      ' (01) Dev11 & Roslyn: 0.14999999997999969997999969

        Const f_16_3 As Double = 0.149_999_999_799_995#       ' (03) Dev11 & Roslyn: Unchanged

        Const f_16_4 As Double = 1.499_999_999_799_994R       ' (04) Dev11 & Roslyn: Unchanged
        Const f_16_5 As Double = 1.499_999_999_799_995R       ' (05) Dev11 & Roslyn: 1.4999999997999951951

        Const f_16_6 As Double = 14_999_999.997_999_94#       ' (06) Dev11 & Roslyn: Unchanged
        Const f_16_7 As Double = 14_999_999.997_999_95R       ' (07) Dev11 & Roslyn: 14999999.99799994997999949

        Const f_16_8 As Double = 149_999_999_997_999.2R       ' (08) Dev11 & Roslyn: 149999999997999.1997999.19
        Const f_16_9 As Double = 149_999_999_997_999.8#       ' (09) Dev11 & Roslyn: 149999999997999.8197999.81

        Const f_16_10 As Double = 1_499_999_999_979_995.0R    ' (10) Dev11 & Roslyn: Unchanged

        Console.WriteLine(f_16_1)
        Console.WriteLine(f_16_2)
        Console.WriteLine(f_16_3)
        Console.WriteLine(f_16_4)
        Console.WriteLine(f_16_5)
        Console.WriteLine(f_16_6)
        Console.WriteLine(f_16_7)
        Console.WriteLine(f_16_8)
        Console.WriteLine(f_16_9)
        Console.WriteLine(f_16_10)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDoubleLiterals_GreaterThan16Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
        Const f_17_1 As Double = .149_999_999_979_999_38    ' (01) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_2 As Double = .149_999_999_979_999_39    ' (02) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_3 As Double = .149_999_999_979_999_37    ' (03) Dev11 & Roslyn: 0.14999999997999938

        Const f_17_4 As Double = 0.149_999_999_799_995_7    ' (04) Dev11 & Roslyn: Unchanged
        Const f_17_5 As Double = 0.149_999_999_799_995_8    ' (05) Dev11 & Roslyn: 0.14999999979999579

        Const f_17_6 As Double = 1.499_999_999_799_994_7    ' (06) Dev11 & Roslyn: Unchanged
        Const f_17_7 As Double = 1.499_999_999_799_994_5    ' (07) Dev11 & Roslyn: 1.4999999997999944
        Const f_17_8 As Double = 1.499_999_999_799_994_6    ' (08) Dev11 & Roslyn: 1.4999999997999947

        Const f_18_1 As Double = 14_999_999.997_999_945_9   ' (09) Dev11 & Roslyn: 14999999.997999946
        Const f_18_2 As Double = 14_999_999.997_999_945_1   ' (10) Dev11 & Roslyn: 14999999.997999946
        Const f_18_3 As Double = 14_999_999.997_999_945_4   ' (11) Dev11 & Roslyn: 14999999.997999946

        ' (b) > 16 significant digits before decimal point.
        Const f_18_4 As Double = 14_999_999_999_733_999.2   ' (12) Dev11 & Roslyn: 1.4999999999734E+16
        Const f_18_5 As Double = 14_999_999_999_379_995.0   ' (13) Dev11 & Roslyn: 14999999999379996.0

        Const f_24_1 As Double = 111_111_149_999_124_689_999.499     ' (14) Dev11 & Roslyn: 1.1111114999912469E+20

        ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
        '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
        '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

        Const f_overflow_1 As Double = -1.797_693_134_862_315_70E+309       ' (15) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Double = 1.797_693_134_862_315_70E+309        ' (16) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Double = -4.940_656_458_412_465_44E-326      ' (17) Dev11: -0.0F, Roslyn: unchanged
        Const f_underflow_2 As Double = 4.940_656_458_412_465_44E-326       ' (18) Dev11: 0.0F, Roslyn: unchanged

        Console.WriteLine(f_17_1)
        Console.WriteLine(f_17_2)
        Console.WriteLine(f_17_3)
        Console.WriteLine(f_17_4)
        Console.WriteLine(f_17_5)
        Console.WriteLine(f_17_6)
        Console.WriteLine(f_17_7)
        Console.WriteLine(f_17_8)
        
        Console.WriteLine(f_18_1)
        Console.WriteLine(f_18_2)
        Console.WriteLine(f_18_3)
        Console.WriteLine(f_18_4)
        Console.WriteLine(f_18_5)

        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
        Const f_17_1 As Double = .149_999_999_979_999_38    ' (01) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_2 As Double = .149_999_999_979_999_39    ' (02) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_3 As Double = .149_999_999_979_999_37    ' (03) Dev11 & Roslyn: 0.14999999997999938

        Const f_17_4 As Double = 0.149_999_999_799_995_7    ' (04) Dev11 & Roslyn: Unchanged
        Const f_17_5 As Double = 0.149_999_999_799_995_8    ' (05) Dev11 & Roslyn: 0.14999999979999579

        Const f_17_6 As Double = 1.499_999_999_799_994_7    ' (06) Dev11 & Roslyn: Unchanged
        Const f_17_7 As Double = 1.499_999_999_799_994_5    ' (07) Dev11 & Roslyn: 1.4999999997999944
        Const f_17_8 As Double = 1.499_999_999_799_994_6    ' (08) Dev11 & Roslyn: 1.4999999997999947

        Const f_18_1 As Double = 14_999_999.997_999_945_9   ' (09) Dev11 & Roslyn: 14999999.997999946
        Const f_18_2 As Double = 14_999_999.997_999_945_1   ' (10) Dev11 & Roslyn: 14999999.997999946
        Const f_18_3 As Double = 14_999_999.997_999_945_4   ' (11) Dev11 & Roslyn: 14999999.997999946

        ' (b) > 16 significant digits before decimal point.
        Const f_18_4 As Double = 14_999_999_999_733_999.2   ' (12) Dev11 & Roslyn: 1.4999999999734E+16
        Const f_18_5 As Double = 14_999_999_999_379_995.0   ' (13) Dev11 & Roslyn: 14999999999379996.0

        Const f_24_1 As Double = 111_111_149_999_124_689_999.499     ' (14) Dev11 & Roslyn: 1.1111114999912469E+20

        ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
        '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
        '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

        Const f_overflow_1 As Double = -1.797_693_134_862_315_70E+309       ' (15) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Double = 1.797_693_134_862_315_70E+309        ' (16) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Double = -4.940_656_458_412_465_44E-326      ' (17) Dev11: -0.0F, Roslyn: unchanged
        Const f_underflow_2 As Double = 4.940_656_458_412_465_44E-326       ' (18) Dev11: 0.0F, Roslyn: unchanged

        Console.WriteLine(f_17_1)
        Console.WriteLine(f_17_2)
        Console.WriteLine(f_17_3)
        Console.WriteLine(f_17_4)
        Console.WriteLine(f_17_5)
        Console.WriteLine(f_17_6)
        Console.WriteLine(f_17_7)
        Console.WriteLine(f_17_8)

        Console.WriteLine(f_18_1)
        Console.WriteLine(f_18_2)
        Console.WriteLine(f_18_3)
        Console.WriteLine(f_18_4)
        Console.WriteLine(f_18_5)

        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDoubleLiterals_GreaterThan16Digits_WithTypeCharacter()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
        Const f_17_1 As Double = .149_999_999_979_999_38R     ' (01) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_2 As Double = .149_999_999_979_999_39r     ' (02) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_3 As Double = .149_999_999_979_999_37#     ' (03) Dev11 & Roslyn: 0.14999999997999938

        Const f_17_4 As Double = 0.149_999_999_799_9957R      ' (04) Dev11 & Roslyn: Unchanged
        Const f_17_5 As Double = 0.149_999_999_799_9958r      ' (05) Dev11 & Roslyn: 0.14999999979999579

        Const f_17_6 As Double = 1.499_999_999_799_9947#      ' (06) Dev11 & Roslyn: Unchanged
        Const f_17_7 As Double = 1.499_999_999_799_9945R      ' (07) Dev11 & Roslyn: 1.4999999997999944
        Const f_17_8 As Double = 1.499_999_999_799_9946r      ' (08) Dev11 & Roslyn: 1.4999999997999947

        Const f_18_1 As Double = 14_999_999.997_999_945_9#    ' (09) Dev11 & Roslyn: 14999999.997999946
        Const f_18_2 As Double = 14_999_999.997_999_945_1R    ' (10) Dev11 & Roslyn: 14999999.997999946
        Const f_18_3 As Double = 14_999_999.997_999_945_4r    ' (11) Dev11 & Roslyn: 14999999.997999946

        ' (b) > 16 significant digits before decimal point.
        Const f_18_4 As Double = 14_999_999_999_733_999.2#    ' (12) Dev11 & Roslyn: 1.4999999999734E+16
        Const f_18_5 As Double = 14_999_999_999_379_995.0R    ' (13) Dev11 & Roslyn: 14999999999379996.0

        Const f_24_1 As Double = 1_111_111_499_991_246_899_99.499r      ' (14) Dev11 & Roslyn: 1.1111114999912469E+20

        ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
        '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
        '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

        Const f_overflow_1 As Double = -1.797_693_134_862_315_70E+309#        ' (15) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Double = 1.797_693_134_862_315_70E+309R         ' (16) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Double = -4.940_656_458_412_465_44E-326r       ' (17) Dev11: -0.0F, Roslyn: unchanged
        Const f_underflow_2 As Double = 4.940_656_458_412_465_44E-326#        ' (18) Dev11: 0.0F, Roslyn: unchanged

        Console.WriteLine(f_17_1)
        Console.WriteLine(f_17_2)
        Console.WriteLine(f_17_3)
        Console.WriteLine(f_17_4)
        Console.WriteLine(f_17_5)
        Console.WriteLine(f_17_6)
        Console.WriteLine(f_17_7)
        Console.WriteLine(f_17_8)
        
        Console.WriteLine(f_18_1)
        Console.WriteLine(f_18_2)
        Console.WriteLine(f_18_3)
        Console.WriteLine(f_18_4)
        Console.WriteLine(f_18_5)

        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 16 significant digits
        ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

        ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
        Const f_17_1 As Double = .149_999_999_979_999_38R     ' (01) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_2 As Double = .149_999_999_979_999_39R     ' (02) Dev11 & Roslyn: 0.14999999997999938
        Const f_17_3 As Double = .149_999_999_979_999_37#     ' (03) Dev11 & Roslyn: 0.14999999997999938

        Const f_17_4 As Double = 0.149_999_999_799_9957R      ' (04) Dev11 & Roslyn: Unchanged
        Const f_17_5 As Double = 0.149_999_999_799_9958R      ' (05) Dev11 & Roslyn: 0.14999999979999579

        Const f_17_6 As Double = 1.499_999_999_799_9947#      ' (06) Dev11 & Roslyn: Unchanged
        Const f_17_7 As Double = 1.499_999_999_799_9945R      ' (07) Dev11 & Roslyn: 1.4999999997999944
        Const f_17_8 As Double = 1.499_999_999_799_9946R      ' (08) Dev11 & Roslyn: 1.4999999997999947

        Const f_18_1 As Double = 14_999_999.997_999_945_9#    ' (09) Dev11 & Roslyn: 14999999.997999946
        Const f_18_2 As Double = 14_999_999.997_999_945_1R    ' (10) Dev11 & Roslyn: 14999999.997999946
        Const f_18_3 As Double = 14_999_999.997_999_945_4R    ' (11) Dev11 & Roslyn: 14999999.997999946

        ' (b) > 16 significant digits before decimal point.
        Const f_18_4 As Double = 14_999_999_999_733_999.2#    ' (12) Dev11 & Roslyn: 1.4999999999734E+16
        Const f_18_5 As Double = 14_999_999_999_379_995.0R    ' (13) Dev11 & Roslyn: 14999999999379996.0

        Const f_24_1 As Double = 1_111_111_499_991_246_899_99.499R      ' (14) Dev11 & Roslyn: 1.1111114999912469E+20

        ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
        '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
        '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

        Const f_overflow_1 As Double = -1.797_693_134_862_315_70E+309#        ' (15) Dev11 & Roslyn: Unchanged
        Const f_overflow_2 As Double = 1.797_693_134_862_315_70E+309R         ' (16) Dev11 & Roslyn: Unchanged
        Const f_underflow_1 As Double = -4.940_656_458_412_465_44E-326R       ' (17) Dev11: -0.0F, Roslyn: unchanged
        Const f_underflow_2 As Double = 4.940_656_458_412_465_44E-326#        ' (18) Dev11: 0.0F, Roslyn: unchanged

        Console.WriteLine(f_17_1)
        Console.WriteLine(f_17_2)
        Console.WriteLine(f_17_3)
        Console.WriteLine(f_17_4)
        Console.WriteLine(f_17_5)
        Console.WriteLine(f_17_6)
        Console.WriteLine(f_17_7)
        Console.WriteLine(f_17_8)

        Console.WriteLine(f_18_1)
        Console.WriteLine(f_18_2)
        Console.WriteLine(f_18_3)
        Console.WriteLine(f_18_4)
        Console.WriteLine(f_18_5)

        Console.WriteLine(f_24_1)

        Console.WriteLine(f_overflow_1)
        Console.WriteLine(f_overflow_2)
        Console.WriteLine(f_underflow_1)
        Console.WriteLine(f_underflow_2)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiterals_LessThan30Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 30 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 27 significant digits
        Const d_27_1 As Decimal = .123_456_789_012_345_678_901_234_567D        ' (00) Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
        Const d_27_2 As Decimal = 0.123_456_789_012_345_678_901_234_567d       ' (01) Dev11 & Roslyn: Unchanged
        Const d_27_3 As Decimal = 1.234_567_890_123_456_789_012_345_67D        ' (02) Dev11 & Roslyn: Unchanged
        Const d_27_4 As Decimal = 123456789012.345_678_901_234_567d            ' (03) Dev11 & Roslyn: Unchanged
        Const d_27_5 As Decimal = 12_345_678_901_234_567_890_123_456.7D        ' (04) Dev11 & Roslyn: Unchanged
        Const d_27_6 As Decimal = 123_456_789_012_345_678_901_234_567.0d       ' (05) Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

        ' 29 significant digits
        Const d_29_1 As Decimal = .123_456_789_012_345_678_901_234_567_89D     ' (06) Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_29_2 As Decimal = 0.123_456_789_012_345_678_901_234_567_89d    ' (07) Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_29_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_9D     ' (08) Dev11 & Roslyn: Unchanged
        Const d_29_4 As Decimal = 123_456_789_012.345_678_901_234_567_8D       ' (09) Dev11 & Roslyn: Unchanged
        Const d_29_5 As Decimal = 1_234_567_890_123_456_789_012_345_678.9D     ' (10) Dev11 & Roslyn: Unchanged
        Const d_29_6 As Decimal = 12_345_678_901_234_567_890_123_456_789.0d    ' (11) Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

        Console.WriteLine(d_27_1)
        Console.WriteLine(d_27_2)
        Console.WriteLine(d_27_3)
        Console.WriteLine(d_27_4)
        Console.WriteLine(d_27_5)
        Console.WriteLine(d_27_6)

        Console.WriteLine(d_29_1)
        Console.WriteLine(d_29_2)
        Console.WriteLine(d_29_3)
        Console.WriteLine(d_29_4)
        Console.WriteLine(d_29_5)
        Console.WriteLine(d_29_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 30 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 27 significant digits
        Const d_27_1 As Decimal = .123_456_789_012_345_678_901_234_567D        ' (00) Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
        Const d_27_2 As Decimal = 0.123_456_789_012_345_678_901_234_567D       ' (01) Dev11 & Roslyn: Unchanged
        Const d_27_3 As Decimal = 1.234_567_890_123_456_789_012_345_67D        ' (02) Dev11 & Roslyn: Unchanged
        Const d_27_4 As Decimal = 123456789012.345_678_901_234_567D            ' (03) Dev11 & Roslyn: Unchanged
        Const d_27_5 As Decimal = 12_345_678_901_234_567_890_123_456.7D        ' (04) Dev11 & Roslyn: Unchanged
        Const d_27_6 As Decimal = 123_456_789_012_345_678_901_234_567.0D       ' (05) Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

        ' 29 significant digits
        Const d_29_1 As Decimal = .123_456_789_012_345_678_901_234_567_89D     ' (06) Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_29_2 As Decimal = 0.123_456_789_012_345_678_901_234_567_89D    ' (07) Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_29_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_9D     ' (08) Dev11 & Roslyn: Unchanged
        Const d_29_4 As Decimal = 123_456_789_012.345_678_901_234_567_8D       ' (09) Dev11 & Roslyn: Unchanged
        Const d_29_5 As Decimal = 1_234_567_890_123_456_789_012_345_678.9D     ' (10) Dev11 & Roslyn: Unchanged
        Const d_29_6 As Decimal = 12_345_678_901_234_567_890_123_456_789.0D    ' (11) Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

        Console.WriteLine(d_27_1)
        Console.WriteLine(d_27_2)
        Console.WriteLine(d_27_3)
        Console.WriteLine(d_27_4)
        Console.WriteLine(d_27_5)
        Console.WriteLine(d_27_6)

        Console.WriteLine(d_29_1)
        Console.WriteLine(d_29_2)
        Console.WriteLine(d_29_3)
        Console.WriteLine(d_29_4)
        Console.WriteLine(d_29_5)
        Console.WriteLine(d_29_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiterals_LessThan30Digits_WithTypeCharacterDecimal()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 30 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 27 significant digits
        Const d_27_1 As Decimal = .123_456_789_012_345_678_901_234_567@        ' (00) Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
        Const d_27_2 As Decimal = 0.123_456_789_012_345_678_901_234_567@       ' (01) Dev11 & Roslyn: Unchanged
        Const d_27_3 As Decimal = 1.234_567_890_123_456_789_012_345_67@        ' (02) Dev11 & Roslyn: Unchanged
        Const d_27_4 As Decimal = 123_456_789_012.345678901234567@             ' (03) Dev11 & Roslyn: Unchanged
        Const d_27_5 As Decimal = 12_345_678_901_234_567_890_123_456.7@        ' (04) Dev11 & Roslyn: Unchanged
        Const d_27_6 As Decimal = 123_456_789_012_345_678_901_234_567.0@       ' (05) Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

        ' 29 significant digits
        Const d_29_1 As Decimal = .123_456_789_012_345_678_901_234_567_89@     ' (06) Dev11 & Roslyn: 0.1234567890123456789012345679@
        Const d_29_2 As Decimal = 0.123_456_789_012_345_678_901_234_567_89@    ' (07) Dev11 & Roslyn: 0.1234567890123456789012345679@
        Const d_29_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_9@     ' (08) Dev11 & Roslyn: Unchanged
        Const d_29_4 As Decimal = 123_456_789_012.345_678_901_234_567_89@      ' (09) Dev11 & Roslyn: Unchanged
        Const d_29_5 As Decimal = 1_234_567_890_123_456_789_012_345_678.9@     ' (10) Dev11 & Roslyn: Unchanged
        Const d_29_6 As Decimal = 12_345_678_901_234_567_890_123_456_789.0@    ' (11) Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

        Console.WriteLine(d_27_1)
        Console.WriteLine(d_27_2)
        Console.WriteLine(d_27_3)
        Console.WriteLine(d_27_4)
        Console.WriteLine(d_27_5)
        Console.WriteLine(d_27_6)

        Console.WriteLine(d_29_1)
        Console.WriteLine(d_29_2)
        Console.WriteLine(d_29_3)
        Console.WriteLine(d_29_4)
        Console.WriteLine(d_29_5)
        Console.WriteLine(d_29_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 1: Less than 30 significant digits
        ' Dev11 and Roslyn behavior are identical: UNCHANGED

        ' 27 significant digits
        Const d_27_1 As Decimal = .123_456_789_012_345_678_901_234_567@        ' (00) Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
        Const d_27_2 As Decimal = 0.123_456_789_012_345_678_901_234_567@       ' (01) Dev11 & Roslyn: Unchanged
        Const d_27_3 As Decimal = 1.234_567_890_123_456_789_012_345_67@        ' (02) Dev11 & Roslyn: Unchanged
        Const d_27_4 As Decimal = 123_456_789_012.345678901234567@             ' (03) Dev11 & Roslyn: Unchanged
        Const d_27_5 As Decimal = 12_345_678_901_234_567_890_123_456.7@        ' (04) Dev11 & Roslyn: Unchanged
        Const d_27_6 As Decimal = 123_456_789_012_345_678_901_234_567.0@       ' (05) Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

        ' 29 significant digits
        Const d_29_1 As Decimal = .123_456_789_012_345_678_901_234_567_89@     ' (06) Dev11 & Roslyn: 0.1234567890123456789012345679@
        Const d_29_2 As Decimal = 0.123_456_789_012_345_678_901_234_567_89@    ' (07) Dev11 & Roslyn: 0.1234567890123456789012345679@
        Const d_29_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_9@     ' (08) Dev11 & Roslyn: Unchanged
        Const d_29_4 As Decimal = 123_456_789_012.345_678_901_234_567_89@      ' (09) Dev11 & Roslyn: Unchanged
        Const d_29_5 As Decimal = 1_234_567_890_123_456_789_012_345_678.9@     ' (10) Dev11 & Roslyn: Unchanged
        Const d_29_6 As Decimal = 12_345_678_901_234_567_890_123_456_789.0@    ' (11) Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

        Console.WriteLine(d_27_1)
        Console.WriteLine(d_27_2)
        Console.WriteLine(d_27_3)
        Console.WriteLine(d_27_4)
        Console.WriteLine(d_27_5)
        Console.WriteLine(d_27_6)

        Console.WriteLine(d_29_1)
        Console.WriteLine(d_29_2)
        Console.WriteLine(d_29_3)
        Console.WriteLine(d_29_4)
        Console.WriteLine(d_29_5)
        Console.WriteLine(d_29_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiterals_30Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 30 significant digits
        ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits
        
        Const d_30_1 As Decimal = .123_456_789_012_345_678_901_234_567_891D         ' Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_30_2 As Decimal = 0.123_456_789_012_345_678_901_234_568_789_1D      ' Dev11 & Roslyn: 0.1234567890123456789012345688D
        Const d_30_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_91D         ' Dev11 & Roslyn: 1.2345678901234567890123456789D
        Const d_30_4 As Decimal = 123_456_789_012_345.678_901_234_567_891D          ' Dev11 & Roslyn: 123456789012345.67890123456789D
        Const d_30_5 As Decimal = 12_345_678_901_234_567_890_123_456_789.1D         ' Dev11 & Roslyn: 12345678901234567890123456789D

        ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
        Const d_30_6 As Decimal = 123_456_789_012_345_678_901_234_567_891.0D        ' Dev11 & Roslyn: 123456789012345678901234567891.0D

        Console.WriteLine(d_30_1)
        Console.WriteLine(d_30_2)
        Console.WriteLine(d_30_3)
        Console.WriteLine(d_30_4)
        Console.WriteLine(d_30_5)
        Console.WriteLine(d_30_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 30 significant digits
        ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits

        Const d_30_1 As Decimal = .123_456_789_012_345_678_901_234_567_891D         ' Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_30_2 As Decimal = 0.123_456_789_012_345_678_901_234_568_789_1D      ' Dev11 & Roslyn: 0.1234567890123456789012345688D
        Const d_30_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_91D         ' Dev11 & Roslyn: 1.2345678901234567890123456789D
        Const d_30_4 As Decimal = 123_456_789_012_345.678_901_234_567_891D          ' Dev11 & Roslyn: 123456789012345.67890123456789D
        Const d_30_5 As Decimal = 12_345_678_901_234_567_890_123_456_789.1D         ' Dev11 & Roslyn: 12345678901234567890123456789D

        ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
        Const d_30_6 As Decimal = 123_456_789_012_345_678_901_234_567_891.0D        ' Dev11 & Roslyn: 123456789012345678901234567891.0D

        Console.WriteLine(d_30_1)
        Console.WriteLine(d_30_2)
        Console.WriteLine(d_30_3)
        Console.WriteLine(d_30_4)
        Console.WriteLine(d_30_5)
        Console.WriteLine(d_30_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiterals_30Digits_WithTypeCharacterDecimal()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 30 significant digits
        ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits
        
        Const d_30_1 As Decimal = .123_456_789_012_345_678_901_234_567_891@         ' Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_30_2 As Decimal = 0.123_456_789_012_345_678_901_234_568_789_1@      ' Dev11 & Roslyn: 0.1234567890123456789012345688D
        Const d_30_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_91@         ' Dev11 & Roslyn: 1.2345678901234567890123456789D
        Const d_30_4 As Decimal = 123_456_789_012_345.678_901_234_567_891@          ' Dev11 & Roslyn: 123456789012345.67890123456789D
        Const d_30_5 As Decimal = 12_345_678_901_234_567_890_123_456_789.1@         ' Dev11 & Roslyn: 12345678901234567890123456789D

        ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
        Const d_30_6 As Decimal = 123_456_789_012_345_678_901_234_567_891.0@        ' Dev11 & Roslyn: 123456789012345678901234567891.0D

        Console.WriteLine(d_30_1)
        Console.WriteLine(d_30_2)
        Console.WriteLine(d_30_3)
        Console.WriteLine(d_30_4)
        Console.WriteLine(d_30_5)
        Console.WriteLine(d_30_6)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 2: 30 significant digits
        ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits

        Const d_30_1 As Decimal = .123_456_789_012_345_678_901_234_567_891@         ' Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_30_2 As Decimal = 0.123_456_789_012_345_678_901_234_568_789_1@      ' Dev11 & Roslyn: 0.1234567890123456789012345688D
        Const d_30_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_91@         ' Dev11 & Roslyn: 1.2345678901234567890123456789D
        Const d_30_4 As Decimal = 123_456_789_012_345.678_901_234_567_891@          ' Dev11 & Roslyn: 123456789012345.67890123456789D
        Const d_30_5 As Decimal = 12_345_678_901_234_567_890_123_456_789.1@         ' Dev11 & Roslyn: 12345678901234567890123456789D

        ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
        Const d_30_6 As Decimal = 123_456_789_012_345_678_901_234_567_891.0@        ' Dev11 & Roslyn: 123456789012345678901234567891.0D

        Console.WriteLine(d_30_1)
        Console.WriteLine(d_30_2)
        Console.WriteLine(d_30_3)
        Console.WriteLine(d_30_4)
        Console.WriteLine(d_30_5)
        Console.WriteLine(d_30_6)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiterals_GreaterThan30Digits()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 30 significant digits
        ' Dev11 has unpredictable behavior: pretty listed/round off to wrong values in certain cases
        ' Roslyn behavior: Always rounded off + pretty listed to <= 29 significant digits
        
        ' (a) > 30 significant digits overall, but < 30 digits before decimal point.
        Const d_32_1 As Decimal = .123_456_789_012_345_678_901_234_567_890_12D          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_32_2 As Decimal = 0.123_456_789_012_345_678_901_234_568_789_012@        ' Dev11 & Roslyn: 0.1234567890123456789012345688@
        Const d_32_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_901_2d          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
        Const d_32_4 As Decimal = 123_456_789_012_345.678_901_234_567_890_12@           ' Dev11 & Roslyn: 123456789012345.67890123456789@
        
        ' (b) > 30 significant digits before decimal point (Overflow case): Ensure no pretty listing.
        Const d_35_1 As Decimal = 123_456_789_012_345_678_901_234_567_890_123.45D       ' Dev11 & Roslyn: 123456789012345678901234567890123.45D

        Console.WriteLine(d_32_1)
        Console.WriteLine(d_32_2)
        Console.WriteLine(d_32_3)
        Console.WriteLine(d_32_4)
        Console.WriteLine(d_35_1)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        ' CATEGORY 3: > 30 significant digits
        ' Dev11 has unpredictable behavior: pretty listed/round off to wrong values in certain cases
        ' Roslyn behavior: Always rounded off + pretty listed to <= 29 significant digits

        ' (a) > 30 significant digits overall, but < 30 digits before decimal point.
        Const d_32_1 As Decimal = .123_456_789_012_345_678_901_234_567_890_12D          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
        Const d_32_2 As Decimal = 0.123_456_789_012_345_678_901_234_568_789_012@        ' Dev11 & Roslyn: 0.1234567890123456789012345688@
        Const d_32_3 As Decimal = 1.234_567_890_123_456_789_012_345_678_901_2D          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
        Const d_32_4 As Decimal = 123_456_789_012_345.678_901_234_567_890_12@           ' Dev11 & Roslyn: 123456789012345.67890123456789@

        ' (b) > 30 significant digits before decimal point (Overflow case): Ensure no pretty listing.
        Const d_35_1 As Decimal = 123_456_789_012_345_678_901_234_567_890_123.45D       ' Dev11 & Roslyn: 123456789012345678901234567890123.45D

        Console.WriteLine(d_32_1)
        Console.WriteLine(d_32_2)
        Console.WriteLine(d_32_3)
        Console.WriteLine(d_32_4)
        Console.WriteLine(d_35_1)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceFloatLiteralsWithNegativeExponents()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())

        ' Floating point values might be represented either in fixed point notation or scientific/exponent notation.
        ' MSDN comment for Standard Numeric Format Strings used in Single.ToString(String) API (or Double.ToString(String)):
        '   Fixed-point notation is used if the exponent that would result from expressing the number in scientific notation is greater than -5 and
        '   less than the precision specifier; otherwise, scientific notation is used.
        '
        ' However, Dev11 pretty lister differs from this for floating point values < 0. It uses fixed point notation as long as exponent is greater than '-(actualPrecision + 1)'.
        ' For example, consider Single Floating literals:
        '     (i) Precision = 7
        '           0.0000001234567F        =>  0.0000001234567F             (exponent = -7: fixed point notation)
        '           0.00000001234567F       =>  0.00000001234567F            (exponent = -8: fixed point notation)
        '           0.000000001234567F      =>  1.234567E-9F                 (exponent = -9: exponent notation)
        '           0.0000000001234567F     =>  1.234567E-10F                (exponent = -10: exponent notation)
        '     (ii) Precision = 9
        '           0.0000000012345678F     =>  0.00000000123456778F         (exponent = -9: fixed point notation)
        '           0.00000000012345678F    =>  0.000000000123456786F        (exponent = -10: fixed point notation)
        '           0.000000000012345678F   =>  1.23456783E-11F              (exponent = -11: exponent notation)
        '           0.0000000000012345678F  =>  1.23456779E-12F              (exponent = -12: exponent notation)

        Const f_1 As Single = 0.000_001_234_567F
        Const f_2 As Single = 0.000_000_123_456_7F
        Const f_3 As Single = 0.000_000_012_345_67F
        Const f_4 As Single = 0.000_000_001_234_567F ' Change at -9
        Const f_5 As Single = 0.000_000_000_123_456_7F

        Const f_6 As Single = 0.000_000_001_234_567_78F
        Const f_7 As Single = 0.000_000_000_123_456_786F
        Const f_8 As Single = 0.000_000_000_012_345_678F ' Change at -11
        Const f_9 As Single = 0.000_000_000_001_234_567_8F

        Const d_1 As Single = 0.000_000_000_000_012_345_678_901_234_5
        Const d_2 As Single = 0.000_000_000_000_000_123_456_789_012_345
        Const d_3 As Single = 0.000_000_000_000_000_012_345_678_901_234_5 ' Change at -17
        Const d_4 As Single = 0.000_000_000_000_000_001_234_567_890_123_45

        Const d_5 As Double = 0.000_000_000_000_000_012_345_678_901_234_56
        Const d_6 As Double = 0.000_000_000_000_000_001_234_567_890_123_456
        Const d_7 As Double = 0.000_000_000_000_000_000_123_456_789_012_345_6   ' Change at -19
        Const d_8 As Double = 0.000_000_000_000_000_000_012_345_678_901_234_56
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())

        ' Floating point values might be represented either in fixed point notation or scientific/exponent notation.
        ' MSDN comment for Standard Numeric Format Strings used in Single.ToString(String) API (or Double.ToString(String)):
        '   Fixed-point notation is used if the exponent that would result from expressing the number in scientific notation is greater than -5 and
        '   less than the precision specifier; otherwise, scientific notation is used.
        '
        ' However, Dev11 pretty lister differs from this for floating point values < 0. It uses fixed point notation as long as exponent is greater than '-(actualPrecision + 1)'.
        ' For example, consider Single Floating literals:
        '     (i) Precision = 7
        '           0.0000001234567F        =>  0.0000001234567F             (exponent = -7: fixed point notation)
        '           0.00000001234567F       =>  0.00000001234567F            (exponent = -8: fixed point notation)
        '           0.000000001234567F      =>  1.234567E-9F                 (exponent = -9: exponent notation)
        '           0.0000000001234567F     =>  1.234567E-10F                (exponent = -10: exponent notation)
        '     (ii) Precision = 9
        '           0.0000000012345678F     =>  0.00000000123456778F         (exponent = -9: fixed point notation)
        '           0.00000000012345678F    =>  0.000000000123456786F        (exponent = -10: fixed point notation)
        '           0.000000000012345678F   =>  1.23456783E-11F              (exponent = -11: exponent notation)
        '           0.0000000000012345678F  =>  1.23456779E-12F              (exponent = -12: exponent notation)

        Const f_1 As Single = 0.000_001_234_567F
        Const f_2 As Single = 0.000_000_123_456_7F
        Const f_3 As Single = 0.000_000_012_345_67F
        Const f_4 As Single = 0.000_000_001_234_567F ' Change at -9
        Const f_5 As Single = 0.000_000_000_123_456_7F

        Const f_6 As Single = 0.000_000_001_234_567_78F
        Const f_7 As Single = 0.000_000_000_123_456_786F
        Const f_8 As Single = 0.000_000_000_012_345_678F ' Change at -11
        Const f_9 As Single = 0.000_000_000_001_234_567_8F

        Const d_1 As Single = 0.000_000_000_000_012_345_678_901_234_5
        Const d_2 As Single = 0.000_000_000_000_000_123_456_789_012_345
        Const d_3 As Single = 0.000_000_000_000_000_012_345_678_901_234_5 ' Change at -17
        Const d_4 As Single = 0.000_000_000_000_000_001_234_567_890_123_45

        Const d_5 As Double = 0.000_000_000_000_000_012_345_678_901_234_56
        Const d_6 As Double = 0.000_000_000_000_000_001_234_567_890_123_456
        Const d_7 As Double = 0.000_000_000_000_000_000_123_456_789_012_345_6   ' Change at -19
        Const d_8 As Double = 0.000_000_000_000_000_000_012_345_678_901_234_56
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceSingleLiteralsWithTrailingZeros()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        Const f1 As Single = 3.011_000F                      ' Dev11 & Roslyn: 3.011F
        Const f2 As Single = 3.000_000!                      ' Dev11 & Roslyn: 3.0!
        Const f3 As Single = 3.0F                            ' Dev11 & Roslyn: Unchanged
        Const f4 As Single = 3_000f                          ' Dev11 & Roslyn: 3000.0F
        Const f5 As Single = 3_000E+10!                      ' Dev11 & Roslyn: 3.0E+13!
        Const f6 As Single = 3_000.0E+10F                    ' Dev11 & Roslyn: 3.0E+13F
        Const f7 As Single = 3_000.010E+1F                   ' Dev11 & Roslyn: 30000.1F
        Const f8 As Single = 3_000.123_456_789_010E+10!      ' Dev11 & Roslyn: 3.00012337E+13!
        Const f9 As Single = 3_000.123_456_789_000E+10F      ' Dev11 & Roslyn: 3.00012337E+13F
        Const f10 As Single = 30_001_234_567_890.10E-10f     ' Dev11 & Roslyn: 3000.12354F
        Const f11 As Single = 3_000E-10!                     ' Dev11 & Roslyn: 0.0000003!

        Console.WriteLine(f1)
        Console.WriteLine(f2)
        Console.WriteLine(f3)
        Console.WriteLine(f4)
        Console.WriteLine(f5)
        Console.WriteLine(f6)
        Console.WriteLine(f7)
        Console.WriteLine(f8)
        Console.WriteLine(f9)
        Console.WriteLine(f10)
        Console.WriteLine(f11)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        Const f1 As Single = 3.011_000F                      ' Dev11 & Roslyn: 3.011F
        Const f2 As Single = 3.000_000!                      ' Dev11 & Roslyn: 3.0!
        Const f3 As Single = 3.0F                            ' Dev11 & Roslyn: Unchanged
        Const f4 As Single = 3_000F                          ' Dev11 & Roslyn: 3000.0F
        Const f5 As Single = 3_000E+10!                      ' Dev11 & Roslyn: 3.0E+13!
        Const f6 As Single = 3_000.0E+10F                    ' Dev11 & Roslyn: 3.0E+13F
        Const f7 As Single = 3_000.010E+1F                   ' Dev11 & Roslyn: 30000.1F
        Const f8 As Single = 3_000.123_456_789_010E+10!      ' Dev11 & Roslyn: 3.00012337E+13!
        Const f9 As Single = 3_000.123_456_789_000E+10F      ' Dev11 & Roslyn: 3.00012337E+13F
        Const f10 As Single = 30_001_234_567_890.10E-10F     ' Dev11 & Roslyn: 3000.12354F
        Const f11 As Single = 3_000E-10!                     ' Dev11 & Roslyn: 0.0000003!

        Console.WriteLine(f1)
        Console.WriteLine(f2)
        Console.WriteLine(f3)
        Console.WriteLine(f4)
        Console.WriteLine(f5)
        Console.WriteLine(f6)
        Console.WriteLine(f7)
        Console.WriteLine(f8)
        Console.WriteLine(f9)
        Console.WriteLine(f10)
        Console.WriteLine(f11)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(5529, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDoubleLiteralsWithTrailingZeros()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        Const d1 As Double = 3.011_000                       ' Dev11 & Roslyn: 3.011
        Const d2 As Double = 3.000_000                       ' Dev11 & Roslyn: 3.0
        Const d3 As Double = 3.0                             ' Dev11 & Roslyn: Unchanged
        Const d4 As Double = 3_000R                          ' Dev11 & Roslyn: 3000.0R
        Const d5 As Double = 3_000E+10#                      ' Dev11 & Roslyn: 30000000000000.0#
        Const d6 As Double = 3_000.0E+10                     ' Dev11 & Roslyn: 30000000000000.0
        Const d7 As Double = 3_000.010E+1                    ' Dev11 & Roslyn: 30000.1
        Const d8 As Double = 3_000.123_456_789_010E+10#      ' Dev11 & Roslyn: 30001234567890.1#
        Const d9 As Double = 3_000.123_456_789_000E+10       ' Dev11 & Roslyn: 30001234567890.0
        Const d10 As Double = 30_001_234_567_890.10E-10d     ' Dev11 & Roslyn: 3000.12345678901D
        Const d11 As Double = 3_000E-10                      ' Dev11 & Roslyn: 0.0000003

        Console.WriteLine(d1)
        Console.WriteLine(d2)
        Console.WriteLine(d3)
        Console.WriteLine(d4)
        Console.WriteLine(d5)
        Console.WriteLine(d6)
        Console.WriteLine(d7)
        Console.WriteLine(d8)
        Console.WriteLine(d9)
        Console.WriteLine(d10)
        Console.WriteLine(d11)
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        Const d1 As Double = 3.011_000                       ' Dev11 & Roslyn: 3.011
        Const d2 As Double = 3.000_000                       ' Dev11 & Roslyn: 3.0
        Const d3 As Double = 3.0                             ' Dev11 & Roslyn: Unchanged
        Const d4 As Double = 3_000R                          ' Dev11 & Roslyn: 3000.0R
        Const d5 As Double = 3_000E+10#                      ' Dev11 & Roslyn: 30000000000000.0#
        Const d6 As Double = 3_000.0E+10                     ' Dev11 & Roslyn: 30000000000000.0
        Const d7 As Double = 3_000.010E+1                    ' Dev11 & Roslyn: 30000.1
        Const d8 As Double = 3_000.123_456_789_010E+10#      ' Dev11 & Roslyn: 30001234567890.1#
        Const d9 As Double = 3_000.123_456_789_000E+10       ' Dev11 & Roslyn: 30001234567890.0
        Const d10 As Double = 30_001_234_567_890.10E-10D     ' Dev11 & Roslyn: 3000.12345678901D
        Const d11 As Double = 3_000E-10                      ' Dev11 & Roslyn: 0.0000003

        Console.WriteLine(d1)
        Console.WriteLine(d2)
        Console.WriteLine(d3)
        Console.WriteLine(d4)
        Console.WriteLine(d5)
        Console.WriteLine(d6)
        Console.WriteLine(d7)
        Console.WriteLine(d8)
        Console.WriteLine(d9)
        Console.WriteLine(d10)
        Console.WriteLine(d11)
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        #region "Reduce Decimal Literal"

        #region "With Trailing Zeros"
        private async Task ReduceDecimalLiteralsWithTrailingZeros(string literal,string expected)
        {
            var code = $@"[|
Module Program
    Sub Main(args As String())
        Const d1 As Decimal = {literal}
        Console.WriteLine(d1)
    End Sub
End Module
|]";
            var code_expected = $@"
Module Program
    Sub Main(args As String())
        Const d1 As Decimal = {expected}
        Console.WriteLine(d1)
    End Sub
End Module
";
            await VerifyAsync(code, code_expected);
        }

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_00()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3.011_000D",@"3.011_000D"); /* Dev11 & Roslyn: 3.011D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_01()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3.000_000D", @"3.000_000D"); /* Dev11 & Roslyn: 3D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_02()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3.0D", @"3D"); /* Dev11 & Roslyn: 3D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_03()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3_000D", @"3_000D"); /* Dev11 & Roslyn: 3000D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_04()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3_000E+10D", @"3_000E+10D"); /* Dev11 & Roslyn: 30000000000000D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_05()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3_000.0E+10D", @"3_000.0E+10D"); /* Dev11 & Roslyn: 30000000000000D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_06()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3_000.010E+1D", @"3_000.010E+1D"); /* Dev11 & Roslyn: 30000.1D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_07()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3_000.123_456_789_010E+10D", @"3_000.123_456_789_010E+10D"); /* Dev11 & Roslyn: 30001234567890.1D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_08()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3_000.123_456_789_000E+10D", @"3_000.123_456_789_000E+10D"); /* Dev11 & Roslyn: 30001234567890D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_09()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"30_001_234_567_890.10E-10D", @"30_001_234_567_890.10E-10D"); /* Dev11 & Roslyn: 3000.12345678901D */

        [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.ReduceTokens), Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceDecimalLiteralsWithTrailingZeros_10()
            => await ReduceDecimalLiteralsWithTrailingZeros(@"3_000E-10D", @"3_000E-10D"); /* Dev11 & Roslyn: 0.0000003D */

        #endregion

        #endregion

        [Fact]
        [WorkItem(623319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623319")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceFloatingAndDecimalLiteralsWithDifferentCulture()
        {
            var savedCulture = System.Threading.Thread.CurrentThread.CurrentCulture;

            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.CreateSpecificCulture("de-DE");

                var code = @"[|
Module Program
    Sub Main(args As String())
        Dim d = 1.0D
        Dim f = 1.0F
        Dim x = 1.0
    End Sub
End Module|]";

                var expected = @"
Module Program
    Sub Main(args As String())
        Dim d = 1D
        Dim f = 1.0F
        Dim x = 1.0
    End Sub
End Module";
                await VerifyAsync(code, expected);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = savedCulture;
            }
        }

        [Fact]
        [WorkItem(652147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652147")]
        public async Task ReduceFloatingAndDecimalLiteralsWithInvariantCultureNegatives()
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = (CultureInfo)oldCulture.Clone();
                Thread.CurrentThread.CurrentCulture.NumberFormat.NegativeSign = "~";

                var code = @"[|
Module Program
    Sub Main(args As String())
        Dim d = -1.0E-11D
        Dim f = -1.0E-11F
        Dim x = -1.0E-11
    End Sub
End Module|]";

                var expected = @"
Module Program
    Sub Main(args As String())
        Dim d = -0.00000000001D
        Dim f = -1.0E-11F
        Dim x = -0.00000000001
    End Sub
End Module";
                await VerifyAsync(code, expected);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceIntegerLiteralWithLeadingZeros()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        Const i0 As Integer = 0060
        Const i1 As Integer = 0060%
        Const i2 As Integer = &H006F
        Const i3 As Integer = &O0060
        Const i4 As Integer = 0060I
        Const i5 As Integer = -0060
        Const i6 As Integer = 000
        Const i7 As UInteger = 0060UI
        Const i8 As Integer = &H0000FFFFI
        Const i9 As Integer = &O000
        Const i10 As Integer = &H000
        Const l0 As Long = 0060L
        Const l1 As Long = 0060&
        Const l2 As ULong = 0060UL
        Const s0 As Short = 0060S
        Const s1 As UShort = 0060US
        Const s2 As Short = &H0000FFFFS
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        Const i0 As Integer = 60
        Const i1 As Integer = 60%
        Const i2 As Integer = &H6F
        Const i3 As Integer = &O60
        Const i4 As Integer = 60I
        Const i5 As Integer = -60
        Const i6 As Integer = 0
        Const i7 As UInteger = 60UI
        Const i8 As Integer = &HFFFFI
        Const i9 As Integer = &O0
        Const i10 As Integer = &H0
        Const l0 As Long = 60L
        Const l1 As Long = 60&
        Const l2 As ULong = 60UL
        Const s0 As Short = 60S
        Const s1 As UShort = 60US
        Const s2 As Short = &HFFFFS
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceIntegerLiteralWithNegativeHexOrOctalValue()
        {
            var code = @"[|
Module Program
    Sub Main(args As String())
        Const s0 As Short = &HFFFFS
        Const s1 As Short = &O177777S
        Const s2 As Short = &H8000S
        Const s3 As Short = &O100000S
        Const i0 As Integer = &O37777777777I
        Const i1 As Integer = &HFFFFFFFFI
        Const i2 As Integer = &H80000000I
        Const i3 As Integer = &O20000000000I
        Const l0 As Long = &HFFFFFFFFFFFFFFFFL
        Const l1 As Long = &O1777777777777777777777L
        Const l2 As Long = &H8000000000000000L
        Const l2 As Long = &O1000000000000000000000L
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main(args As String())
        Const s0 As Short = &HFFFFS
        Const s1 As Short = &O177777S
        Const s2 As Short = &H8000S
        Const s3 As Short = &O100000S
        Const i0 As Integer = &O37777777777I
        Const i1 As Integer = &HFFFFFFFFI
        Const i2 As Integer = &H80000000I
        Const i3 As Integer = &O20000000000I
        Const l0 As Long = &HFFFFFFFFFFFFFFFFL
        Const l1 As Long = &O1777777777777777777777L
        Const l2 As Long = &H8000000000000000L
        Const l2 As Long = &O1000000000000000000000L
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceIntegerLiteralWithOverflow()
        {
            var code = @"[|
Module Module1
    Sub Main()
        Dim sMax As Short = 0032768S
        Dim usMax As UShort = 00655536US
        Dim iMax As Integer = 002147483648I
        Dim uiMax As UInteger = 004294967296UI
        Dim lMax As Long = 009223372036854775808L
        Dim ulMax As ULong = 0018446744073709551616UL
        Dim z As Long = &O37777777777777777777777
        Dim x As Long = &HFFFFFFFFFFFFFFFFF
    End Sub
End Module
|]";

            var expected = @"
Module Module1
    Sub Main()
        Dim sMax As Short = 0032768S
        Dim usMax As UShort = 00655536US
        Dim iMax As Integer = 002147483648I
        Dim uiMax As UInteger = 004294967296UI
        Dim lMax As Long = 009223372036854775808L
        Dim ulMax As ULong = 0018446744073709551616UL
        Dim z As Long = &O37777777777777777777777
        Dim x As Long = &HFFFFFFFFFFFFFFFFF
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceBinaryIntegerLiteral()
        {
            var code = @"[|
Module Module1
    Sub Main()
        ' signed
        Dim a As SByte = &B0111
        Dim b As Short = &B0101
        Dim c As Integer = &B00100100
        Dim d As Long = &B001001100110
        ' with digit separators
        Dim a1 As SByte = &B01_11
        Dim b1 As Short = &B01_01
        Dim c1 As Integer = &B00_10_01_00
        Dim d1 As Long = &B00_10_01_10_01_10

        ' unsigned
        Dim e As Byte = &B01011
        Dim f As UShort = &B00100
        Dim g As UInteger = &B001001100110
        Dim h As ULong = &B001001100110
        ' with digit separators
        Dim e1 As Byte = &B0_10_11
        Dim f1 As UShort = &B0_01_00
        Dim g1 As UInteger = &B00_10_01_10_01_10
        Dim h1 As ULong = &B00_10_01_10_01_10

        ' negative
        Dim i As SByte = -&B0111
        Dim j As Short = -&B00101
        Dim k As Integer = -&B00100100
        Dim l As Long = -&B001001100110
        ' with digit separators
        Dim i1 As SByte = -&B01_11
        Dim j2 As Short = -&B0_01_01
        Dim k3 As Integer = -&B00_10_01_00
        Dim l4 As Long = -&B00_10_01_10_01_10

        ' negative literal
        Dim m As SByte = &B10000001
        Dim n As Short = &B1000000000000001
        Dim o As Integer = &B10000000000000000000000000000001
        Dim p As Long = &B1000000000000000000000000000000000000000000000000000000000000001
        ' with digit separators
        Dim m1 As SByte = &B10_00_00_01
        Dim n2 As Short = &B10_00_00_00_00_00_00_01
        Dim o3 As Integer = &B10_00_00_00_00_00_00_00_00_00_00_00_00_00_00_01
        Dim p4 As Long = &B10_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_01
    End Sub
End Module
|]";

            var expected = @"
Module Module1
    Sub Main()
        ' signed
        Dim a As SByte = &B111
        Dim b As Short = &B101
        Dim c As Integer = &B100100
        Dim d As Long = &B1001100110
        ' with digit separators
        Dim a1 As SByte = &B01_11
        Dim b1 As Short = &B01_01
        Dim c1 As Integer = &B00_10_01_00
        Dim d1 As Long = &B00_10_01_10_01_10

        ' unsigned
        Dim e As Byte = &B1011
        Dim f As UShort = &B100
        Dim g As UInteger = &B1001100110
        Dim h As ULong = &B1001100110
        ' with digit separators
        Dim e1 As Byte = &B0_10_11
        Dim f1 As UShort = &B0_01_00
        Dim g1 As UInteger = &B00_10_01_10_01_10
        Dim h1 As ULong = &B00_10_01_10_01_10

        ' negative
        Dim i As SByte = -&B111
        Dim j As Short = -&B101
        Dim k As Integer = -&B100100
        Dim l As Long = -&B1001100110
        ' with digit separators
        Dim i1 As SByte = -&B01_11
        Dim j2 As Short = -&B0_01_01
        Dim k3 As Integer = -&B00_10_01_00
        Dim l4 As Long = -&B00_10_01_10_01_10

        ' negative literal
        Dim m As SByte = &B10000001
        Dim n As Short = &B1000000000000001
        Dim o As Integer = &B10000000000000000000000000000001
        Dim p As Long = &B1000000000000000000000000000000000000000000000000000000000000001
        ' with digit separators
        Dim m1 As SByte = &B10_00_00_01
        Dim n2 As Short = &B10_00_00_00_00_00_00_01
        Dim o3 As Integer = &B10_00_00_00_00_00_00_00_00_00_00_00_00_00_00_01
        Dim p4 As Long = &B10_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_00_01
    End Sub
End Module
";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(14034, "https://github.com/dotnet/roslyn/issues/14034")]
        [Trait(Traits.Feature, Traits.Features.ReduceTokens)]
        [Trait(Traits.Feature, "WithDigitSeparators")]
        public async Task ReduceIntegersWithDigitSeparators()
        {
            var source = @"
Module Module1
    Sub Main()
        Dim x = 100_000
    End Sub
End Module
";
            var expected = source;
            await VerifyAsync($"[|{source}|]", expected);
        }

        private static async Task VerifyAsync(string codeWithMarker, string expectedResult)
        {
            MarkupTestFile.GetSpans(codeWithMarker, 
                out var codeWithoutMarker, out ImmutableArray<TextSpan> textSpans);

            var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic);
            var codeCleanups = CodeCleaner.GetDefaultProviders(document).WhereAsArray(p => p.Name == PredefinedCodeCleanupProviderNames.ReduceTokens || p.Name == PredefinedCodeCleanupProviderNames.CaseCorrection || p.Name == PredefinedCodeCleanupProviderNames.Format);

            var cleanDocument = await CodeCleaner.CleanupAsync(document, textSpans[0], codeCleanups);

            Assert.Equal(expectedResult, (await cleanDocument.GetSyntaxRootAsync()).ToFullString());
        }

        private static Document CreateDocument(string code, string language)
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

            return project.AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib)
                          .AddDocument("Document", SourceText.From(code));
        }
    }
}
