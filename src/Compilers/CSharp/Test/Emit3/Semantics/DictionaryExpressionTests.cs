// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
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

        [Theory]
        [InlineData(LanguageVersion.CSharp13)]
        [InlineData(LanguageVersion.Preview)]
        public void LanguageVersionDiagnostics_01(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d;
                d = [];
                d = [1:"one"];
                var x = new KeyValuePair<int, string>(2, "two");
                var y = new KeyValuePair<int, string>[] { new(3, "three") };
                d = [x];
                d = [..y];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                // PROTOTYPE: Should report language version errors for all d = ... assignments.
                comp.VerifyEmitDiagnostics(
                    // (4,7): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(4, 7));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

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
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        F(x, y).Report();
                    }
                    static Dictionary<int, string> F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
                    {
                        return [1:"one", x, ..y];
                    }
                }
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions]);
            comp.VerifyEmitDiagnostics(
                // (12,16): error CS9215: Collection expression type 'Dictionary<int, string>' must have an instance or extension method 'Add' that can be called with a single argument.
                //         return [1:"one", x, ..y];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, @"[1:""one"", x, ..y]").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(12, 16),
                // (12,17): error CS9268: Collection expression type 'Dictionary<int, string>' does not support key-value pair elements.
                //         return [1:"one", x, ..y];
                Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, @"1:""one""").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(12, 17));
        }

        [Theory]
        [InlineData("IDictionary<int, string>")]
        [InlineData("IReadOnlyDictionary<int, string>")]
        public void DictionaryInterface(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        F(x, y).Report();
                    }
                    static {{typeName}} F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
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
                  // Code size       98 (0x62)
                  .maxstack  3
                  .locals init (System.Collections.Generic.Dictionary<int, string> V_0,
                                System.Collections.Generic.KeyValuePair<int, string> V_1,
                                System.Collections.Generic.KeyValuePair<int, string> V_2,
                                System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int, string>> V_3)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.1
                  IL_0008:  ldstr      "one"
                  IL_000d:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.Add(int, string)"
                  IL_0012:  ldarg.0
                  IL_0013:  stloc.1
                  IL_0014:  ldloc.0
                  IL_0015:  ldloca.s   V_1
                  IL_0017:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_001c:  ldloca.s   V_1
                  IL_001e:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_0023:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.Add(int, string)"
                  IL_0028:  ldarg.1
                  IL_0029:  callvirt   "System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int, string>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>.GetEnumerator()"
                  IL_002e:  stloc.3
                  .try
                  {
                    IL_002f:  br.s       IL_004c
                    IL_0031:  ldloc.3
                    IL_0032:  callvirt   "System.Collections.Generic.KeyValuePair<int, string> System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int, string>>.Current.get"
                    IL_0037:  stloc.2
                    IL_0038:  ldloc.0
                    IL_0039:  ldloca.s   V_2
                    IL_003b:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                    IL_0040:  ldloca.s   V_2
                    IL_0042:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                    IL_0047:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.Add(int, string)"
                    IL_004c:  ldloc.3
                    IL_004d:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                    IL_0052:  brtrue.s   IL_0031
                    IL_0054:  leave.s    IL_0060
                  }
                  finally
                  {
                    IL_0056:  ldloc.3
                    IL_0057:  brfalse.s  IL_005f
                    IL_0059:  ldloc.3
                    IL_005a:  callvirt   "void System.IDisposable.Dispose()"
                    IL_005f:  endfinally
                  }
                  IL_0060:  ldloc.0
                  IL_0061:  ret
                }
                """);
        }

        [Fact]
        public void KeyValueConversions()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        F(x, y);
                    }
                    static IDictionary<long, object> F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,17): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<long, object>'
                //         return [x, ..y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<long, object>").WithLocation(12, 17),
                // (12,22): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<long, object>'
                //         return [x, ..y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<long, object>").WithLocation(12, 22));
        }

        // PROTOTYPE: Test order of evaluation and side-effects for k:v pairs.

        [Fact]
        public void InferredType_ExpressionElement()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d = [default, new()];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void InferredType_KeyValueElement()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d = [default:default, null:new()];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ConversionError_ExpressionElement()
        {
            string source = """
                using System.Collections.Generic;
                var x = new object();
                IDictionary<int, int> d = [x, null];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,28): error CS0029: Cannot implicitly convert type 'object' to 'System.Collections.Generic.KeyValuePair<int, int>'
                // IDictionary<int, int> d = [x, null];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("object", "System.Collections.Generic.KeyValuePair<int, int>").WithLocation(3, 28),
                // (3,31): error CS0037: Cannot convert null to 'KeyValuePair<int, int>' because it is a non-nullable value type
                // IDictionary<int, int> d = [x, null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Collections.Generic.KeyValuePair<int, int>").WithLocation(3, 31));
        }

        [Fact]
        public void ConversionError_KeyValueElement_01()
        {
            string source = """
                using System.Collections.Generic;
                var x = new object();
                var y = new object();
                IDictionary<string, int> d = [x:1, "2":y];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,31): error CS0029: Cannot implicitly convert type 'object' to 'string'
                // IDictionary<string, int> d = [x:1, "2":y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("object", "string").WithLocation(4, 31),
                // (4,40): error CS0029: Cannot implicitly convert type 'object' to 'int'
                // IDictionary<string, int> d = [x:1, "2":y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("object", "int").WithLocation(4, 40));
        }

        [Fact]
        public void ConversionError_KeyValueElement_02()
        {
            // PROTOTYPE: Why aren't we reporting an error for new() since string doesn't have an
            // empty .ctor? Compare with ConversionError_PROTOTYPE_01 below.
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d = [new():null];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,37): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                // IDictionary<string, int> d = [new():null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(2, 37));
        }

        [Fact]
        public void ConversionError_PROTOTYPE_01()
        {
            string source = """
                using System.Collections.Generic;
                IList<string> z = [new()];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // IList<string> z = [new()];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("string", "0").WithLocation(2, 20));
        }

        [Fact]
        public void ConversionError_SpreadElement()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d = [..new()];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,33): error CS8754: There is no target type for 'new()'
                // IDictionary<string, int> d = [..new()];
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(2, 33));
        }

        [Fact]
        public void Lock()
        {
            string source = """
                using System.Collections.Generic;
                using System.Threading;
                class Program
                {
                    static void Main()
                    {
                        var x = new Lock();
                        var y = new Lock();
                        object[] a = [x];
                        IDictionary<object, object> d = [x:1, 2:y];
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (9,23): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         object[] a = [x];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "x").WithLocation(9, 23),
                // (10,42): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         IDictionary<object, object> d = [x:1, 2:y];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "x").WithLocation(10, 42),
                // (10,49): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         IDictionary<object, object> d = [x:1, 2:y];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "y").WithLocation(10, 49));
        }
    }
}
