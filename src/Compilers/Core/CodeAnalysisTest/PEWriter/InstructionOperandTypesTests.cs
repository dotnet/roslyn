// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Cci;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.PEWriter
{
    public class InstructionOperandTypesTests
    {
        [Fact]
        public void OperandTypes()
        {
            var OneByteOperandTypes = new byte[0xff];
            var TwoByteOperandTypes = new byte[0x1f];

            var typeOfOpCode = typeof(OpCode);
            var reserved = new[] { "Prefix1", "Prefix2", "Prefix3", "Prefix4", "Prefix5", "Prefix6", "Prefix7", "Prefixref" };

            foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => !reserved.Contains(f.Name)))
            {
                if (fi.FieldType != typeOfOpCode)
                {
                    continue;
                }

                OpCode opCode = (OpCode)fi.GetValue(null);
                var value = unchecked((ushort)opCode.Value);
                if (value < 0x100)
                {
                    OneByteOperandTypes[value] = (byte)opCode.OperandType;
                }
                else if ((value & 0xff00) == 0xfe00)
                {
                    TwoByteOperandTypes[value & 0xff] = (byte)opCode.OperandType;
                }
            }

            AssertEx.Equal(OneByteOperandTypes, InstructionOperandTypes.OneByte);
            AssertEx.Equal(TwoByteOperandTypes, InstructionOperandTypes.TwoByte);
        }
    }
}
