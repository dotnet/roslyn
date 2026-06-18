// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB;

public sealed class LabeledBreakContinuePdbTests : CSharpTestBase
{
    [Fact]
    public void SequencePoints_LabeledBreak()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: while (b)
                    {
                        while (b)
                        {
                            if (b)
                                break outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.DebugDll);
        comp.VerifyPdb("C.M", """
            <symbols>
              <files>
                <file id="1" name="" language="C#" />
              </files>
              <methods>
                <method containingType="C" name="M" parameterNames="b">
                  <customDebugInfo>
                    <encLocalSlotMap>
                      <slot kind="1" offset="89" />
                      <slot kind="1" offset="49" />
                      <slot kind="1" offset="17" />
                    </encLocalSlotMap>
                  </customDebugInfo>
                  <sequencePoints>
                    <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="6" document="1" />
                    <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="15" document="1" />
                    <entry offset="0x2" hidden="true" document="1" />
                    <entry offset="0x4" startLine="6" startColumn="9" endLine="6" endColumn="10" document="1" />
                    <entry offset="0x5" hidden="true" document="1" />
                    <entry offset="0x7" startLine="8" startColumn="13" endLine="8" endColumn="14" document="1" />
                    <entry offset="0x8" startLine="9" startColumn="17" endLine="9" endColumn="23" document="1" />
                    <entry offset="0xa" hidden="true" document="1" />
                    <entry offset="0xd" startLine="10" startColumn="21" endLine="10" endColumn="33" document="1" />
                    <entry offset="0xf" startLine="11" startColumn="13" endLine="11" endColumn="14" document="1" />
                    <entry offset="0x10" startLine="7" startColumn="13" endLine="7" endColumn="22" document="1" />
                    <entry offset="0x12" hidden="true" document="1" />
                    <entry offset="0x15" startLine="12" startColumn="9" endLine="12" endColumn="10" document="1" />
                    <entry offset="0x16" startLine="5" startColumn="16" endLine="5" endColumn="25" document="1" />
                    <entry offset="0x18" hidden="true" document="1" />
                    <entry offset="0x1b" startLine="13" startColumn="5" endLine="13" endColumn="6" document="1" />
                  </sequencePoints>
                </method>
              </methods>
            </symbols>
            """);
    }

    [Fact]
    public void SequencePoints_LabeledContinue()
    {
        var source = """
            class C
            {
                static void M(bool b)
                {
                    outer: while (b)
                    {
                        while (b)
                        {
                            if (b)
                                continue outer;
                        }
                    }
                }
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.DebugDll);
        comp.VerifyPdb("C.M", """
            <symbols>
              <files>
                <file id="1" name="" language="C#" />
              </files>
              <methods>
                <method containingType="C" name="M" parameterNames="b">
                  <customDebugInfo>
                    <encLocalSlotMap>
                      <slot kind="1" offset="89" />
                      <slot kind="1" offset="49" />
                      <slot kind="1" offset="17" />
                    </encLocalSlotMap>
                  </customDebugInfo>
                  <sequencePoints>
                    <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="6" document="1" />
                    <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="15" document="1" />
                    <entry offset="0x2" hidden="true" document="1" />
                    <entry offset="0x4" startLine="6" startColumn="9" endLine="6" endColumn="10" document="1" />
                    <entry offset="0x5" hidden="true" document="1" />
                    <entry offset="0x7" startLine="8" startColumn="13" endLine="8" endColumn="14" document="1" />
                    <entry offset="0x8" startLine="9" startColumn="17" endLine="9" endColumn="23" document="1" />
                    <entry offset="0xa" hidden="true" document="1" />
                    <entry offset="0xd" startLine="10" startColumn="21" endLine="10" endColumn="36" document="1" />
                    <entry offset="0xf" startLine="11" startColumn="13" endLine="11" endColumn="14" document="1" />
                    <entry offset="0x10" startLine="7" startColumn="13" endLine="7" endColumn="22" document="1" />
                    <entry offset="0x12" hidden="true" document="1" />
                    <entry offset="0x15" startLine="12" startColumn="9" endLine="12" endColumn="10" document="1" />
                    <entry offset="0x16" startLine="5" startColumn="16" endLine="5" endColumn="25" document="1" />
                    <entry offset="0x18" hidden="true" document="1" />
                    <entry offset="0x1b" startLine="13" startColumn="5" endLine="13" endColumn="6" document="1" />
                  </sequencePoints>
                </method>
              </methods>
            </symbols>
            """);
    }
}
