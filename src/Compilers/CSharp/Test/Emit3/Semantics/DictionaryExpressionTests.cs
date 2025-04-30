// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DictionaryExpressionTests : CSharpTestBase
    {
        private static string IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

        private const string s_collectionExtensions = CollectionExpressionTests.s_collectionExtensions;
        private const string s_dictionaryExtensions = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            static class DictionaryExtensions
            {
                private static void Append(StringBuilder builder, object value)
                {
                    builder.Append(value is null ? "null" : value.ToString());
                }
                internal static void Report<K, V>(this IEnumerable<KeyValuePair<K, V>> e)
                {
                    e = e.OrderBy(kvp => kvp.Key);
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
                    builder.Append("], ");
                    Console.Write(builder.ToString());
                }
            }
            """;

        public static readonly TheoryData<LanguageVersion> LanguageVersions = new([LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext]);

        [Theory]
        [MemberData(nameof(LanguageVersions))]
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
                    // (2,30): error CS9174: Cannot initialize type 'IDictionary<int, string>' with a collection expression because the type is not constructible.
                    // IDictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, @"[1:""one""]").WithArguments("System.Collections.Generic.IDictionary<int, string>").WithLocation(2, 30),
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
        [MemberData(nameof(LanguageVersions))]
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
                    // (3,5): error CS9174: Cannot initialize type 'IDictionary<int, string>' with a collection expression because the type is not constructible.
                    // d = [];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[]").WithArguments("System.Collections.Generic.IDictionary<int, string>").WithLocation(3, 5),
                    // (6,5): error CS9174: Cannot initialize type 'IDictionary<int, string>' with a collection expression because the type is not constructible.
                    // d = [x];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[x]").WithArguments("System.Collections.Generic.IDictionary<int, string>").WithLocation(6, 5),
                    // (7,5): error CS9174: Cannot initialize type 'IDictionary<int, string>' with a collection expression because the type is not constructible.
                    // d = [..y];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[..y]").WithArguments("System.Collections.Generic.IDictionary<int, string>").WithLocation(7, 5));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersionDiagnostics_03(LanguageVersion languageVersion)
        {
            string source = """
                var x = [1:"one"];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (1,9): error CS9176: There is no target type for the collection expression.
                    // var x = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, @"[1:""one""]").WithLocation(1, 9),
                    // (1,11): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // var x = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(1, 11));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (1,9): error CS9176: There is no target type for the collection expression.
                    // var x = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, @"[1:""one""]").WithLocation(1, 9));
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersionDiagnostics_04(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, string> d = [];
                        d.Report();
                    }
                }
                """;
            // C#12 collection expressions support target types that implement IEnumerable,
            // with no Add requirement if the collection is empty.
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                expectedOutput: "[], ");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                  IL_0005:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_000a:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void LanguageVersionDiagnostics_05(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool includeExtensionAdd)
        {
            string sourceA = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, string> d = [1:"one"];
                        d.Report();
                    }
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                static class Extensions
                {
                    internal static void Add<K, V>(this Dictionary<K, V> d, KeyValuePair<K, V> kvp)
                    {
                        d.Add(kvp.Key, kvp.Value);
                    }
                }
                """;
            var comp = CreateCompilation(
                includeExtensionAdd ? [sourceA, sourceB, s_dictionaryExtensions] : [sourceA, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && !includeExtensionAdd)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,37): error CS9215: Collection expression type 'Dictionary<int, string>' must have an instance or extension method 'Add' that can be called with a single argument.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(6, 37),
                    // (6,38): error CS9500: Collection expression type 'Dictionary<int, string>' does not support key-value pair elements.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, @"1:""one""").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(6, 38),
                    // (6,39): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(6, 39));
            }
            else if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,38): error CS9500: Collection expression type 'Dictionary<int, string>' does not support key-value pair elements.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, @"1:""one""").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(6, 38),
                    // (6,39): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(6, 39));
            }
            else
            {
                var verifier = CompileAndVerify(comp, expectedOutput: "[1:one], ");
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("Program.Main", """
                    {
                      // Code size       23 (0x17)
                      .maxstack  4
                      IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                      IL_0005:  dup
                      IL_0006:  ldc.i4.1
                      IL_0007:  ldstr      "one"
                      IL_000c:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                      IL_0011:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0016:  ret
                    }
                    """);
            }
        }

        [Theory]
        [CombinatorialData]
        public void BreakingChange_DictionaryAdd_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool includeExtensionAdd)
        {
            string sourceA = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, string> d;
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        d = [x];
                        d.Report();
                        d = [..y];
                        d.Report();
                    }
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                static class Extensions
                {
                    internal static void Add<K, V>(this Dictionary<K, V> d, KeyValuePair<K, V> kvp)
                    {
                        d.Add(kvp.Key, kvp.Value);
                    }
                }
                """;
            var comp = CreateCompilation(
                includeExtensionAdd ? [sourceA, sourceB, s_dictionaryExtensions] : [sourceA, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && !includeExtensionAdd)
            {
                comp.VerifyEmitDiagnostics(
                    // (9,13): error CS9215: Collection expression type 'Dictionary<int, string>' must have an instance or extension method 'Add' that can be called with a single argument.
                    //         d = [x];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[x]").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(9, 13),
                    // (11,13): error CS9215: Collection expression type 'Dictionary<int, string>' must have an instance or extension method 'Add' that can be called with a single argument.
                    //         d = [..y];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[..y]").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(11, 13));
                return;
            }
            var verifier = CompileAndVerify(comp, expectedOutput: "[2:two], [3:three], ");
            verifier.VerifyDiagnostics();
            if (languageVersion == LanguageVersion.CSharp13)
            {
                verifier.VerifyIL("Program.Main", """
                    {
                        // Code size       99 (0x63)
                        .maxstack  5
                        .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0, //x
                                    System.Collections.Generic.Dictionary<int, string> V_1,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<int, string> V_4)
                        IL_0000:  ldloca.s   V_0
                        IL_0002:  ldc.i4.2
                        IL_0003:  ldstr      "two"
                        IL_0008:  call       "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                        IL_000d:  ldc.i4.1
                        IL_000e:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                        IL_0013:  dup
                        IL_0014:  ldc.i4.0
                        IL_0015:  ldc.i4.3
                        IL_0016:  ldstr      "three"
                        IL_001b:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                        IL_0020:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                        IL_0025:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                        IL_002a:  dup
                        IL_002b:  ldloc.0
                        IL_002c:  call       "void Extensions.Add<int, string>(System.Collections.Generic.Dictionary<int, string>, System.Collections.Generic.KeyValuePair<int, string>)"
                        IL_0031:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                        IL_0036:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                        IL_003b:  stloc.1
                        IL_003c:  stloc.2
                        IL_003d:  ldc.i4.0
                        IL_003e:  stloc.3
                        IL_003f:  br.s       IL_0056
                        IL_0041:  ldloc.2
                        IL_0042:  ldloc.3
                        IL_0043:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                        IL_0048:  stloc.s    V_4
                        IL_004a:  ldloc.1
                        IL_004b:  ldloc.s    V_4
                        IL_004d:  call       "void Extensions.Add<int, string>(System.Collections.Generic.Dictionary<int, string>, System.Collections.Generic.KeyValuePair<int, string>)"
                        IL_0052:  ldloc.3
                        IL_0053:  ldc.i4.1
                        IL_0054:  add
                        IL_0055:  stloc.3
                        IL_0056:  ldloc.3
                        IL_0057:  ldloc.2
                        IL_0058:  ldlen
                        IL_0059:  conv.i4
                        IL_005a:  blt.s      IL_0041
                        IL_005c:  ldloc.1
                        IL_005d:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                        IL_0062:  ret
                    }
                    """);
            }
            else
            {
                // Using indexer rather than extension method Add() is a breaking change from C#13.
                verifier.VerifyIL("Program.Main", """
                    {
                      // Code size      130 (0x82)
                      .maxstack  5
                      .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0, //x
                                    System.Collections.Generic.KeyValuePair<int, string> V_1,
                                    System.Collections.Generic.Dictionary<int, string> V_2,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_3,
                                    int V_4)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  ldc.i4.2
                      IL_0003:  ldstr      "two"
                      IL_0008:  call       "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_000d:  ldc.i4.1
                      IL_000e:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0013:  dup
                      IL_0014:  ldc.i4.0
                      IL_0015:  ldc.i4.3
                      IL_0016:  ldstr      "three"
                      IL_001b:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_0020:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0025:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                      IL_002a:  ldloc.0
                      IL_002b:  stloc.1
                      IL_002c:  dup
                      IL_002d:  ldloca.s   V_1
                      IL_002f:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0034:  ldloca.s   V_1
                      IL_0036:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_003b:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                      IL_0040:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0045:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                      IL_004a:  stloc.2
                      IL_004b:  stloc.3
                      IL_004c:  ldc.i4.0
                      IL_004d:  stloc.s    V_4
                      IL_004f:  br.s       IL_0074
                      IL_0051:  ldloc.3
                      IL_0052:  ldloc.s    V_4
                      IL_0054:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0059:  stloc.1
                      IL_005a:  ldloc.2
                      IL_005b:  ldloca.s   V_1
                      IL_005d:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0062:  ldloca.s   V_1
                      IL_0064:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_0069:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                      IL_006e:  ldloc.s    V_4
                      IL_0070:  ldc.i4.1
                      IL_0071:  add
                      IL_0072:  stloc.s    V_4
                      IL_0074:  ldloc.s    V_4
                      IL_0076:  ldloc.3
                      IL_0077:  ldlen
                      IL_0078:  conv.i4
                      IL_0079:  blt.s      IL_0051
                      IL_007b:  ldloc.2
                      IL_007c:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0081:  ret
                    }
                    """);
            }
        }

        [Theory]
        [CombinatorialData]
        public void BreakingChange_DictionaryAdd_02(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool includeAdd)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                struct MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _d;
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => GetDictionary().GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key]
                    {
                        get { return GetDictionary()[key]; }
                        set { GetDictionary()[key] = value; }
                    }
                    {{(includeAdd ? "public void Add(KeyValuePair<K, V> kvp) { ((ICollection<KeyValuePair<K, V>>)GetDictionary()).Add(kvp); }" : "")}}
                    private Dictionary<K, V> GetDictionary() => _d ??= new();
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<int, string> d;
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        d = [x];
                        d.Report();
                        d = [..y];
                        d.Report();
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && !includeAdd)
            {
                comp.VerifyEmitDiagnostics(
                    // (9,13): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                    //         d = [x];
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[x]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(9, 13),
                    // (11,13): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                    //         d = [..y];
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[..y]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(11, 13));
                return;
            }
            var verifier = CompileAndVerify(comp, expectedOutput: "[2:two], [3:three], ");
            verifier.VerifyDiagnostics();
            if (languageVersion == LanguageVersion.CSharp13)
            {
                verifier.VerifyIL("Program.Main", """
                    {
                      // Code size      117 (0x75)
                      .maxstack  5
                      .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0, //x
                                    MyDictionary<int, string> V_1,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<int, string> V_4)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  ldc.i4.2
                      IL_0003:  ldstr      "two"
                      IL_0008:  call       "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_000d:  ldc.i4.1
                      IL_000e:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0013:  dup
                      IL_0014:  ldc.i4.0
                      IL_0015:  ldc.i4.3
                      IL_0016:  ldstr      "three"
                      IL_001b:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_0020:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0025:  ldloca.s   V_1
                      IL_0027:  initobj    "MyDictionary<int, string>"
                      IL_002d:  ldloca.s   V_1
                      IL_002f:  ldloc.0
                      IL_0030:  call       "void MyDictionary<int, string>.Add(System.Collections.Generic.KeyValuePair<int, string>)"
                      IL_0035:  ldloc.1
                      IL_0036:  box        "MyDictionary<int, string>"
                      IL_003b:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0040:  ldloca.s   V_1
                      IL_0042:  initobj    "MyDictionary<int, string>"
                      IL_0048:  stloc.2
                      IL_0049:  ldc.i4.0
                      IL_004a:  stloc.3
                      IL_004b:  br.s       IL_0063
                      IL_004d:  ldloc.2
                      IL_004e:  ldloc.3
                      IL_004f:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0054:  stloc.s    V_4
                      IL_0056:  ldloca.s   V_1
                      IL_0058:  ldloc.s    V_4
                      IL_005a:  call       "void MyDictionary<int, string>.Add(System.Collections.Generic.KeyValuePair<int, string>)"
                      IL_005f:  ldloc.3
                      IL_0060:  ldc.i4.1
                      IL_0061:  add
                      IL_0062:  stloc.3
                      IL_0063:  ldloc.3
                      IL_0064:  ldloc.2
                      IL_0065:  ldlen
                      IL_0066:  conv.i4
                      IL_0067:  blt.s      IL_004d
                      IL_0069:  ldloc.1
                      IL_006a:  box        "MyDictionary<int, string>"
                      IL_006f:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0074:  ret
                    }
                    """);
            }
            else
            {
                // Using indexer rather than instance method Add() is a breaking change from C#13.
                verifier.VerifyIL("Program.Main", """
                    {
                      // Code size      148 (0x94)
                      .maxstack  5
                      .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0, //x
                                    MyDictionary<int, string> V_1,
                                    System.Collections.Generic.KeyValuePair<int, string> V_2,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_3,
                                    int V_4)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  ldc.i4.2
                      IL_0003:  ldstr      "two"
                      IL_0008:  call       "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_000d:  ldc.i4.1
                      IL_000e:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0013:  dup
                      IL_0014:  ldc.i4.0
                      IL_0015:  ldc.i4.3
                      IL_0016:  ldstr      "three"
                      IL_001b:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_0020:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0025:  ldloca.s   V_1
                      IL_0027:  initobj    "MyDictionary<int, string>"
                      IL_002d:  ldloc.0
                      IL_002e:  stloc.2
                      IL_002f:  ldloca.s   V_1
                      IL_0031:  ldloca.s   V_2
                      IL_0033:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0038:  ldloca.s   V_2
                      IL_003a:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_003f:  call       "void MyDictionary<int, string>.this[int].set"
                      IL_0044:  ldloc.1
                      IL_0045:  box        "MyDictionary<int, string>"
                      IL_004a:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_004f:  ldloca.s   V_1
                      IL_0051:  initobj    "MyDictionary<int, string>"
                      IL_0057:  stloc.3
                      IL_0058:  ldc.i4.0
                      IL_0059:  stloc.s    V_4
                      IL_005b:  br.s       IL_0081
                      IL_005d:  ldloc.3
                      IL_005e:  ldloc.s    V_4
                      IL_0060:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0065:  stloc.2
                      IL_0066:  ldloca.s   V_1
                      IL_0068:  ldloca.s   V_2
                      IL_006a:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_006f:  ldloca.s   V_2
                      IL_0071:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_0076:  call       "void MyDictionary<int, string>.this[int].set"
                      IL_007b:  ldloc.s    V_4
                      IL_007d:  ldc.i4.1
                      IL_007e:  add
                      IL_007f:  stloc.s    V_4
                      IL_0081:  ldloc.s    V_4
                      IL_0083:  ldloc.3
                      IL_0084:  ldlen
                      IL_0085:  conv.i4
                      IL_0086:  blt.s      IL_005d
                      IL_0088:  ldloc.1
                      IL_0089:  box        "MyDictionary<int, string>"
                      IL_008e:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0093:  ret
                    }
                    """);
            }
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void Dictionary_01(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        F<int, string>().Report();
                    }
                    static {{typeName}}<K, V> F<K, V>()
                    {
                        return [];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[], ");
            verifier.VerifyDiagnostics();
            if (typeName == "IReadOnlyDictionary")
            {
                verifier.VerifyIL("Program.F<K, V>", """
                    {
                      // Code size       11 (0xb)
                      .maxstack  1
                      IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                      IL_0005:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                      IL_000a:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.F<K, V>", """
                    {
                      // Code size        6 (0x6)
                      .maxstack  1
                      IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                      IL_0005:  ret
                    }
                    """);
            }
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void Dictionary_02(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        F(1, "one", x, y).Report();
                    }
                    static {{typeName}}<K, V> F<K, V>(K k, V v, KeyValuePair<K, V> e, IEnumerable<KeyValuePair<K, V>> s)
                    {
                        return /*<bind>*/[k:v, e, ..s]/*</bind>*/;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[1:one, 2:two, 3:three], ");
            verifier.VerifyDiagnostics();
            if (typeName == "IReadOnlyDictionary")
            {
                verifier.VerifyIL("Program.F<K, V>", """
                    {
                      // Code size       99 (0x63)
                      .maxstack  3
                      .locals init (System.Collections.Generic.Dictionary<K, V> V_0,
                                    System.Collections.Generic.KeyValuePair<K, V> V_1,
                                    System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> V_2,
                                    System.Collections.Generic.KeyValuePair<K, V> V_3)
                      IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldloc.0
                      IL_0007:  ldarg.0
                      IL_0008:  ldarg.1
                      IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_000e:  ldarg.2
                      IL_000f:  stloc.1
                      IL_0010:  ldloc.0
                      IL_0011:  ldloca.s   V_1
                      IL_0013:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0018:  ldloca.s   V_1
                      IL_001a:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_001f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_0024:  ldarg.3
                      IL_0025:  callvirt   "System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()"
                      IL_002a:  stloc.2
                      .try
                      {
                        IL_002b:  br.s       IL_0048
                        IL_002d:  ldloc.2
                        IL_002e:  callvirt   "System.Collections.Generic.KeyValuePair<K, V> System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>>.Current.get"
                        IL_0033:  stloc.3
                        IL_0034:  ldloc.0
                        IL_0035:  ldloca.s   V_3
                        IL_0037:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                        IL_003c:  ldloca.s   V_3
                        IL_003e:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                        IL_0043:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                        IL_0048:  ldloc.2
                        IL_0049:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                        IL_004e:  brtrue.s   IL_002d
                        IL_0050:  leave.s    IL_005c
                      }
                      finally
                      {
                        IL_0052:  ldloc.2
                        IL_0053:  brfalse.s  IL_005b
                        IL_0055:  ldloc.2
                        IL_0056:  callvirt   "void System.IDisposable.Dispose()"
                        IL_005b:  endfinally
                      }
                      IL_005c:  ldloc.0
                      IL_005d:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                      IL_0062:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.F<K, V>", """
                    {
                      // Code size       94 (0x5e)
                      .maxstack  3
                      .locals init (System.Collections.Generic.Dictionary<K, V> V_0,
                                    System.Collections.Generic.KeyValuePair<K, V> V_1,
                                    System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> V_2,
                                    System.Collections.Generic.KeyValuePair<K, V> V_3)
                      IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldloc.0
                      IL_0007:  ldarg.0
                      IL_0008:  ldarg.1
                      IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_000e:  ldarg.2
                      IL_000f:  stloc.1
                      IL_0010:  ldloc.0
                      IL_0011:  ldloca.s   V_1
                      IL_0013:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0018:  ldloca.s   V_1
                      IL_001a:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_001f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_0024:  ldarg.3
                      IL_0025:  callvirt   "System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()"
                      IL_002a:  stloc.2
                      .try
                      {
                        IL_002b:  br.s       IL_0048
                        IL_002d:  ldloc.2
                        IL_002e:  callvirt   "System.Collections.Generic.KeyValuePair<K, V> System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>>.Current.get"
                        IL_0033:  stloc.3
                        IL_0034:  ldloc.0
                        IL_0035:  ldloca.s   V_3
                        IL_0037:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                        IL_003c:  ldloca.s   V_3
                        IL_003e:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                        IL_0043:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                        IL_0048:  ldloc.2
                        IL_0049:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                        IL_004e:  brtrue.s   IL_002d
                        IL_0050:  leave.s    IL_005c
                      }
                      finally
                      {
                        IL_0052:  ldloc.2
                        IL_0053:  brfalse.s  IL_005b
                        IL_0055:  ldloc.2
                        IL_0056:  callvirt   "void System.IDisposable.Dispose()"
                        IL_005b:  endfinally
                      }
                      IL_005c:  ldloc.0
                      IL_005d:  ret
                    }
                    """);
            }
            var comp = (CSharpCompilation)verifier.Compilation;
            string constructMethod = typeName == "Dictionary" ? "System.Collections.Generic.Dictionary<K, V>..ctor()" : "null";
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                $$"""
                ICollectionExpressionOperation (3 elements, ConstructMethod: {{constructMethod}}) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}<K, V>) (Syntax: '[k:v, e, ..s]')
                  Elements(3):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'k:v')
                      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<K, V>) (Syntax: 'e')
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<K, V>) (OperationKind.Spread, Type: null) (Syntax: '..s')
                        Operand:
                          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>) (Syntax: 's')
                        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                """);
        }

        [Theory]
        [CombinatorialData]
        public void Dictionary_Params(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            [CombinatorialValues("Dictionary", "IDictionary", "IReadOnlyDictionary")] string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Params<int, string>();
                        Three<int, string>(new(1, "one"), new(2, "two"), new(1, "three"));
                    }
                    static void Empty<K, V>() { Params<K, V>(); }
                    static void Three<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z) { Params(x, y, z); }
                    static void Params<K, V>(params {{typeName}}<K, V> args) { args.Report(); }
                }
                """;
            var comp = CreateCompilation(
                [source, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                if (typeName == "Dictionary")
                {
                    comp.VerifyEmitDiagnostics(
                        // (6,9): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.Params<K, V>(params Dictionary<K, V>)'
                        //         Params<int, string>();
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Params<int, string>").WithArguments("args", "Program.Params<K, V>(params System.Collections.Generic.Dictionary<K, V>)").WithLocation(6, 9),
                        // (9,33): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.Params<K, V>(params Dictionary<K, V>)'
                        //     static void Empty<K, V>() { Params<K, V>(); }
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Params<K, V>").WithArguments("args", "Program.Params<K, V>(params System.Collections.Generic.Dictionary<K, V>)").WithLocation(9, 33),
                        // (10,97): error CS1501: No overload for method 'Params' takes 3 arguments
                        //     static void Three<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z) { Params(x, y, z); }
                        Diagnostic(ErrorCode.ERR_BadArgCount, "Params").WithArguments("Params", "3").WithLocation(10, 97),
                        // (11,30): error CS9215: Collection expression type 'Dictionary<K, V>' must have an instance or extension method 'Add' that can be called with a single argument.
                        //     static void Params<K, V>(params Dictionary<K, V> args) { args.Report(); }
                        Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "params Dictionary<K, V> args").WithArguments("System.Collections.Generic.Dictionary<K, V>").WithLocation(11, 30));
                }
                else
                {
                    comp.VerifyEmitDiagnostics(
                        // (6,9): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.Params<K, V>(params IDictionary<K, V>)'
                        //         Params<int, string>();
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Params<int, string>").WithArguments("args", $"Program.Params<K, V>(params System.Collections.Generic.{typeName}<K, V>)").WithLocation(6, 9),
                        // (9,33): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.Params<K, V>(params IDictionary<K, V>)'
                        //     static void Empty<K, V>() { Params<K, V>(); }
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Params<K, V>").WithArguments("args", $"Program.Params<K, V>(params System.Collections.Generic.{typeName}<K, V>)").WithLocation(9, 33),
                        // (10,97): error CS1501: No overload for method 'Params' takes 3 arguments
                        //     static void Three<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z) { Params(x, y, z); }
                        Diagnostic(ErrorCode.ERR_BadArgCount, "Params").WithArguments("Params", "3").WithLocation(10, 97),
                        // (11,30): error CS0225: The params parameter must have a valid collection type
                        //     static void Params<K, V>(params IDictionary<K, V> args) { args.Report(); }
                        Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(11, 30));
                }
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: "[], [1:three, 2:two], ");
                verifier.VerifyDiagnostics();
                if (typeName == "IReadOnlyDictionary")
                {
                    verifier.VerifyIL("Program.Empty<K, V>", $$"""
                        {
                          // Code size       16 (0x10)
                          .maxstack  1
                          IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                          IL_0005:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                          IL_000a:  call       "void Program.Params<K, V>(params System.Collections.Generic.IReadOnlyDictionary<K, V>)"
                          IL_000f:  ret
                        }
                        """);
                    verifier.VerifyIL("Program.Three<K, V>", $$"""
                        {
                          // Code size       82 (0x52)
                          .maxstack  4
                          .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0,
                                        System.Collections.Generic.KeyValuePair<K, V> V_1,
                                        System.Collections.Generic.KeyValuePair<K, V> V_2)
                          IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                          IL_0005:  ldarg.0
                          IL_0006:  stloc.0
                          IL_0007:  dup
                          IL_0008:  ldloca.s   V_0
                          IL_000a:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                          IL_000f:  ldloca.s   V_0
                          IL_0011:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                          IL_0016:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                          IL_001b:  ldarg.1
                          IL_001c:  stloc.1
                          IL_001d:  dup
                          IL_001e:  ldloca.s   V_1
                          IL_0020:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                          IL_0025:  ldloca.s   V_1
                          IL_0027:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                          IL_002c:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                          IL_0031:  ldarg.2
                          IL_0032:  stloc.2
                          IL_0033:  dup
                          IL_0034:  ldloca.s   V_2
                          IL_0036:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                          IL_003b:  ldloca.s   V_2
                          IL_003d:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                          IL_0042:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                          IL_0047:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                          IL_004c:  call       "void Program.Params<K, V>(params System.Collections.Generic.IReadOnlyDictionary<K, V>)"
                          IL_0051:  ret
                        }
                        """);
                }
                else
                {
                    verifier.VerifyIL("Program.Empty<K, V>", $$"""
                        {
                          // Code size       11 (0xb)
                          .maxstack  1
                          IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                          IL_0005:  call       "void Program.Params<K, V>(params System.Collections.Generic.{{typeName}}<K, V>)"
                          IL_000a:  ret
                        }
                        """);
                    verifier.VerifyIL("Program.Three<K, V>", $$"""
                        {
                          // Code size       77 (0x4d)
                          .maxstack  4
                          .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0,
                                        System.Collections.Generic.KeyValuePair<K, V> V_1,
                                        System.Collections.Generic.KeyValuePair<K, V> V_2)
                          IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                          IL_0005:  ldarg.0
                          IL_0006:  stloc.0
                          IL_0007:  dup
                          IL_0008:  ldloca.s   V_0
                          IL_000a:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                          IL_000f:  ldloca.s   V_0
                          IL_0011:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                          IL_0016:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                          IL_001b:  ldarg.1
                          IL_001c:  stloc.1
                          IL_001d:  dup
                          IL_001e:  ldloca.s   V_1
                          IL_0020:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                          IL_0025:  ldloca.s   V_1
                          IL_0027:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                          IL_002c:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                          IL_0031:  ldarg.2
                          IL_0032:  stloc.2
                          IL_0033:  dup
                          IL_0034:  ldloca.s   V_2
                          IL_0036:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                          IL_003b:  ldloca.s   V_2
                          IL_003d:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                          IL_0042:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                          IL_0047:  call       "void Program.Params<K, V>(params System.Collections.Generic.{{typeName}}<K, V>)"
                          IL_004c:  ret
                        }
                        """);
                }
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void CustomDictionary_Params(LanguageVersion languageVersion)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key]
                    {
                        get { return _d[key]; }
                        set { _d[key] = value; }
                    }
                }
                """;
            string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Params<int, string>();
                        Three<int, string>(new(1, "one"), new(2, "two"), new(1, "three"));
                    }
                    static void Empty<K, V>() { Params<K, V>(); }
                    static void Three<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z) { Params(x, y, z); }
                    static void Params<K, V>(params MyDictionary<K, V> args) { args.Report(); }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,9): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.Params<K, V>(params MyDictionary<K, V>)'
                    //         Params<int, string>();
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Params<int, string>").WithArguments("args", "Program.Params<K, V>(params MyDictionary<K, V>)").WithLocation(6, 9),
                    // (9,33): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.Params<K, V>(params MyDictionary<K, V>)'
                    //     static void Empty<K, V>() { Params<K, V>(); }
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Params<K, V>").WithArguments("args", "Program.Params<K, V>(params MyDictionary<K, V>)").WithLocation(9, 33),
                    // (10,97): error CS1501: No overload for method 'Params' takes 3 arguments
                    //     static void Three<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z) { Params(x, y, z); }
                    Diagnostic(ErrorCode.ERR_BadArgCount, "Params").WithArguments("Params", "3").WithLocation(10, 97),
                    // (11,30): error CS0117: 'MyDictionary<K, V>' does not contain a definition for 'Add'
                    //     static void Params<K, V>(params MyDictionary<K, V> args) { args.Report(); }
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>", "Add").WithLocation(11, 30));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: "[], [1:three, 2:two], ");
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("Program.Empty<K, V>", $$"""
                    {
                      // Code size       11 (0xb)
                      .maxstack  1
                      IL_0000:  newobj     "MyDictionary<K, V>..ctor()"
                      IL_0005:  call       "void Program.Params<K, V>(params MyDictionary<K, V>)"
                      IL_000a:  ret
                    }
                    """);
                verifier.VerifyIL("Program.Three<K, V>", $$"""
                    {
                      // Code size       77 (0x4d)
                      .maxstack  4
                      .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0,
                                    System.Collections.Generic.KeyValuePair<K, V> V_1,
                                    System.Collections.Generic.KeyValuePair<K, V> V_2)
                      IL_0000:  newobj     "MyDictionary<K, V>..ctor()"
                      IL_0005:  ldarg.0
                      IL_0006:  stloc.0
                      IL_0007:  dup
                      IL_0008:  ldloca.s   V_0
                      IL_000a:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_000f:  ldloca.s   V_0
                      IL_0011:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0016:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_001b:  ldarg.1
                      IL_001c:  stloc.1
                      IL_001d:  dup
                      IL_001e:  ldloca.s   V_1
                      IL_0020:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0025:  ldloca.s   V_1
                      IL_0027:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_002c:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_0031:  ldarg.2
                      IL_0032:  stloc.2
                      IL_0033:  dup
                      IL_0034:  ldloca.s   V_2
                      IL_0036:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_003b:  ldloca.s   V_2
                      IL_003d:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0042:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_0047:  call       "void Program.Params<K, V>(params MyDictionary<K, V>)"
                      IL_004c:  ret
                    }
                    """);
            }
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void Dictionary_DuplicateKeys(string typeName)
        {
            string source = $$"""
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(101, "one");
                        var y = new KeyValuePair<int, string>(202, "two");
                        var z = new KeyValuePair<int, string>(101, "three");
                        Report(F1(x, y, z));
                        Report(F1(y, z, x));
                        Report(F1(z, x, y));
                        Report(F2(x, y, z));
                        Report(F2(y, z, x));
                        Report(F2(z, x, y));
                        Report(F3(x, y, z));
                        Report(F3(y, z, x));
                        Report(F3(z, x, y));
                    }
                    static void Report<K, V>({{typeName}}<K, V> d)
                    {
                        d.Report();
                        Console.WriteLine();
                    }
                    static {{typeName}}<K, V> F1<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z)
                    {
                        return [x.Key:x.Value, y, .. new[] { z }];
                    }
                    static {{typeName}}<K, V> F2<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z)
                    {
                        return [x, .. new[] { y }, z.Key:z.Value];
                    }
                    static {{typeName}}<K, V> F3<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z)
                    {
                        return [.. new[] { x }, y.Key:y.Value, z];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: """
                    [101:three, 202:two], 
                    [101:one, 202:two], 
                    [101:one, 202:two], 
                    [101:three, 202:two], 
                    [101:one, 202:two], 
                    [101:one, 202:two], 
                    [101:three, 202:two], 
                    [101:one, 202:two], 
                    [101:one, 202:two], 
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void DictionaryNotImplementingIDictionary(string typeName)
        {
            string sourceA = """
                using System.Collections.Generic;
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public struct Enum { }
                    public class Attribute { }
                    public class AttributeUsageAttribute : Attribute
                    {
                        public AttributeUsageAttribute(AttributeTargets t) { }
                        public bool AllowMultiple { get; set; }
                        public bool Inherited { get; set; }
                    }
                    public enum AttributeTargets { }
                    public interface IDisposable
                    {
                        void Dispose();
                    }
                }
                namespace System.Collections
                {
                    public interface IEnumerator
                    {
                        bool MoveNext();
                        object Current { get; }
                    }
                    public interface IEnumerable
                    {
                        IEnumerator GetEnumerator();
                    }
                }
                namespace System.Collections.Generic
                {
                    public interface IEnumerator<T> : IEnumerator
                    {
                        new T Current { get; }
                    }
                    public interface IEnumerable<T> : IEnumerable
                    {
                        new IEnumerator<T> GetEnumerator();
                    }
                    public interface IDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
                    {
                    }
                    public interface IReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
                    {
                    }
                    public struct KeyValuePair<TKey, TValue>
                    {
                        public KeyValuePair(TKey key, TValue value) { }
                        public TKey Key { get; }
                        public TValue Value { get; }
                    }
                    public sealed class Dictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
                    {
                        public Dictionary() { }
                        public TValue this[TKey key] { get { return default; } set { } }
                        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => null;
                        IEnumerator IEnumerable.GetEnumerator() => null;
                    }
                }
                namespace System.Collections.ObjectModel
                {
                    public sealed class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
                    {
                        public ReadOnlyDictionary(IDictionary<TKey, TValue> d) { }
                        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => null;
                        IEnumerator IEnumerable.GetEnumerator() => null;
                    }
                }
                namespace System.Reflection
                {
                    public class DefaultMemberAttribute : Attribute
                    {
                        public DefaultMemberAttribute(string name) { }
                    }
                }
                """;
            string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Empty<int, string>();
                        Many(1, "one", new KeyValuePair<int, string>(), null);
                    }
                    static {{typeName}}<K, V> Empty<K, V>()
                    {
                        return [];
                    }
                    static {{typeName}}<K, V> Many<K, V>(K k, V v, KeyValuePair<K, V> x, IEnumerable<KeyValuePair<K, V>> y)
                    {
                        return [k:v, x, ..y];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB });
            var emitOptions = Microsoft.CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0");
            if (typeName == "Dictionary")
            {
                comp.VerifyEmitDiagnostics(emitOptions);
            }
            else
            {
                comp.VerifyEmitDiagnostics(emitOptions,
                    // 1.cs(11,16): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.Dictionary<K, V>' to 'System.Collections.Generic.IDictionary<K, V>'
                    //         return [];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "[]").WithArguments("System.Collections.Generic.Dictionary<K, V>", $"System.Collections.Generic.{typeName}<K, V>").WithLocation(11, 16),
                    // 1.cs(15,16): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.Dictionary<K, V>' to 'System.Collections.Generic.IDictionary<K, V>'
                    //         return [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "[k:v, x, ..y]").WithArguments("System.Collections.Generic.Dictionary<K, V>", $"System.Collections.Generic.{typeName}<K, V>").WithLocation(15, 16));
            }
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
                // (3,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(3, 5),
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(3, 5),
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(3, 5),
                // (4,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(4, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(4, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(4, 5),
                // (5,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(5, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(5, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(5, 5),
                // (6,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(6, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(6, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(6, 5));

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
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_Dictionary_KV__set_Item);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(6, 5));
        }

        [Theory]
        [InlineData("IDictionary<int, string>")]
        [InlineData("IReadOnlyDictionary<int, string>")]
        [InlineData("IDictionary<int?, object>")]
        [InlineData("IReadOnlyDictionary<int?, object>")]
        public void KeyValuePair_MissingMember_DictionaryInterface(string typeName)
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
                // (5,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(5, 6),
                // (6,9): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(6, 9));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value);
            comp.VerifyEmitDiagnostics(
                // (5,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(5, 6),
                // (6,9): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(6, 9));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__ctor);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void KeyValuePair_MissingMember_CustomDictionary()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                public class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K k] { get => default; set { } }
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections.Generic;
                MyDictionary<int, string> d;
                d = [];
                d = [1:"one"];
                d = [new KeyValuePair<int, string>(2, "two")];
                d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                """;

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Key);
            comp.VerifyEmitDiagnostics(
                // (5,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(5, 6),
                // (6,9): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(6, 9));

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value);
            comp.VerifyEmitDiagnostics(
                // (5,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(5, 6),
                // (6,9): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(6, 9));

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__ctor);
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePair_MissingMember_NonDictionary_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues("KeyValuePair<int, string>[]", "List<KeyValuePair<int, string>>")] string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                {{typeName}} d;
                d = [];
                d = [new KeyValuePair<int, string>(2, "two")];
                d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                """;

            var parseOptions = TestOptions.Regular.WithLanguageVersion(languageVersion);
            var comp = CreateCompilation(source, parseOptions: parseOptions);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source, parseOptions: parseOptions);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Key);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source, parseOptions: parseOptions);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source, parseOptions: parseOptions);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__ctor);
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePair_MissingMember_NonDictionary_02(
            [CombinatorialValues("KeyValuePair<int?, object>[]", "List<KeyValuePair<int?, object>>")] string typeName)
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
                // (5,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(5, 6),
                // (6,9): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(6, 9));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value);
            comp.VerifyEmitDiagnostics(
                // (5,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(5, 6),
                // (6,9): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(6, 9));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__ctor);
            comp.VerifyEmitDiagnostics(
                // (4,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2..ctor'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"1:""one""").WithArguments("System.Collections.Generic.KeyValuePair`2", ".ctor").WithLocation(4, 6),
                // (5,6): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2..ctor'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair`2", ".ctor").WithLocation(5, 6),
                // (6,9): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2..ctor'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair`2", ".ctor").WithLocation(6, 9));
        }

        [Fact]
        public void RefSafety_Indexer()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                using System.Diagnostics.CodeAnalysis;
                ref struct MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private ref readonly K _key;
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[[UnscopedRef] in K key]  { get { return default; } set { _key = ref key; } }
                }
                """;
            string sourceB = """
                class Program
                {
                    static MyDictionary<K, V> FromPair1<K, V>(K k, V v)
                    {
                        MyDictionary<K, V> d = new();
                        d[k] = v;
                        return d;
                    }
                    static MyDictionary<K, V> FromPair2<K, V>(K k, V v)
                    {
                        MyDictionary<K, V> d;
                        d = [k:v];
                        return d;
                    }
                    static MyDictionary<K, V> FromPair3<K, V>(K k, V v)
                    {
                        MyDictionary<K, V> d = [k:v];
                        return d;
                    }
                    static MyDictionary<K, V> FromPair4<K, V>(ref K k, ref V v)
                    {
                        return [k:v];
                    }
                    static MyDictionary<K, V> FromPair5<K, V>(V v)
                    {
                        MyDictionary<K, V> d = [MakeKey<K>():v];
                        return d;
                    }
                    static K MakeKey<K>() => default;
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS8350: This combination of arguments to 'MyDictionary<K, V>.this[in K]' is disallowed because it may expose variables referenced by parameter 'key' outside of their declaration scope
                //         d[k] = v;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "d[k]").WithArguments("MyDictionary<K, V>.this[in K]", "key").WithLocation(6, 9),
                // (6,11): error CS8166: Cannot return a parameter by reference 'k' because it is not a ref parameter
                //         d[k] = v;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "k").WithArguments("k").WithLocation(6, 11),
                // (12,13): error CS9203: A collection expression of type 'MyDictionary<K, V>' cannot be used in this context because it may be exposed outside of the current scope.
                //         d = [k:v];
                Diagnostic(ErrorCode.ERR_CollectionExpressionEscape, "[k:v]").WithArguments("MyDictionary<K, V>").WithLocation(12, 13),
                // (18,16): error CS8352: Cannot use variable 'd' in this context because it may expose referenced variables outside of their declaration scope
                //         return d;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "d").WithArguments("d").WithLocation(18, 16),
                // (22,16): error CS9203: A collection expression of type 'MyDictionary<K, V>' cannot be used in this context because it may be exposed outside of the current scope.
                //         return [k:v];
                Diagnostic(ErrorCode.ERR_CollectionExpressionEscape, "[k:v]").WithArguments("MyDictionary<K, V>").WithLocation(22, 16),
                // (27,16): error CS8352: Cannot use variable 'd' in this context because it may expose referenced variables outside of their declaration scope
                //         return d;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "d").WithArguments("d").WithLocation(27, 16));
        }

        [Fact]
        public void Async()
        {
            string source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class Program
                {
                    static async Task Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        (await Create(1, "one", x, y)).Report();
                    }
                    static async Task<IDictionary<object, object>> Create<K, V>(K k, V v, KeyValuePair<K, V> e, IEnumerable<KeyValuePair<K, V>> s)
                    {
                        return [await F(k):v, k:await F(v), await F(e), .. await F(s)];
                    }
                    static async Task<T> F<T>(T t)
                    {
                        await Task.Yield();
                        return t;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[1:one, 2:two, 3:three], ");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        F(1, "one").Report();
                    }
                    static IDictionary<long, object> F(int x, string y)
                    {
                        return /*<bind>*/[x:y]/*</bind>*/;
                    }
                }
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[1:one], ");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var kvpElement = tree.GetRoot().DescendantNodes().OfType<KeyValuePairElementSyntax>().Single();
            var typeInfo = model.GetTypeInfo(kvpElement.KeyExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int64, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(kvpElement.ValueExpression);
            Assert.Equal(SpecialType.System_String, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, typeInfo.ConvertedType.SpecialType);

            // https://github.com/dotnet/roslyn/issues/77872: Implement IOperation support.
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IDictionary<System.Int64, System.Object>) (Syntax: '[x:y]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'x:y')
                """);
        }

        [Fact]
        public void KeyValuePairExpressionConversions_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>();
                        IDictionary<object, object> d = /*<bind>*/[x]/*</bind>*/;
                        d.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify([source, s_dictionaryExtensions], expectedOutput: "[0:null], ");
            verifier.VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var element = tree.GetRoot().DescendantNodes().OfType<ExpressionElementSyntax>().Single();
            var typeInfo = model.GetTypeInfo(element.Expression);
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Object, System.Object>", typeInfo.ConvertedType.ToTestDisplayString());

            // https://github.com/dotnet/roslyn/issues/77872: Include IOperation support for implicit Key and Value conversions.
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IDictionary<System.Object, System.Object>) (Syntax: '[x]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: System.Collections.Generic.KeyValuePair<System.Object, System.Object>) (Syntax: 'x')
                """);
        }

        [Fact]
        public void KeyValuePairExpressionConversions_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var y = new KeyValuePair<int, string>[0];
                        IDictionary<object, object> d = /*<bind>*/[..y]/*</bind>*/;
                        d.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify([source, s_dictionaryExtensions], expectedOutput: "[], ");
            verifier.VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var element = tree.GetRoot().DescendantNodes().OfType<SpreadElementSyntax>().Single();
            var typeInfo = model.GetTypeInfo(element.Expression);
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]", typeInfo.ConvertedType.ToTestDisplayString());

            // https://github.com/dotnet/roslyn/issues/77872: Include IOperation support for implicit Key and Value conversions.
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IDictionary<System.Object, System.Object>) (Syntax: '[..y]')
                  Elements(1):
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<System.Int32, System.String>) (OperationKind.Spread, Type: null) (Syntax: '..y')
                        Operand:
                          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) (Syntax: 'y')
                        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                """);
        }

        [Fact]
        public void KeyValuePairConversions_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        var d = F(x, y);
                        d.Report();
                    }
                    static IDictionary<long, object> F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[2:two, 3:three], ");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_03()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IDictionary<int, string> F1(KeyValuePair<int?, string> x1, IEnumerable<KeyValuePair<int?, string>> y1)
                    {
                        return [x1, ..y1];
                    }
                    static IDictionary<int, string> F2(KeyValuePair<int, object> x2, IEnumerable<KeyValuePair<int, object>> y2)
                    {
                        return [x2, ..y2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0029: Cannot implicitly convert type 'int?' to 'int'
                //         return [x1, ..y1];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x1").WithArguments("int?", "int").WithLocation(6, 17),
                // (6,23): error CS0029: Cannot implicitly convert type 'int?' to 'int'
                //         return [x1, ..y1];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y1").WithArguments("int?", "int").WithLocation(6, 23),
                // (10,17): error CS0029: Cannot implicitly convert type 'object' to 'string'
                //         return [x2, ..y2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2").WithArguments("object", "string").WithLocation(10, 17),
                // (10,23): error CS0029: Cannot implicitly convert type 'object' to 'string'
                //         return [x2, ..y2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y2").WithArguments("object", "string").WithLocation(10, 23));
        }

        [Fact]
        public void KeyValuePairConversions_04()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, int> d;
                d = [null:default];
                d = [default:null];
                d = [default, null];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,6): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                // d = [null:default];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(3, 6),
                // (4,14): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                // d = [default:null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(4, 14),
                // (5,15): error CS0037: Cannot convert null to 'KeyValuePair<int, int>' because it is a non-nullable value type
                // d = [default, null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Collections.Generic.KeyValuePair<int, int>").WithLocation(5, 15));
        }

        [Fact]
        public void KeyValuePairConversions_05()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IDictionary<K, V> F1<K, V>(K k) => [k];
                    static IDictionary<K, V> F2<K, V>(IEnumerable<K> e) => [..e];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,48): error CS0029: Cannot implicitly convert type 'K' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //     static IDictionary<K, V> F1<K, V>(K k) => [k];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "k").WithArguments("K", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(4, 48),
                // (5,63): error CS0029: Cannot implicitly convert type 'K' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //     static IDictionary<K, V> F2<K, V>(IEnumerable<K> e) => [..e];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("K", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(5, 63));
        }

        [Fact]
        public void KeyValuePairConversions_06()
        {
            string sourceA = """
                using System.Collections.Generic;
                public class MyKeyValuePair<K, V>
                {
                    public MyKeyValuePair(K key, V value)
                    {
                        Key = key;
                        Value = value;
                    }
                    public readonly K Key;
                    public readonly V Value;
                    public override string ToString() => $"{Key}:{Value}";
                    public static implicit operator MyKeyValuePair<K, V>(KeyValuePair<K, V> kvp) => new(kvp.Key, kvp.Value);
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        F(1, "one");
                    }
                    static IEnumerable<MyKeyValuePair<K, V>> F<K, V>(K k, V v)
                    {
                        return [k:v];
                    }
                }
                """;
            comp = CreateCompilation(sourceB1, references: [refA]);
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS9500: Collection expression type 'IEnumerable<MyKeyValuePair<K, V>>' does not support key-value pair elements.
                //         return [k:v];
                Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments("System.Collections.Generic.IEnumerable<MyKeyValuePair<K, V>>").WithLocation(10, 17));

            string sourceB2 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var e = F(new KeyValuePair<int, string>(2, "two"), new KeyValuePair<int, string>[] { new(3, "three") });
                        e.Report();
                    }
                    static IEnumerable<MyKeyValuePair<K, V>> F<K, V>(KeyValuePair<K, V> x, IEnumerable<KeyValuePair<K, V>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB2, s_collectionExtensions],
                references: [refA],
                expectedOutput: "[2:two, 3:three], ");
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp12, "IEnumerable<KeyValuePair<K, V>>")]
        [InlineData(LanguageVersion.Preview, "IEnumerable<KeyValuePair<K, V>>")]
        [InlineData(LanguageVersion.Preview, "IDictionary<K, V>")]
        [InlineData(LanguageVersion.Preview, "Dictionary<K, V>")]
        public void KeyValuePairConversions_07(LanguageVersion languageVersion, string typeName)
        {
            string sourceA = """
                using System.Collections.Generic;
                public class MyKeyValuePair<K, V>
                {
                    public MyKeyValuePair(K key, V value)
                    {
                        Key = key;
                        Value = value;
                    }
                    public readonly K Key;
                    public readonly V Value;
                    public static implicit operator KeyValuePair<K, V>(MyKeyValuePair<K, V> kvp) => new(kvp.Key, kvp.Value);
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new MyKeyValuePair<int, string>(2, "two");
                        var y = new MyKeyValuePair<int, string>[] { new(3, "three") };
                        F(x, y).Report();
                    }
                    static {{typeName}} F<K, V>(MyKeyValuePair<K, V> x, IEnumerable<MyKeyValuePair<K, V>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, s_dictionaryExtensions],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                expectedOutput: "[2:two, 3:three], ");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_08()
        {
            string sourceA = """
                using System.Collections.Generic;
                public class MyKeyValuePair<K, V>
                {
                    public MyKeyValuePair(K key, V value)
                    {
                        Key = key;
                        Value = value;
                    }
                    public readonly K Key;
                    public readonly V Value;
                    public static implicit operator KeyValuePair<K, V>(MyKeyValuePair<K, V> kvp) => new(kvp.Key, kvp.Value);
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<object, string>(1, "one");
                        var y = new MyKeyValuePair<int, string>(2, "two");
                        var z = new MyKeyValuePair<int, object>(3, "three");
                        IDictionary<int, string> d;
                        d = [x, y, z];
                        var sx = new[] { x };
                        var sy = new[] { y };
                        var sz = new[] { z };
                        d = [..sx, ..sy, ..sz];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (10,14): error CS0029: Cannot implicitly convert type 'object' to 'int'
                //         d = [x, y, z];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("object", "int").WithLocation(10, 14),
                // (10,20): error CS0029: Cannot implicitly convert type 'MyKeyValuePair<int, object>' to 'KeyValuePair<int, string>'
                //         d = [x, y, z];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "z").WithArguments("MyKeyValuePair<int, object>", "System.Collections.Generic.KeyValuePair<int, string>").WithLocation(10, 20),
                // (14,16): error CS0029: Cannot implicitly convert type 'object' to 'int'
                //         d = [..sx, ..sy, ..sz];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "sx").WithArguments("object", "int").WithLocation(14, 16),
                // (14,28): error CS0029: Cannot implicitly convert type 'MyKeyValuePair<int, object>' to 'KeyValuePair<int, string>'
                //         d = [..sx, ..sy, ..sz];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "sz").WithArguments("MyKeyValuePair<int, object>", "System.Collections.Generic.KeyValuePair<int, string>").WithLocation(14, 28));
        }

        // PROTOTYPE: Test error cases for k:v and kvp. See GenerateImplicitConversionErrorForCollectionExpression().

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_KeyValueElement(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>",
                "System.Collections.Generic.KeyValuePair<K, V>[]",
                "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>")] string typeName)
        {
            string typeName1 = typeName.Replace("K, V", "int, string");
            string typeName2 = typeName.Replace("K, V", "int?, object");
            string source = $$"""
                class Program
                {
                    static void Main()
                    {
                        int k = 1;
                        string v = "one";
                        KeyValue1(k, v).Report();
                        KeyValue2(k, v).Report();
                    }
                    static {{typeName1}} KeyValue1(int k, string v) =>
                        [k:v];
                    static {{typeName2}} KeyValue2(int k, string v) =>
                        /*<bind>*/[k:v]/*</bind>*/;
                }
                """;
            var comp = CreateCompilation(
                [source, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (11,10): error CS9500: Collection expression type 'IEnumerable<KeyValuePair<int, string>>' does not support key-value pair elements.
                    //         [k:v];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments(typeName1).WithLocation(11, 10),
                    // (11,11): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         [k:v];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(11, 11),
                    // (13,20): error CS9500: Collection expression type 'IEnumerable<KeyValuePair<int?, object>>' does not support key-value pair elements.
                    //         /*<bind>*/[k:v]/*</bind>*/;
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments(typeName2).WithLocation(13, 20),
                    // (13,21): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         /*<bind>*/[k:v]/*</bind>*/;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(13, 21));
            }
            else
            {
                var verifier = CompileAndVerify(comp, expectedOutput: "[[1, one]], [[1, one]],");
                verifier.VerifyDiagnostics();
                if (typeName == "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>")
                {
                    verifier.VerifyIL("Program.KeyValue1", """
                    {
                      // Code size       13 (0xd)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldarg.1
                      IL_0002:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_0007:  newobj     "<>z__ReadOnlySingleElementList<System.Collections.Generic.KeyValuePair<int, string>>..ctor(System.Collections.Generic.KeyValuePair<int, string>)"
                      IL_000c:  ret
                    }
                    """);
                    verifier.VerifyIL("Program.KeyValue2", """
                    {
                      // Code size       18 (0x12)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  newobj     "int?..ctor(int)"
                      IL_0006:  ldarg.1
                      IL_0007:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_000c:  newobj     "<>z__ReadOnlySingleElementList<System.Collections.Generic.KeyValuePair<int?, object>>..ctor(System.Collections.Generic.KeyValuePair<int?, object>)"
                      IL_0011:  ret
                    }
                    """);
                }
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var element = tree.GetRoot().DescendantNodes().OfType<KeyValuePairElementSyntax>().Last();
            var typeInfo = model.GetTypeInfo(element.KeyExpression);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal((languageVersion == LanguageVersion.CSharp12) ? "System.Int32" : "System.Int32?", typeInfo.ConvertedType.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(element.ValueExpression);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal((languageVersion == LanguageVersion.CSharp12) ? "System.String" : "System.Object", typeInfo.ConvertedType.ToTestDisplayString());

            string collectionTypeName = typeName.Replace("K, V", "System.Int32?, System.Object");
            // https://github.com/dotnet/roslyn/issues/77872: Include IOperation support for implicit Key and Value conversions.
            string expectedIOperation = (languageVersion == LanguageVersion.CSharp12) ?
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: {{collectionTypeName}}, IsInvalid) (Syntax: '[k:v]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'k:v')
                """ :
                (typeName == "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>") ?
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>>) (Syntax: '[k:v]')
                  Elements(1):
                      IObjectCreationOperation (Constructor: System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>..ctor(System.Int32? key, System.Object value)) (OperationKind.ObjectCreation, Type: System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>, IsImplicit) (Syntax: 'k:v')
                        Arguments(2):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'k')
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'k')
                                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Operand:
                                  IParameterReferenceOperation: k (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'k')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'v')
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'v')
                                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                Operand:
                                  IParameterReferenceOperation: v (OperationKind.ParameterReference, Type: System.String) (Syntax: 'v')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Initializer:
                          null
                """ :
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: {{collectionTypeName}}) (Syntax: '[k:v]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'k:v')
                """;
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp, expectedIOperation);
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_ExpressionElement(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>",
                "System.Collections.Generic.KeyValuePair<K, V>[]",
                "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>")] string typeName)
        {
            string typeName1 = typeName.Replace("K, V", "int, string");
            string typeName2 = typeName.Replace("K, V", "int?, object");
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Expression1(x).Report();
                        Expression2(x).Report();
                    }
                    static {{typeName1}} Expression1(KeyValuePair<int, string> e) =>
                        [e];
                    static {{typeName2}} Expression2(KeyValuePair<int, string> e) =>
                        /*<bind>*/[e]/*</bind>*/;
                }
                """;
            var comp = CreateCompilation(
                [source, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (13,20): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         /*<bind>*/[e]/*</bind>*/;
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(13, 20));
            }
            else
            {
                var verifier = CompileAndVerify(comp, expectedOutput: "[[2, two]], [[2, two]], ");
                verifier.VerifyDiagnostics();
                if (typeName == "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>")
                {
                    verifier.VerifyIL("Program.Expression1", """
                        {
                          // Code size        7 (0x7)
                          .maxstack  1
                          IL_0000:  ldarg.0
                          IL_0001:  newobj     "<>z__ReadOnlySingleElementList<System.Collections.Generic.KeyValuePair<int, string>>..ctor(System.Collections.Generic.KeyValuePair<int, string>)"
                          IL_0006:  ret
                        }
                        """);
                    verifier.VerifyIL("Program.Expression2", """
                        {
                          // Code size       32 (0x20)
                          .maxstack  2
                          .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0)
                          IL_0000:  ldarg.0
                          IL_0001:  stloc.0
                          IL_0002:  ldloca.s   V_0
                          IL_0004:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                          IL_0009:  newobj     "int?..ctor(int)"
                          IL_000e:  ldloca.s   V_0
                          IL_0010:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                          IL_0015:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                          IL_001a:  newobj     "<>z__ReadOnlySingleElementList<System.Collections.Generic.KeyValuePair<int?, object>>..ctor(System.Collections.Generic.KeyValuePair<int?, object>)"
                          IL_001f:  ret
                        }
                        """);
                }
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var element = tree.GetRoot().DescendantNodes().OfType<ExpressionElementSyntax>().Last();
            // https://github.com/dotnet/roslyn/issues/77872: Implement GetTypeInfo() support.
            var typeInfo = model.GetTypeInfo(element.Expression);
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal((languageVersion == LanguageVersion.CSharp12) ? "System.Collections.Generic.KeyValuePair<System.Int32, System.String>" : "System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>", typeInfo.ConvertedType.ToTestDisplayString());

            string collectionTypeName = typeName.Replace("K, V", "System.Int32?, System.Object");
            // https://github.com/dotnet/roslyn/issues/77872: Include IOperation support for implicit Key and Value conversions.
            string expectedIOperation = (languageVersion == LanguageVersion.CSharp12) ?
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: {{collectionTypeName}}, IsInvalid) (Syntax: '[e]')
                  Elements(1):
                      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.String>, IsInvalid) (Syntax: 'e')
                """ :
                (typeName == "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>") ?
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>>) (Syntax: '[e]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>) (Syntax: 'e')
                """ :
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: {{collectionTypeName}}) (Syntax: '[e]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>) (Syntax: 'e')
                """;
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp, expectedIOperation);
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_SpreadElement_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>",
                "System.Collections.Generic.KeyValuePair<K, V>[]",
                "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>")] string typeName)
        {
            string typeName1 = typeName.Replace("K, V", "int, string");
            string typeName2 = typeName.Replace("K, V", "int?, object");
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        Spread1(y).Report();
                        Spread2(y).Report();
                    }
                    static {{typeName1}} Spread1(KeyValuePair<int, string>[] s) =>
                        [..s];
                    static {{typeName2}} Spread2(KeyValuePair<int, string>[] s) =>
                        /*<bind>*/[..s]/*</bind>*/;
                }
                """;
            var comp = CreateCompilation(
                [source, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (13,22): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         /*<bind>*/[..s]/*</bind>*/;
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(13, 22));
            }
            else
            {
                var verifier = CompileAndVerify(comp, expectedOutput: "[[3, three]], [[3, three]], ");
                verifier.VerifyDiagnostics();
                if (typeName == "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>")
                {
                    verifier.VerifyIL("Program.Spread1", """
                        {
                          // Code size       12 (0xc)
                          .maxstack  1
                          IL_0000:  ldarg.0
                          IL_0001:  call       "System.Collections.Generic.KeyValuePair<int, string>[] System.Linq.Enumerable.ToArray<System.Collections.Generic.KeyValuePair<int, string>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                          IL_0006:  newobj     "<>z__ReadOnlyArray<System.Collections.Generic.KeyValuePair<int, string>>..ctor(System.Collections.Generic.KeyValuePair<int, string>[])"
                          IL_000b:  ret
                        }
                        """);
                    verifier.VerifyIL("Program.Spread2", """
                        {
                          // Code size       82 (0x52)
                          .maxstack  4
                          .locals init (int V_0,
                                        System.Collections.Generic.KeyValuePair<int?, object>[] V_1,
                                        System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                        int V_3,
                                        System.Collections.Generic.KeyValuePair<int, string> V_4,
                                        System.Collections.Generic.KeyValuePair<int, string> V_5)
                          IL_0000:  ldarg.0
                          IL_0001:  ldc.i4.0
                          IL_0002:  stloc.0
                          IL_0003:  dup
                          IL_0004:  ldlen
                          IL_0005:  conv.i4
                          IL_0006:  newarr     "System.Collections.Generic.KeyValuePair<int?, object>"
                          IL_000b:  stloc.1
                          IL_000c:  stloc.2
                          IL_000d:  ldc.i4.0
                          IL_000e:  stloc.3
                          IL_000f:  br.s       IL_0045
                          IL_0011:  ldloc.2
                          IL_0012:  ldloc.3
                          IL_0013:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                          IL_0018:  stloc.s    V_4
                          IL_001a:  ldloc.1
                          IL_001b:  ldloc.0
                          IL_001c:  ldloc.s    V_4
                          IL_001e:  stloc.s    V_5
                          IL_0020:  ldloca.s   V_5
                          IL_0022:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                          IL_0027:  newobj     "int?..ctor(int)"
                          IL_002c:  ldloca.s   V_5
                          IL_002e:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                          IL_0033:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                          IL_0038:  stelem     "System.Collections.Generic.KeyValuePair<int?, object>"
                          IL_003d:  ldloc.0
                          IL_003e:  ldc.i4.1
                          IL_003f:  add
                          IL_0040:  stloc.0
                          IL_0041:  ldloc.3
                          IL_0042:  ldc.i4.1
                          IL_0043:  add
                          IL_0044:  stloc.3
                          IL_0045:  ldloc.3
                          IL_0046:  ldloc.2
                          IL_0047:  ldlen
                          IL_0048:  conv.i4
                          IL_0049:  blt.s      IL_0011
                          IL_004b:  ldloc.1
                          IL_004c:  newobj     "<>z__ReadOnlyArray<System.Collections.Generic.KeyValuePair<int?, object>>..ctor(System.Collections.Generic.KeyValuePair<int?, object>[])"
                          IL_0051:  ret
                        }
                        """);
                }
            }

            string collectionTypeName = typeName.Replace("K, V", "System.Int32?, System.Object");
            // https://github.com/dotnet/roslyn/issues/77872: Include IOperation support for implicit Key and Value conversions.
            string expectedIOperation = (languageVersion == LanguageVersion.CSharp12) ?
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: {{collectionTypeName}}, IsInvalid) (Syntax: '[..s]')
                  Elements(1):
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<System.Int32, System.String>) (OperationKind.Spread, Type: null, IsInvalid) (Syntax: '..s')
                        Operand:
                          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.String>[], IsInvalid) (Syntax: 's')
                        ElementConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (NoConversion)
                """ :
                (typeName == "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>") ?
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Int32?, System.Object>>) (Syntax: '[..s]')
                  Elements(1):
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<System.Int32, System.String>) (OperationKind.Spread, Type: null) (Syntax: '..s')
                        Operand:
                          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) (Syntax: 's')
                        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                """ :
                $$"""
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: {{collectionTypeName}}) (Syntax: '[..s]')
                  Elements(1):
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<System.Int32, System.String>) (OperationKind.Spread, Type: null) (Syntax: '..s')
                        Operand:
                          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) (Syntax: 's')
                        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                """;
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp, expectedIOperation);
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_SpreadElement_02(
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>",
                "System.Collections.Generic.KeyValuePair<K, V>[]",
                "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>",
                "System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>",
                "System.Collections.Immutable.ImmutableArray<KeyValuePair<K, V>>")] string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Spread<int, string, object, object>([new KeyValuePair<int, string>(3, "three")]).Report();
                    }
                    static KeyValuePair<KBase, VBase>[] Spread<K, V, KBase, VBase>({{typeName}} s)
                        where K : KBase
                        where V : VBase
                    {
                        return [..s];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[[3, three]], "));
            verifier.VerifyDiagnostics();
            string expectedIL = typeName switch
            {
                "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>" => """
                    {
                      // Code size       96 (0x60)
                      .maxstack  3
                      .locals init (System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<KBase, VBase>> V_0,
                                    System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> V_1,
                                    System.Collections.Generic.KeyValuePair<K, V> V_2,
                                    System.Collections.Generic.KeyValuePair<K, V> V_3)
                      IL_0000:  newobj     "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<KBase, VBase>>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldarg.0
                      IL_0007:  callvirt   "System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()"
                      IL_000c:  stloc.1
                      .try
                      {
                        IL_000d:  br.s       IL_0045
                        IL_000f:  ldloc.1
                        IL_0010:  callvirt   "System.Collections.Generic.KeyValuePair<K, V> System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>>.Current.get"
                        IL_0015:  stloc.2
                        IL_0016:  ldloc.0
                        IL_0017:  ldloc.2
                        IL_0018:  stloc.3
                        IL_0019:  ldloca.s   V_3
                        IL_001b:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                        IL_0020:  box        "K"
                        IL_0025:  unbox.any  "KBase"
                        IL_002a:  ldloca.s   V_3
                        IL_002c:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                        IL_0031:  box        "V"
                        IL_0036:  unbox.any  "VBase"
                        IL_003b:  newobj     "System.Collections.Generic.KeyValuePair<KBase, VBase>..ctor(KBase, VBase)"
                        IL_0040:  callvirt   "void System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<KBase, VBase>>.Add(System.Collections.Generic.KeyValuePair<KBase, VBase>)"
                        IL_0045:  ldloc.1
                        IL_0046:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                        IL_004b:  brtrue.s   IL_000f
                        IL_004d:  leave.s    IL_0059
                      }
                      finally
                      {
                        IL_004f:  ldloc.1
                        IL_0050:  brfalse.s  IL_0058
                        IL_0052:  ldloc.1
                        IL_0053:  callvirt   "void System.IDisposable.Dispose()"
                        IL_0058:  endfinally
                      }
                      IL_0059:  ldloc.0
                      IL_005a:  callvirt   "System.Collections.Generic.KeyValuePair<KBase, VBase>[] System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<KBase, VBase>>.ToArray()"
                      IL_005f:  ret
                    }
                    """,
                "System.Collections.Generic.KeyValuePair<K, V>[]" => """
                    {
                      // Code size       92 (0x5c)
                      .maxstack  4
                      .locals init (int V_0,
                                    System.Collections.Generic.KeyValuePair<KBase, VBase>[] V_1,
                                    System.Collections.Generic.KeyValuePair<K, V>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4,
                                    System.Collections.Generic.KeyValuePair<K, V> V_5)
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stloc.0
                      IL_0003:  dup
                      IL_0004:  ldlen
                      IL_0005:  conv.i4
                      IL_0006:  newarr     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                      IL_000b:  stloc.1
                      IL_000c:  stloc.2
                      IL_000d:  ldc.i4.0
                      IL_000e:  stloc.3
                      IL_000f:  br.s       IL_0054
                      IL_0011:  ldloc.2
                      IL_0012:  ldloc.3
                      IL_0013:  ldelem     "System.Collections.Generic.KeyValuePair<K, V>"
                      IL_0018:  stloc.s    V_4
                      IL_001a:  ldloc.1
                      IL_001b:  ldloc.0
                      IL_001c:  ldloc.s    V_4
                      IL_001e:  stloc.s    V_5
                      IL_0020:  ldloca.s   V_5
                      IL_0022:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0027:  box        "K"
                      IL_002c:  unbox.any  "KBase"
                      IL_0031:  ldloca.s   V_5
                      IL_0033:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0038:  box        "V"
                      IL_003d:  unbox.any  "VBase"
                      IL_0042:  newobj     "System.Collections.Generic.KeyValuePair<KBase, VBase>..ctor(KBase, VBase)"
                      IL_0047:  stelem     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                      IL_004c:  ldloc.0
                      IL_004d:  ldc.i4.1
                      IL_004e:  add
                      IL_004f:  stloc.0
                      IL_0050:  ldloc.3
                      IL_0051:  ldc.i4.1
                      IL_0052:  add
                      IL_0053:  stloc.3
                      IL_0054:  ldloc.3
                      IL_0055:  ldloc.2
                      IL_0056:  ldlen
                      IL_0057:  conv.i4
                      IL_0058:  blt.s      IL_0011
                      IL_005a:  ldloc.1
                      IL_005b:  ret
                    }
                    """,
                "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>" => """
                    {
                      // Code size      111 (0x6f)
                      .maxstack  4
                      .locals init (int V_0,
                                    System.Collections.Generic.KeyValuePair<KBase, VBase>[] V_1,
                                    System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator V_2,
                                    System.Collections.Generic.KeyValuePair<K, V> V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4)
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stloc.0
                      IL_0003:  dup
                      IL_0004:  callvirt   "int System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>.Count.get"
                      IL_0009:  newarr     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                      IL_000e:  stloc.1
                      IL_000f:  callvirt   "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()"
                      IL_0014:  stloc.2
                      .try
                      {
                        IL_0015:  br.s       IL_0054
                        IL_0017:  ldloca.s   V_2
                        IL_0019:  call       "System.Collections.Generic.KeyValuePair<K, V> System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator.Current.get"
                        IL_001e:  stloc.3
                        IL_001f:  ldloc.1
                        IL_0020:  ldloc.0
                        IL_0021:  ldloc.3
                        IL_0022:  stloc.s    V_4
                        IL_0024:  ldloca.s   V_4
                        IL_0026:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                        IL_002b:  box        "K"
                        IL_0030:  unbox.any  "KBase"
                        IL_0035:  ldloca.s   V_4
                        IL_0037:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                        IL_003c:  box        "V"
                        IL_0041:  unbox.any  "VBase"
                        IL_0046:  newobj     "System.Collections.Generic.KeyValuePair<KBase, VBase>..ctor(KBase, VBase)"
                        IL_004b:  stelem     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                        IL_0050:  ldloc.0
                        IL_0051:  ldc.i4.1
                        IL_0052:  add
                        IL_0053:  stloc.0
                        IL_0054:  ldloca.s   V_2
                        IL_0056:  call       "bool System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator.MoveNext()"
                        IL_005b:  brtrue.s   IL_0017
                        IL_005d:  leave.s    IL_006d
                      }
                      finally
                      {
                        IL_005f:  ldloca.s   V_2
                        IL_0061:  constrained. "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator"
                        IL_0067:  callvirt   "void System.IDisposable.Dispose()"
                        IL_006c:  endfinally
                      }
                      IL_006d:  ldloc.1
                      IL_006e:  ret
                    }
                    """,
                "System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>" => """
                    {
                      // Code size      106 (0x6a)
                      .maxstack  4
                      .locals init (System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>> V_0,
                                    int V_1,
                                    System.Collections.Generic.KeyValuePair<KBase, VBase>[] V_2,
                                    System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4,
                                    System.Collections.Generic.KeyValuePair<K, V> V_5)
                      IL_0000:  ldarg.0
                      IL_0001:  stloc.0
                      IL_0002:  ldc.i4.0
                      IL_0003:  stloc.1
                      IL_0004:  ldloca.s   V_0
                      IL_0006:  call       "int System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>.Length.get"
                      IL_000b:  newarr     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                      IL_0010:  stloc.2
                      IL_0011:  ldloca.s   V_0
                      IL_0013:  call       "System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()"
                      IL_0018:  stloc.3
                      IL_0019:  br.s       IL_005f
                      IL_001b:  ldloca.s   V_3
                      IL_001d:  call       "ref readonly System.Collections.Generic.KeyValuePair<K, V> System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator.Current.get"
                      IL_0022:  ldobj      "System.Collections.Generic.KeyValuePair<K, V>"
                      IL_0027:  stloc.s    V_4
                      IL_0029:  ldloc.2
                      IL_002a:  ldloc.1
                      IL_002b:  ldloc.s    V_4
                      IL_002d:  stloc.s    V_5
                      IL_002f:  ldloca.s   V_5
                      IL_0031:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0036:  box        "K"
                      IL_003b:  unbox.any  "KBase"
                      IL_0040:  ldloca.s   V_5
                      IL_0042:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0047:  box        "V"
                      IL_004c:  unbox.any  "VBase"
                      IL_0051:  newobj     "System.Collections.Generic.KeyValuePair<KBase, VBase>..ctor(KBase, VBase)"
                      IL_0056:  stelem     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                      IL_005b:  ldloc.1
                      IL_005c:  ldc.i4.1
                      IL_005d:  add
                      IL_005e:  stloc.1
                      IL_005f:  ldloca.s   V_3
                      IL_0061:  call       "bool System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator.MoveNext()"
                      IL_0066:  brtrue.s   IL_001b
                      IL_0068:  ldloc.2
                      IL_0069:  ret
                    }
                    """,
                "System.Collections.Immutable.ImmutableArray<KeyValuePair<K, V>>" => """
                    {
                      // Code size      101 (0x65)
                      .maxstack  4
                      .locals init (System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<K, V>> V_0,
                                    int V_1,
                                    System.Collections.Generic.KeyValuePair<KBase, VBase>[] V_2,
                                    System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4,
                                    System.Collections.Generic.KeyValuePair<K, V> V_5)
                      IL_0000:  ldarg.0
                      IL_0001:  stloc.0
                      IL_0002:  ldc.i4.0
                      IL_0003:  stloc.1
                      IL_0004:  ldloca.s   V_0
                      IL_0006:  call       "int System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<K, V>>.Length.get"
                      IL_000b:  newarr     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                      IL_0010:  stloc.2
                      IL_0011:  ldloca.s   V_0
                      IL_0013:  call       "System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()"
                      IL_0018:  stloc.3
                      IL_0019:  br.s       IL_005a
                      IL_001b:  ldloca.s   V_3
                      IL_001d:  call       "System.Collections.Generic.KeyValuePair<K, V> System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator.Current.get"
                      IL_0022:  stloc.s    V_4
                      IL_0024:  ldloc.2
                      IL_0025:  ldloc.1
                      IL_0026:  ldloc.s    V_4
                      IL_0028:  stloc.s    V_5
                      IL_002a:  ldloca.s   V_5
                      IL_002c:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0031:  box        "K"
                      IL_0036:  unbox.any  "KBase"
                      IL_003b:  ldloca.s   V_5
                      IL_003d:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0042:  box        "V"
                      IL_0047:  unbox.any  "VBase"
                      IL_004c:  newobj     "System.Collections.Generic.KeyValuePair<KBase, VBase>..ctor(KBase, VBase)"
                      IL_0051:  stelem     "System.Collections.Generic.KeyValuePair<KBase, VBase>"
                      IL_0056:  ldloc.1
                      IL_0057:  ldc.i4.1
                      IL_0058:  add
                      IL_0059:  stloc.1
                      IL_005a:  ldloca.s   V_3
                      IL_005c:  call       "bool System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<K, V>>.Enumerator.MoveNext()"
                      IL_0061:  brtrue.s   IL_001b
                      IL_0063:  ldloc.2
                      IL_0064:  ret
                    }
                    """,
                _ => throw ExceptionUtilities.UnexpectedValue(typeName),
            };
            verifier.VerifyIL("Program.Spread<K, V, KBase, VBase>", expectedIL);
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_Params_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>",
                "System.Collections.Generic.KeyValuePair<K, V>[]",
                "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<K, V>>")] string typeName)
        {
            string sourceA = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Params(x);
                        Params<int, string>(x);
                        /*<bind>*/Params<int?, object>(x)/*</bind>*/;
                    }
                    static void Params<K, V>(params {{typeName}} args)
                    {
                        args.Report();
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            // https://github.com/dotnet/csharplang/blob/main/proposals/dictionary-expressions.md#support-keyvaluepair-variance-with-params: Support KeyValuePair<,> variance with params?
            comp.VerifyEmitDiagnostics(
                // (9,40): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         /*<bind>*/Params<int?, object>(x)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(9, 40));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = GetSyntaxNodeOfTypeForBinding<InvocationExpressionSyntax>(tree.GetRoot().DescendantNodes().ToList());
            var expression = node.ArgumentList.Arguments[0];
            // https://github.com/dotnet/roslyn/issues/77872: Implement GetTypeInfo() support.
            //var typeInfo = model.GetTypeInfo(expression);
            //Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.Type.ToTestDisplayString());
            //Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.ConvertedType.ToTestDisplayString());

            // https://github.com/dotnet/roslyn/issues/77872: Include IOperation support for implicit Key and Value conversions.
            VerifyOperationTreeForTest<InvocationExpressionSyntax>(comp,
                """
                IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'Params<int?, object>(x)')
                  Children(1):
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.String>, IsInvalid) (Syntax: 'x')
                """);

            string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Single(x);
                        Single<int, string>(x);
                        Single<int?, object>(x);
                    }
                    static void Single<K, V>(KeyValuePair<K, V> kvp)
                    {
                    }
                }
                """;
            comp = CreateCompilation(
                sourceB,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (9,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         Single<int?, object>(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(9, 30));

            string collectionTypeName = typeName.Replace("K, V", "int, string");
            string sourceC = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        {{collectionTypeName}} args = [x];
                        Params(args);
                        Params<int, string>(args);
                        Params<int?, object>(args);
                    }
                    static void Params<K, V>(params {{typeName}} args)
                    {
                    }
                }
                """;
            comp = CreateCompilation(
                sourceC,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (10,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         Params<int?, object>(args);
                Diagnostic(ErrorCode.ERR_BadArgType, "args").WithArguments("1", collectionTypeName, "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(10, 30));
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_Params_02(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues("ReadOnlySpan", "Span")] string typeName)
        {
            string source = $$"""
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Params<int, string>(x);
                        Params<int?, object>(x);
                    }
                    static void Params<K, V>(params {{typeName}}<KeyValuePair<K, V>> args)
                    {
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (9,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         Params<int?, object>(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(9, 30));
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_Params_03(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues("IReadOnlyDictionary", "Dictionary")] string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Params<int, string>(x);
                        Params<int?, object>(x);
                    }
                    static void Params<K, V>(params {{typeName}}<K, V> args)
                    {
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                if (typeName == "Dictionary")
                {
                    comp.VerifyEmitDiagnostics(
                        // (7,29): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'params System.Collections.Generic.Dictionary<int, string>'
                        //         Params<int, string>(x);
                        Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "params System.Collections.Generic.Dictionary<int, string>").WithLocation(7, 29),
                        // (8,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'params System.Collections.Generic.Dictionary<int?, object>'
                        //         Params<int?, object>(x);
                        Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "params System.Collections.Generic.Dictionary<int?, object>").WithLocation(8, 30),
                        // (10,30): error CS9215: Collection expression type 'Dictionary<K, V>' must have an instance or extension method 'Add' that can be called with a single argument.
                        //     static void Params<K, V>(params Dictionary<K, V> args)
                        Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "params Dictionary<K, V> args").WithArguments("System.Collections.Generic.Dictionary<K, V>").WithLocation(10, 30));
                }
                else
                {
                    comp.VerifyEmitDiagnostics(
                        // (7,29): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'params System.Collections.Generic.IReadOnlyDictionary<int, string>'
                        //         Params<int, string>(x);
                        Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "params System.Collections.Generic.IReadOnlyDictionary<int, string>").WithLocation(7, 29),
                        // (8,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'params System.Collections.Generic.IReadOnlyDictionary<int?, object>'
                        //         Params<int?, object>(x);
                        Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "params System.Collections.Generic.IReadOnlyDictionary<int?, object>").WithLocation(8, 30),
                        // (10,30): error CS0225: The params parameter must have a valid collection type
                        //     static void Params<K, V>(params IReadOnlyDictionary<K, V> args)
                        Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(10, 30));
                }
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (8,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         Params<int?, object>(x);
                    Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(8, 30));
            }
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_Span(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int k = 1;
                        string v = "one";
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        ReadOnlySpan<KeyValuePair<int, string>> c1 = [k:v, x, ..y];
                        c1.ToArray().Report();
                        ReadOnlySpan<KeyValuePair<int?, object>> c2 = [k:v, x, ..y];
                        c2.ToArray().Report();
                    }
                }
                """;
            var comp = CreateCompilation(
                [source, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net80);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (11,55): error CS9500: Collection expression type 'ReadOnlySpan<KeyValuePair<int, string>>' does not support key-value pair elements.
                    //         ReadOnlySpan<KeyValuePair<int, string>> c1 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments("System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<int, string>>").WithLocation(11, 55),
                    // (11,56): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         ReadOnlySpan<KeyValuePair<int, string>> c1 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(11, 56),
                    // (13,56): error CS9500: Collection expression type 'ReadOnlySpan<KeyValuePair<int?, object>>' does not support key-value pair elements.
                    //         ReadOnlySpan<KeyValuePair<int?, object>> c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments("System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<int?, object>>").WithLocation(13, 56),
                    // (13,57): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         ReadOnlySpan<KeyValuePair<int?, object>> c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(13, 57),
                    // (13,61): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         ReadOnlySpan<KeyValuePair<int?, object>> c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(13, 61),
                    // (13,66): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         ReadOnlySpan<KeyValuePair<int?, object>> c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(13, 66));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    verify: Verification.Skipped,
                    expectedOutput: IncludeExpectedOutput("[[1, one], [2, two], [3, three]], [[1, one], [2, two], [3, three]], "));
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_CustomType(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool useCollectionBuilder)
        {
            string sourceA = useCollectionBuilder ?
                """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                public class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    internal MyCollection(ReadOnlySpan<T> items) { _items = new(items.ToArray()); }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(items);
                }
                """ :
                """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                public class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection() { _items = new(); }
                    public void Add(T t) { _items.Add(t); }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                """;
            var comp = CreateCompilation(
                sourceA,
                targetFramework: TargetFramework.Net80);
            var refA = comp.EmitToImageReference();

            string sourceB1 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int k = 1;
                        string v = "one";
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        MyCollection<KeyValuePair<int, string>> c1;
                        c1 = [k:v, x, ..y];
                        c1.Report();
                        MyCollection<KeyValuePair<int?, object>> c2;
                        c2 = [k:v, x, ..y];
                        c2.Report();
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe,
                references: [refA],
                targetFramework: TargetFramework.Net80);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (11,15): error CS9500: Collection expression type 'MyCollection<KeyValuePair<int, string>>' does not support key-value pair elements.
                    //         c1 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int, string>>").WithLocation(11, 15),
                    // (11,16): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         c1 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(11, 16),
                    // (14,15): error CS9500: Collection expression type 'MyCollection<KeyValuePair<int?, object>>' does not support key-value pair elements.
                    //         c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>").WithLocation(14, 15),
                    // (14,16): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(14, 16),
                    // (14,20): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(14, 20),
                    // (14,25): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         c2 = [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(14, 25));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    verify: Verification.Skipped,
                    expectedOutput: IncludeExpectedOutput("[[1, one], [2, two], [3, three]], [[1, one], [2, two], [3, three]], "));
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Params(x);
                        Params<int, string>(x);
                        Params<int?, object>(x);
                    }
                    static void Params<K, V>(params MyCollection<KeyValuePair<K, V>> args)
                    {
                    }
                }
                """;
            comp = CreateCompilation(
                sourceB2,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: [refA],
                targetFramework: TargetFramework.Net80);
            // https://github.com/dotnet/csharplang/blob/main/proposals/dictionary-expressions.md#support-keyvaluepair-variance-with-params: Support KeyValuePair<,> variance with params?
            comp.VerifyEmitDiagnostics(
                // (9,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         Params<int?, object>(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(9, 30));
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_AddObject(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool useDynamic)
        {
            string parameterType = useDynamic ? "dynamic" : "object";
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                public class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection() { _items = new(); }
                    public void Add({{parameterType}} o) { _items.Add((T)o); }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                """;
            var comp = CreateCompilation(sourceA,
                targetFramework: TargetFramework.Net80);
            var refA = comp.EmitToImageReference();

            string sourceB1 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int k = 1;
                        string v = "one";
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        Many1(k, v, x, y).Report();
                        Many2(k, v, x, y).Report();
                    }
                    static MyCollection<KeyValuePair<int, string>> Many1(int k1, string v1, KeyValuePair<int, string> x1, KeyValuePair<int, string>[] y1)
                    {
                        return [k1:v1, x1, ..y1];
                    }
                    static MyCollection<KeyValuePair<int?, object>> Many2(int k2, string v2, KeyValuePair<int, string> x2, KeyValuePair<int, string>[] y2)
                    {
                        return [k2:v2, x2, ..y2];
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: [refA],
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net80);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (15,17): error CS9500: Collection expression type 'MyCollection<KeyValuePair<int, string>>' does not support key-value pair elements.
                    //         return [k1:v1, x1, ..y1];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k1:v1").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int, string>>").WithLocation(15, 17),
                    // (15,19): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         return [k1:v1, x1, ..y1];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(15, 19),
                    // (19,17): error CS9500: Collection expression type 'MyCollection<KeyValuePair<int?, object>>' does not support key-value pair elements.
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k2:v2").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>").WithLocation(19, 17),
                    // (19,19): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(19, 19),
                    // (19,24): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(19, 24),
                    // (19,30): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "y2").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(19, 30));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    verify: Verification.Skipped,
                    expectedOutput: IncludeExpectedOutput("[[1, one], [2, two], [3, three]], [[1, one], [2, two], [3, three]], "));
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("Program.Many2", $$"""
                    {
                      // Code size      130 (0x82)
                      .maxstack  3
                      .locals init (MyCollection<System.Collections.Generic.KeyValuePair<int?, object>> V_0,
                                    System.Collections.Generic.KeyValuePair<int, string> V_1,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<int, string> V_4)
                      IL_0000:  newobj     "MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldloc.0
                      IL_0007:  ldarg.0
                      IL_0008:  newobj     "int?..ctor(int)"
                      IL_000d:  ldarg.1
                      IL_000e:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_0013:  box        "System.Collections.Generic.KeyValuePair<int?, object>"
                      IL_0018:  callvirt   "void MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add({{parameterType}})"
                      IL_001d:  ldloc.0
                      IL_001e:  ldarg.2
                      IL_001f:  stloc.1
                      IL_0020:  ldloca.s   V_1
                      IL_0022:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0027:  newobj     "int?..ctor(int)"
                      IL_002c:  ldloca.s   V_1
                      IL_002e:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_0033:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_0038:  box        "System.Collections.Generic.KeyValuePair<int?, object>"
                      IL_003d:  callvirt   "void MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add({{parameterType}})"
                      IL_0042:  ldarg.3
                      IL_0043:  stloc.2
                      IL_0044:  ldc.i4.0
                      IL_0045:  stloc.3
                      IL_0046:  br.s       IL_007a
                      IL_0048:  ldloc.2
                      IL_0049:  ldloc.3
                      IL_004a:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_004f:  stloc.1
                      IL_0050:  ldloc.0
                      IL_0051:  ldloc.1
                      IL_0052:  stloc.s    V_4
                      IL_0054:  ldloca.s   V_4
                      IL_0056:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_005b:  newobj     "int?..ctor(int)"
                      IL_0060:  ldloca.s   V_4
                      IL_0062:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_0067:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_006c:  box        "System.Collections.Generic.KeyValuePair<int?, object>"
                      IL_0071:  callvirt   "void MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add({{parameterType}})"
                      IL_0076:  ldloc.3
                      IL_0077:  ldc.i4.1
                      IL_0078:  add
                      IL_0079:  stloc.3
                      IL_007a:  ldloc.3
                      IL_007b:  ldloc.2
                      IL_007c:  ldlen
                      IL_007d:  conv.i4
                      IL_007e:  blt.s      IL_0048
                      IL_0080:  ldloc.0
                      IL_0081:  ret
                    }
                    """);
            }

            string sourceB2 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Params<int, string>();
                        Params<int?, object>();
                        Params<int, string>(x);
                        Params<int?, object>(x);
                    }
                    static MyCollection<KeyValuePair<K, V>> Params<K, V>(params MyCollection<KeyValuePair<K, V>> args) => args;
                }
                """;
            comp = CreateCompilation(
                sourceB2,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: [refA],
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (10,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         Params<int?, object>(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(10, 30));
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_AddUserDefinedConversion(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                public struct MyPair<K, V>
                {
                    public readonly K Key;
                    public readonly V Value;
                    public MyPair(K key, V value) { Key = key; Value = value; }
                    public static implicit operator MyPair<K, V>(KeyValuePair<K, V> kvp) => new(kvp.Key, kvp.Value);
                }
                public class MyCollection<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private readonly List<KeyValuePair<K, V>> _items;
                    public MyCollection() { _items = new(); }
                    public void Add(MyPair<K, V> pair) { _items.Add(new(pair.Key, pair.Value)); }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int k = 1;
                        string v = "one";
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        Many1(k, v, x, y).Report();
                        Many2(k, v, x, y).Report();
                    }
                    static MyCollection<int, string> Many1(int k1, string v1, KeyValuePair<int, string> x1, KeyValuePair<int, string>[] y1)
                    {
                        return [k1:v1, x1, ..y1];
                    }
                    static MyCollection<int?, object> Many2(int k2, string v2, KeyValuePair<int, string> x2, KeyValuePair<int, string>[] y2)
                    {
                        return [k2:v2, x2, ..y2];
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: [refA],
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (15,17): error CS9500: Collection expression type 'MyCollection<int, string>' does not support key-value pair elements.
                    //         return [k1:v1, x1, ..y1];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k1:v1").WithArguments("MyCollection<int, string>").WithLocation(15, 17),
                    // (15,19): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         return [k1:v1, x1, ..y1];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(15, 19),
                    // (19,17): error CS9500: Collection expression type 'MyCollection<int?, object>' does not support key-value pair elements.
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k2:v2").WithArguments("MyCollection<int?, object>").WithLocation(19, 17),
                    // (19,19): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(19, 19),
                    // (19,24): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(19, 24),
                    // (19,30): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "y2").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(19, 30));
            }
            else
            {
                var verifier = CompileAndVerify(comp, expectedOutput: "[[1, one], [2, two], [3, three]], [[1, one], [2, two], [3, three]], ");
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("Program.Many2", """
                    {
                      // Code size      130 (0x82)
                      .maxstack  3
                      .locals init (MyCollection<int?, object> V_0,
                                    System.Collections.Generic.KeyValuePair<int, string> V_1,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<int, string> V_4)
                      IL_0000:  newobj     "MyCollection<int?, object>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldloc.0
                      IL_0007:  ldarg.0
                      IL_0008:  newobj     "int?..ctor(int)"
                      IL_000d:  ldarg.1
                      IL_000e:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_0013:  call       "MyPair<int?, object> MyPair<int?, object>.op_Implicit(System.Collections.Generic.KeyValuePair<int?, object>)"
                      IL_0018:  callvirt   "void MyCollection<int?, object>.Add(MyPair<int?, object>)"
                      IL_001d:  ldloc.0
                      IL_001e:  ldarg.2
                      IL_001f:  stloc.1
                      IL_0020:  ldloca.s   V_1
                      IL_0022:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0027:  newobj     "int?..ctor(int)"
                      IL_002c:  ldloca.s   V_1
                      IL_002e:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_0033:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_0038:  call       "MyPair<int?, object> MyPair<int?, object>.op_Implicit(System.Collections.Generic.KeyValuePair<int?, object>)"
                      IL_003d:  callvirt   "void MyCollection<int?, object>.Add(MyPair<int?, object>)"
                      IL_0042:  ldarg.3
                      IL_0043:  stloc.2
                      IL_0044:  ldc.i4.0
                      IL_0045:  stloc.3
                      IL_0046:  br.s       IL_007a
                      IL_0048:  ldloc.2
                      IL_0049:  ldloc.3
                      IL_004a:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_004f:  stloc.1
                      IL_0050:  ldloc.0
                      IL_0051:  ldloc.1
                      IL_0052:  stloc.s    V_4
                      IL_0054:  ldloca.s   V_4
                      IL_0056:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_005b:  newobj     "int?..ctor(int)"
                      IL_0060:  ldloca.s   V_4
                      IL_0062:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_0067:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_006c:  call       "MyPair<int?, object> MyPair<int?, object>.op_Implicit(System.Collections.Generic.KeyValuePair<int?, object>)"
                      IL_0071:  callvirt   "void MyCollection<int?, object>.Add(MyPair<int?, object>)"
                      IL_0076:  ldloc.3
                      IL_0077:  ldc.i4.1
                      IL_0078:  add
                      IL_0079:  stloc.3
                      IL_007a:  ldloc.3
                      IL_007b:  ldloc.2
                      IL_007c:  ldlen
                      IL_007d:  conv.i4
                      IL_007e:  blt.s      IL_0048
                      IL_0080:  ldloc.0
                      IL_0081:  ret
                    }
                    """);
            }

            string sourceB2 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Params<int, string>();
                        Params<int?, object>();
                        Params<int, string>(x);
                        Params<int?, object>(x);
                    }
                    static MyCollection<K, V> Params<K, V>(params MyCollection<K, V> args) => args;
                }
                """;
            comp = CreateCompilation(
                sourceB2,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: [refA],
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (10,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         Params<int?, object>(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(10, 30));
        }

        [Theory]
        [CombinatorialData]
        public void KeyValuePairConversions_AddOverloads(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool includeAddT)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                public class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection() { _items = new(); }
                    {{(includeAddT ? "public void Add(T t) { _items.Add(t); }" : "")}}
                    public void Add(KeyValuePair<int, string> kvp) { _items.Add((T)(object)kvp); }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        int k = 1;
                        string v = "one";
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        Many1(k, v, x, y).Report();
                        Many2(k, v, x, y).Report();
                    }
                    static MyCollection<KeyValuePair<int, string>> Many1(int k1, string v1, KeyValuePair<int, string> x1, KeyValuePair<int, string>[] y1)
                    {
                        return [k1:v1, x1, ..y1];
                    }
                    static MyCollection<KeyValuePair<int?, object>> Many2(int k2, string v2, KeyValuePair<int, string> x2, KeyValuePair<int, string>[] y2)
                    {
                        return [k2:v2, x2, ..y2];
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: [refA],
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (15,17): error CS9500: Collection expression type 'MyCollection<KeyValuePair<int, string>>' does not support key-value pair elements.
                    //         return [k1:v1, x1, ..y1];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k1:v1").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int, string>>").WithLocation(15, 17),
                    // (15,19): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         return [k1:v1, x1, ..y1];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(15, 19),
                    // (19,17): error CS9500: Collection expression type 'MyCollection<KeyValuePair<int?, object>>' does not support key-value pair elements.
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k2:v2").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>").WithLocation(19, 17),
                    // (19,19): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(19, 19),
                    // (19,24): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(19, 24),
                    // (19,30): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "y2").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(19, 30));
            }
            else if (!includeAddT)
            {
                comp.VerifyEmitDiagnostics(
                    // (19,17): error CS1950: The best overloaded Add method 'MyCollection<KeyValuePair<int?, object>>.Add(KeyValuePair<int, string>)' for the collection initializer has some invalid arguments
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "k2:v2").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add(System.Collections.Generic.KeyValuePair<int, string>)").WithLocation(19, 17),
                    // (19,17): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int?, object>' to 'System.Collections.Generic.KeyValuePair<int, string>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_BadArgType, "k2:v2").WithArguments("1", "System.Collections.Generic.KeyValuePair<int?, object>", "System.Collections.Generic.KeyValuePair<int, string>").WithLocation(19, 17),
                    // (19,24): error CS1950: The best overloaded Add method 'MyCollection<KeyValuePair<int?, object>>.Add(KeyValuePair<int, string>)' for the collection initializer has some invalid arguments
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "x2").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add(System.Collections.Generic.KeyValuePair<int, string>)").WithLocation(19, 24),
                    // (19,24): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int?, object>' to 'System.Collections.Generic.KeyValuePair<int, string>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_BadArgType, "x2").WithArguments("1", "System.Collections.Generic.KeyValuePair<int?, object>", "System.Collections.Generic.KeyValuePair<int, string>").WithLocation(19, 24),
                    // (19,30): error CS1950: The best overloaded Add method 'MyCollection<KeyValuePair<int?, object>>.Add(KeyValuePair<int, string>)' for the collection initializer has some invalid arguments
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "y2").WithArguments("MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add(System.Collections.Generic.KeyValuePair<int, string>)").WithLocation(19, 30),
                    // (19,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int?, object>' to 'System.Collections.Generic.KeyValuePair<int, string>'
                    //         return [k2:v2, x2, ..y2];
                    Diagnostic(ErrorCode.ERR_BadArgType, "y2").WithArguments("1", "System.Collections.Generic.KeyValuePair<int?, object>", "System.Collections.Generic.KeyValuePair<int, string>").WithLocation(19, 30));
            }
            else
            {
                var verifier = CompileAndVerify(comp, expectedOutput: "[[1, one], [2, two], [3, three]], [[1, one], [2, two], [3, three]], ");
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("Program.Many2", """
                    {
                      // Code size      115 (0x73)
                      .maxstack  3
                      .locals init (MyCollection<System.Collections.Generic.KeyValuePair<int?, object>> V_0,
                                    System.Collections.Generic.KeyValuePair<int, string> V_1,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<int, string> V_4)
                      IL_0000:  newobj     "MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldloc.0
                      IL_0007:  ldarg.0
                      IL_0008:  newobj     "int?..ctor(int)"
                      IL_000d:  ldarg.1
                      IL_000e:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_0013:  callvirt   "void MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add(System.Collections.Generic.KeyValuePair<int?, object>)"
                      IL_0018:  ldloc.0
                      IL_0019:  ldarg.2
                      IL_001a:  stloc.1
                      IL_001b:  ldloca.s   V_1
                      IL_001d:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0022:  newobj     "int?..ctor(int)"
                      IL_0027:  ldloca.s   V_1
                      IL_0029:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_002e:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_0033:  callvirt   "void MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add(System.Collections.Generic.KeyValuePair<int?, object>)"
                      IL_0038:  ldarg.3
                      IL_0039:  stloc.2
                      IL_003a:  ldc.i4.0
                      IL_003b:  stloc.3
                      IL_003c:  br.s       IL_006b
                      IL_003e:  ldloc.2
                      IL_003f:  ldloc.3
                      IL_0040:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0045:  stloc.1
                      IL_0046:  ldloc.0
                      IL_0047:  ldloc.1
                      IL_0048:  stloc.s    V_4
                      IL_004a:  ldloca.s   V_4
                      IL_004c:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0051:  newobj     "int?..ctor(int)"
                      IL_0056:  ldloca.s   V_4
                      IL_0058:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_005d:  newobj     "System.Collections.Generic.KeyValuePair<int?, object>..ctor(int?, object)"
                      IL_0062:  callvirt   "void MyCollection<System.Collections.Generic.KeyValuePair<int?, object>>.Add(System.Collections.Generic.KeyValuePair<int?, object>)"
                      IL_0067:  ldloc.3
                      IL_0068:  ldc.i4.1
                      IL_0069:  add
                      IL_006a:  stloc.3
                      IL_006b:  ldloc.3
                      IL_006c:  ldloc.2
                      IL_006d:  ldlen
                      IL_006e:  conv.i4
                      IL_006f:  blt.s      IL_003e
                      IL_0071:  ldloc.0
                      IL_0072:  ret
                    }
                    """);
            }

            string sourceB2 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        Params<int, string>();
                        Params<int?, object>();
                        Params<int, string>(x);
                        Params<int?, object>(x);
                    }
                    static MyCollection<KeyValuePair<K, V>> Params<K, V>(params MyCollection<KeyValuePair<K, V>> args) => args;
                }
                """;
            comp = CreateCompilation(
                sourceB2,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: [refA],
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (10,30): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'System.Collections.Generic.KeyValuePair<int?, object>'
                //         Params<int?, object>(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int?, object>").WithLocation(10, 30));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp12, "IEnumerable<KeyValuePair<int, string>>")]
        [InlineData(LanguageVersion.Preview, "IEnumerable<KeyValuePair<int, string>>")]
        [InlineData(LanguageVersion.Preview, "IDictionary<int, string>")]
        [InlineData(LanguageVersion.Preview, "Dictionary<int, string>")]
        public void KeyValuePairConversions_ConversionFromExpression(LanguageVersion languageVersion, string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        {{typeName}} c;
                        c = [default];
                        c.Report();
                        c = [new()];
                        c.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                expectedOutput: "[0:null], [0:null], ");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_NotKeyValuePair()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static Dictionary<K, V> FromExpression<K, V>(K k) => [k];
                    static Dictionary<K, V> FromSpread<K, V>(IEnumerable<V> e) => [..e];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,59): error CS0029: Cannot implicitly convert type 'K' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //     static Dictionary<K, V> FromExpression<K, V>(K k) => [k];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "k").WithArguments("K", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(4, 59),
                // (5,70): error CS0029: Cannot implicitly convert type 'V' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //     static Dictionary<K, V> FromSpread<K, V>(IEnumerable<V> e) => [..e];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("V", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(5, 70));
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void KeyValuePairConversions_Dynamic_01(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        KeyValuePair<string, int> x = new("two", 2);
                        IEnumerable<object> y = [new KeyValuePair<string, int>("three", 3)];
                        FromExpression<string, int>(x).Report();
                        FromSpread<string, int>(y).Report();
                    }
                    static List<KeyValuePair<K, V>> FromExpression<K, V>(dynamic d) => [d];
                    static List<KeyValuePair<K, V>> FromSpread<K, V>(IEnumerable<dynamic> e) => [..e];
                }
                """;
            var comp = CreateCompilation(
                [source, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net80);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                var verifier = CompileAndVerify(
                    comp,
                    verify: Verification.Skipped,
                    expectedOutput: IncludeExpectedOutput("[two:2], [three:3], "));
                verifier.VerifyDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp12, "IEnumerable<KeyValuePair<K, V>>")]
        [InlineData(LanguageVersion.Preview, "IEnumerable<KeyValuePair<K, V>>")]
        [InlineData(LanguageVersion.Preview, "IDictionary<K, V>")]
        [InlineData(LanguageVersion.Preview, "Dictionary<K, V>")]
        public void KeyValuePairConversions_Dynamic_02(LanguageVersion languageVersion, string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>(3, "three");
                        FromExpression<int, string>(x).Report();
                        FromSpread<int, string>(new dynamic[] { y }).Report();
                    }
                    static {{typeName}} FromExpression<K, V>(dynamic d) => [d];
                    static {{typeName}} FromSpread<K, V>(IEnumerable<dynamic> e) => [..e];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[2:two], [3:three], "));
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_Dynamic_03()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        FromPair1<string, int>("one", 1).Report();
                        FromPair2<string, int>("two", 2).Report();
                    }
                    static Dictionary<K, V> FromPair1<K, V>(K k, dynamic d) => [k:d];
                    static Dictionary<K, V> FromPair2<K, V>(dynamic d, V v) => [d:v];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[one:1], [two:2], "));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.FromPair1<K, V>", """
                {
                  // Code size       77 (0x4d)
                  .maxstack  6
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldarg.0
                  IL_0007:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>> Program.<>o__1<K, V>.<>p__0"
                  IL_000c:  brtrue.s   IL_0032
                  IL_000e:  ldc.i4.0
                  IL_000f:  ldtoken    "V"
                  IL_0014:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0019:  ldtoken    "Program"
                  IL_001e:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0023:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)"
                  IL_0028:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                  IL_002d:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>> Program.<>o__1<K, V>.<>p__0"
                  IL_0032:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>> Program.<>o__1<K, V>.<>p__0"
                  IL_0037:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>>.Target"
                  IL_003c:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>> Program.<>o__1<K, V>.<>p__0"
                  IL_0041:  ldarg.1
                  IL_0042:  callvirt   "V System.Func<System.Runtime.CompilerServices.CallSite, dynamic, V>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                  IL_0047:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_004c:  ret
                }
                """);
            verifier.VerifyIL("Program.FromPair2<K, V>", """
                {
                  // Code size       77 (0x4d)
                  .maxstack  5
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>> Program.<>o__2<K, V>.<>p__0"
                  IL_000b:  brtrue.s   IL_0031
                  IL_000d:  ldc.i4.0
                  IL_000e:  ldtoken    "K"
                  IL_0013:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0018:  ldtoken    "Program"
                  IL_001d:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0022:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)"
                  IL_0027:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                  IL_002c:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>> Program.<>o__2<K, V>.<>p__0"
                  IL_0031:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>> Program.<>o__2<K, V>.<>p__0"
                  IL_0036:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>>.Target"
                  IL_003b:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>> Program.<>o__2<K, V>.<>p__0"
                  IL_0040:  ldarg.0
                  IL_0041:  callvirt   "K System.Func<System.Runtime.CompilerServices.CallSite, dynamic, K>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                  IL_0046:  ldarg.1
                  IL_0047:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_004c:  ret
                }
                """);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp12, "IEnumerable<KeyValuePair<K, V>>")]
        [InlineData(LanguageVersion.Preview, "IEnumerable<KeyValuePair<K, V>>")]
        [InlineData(LanguageVersion.Preview, "IDictionary<K, V>")]
        [InlineData(LanguageVersion.Preview, "Dictionary<K, V>")]
        public void KeyValuePairConversions_Dynamic_04(LanguageVersion languageVersion, string typeName)
        {
            string source = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        FromSpread1<int, string>(y);
                        FromSpread2<int, string>(y);
                    }
                    static {{typeName}}
                        FromSpread1<K, V>(dynamic e) => [..e];
                    static {{typeName}}
                        FromSpread2<K, V>(IEnumerable e) => [..e];
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (12,44): error CS0029: Cannot implicitly convert type 'object' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //         FromSpread1<K, V>(dynamic e) => [..e];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("object", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(12, 44),
                // (14,48): error CS0029: Cannot implicitly convert type 'object' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //         FromSpread2<K, V>(IEnumerable e) => [..e];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("object", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(14, 48));
        }

        [Fact]
        public void KeyValuePairConversions_Dynamic_MultipleIndexers()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key]
                    {
                        get { return _d[key]; }
                        set { Console.WriteLine("{0}, {1}, {2}, {3}", typeof(K).Name, typeof(V).Name, key, value); _d[key] = value; }
                    }
                    public int this[string key]
                    {
                        get { return (int)(object)_d[(K)(object)key]; }
                        set { Console.WriteLine("string, int, {0}, {1}", key, value); _d[(K)(object)key] = (V)(object)value; }
                    }
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        FromPair1A("one", 1);
                        FromPair2A("two", 2);
                        FromPair1B("one", 1);
                        FromPair2B("two", 2);
                    }
                    static MyDictionary<object, object> FromPair1A(string k, dynamic v) => [k:v];
                    static MyDictionary<object, object> FromPair2A(dynamic k, int v) => [k:v];
                    static MyDictionary<object, object> FromPair1B(string k, dynamic v) { var d = new MyDictionary<object, object>(); d[k] = v; return d; }
                    static MyDictionary<object, object> FromPair2B(dynamic k, int v) { var d = new MyDictionary<object, object>(); d[k] = v; return d; }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("""
                    Object, Object, one, 1
                    Object, Object, two, 2
                    string, int, one, 1
                    string, int, two, 2
                    """));
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_MultipleIndexers_UnrelatedTypes()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key]
                    {
                        get { return _d[key]; }
                        set { Console.WriteLine("{0}, {1}, {2}, {3}", typeof(K).Name, typeof(V).Name, key, value); _d[key] = value; }
                    }
                    public V this[int key]
                    {
                        get { return default; }
                        set { Console.WriteLine("int, {0}, {1}, {2}", typeof(V).Name, key, value); }
                    }
                }
                """;
            string sourceB = """
                class A
                {
                    public static implicit operator uint?(A a) => 1;
                    public static implicit operator int(A a) => 2;
                }
                class Program
                {
                    static void Main()
                    {
                        FromPair1(new A(), "one");
                        FromPair2(new A(), "two");
                    }
                    static MyDictionary<uint?, object> FromPair1(A k, object v) => [k:v];
                    static MyDictionary<uint?, object> FromPair2(A k, object v) { var d = new MyDictionary<uint?, object>(); d[k] = v; return d; }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                verify: Verification.Skipped,
                expectedOutput: """
                    Nullable`1, Object, 1, one
                    int, Object, 2, two
                    """);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_KeyValuePairConstraint()
        {
            string source = """
                using System.Collections.Generic;
                abstract class A<T>
                {
                    public abstract void F<U>(U u, IEnumerable<U> e) where U : T;
                }
                class B<K, V> : A<KeyValuePair<K, V>>
                {
                    public override void F<U>(U u, IEnumerable<U> e)
                    {
                        Dictionary<K, V> d;
                        d = [u];
                        d = [..e];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (11,14): error CS0029: Cannot implicitly convert type 'U' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //         d = [u];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "u").WithArguments("U", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(11, 14),
                // (12,16): error CS0029: Cannot implicitly convert type 'U' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //         d = [..e];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("U", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(12, 16));
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void KeyValuePairConversions_LanguageVersion(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        FromExpression(new KeyValuePair<string, int>("one", 1)).Report();
                        FromSpread([new KeyValuePair<string, int>("two", 2)]).Report();
                    }
                    static KeyValuePair<K, V>[] FromExpression<K, V>(KeyValuePair<K, V> e) => [e];
                    static KeyValuePair<K, V>[] FromSpread<K, V>(IEnumerable<KeyValuePair<K, V>> s) => [..s];
                }
                """;
            var comp = CreateCompilation(
                [source, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[one:1], [two:2], ");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.FromExpression<K, V>", """
                {
                    // Code size       15 (0xf)
                    .maxstack  4
                    IL_0000:  ldc.i4.1
                    IL_0001:  newarr     "System.Collections.Generic.KeyValuePair<K, V>"
                    IL_0006:  dup
                    IL_0007:  ldc.i4.0
                    IL_0008:  ldarg.0
                    IL_0009:  stelem     "System.Collections.Generic.KeyValuePair<K, V>"
                    IL_000e:  ret
                }
                """);
            verifier.VerifyIL("Program.FromSpread<K, V>", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  call       "System.Collections.Generic.KeyValuePair<K, V>[] System.Linq.Enumerable.ToArray<System.Collections.Generic.KeyValuePair<K, V>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>)"
                  IL_0006:  ret
                }
                """);
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
                    [1:one, 2:two, 3:three], 
                    """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size      150 (0x96)
                  .maxstack  5
                  .locals init (System.Collections.Generic.Dictionary<int, string> V_0,
                                System.Collections.Generic.KeyValuePair<int, string> V_1,
                                System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                int V_3,
                                System.Collections.Generic.KeyValuePair<int, string> V_4)
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
                  IL_0026:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                  IL_002b:  ldloc.0
                  IL_002c:  ldc.i4.2
                  IL_002d:  call       "int Program.Identity<int>(int)"
                  IL_0032:  ldstr      "two"
                  IL_0037:  call       "string Program.Identity<string>(string)"
                  IL_003c:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                  IL_0041:  ldc.i4.1
                  IL_0042:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0047:  dup
                  IL_0048:  ldc.i4.0
                  IL_0049:  ldc.i4.3
                  IL_004a:  ldstr      "three"
                  IL_004f:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                  IL_0054:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0059:  call       "System.Collections.Generic.KeyValuePair<int, string>[] Program.Identity<System.Collections.Generic.KeyValuePair<int, string>[]>(System.Collections.Generic.KeyValuePair<int, string>[])"
                  IL_005e:  stloc.2
                  IL_005f:  ldc.i4.0
                  IL_0060:  stloc.3
                  IL_0061:  br.s       IL_0084
                  IL_0063:  ldloc.2
                  IL_0064:  ldloc.3
                  IL_0065:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_006a:  stloc.s    V_4
                  IL_006c:  ldloc.0
                  IL_006d:  ldloca.s   V_4
                  IL_006f:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_0074:  ldloca.s   V_4
                  IL_0076:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_007b:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                  IL_0080:  ldloc.3
                  IL_0081:  ldc.i4.1
                  IL_0082:  add
                  IL_0083:  stloc.3
                  IL_0084:  ldloc.3
                  IL_0085:  ldloc.2
                  IL_0086:  ldlen
                  IL_0087:  conv.i4
                  IL_0088:  blt.s      IL_0063
                  IL_008a:  ldloc.0
                  IL_008b:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<int, string>..ctor(System.Collections.Generic.IDictionary<int, string>)"
                  IL_0090:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_0095:  ret
                }
                """);
        }

        [Fact]
        public void EvaluationOrder_02()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var d = F(101, "A", 102, "B", 103, "C", true, 3);
                        d.Report();
                    }
                    static IReadOnlyDictionary<K, V> F<K, V>(K k1, V v1, K k2, V v2, K k3, V v3, bool b, int i)
                    {
                        return [
                            Identity(Identity(b) ? Identity(k1) : Identity(k2)) : Identity(v2),
                            Identity(k3) : Identity(Identity(i) switch { 1 => Identity(v1), 2 => Identity(v2), _ => Identity(v3) })];
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
                    True
                    101
                    101
                    B
                    103
                    3
                    C
                    C
                    [101:B, 103:C], 
                    """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F<K, V>", """
                {
                  // Code size      124 (0x7c)
                  .maxstack  3
                  .locals init (System.Collections.Generic.Dictionary<K, V> V_0,
                                K V_1,
                                V V_2,
                                int V_3,
                                System.Collections.Generic.Dictionary<K, V> V_4)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  stloc.s    V_4
                  IL_0007:  ldloc.s    V_4
                  IL_0009:  ldarg.s    V_6
                  IL_000b:  call       "bool Program.Identity<bool>(bool)"
                  IL_0010:  brtrue.s   IL_001a
                  IL_0012:  ldarg.2
                  IL_0013:  call       "K Program.Identity<K>(K)"
                  IL_0018:  br.s       IL_0020
                  IL_001a:  ldarg.0
                  IL_001b:  call       "K Program.Identity<K>(K)"
                  IL_0020:  call       "K Program.Identity<K>(K)"
                  IL_0025:  ldarg.3
                  IL_0026:  call       "V Program.Identity<V>(V)"
                  IL_002b:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0030:  ldloc.s    V_4
                  IL_0032:  stloc.0
                  IL_0033:  ldarg.s    V_4
                  IL_0035:  call       "K Program.Identity<K>(K)"
                  IL_003a:  stloc.1
                  IL_003b:  ldarg.s    V_7
                  IL_003d:  call       "int Program.Identity<int>(int)"
                  IL_0042:  stloc.3
                  IL_0043:  ldloc.3
                  IL_0044:  ldc.i4.1
                  IL_0045:  beq.s      IL_004d
                  IL_0047:  ldloc.3
                  IL_0048:  ldc.i4.2
                  IL_0049:  beq.s      IL_0056
                  IL_004b:  br.s       IL_005f
                  IL_004d:  ldarg.1
                  IL_004e:  call       "V Program.Identity<V>(V)"
                  IL_0053:  stloc.2
                  IL_0054:  br.s       IL_0067
                  IL_0056:  ldarg.3
                  IL_0057:  call       "V Program.Identity<V>(V)"
                  IL_005c:  stloc.2
                  IL_005d:  br.s       IL_0067
                  IL_005f:  ldarg.s    V_5
                  IL_0061:  call       "V Program.Identity<V>(V)"
                  IL_0066:  stloc.2
                  IL_0067:  ldloc.0
                  IL_0068:  ldloc.1
                  IL_0069:  ldloc.2
                  IL_006a:  call       "V Program.Identity<V>(V)"
                  IL_006f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0074:  ldloc.s    V_4
                  IL_0076:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_007b:  ret
                }
                """);
        }

        [Fact]
        public void EvaluationOrder_03()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class MyKeyValuePair<K, V>
                {
                    public MyKeyValuePair(K key, V value)
                    {
                        Key = key;
                        Value = value;
                    }
                    public readonly K Key;
                    public readonly V Value;
                    public static implicit operator KeyValuePair<K, V>(MyKeyValuePair<K, V> kvp)
                    {
                        Console.WriteLine("conversion to MyKeyValuePair<{0}, {1}>: {2}, {3}", typeof(K).Name, typeof(V).Name, kvp.Key, kvp.Value);
                        return new(kvp.Key, kvp.Value);
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        var x = new MyKeyValuePair<int, string>(1, "one");
                        var y = new MyKeyValuePair<int, string>[] { new(2, "two"), new(3, "three") };
                        F(x, y).Report();
                    }
                    static IDictionary<K, V> F<K, V>(MyKeyValuePair<K, V> x, IEnumerable<MyKeyValuePair<K, V>> y)
                    {
                        return [Identity(x), ..Identity(y)];
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
                    MyKeyValuePair`2[System.Int32,System.String]
                    conversion to MyKeyValuePair<Int32, String>: 1, one
                    MyKeyValuePair`2[System.Int32,System.String][]
                    conversion to MyKeyValuePair<Int32, String>: 2, two
                    conversion to MyKeyValuePair<Int32, String>: 3, three
                    [1:one, 2:two, 3:three],
                    """);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void EvaluationOrder_04()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                class A<T>
                {
                    public readonly T Value;
                    public A(T value) { Value = value; }
                    public static implicit operator A<T>(T value)
                    {
                        Console.WriteLine("conversion to A<{0}>: {1}", typeof(T).Name, value);
                        return new(value);
                    }
                    public override string ToString() => Value.ToString();
                    public class Comparer : IEqualityComparer<A<T>>
                    {
                        public Comparer()
                        {
                            Console.WriteLine("new Comparer<{0}>()", typeof(T).Name);
                        }
                        public bool Equals(A<T> x, A<T> y) => object.Equals(x.Value, y.Value);
                        public int GetHashCode(A<T> a) => a.Value.GetHashCode();
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(1, "one");
                        var y = new KeyValuePair<int, string>[] { new(2, "two"), new(3, "three") };
                        var d = F(x, y);
                        d.Select(kvp => new KeyValuePair<int, string>(kvp.Key.Value, kvp.Value.Value)).Report();
                    }
                    static Dictionary<A<K>, A<V>> F<K, V>(KeyValuePair<K, V> x, IEnumerable<KeyValuePair<K, V>> y)
                    {
                        return [with(comparer: new A<K>.Comparer()), Identity(x), ..Identity(y)];
                    }
                    static T Identity<T>(T value)
                    {
                        Console.WriteLine(value);
                        return value;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                expectedOutput: """
                    new Comparer<Int32>()
                    [1, one]
                    conversion to A<Int32>: 1
                    conversion to A<String>: one
                    System.Collections.Generic.KeyValuePair`2[System.Int32,System.String][]
                    conversion to A<Int32>: 2
                    conversion to A<String>: two
                    conversion to A<Int32>: 3
                    conversion to A<String>: three
                    [[1, one], [2, two], [3, three]], 
                    """);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void InferredType_ExpressionElement()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d = [new()];
                d.Report();
                d = [default];
                d.Report();
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[0:null], [0:null], ");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var elements = tree.GetRoot().DescendantNodes().OfType<ExpressionElementSyntax>().ToArray();

            var typeInfo = model.GetTypeInfo(elements[0].Expression);
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.ConvertedType.ToTestDisplayString());

            typeInfo = model.GetTypeInfo(elements[1].Expression);
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void InferredType_KeyValueElement_01()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d = [default:default];
                d.Report();
                d = [new():null];
                d.Report();
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[0:null], [0:null], ");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var elements = tree.GetRoot().DescendantNodes().OfType<KeyValuePairElementSyntax>().ToArray();

            var typeInfo = model.GetTypeInfo(elements[0].KeyExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(elements[0].ValueExpression);
            Assert.Equal(SpecialType.System_String, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_String, typeInfo.ConvertedType.SpecialType);

            typeInfo = model.GetTypeInfo(elements[1].KeyExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(elements[1].ValueExpression);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_String, typeInfo.ConvertedType.SpecialType);
        }

        [Fact]
        public void InferredType_KeyValueElement_02()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d = [null:new()];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var elements = tree.GetRoot().DescendantNodes().OfType<KeyValuePairElementSyntax>().ToArray();

            var typeInfo = model.GetTypeInfo(elements[0].KeyExpression);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_String, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(elements[0].ValueExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, typeInfo.ConvertedType.SpecialType);
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

        [Theory]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        [InlineData("Dictionary")]
        public void TypeInference(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Identity([default:default]);
                        Identity([default:"2"]);
                        Identity([1:default]);
                        Identity([1:default, default:"2"]);
                    }
                    static {{typeName}}<K, V> Identity<K, V>({{typeName}}<K, V> d) => d;
                }
                """;
            var comp = CreateCompilation(source);
            // https://github.com/dotnet/roslyn/issues/77873: Type inference should succeed for Identity([1:default, default:"2"]);.
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([default:default]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(6, 9),
                // (7,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([default:"2"]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(7, 9),
                // (8,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([1:default]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(8, 9),
                // (9,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([1:default, default:"2"]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(9, 9));
        }

        // Type inference should not walk into KeyValuePair<A, B> when the KeyValuePair<A, B>
        // is substituted type for T rather than substituted for KeyValuePair<K, V>.
        [Theory]
        [InlineData("System.Collections.Generic.IEnumerable<T>")]
        [InlineData("T[]")]
        [InlineData("System.Collections.Generic.List<T>")]
        public void TypeInference_KeyValuePairConversions(string typeName)
        {
            string sourceA1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        (int, string) x = (1, "one");
                        (int?, object) y = (2, "two");
                        Identity([x, y]);
                        Identity([y, x]);
                    }
                    static {{typeName}} Identity<T>({{typeName}} c)
                    {
                        c.Report();
                        return c;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA1, s_collectionExtensions],
                expectedOutput: "[(1, one), (2, two)], [(2, two), (1, one)], ");
            verifier.VerifyDiagnostics();

            string sourceB1 = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(1, "one");
                        var y = new KeyValuePair<int?, object>(2, "two");
                        Identity([x, y]);
                        Identity([y, x]);
                    }
                    static {{typeName}} Identity<T>({{typeName}} c)
                    {
                        c.Report();
                        return c;
                    }
                }
                """;
            var comp = CreateCompilation([sourceB1, s_collectionExtensions]);
            comp.VerifyEmitDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'Program.Identity<T>(IEnumerable<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([x, y]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<T>({typeName})").WithLocation(8, 9),
                // (9,9): error CS0411: The type arguments for method 'Program.Identity<T>(IEnumerable<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([y, x]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<T>({typeName})").WithLocation(9, 9));

            string sourceA2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        (int, string) x = (1, "one");
                        (int?, object) y = (2, "two");
                        Identity(x, y);
                        Identity(y, x);
                    }
                    static {{typeName}} Identity<T>(params {{typeName}} c)
                    {
                        c.Report();
                        return c;
                    }
                }
                """;
            verifier = CompileAndVerify(
                [sourceA2, s_collectionExtensions],
                expectedOutput: "[(1, one), (2, two)], [(2, two), (1, one)], ");
            verifier.VerifyDiagnostics();

            string sourceB2 = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(1, "one");
                        var y = new KeyValuePair<int?, object>(2, "two");
                        Identity(x, y);
                        Identity(y, x);
                    }
                    static {{typeName}} Identity<T>(params {{typeName}} c)
                    {
                        c.Report();
                        return c;
                    }
                }
                """;
            comp = CreateCompilation([sourceB2, s_collectionExtensions]);
            comp.VerifyEmitDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'Program.Identity<T>(params IEnumerable<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity(x, y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<T>(params {typeName})").WithLocation(8, 9),
                // (9,9): error CS0411: The type arguments for method 'Program.Identity<T>(params IEnumerable<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity(y, x);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<T>(params {typeName})").WithLocation(9, 9));
        }

        [Theory]
        [CombinatorialData]
        public void CustomDictionary_01([CombinatorialValues("class", "struct")] string typeKind, bool useCompilationReference)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                public {{typeKind}} MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _d;
                    public MyDictionary(IEqualityComparer<K> comparer = null) { _d = new(comparer); }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => GetDictionary().GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key]
                    {
                        get { return GetDictionary()[key]; }
                        set { GetDictionary()[key] = value; }
                    }
                    private Dictionary<K, V> GetDictionary() => _d ??= new();
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Empty<string, int>().Report();
                        Many(1, "one", new KeyValuePair<int, string>(2, "two"), new KeyValuePair<int, string>[] { new(3, "three") }).Report();
                        WithComparer(StringComparer.OrdinalIgnoreCase, "ABC", 1, "ab", 2).Report();
                    }
                    static MyDictionary<K, V> Empty<K, V>() => [];
                    static MyDictionary<K, V> Many<K, V>(K k, V v, KeyValuePair<K, V> e, KeyValuePair<K, V>[] s) => /*<bind>*/[with(null), k:v, e, ..s]/*</bind>*/;
                    static MyDictionary<K, V> WithComparer<K, V>(IEqualityComparer<K> comparer, K k1, V v1, K k2, V v2) => [with(comparer), k1:v1, k2:v2];
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, s_dictionaryExtensions],
                references: [refA],
                expectedOutput: "[], [1:one, 2:two, 3:three], [ab:2, ABC:1], ");
            verifier.VerifyDiagnostics();
            if (typeKind == "class")
            {
                verifier.VerifyIL("Program.Empty<K, V>", """
                    {
                      // Code size        7 (0x7)
                      .maxstack  1
                      IL_0000:  ldnull
                      IL_0001:  newobj     "MyDictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                      IL_0006:  ret
                    }
                    """);
                verifier.VerifyIL("Program.Many<K, V>", """
                    {
                      // Code size       84 (0x54)
                      .maxstack  3
                      .locals init (MyDictionary<K, V> V_0,
                                    System.Collections.Generic.KeyValuePair<K, V> V_1,
                                    System.Collections.Generic.KeyValuePair<K, V>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4)
                      IL_0000:  ldnull
                      IL_0001:  newobj     "MyDictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                      IL_0006:  stloc.0
                      IL_0007:  ldloc.0
                      IL_0008:  ldarg.0
                      IL_0009:  ldarg.1
                      IL_000a:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_000f:  ldarg.2
                      IL_0010:  stloc.1
                      IL_0011:  ldloc.0
                      IL_0012:  ldloca.s   V_1
                      IL_0014:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0019:  ldloca.s   V_1
                      IL_001b:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0020:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_0025:  ldarg.3
                      IL_0026:  stloc.2
                      IL_0027:  ldc.i4.0
                      IL_0028:  stloc.3
                      IL_0029:  br.s       IL_004c
                      IL_002b:  ldloc.2
                      IL_002c:  ldloc.3
                      IL_002d:  ldelem     "System.Collections.Generic.KeyValuePair<K, V>"
                      IL_0032:  stloc.s    V_4
                      IL_0034:  ldloc.0
                      IL_0035:  ldloca.s   V_4
                      IL_0037:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_003c:  ldloca.s   V_4
                      IL_003e:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0043:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_0048:  ldloc.3
                      IL_0049:  ldc.i4.1
                      IL_004a:  add
                      IL_004b:  stloc.3
                      IL_004c:  ldloc.3
                      IL_004d:  ldloc.2
                      IL_004e:  ldlen
                      IL_004f:  conv.i4
                      IL_0050:  blt.s      IL_002b
                      IL_0052:  ldloc.0
                      IL_0053:  ret
                    }
                    """);
                verifier.VerifyIL("Program.WithComparer<K, V>", """
                    {
                      // Code size       24 (0x18)
                      .maxstack  4
                      IL_0000:  ldarg.0
                      IL_0001:  newobj     "MyDictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                      IL_0006:  dup
                      IL_0007:  ldarg.1
                      IL_0008:  ldarg.2
                      IL_0009:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_000e:  dup
                      IL_000f:  ldarg.3
                      IL_0010:  ldarg.s    V_4
                      IL_0012:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_0017:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.Empty<K, V>", """
                    {
                      // Code size       10 (0xa)
                      .maxstack  1
                      .locals init (MyDictionary<K, V> V_0)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  initobj    "MyDictionary<K, V>"
                      IL_0008:  ldloc.0
                      IL_0009:  ret
                    }
                    """);
                verifier.VerifyIL("Program.Many<K, V>", """
                    {
                      // Code size       88 (0x58)
                      .maxstack  3
                      .locals init (MyDictionary<K, V> V_0,
                                    System.Collections.Generic.KeyValuePair<K, V> V_1,
                                    System.Collections.Generic.KeyValuePair<K, V>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  ldnull
                      IL_0003:  call       "MyDictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                      IL_0008:  ldloca.s   V_0
                      IL_000a:  ldarg.0
                      IL_000b:  ldarg.1
                      IL_000c:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_0011:  ldarg.2
                      IL_0012:  stloc.1
                      IL_0013:  ldloca.s   V_0
                      IL_0015:  ldloca.s   V_1
                      IL_0017:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_001c:  ldloca.s   V_1
                      IL_001e:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0023:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_0028:  ldarg.3
                      IL_0029:  stloc.2
                      IL_002a:  ldc.i4.0
                      IL_002b:  stloc.3
                      IL_002c:  br.s       IL_0050
                      IL_002e:  ldloc.2
                      IL_002f:  ldloc.3
                      IL_0030:  ldelem     "System.Collections.Generic.KeyValuePair<K, V>"
                      IL_0035:  stloc.s    V_4
                      IL_0037:  ldloca.s   V_0
                      IL_0039:  ldloca.s   V_4
                      IL_003b:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0040:  ldloca.s   V_4
                      IL_0042:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0047:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_004c:  ldloc.3
                      IL_004d:  ldc.i4.1
                      IL_004e:  add
                      IL_004f:  stloc.3
                      IL_0050:  ldloc.3
                      IL_0051:  ldloc.2
                      IL_0052:  ldlen
                      IL_0053:  conv.i4
                      IL_0054:  blt.s      IL_002e
                      IL_0056:  ldloc.0
                      IL_0057:  ret
                    }
                    """);
                verifier.VerifyIL("Program.WithComparer<K, V>", """
                    {
                      // Code size       29 (0x1d)
                      .maxstack  3
                      .locals init (MyDictionary<K, V> V_0)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  ldarg.0
                      IL_0003:  call       "MyDictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                      IL_0008:  ldloca.s   V_0
                      IL_000a:  ldarg.1
                      IL_000b:  ldarg.2
                      IL_000c:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_0011:  ldloca.s   V_0
                      IL_0013:  ldarg.3
                      IL_0014:  ldarg.s    V_4
                      IL_0016:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_001b:  ldloc.0
                      IL_001c:  ret
                    }
                    """);
            }

            comp = (CSharpCompilation)verifier.Compilation;
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                ICollectionExpressionOperation (3 elements, ConstructMethod: MyDictionary<K, V>..ctor([System.Collections.Generic.IEqualityComparer<K> comparer = null])) (OperationKind.CollectionExpression, Type: MyDictionary<K, V>) (Syntax: '[with(null) ... :v, e, ..s]')
                  Elements(3):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'k:v')
                      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<K, V>) (Syntax: 'e')
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<K, V>) (OperationKind.Spread, Type: null) (Syntax: '..s')
                        Operand:
                          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<K, V>[]) (Syntax: 's')
                        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                """);
        }

        [Fact]
        public void CustomDictionary_NoParameterlessConstructor()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private MyDictionary() { }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key] { get { return default; } set { } }
                }
                class Program
                {
                    static MyDictionary<K, V> OnePair<K, V>() => [default:default];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,50): error CS0122: 'MyDictionary<K, V>.MyDictionary()' is inaccessible due to its protection level
                //     static MyDictionary<K, V> OnePair<K, V>() => [default:default];
                Diagnostic(ErrorCode.ERR_BadAccess, "[default:default]").WithArguments("MyDictionary<K, V>.MyDictionary()").WithLocation(12, 50),
                // (12,51): error CS8716: There is no target type for the default literal.
                //     static MyDictionary<K, V> OnePair<K, V>() => [default:default];
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(12, 51),
                // (12,59): error CS8716: There is no target type for the default literal.
                //     static MyDictionary<K, V> OnePair<K, V>() => [default:default];
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(12, 59));
        }

        [Fact]
        public void CustomDictionary_Params_LessAccessibleConstructor()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                public class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    internal MyDictionary() { }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key] { get { return default; } set { } }
                }
                """;
            string sourceB = """
                public class Program
                {
                    public static void Main()
                    {
                        var f1 = (params MyDictionary<string, int> args) => { };
                        static void f2<K, V>(params MyDictionary<K, V> args) { }
                        f1();
                        f2<string, int>();
                    }
                    public static void F3<K, V>(params MyDictionary<K, V> args) { }
                    internal static void F4<K, V>(params MyDictionary<K, V> args) { }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (10,33): error CS9224: Method 'MyDictionary<K, V>.MyDictionary()' cannot be less visible than the member with params collection 'Program.F3<K, V>(params MyDictionary<K, V>)'.
                //     public static void F3<K, V>(params MyDictionary<K, V> args) { }
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>.MyDictionary()", "Program.F3<K, V>(params MyDictionary<K, V>)").WithLocation(10, 33));
        }

        [Fact]
        public void CustomDictionary_Params_ProtectedConstructor()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                internal class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    protected MyDictionary() { }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key] { get { return default; } set { } }
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        var f1 = (params MyDictionary<string, int> args) => { };
                        static void f2<K, V>(params MyDictionary<K, V> args) { }
                        f1();
                        f2<string, int>();
                    }
                    static void F3<K, V>(params MyDictionary<K, V> args) { }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (5,19): error CS0122: 'MyDictionary<string, int>.MyDictionary()' is inaccessible due to its protection level
                //         var f1 = (params MyDictionary<string, int> args) => { };
                Diagnostic(ErrorCode.ERR_BadAccess, "params MyDictionary<string, int> args").WithArguments("MyDictionary<string, int>.MyDictionary()").WithLocation(5, 19),
                // (6,30): error CS0122: 'MyDictionary<K, V>.MyDictionary()' is inaccessible due to its protection level
                //         static void f2<K, V>(params MyDictionary<K, V> args) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>.MyDictionary()").WithLocation(6, 30),
                // (7,9): error CS7036: There is no argument given that corresponds to the required parameter 'obj' of 'Action<MyDictionary<string, int>>'
                //         f1();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "f1").WithArguments("obj", "System.Action<MyDictionary<string, int>>").WithLocation(7, 9),
                // (8,9): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'f2<K, V>(params MyDictionary<K, V>)'
                //         f2<string, int>();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "f2<string, int>").WithArguments("args", "f2<K, V>(params MyDictionary<K, V>)").WithLocation(8, 9),
                // (10,26): error CS0122: 'MyDictionary<K, V>.MyDictionary()' is inaccessible due to its protection level
                //     static void F3<K, V>(params MyDictionary<K, V> args) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>.MyDictionary()").WithLocation(10, 26));
        }

        [Fact]
        public void Params_Cycle_IncorrectSignature()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                public class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public MyDictionary(params MyDictionary<K, V> args) { }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key, params MyDictionary<K, V> args] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(1, "one");
                        F<int, string>();
                        F(x);
                        F<int, string>(x);
                        F([x]);
                        F<int, string>([2:"two"]);
                    }
                    static void F<K, V>(params MyDictionary<K, V> args) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,25): error CS0117: 'MyDictionary<K, V>' does not contain a definition for 'Add'
                //     public MyDictionary(params MyDictionary<K, V> args) { }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>", "Add").WithLocation(5, 25),
                // (8,26): error CS0117: 'MyDictionary<K, V>' does not contain a definition for 'Add'
                //     public V this[K key, params MyDictionary<K, V> args] { get { return default; } set { } }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>", "Add").WithLocation(8, 26),
                // (15,9): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.F<K, V>(params MyDictionary<K, V>)'
                //         F<int, string>();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "F<int, string>").WithArguments("args", "Program.F<K, V>(params MyDictionary<K, V>)").WithLocation(15, 9),
                // (16,9): error CS0411: The type arguments for method 'Program.F<K, V>(params MyDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(x);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<K, V>(params MyDictionary<K, V>)").WithLocation(16, 9),
                // (17,24): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'params MyDictionary<int, string>'
                //         F<int, string>(x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Collections.Generic.KeyValuePair<int, string>", "params MyDictionary<int, string>").WithLocation(17, 24),
                // (18,11): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         F([x]);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[x]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(18, 11),
                // (19,24): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         F<int, string>([2:"two"]);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"[2:""two""]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(19, 24),
                // (21,25): error CS0117: 'MyDictionary<K, V>' does not contain a definition for 'Add'
                //     static void F<K, V>(params MyDictionary<K, V> args) { }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>", "Add").WithLocation(21, 25));
        }

        [Fact]
        public void Params_Cycle_Overloads()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                public class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public MyDictionary(params MyDictionary<K, V> args) { }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key] { get { return default; } set { } }
                    public V this[K key, params MyDictionary<K, V> args] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(1, "one");
                        F<int, string>();
                        F(x);
                        F<int, string>(x);
                        F([x]);
                        F<int, string>([2:"two"]);
                    }
                    static void F<K, V>(params MyDictionary<K, V> args) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (16,9): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         F<int, string>();
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "F<int, string>()").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(16, 9),
                // (17,9): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         F(x);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "F(x)").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(17, 9),
                // (18,9): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         F<int, string>(x);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "F<int, string>(x)").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(18, 9),
                // (19,11): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         F([x]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[x]").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(19, 11),
                // (20,24): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         F<int, string>([2:"two"]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, @"[2:""two""]").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(20, 24));
        }

        [Fact]
        public void Params_Cycle_ConstructorArguments()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                public class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public MyDictionary() { }
                    public MyDictionary(object arg, params MyDictionary<K, V> args) { }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key, params MyDictionary<K, V> args] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(1, "one");
                        F<int, string>([with(null)]);
                        F([with(null)], x);
                        F<int, string>([with(null)], x);
                        F([with(null)], [x]);
                    }
                    static void F<K, V>(MyDictionary<K, V> d, params MyDictionary<K, V> args) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,37): error CS0117: 'MyDictionary<K, V>' does not contain a definition for 'Add'
                //     public MyDictionary(object arg, params MyDictionary<K, V> args) { }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>", "Add").WithLocation(6, 37),
                // (9,26): error CS0117: 'MyDictionary<K, V>' does not contain a definition for 'Add'
                //     public V this[K key, params MyDictionary<K, V> args] { get { return default; } set { } }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>", "Add").WithLocation(9, 26),
                // (16,9): error CS7036: There is no argument given that corresponds to the required parameter 'args' of 'Program.F<K, V>(MyDictionary<K, V>, params MyDictionary<K, V>)'
                //         F<int, string>([with(null)]);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "F<int, string>").WithArguments("args", "Program.F<K, V>(MyDictionary<K, V>, params MyDictionary<K, V>)").WithLocation(16, 9),
                // (17,9): error CS0411: The type arguments for method 'Program.F<K, V>(MyDictionary<K, V>, params MyDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([with(null)], x);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<K, V>(MyDictionary<K, V>, params MyDictionary<K, V>)").WithLocation(17, 9),
                // (18,24): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         F<int, string>([with(null)], x);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[with(null)]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(18, 24),
                // (18,38): error CS1503: Argument 2: cannot convert from 'System.Collections.Generic.KeyValuePair<int, string>' to 'params MyDictionary<int, string>'
                //         F<int, string>([with(null)], x);
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("2", "System.Collections.Generic.KeyValuePair<int, string>", "params MyDictionary<int, string>").WithLocation(18, 38),
                // (19,11): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         F([with(null)], [x]);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[with(null)]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(19, 11),
                // (19,25): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         F([with(null)], [x]);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[x]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(19, 25),
                // (21,47): error CS0117: 'MyDictionary<K, V>' does not contain a definition for 'Add'
                //     static void F<K, V>(MyDictionary<K, V> d, params MyDictionary<K, V> args) { }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "params MyDictionary<K, V> args").WithArguments("MyDictionary<K, V>", "Add").WithLocation(21, 47));
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void Params_Cycle_AddAndIndexer(LanguageVersion languageVersion)
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                public class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public MyDictionary(params MyDictionary<K, V> args) { }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key, params MyDictionary<K, V> args] { get { return default; } set { } }
                    public void Add(KeyValuePair<K, V> kvp) { }
                }
                public static class Helpers
                {
                    public static void F<K, V>(params MyDictionary<K, V> args) { }
                }
                """;
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular13);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(1, "one");
                        Helpers.F<int, string>();
                        Helpers.F(x);
                        Helpers.F([x]);
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         Helpers.F<int, string>();
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Helpers.F<int, string>()").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(7, 9),
                // (8,9): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         Helpers.F(x);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "Helpers.F(x)").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(8, 9),
                // (9,19): error CS9223: Creation of params collection 'MyDictionary<int, string>' results in an infinite chain of invocation of constructor 'MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)'.
                //         Helpers.F([x]);
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[x]").WithArguments("MyDictionary<int, string>", "MyDictionary<K, V>.MyDictionary(params MyDictionary<K, V>)").WithLocation(9, 19));
        }

        [Theory]
        [InlineData("public V this[K key] { get { return default; } set { } }", true)]
        [InlineData("public K this[K key] { get { return default; } set { } }", false)]
        [InlineData("public V this[V key] { get { return default; } set { } }", false)]
        [InlineData("public V this[K x, K y = default] { get { return default; } set { } }", false)]
        [InlineData("public V this[K key] { get { return default; } }", false)]
        [InlineData("public V this[K key] { set { } }", false)]
        [InlineData("public V this[K key, object arg = null] { get { return default; } set { } }", false)]
        [InlineData("public V this[K key, params object[] args] { get { return default; } set { } }", false)]
        [InlineData("public V this[params K[] key] { get { return default; } set { } }", false)]
        public void IndexerSignature_01(string indexer, bool supported)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    {{indexer}}
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static MyDictionary<K, V> Empty<K, V>() => [];
                    static MyDictionary<K, V> FromPair<K, V>(K k, V v) => [k:v];
                    static MyDictionary<K, V> FromExpression<K, V>(KeyValuePair<K, V> e) => [e];
                    static MyDictionary<K, V> FromSpread<K, V>(IEnumerable<KeyValuePair<K, V>> s) => [..s];
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            if (supported)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (5,59): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                    //     static MyDictionary<K, V> FromPair<K, V>(K k, V v) => [k:v];
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[k:v]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(5, 59),
                    // (6,77): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                    //     static MyDictionary<K, V> FromExpression<K, V>(KeyValuePair<K, V> e) => [e];
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[e]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(6, 77),
                    // (7,86): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                    //     static MyDictionary<K, V> FromSpread<K, V>(IEnumerable<KeyValuePair<K, V>> s) => [..s];
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[..s]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(7, 86)); ;
            }
        }

        [Fact]
        public void IndexerSignature_02()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<Ke, Ve, Ki, Vi> : IEnumerable<KeyValuePair<Ke, Ve>>
                {
                    public IEnumerator<KeyValuePair<Ke, Ve>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public Vi this[Ki key] { get { return default; } set { } }
                }
                """;
            string sourceB = """
                class MyDictionary1<K, V> : MyDictionary<K, V, K, V> { }
                class MyDictionary2 : MyDictionary<string, object, string, object> { }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary1<string, object> d1 = [default:default];
                        MyDictionary2 d2 = [default:default];
                        MyDictionary<string, object, string, object> d3 = [default:default];
                        MyDictionary<string, object, object, string> d4 = [default:default];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (10,59): error CS1061: 'MyDictionary<string, object, object, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<string, object, object, string>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary<string, object, object, string> d4 = [default:default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default:default]").WithArguments("MyDictionary<string, object, object, string>", "Add").WithLocation(10, 59));
        }

        [Fact]
        public void IndexerSignature_03()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary1 : IEnumerable<KeyValuePair<object, object>>
                {
                    public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public dynamic this[dynamic key] { get { return default; } set { } }
                }
                class MyDictionary2 : IEnumerable<KeyValuePair<(int X, int Y), (object, object)>>
                {
                    public IEnumerator<KeyValuePair<(int X, int Y), (object, object)>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public (object A, object B) this[(int, int) key] { get { return default; } set { } }
                }
                class MyDictionary3 : IEnumerable<KeyValuePair<System.IntPtr, nuint>>
                {
                    public IEnumerator<KeyValuePair<System.IntPtr, nuint>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public System.UIntPtr this[nint key] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary1 d1 = [default:default];
                        MyDictionary2 d2 = [default:default];
                        MyDictionary3 d3 = [default:default];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void IndexerSignature_04()
        {
            string source = """
                #nullable enable
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary1 : IEnumerable<KeyValuePair<string?, object>>
                {
                    public IEnumerator<KeyValuePair<string?, object>> GetEnumerator() => null!;
                    IEnumerator IEnumerable.GetEnumerator() => null!;
                    public object? this[string key] { get { return default!; } set { } }
                }
                class MyDictionary2 : IEnumerable<KeyValuePair<string, object?>>
                {
                    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => null!;
                    IEnumerator IEnumerable.GetEnumerator() => null!;
                    public object this[string? key] { get { return default!; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary1 d1 = [default:default];
                        MyDictionary2 d2 = [default:default];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void IndexerSignature_MultipleIndexers()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public int this[string key]
                    {
                        get { return (int)(object)_d[(K)(object)key]; }
                        set { Console.WriteLine("string:{0}, int:{1}", key, value); _d[(K)(object)key] = (V)(object)value; }
                    }
                    public object this[object key]
                    {
                        get { return _d[(K)key]; }
                        set { Console.WriteLine("object:{0}, object:{1}", key, value); _d[(K)key] = (V)value; }
                    }
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<string, object> d;
                        d = ["one":1];
                        d = ["two":(object)2];
                        d = [(object)"three":(object)3];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB1]);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS1061: 'MyDictionary<string, object>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<string, object>' could be found (are you missing a using directive or an assembly reference?)
                //         d = ["one":1];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"[""one"":1]").WithArguments("MyDictionary<string, object>", "Add").WithLocation(6, 13),
                // (7,13): error CS1061: 'MyDictionary<string, object>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<string, object>' could be found (are you missing a using directive or an assembly reference?)
                //         d = ["two":(object)2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"[""two"":(object)2]").WithArguments("MyDictionary<string, object>", "Add").WithLocation(7, 13),
                // (8,13): error CS1061: 'MyDictionary<string, object>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<string, object>' could be found (are you missing a using directive or an assembly reference?)
                //         d = [(object)"three":(object)3];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"[(object)""three"":(object)3]").WithArguments("MyDictionary<string, object>", "Add").WithLocation(8, 13),
                // (8,14): error CS0029: Cannot implicitly convert type 'object' to 'string'
                //         d = [(object)"three":(object)3];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(object)""three""").WithArguments("object", "string").WithLocation(8, 14));

            string sourceB2 = """
                class MyDictionary1 : MyDictionary<string, int> { }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary1 d1 = ["one":1];
                        d1.Report();
                        MyDictionary<string, int> d2 = ["two":2];
                        d2.Report();
                        MyDictionary<object, object> d3 = ["three":3];
                        d3.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB2, s_dictionaryExtensions],
                expectedOutput: """
                    string:one, int:1
                    [one:1], string:two, int:2
                    [two:2], object:three, object:3
                    [three:3], 
                    """);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void IndexerSignature_ExplicitImplementation()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                interface IMyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    V this[K key] { get; set; }
                }
                class MyDictionary<K, V> : IMyDictionary<K, V>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    V IMyDictionary<K, V>.this[K key] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<int, string> d = [default:default];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (17,39): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary<int, string> d = [default:default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default:default]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(17, 39));
        }

        [Fact]
        public void IndexerSignature_Static()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public static V this[K key] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<int, string> d = [default:default];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,21): error CS0106: The modifier 'static' is not valid for this item
                //     public static V this[K key] { get { return default; } set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(7, 21));
        }

        [Theory]
        [InlineData("")]
        [InlineData("in")]
        [InlineData("ref readonly")]
        [InlineData("ref")]
        [InlineData("out")]
        public void IndexerSignature_RefParameter(string refKind)
        {
            string assignIfOut = refKind == "out" ? "key = default;" : "              ";
            string source = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[{{refKind}} K key] { get { {{assignIfOut}} return _d[key]; } set { {{assignIfOut}} _d[key] = value; } }
                }
                class Program
                {
                    static MyDictionary<K, V> Empty<K, V>() => [];
                    static MyDictionary<K, V> FromPair<K, V>(K k, V v) => [k:v];
                    static MyDictionary<K, V> FromExpression<K, V>(KeyValuePair<K, V> e) => [e];
                    static MyDictionary<K, V> FromSpread<K, V>(IEnumerable<KeyValuePair<K, V>> s) => [..s];
                    static void Main()
                    {
                        Empty<string, int>().Report();
                        FromPair(1, "one").Report();
                        FromExpression(new KeyValuePair<int, string>(2, "two")).Report();
                        FromSpread(new KeyValuePair<int, string>[] { new(3, "three") }).Report();
                    }
                }
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            switch (refKind)
            {
                case "":
                case "in":
                    var verifier = CompileAndVerify(comp,
                        expectedOutput: "[], [1:one], [2:two], [3:three], ");
                    verifier.VerifyDiagnostics();
                    break;
                case "ref readonly":
                    comp.VerifyEmitDiagnostics(
                        // (13,59): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                        //     static MyDictionary<K, V> FromPair<K, V>(K k, V v) => [k:v];
                        Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[k:v]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(13, 59),
                        // (14,77): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                        //     static MyDictionary<K, V> FromExpression<K, V>(KeyValuePair<K, V> e) => [e];
                        Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[e]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(14, 77),
                        // (15,86): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                        //     static MyDictionary<K, V> FromSpread<K, V>(IEnumerable<KeyValuePair<K, V>> s) => [..s];
                        Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[..s]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(15, 86));
                    break;
                case "ref":
                case "out":
                    comp.VerifyEmitDiagnostics(
                        // (8,19): error CS0631: ref and out are not valid in this context
                        //     public V this[ref K key] { get {                return _d[key]; } set {                _d[key] = value; } }
                        Diagnostic(ErrorCode.ERR_IllegalRefParam, refKind).WithLocation(8, 19),
                        // (13,59): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                        //     static MyDictionary<K, V> FromPair<K, V>(K k, V v) => [k:v];
                        Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[k:v]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(13, 59),
                        // (14,77): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                        //     static MyDictionary<K, V> FromExpression<K, V>(KeyValuePair<K, V> e) => [e];
                        Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[e]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(14, 77),
                        // (15,86): error CS1061: 'MyDictionary<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<K, V>' could be found (are you missing a using directive or an assembly reference?)
                        //     static MyDictionary<K, V> FromSpread<K, V>(IEnumerable<KeyValuePair<K, V>> s) => [..s];
                        Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[..s]").WithArguments("MyDictionary<K, V>", "Add").WithLocation(15, 86));
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }
        }

        [Fact]
        public void IndexerSignature_RefReturn()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary1<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public ref V this[K key] { get { throw null; } }
                }
                class MyDictionary2<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public ref V this[K key] { get { throw null; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary1<int, string> d1 = [default];
                        MyDictionary2<int, string> d2 = [default];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,52): error CS8147: Properties which return by reference cannot have set accessors
                //     public ref V this[K key] { get { throw null; } set { } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(13, 52),
                // (19,41): error CS1061: 'MyDictionary1<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary1<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary1<int, string> d1 = [default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default]").WithArguments("MyDictionary1<int, string>", "Add").WithLocation(19, 41),
                // (20,41): error CS1061: 'MyDictionary2<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary2<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary2<int, string> d2 = [default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default]").WithArguments("MyDictionary2<int, string>", "Add").WithLocation(20, 41));
        }

        [Fact]
        public void IndexerSignature_Overloads_01()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    protected Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key, object arg = null] { get { return default; } set { } }
                    public V this[K key] { get { return _d[key]; } set { _d[key] = value; } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<int, string> d = [default];
                        d.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[0:null], ");
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size       39 (0x27)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0)
                  IL_0000:  newobj     "MyDictionary<int, string>..ctor()"
                  IL_0005:  ldloca.s   V_0
                  IL_0007:  initobj    "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_000d:  dup
                  IL_000e:  ldloca.s   V_0
                  IL_0010:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_0015:  ldloca.s   V_0
                  IL_0017:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_001c:  callvirt   "void MyDictionary<int, string>.this[int].set"
                  IL_0021:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_0026:  ret
                }
                """);
        }

        [Fact]
        public void IndexerSignature_Overloads_02()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    protected Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key, object arg = null] { get { return default; } set { } }
                    public V this[K key, int arg] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<int, string> d = [default];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (15,39): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary<int, string> d = [default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(15, 39));
        }

        // Use indexer with expected signature, even if a better overload exists.
        [Fact]
        public void IndexerSignature_Overloads_BetterOverload()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V, KDerived> : IEnumerable<KeyValuePair<K, V>>
                    where KDerived : K
                {
                    protected List<KeyValuePair<K, V>> _list = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[KDerived key] { get { throw null; } set { throw null; } }
                    public V this[K key] { get { throw null; } set { _list.Add(new(key, value)); } }
                }
                class Program
                {
                    static void Main()
                    {
                        FromPair<object, string, int>(1, "one").Report();
                        FromExpression<object, string, int>(new KeyValuePair<int, string>(2, "two")).Report();
                        FromSpread<object, string, int>(new KeyValuePair<int, string>[] { new(3, "three") }).Report();
                    }
                    static MyDictionary<object, int, string> FromDefault() => [default];
                    static MyDictionary<K, V, KDerived> FromPair<K, V, KDerived>(KDerived k, V v)
                        where KDerived : K
                    {
                        return [k:v];
                    }
                    static MyDictionary<K, V, KDerived> FromExpression<K, V, KDerived>(KeyValuePair<KDerived, V> e)
                        where KDerived : K
                    {
                        return [e];
                    }
                    static MyDictionary<K, V, KDerived> FromSpread<K, V, KDerived>(KeyValuePair<KDerived, V>[] s)
                        where KDerived : K
                    {
                        return [..s];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[1:one], [2:two], [3:three], ");
            verifier.VerifyIL("Program.FromPair<K, V, KDerived>", """
                {
                  // Code size       24 (0x18)
                  .maxstack  4
                  IL_0000:  newobj     "MyDictionary<K, V, KDerived>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldarg.0
                  IL_0007:  box        "KDerived"
                  IL_000c:  unbox.any  "K"
                  IL_0011:  ldarg.1
                  IL_0012:  callvirt   "void MyDictionary<K, V, KDerived>.this[K].set"
                  IL_0017:  ret
                }
                """);
            verifier.VerifyIL("Program.FromExpression<K, V, KDerived>", """
                {
                  // Code size       38 (0x26)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<KDerived, V> V_0)
                  IL_0000:  newobj     "MyDictionary<K, V, KDerived>..ctor()"
                  IL_0005:  ldarg.0
                  IL_0006:  stloc.0
                  IL_0007:  dup
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  call       "KDerived System.Collections.Generic.KeyValuePair<KDerived, V>.Key.get"
                  IL_000f:  box        "KDerived"
                  IL_0014:  unbox.any  "K"
                  IL_0019:  ldloca.s   V_0
                  IL_001b:  call       "V System.Collections.Generic.KeyValuePair<KDerived, V>.Value.get"
                  IL_0020:  callvirt   "void MyDictionary<K, V, KDerived>.this[K].set"
                  IL_0025:  ret
                }
                """);
            verifier.VerifyIL("Program.FromSpread<K, V, KDerived>", """
                {
                  // Code size       62 (0x3e)
                  .maxstack  3
                  .locals init (MyDictionary<K, V, KDerived> V_0,
                                System.Collections.Generic.KeyValuePair<KDerived, V>[] V_1,
                                int V_2,
                                System.Collections.Generic.KeyValuePair<KDerived, V> V_3)
                  IL_0000:  newobj     "MyDictionary<K, V, KDerived>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldarg.0
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  IL_000a:  br.s       IL_0036
                  IL_000c:  ldloc.1
                  IL_000d:  ldloc.2
                  IL_000e:  ldelem     "System.Collections.Generic.KeyValuePair<KDerived, V>"
                  IL_0013:  stloc.3
                  IL_0014:  ldloc.0
                  IL_0015:  ldloca.s   V_3
                  IL_0017:  call       "KDerived System.Collections.Generic.KeyValuePair<KDerived, V>.Key.get"
                  IL_001c:  box        "KDerived"
                  IL_0021:  unbox.any  "K"
                  IL_0026:  ldloca.s   V_3
                  IL_0028:  call       "V System.Collections.Generic.KeyValuePair<KDerived, V>.Value.get"
                  IL_002d:  callvirt   "void MyDictionary<K, V, KDerived>.this[K].set"
                  IL_0032:  ldloc.2
                  IL_0033:  ldc.i4.1
                  IL_0034:  add
                  IL_0035:  stloc.2
                  IL_0036:  ldloc.2
                  IL_0037:  ldloc.1
                  IL_0038:  ldlen
                  IL_0039:  conv.i4
                  IL_003a:  blt.s      IL_000c
                  IL_003c:  ldloc.0
                  IL_003d:  ret
                }
                """);
        }

        [Fact]
        public void IndexerSignature_Overloads_BaseAndDerived_01()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionaryBase<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    protected Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key, object arg = null] { get { return default; } set { } }
                }
                class MyDictionary<K, V> : MyDictionaryBase<K, V>
                {
                    public V this[K key] { get { return _d[key]; } set { _d[key] = value; } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<int, string> d = [default];
                        d.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[0:null], ");
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size       39 (0x27)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0)
                  IL_0000:  newobj     "MyDictionary<int, string>..ctor()"
                  IL_0005:  ldloca.s   V_0
                  IL_0007:  initobj    "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_000d:  dup
                  IL_000e:  ldloca.s   V_0
                  IL_0010:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_0015:  ldloca.s   V_0
                  IL_0017:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_001c:  callvirt   "void MyDictionary<int, string>.this[int].set"
                  IL_0021:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_0026:  ret
                }
                """);
        }

        [Fact]
        public void IndexerSignature_Overloads_BaseAndDerived_02()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionaryBase<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    protected Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key] { get { return _d[key]; } set { _d[key] = value; } }
                }
                class MyDictionary<K, V> : MyDictionaryBase<K, V>
                {
                    public V this[K key, object arg = null] { get { return default; } set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        MyDictionary<int, string> d = [default];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (18,39): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary<int, string> d = [default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(18, 39));
        }

        [Fact]
        public void IndexerSignature_Overridden()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    protected Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public virtual V this[K key] { get { return default; } set { } }
                }
                class MyDictionary1<K, V> : MyDictionary<K, V>
                {
                    public override V this[K key] { get { return _d[key]; } }
                }
                class MyDictionary2<K, V> : MyDictionary<K, V>
                {
                    public override V this[K key] { set { _d[key] = value; } }
                }
                class MyDictionary3<K, V> : MyDictionary<K, V>
                {
                    public override V this[K key] { get { return _d[key]; } set { _d[key] = value; } }
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyDictionary1<int, string> d1 = [default];
                        d1.Report();
                        MyDictionary2<int, string> d2 = [default];
                        d2.Report();
                        MyDictionary3<int, string> d3 = [default];
                        d3.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_dictionaryExtensions],
                expectedOutput: "[], [0:null], [0:null], ");
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size      115 (0x73)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0)
                  IL_0000:  newobj     "MyDictionary1<int, string>..ctor()"
                  IL_0005:  ldloca.s   V_0
                  IL_0007:  initobj    "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_000d:  dup
                  IL_000e:  ldloca.s   V_0
                  IL_0010:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_0015:  ldloca.s   V_0
                  IL_0017:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_001c:  callvirt   "void MyDictionary<int, string>.this[int].set"
                  IL_0021:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_0026:  newobj     "MyDictionary2<int, string>..ctor()"
                  IL_002b:  ldloca.s   V_0
                  IL_002d:  initobj    "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0033:  dup
                  IL_0034:  ldloca.s   V_0
                  IL_0036:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_003b:  ldloca.s   V_0
                  IL_003d:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_0042:  callvirt   "void MyDictionary<int, string>.this[int].set"
                  IL_0047:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_004c:  newobj     "MyDictionary3<int, string>..ctor()"
                  IL_0051:  ldloca.s   V_0
                  IL_0053:  initobj    "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0059:  dup
                  IL_005a:  ldloca.s   V_0
                  IL_005c:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_0061:  ldloca.s   V_0
                  IL_0063:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_0068:  callvirt   "void MyDictionary<int, string>.this[int].set"
                  IL_006d:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_0072:  ret
                }
                """);
        }

        [Fact]
        public void IndexerSignature_New()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    protected Dictionary<K, V> _d = new();
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _d.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key] { get { return default; } set { } }
                }
                """;

            string sourceB1 = """
                class MyDictionary1<K, V> : MyDictionary<K, V>
                {
                    public new V this[K key] { get { return _d[key]; } }
                }
                class MyDictionary2<K, V> : MyDictionary<K, V>
                {
                    public new V this[K key] { set { _d[key] = value; } }
                }
                class Program
                {
                    static void F<K, V>()
                    {
                        MyDictionary1<K, V> d1 = [default:default];
                        MyDictionary2<K, V> d2 = [default:default];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB1]);
            comp.VerifyEmitDiagnostics(
                // (13,34): error CS1061: 'MyDictionary1<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary1<K, V>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary1<K, V> d1 = [default:default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default:default]").WithArguments("MyDictionary1<K, V>", "Add").WithLocation(13, 34),
                // (14,34): error CS1061: 'MyDictionary2<K, V>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary2<K, V>' could be found (are you missing a using directive or an assembly reference?)
                //         MyDictionary2<K, V> d2 = [default:default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default:default]").WithArguments("MyDictionary2<K, V>", "Add").WithLocation(14, 34));

            string sourceB2 = """
                class MyDictionary3<K, V> : MyDictionary<K, V>
                {
                    public new V this[K key] { get { return _d[key]; } set { _d[key] = value; } }
                }
                class Program
                {
                    static void Main()
                    {
                        F<int, string>().Report();
                    }
                    static MyDictionary3<K, V> F<K, V>()
                    {
                        return [default:default];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB2, s_dictionaryExtensions],
                expectedOutput: "[0:null], ");
            verifier.VerifyIL("Program.F<K, V>", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  4
                  .locals init (K V_0,
                                V V_1)
                  IL_0000:  newobj     "MyDictionary3<K, V>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  initobj    "K"
                  IL_000e:  ldloc.0
                  IL_000f:  ldloca.s   V_1
                  IL_0011:  initobj    "V"
                  IL_0017:  ldloc.1
                  IL_0018:  callvirt   "void MyDictionary3<K, V>.this[K].set"
                  IL_001d:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void IndexerAccessibility_01(
            [CombinatorialValues("", "public", "internal")] string typeAccessibility,
            [CombinatorialValues("", "public", "internal")] string indexerAccessibility)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                {{typeAccessibility}} class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    {{indexerAccessibility}} V this[K key] { get { return default; } set { } }
                }
                """;
            string sourceB = """
                MyDictionary<string, object> d = [default:default];
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            if (indexerAccessibility == "public")
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (1,34): error CS1061: 'MyDictionary<string, object>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<string, object>' could be found (are you missing a using directive or an assembly reference?)
                    // MyDictionary<string, object> d = [default:default];
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default:default]").WithArguments("MyDictionary<string, object>", "Add").WithLocation(1, 34));
            }
        }

        [Theory]
        [CombinatorialData]
        public void IndexerAccessibility_02(
            [CombinatorialValues("private", "protected")] string indexerAccessibility)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    {{indexerAccessibility}} V this[K key] { get { return default; } set { } }
                }
                """;
            string sourceB = """
                MyDictionary<string, object> d = [default:default];
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (1,34): error CS1061: 'MyDictionary<string, object>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<string, object>' could be found (are you missing a using directive or an assembly reference?)
                // MyDictionary<string, object> d = [default:default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default:default]").WithArguments("MyDictionary<string, object>", "Add").WithLocation(1, 34));
        }

        [Theory]
        [CombinatorialData]
        public void IndexerAccessibility_03(
            [CombinatorialValues("internal", "private", "protected")] string indexerAccessibility, bool modifyGetter)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K key]
                    {
                        {{(modifyGetter ? indexerAccessibility : "")}} get { return default; }
                        {{(modifyGetter ? "" : indexerAccessibility)}} set { }
                    }
                }
                """;
            string sourceB = """
                MyDictionary<string, object> d = [default:default];
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (1,34): error CS1061: 'MyDictionary<string, object>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<string, object>' could be found (are you missing a using directive or an assembly reference?)
                // MyDictionary<string, object> d = [default:default];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "[default:default]").WithArguments("MyDictionary<string, object>", "Add").WithLocation(1, 34));
        }

        [Fact]
        public void IReadOnlyDictionary_01()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Empty<int, string>();
                        Pair(1, "one");
                        Many([new KeyValuePair<int, string>(3, "three")]);
                        ParamsEmptyOrOne(new KeyValuePair<int, string>(2, "two"));
                    }
                    static void Empty<K, V>() { Report<K, V>([]); }
                    static void Pair<K, V>(K k, V v) { Report<K, V>([k:v]); }
                    static void Many<K, V>(KeyValuePair<K, V>[] s) { Report([..s]); }
                    static void Params<K, V>(params IReadOnlyDictionary<K, V> d) { Report(d); }
                    static void ParamsEmptyOrOne<K, V>(KeyValuePair<K, V> e)
                    {
                        Params<K, V>();
                        Params(e);
                    }
                    static void Report<K, V>(IReadOnlyDictionary<K, V> d)
                    {
                        Console.Write("{0}: ", d.GetType().GetTypeName());
                        d.Report();
                        Console.WriteLine();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions, s_dictionaryExtensions],
                expectedOutput: """
                    System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>: [], 
                    System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>: [1:one], 
                    System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>: [3:three], 
                    System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>: [], 
                    System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>: [2:two],  
                    """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Empty<K, V>", """
                {
                  // Code size       16 (0x10)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_000a:  call       "void Program.Report<K, V>(System.Collections.Generic.IReadOnlyDictionary<K, V>)"
                  IL_000f:  ret
                }
                """);
            verifier.VerifyIL("Program.Pair<K, V>", """
                {
                  // Code size       24 (0x18)
                  .maxstack  4
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldarg.0
                  IL_0007:  ldarg.1
                  IL_0008:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000d:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_0012:  call       "void Program.Report<K, V>(System.Collections.Generic.IReadOnlyDictionary<K, V>)"
                  IL_0017:  ret
                }
                """);
            verifier.VerifyIL("Program.Many<K, V>", """
                {
                  // Code size       62 (0x3e)
                  .maxstack  3
                  .locals init (System.Collections.Generic.Dictionary<K, V> V_0,
                                System.Collections.Generic.KeyValuePair<K, V>[] V_1,
                                int V_2,
                                System.Collections.Generic.KeyValuePair<K, V> V_3)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldarg.0
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  IL_000a:  br.s       IL_002c
                  IL_000c:  ldloc.1
                  IL_000d:  ldloc.2
                  IL_000e:  ldelem     "System.Collections.Generic.KeyValuePair<K, V>"
                  IL_0013:  stloc.3
                  IL_0014:  ldloc.0
                  IL_0015:  ldloca.s   V_3
                  IL_0017:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_001c:  ldloca.s   V_3
                  IL_001e:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_0023:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0028:  ldloc.2
                  IL_0029:  ldc.i4.1
                  IL_002a:  add
                  IL_002b:  stloc.2
                  IL_002c:  ldloc.2
                  IL_002d:  ldloc.1
                  IL_002e:  ldlen
                  IL_002f:  conv.i4
                  IL_0030:  blt.s      IL_000c
                  IL_0032:  ldloc.0
                  IL_0033:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_0038:  call       "void Program.Report<K, V>(System.Collections.Generic.IReadOnlyDictionary<K, V>)"
                  IL_003d:  ret
                }
                """);
            verifier.VerifyIL("Program.ParamsEmptyOrOne<K, V>", """
                {
                  // Code size       53 (0x35)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_000a:  call       "void Program.Params<K, V>(params System.Collections.Generic.IReadOnlyDictionary<K, V>)"
                  IL_000f:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0014:  ldarg.0
                  IL_0015:  stloc.0
                  IL_0016:  dup
                  IL_0017:  ldloca.s   V_0
                  IL_0019:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_001e:  ldloca.s   V_0
                  IL_0020:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_0025:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_002a:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_002f:  call       "void Program.Params<K, V>(params System.Collections.Generic.IReadOnlyDictionary<K, V>)"
                  IL_0034:  ret
                }
                """);
        }

        [Fact]
        public void IReadOnlyDictionary_02()
        {
            string source = """
                using System;
                using static System.Console;
                using System.Collections;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new[] { new KeyValuePair<int, string>(3, "three") };
                        ReportDictionary<int, string>([], 2, "two");
                        ReportDictionary([1:"one", x, ..y], 2, "two");
                    }
                    static void ReportDictionary<K, V>(IReadOnlyDictionary<K, V> x, K k, V v)
                    {
                        int length = x.Count;
                        KeyValuePair<K, V>[] a;
                        Write("{0}: ", x.GetType().GetTypeName());
                        ReportCollection(x, new KeyValuePair<K, V>(k, v));
                        WriteLine("IDictionary.IsFixedSize: {0}", ((IDictionary)x).IsFixedSize);
                        WriteLine("IDictionary.IsReadOnly: {0}", ((IDictionary)x).IsReadOnly);
                        WriteLine("IDictionary.this[k]: {0}", Invoke(() => ((IDictionary)x)[k]));
                        WriteLine("IDictionary.this[k] = v: {0}", Invoke(() => { ((IDictionary)x)[k] = v; }));
                        ((IDictionary)x).Keys.Report(includeType: true);
                        ((IDictionary)x).Values.Report(includeType: true);
                        WriteLine("IDictionary.Add(k, v): {0}", Invoke(() => ((IDictionary)x).Add(k, v)));
                        WriteLine("IDictionary.Clear(): {0}", Invoke(() => ((IDictionary)x).Clear()));
                        WriteLine("IDictionary.Contains(k): {0}", Invoke(() => ((IDictionary)x).Contains(k)));
                        Write("IDictionary.CopyTo(..., 0): ");
                        a = new KeyValuePair<K, V>[length];
                        ((IDictionary)x).CopyTo(a, 0);
                        a.Report(includeType: true);
                        WriteLine();
                        WriteLine("IDictionary.Remove(k): {0}", Invoke(() => ((IDictionary)x).Remove(k)));
                        WriteLine("IDictionary<K, V>.Count: {0}", ((IDictionary<K, V>)x).Count);
                        WriteLine("IDictionary<K, V>.IsReadOnly: {0}", ((IDictionary<K, V>)x).IsReadOnly);
                        WriteLine("IDictionary<K, V>.this[k]: {0}", Invoke(() => ((IDictionary<K, V>)x)[k]));
                        WriteLine("IDictionary<K, V>.this[k] = v: {0}", Invoke(() => { ((IDictionary<K, V>)x)[k] = v; }));
                        ((IDictionary<K, V>)x).Keys.Report(includeType: true);
                        ((IDictionary<K, V>)x).Values.Report(includeType: true);
                        WriteLine("IDictionary<K, V>.Add(k, v): {0}", Invoke(() => ((IDictionary<K, V>)x).Add(k, v)));
                        WriteLine("IDictionary<K, V>.Clear(): {0}", Invoke(() => ((IDictionary<K, V>)x).Clear()));
                        WriteLine("IDictionary<K, V>.ContainsKey(k): {0}", Invoke(() => ((IDictionary<K, V>)x).ContainsKey(k)));
                        Write("IDictionary<K, V>.CopyTo(..., 0): ");
                        a = new KeyValuePair<K, V>[length];
                        ((IDictionary<K, V>)x).CopyTo(a, 0);
                        a.Report(includeType: true);
                        WriteLine();
                        WriteLine("IDictionary<K, V>.Remove(k): {0}", Invoke(() => ((IDictionary<K, V>)x).Remove(k)));
                        WriteLine("IDictionary<K, V>.TryGetValue(k, out v): {0}", Invoke(() => ((IDictionary<K, V>)x).TryGetValue(k, out _)));
                        WriteLine("IReadOnlyDictionary<K, V>.Count: {0}", ((IReadOnlyDictionary<K, V>)x).Count);
                        WriteLine("IReadOnlyDictionary<K, V>.this[k]: {0}", Invoke(() => ((IReadOnlyDictionary<K, V>)x)[k]));
                        ((IReadOnlyDictionary<K, V>)x).Keys.Report(includeType: true);
                        ((IReadOnlyDictionary<K, V>)x).Values.Report(includeType: true);
                        WriteLine("IReadOnlyDictionary<K, V>.ContainsKey(k): {0}", Invoke(() => ((IReadOnlyDictionary<K, V>)x).ContainsKey(k)));
                        WriteLine("IReadOnlyDictionary<K, V>.TryGetValue(k, out v): {0}", Invoke(() => ((IReadOnlyDictionary<K, V>)x).TryGetValue(k, out _)));
                    }
                    static void ReportCollection<T>(IReadOnlyCollection<T> x, T value)
                    {
                        int length = x.Count;
                        T[] a;
                        Write("IEnumerable.GetEnumerator(): ");
                        ((IEnumerable)x).Report(includeType: true);
                        WriteLine();
                        WriteLine("ICollection.Count: {0}", ((ICollection)x).Count);
                        WriteLine("ICollection.IsSynchronized: {0}", ((ICollection)x).IsSynchronized);
                        WriteLine("ICollection.SyncRoot == (object)x: {0}", ((ICollection)x).SyncRoot == (object)x);
                        Write("ICollection.CopyTo(..., 0): ");
                        a = new T[length];
                        ((ICollection)x).CopyTo(a, 0);
                        a.Report(includeType: true);
                        WriteLine();
                        Write("IEnumerable<T>.GetEnumerator(): ");
                        ((IEnumerable<T>)x).Report(includeType: true);
                        WriteLine();
                        WriteLine("IReadOnlyCollection<T>.Count: {0}", ((IReadOnlyCollection<T>)x).Count);
                        WriteLine("ICollection<T>.Count: {0}", ((ICollection<T>)x).Count);
                        WriteLine("ICollection<T>.IsReadOnly: {0}", ((ICollection<T>)x).IsReadOnly);
                        WriteLine("ICollection<T>.Add(value): {0}", Invoke(() => ((ICollection<T>)x).Add(value)));
                        WriteLine("ICollection<T>.Clear(): {0}", Invoke(() => ((ICollection<T>)x).Clear()));
                        WriteLine("ICollection<T>.Contains(value): {0}", ((ICollection<T>)x).Contains(value));
                        Write("ICollection<T>.CopyTo(..., 0): ");
                        a = new T[length];
                        ((ICollection<T>)x).CopyTo(a, 0);
                        a.Report(includeType: true);
                        WriteLine();
                        WriteLine("ICollection<T>.Remove(value): {0}", Invoke(() => ((ICollection<T>)x).Remove(value)));
                    }
                    static string Invoke<T>(Func<T> f)
                    {
                        try
                        {
                            var value = f();
                            return value is null ? "null" : value.ToString();
                        }
                        catch (Exception e)
                        {
                            return e.GetType().FullName;
                        }
                    }
                    static string Invoke(Action a)
                    {
                        try
                        {
                            a();
                            return "completed";
                        }
                        catch (Exception e)
                        {
                            return e.GetType().FullName;
                        }
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                expectedOutput: $$"""
                    System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>: IEnumerable.GetEnumerator(): (System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>) [], 
                    ICollection.Count: 0
                    ICollection.IsSynchronized: False
                    ICollection.SyncRoot == (object)x: False
                    ICollection.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [], 
                    IEnumerable<T>.GetEnumerator(): (System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>) [], 
                    IReadOnlyCollection<T>.Count: 0
                    ICollection<T>.Count: 0
                    ICollection<T>.IsReadOnly: True
                    ICollection<T>.Add(value): System.NotSupportedException
                    ICollection<T>.Clear(): System.NotSupportedException
                    ICollection<T>.Contains(value): False
                    ICollection<T>.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [], 
                    ICollection<T>.Remove(value): System.NotSupportedException
                    IDictionary.IsFixedSize: True
                    IDictionary.IsReadOnly: True
                    IDictionary.this[k]: {{(ExecutionConditionUtil.IsMonoOrCoreClr ? "null" : "System.Collections.Generic.KeyNotFoundException")}}
                    IDictionary.this[k] = v: System.NotSupportedException
                    (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection<System.Int32, System.String>) [], (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection<System.Int32, System.String>) [], IDictionary.Add(k, v): System.NotSupportedException
                    IDictionary.Clear(): System.NotSupportedException
                    IDictionary.Contains(k): False
                    IDictionary.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [], 
                    IDictionary.Remove(k): System.NotSupportedException
                    IDictionary<K, V>.Count: 0
                    IDictionary<K, V>.IsReadOnly: True
                    IDictionary<K, V>.this[k]: System.Collections.Generic.KeyNotFoundException
                    IDictionary<K, V>.this[k] = v: System.NotSupportedException
                    (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection<System.Int32, System.String>) [], (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection<System.Int32, System.String>) [], IDictionary<K, V>.Add(k, v): System.NotSupportedException
                    IDictionary<K, V>.Clear(): System.NotSupportedException
                    IDictionary<K, V>.ContainsKey(k): False
                    IDictionary<K, V>.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [], 
                    IDictionary<K, V>.Remove(k): System.NotSupportedException
                    IDictionary<K, V>.TryGetValue(k, out v): False
                    IReadOnlyDictionary<K, V>.Count: 0
                    IReadOnlyDictionary<K, V>.this[k]: System.Collections.Generic.KeyNotFoundException
                    (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection<System.Int32, System.String>) [], (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection<System.Int32, System.String>) [], IReadOnlyDictionary<K, V>.ContainsKey(k): False
                    IReadOnlyDictionary<K, V>.TryGetValue(k, out v): False
                    System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>: IEnumerable.GetEnumerator(): (System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>) [[1, one], [2, two], [3, three]], 
                    ICollection.Count: 3
                    ICollection.IsSynchronized: False
                    ICollection.SyncRoot == (object)x: False
                    ICollection.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [[1, one], [2, two], [3, three]], 
                    IEnumerable<T>.GetEnumerator(): (System.Collections.ObjectModel.ReadOnlyDictionary<System.Int32, System.String>) [[1, one], [2, two], [3, three]], 
                    IReadOnlyCollection<T>.Count: 3
                    ICollection<T>.Count: 3
                    ICollection<T>.IsReadOnly: True
                    ICollection<T>.Add(value): System.NotSupportedException
                    ICollection<T>.Clear(): System.NotSupportedException
                    ICollection<T>.Contains(value): True
                    ICollection<T>.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [[1, one], [2, two], [3, three]], 
                    ICollection<T>.Remove(value): System.NotSupportedException
                    IDictionary.IsFixedSize: True
                    IDictionary.IsReadOnly: True
                    IDictionary.this[k]: two
                    IDictionary.this[k] = v: System.NotSupportedException
                    (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection<System.Int32, System.String>) [1, 2, 3], (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection<System.Int32, System.String>) [one, two, three], IDictionary.Add(k, v): System.NotSupportedException
                    IDictionary.Clear(): System.NotSupportedException
                    IDictionary.Contains(k): True
                    IDictionary.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [[1, one], [2, two], [3, three]], 
                    IDictionary.Remove(k): System.NotSupportedException
                    IDictionary<K, V>.Count: 3
                    IDictionary<K, V>.IsReadOnly: True
                    IDictionary<K, V>.this[k]: two
                    IDictionary<K, V>.this[k] = v: System.NotSupportedException
                    (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection<System.Int32, System.String>) [1, 2, 3], (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection<System.Int32, System.String>) [one, two, three], IDictionary<K, V>.Add(k, v): System.NotSupportedException
                    IDictionary<K, V>.Clear(): System.NotSupportedException
                    IDictionary<K, V>.ContainsKey(k): True
                    IDictionary<K, V>.CopyTo(..., 0): (System.Collections.Generic.KeyValuePair<System.Int32, System.String>[]) [[1, one], [2, two], [3, three]], 
                    IDictionary<K, V>.Remove(k): System.NotSupportedException
                    IDictionary<K, V>.TryGetValue(k, out v): True
                    IReadOnlyDictionary<K, V>.Count: 3
                    IReadOnlyDictionary<K, V>.this[k]: two
                    (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection<System.Int32, System.String>) [1, 2, 3], (System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection<System.Int32, System.String>) [one, two, three], IReadOnlyDictionary<K, V>.ContainsKey(k): True
                    IReadOnlyDictionary<K, V>.TryGetValue(k, out v): True
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void IReadOnlyDictionary_MissingMembers(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                {{typeName}}<int, string> d;
                var x = new KeyValuePair<int, string>(2, "two");
                var y = new[] { new KeyValuePair<int, string>(3, "three") };
                d = [];
                d = [1:"one"];
                d = [x, ..y];
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_ObjectModel_ReadOnlyDictionary_KV);
            if (typeName == "IReadOnlyDictionary")
            {
                comp.VerifyEmitDiagnostics(
                    // (5,5): error CS0656: Missing compiler required member 'System.Collections.ObjectModel.ReadOnlyDictionary`2..ctor'
                    // d = [];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.ObjectModel.ReadOnlyDictionary`2", ".ctor").WithLocation(5, 5),
                    // (6,5): error CS0656: Missing compiler required member 'System.Collections.ObjectModel.ReadOnlyDictionary`2..ctor'
                    // d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.ObjectModel.ReadOnlyDictionary`2", ".ctor").WithLocation(6, 5),
                    // (7,5): error CS0656: Missing compiler required member 'System.Collections.ObjectModel.ReadOnlyDictionary`2..ctor'
                    // d = [x, ..y];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[x, ..y]").WithArguments("System.Collections.ObjectModel.ReadOnlyDictionary`2", ".ctor").WithLocation(7, 5));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_ObjectModel_ReadOnlyDictionary_KV__ctor);
            if (typeName == "IReadOnlyDictionary")
            {
                comp.VerifyEmitDiagnostics(
                    // (5,5): error CS0656: Missing compiler required member 'System.Collections.ObjectModel.ReadOnlyDictionary`2..ctor'
                    // d = [];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.ObjectModel.ReadOnlyDictionary`2", ".ctor").WithLocation(5, 5),
                    // (6,5): error CS0656: Missing compiler required member 'System.Collections.ObjectModel.ReadOnlyDictionary`2..ctor'
                    // d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.ObjectModel.ReadOnlyDictionary`2", ".ctor").WithLocation(6, 5),
                    // (7,5): error CS0656: Missing compiler required member 'System.Collections.ObjectModel.ReadOnlyDictionary`2..ctor'
                    // d = [x, ..y];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[x, ..y]").WithArguments("System.Collections.ObjectModel.ReadOnlyDictionary`2", ".ctor").WithLocation(7, 5));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
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
                        IDictionary<object, object> d = [x:y];
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (9,23): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         object[] a = [x];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "x").WithLocation(9, 23),
                // (10,42): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         IDictionary<object, object> d = [x:y];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "x").WithLocation(10, 42),
                // (10,44): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         IDictionary<object, object> d = [x:y];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "y").WithLocation(10, 44));
        }
    }
}
