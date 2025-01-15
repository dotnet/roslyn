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
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void DictionaryInterface_01(string typeName)
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
            verifier.VerifyIL("Program.F<K, V>", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  ret
                }
                """);
        }

        [Theory]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void DictionaryInterface_02(string typeName)
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
                    static {{typeName}}<int, string> F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
                    {
                        return [1:"one", x, ..y];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[1:one, 2:two, 3:three], ");
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
                  IL_000d:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                  IL_0012:  ldarg.0
                  IL_0013:  stloc.1
                  IL_0014:  ldloc.0
                  IL_0015:  ldloca.s   V_1
                  IL_0017:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_001c:  ldloca.s   V_1
                  IL_001e:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_0023:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
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
                    IL_0047:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
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
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void DictionaryInterface_DuplicateKeys(string typeName)
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
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void DictionaryNotImplementingIDictionary(string typeName)
        {
            string sourceA = """
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
                        var d = F(1, "one", new KeyValuePair<int, string>(), null);
                    }
                    static {{typeName}}<K, V> F<K, V>(K k, V v, KeyValuePair<K, V> x, IEnumerable<KeyValuePair<K, V>> y)
                    {
                        return [k:v, x, ..y];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB });
            comp.VerifyEmitDiagnostics(Microsoft.CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"),
                // 1.cs(10,16): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.Dictionary<K, V>' to 'System.Collections.Generic.IDictionary<K, V>'
                //         return [k:v, x, ..y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[k:v, x, ..y]").WithArguments("System.Collections.Generic.Dictionary<K, V>", $"System.Collections.Generic.{typeName}<K, V>").WithLocation(10, 16));
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
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_KeyValuePair_KV);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(3, 5),
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(4, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(4, 5));

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

            // PROTOTYPE: Implement IOperation support.
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IDictionary<System.Int64, System.Object>) (Syntax: '[x:y]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'x:y')
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
        public void KeyValuePairConversions_03()
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
                // (10,17): error CS9268: Collection expression type 'IEnumerable<MyKeyValuePair<K, V>>' does not support key-value pair elements.
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

        [Fact]
        public void KeyValuePairConversions_04()
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

            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new MyKeyValuePair<int, string>(2, "two");
                        var y = new MyKeyValuePair<int, string>[] { new(3, "three") };
                        var e = F1(x, y);
                        e.Report();
                        var d = F2(x, y);
                        d.Report();
                    }
                    static IEnumerable<KeyValuePair<K, V>> F1<K, V>(MyKeyValuePair<K, V> x, IEnumerable<MyKeyValuePair<K, V>> y)
                    {
                        return [x, ..y];
                    }
                    static IDictionary<K, V> F2<K, V>(MyKeyValuePair<K, V> x, IEnumerable<MyKeyValuePair<K, V>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, s_collectionExtensions],
                references: [refA],
                expectedOutput: "[[2, two], [3, three]], [[2, two], [3, three]], ");
            verifier.VerifyDiagnostics();
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
                  IL_007c:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
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
                  // Code size      119 (0x77)
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
                  IL_0076:  ret
                }
                """);
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
        public void InferredType_KeyValueElement()
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
