// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ReduceTokens)]
public sealed class ReduceTokenTests
{
#if NET
    private static bool IsNetCoreApp => true;
#else
    private static bool IsNetCoreApp => false;
#endif

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceSingleLiterals_LessThan8Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 5 significant digits
                    Const f_5_1 As Single = .14995F        ' Dev11 & Roslyn: Pretty listed to 0.14995F
                    Const f_5_2 As Single = 0.14995f       ' Dev11 & Roslyn: Unchanged
                    Const f_5_3 As Single = 1.4995F        ' Dev11 & Roslyn: Unchanged
                    Const f_5_4 As Single = 149.95f        ' Dev11 & Roslyn: Unchanged
                    Const f_5_5 As Single = 1499.5F        ' Dev11 & Roslyn: Unchanged
                    Const f_5_6 As Single = 14995.0f       ' Dev11 & Roslyn: Unchanged

                    ' 7 significant digits
                    Const f_7_1 As Single = .1499995F      ' Dev11 & Roslyn: Pretty listed to 0.1499995F
                    Const f_7_2 As Single = 0.1499995f     ' Dev11 & Roslyn: Unchanged
                    Const f_7_3 As Single = 1.499995F      ' Dev11 & Roslyn: Unchanged
                    Const f_7_4 As Single = 1499.995f      ' Dev11 & Roslyn: Unchanged
                    Const f_7_5 As Single = 149999.5F      ' Dev11 & Roslyn: Unchanged
                    Const f_7_6 As Single = 1499995.0f     ' Dev11 & Roslyn: Unchanged

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 5 significant digits
                    Const f_5_1 As Single = 0.14995F        ' Dev11 & Roslyn: Pretty listed to 0.14995F
                    Const f_5_2 As Single = 0.14995F       ' Dev11 & Roslyn: Unchanged
                    Const f_5_3 As Single = 1.4995F        ' Dev11 & Roslyn: Unchanged
                    Const f_5_4 As Single = 149.95F        ' Dev11 & Roslyn: Unchanged
                    Const f_5_5 As Single = 1499.5F        ' Dev11 & Roslyn: Unchanged
                    Const f_5_6 As Single = 14995.0F       ' Dev11 & Roslyn: Unchanged

                    ' 7 significant digits
                    Const f_7_1 As Single = 0.1499995F      ' Dev11 & Roslyn: Pretty listed to 0.1499995F
                    Const f_7_2 As Single = 0.1499995F     ' Dev11 & Roslyn: Unchanged
                    Const f_7_3 As Single = 1.499995F      ' Dev11 & Roslyn: Unchanged
                    Const f_7_4 As Single = 1499.995F      ' Dev11 & Roslyn: Unchanged
                    Const f_7_5 As Single = 149999.5F      ' Dev11 & Roslyn: Unchanged
                    Const f_7_6 As Single = 1499995.0F     ' Dev11 & Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceSingleLiterals_LessThan8Digits_WithTypeCharacterSingle()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 5 significant digits
                    Const f_5_1 As Single = .14995!        ' Dev11 & Roslyn: Pretty listed to 0.14995!
                    Const f_5_2 As Single = 0.14995!       ' Dev11 & Roslyn: Unchanged
                    Const f_5_3 As Single = 1.4995!        ' Dev11 & Roslyn: Unchanged
                    Const f_5_4 As Single = 149.95!        ' Dev11 & Roslyn: Unchanged
                    Const f_5_5 As Single = 1499.5!        ' Dev11 & Roslyn: Unchanged
                    Const f_5_6 As Single = 14995.0!       ' Dev11 & Roslyn: Unchanged

                    ' 7 significant digits
                    Const f_7_1 As Single = .1499995!      ' Dev11 & Roslyn: Pretty listed to 0.1499995!
                    Const f_7_2 As Single = 0.1499995!     ' Dev11 & Roslyn: Unchanged
                    Const f_7_3 As Single = 1.499995!      ' Dev11 & Roslyn: Unchanged
                    Const f_7_4 As Single = 1499.995!      ' Dev11 & Roslyn: Unchanged
                    Const f_7_5 As Single = 149999.5!      ' Dev11 & Roslyn: Unchanged
                    Const f_7_6 As Single = 1499995.0!     ' Dev11 & Roslyn: Unchanged

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 5 significant digits
                    Const f_5_1 As Single = 0.14995!        ' Dev11 & Roslyn: Pretty listed to 0.14995!
                    Const f_5_2 As Single = 0.14995!       ' Dev11 & Roslyn: Unchanged
                    Const f_5_3 As Single = 1.4995!        ' Dev11 & Roslyn: Unchanged
                    Const f_5_4 As Single = 149.95!        ' Dev11 & Roslyn: Unchanged
                    Const f_5_5 As Single = 1499.5!        ' Dev11 & Roslyn: Unchanged
                    Const f_5_6 As Single = 14995.0!       ' Dev11 & Roslyn: Unchanged

                    ' 7 significant digits
                    Const f_7_1 As Single = 0.1499995!      ' Dev11 & Roslyn: Pretty listed to 0.1499995!
                    Const f_7_2 As Single = 0.1499995!     ' Dev11 & Roslyn: Unchanged
                    Const f_7_3 As Single = 1.499995!      ' Dev11 & Roslyn: Unchanged
                    Const f_7_4 As Single = 1499.995!      ' Dev11 & Roslyn: Unchanged
                    Const f_7_5 As Single = 149999.5!      ' Dev11 & Roslyn: Unchanged
                    Const f_7_6 As Single = 1499995.0!     ' Dev11 & Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceSingleLiterals_8Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

                    Const f_8_1 As Single = .14999795F      ' Dev11 & Roslyn: 0.14999795F
                    Const f_8_2 As Single = .14999797f      ' Dev11 & Roslyn: 0.149997965F

                    Const f_8_3 As Single = 0.1499797F      ' Dev11 & Roslyn: Unchanged

                    Const f_8_4 As Single = 1.4999794f      ' Dev11 & Roslyn: 1.49997938F
                    Const f_8_5 As Single = 1.4999797F      ' Dev11 & Roslyn: 1.49997973F

                    Const f_8_6 As Single = 1499.9794f      ' Dev11 & Roslyn: 1499.97937F

                    Const f_8_7 As Single = 1499979.7F      ' Dev11 & Roslyn: 1499979.75F

                    Const f_8_8 As Single = 14999797.0F     ' Dev11 & Roslyn: unchanged

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

                    Const f_8_1 As Single = 0.14999795F      ' Dev11 & Roslyn: 0.14999795F
                    Const f_8_2 As Single = {(IsNetCoreApp ? "0.14999796F" : "0.149997965F")}      ' Dev11 & Roslyn: 0.149997965F

                    Const f_8_3 As Single = 0.1499797F      ' Dev11 & Roslyn: Unchanged

                    Const f_8_4 As Single = {(IsNetCoreApp ? "1.4999794F" : "1.49997938F")}      ' Dev11 & Roslyn: 1.49997938F
                    Const f_8_5 As Single = {(IsNetCoreApp ? "1.4999797F" : "1.49997973F")}      ' Dev11 & Roslyn: 1.49997973F

                    Const f_8_6 As Single = {(IsNetCoreApp ? "1499.9794F" : "1499.97937F")}      ' Dev11 & Roslyn: 1499.97937F

                    Const f_8_7 As Single = {(IsNetCoreApp ? "1499979.8F" : "1499979.75F")}      ' Dev11 & Roslyn: 1499979.75F

                    Const f_8_8 As Single = 14999797.0F     ' Dev11 & Roslyn: unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceSingleLiterals_8Digits_WithTypeCharacterSingle()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

                    Const f_8_1 As Single = .14999795!      ' Dev11 & Roslyn: 0.14999795F
                    Const f_8_2 As Single = .14999797!      ' Dev11 & Roslyn: 0.149997965F

                    Const f_8_3 As Single = 0.1499797!      ' Dev11 & Roslyn: Unchanged

                    Const f_8_4 As Single = 1.4999794!      ' Dev11 & Roslyn: 1.49997938F
                    Const f_8_5 As Single = 1.4999797!      ' Dev11 & Roslyn: 1.49997973F

                    Const f_8_6 As Single = 1499.9794!      ' Dev11 & Roslyn: 1499.97937F

                    Const f_8_7 As Single = 1499979.7!      ' Dev11 & Roslyn: 1499979.75F

                    Const f_8_8 As Single = 14999797.0!     ' Dev11 & Roslyn: unchanged

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

                    Const f_8_1 As Single = 0.14999795!      ' Dev11 & Roslyn: 0.14999795F
                    Const f_8_2 As Single = {(IsNetCoreApp ? "0.14999796!" : "0.149997965!")}      ' Dev11 & Roslyn: 0.149997965F

                    Const f_8_3 As Single = 0.1499797!      ' Dev11 & Roslyn: Unchanged

                    Const f_8_4 As Single = {(IsNetCoreApp ? "1.4999794!" : "1.49997938!")}      ' Dev11 & Roslyn: 1.49997938F
                    Const f_8_5 As Single = {(IsNetCoreApp ? "1.4999797!" : "1.49997973!")}      ' Dev11 & Roslyn: 1.49997973F

                    Const f_8_6 As Single = {(IsNetCoreApp ? "1499.9794!" : "1499.97937!")}      ' Dev11 & Roslyn: 1499.97937F

                    Const f_8_7 As Single = {(IsNetCoreApp ? "1499979.8!" : "1499979.75!")}      ' Dev11 & Roslyn: 1499979.75F

                    Const f_8_8 As Single = 14999797.0!     ' Dev11 & Roslyn: unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceSingleLiterals_GreaterThan8Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits
                    
                    ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
                    Const f_9_1 As Single = .149997938F     ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_2 As Single = 0.149997931f    ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_3 As Single = 1.49997965F     ' Dev11 & Roslyn: 1.49997962F

                    Const f_10_1 As Single = 14999.79652f   ' Dev11 & Roslyn: 14999.7969F

                    ' (b) > 8 significant digits before decimal point.
                    Const f_10_2 As Single = 149997965.2F   ' Dev11 & Roslyn: 149997968.0F
                    Const f_10_3 As Single = 1499979652.0f  ' Dev11 & Roslyn: 1.49997965E+9F

                    Const f_24_1 As Single = 111111149999124689999.499F      ' Dev11 & Roslyn: 1.11111148E+20F

                    ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
                    '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
                    '     from 1.401298E-45 through 3.4028235E+38 for positive values.
                    
                    Const f_overflow_1 As Single = -3.4028235E+39F          ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Single = 3.4028235E+39F           ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Single = -1.401298E-47F          ' Dev11: -0.0F, Roslyn: Unchanged
                    Const f_underflow_2 As Single = 1.401298E-47F           ' Dev11: 0.0F, Roslyn: Unchanged
                    
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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

                    ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
                    Const f_9_1 As Single = {(IsNetCoreApp ? "0.14999793F" : "0.149997935F")}     ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_2 As Single = {(IsNetCoreApp ? "0.14999793F" : "0.149997935F")}    ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_3 As Single = {(IsNetCoreApp ? "1.4999796F" : "1.49997962F")}     ' Dev11 & Roslyn: 1.49997962F

                    Const f_10_1 As Single = {(IsNetCoreApp ? "14999.797F" : "14999.7969F")}   ' Dev11 & Roslyn: 14999.7969F

                    ' (b) > 8 significant digits before decimal point.
                    Const f_10_2 As Single = {(IsNetCoreApp ? "149997970.0F" : "149997968.0F")}   ' Dev11 & Roslyn: 149997968.0F
                    Const f_10_3 As Single = {(IsNetCoreApp ? "1.4999796E+9F" : "1.49997965E+9F")}  ' Dev11 & Roslyn: 1.49997965E+9F

                    Const f_24_1 As Single = {(IsNetCoreApp ? "1.1111115E+20F" : "1.11111148E+20F")}      ' Dev11 & Roslyn: 1.11111148E+20F

                    ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
                    '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
                    '     from 1.401298E-45 through 3.4028235E+38 for positive values.

                    Const f_overflow_1 As Single = -3.4028235E+39F          ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Single = 3.4028235E+39F           ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Single = -1.401298E-47F          ' Dev11: -0.0F, Roslyn: Unchanged
                    Const f_underflow_2 As Single = 1.401298E-47F           ' Dev11: 0.0F, Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceSingleLiterals_GreaterThan8Digits_WithTypeCharacterSingle()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits
                    
                    ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
                    Const f_9_1 As Single = .149997938!     ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_2 As Single = 0.149997931!    ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_3 As Single = 1.49997965!     ' Dev11 & Roslyn: 1.49997962F

                    Const f_10_1 As Single = 14999.79652!   ' Dev11 & Roslyn: 14999.7969F

                    ' (b) > 8 significant digits before decimal point.
                    Const f_10_2 As Single = 149997965.2!   ' Dev11 & Roslyn: 149997968.0F
                    Const f_10_3 As Single = 1499979652.0!  ' Dev11 & Roslyn: 1.49997965E+9F

                    Const f_24_1 As Single = 111111149999124689999.499!      ' Dev11 & Roslyn: 1.11111148E+20F

                    ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
                    '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
                    '     from 1.401298E-45 through 3.4028235E+38 for positive values.
                    
                    Const f_overflow_1 As Single = -3.4028235E+39!          ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Single = 3.4028235E+39!           ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Single = -1.401298E-47!          ' Dev11: -0.0F, Roslyn: Unchanged
                    Const f_underflow_2 As Single = 1.401298E-47!           ' Dev11: 0.0F, Roslyn: Unchanged

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 8 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 9 significant digits

                    ' (a) > 8 significant digits overall, but < 8 digits before decimal point.
                    Const f_9_1 As Single = {(IsNetCoreApp ? "0.14999793!" : "0.149997935!")}     ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_2 As Single = {(IsNetCoreApp ? "0.14999793!" : "0.149997935!")}    ' Dev11 & Roslyn: 0.149997935F
                    Const f_9_3 As Single = {(IsNetCoreApp ? "1.4999796!" : "1.49997962!")}     ' Dev11 & Roslyn: 1.49997962F

                    Const f_10_1 As Single = {(IsNetCoreApp ? "14999.797!" : "14999.7969!")}   ' Dev11 & Roslyn: 14999.7969F

                    ' (b) > 8 significant digits before decimal point.
                    Const f_10_2 As Single = {(IsNetCoreApp ? "149997970.0!" : "149997968.0!")}   ' Dev11 & Roslyn: 149997968.0F
                    Const f_10_3 As Single = {(IsNetCoreApp ? "1.4999796E+9!" : "1.49997965E+9!")}  ' Dev11 & Roslyn: 1.49997965E+9F

                    Const f_24_1 As Single = {(IsNetCoreApp ? "1.1111115E+20!" : "1.11111148E+20!")}      ' Dev11 & Roslyn: 1.11111148E+20F

                    ' (c) Overflow/Underflow cases for Single: Ensure no pretty listing/round off
                    '     Holds signed IEEE 32-bit (4-byte) single-precision floating-point numbers ranging in value from -3.4028235E+38 through -1.401298E-45 for negative values and
                    '     from 1.401298E-45 through 3.4028235E+38 for positive values.

                    Const f_overflow_1 As Single = -3.4028235E+39!          ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Single = 3.4028235E+39!           ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Single = -1.401298E-47!          ' Dev11: -0.0F, Roslyn: Unchanged
                    Const f_underflow_2 As Single = 1.401298E-47!           ' Dev11: 0.0F, Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDoubleLiterals_LessThan16Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 16 significant digits precision,
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 13 significant digits
                    Const f_13_1 As Double = .1499599999999         ' Dev11 & Roslyn: Pretty listed to 0.1499599999999
                    Const f_13_2 As Double = 0.149959999999         ' Dev11 & Roslyn: Unchanged
                    Const f_13_3 As Double = 1.499599999999         ' Dev11 & Roslyn: Unchanged
                    Const f_13_4 As Double = 1499599.999999         ' Dev11 & Roslyn: Unchanged
                    Const f_13_5 As Double = 149959999999.9         ' Dev11 & Roslyn: Unchanged
                    Const f_13_6 As Double = 1499599999999.0        ' Dev11 & Roslyn: Unchanged

                    ' 15 significant digits
                    Const f_15_1 As Double = .149999999999995       ' Dev11 & Roslyn: Pretty listed to 0.149999999999995
                    Const f_15_2 As Double = 0.14999999999995       ' Dev11 & Roslyn: Unchanged
                    Const f_15_3 As Double = 1.49999999999995       ' Dev11 & Roslyn: Unchanged
                    Const f_15_4 As Double = 14999999.9999995       ' Dev11 & Roslyn: Unchanged
                    Const f_15_5 As Double = 14999999999999.5       ' Dev11 & Roslyn: Unchanged
                    Const f_15_6 As Double = 149999999999995.0      ' Dev11 & Roslyn: Unchanged

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 16 significant digits precision,
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 13 significant digits
                    Const f_13_1 As Double = 0.1499599999999         ' Dev11 & Roslyn: Pretty listed to 0.1499599999999
                    Const f_13_2 As Double = 0.149959999999         ' Dev11 & Roslyn: Unchanged
                    Const f_13_3 As Double = 1.499599999999         ' Dev11 & Roslyn: Unchanged
                    Const f_13_4 As Double = 1499599.999999         ' Dev11 & Roslyn: Unchanged
                    Const f_13_5 As Double = 149959999999.9         ' Dev11 & Roslyn: Unchanged
                    Const f_13_6 As Double = 1499599999999.0        ' Dev11 & Roslyn: Unchanged

                    ' 15 significant digits
                    Const f_15_1 As Double = 0.149999999999995       ' Dev11 & Roslyn: Pretty listed to 0.149999999999995
                    Const f_15_2 As Double = 0.14999999999995       ' Dev11 & Roslyn: Unchanged
                    Const f_15_3 As Double = 1.49999999999995       ' Dev11 & Roslyn: Unchanged
                    Const f_15_4 As Double = 14999999.9999995       ' Dev11 & Roslyn: Unchanged
                    Const f_15_5 As Double = 14999999999999.5       ' Dev11 & Roslyn: Unchanged
                    Const f_15_6 As Double = 149999999999995.0      ' Dev11 & Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDoubleLiterals_LessThan16Digits_WithTypeCharacter()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 16 significant digits precision,
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 13 significant digits
                    Const f_13_1 As Double = .1499599999999R         ' Dev11 & Roslyn: Pretty listed to 0.1499599999999
                    Const f_13_2 As Double = 0.149959999999r         ' Dev11 & Roslyn: Unchanged
                    Const f_13_3 As Double = 1.499599999999#         ' Dev11 & Roslyn: Unchanged
                    Const f_13_4 As Double = 1499599.999999#         ' Dev11 & Roslyn: Unchanged
                    Const f_13_5 As Double = 149959999999.9r         ' Dev11 & Roslyn: Unchanged
                    Const f_13_6 As Double = 1499599999999.0R        ' Dev11 & Roslyn: Unchanged

                    ' 15 significant digits
                    Const f_15_1 As Double = .149999999999995R       ' Dev11 & Roslyn: Pretty listed to 0.149999999999995
                    Const f_15_2 As Double = 0.14999999999995r       ' Dev11 & Roslyn: Unchanged
                    Const f_15_3 As Double = 1.49999999999995#       ' Dev11 & Roslyn: Unchanged
                    Const f_15_4 As Double = 14999999.9999995#       ' Dev11 & Roslyn: Unchanged
                    Const f_15_5 As Double = 14999999999999.5r       ' Dev11 & Roslyn: Unchanged
                    Const f_15_6 As Double = 149999999999995.0R      ' Dev11 & Roslyn: Unchanged

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 16 significant digits precision,
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 13 significant digits
                    Const f_13_1 As Double = 0.1499599999999R         ' Dev11 & Roslyn: Pretty listed to 0.1499599999999
                    Const f_13_2 As Double = 0.149959999999R         ' Dev11 & Roslyn: Unchanged
                    Const f_13_3 As Double = 1.499599999999#         ' Dev11 & Roslyn: Unchanged
                    Const f_13_4 As Double = 1499599.999999#         ' Dev11 & Roslyn: Unchanged
                    Const f_13_5 As Double = 149959999999.9R         ' Dev11 & Roslyn: Unchanged
                    Const f_13_6 As Double = 1499599999999.0R        ' Dev11 & Roslyn: Unchanged

                    ' 15 significant digits
                    Const f_15_1 As Double = 0.149999999999995R       ' Dev11 & Roslyn: Pretty listed to 0.149999999999995
                    Const f_15_2 As Double = 0.14999999999995R       ' Dev11 & Roslyn: Unchanged
                    Const f_15_3 As Double = 1.49999999999995#       ' Dev11 & Roslyn: Unchanged
                    Const f_15_4 As Double = 14999999.9999995#       ' Dev11 & Roslyn: Unchanged
                    Const f_15_5 As Double = 14999999999999.5R       ' Dev11 & Roslyn: Unchanged
                    Const f_15_6 As Double = 149999999999995.0R      ' Dev11 & Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDoubleLiterals_16Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    Const f_16_1 As Double = .1499999999799993      ' Dev11 & Roslyn: 0.1499999999799993
                    Const f_16_2 As Double = .1499999999799997      ' Dev11 & Roslyn: 0.14999999997999969

                    Const f_16_3 As Double = 0.149999999799995      ' Dev11 & Roslyn: Unchanged

                    Const f_16_4 As Double = 1.499999999799994      ' Dev11 & Roslyn: Unchanged
                    Const f_16_5 As Double = 1.499999999799995      ' Dev11 & Roslyn: 1.4999999997999951

                    Const f_16_6 As Double = 14999999.99799994      ' Dev11 & Roslyn: Unchanged
                    Const f_16_7 As Double = 14999999.99799995      ' Dev11 & Roslyn: 14999999.997999949

                    Const f_16_8 As Double = 149999999997999.2      ' Dev11 & Roslyn: 149999999997999.19
                    Const f_16_9 As Double = 149999999997999.8      ' Dev11 & Roslyn: 149999999997999.81

                    Const f_16_10 As Double = 1499999999979995.0    ' Dev11 & Roslyn: Unchanged

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    Const f_16_1 As Double = 0.1499999999799993      ' Dev11 & Roslyn: 0.1499999999799993
                    Const f_16_2 As Double = {(IsNetCoreApp ? "0.1499999999799997" : "0.14999999997999969")}      ' Dev11 & Roslyn: 0.14999999997999969

                    Const f_16_3 As Double = 0.149999999799995      ' Dev11 & Roslyn: Unchanged

                    Const f_16_4 As Double = 1.499999999799994      ' Dev11 & Roslyn: Unchanged
                    Const f_16_5 As Double = {(IsNetCoreApp ? "1.499999999799995" : "1.4999999997999951")}      ' Dev11 & Roslyn: 1.4999999997999951

                    Const f_16_6 As Double = 14999999.99799994      ' Dev11 & Roslyn: Unchanged
                    Const f_16_7 As Double = {(IsNetCoreApp ? "14999999.99799995" : "14999999.997999949")}      ' Dev11 & Roslyn: 14999999.997999949

                    Const f_16_8 As Double = {(IsNetCoreApp ? "149999999997999.2" : "149999999997999.19")}      ' Dev11 & Roslyn: 149999999997999.19
                    Const f_16_9 As Double = {(IsNetCoreApp ? "149999999997999.8" : "149999999997999.81")}      ' Dev11 & Roslyn: 149999999997999.81

                    Const f_16_10 As Double = 1499999999979995.0    ' Dev11 & Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDoubleLiterals_16Digits_WithTypeCharacter()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    Const f_16_1 As Double = .1499999999799993R      ' Dev11 & Roslyn: 0.1499999999799993
                    Const f_16_2 As Double = .1499999999799997r      ' Dev11 & Roslyn: 0.14999999997999969

                    Const f_16_3 As Double = 0.149999999799995#      ' Dev11 & Roslyn: Unchanged

                    Const f_16_4 As Double = 1.499999999799994R      ' Dev11 & Roslyn: Unchanged
                    Const f_16_5 As Double = 1.499999999799995r      ' Dev11 & Roslyn: 1.4999999997999951

                    Const f_16_6 As Double = 14999999.99799994#      ' Dev11 & Roslyn: Unchanged
                    Const f_16_7 As Double = 14999999.99799995R      ' Dev11 & Roslyn: 14999999.997999949

                    Const f_16_8 As Double = 149999999997999.2r      ' Dev11 & Roslyn: 149999999997999.19
                    Const f_16_9 As Double = 149999999997999.8#      ' Dev11 & Roslyn: 149999999997999.81

                    Const f_16_10 As Double = 1499999999979995.0R    ' Dev11 & Roslyn: Unchanged

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    Const f_16_1 As Double = 0.1499999999799993R      ' Dev11 & Roslyn: 0.1499999999799993
                    Const f_16_2 As Double = {(IsNetCoreApp ? "0.1499999999799997R" : "0.14999999997999969R")}      ' Dev11 & Roslyn: 0.14999999997999969

                    Const f_16_3 As Double = 0.149999999799995#      ' Dev11 & Roslyn: Unchanged

                    Const f_16_4 As Double = 1.499999999799994R      ' Dev11 & Roslyn: Unchanged
                    Const f_16_5 As Double = {(IsNetCoreApp ? "1.499999999799995R" : "1.4999999997999951R")}      ' Dev11 & Roslyn: 1.4999999997999951

                    Const f_16_6 As Double = 14999999.99799994#      ' Dev11 & Roslyn: Unchanged
                    Const f_16_7 As Double = {(IsNetCoreApp ? "14999999.99799995R" : "14999999.997999949R")}      ' Dev11 & Roslyn: 14999999.997999949

                    Const f_16_8 As Double = {(IsNetCoreApp ? "149999999997999.2R" : "149999999997999.19R")}      ' Dev11 & Roslyn: 149999999997999.19
                    Const f_16_9 As Double = {(IsNetCoreApp ? "149999999997999.8#" : "149999999997999.81#")}      ' Dev11 & Roslyn: 149999999997999.81

                    Const f_16_10 As Double = 1499999999979995.0R    ' Dev11 & Roslyn: Unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDoubleLiterals_GreaterThan16Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
                    Const f_17_1 As Double = .14999999997999938     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_2 As Double = .14999999997999939     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_3 As Double = .14999999997999937     ' Dev11 & Roslyn: 0.14999999997999938

                    Const f_17_4 As Double = 0.1499999997999957     ' Dev11 & Roslyn: Unchanged
                    Const f_17_5 As Double = 0.1499999997999958     ' Dev11 & Roslyn: 0.14999999979999579

                    Const f_17_6 As Double = 1.4999999997999947     ' Dev11 & Roslyn: Unchanged
                    Const f_17_7 As Double = 1.4999999997999945     ' Dev11 & Roslyn: 1.4999999997999944
                    Const f_17_8 As Double = 1.4999999997999946     ' Dev11 & Roslyn: 1.4999999997999947

                    Const f_18_1 As Double = 14999999.9979999459    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_2 As Double = 14999999.9979999451    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_3 As Double = 14999999.9979999454    ' Dev11 & Roslyn: 14999999.997999946

                    ' (b) > 16 significant digits before decimal point.
                    Const f_18_4 As Double = 14999999999733999.2    ' Dev11 & Roslyn: 1.4999999999734E+16
                    Const f_18_5 As Double = 14999999999379995.0    ' Dev11 & Roslyn: 14999999999379996.0

                    Const f_24_1 As Double = 111111149999124689999.499      ' Dev11 & Roslyn: 1.1111114999912469E+20

                    ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
                    '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
                    '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

                    Const f_overflow_1 As Double = -1.79769313486231570E+309        ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Double = 1.79769313486231570E+309         ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Double = -4.94065645841246544E-326       ' Dev11: -0.0F, Roslyn: unchanged
                    Const f_underflow_2 As Double = 4.94065645841246544E-326        ' Dev11: 0.0F, Roslyn: unchanged

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
                    Const f_17_1 As Double = 0.14999999997999938     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_2 As Double = 0.14999999997999938     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_3 As Double = 0.14999999997999938     ' Dev11 & Roslyn: 0.14999999997999938

                    Const f_17_4 As Double = 0.1499999997999957     ' Dev11 & Roslyn: Unchanged
                    Const f_17_5 As Double = {(IsNetCoreApp ? "0.1499999997999958" : "0.14999999979999579")}     ' Dev11 & Roslyn: 0.14999999979999579

                    Const f_17_6 As Double = 1.4999999997999947     ' Dev11 & Roslyn: Unchanged
                    Const f_17_7 As Double = 1.4999999997999944     ' Dev11 & Roslyn: 1.4999999997999944
                    Const f_17_8 As Double = 1.4999999997999947     ' Dev11 & Roslyn: 1.4999999997999947

                    Const f_18_1 As Double = 14999999.997999946    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_2 As Double = 14999999.997999946    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_3 As Double = 14999999.997999946    ' Dev11 & Roslyn: 14999999.997999946

                    ' (b) > 16 significant digits before decimal point.
                    Const f_18_4 As Double = {(IsNetCoreApp ? "14999999999734000.0" : "1.4999999999734E+16")}    ' Dev11 & Roslyn: 1.4999999999734E+16
                    Const f_18_5 As Double = 14999999999379996.0    ' Dev11 & Roslyn: 14999999999379996.0

                    Const f_24_1 As Double = {(IsNetCoreApp ? "1.111111499991247E+20" : "1.1111114999912469E+20")}      ' Dev11 & Roslyn: 1.1111114999912469E+20

                    ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
                    '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
                    '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

                    Const f_overflow_1 As Double = -1.79769313486231570E+309        ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Double = 1.79769313486231570E+309         ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Double = -4.94065645841246544E-326       ' Dev11: -0.0F, Roslyn: unchanged
                    Const f_underflow_2 As Double = 4.94065645841246544E-326        ' Dev11: 0.0F, Roslyn: unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDoubleLiterals_GreaterThan16Digits_WithTypeCharacter()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
                    Const f_17_1 As Double = .14999999997999938R     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_2 As Double = .14999999997999939r     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_3 As Double = .14999999997999937#     ' Dev11 & Roslyn: 0.14999999997999938

                    Const f_17_4 As Double = 0.1499999997999957R     ' Dev11 & Roslyn: Unchanged
                    Const f_17_5 As Double = 0.1499999997999958r     ' Dev11 & Roslyn: 0.14999999979999579

                    Const f_17_6 As Double = 1.4999999997999947#     ' Dev11 & Roslyn: Unchanged
                    Const f_17_7 As Double = 1.4999999997999945R     ' Dev11 & Roslyn: 1.4999999997999944
                    Const f_17_8 As Double = 1.4999999997999946r     ' Dev11 & Roslyn: 1.4999999997999947

                    Const f_18_1 As Double = 14999999.9979999459#    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_2 As Double = 14999999.9979999451R    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_3 As Double = 14999999.9979999454r    ' Dev11 & Roslyn: 14999999.997999946

                    ' (b) > 16 significant digits before decimal point.
                    Const f_18_4 As Double = 14999999999733999.2#    ' Dev11 & Roslyn: 1.4999999999734E+16
                    Const f_18_5 As Double = 14999999999379995.0R    ' Dev11 & Roslyn: 14999999999379996.0

                    Const f_24_1 As Double = 111111149999124689999.499r      ' Dev11 & Roslyn: 1.1111114999912469E+20

                    ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
                    '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
                    '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

                    Const f_overflow_1 As Double = -1.79769313486231570E+309#        ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Double = 1.79769313486231570E+309R         ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Double = -4.94065645841246544E-326r       ' Dev11: -0.0F, Roslyn: unchanged
                    Const f_underflow_2 As Double = 4.94065645841246544E-326#        ' Dev11: 0.0F, Roslyn: unchanged

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 16 significant digits
                    ' Dev11 and Roslyn behavior are identical: Always rounded off and pretty listed to <= 17 significant digits

                    ' (a) > 16 significant digits overall, but < 16 digits before decimal point.
                    Const f_17_1 As Double = 0.14999999997999938R     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_2 As Double = 0.14999999997999938R     ' Dev11 & Roslyn: 0.14999999997999938
                    Const f_17_3 As Double = 0.14999999997999938#     ' Dev11 & Roslyn: 0.14999999997999938

                    Const f_17_4 As Double = 0.1499999997999957R     ' Dev11 & Roslyn: Unchanged
                    Const f_17_5 As Double = {(IsNetCoreApp ? "0.1499999997999958R" : "0.14999999979999579R")}     ' Dev11 & Roslyn: 0.14999999979999579

                    Const f_17_6 As Double = 1.4999999997999947#     ' Dev11 & Roslyn: Unchanged
                    Const f_17_7 As Double = 1.4999999997999944R     ' Dev11 & Roslyn: 1.4999999997999944
                    Const f_17_8 As Double = 1.4999999997999947R     ' Dev11 & Roslyn: 1.4999999997999947

                    Const f_18_1 As Double = 14999999.997999946#    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_2 As Double = 14999999.997999946R    ' Dev11 & Roslyn: 14999999.997999946
                    Const f_18_3 As Double = 14999999.997999946R    ' Dev11 & Roslyn: 14999999.997999946

                    ' (b) > 16 significant digits before decimal point.
                    Const f_18_4 As Double = {(IsNetCoreApp ? "14999999999734000.0#" : "1.4999999999734E+16#")}    ' Dev11 & Roslyn: 1.4999999999734E+16
                    Const f_18_5 As Double = 14999999999379996.0R    ' Dev11 & Roslyn: 14999999999379996.0

                    Const f_24_1 As Double = {(IsNetCoreApp ? "1.111111499991247E+20R" : "1.1111114999912469E+20R")}      ' Dev11 & Roslyn: 1.1111114999912469E+20

                    ' (c) Overflow/Underflow cases for Double: Ensure no pretty listing/round off
                    '     Holds signed IEEE 64-bit (8-byte) double-precision floating-point numbers ranging in value from -1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values and
                    '     from 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values.

                    Const f_overflow_1 As Double = -1.79769313486231570E+309#        ' Dev11 & Roslyn: Unchanged
                    Const f_overflow_2 As Double = 1.79769313486231570E+309R         ' Dev11 & Roslyn: Unchanged
                    Const f_underflow_1 As Double = -4.94065645841246544E-326R       ' Dev11: -0.0F, Roslyn: unchanged
                    Const f_underflow_2 As Double = 4.94065645841246544E-326#        ' Dev11: 0.0F, Roslyn: unchanged

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDecimalLiterals_LessThan30Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 30 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 27 significant digits
                    Const d_27_1 As Decimal = .123456789012345678901234567D        ' Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
                    Const d_27_2 As Decimal = 0.123456789012345678901234567d       ' Dev11 & Roslyn: Unchanged
                    Const d_27_3 As Decimal = 1.23456789012345678901234567D        ' Dev11 & Roslyn: Unchanged
                    Const d_27_4 As Decimal = 123456789012.345678901234567d        ' Dev11 & Roslyn: Unchanged
                    Const d_27_5 As Decimal = 12345678901234567890123456.7D        ' Dev11 & Roslyn: Unchanged
                    Const d_27_6 As Decimal = 123456789012345678901234567.0d       ' Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

                    ' 29 significant digits
                    Const d_29_1 As Decimal = .12345678901234567890123456789D      ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_29_2 As Decimal = 0.12345678901234567890123456789d     ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_29_3 As Decimal = 1.2345678901234567890123456789D      ' Dev11 & Roslyn: Unchanged
                    Const d_29_4 As Decimal = 123456789012.34567890123456789d      ' Dev11 & Roslyn: Unchanged
                    Const d_29_5 As Decimal = 1234567890123456789012345678.9D      ' Dev11 & Roslyn: Unchanged
                    Const d_29_6 As Decimal = 12345678901234567890123456789.0d     ' Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 30 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 27 significant digits
                    Const d_27_1 As Decimal = 0.123456789012345678901234567D        ' Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
                    Const d_27_2 As Decimal = 0.123456789012345678901234567D       ' Dev11 & Roslyn: Unchanged
                    Const d_27_3 As Decimal = 1.23456789012345678901234567D        ' Dev11 & Roslyn: Unchanged
                    Const d_27_4 As Decimal = 123456789012.345678901234567D        ' Dev11 & Roslyn: Unchanged
                    Const d_27_5 As Decimal = 12345678901234567890123456.7D        ' Dev11 & Roslyn: Unchanged
                    Const d_27_6 As Decimal = 123456789012345678901234567D       ' Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

                    ' 29 significant digits
                    Const d_29_1 As Decimal = 0.1234567890123456789012345679D      ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_29_2 As Decimal = 0.1234567890123456789012345679D     ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_29_3 As Decimal = 1.2345678901234567890123456789D      ' Dev11 & Roslyn: Unchanged
                    Const d_29_4 As Decimal = 123456789012.34567890123456789D      ' Dev11 & Roslyn: Unchanged
                    Const d_29_5 As Decimal = 1234567890123456789012345678.9D      ' Dev11 & Roslyn: Unchanged
                    Const d_29_6 As Decimal = 12345678901234567890123456789D     ' Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDecimalLiterals_LessThan30Digits_WithTypeCharacterDecimal()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 30 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 27 significant digits
                    Const d_27_1 As Decimal = .123456789012345678901234567@        ' Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
                    Const d_27_2 As Decimal = 0.123456789012345678901234567@       ' Dev11 & Roslyn: Unchanged
                    Const d_27_3 As Decimal = 1.23456789012345678901234567@        ' Dev11 & Roslyn: Unchanged
                    Const d_27_4 As Decimal = 123456789012.345678901234567@        ' Dev11 & Roslyn: Unchanged
                    Const d_27_5 As Decimal = 12345678901234567890123456.7@        ' Dev11 & Roslyn: Unchanged
                    Const d_27_6 As Decimal = 123456789012345678901234567.0@       ' Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

                    ' 29 significant digits
                    Const d_29_1 As Decimal = .12345678901234567890123456789@      ' Dev11 & Roslyn: 0.1234567890123456789012345679@
                    Const d_29_2 As Decimal = 0.12345678901234567890123456789@     ' Dev11 & Roslyn: 0.1234567890123456789012345679@
                    Const d_29_3 As Decimal = 1.2345678901234567890123456789@      ' Dev11 & Roslyn: Unchanged
                    Const d_29_4 As Decimal = 123456789012.34567890123456789@      ' Dev11 & Roslyn: Unchanged
                    Const d_29_5 As Decimal = 1234567890123456789012345678.9@      ' Dev11 & Roslyn: Unchanged
                    Const d_29_6 As Decimal = 12345678901234567890123456789.0@     ' Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 1: Less than 30 significant digits
                    ' Dev11 and Roslyn behavior are identical: UNCHANGED

                    ' 27 significant digits
                    Const d_27_1 As Decimal = 0.123456789012345678901234567@        ' Dev11 & Roslyn: Pretty listed to 0.123456789012345678901234567D
                    Const d_27_2 As Decimal = 0.123456789012345678901234567@       ' Dev11 & Roslyn: Unchanged
                    Const d_27_3 As Decimal = 1.23456789012345678901234567@        ' Dev11 & Roslyn: Unchanged
                    Const d_27_4 As Decimal = 123456789012.345678901234567@        ' Dev11 & Roslyn: Unchanged
                    Const d_27_5 As Decimal = 12345678901234567890123456.7@        ' Dev11 & Roslyn: Unchanged
                    Const d_27_6 As Decimal = 123456789012345678901234567@       ' Dev11 & Roslyn: Pretty listed to 123456789012345678901234567D

                    ' 29 significant digits
                    Const d_29_1 As Decimal = 0.1234567890123456789012345679@      ' Dev11 & Roslyn: 0.1234567890123456789012345679@
                    Const d_29_2 As Decimal = 0.1234567890123456789012345679@     ' Dev11 & Roslyn: 0.1234567890123456789012345679@
                    Const d_29_3 As Decimal = 1.2345678901234567890123456789@      ' Dev11 & Roslyn: Unchanged
                    Const d_29_4 As Decimal = 123456789012.34567890123456789@      ' Dev11 & Roslyn: Unchanged
                    Const d_29_5 As Decimal = 1234567890123456789012345678.9@      ' Dev11 & Roslyn: Unchanged
                    Const d_29_6 As Decimal = 12345678901234567890123456789@     ' Dev11 & Roslyn: Pretty listed to 12345678901234567890123456789D

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDecimalLiterals_30Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 30 significant digits
                    ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits
                    
                    Const d_30_1 As Decimal = .123456789012345678901234567891D          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_30_2 As Decimal = 0.1234567890123456789012345687891D        ' Dev11 & Roslyn: 0.1234567890123456789012345688D
                    Const d_30_3 As Decimal = 1.23456789012345678901234567891D          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
                    Const d_30_4 As Decimal = 123456789012345.678901234567891D          ' Dev11 & Roslyn: 123456789012345.67890123456789D
                    Const d_30_5 As Decimal = 12345678901234567890123456789.1D          ' Dev11 & Roslyn: 12345678901234567890123456789D

                    ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
                    Const d_30_6 As Decimal = 123456789012345678901234567891.0D          ' Dev11 & Roslyn: 123456789012345678901234567891.0D

                    Console.WriteLine(d_30_1)
                    Console.WriteLine(d_30_2)
                    Console.WriteLine(d_30_3)
                    Console.WriteLine(d_30_4)
                    Console.WriteLine(d_30_5)
                    Console.WriteLine(d_30_6)
                End Sub
            End Module
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 30 significant digits
                    ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits

                    Const d_30_1 As Decimal = 0.1234567890123456789012345679D          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_30_2 As Decimal = 0.1234567890123456789012345688D        ' Dev11 & Roslyn: 0.1234567890123456789012345688D
                    Const d_30_3 As Decimal = 1.2345678901234567890123456789D          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
                    Const d_30_4 As Decimal = 123456789012345.67890123456789D          ' Dev11 & Roslyn: 123456789012345.67890123456789D
                    Const d_30_5 As Decimal = 12345678901234567890123456789D          ' Dev11 & Roslyn: 12345678901234567890123456789D

                    ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
                    Const d_30_6 As Decimal = 123456789012345678901234567891.0D          ' Dev11 & Roslyn: 123456789012345678901234567891.0D

                    Console.WriteLine(d_30_1)
                    Console.WriteLine(d_30_2)
                    Console.WriteLine(d_30_3)
                    Console.WriteLine(d_30_4)
                    Console.WriteLine(d_30_5)
                    Console.WriteLine(d_30_6)
                End Sub
            End Module

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDecimalLiterals_30Digits_WithTypeCharacterDecimal()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 30 significant digits
                    ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits
                    
                    Const d_30_1 As Decimal = .123456789012345678901234567891@          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_30_2 As Decimal = 0.1234567890123456789012345687891@        ' Dev11 & Roslyn: 0.1234567890123456789012345688D
                    Const d_30_3 As Decimal = 1.23456789012345678901234567891@          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
                    Const d_30_4 As Decimal = 123456789012345.678901234567891@          ' Dev11 & Roslyn: 123456789012345.67890123456789D
                    Const d_30_5 As Decimal = 12345678901234567890123456789.1@          ' Dev11 & Roslyn: 12345678901234567890123456789D

                    ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
                    Const d_30_6 As Decimal = 123456789012345678901234567891.0@          ' Dev11 & Roslyn: 123456789012345678901234567891.0D

                    Console.WriteLine(d_30_1)
                    Console.WriteLine(d_30_2)
                    Console.WriteLine(d_30_3)
                    Console.WriteLine(d_30_4)
                    Console.WriteLine(d_30_5)
                    Console.WriteLine(d_30_6)
                End Sub
            End Module
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 2: 30 significant digits
                    ' Dev11 & Roslyn have identical behavior: pretty listed and round off to <= 29 significant digits

                    Const d_30_1 As Decimal = 0.1234567890123456789012345679@          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_30_2 As Decimal = 0.1234567890123456789012345688@        ' Dev11 & Roslyn: 0.1234567890123456789012345688D
                    Const d_30_3 As Decimal = 1.2345678901234567890123456789@          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
                    Const d_30_4 As Decimal = 123456789012345.67890123456789@          ' Dev11 & Roslyn: 123456789012345.67890123456789D
                    Const d_30_5 As Decimal = 12345678901234567890123456789@          ' Dev11 & Roslyn: 12345678901234567890123456789D

                    ' Overflow case 30 significant digits before decimal place: Ensure no pretty listing.
                    Const d_30_6 As Decimal = 123456789012345678901234567891.0@          ' Dev11 & Roslyn: 123456789012345678901234567891.0D

                    Console.WriteLine(d_30_1)
                    Console.WriteLine(d_30_2)
                    Console.WriteLine(d_30_3)
                    Console.WriteLine(d_30_4)
                    Console.WriteLine(d_30_5)
                    Console.WriteLine(d_30_6)
                End Sub
            End Module

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDecimalLiterals_GreaterThan30Digits()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 30 significant digits
                    ' Dev11 has unpredictable behavior: pretty listed/round off to wrong values in certain cases
                    ' Roslyn behavior: Always rounded off + pretty listed to <= 29 significant digits
                    
                    ' (a) > 30 significant digits overall, but < 30 digits before decimal point.
                    Const d_32_1 As Decimal = .12345678901234567890123456789012D          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_32_2 As Decimal = 0.123456789012345678901234568789012@        ' Dev11 & Roslyn: 0.1234567890123456789012345688@
                    Const d_32_3 As Decimal = 1.2345678901234567890123456789012d          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
                    Const d_32_4 As Decimal = 123456789012345.67890123456789012@          ' Dev11 & Roslyn: 123456789012345.67890123456789@
                    
                    ' (b) > 30 significant digits before decimal point (Overflow case): Ensure no pretty listing.
                    Const d_35_1 As Decimal = 123456789012345678901234567890123.45D          ' Dev11 & Roslyn: 123456789012345678901234567890123.45D

                    Console.WriteLine(d_32_1)
                    Console.WriteLine(d_32_2)
                    Console.WriteLine(d_32_3)
                    Console.WriteLine(d_32_4)
                    Console.WriteLine(d_35_1)
                End Sub
            End Module
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    ' CATEGORY 3: > 30 significant digits
                    ' Dev11 has unpredictable behavior: pretty listed/round off to wrong values in certain cases
                    ' Roslyn behavior: Always rounded off + pretty listed to <= 29 significant digits

                    ' (a) > 30 significant digits overall, but < 30 digits before decimal point.
                    Const d_32_1 As Decimal = 0.1234567890123456789012345679D          ' Dev11 & Roslyn: 0.1234567890123456789012345679D
                    Const d_32_2 As Decimal = 0.1234567890123456789012345688@        ' Dev11 & Roslyn: 0.1234567890123456789012345688@
                    Const d_32_3 As Decimal = 1.2345678901234567890123456789D          ' Dev11 & Roslyn: 1.2345678901234567890123456789D
                    Const d_32_4 As Decimal = 123456789012345.67890123456789@          ' Dev11 & Roslyn: 123456789012345.67890123456789@

                    ' (b) > 30 significant digits before decimal point (Overflow case): Ensure no pretty listing.
                    Const d_35_1 As Decimal = 123456789012345678901234567890123.45D          ' Dev11 & Roslyn: 123456789012345678901234567890123.45D

                    Console.WriteLine(d_32_1)
                    Console.WriteLine(d_32_2)
                    Console.WriteLine(d_32_3)
                    Console.WriteLine(d_32_4)
                    Console.WriteLine(d_35_1)
                End Sub
            End Module

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceFloatLiteralsWithNegativeExponents()
        => VerifyAsync("""
            [|
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

                    Const f_1 As Single = 0.000001234567F
                    Const f_2 As Single = 0.0000001234567F
                    Const f_3 As Single = 0.00000001234567F
                    Const f_4 As Single = 0.000000001234567F ' Change at -9
                    Const f_5 As Single = 0.0000000001234567F

                    Const f_6 As Single = 0.00000000123456778F
                    Const f_7 As Single = 0.000000000123456786F
                    Const f_8 As Single = 0.000000000012345678F ' Change at -11
                    Const f_9 As Single = 0.0000000000012345678F

                    Const d_1 As Single = 0.00000000000000123456789012345
                    Const d_2 As Single = 0.000000000000000123456789012345
                    Const d_3 As Single = 0.0000000000000000123456789012345 ' Change at -17
                    Const d_4 As Single = 0.00000000000000000123456789012345

                    Const d_5 As Double = 0.00000000000000001234567890123456
                    Const d_6 As Double = 0.000000000000000001234567890123456
                    Const d_7 As Double = 0.0000000000000000001234567890123456   ' Change at -19
                    Const d_8 As Double = 0.00000000000000000001234567890123456
                End Sub
            End Module
            |]
            """, $"""

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

                    Const f_1 As Single = 0.000001234567F
                    Const f_2 As Single = 0.0000001234567F
                    Const f_3 As Single = 0.00000001234567F
                    Const f_4 As Single = 1.234567E-9F ' Change at -9
                    Const f_5 As Single = 1.234567E-10F

                    Const f_6 As Single = {(IsNetCoreApp ? "0.0000000012345678F" : "0.00000000123456778F")}
                    Const f_7 As Single = {(IsNetCoreApp ? "0.00000000012345679F" : "0.000000000123456786F")}
                    Const f_8 As Single = {(IsNetCoreApp ? "1.2345678E-11F" : "1.23456783E-11F")} ' Change at -11
                    Const f_9 As Single = {(IsNetCoreApp ? "1.2345678E-12F" : "1.23456779E-12F")}

                    Const d_1 As Single = 0.00000000000000123456789012345
                    Const d_2 As Single = 0.000000000000000123456789012345
                    Const d_3 As Single = 1.23456789012345E-17 ' Change at -17
                    Const d_4 As Single = 1.23456789012345E-18

                    Const d_5 As Double = {(IsNetCoreApp ? "0.00000000000000001234567890123456" : "0.000000000000000012345678901234561")}
                    Const d_6 As Double = 0.000000000000000001234567890123456
                    Const d_7 As Double = {(IsNetCoreApp ? "1.234567890123456E-19" : "1.2345678901234561E-19")}   ' Change at -19
                    Const d_8 As Double = 1.234567890123456E-20
                End Sub
            End Module

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceSingleLiteralsWithTrailingZeros()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    Const f1 As Single = 3.011000F                      ' Dev11 & Roslyn: 3.011F
                    Const f2 As Single = 3.000000!                      ' Dev11 & Roslyn: 3.0!
                    Const f3 As Single = 3.0F                           ' Dev11 & Roslyn: Unchanged
                    Const f4 As Single = 3000f                          ' Dev11 & Roslyn: 3000.0F
                    Const f5 As Single = 3000E+10!                      ' Dev11 & Roslyn: 3.0E+13!
                    Const f6 As Single = 3000.0E+10F                    ' Dev11 & Roslyn: 3.0E+13F
                    Const f7 As Single = 3000.010E+1F                   ' Dev11 & Roslyn: 30000.1F
                    Const f8 As Single = 3000.123456789010E+10!         ' Dev11 & Roslyn: 3.00012337E+13!
                    Const f9 As Single = 3000.123456789000E+10F         ' Dev11 & Roslyn: 3.00012337E+13F
                    Const f10 As Single = 30001234567890.10E-10f        ' Dev11 & Roslyn: 3000.12354F
                    Const f11 As Single = 3000E-10!                     ' Dev11 & Roslyn: 0.0000003!

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
            |]
            """, $"""

            Module Program
                Sub Main(args As String())
                    Const f1 As Single = 3.011F                      ' Dev11 & Roslyn: 3.011F
                    Const f2 As Single = 3.0!                      ' Dev11 & Roslyn: 3.0!
                    Const f3 As Single = 3.0F                           ' Dev11 & Roslyn: Unchanged
                    Const f4 As Single = 3000.0F                          ' Dev11 & Roslyn: 3000.0F
                    Const f5 As Single = 3.0E+13!                      ' Dev11 & Roslyn: 3.0E+13!
                    Const f6 As Single = 3.0E+13F                    ' Dev11 & Roslyn: 3.0E+13F
                    Const f7 As Single = 30000.1F                   ' Dev11 & Roslyn: 30000.1F
                    Const f8 As Single = {(IsNetCoreApp ? "3.0001234E+13!" : "3.00012337E+13!")}         ' Dev11 & Roslyn: 3.00012337E+13!
                    Const f9 As Single = {(IsNetCoreApp ? "3.0001234E+13F" : "3.00012337E+13F")}         ' Dev11 & Roslyn: 3.00012337E+13F
                    Const f10 As Single = {(IsNetCoreApp ? "3000.1235F" : "3000.12354F")}        ' Dev11 & Roslyn: 3000.12354F
                    Const f11 As Single = 0.0000003!                     ' Dev11 & Roslyn: 0.0000003!

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDoubleLiteralsWithTrailingZeros()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    Const d1 As Double = 3.011000                       ' Dev11 & Roslyn: 3.011
                    Const d2 As Double = 3.000000                       ' Dev11 & Roslyn: 3.0
                    Const d3 As Double = 3.0                            ' Dev11 & Roslyn: Unchanged
                    Const d4 As Double = 3000R                          ' Dev11 & Roslyn: 3000.0R
                    Const d5 As Double = 3000E+10#                      ' Dev11 & Roslyn: 30000000000000.0#
                    Const d6 As Double = 3000.0E+10                     ' Dev11 & Roslyn: 30000000000000.0
                    Const d7 As Double = 3000.010E+1                    ' Dev11 & Roslyn: 30000.1
                    Const d8 As Double = 3000.123456789010E+10#         ' Dev11 & Roslyn: 30001234567890.1#
                    Const d9 As Double = 3000.123456789000E+10          ' Dev11 & Roslyn: 30001234567890.0
                    Const d10 As Double = 30001234567890.10E-10d        ' Dev11 & Roslyn: 3000.12345678901D
                    Const d11 As Double = 3000E-10                      ' Dev11 & Roslyn: 0.0000003

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    Const d1 As Double = 3.011                       ' Dev11 & Roslyn: 3.011
                    Const d2 As Double = 3.0                       ' Dev11 & Roslyn: 3.0
                    Const d3 As Double = 3.0                            ' Dev11 & Roslyn: Unchanged
                    Const d4 As Double = 3000.0R                          ' Dev11 & Roslyn: 3000.0R
                    Const d5 As Double = 30000000000000.0#                      ' Dev11 & Roslyn: 30000000000000.0#
                    Const d6 As Double = 30000000000000.0                     ' Dev11 & Roslyn: 30000000000000.0
                    Const d7 As Double = 30000.1                    ' Dev11 & Roslyn: 30000.1
                    Const d8 As Double = 30001234567890.1#         ' Dev11 & Roslyn: 30001234567890.1#
                    Const d9 As Double = 30001234567890.0          ' Dev11 & Roslyn: 30001234567890.0
                    Const d10 As Double = 3000.12345678901D        ' Dev11 & Roslyn: 3000.12345678901D
                    Const d11 As Double = 0.0000003                      ' Dev11 & Roslyn: 0.0000003

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

            """);

    [Fact, WorkItem(5529, "DevDiv_Projects/Roslyn")]
    public Task ReduceDecimalLiteralsWithTrailingZeros()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main(args As String())
                    Const d1 As Decimal = 3.011000D                     ' Dev11 & Roslyn: 3.011D
                    Const d2 As Decimal = 3.000000D                     ' Dev11 & Roslyn: 3D
                    Const d3 As Decimal = 3.0D                          ' Dev11 & Roslyn: 3D
                    Const d4 As Decimal = 3000D                         ' Dev11 & Roslyn: 3000D
                    Const d5 As Decimal = 3000E+10D                     ' Dev11 & Roslyn: 30000000000000D
                    Const d6 As Decimal = 3000.0E+10D                   ' Dev11 & Roslyn: 30000000000000D
                    Const d7 As Decimal = 3000.010E+1D                  ' Dev11 & Roslyn: 30000.1D
                    Const d8 As Decimal = 3000.123456789010E+10D        ' Dev11 & Roslyn: 30001234567890.1D
                    Const d9 As Decimal = 3000.123456789000E+10D        ' Dev11 & Roslyn: 30001234567890D
                    Const d10 As Decimal = 30001234567890.10E-10D        ' Dev11 & Roslyn: 3000.12345678901D
                    Const d11 As Decimal = 3000E-10D                    ' Dev11 & Roslyn: 0.0000003D

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
            |]
            """, """

            Module Program
                Sub Main(args As String())
                    Const d1 As Decimal = 3.011D                     ' Dev11 & Roslyn: 3.011D
                    Const d2 As Decimal = 3D                     ' Dev11 & Roslyn: 3D
                    Const d3 As Decimal = 3D                          ' Dev11 & Roslyn: 3D
                    Const d4 As Decimal = 3000D                         ' Dev11 & Roslyn: 3000D
                    Const d5 As Decimal = 30000000000000D                     ' Dev11 & Roslyn: 30000000000000D
                    Const d6 As Decimal = 30000000000000D                   ' Dev11 & Roslyn: 30000000000000D
                    Const d7 As Decimal = 30000.1D                  ' Dev11 & Roslyn: 30000.1D
                    Const d8 As Decimal = 30001234567890.1D        ' Dev11 & Roslyn: 30001234567890.1D
                    Const d9 As Decimal = 30001234567890D        ' Dev11 & Roslyn: 30001234567890D
                    Const d10 As Decimal = 3000.12345678901D        ' Dev11 & Roslyn: 3000.12345678901D
                    Const d11 As Decimal = 0.0000003D                    ' Dev11 & Roslyn: 0.0000003D

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

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623319")]
    public async Task ReduceFloatingAndDecimalLiteralsWithDifferentCulture()
    {
        var savedCulture = System.Threading.Thread.CurrentThread.CurrentCulture;

        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                System.Globalization.CultureInfo.CreateSpecificCulture("de-DE");
            await VerifyAsync("""
                [|
                Module Program
                    Sub Main(args As String())
                        Dim d = 1.0D
                        Dim f = 1.0F
                        Dim x = 1.0
                    End Sub
                End Module|]
                """, """

                Module Program
                    Sub Main(args As String())
                        Dim d = 1D
                        Dim f = 1.0F
                        Dim x = 1.0
                    End Sub
                End Module
                """);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = savedCulture;
        }
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652147")]
    public async Task ReduceFloatingAndDecimalLiteralsWithInvariantCultureNegatives()
    {
        var oldCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = (CultureInfo)oldCulture.Clone();
            Thread.CurrentThread.CurrentCulture.NumberFormat.NegativeSign = "~";
            await VerifyAsync("""
                [|
                Module Program
                    Sub Main(args As String())
                        Dim d = -1.0E-11D
                        Dim f = -1.0E-11F
                        Dim x = -1.0E-11
                    End Sub
                End Module|]
                """, """

                Module Program
                    Sub Main(args As String())
                        Dim d = -0.00000000001D
                        Dim f = -1.0E-11F
                        Dim x = -0.00000000001
                    End Sub
                End Module
                """);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = oldCulture;
        }
    }

    [Fact]
    public Task ReduceIntegerLiteralWithLeadingZeros()
        => VerifyAsync("""
            [|
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
            |]
            """, """

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

            """);

    [Fact]
    public Task ReduceIntegerLiteralWithNegativeHexOrOctalValue()
        => VerifyAsync("""
            [|
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
            |]
            """, """

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

            """);

    [Fact]
    public Task ReduceIntegerLiteralWithOverflow()
        => VerifyAsync("""
            [|
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
            |]
            """, """

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

            """);

    [Fact]
    public Task ReduceBinaryIntegerLiteral()
        => VerifyAsync("""
            [|
            Module Module1
                Sub Main()
                    ' signed
                    Dim a As SByte = &B0111
                    Dim b As Short = &B0101
                    Dim c As Integer = &B00100100
                    Dim d As Long = &B001001100110

                    ' unsigned
                    Dim e As Byte = &B01011
                    Dim f As UShort = &B00100
                    Dim g As UInteger = &B001001100110
                    Dim h As ULong = &B001001100110

                    ' negative
                    Dim i As SByte = -&B0111
                    Dim j As Short = -&B00101
                    Dim k As Integer = -&B00100100
                    Dim l As Long = -&B001001100110

                    ' negative literal
                    Dim m As SByte = &B10000001
                    Dim n As Short = &B1000000000000001
                    Dim o As Integer = &B10000000000000000000000000000001
                    Dim p As Long = &B1000000000000000000000000000000000000000000000000000000000000001
                End Sub
            End Module
            |]
            """, """

            Module Module1
                Sub Main()
                    ' signed
                    Dim a As SByte = &B111
                    Dim b As Short = &B101
                    Dim c As Integer = &B100100
                    Dim d As Long = &B1001100110

                    ' unsigned
                    Dim e As Byte = &B1011
                    Dim f As UShort = &B100
                    Dim g As UInteger = &B1001100110
                    Dim h As ULong = &B1001100110

                    ' negative
                    Dim i As SByte = -&B111
                    Dim j As Short = -&B101
                    Dim k As Integer = -&B100100
                    Dim l As Long = -&B1001100110

                    ' negative literal
                    Dim m As SByte = &B10000001
                    Dim n As Short = &B1000000000000001
                    Dim o As Integer = &B10000000000000000000000000000001
                    Dim p As Long = &B1000000000000000000000000000000000000000000000000000000000000001
                End Sub
            End Module

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14034")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/48492")]
    public async Task DoNotReduceDigitSeparators()
    {
        var source = """

            Module Module1
                Sub Main()
                    Dim x = 100_000
                    Dim y = 100_000.0F
                    Dim z = 100_000.0D
                End Sub
            End Module

            """;
        var expected = source;
        await VerifyAsync($"[|{source}|]", expected);
    }

    private static async Task VerifyAsync(string codeWithMarker, string expectedResult)
    {
        MarkupTestFile.GetSpans(codeWithMarker, out var codeWithoutMarker, out var textSpans);

        var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic);
        var codeCleanups = CodeCleaner.GetDefaultProviders(document).WhereAsArray(p => p.Name is PredefinedCodeCleanupProviderNames.ReduceTokens or PredefinedCodeCleanupProviderNames.CaseCorrection or PredefinedCodeCleanupProviderNames.Format);

        var cleanDocument = await CodeCleaner.CleanupAsync(document, textSpans[0], await document.GetCodeCleanupOptionsAsync(CancellationToken.None), codeCleanups);

        AssertEx.EqualOrDiff(expectedResult, (await cleanDocument.GetSyntaxRootAsync()).ToFullString());
    }

    private static Document CreateDocument(string code, string language)
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

        return project.AddMetadataReference(NetFramework.mscorlib)
                      .AddDocument("Document", SourceText.From(code));
    }
}
