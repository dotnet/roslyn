// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DictionaryExpressionTests : CSharpTestBase
    {
        private const string s_dictionaryExtensions = """
            using System;
            using System.Collections.Generic;
            using System.Text;
            static class DictionaryExtensions
            {
                private static void Append(StringBuilder builder, object value)
                {
                    builder.Append(value is null ? "null" : value.ToString());
                }
                internal static void Report<K, V>(this IEnumerable<KeyValuePair<K, V>> e)
                {
                    var builder = new StringBuilder();
                    builder.Append("[");
                    bool isFirst = true;
                    foreach (var kvp in e)
                    {
                        if (!isFirst) builder.Append(", ");
                        isFirst = false;
                        Append(builder, kvp.Key);
                        builder.Append(":");
                        Append(builder, kvp.Value);
                    }
                    builder.Append("]");
                    Console.Write(builder.ToString());
                }
            }
            """;

        // PROTOTYPE: Test language version.
        [Fact]
        public void Dictionary()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>(3, "three");
                        F(x, new[] { y }).Report();
                    }
                    static Dictionary<long, object> F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
                    {
                        return [1:"one", x, ..y];
                    }
                }
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions]);
            comp.VerifyEmitDiagnostics(
                // (12,16): error CS9215: Collection expression type 'Dictionary<long, object>' must have an instance or extension method 'Add' that can be called with a single argument.
                //         return [1:"one", x, ..y];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, @"[1:""one"", x, ..y]").WithArguments("System.Collections.Generic.Dictionary<long, object>").WithLocation(12, 16),
                // (12,17): error CS9268: Collection expression type 'Dictionary<long, object>' does not support key-value pair elements.
                //         return [1:"one", x, ..y];
                Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, @"1:""one""").WithArguments("System.Collections.Generic.Dictionary<long, object>").WithLocation(12, 17),
                // (12,26): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<long, object>'
                //         return [1:"one", x, ..y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<long, object>").WithLocation(12, 26),
                // (12,31): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<long, object>'
                //         return [1:"one", x, ..y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<long, object>").WithLocation(12, 31));
        }

        // PROTOTYPE: Test language version.
        // PROTOTYPE: Test IReadOnlyDictionary<,>.
        [Fact]
        public void DictionaryInterface()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>(3, "three");
                        F(x, new[] { y }).Report();
                    }
                    static IDictionary<long, object> F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
                    {
                        return [1:"one", x, ..y];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[1:one, 2:two, 3:three]");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F", """
                {
                  // Code size      101 (0x65)
                  .maxstack  3
                  .locals init (System.Collections.Generic.Dictionary<long, object> V_0,
                                System.Collections.Generic.KeyValuePair<int, string> V_1,
                                System.Collections.Generic.KeyValuePair<int, string> V_2,
                                System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int, string>> V_3)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<long, object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.1
                  IL_0008:  conv.i8
                  IL_0009:  ldstr      "one"
                  IL_000e:  callvirt   "void System.Collections.Generic.Dictionary<long, object>.Add(long, object)"
                  IL_0013:  ldarg.0
                  IL_0014:  stloc.1
                  IL_0015:  ldloc.0
                  IL_0016:  ldloca.s   V_1
                  IL_0018:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_001d:  conv.i8
                  IL_001e:  ldloca.s   V_1
                  IL_0020:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_0025:  callvirt   "void System.Collections.Generic.Dictionary<long, object>.Add(long, object)"
                  IL_002a:  ldarg.1
                  IL_002b:  callvirt   "System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int, string>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>.GetEnumerator()"
                  IL_0030:  stloc.3
                  .try
                  {
                    IL_0031:  br.s       IL_004f
                    IL_0033:  ldloc.3
                    IL_0034:  callvirt   "System.Collections.Generic.KeyValuePair<int, string> System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int, string>>.Current.get"
                    IL_0039:  stloc.2
                    IL_003a:  ldloc.0
                    IL_003b:  ldloca.s   V_2
                    IL_003d:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                    IL_0042:  conv.i8
                    IL_0043:  ldloca.s   V_2
                    IL_0045:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                    IL_004a:  callvirt   "void System.Collections.Generic.Dictionary<long, object>.Add(long, object)"
                    IL_004f:  ldloc.3
                    IL_0050:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                    IL_0055:  brtrue.s   IL_0033
                    IL_0057:  leave.s    IL_0063
                  }
                  finally
                  {
                    IL_0059:  ldloc.3
                    IL_005a:  brfalse.s  IL_0062
                    IL_005c:  ldloc.3
                    IL_005d:  callvirt   "void System.IDisposable.Dispose()"
                    IL_0062:  endfinally
                  }
                  IL_0063:  ldloc.0
                  IL_0064:  ret
                }
                """);
        }
    }
}
