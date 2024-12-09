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
                IDictionary<int, string> d = [1:"one"];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (2,30): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // IDictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, @"[1:""one""]").WithArguments("dictionary expressions").WithLocation(2, 30),
                    // (2,32): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // IDictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(2, 32));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp13)]
        [InlineData(LanguageVersion.Preview)]
        public void LanguageVersionDiagnostics_02(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d;
                d = [];
                var x = new KeyValuePair<int, string>(2, "two");
                var y = new KeyValuePair<int, string>[] { new(3, "three") };
                d = [x];
                d = [..y];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,5): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // d = [];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[]").WithArguments("dictionary expressions").WithLocation(3, 5),
                    // (6,5): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // d = [x];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[x]").WithArguments("dictionary expressions").WithLocation(6, 5),
                    // (7,5): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // d = [..y];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[..y]").WithArguments("dictionary expressions").WithLocation(7, 5));
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

        [Theory]
        [InlineData("IDictionary<int, string>")]
        [InlineData("IReadOnlyDictionary<int, string>")]
        public void DictionaryInterface_MissingMember(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                {{typeName}} d;
                d = [];
                d = [1:"one"];
                d = [new KeyValuePair<int, string>(2, "two")];
                d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                """;

            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_List_T);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_Dictionary_KV);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(3, 5),
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(4, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(5, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(6, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(6, 5));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(6, 5));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_Dictionary_KV__Add);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.Add'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", "Add").WithLocation(6, 5));
        }

        [Theory]
        [InlineData("IDictionary<int, string>")]
        [InlineData("IReadOnlyDictionary<int, string>")]
        public void KeyValuePair_MissingMember(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                {{typeName}} d;
                d = [];
                d = [1:"one"];
                d = [new KeyValuePair<int, string>(2, "two")];
                d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Key);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(6, 5));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(6, 5));
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

        [Fact]
        public void EvaluationOrder_01()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IReadOnlyDictionary<int, string> d = [
                            Identity(new KeyValuePair<int, string>(1, "one")),
                            Identity(2):Identity("two"),
                            ..Identity((KeyValuePair<int, string>[])[new(3, "three")])];
                        d.Report();
                    }
                    static T Identity<T>(T value)
                    {
                        Console.WriteLine(value);
                        return value;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: """
                    [1, one]
                    2
                    two
                    System.Collections.Generic.KeyValuePair`2[System.Int32,System.String][]
                    [1:one, 2:two, 3:three]
                    """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size      149 (0x95)
                  .maxstack  5
                  .locals init (System.Collections.Generic.Dictionary<int, string> V_0,
                                System.Collections.Generic.KeyValuePair<int, string> V_1,
                                System.Collections.Generic.KeyValuePair<int, string> V_2,
                                System.Collections.Generic.KeyValuePair<int, string>[] V_3,
                                int V_4)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldc.i4.1
                  IL_0007:  ldstr      "one"
                  IL_000c:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                  IL_0011:  call       "System.Collections.Generic.KeyValuePair<int, string> Program.Identity<System.Collections.Generic.KeyValuePair<int, string>>(System.Collections.Generic.KeyValuePair<int, string>)"
                  IL_0016:  stloc.1
                  IL_0017:  ldloc.0
                  IL_0018:  ldloca.s   V_1
                  IL_001a:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_001f:  ldloca.s   V_1
                  IL_0021:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_0026:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.Add(int, string)"
                  IL_002b:  ldloc.0
                  IL_002c:  ldc.i4.2
                  IL_002d:  call       "int Program.Identity<int>(int)"
                  IL_0032:  ldstr      "two"
                  IL_0037:  call       "string Program.Identity<string>(string)"
                  IL_003c:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.Add(int, string)"
                  IL_0041:  ldc.i4.1
                  IL_0042:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0047:  dup
                  IL_0048:  ldc.i4.0
                  IL_0049:  ldc.i4.3
                  IL_004a:  ldstr      "three"
                  IL_004f:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                  IL_0054:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0059:  call       "System.Collections.Generic.KeyValuePair<int, string>[] Program.Identity<System.Collections.Generic.KeyValuePair<int, string>[]>(System.Collections.Generic.KeyValuePair<int, string>[])"
                  IL_005e:  stloc.3
                  IL_005f:  ldc.i4.0
                  IL_0060:  stloc.s    V_4
                  IL_0062:  br.s       IL_0087
                  IL_0064:  ldloc.3
                  IL_0065:  ldloc.s    V_4
                  IL_0067:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_006c:  stloc.2
                  IL_006d:  ldloc.0
                  IL_006e:  ldloca.s   V_2
                  IL_0070:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_0075:  ldloca.s   V_2
                  IL_0077:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_007c:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.Add(int, string)"
                  IL_0081:  ldloc.s    V_4
                  IL_0083:  ldc.i4.1
                  IL_0084:  add
                  IL_0085:  stloc.s    V_4
                  IL_0087:  ldloc.s    V_4
                  IL_0089:  ldloc.3
                  IL_008a:  ldlen
                  IL_008b:  conv.i4
                  IL_008c:  blt.s      IL_0064
                  IL_008e:  ldloc.0
                  IL_008f:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_0094:  ret
                }
                """);
        }

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
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d;
                d = [new():1];
                d = ["":null];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,6): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // d = [new():1];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("string", "0").WithLocation(3, 6),
                // (4,9): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                // d = ["":null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(4, 9));
        }

        [Fact]
        public void ConversionError_KeyValueElement_03()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IDictionary<object, object> d = [P:P];
                    }
                    static object P { set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,42): error CS0154: The property or indexer 'Program.P' cannot be used in this context because it lacks the get accessor
                //         IDictionary<object, object> d = [P:P];
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("Program.P").WithLocation(6, 42),
                // (6,44): error CS0154: The property or indexer 'Program.P' cannot be used in this context because it lacks the get accessor
                //         IDictionary<object, object> d = [P:P];
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("Program.P").WithLocation(6, 44));
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
