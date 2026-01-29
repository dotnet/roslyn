// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MissingSpecialMember : CSharpTestBase
    {
        [Fact]
        public void Missing_System_Collections_Generic_IEnumerable_T__GetEnumerator()
        {
            var source =
@"using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
    }

    public IEnumerable<int> M()
    {
        yield return 0;
        yield return 1;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator);

            comp.VerifyEmitDiagnostics(
    // (10,5): error CS0656: Missing compiler required member 'System.Collections.Generic.IEnumerable`1.GetEnumerator'
    //     {
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        yield return 0;
        yield return 1;
    }").WithArguments("System.Collections.Generic.IEnumerable`1", "GetEnumerator").WithLocation(10, 5)
                );
        }

        [Fact]
        public void Missing_System_IDisposable__Dispose()
        {
            var source =
@"using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
    }

    public IEnumerable<int> M()
    {
        yield return 0;
        yield return 1;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(SpecialMember.System_IDisposable__Dispose);

            comp.VerifyEmitDiagnostics(
    // (10,5): error CS0656: Missing compiler required member 'System.IDisposable.Dispose'
    //     {
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        yield return 0;
        yield return 1;
    }").WithArguments("System.IDisposable", "Dispose").WithLocation(10, 5)
                );
        }

        [Fact]
        public void Missing_System_Diagnostics_DebuggerHiddenAttribute__ctor()
        {
            var source =
@"using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
    }

    public IEnumerable<int> M()
    {
        yield return 0;
        yield return 1;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor);

            comp.VerifyEmitDiagnostics(
                // the DebuggerHidden attribute is optional.
                );
        }

        [Fact]
        public void Missing_System_Runtime_CompilerServices_ExtensionAttribute__ctor()
        {
            var source =
@"using System.Collections.Generic;

public static class Program
{
    public static void Main(string[] args)
    {
    }

    public static void Extension(this string x) {}
}";
            var comp = CreateEmptyCompilation(source, new[] { Net40.References.mscorlib }, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor);

            comp.VerifyEmitDiagnostics(
                // (9,34): error CS1110: Cannot define a new extension because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                //     public static void Extension(this string x) {}
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(9, 34)
                );
        }

        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void NonPublicSpecialType()
        {
            var source = @"
namespace System
{
    public class Object
    {
        public Object() { }
    }

    internal class String : Object
    {
    }

    public class ValueType { }
    public struct Void { }
}
";
            Action<CSharpCompilation> validate = comp =>
            {
                var specialType = comp.GetSpecialType(SpecialType.System_String);
                Assert.Equal(TypeKind.Error, specialType.TypeKind);
                Assert.Equal(SpecialType.System_String, specialType.SpecialType);
                Assert.Equal(Accessibility.NotApplicable, specialType.DeclaredAccessibility);

                var lookupType = comp.GetTypeByMetadataName("System.String");
                Assert.Equal(TypeKind.Class, lookupType.TypeKind);
                Assert.Equal(SpecialType.None, lookupType.SpecialType);
                Assert.Equal(Accessibility.Internal, lookupType.DeclaredAccessibility);
            };

            ValidateSourceAndMetadata(source, validate);
        }

        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void NonPublicSpecialTypeMember()
        {
            var sourceTemplate = @"
namespace System
{{
    public class Object
    {{
        public Object() {{ }}

        {0} virtual String ToString() {{ return null; }}
    }}

    {0} class String : Object
    {{
        public static String Concat(String s1, String s2) {{ return null; }}
    }}

    public class ValueType {{ }}
    public struct Void {{ }}
}}
";
            Action<CSharpCompilation> validatePresent = comp =>
            {
                Assert.NotNull(comp.GetSpecialTypeMember(SpecialMember.System_Object__ToString));
                Assert.NotNull(comp.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString));
                comp.GetDiagnostics();
            };

            Action<CSharpCompilation> validateMissing = comp =>
            {
                Assert.Null(comp.GetSpecialTypeMember(SpecialMember.System_Object__ToString));
                Assert.Null(comp.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString));
                comp.GetDiagnostics();
            };

            ValidateSourceAndMetadata(string.Format(sourceTemplate, "public"), validatePresent);
            ValidateSourceAndMetadata(string.Format(sourceTemplate, "internal"), validateMissing);
        }

        // Document the fact that we don't reject type parameters with constraints (yet?).
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void GenericConstraintsOnSpecialType()
        {
            var source = @"
namespace System
{
    public class Object
    {
        public Object() { }
    }

    public struct Nullable<T> where T : new()
    {
    }

    public class ValueType { }
    public struct Void { }
}
";
            Action<CSharpCompilation> validate = comp =>
            {
                var specialType = comp.GetSpecialType(SpecialType.System_Nullable_T);
                Assert.Equal(TypeKind.Struct, specialType.TypeKind);
                Assert.Equal(SpecialType.System_Nullable_T, specialType.SpecialType);

                var lookupType = comp.GetTypeByMetadataName("System.Nullable`1");
                Assert.Equal(TypeKind.Struct, lookupType.TypeKind);
                Assert.Equal(SpecialType.System_Nullable_T, lookupType.SpecialType);
            };

            ValidateSourceAndMetadata(source, validate);
        }

        // No special type members have type parameters that could (incorrectly) be constrained.

        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void NonPublicWellKnownType()
        {
            var source = @"
namespace System
{
    public class Object
    {
        public Object() { }
    }

    internal class Type : Object
    {
    }

    public class ValueType { }
    public struct Void { }
}
";
            var comp = CreateEmptyCompilation(source);

            var wellKnownType = comp.GetWellKnownType(WellKnownType.System_Type);
            Assert.Equal(TypeKind.Class, wellKnownType.TypeKind);
            Assert.Equal(Accessibility.Internal, wellKnownType.DeclaredAccessibility);

            var lookupType = comp.GetTypeByMetadataName("System.Type");
            Assert.Equal(TypeKind.Class, lookupType.TypeKind);
            Assert.Equal(Accessibility.Internal, lookupType.DeclaredAccessibility);
        }

        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void NonPublicWellKnownType_Nested()
        {
            var sourceTemplate = @"
namespace System.Diagnostics
{{
    {0} class DebuggableAttribute
    {{
        {1} enum DebuggingModes {{ }}
    }}
}}

namespace System
{{
    public class Object {{ }}
    public class ValueType {{ }}
    public class Enum : ValueType {{ }}
    public struct Void {{ }}
    public struct Int32 {{ }}
}}
";
            Action<CSharpCompilation> validate = comp =>
            {
                var wellKnownType = comp.GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes);
                Assert.Equal(TypeKind.Error, wellKnownType.TypeKind);
                Assert.Equal(Accessibility.NotApplicable, wellKnownType.DeclaredAccessibility);

                var lookupType = comp.GetTypeByMetadataName("System.Diagnostics.DebuggableAttribute+DebuggingModes");
                Assert.Equal(TypeKind.Enum, lookupType.TypeKind);
                Assert.NotEqual(Accessibility.NotApplicable, lookupType.DeclaredAccessibility);
            };

            ValidateSourceAndMetadata(string.Format(sourceTemplate, "public", "protected"), validate);
            ValidateSourceAndMetadata(string.Format(sourceTemplate, "public", "private"), validate);
        }

        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void NonPublicWellKnownTypeMember()
        {
            var sourceTemplate = @"
namespace System
{{
    public class Object
    {{
        public Object() {{ }}
    }}

    {0} class Type : Object
    {{
        public static readonly Object Missing = new Object();
    }}

    public static class Math : Object
    {{
        {0} static Double Round(Double d) {{ return d; }}
    }}

    public class ValueType {{ }}
    public struct Void {{ }}
    public struct Double {{ }}
}}
";
            Action<CSharpCompilation> validatePresent = comp =>
            {
                Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_Type__Missing));
                Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_Math__RoundDouble));
                comp.GetDiagnostics();
            };

            validatePresent(CreateEmptyCompilation(string.Format(sourceTemplate, "public")));
            validatePresent(CreateEmptyCompilation(string.Format(sourceTemplate, "internal")));
        }

        // Document the fact that we don't reject type parameters with constraints (yet?).
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void GenericConstraintsOnWellKnownType()
        {
            var source = @"
namespace System
{
    public class Object
    {
        public Object() { }
    }

    namespace Threading.Tasks
    {
        public class Task<T> where T : new()
        {
        }
    }

    public class ValueType { }
    public struct Void { }
}
";
            Action<CSharpCompilation> validate = comp =>
            {
                var wellKnownType = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
                Assert.Equal(TypeKind.Class, wellKnownType.TypeKind);

                var lookupType = comp.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                Assert.Equal(TypeKind.Class, lookupType.TypeKind);
            };

            ValidateSourceAndMetadata(source, validate);
        }

        // Document the fact that we don't reject type parameters with constraints (yet?).
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void GenericConstraintsOnWellKnownTypeMember()
        {
            var sourceTemplate = @"
namespace System
{{
    public class Object
    {{
        public Object() {{ }}
    }}

    namespace Threading
    {{
        public static class Interlocked
        {{
            public static T CompareExchange<T>(ref T t1, T t2, T t3){0}
            {{
                return t1;
            }}
        }}
    }}

    public class ValueType {{ }}
    public struct Void {{ }}
    public struct Int32 {{ }}
    public struct Boolean {{ }}
    public class Attribute {{ }}
    public class AttributeUsageAttribute : Attribute
    {{
        public AttributeUsageAttribute(AttributeTargets t) {{ }}
        public bool AllowMultiple {{ get; set; }}
        public bool Inherited {{ get; set; }}
    }}
    public struct Enum {{ }}
    public enum AttributeTargets {{ }}
}}
";
            Action<CSharpCompilation> validate = comp =>
            {
                Assert.NotNull(comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange_T));
                comp.GetDiagnostics();
            };

            ValidateSourceAndMetadata(string.Format(sourceTemplate, ""), validate);
            ValidateSourceAndMetadata(string.Format(sourceTemplate, " where T : new()"), validate);
        }

        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        [Fact]
        public void PublicVersusInternalWellKnownType()
        {
            var corlibSource = @"
namespace System
{
    public class Object
    {
        public Object() { }
    }

    public class String
    {
    }

    public class Attribute 
    {
    }

    public class ValueType { }
    public struct Void { }
}

namespace System.Runtime.CompilerServices
{
    public class InternalsVisibleToAttribute : Attribute
    {
        public InternalsVisibleToAttribute(String s)
        {
        }
    }
}
";
            var libSourceTemplate = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Test"")]

namespace System
{{
    {0} class Type
    {{
    }}
}}
";

            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var corlibRef = CreateEmptyCompilation(corlibSource, parseOptions: parseOptions).EmitToImageReference(expectedWarnings: new[]
            {
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
            });

            var publicLibRef = CreateEmptyCompilation(string.Format(libSourceTemplate, "public"), new[] { corlibRef }, parseOptions: parseOptions).EmitToImageReference();
            var internalLibRef = CreateEmptyCompilation(string.Format(libSourceTemplate, "internal"), new[] { corlibRef }, parseOptions: parseOptions).EmitToImageReference();

            var comp = CreateEmptyCompilation("", new[] { corlibRef, publicLibRef, internalLibRef }, assemblyName: "Test");

            var wellKnown = comp.GetWellKnownType(WellKnownType.System_Type);
            Assert.NotNull(wellKnown);
            Assert.Equal(TypeKind.Class, wellKnown.TypeKind);
            Assert.Equal(Accessibility.Public, wellKnown.DeclaredAccessibility);

            var lookup = comp.GetTypeByMetadataName("System.Type");
            Assert.Null(lookup); // Ambiguous
        }

        private static void ValidateSourceAndMetadata(string source, Action<CSharpCompilation> validate)
        {
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var comp1 = CreateEmptyCompilation(source, parseOptions: parseOptions);
            validate(comp1);

            var reference = comp1.EmitToImageReference(expectedWarnings: new[]
            {
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
            });

            var comp2 = CreateEmptyCompilation("", new[] { reference }, parseOptions: parseOptions);
            validate(comp2);
        }

        [Fact]
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        public void AllSpecialTypes()
        {
            var comp = CreateEmptyCompilation("", new[] { MscorlibRef_v4_0_30316_17626 });

            for (var special = SpecialType.None + 1; special <= SpecialType.Count; special++)
            {
                var symbol = comp.GetSpecialType(special);
                Assert.NotNull(symbol);

                if (special is SpecialType.System_Runtime_CompilerServices_RuntimeFeature or
                               SpecialType.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute or
                               SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute)
                {
                    Assert.Equal(SymbolKind.ErrorType, symbol.Kind); // Not available
                }
                else
                {
                    Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind);
                }
            }
        }

        [Fact]
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        public void AllSpecialTypeMembers()
        {
            var comp = CreateEmptyCompilation("", [Net461.References.mscorlib]);

            foreach (SpecialMember special in Enum.GetValues(typeof(SpecialMember)))
            {
                if (special == SpecialMember.Count) continue; // Not a real value;

                var symbol = comp.GetSpecialTypeMember(special);
                if (special == SpecialMember.System_String__Concat_2ReadOnlySpans
                    || special == SpecialMember.System_String__Concat_3ReadOnlySpans
                    || special == SpecialMember.System_String__Concat_4ReadOnlySpans
                    || special == SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar
                    || special == SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__DefaultImplementationsOfInterfaces
                    || special == SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__CovariantReturnsOfClasses
                    || special == SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__VirtualStaticsInInterfaces
                    || special == SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__UnmanagedSignatureCallingConvention
                    || special == SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__NumericIntPtr
                    || special == SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__ByRefFields
                    || special == SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__ByRefLikeGenerics
                    || special == SpecialMember.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute__ctor
                    || special == SpecialMember.System_Runtime_CompilerServices_InlineArrayAttribute__ctor
                    || special == SpecialMember.System_ReadOnlySpan_T__ctor_Reference
                    || special == SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__AwaitAwaiter_TAwaiter
                    || special == SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__UnsafeAwaitAwaiter_TAwaiter
                    || special == SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__HandleAsyncEntryPoint_Task
                    || special == SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__HandleAsyncEntryPoint_Task_Int32
                    || special == SpecialMember.System_Runtime_InteropServices_ExtendedLayoutAttribute__ctor
                    )
                {
                    Assert.Null(symbol); // Not available
                }
                else
                {
                    Assert.NotNull(symbol);
                }
            }
        }

        [Fact]
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        public void AllWellKnownTypes()
        {
            var refs = new[]
            {
                MscorlibRef_v4_0_30316_17626,
                SystemRef_v4_0_30319_17929,
                SystemCoreRef_v4_0_30319_17929,
                MsvbRef_v4_0_30319_17929,
                CSharpRef,
                SystemXmlRef,
                SystemXmlLinqRef,
                SystemWindowsFormsRef,
                ValueTupleRef
            }.Concat(WinRtRefs).ToArray();
            var comp = CreateEmptyCompilation("", refs);

            for (var wkt = WellKnownType.First; wkt < WellKnownType.NextAvailable; wkt++)
            {
                switch (wkt)
                {
                    case WellKnownType.Microsoft_VisualBasic_Embedded:
                    case WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators:
                        // Not applicable in C#.
                        continue;
                    case WellKnownType.System_FormattableString:
                    case WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory:
                    case WellKnownType.System_Runtime_CompilerServices_NullableAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_NullableContextAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_NullablePublicOnlyAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_RequiresLocationAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute:
                    case WellKnownType.System_Span_T:
                    case WellKnownType.System_ReadOnlySpan_T:
                    case WellKnownType.System_Collections_Immutable_ImmutableArray_T:
                    case WellKnownType.System_Runtime_CompilerServices_IsUnmanagedAttribute:
                    case WellKnownType.System_Index:
                    case WellKnownType.System_Range:
                    case WellKnownType.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute:
                    case WellKnownType.System_IAsyncDisposable:
                    case WellKnownType.System_Collections_Generic_IAsyncEnumerable_T:
                    case WellKnownType.System_Collections_Generic_IAsyncEnumerator_T:
                    case WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T:
                    case WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus:
                    case WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags:
                    case WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T:
                    case WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource:
                    case WellKnownType.System_Threading_Tasks_ValueTask_T:
                    case WellKnownType.System_Threading_Tasks_ValueTask:
                    case WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder:
                    case WellKnownType.System_Threading_CancellationToken:
                    case WellKnownType.System_Runtime_CompilerServices_SwitchExpressionException:
                    case WellKnownType.System_Runtime_CompilerServices_NativeIntegerAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_IsExternalInit:
                    case WellKnownType.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler:
                    case WellKnownType.System_Runtime_CompilerServices_RequiredMemberAttribute:
                    case WellKnownType.System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_ScopedRefAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_RefSafetyRulesAttribute:
                    case WellKnownType.System_MemoryExtensions:
                    case WellKnownType.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute:
                    case WellKnownType.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_MetadataUpdateOriginalTypeAttribute:
                    case WellKnownType.System_Runtime_InteropServices_MemoryMarshal:
                    case WellKnownType.System_Runtime_CompilerServices_Unsafe:
                    case WellKnownType.System_Runtime_CompilerServices_ParamCollectionAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_ExtensionMarkerAttribute:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray2:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray3:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray4:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray5:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray6:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray7:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray8:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray9:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray10:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray11:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray12:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray13:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray14:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray15:
                    case WellKnownType.System_Runtime_CompilerServices_InlineArray16:
                    case WellKnownType.System_Runtime_CompilerServices_ClosedAttribute:
                        // Not yet in the platform.
                        continue;
                    case WellKnownType.Microsoft_CodeAnalysis_Runtime_Instrumentation:
                    case WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker:
                    case WellKnownType.System_Runtime_CompilerServices_ITuple:
                    case WellKnownType.System_Runtime_CompilerServices_NonNullTypesAttribute:
                    case WellKnownType.Microsoft_CodeAnalysis_EmbeddedAttribute:
                    case WellKnownType.System_Runtime_InteropServices_CollectionsMarshal:
                    case WellKnownType.System_Runtime_InteropServices_ImmutableCollectionsMarshal:
                    case WellKnownType.System_Runtime_CompilerServices_HotReloadException:
                    case WellKnownType.System_Runtime_CompilerServices_MetadataUpdateDeletedAttribute:
                        // Not always available.
                        continue;
                    case WellKnownType.ExtSentinel:
                        // Not a real type
                        continue;
                }

                switch (wkt)
                {
                    case WellKnownType.System_ValueTuple:
                    case WellKnownType.System_ValueTuple_T1:
                    case WellKnownType.System_ValueTuple_T2:
                    case WellKnownType.System_ValueTuple_T3:
                    case WellKnownType.System_ValueTuple_T4:
                    case WellKnownType.System_ValueTuple_T5:
                    case WellKnownType.System_ValueTuple_T6:
                    case WellKnownType.System_ValueTuple_T7:
                    case WellKnownType.System_ValueTuple_TRest:
                        Assert.True(wkt.IsValueTupleType());
                        break;

                    default:
                        Assert.False(wkt.IsValueTupleType());
                        break;
                }

                var symbol = comp.GetWellKnownType(wkt);
                Assert.NotNull(symbol);
                Assert.True(symbol.Kind != SymbolKind.ErrorType, $"{wkt} should not be an error type");
            }
        }

        [Fact]
        public void AllWellKnownTypesBeforeCSharp7()
        {
            foreach (var type in new[] {
                            WellKnownType.System_Math,
                            WellKnownType.System_Attribute,
                            WellKnownType.System_CLSCompliantAttribute,
                            WellKnownType.System_Convert,
                            WellKnownType.System_Collections_Immutable_ImmutableArray_T,
                            WellKnownType.System_Exception,
                            WellKnownType.System_FlagsAttribute,
                            WellKnownType.System_FormattableString,
                            WellKnownType.System_Guid,
                            WellKnownType.System_IFormattable,
                            WellKnownType.System_MarshalByRefObject,
                            WellKnownType.System_Type,
                            WellKnownType.System_Reflection_AssemblyKeyFileAttribute,
                            WellKnownType.System_Reflection_AssemblyKeyNameAttribute,
                            WellKnownType.System_Reflection_MethodInfo,
                            WellKnownType.System_Reflection_ConstructorInfo,
                            WellKnownType.System_Reflection_MethodBase,
                            WellKnownType.System_Reflection_FieldInfo,
                            WellKnownType.System_Reflection_MemberInfo,
                            WellKnownType.System_Reflection_Missing,
                            WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory,
                            WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,
                            WellKnownType.System_Runtime_ExceptionServices_ExceptionDispatchInfo,
                            WellKnownType.System_Runtime_InteropServices_StructLayoutAttribute,
                            WellKnownType.System_Runtime_InteropServices_UnknownWrapper,
                            WellKnownType.System_Runtime_InteropServices_DispatchWrapper,
                            WellKnownType.System_Runtime_InteropServices_CallingConvention,
                            WellKnownType.System_Runtime_InteropServices_ClassInterfaceAttribute,
                            WellKnownType.System_Runtime_InteropServices_ClassInterfaceType,
                            WellKnownType.System_Runtime_InteropServices_CoClassAttribute,
                            WellKnownType.System_Runtime_InteropServices_ComAwareEventInfo,
                            WellKnownType.System_Runtime_InteropServices_ComEventInterfaceAttribute,
                            WellKnownType.System_Runtime_InteropServices_ComInterfaceType,
                            WellKnownType.System_Runtime_InteropServices_ComSourceInterfacesAttribute,
                            WellKnownType.System_Runtime_InteropServices_ComVisibleAttribute,
                            WellKnownType.System_Runtime_InteropServices_DispIdAttribute,
                            WellKnownType.System_Runtime_InteropServices_GuidAttribute,
                            WellKnownType.System_Runtime_InteropServices_InterfaceTypeAttribute,
                            WellKnownType.System_Runtime_InteropServices_Marshal,
                            WellKnownType.System_Runtime_InteropServices_TypeIdentifierAttribute,
                            WellKnownType.System_Runtime_InteropServices_BestFitMappingAttribute,
                            WellKnownType.System_Runtime_InteropServices_DefaultParameterValueAttribute,
                            WellKnownType.System_Runtime_InteropServices_LCIDConversionAttribute,
                            WellKnownType.System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute,
                            WellKnownType.System_Activator,
                            WellKnownType.System_Threading_Tasks_Task,
                            WellKnownType.System_Threading_Tasks_Task_T,
                            WellKnownType.System_Threading_Interlocked,
                            WellKnownType.System_Threading_Monitor,
                            WellKnownType.System_Threading_Thread,
                            WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,
                            WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,
                            WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfoFlags,
                            WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                            WellKnownType.Microsoft_VisualBasic_CallType,
                            WellKnownType.Microsoft_VisualBasic_Embedded,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_Utils,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_StringType,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_IncompleteInitialization,
                            WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,
                            WellKnownType.Microsoft_VisualBasic_CompareMethod,
                            WellKnownType.Microsoft_VisualBasic_Strings,
                            WellKnownType.Microsoft_VisualBasic_ErrObject,
                            WellKnownType.Microsoft_VisualBasic_FileSystem,
                            WellKnownType.Microsoft_VisualBasic_ApplicationServices_ApplicationBase,
                            WellKnownType.Microsoft_VisualBasic_ApplicationServices_WindowsFormsApplicationBase,
                            WellKnownType.Microsoft_VisualBasic_Information,
                            WellKnownType.Microsoft_VisualBasic_Interaction,

                            WellKnownType.System_Func_T,
                            WellKnownType.System_Func_T2,
                            WellKnownType.System_Func_T3,
                            WellKnownType.System_Func_T4,
                            WellKnownType.System_Func_T5,
                            WellKnownType.System_Func_T6,
                            WellKnownType.System_Func_T7,
                            WellKnownType.System_Func_T8,
                            WellKnownType.System_Func_T9,
                            WellKnownType.System_Func_T10,
                            WellKnownType.System_Func_T11,
                            WellKnownType.System_Func_T12,
                            WellKnownType.System_Func_T13,
                            WellKnownType.System_Func_T14,
                            WellKnownType.System_Func_T15,
                            WellKnownType.System_Func_T16,
                            WellKnownType.System_Func_T17,

                            WellKnownType.System_Action,
                            WellKnownType.System_Action_T,
                            WellKnownType.System_Action_T2,
                            WellKnownType.System_Action_T3,
                            WellKnownType.System_Action_T4,
                            WellKnownType.System_Action_T5,
                            WellKnownType.System_Action_T6,
                            WellKnownType.System_Action_T7,
                            WellKnownType.System_Action_T8,
                            WellKnownType.System_Action_T9,
                            WellKnownType.System_Action_T10,
                            WellKnownType.System_Action_T11,
                            WellKnownType.System_Action_T12,
                            WellKnownType.System_Action_T13,
                            WellKnownType.System_Action_T14,
                            WellKnownType.System_Action_T15,
                            WellKnownType.System_Action_T16,

                            WellKnownType.System_AttributeUsageAttribute,
                            WellKnownType.System_ParamArrayAttribute,
                            WellKnownType.System_NonSerializedAttribute,
                            WellKnownType.System_STAThreadAttribute,
                            WellKnownType.System_Reflection_DefaultMemberAttribute,
                            WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute,
                            WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute,
                            WellKnownType.System_Runtime_CompilerServices_IUnknownConstantAttribute,
                            WellKnownType.System_Runtime_CompilerServices_IDispatchConstantAttribute,
                            WellKnownType.System_Runtime_CompilerServices_ExtensionAttribute,
                            WellKnownType.System_Runtime_CompilerServices_INotifyCompletion,
                            WellKnownType.System_Runtime_CompilerServices_InternalsVisibleToAttribute,
                            WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute,
                            WellKnownType.System_Runtime_CompilerServices_AccessedThroughPropertyAttribute,
                            WellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute,
                            WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute,
                            WellKnownType.System_Runtime_CompilerServices_UnsafeValueTypeAttribute,
                            WellKnownType.System_Runtime_CompilerServices_FixedBufferAttribute,
                            WellKnownType.System_Runtime_CompilerServices_DynamicAttribute,
                            WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                            WellKnownType.System_Runtime_CompilerServices_CallSite,
                            WellKnownType.System_Runtime_CompilerServices_CallSite_T,

                            WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,
                            WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,
                            WellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal,

                            WellKnownType.Windows_Foundation_IAsyncAction,
                            WellKnownType.Windows_Foundation_IAsyncActionWithProgress_T,
                            WellKnownType.Windows_Foundation_IAsyncOperation_T,
                            WellKnownType.Windows_Foundation_IAsyncOperationWithProgress_T2,

                            WellKnownType.System_Diagnostics_Debugger,
                            WellKnownType.System_Diagnostics_DebuggerDisplayAttribute,
                            WellKnownType.System_Diagnostics_DebuggerNonUserCodeAttribute,
                            WellKnownType.System_Diagnostics_DebuggerHiddenAttribute,
                            WellKnownType.System_Diagnostics_DebuggerBrowsableAttribute,
                            WellKnownType.System_Diagnostics_DebuggerStepThroughAttribute,
                            WellKnownType.System_Diagnostics_DebuggerBrowsableState,
                            WellKnownType.System_Diagnostics_DebuggableAttribute,
                            WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes,

                            WellKnownType.System_ComponentModel_DesignerSerializationVisibilityAttribute,

                            WellKnownType.System_IEquatable_T,

                            WellKnownType.System_Collections_IList,
                            WellKnownType.System_Collections_ICollection,
                            WellKnownType.System_Collections_Generic_EqualityComparer_T,
                            WellKnownType.System_Collections_Generic_List_T,
                            WellKnownType.System_Collections_Generic_IDictionary_KV,
                            WellKnownType.System_Collections_Generic_IReadOnlyDictionary_KV,
                            WellKnownType.System_Collections_ObjectModel_Collection_T,
                            WellKnownType.System_Collections_ObjectModel_ReadOnlyCollection_T,
                            WellKnownType.System_Collections_Specialized_INotifyCollectionChanged,
                            WellKnownType.System_ComponentModel_INotifyPropertyChanged,
                            WellKnownType.System_ComponentModel_EditorBrowsableAttribute,
                            WellKnownType.System_ComponentModel_EditorBrowsableState,

                            WellKnownType.System_Linq_Enumerable,
                            WellKnownType.System_Linq_Expressions_Expression,
                            WellKnownType.System_Linq_Expressions_Expression_T,
                            WellKnownType.System_Linq_Expressions_ParameterExpression,
                            WellKnownType.System_Linq_Expressions_ElementInit,
                            WellKnownType.System_Linq_Expressions_MemberBinding,
                            WellKnownType.System_Linq_Expressions_ExpressionType,
                            WellKnownType.System_Linq_IQueryable,
                            WellKnownType.System_Linq_IQueryable_T,

                            WellKnownType.System_Xml_Linq_Extensions,
                            WellKnownType.System_Xml_Linq_XAttribute,
                            WellKnownType.System_Xml_Linq_XCData,
                            WellKnownType.System_Xml_Linq_XComment,
                            WellKnownType.System_Xml_Linq_XContainer,
                            WellKnownType.System_Xml_Linq_XDeclaration,
                            WellKnownType.System_Xml_Linq_XDocument,
                            WellKnownType.System_Xml_Linq_XElement,
                            WellKnownType.System_Xml_Linq_XName,
                            WellKnownType.System_Xml_Linq_XNamespace,
                            WellKnownType.System_Xml_Linq_XObject,
                            WellKnownType.System_Xml_Linq_XProcessingInstruction,

                            WellKnownType.System_Security_UnverifiableCodeAttribute,
                            WellKnownType.System_Security_Permissions_SecurityAction,
                            WellKnownType.System_Security_Permissions_SecurityAttribute,
                            WellKnownType.System_Security_Permissions_SecurityPermissionAttribute,

                            WellKnownType.System_NotSupportedException,

                            WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion,
                            WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,
                            WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,
                            WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,
                            WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,
                            WellKnownType.System_Runtime_CompilerServices_AsyncStateMachineAttribute,
                            WellKnownType.System_Runtime_CompilerServices_IteratorStateMachineAttribute,

                            WellKnownType.System_Windows_Forms_Form,
                            WellKnownType.System_Windows_Forms_Application,

                            WellKnownType.System_Environment,

                            WellKnownType.System_Runtime_GCLatencyMode}
                )
            {
                Assert.True(type <= WellKnownType.CSharp7Sentinel);
            }

            // There were 200 well-known types prior to CSharp7
            Assert.Equal(200, (int)(WellKnownType.CSharp7Sentinel - WellKnownType.First - 1 /* WellKnownTypes.ExtSentinel is before CSharp7Sentinel */));
        }

        [Fact]
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        public void AllWellKnownTypeMembers()
        {
            var refs = new[]
            {
                MscorlibRef_v4_0_30316_17626,
                SystemRef_v4_0_30319_17929,
                SystemCoreRef_v4_0_30319_17929,
                MsvbRef_v4_0_30319_17929,
                CSharpDesktopRef,
                SystemXmlRef,
                SystemXmlLinqRef,
                SystemWindowsFormsRef,
                ValueTupleRef
            }.Concat(WinRtRefs).ToArray();
            var comp = CreateEmptyCompilation("", refs);

            foreach (WellKnownMember wkm in Enum.GetValues(typeof(WellKnownMember)))
            {
                switch (wkm)
                {
                    case WellKnownMember.Count:
                        // Not a real value;
                        continue;
                    case WellKnownMember.Microsoft_VisualBasic_Embedded__ctor:
                    case WellKnownMember.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean:
                        // C# can't embed VB core.
                        continue;
                    case WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorByte:
                    case WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags:
                    case WellKnownMember.System_Runtime_CompilerServices_NullableContextAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_NullablePublicOnlyAttribute__ctor:
                    case WellKnownMember.System_Span_T__ctor_Pointer:
                    case WellKnownMember.System_Span_T__ctor_Array:
                    case WellKnownMember.System_Span_T__get_Item:
                    case WellKnownMember.System_Span_T__get_Length:
                    case WellKnownMember.System_Span_T__Slice_Int_Int:
                    case WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer:
                    case WellKnownMember.System_ReadOnlySpan_T__ctor_Array:
                    case WellKnownMember.System_ReadOnlySpan_T__ctor_Array_Start_Length:
                    case WellKnownMember.System_ReadOnlySpan_T__get_Item:
                    case WellKnownMember.System_ReadOnlySpan_T__get_Length:
                    case WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int:
                    case WellKnownMember.System_Index__ctor:
                    case WellKnownMember.System_Index__GetOffset:
                    case WellKnownMember.System_Range__ctor:
                    case WellKnownMember.System_Range__StartAt:
                    case WellKnownMember.System_Range__EndAt:
                    case WellKnownMember.System_Range__get_All:
                    case WellKnownMember.System_Range__get_Start:
                    case WellKnownMember.System_Range__get_End:
                    case WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__GetSubArray_T:
                    case WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute__ctor:
                    case WellKnownMember.System_IAsyncDisposable__DisposeAsync:
                    case WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator:
                    case WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync:
                    case WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current:
                    case WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__get_Version:
                    case WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult:
                    case WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus:
                    case WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted:
                    case WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__Reset:
                    case WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetResult:
                    case WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetException:
                    case WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult:
                    case WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus:
                    case WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted:
                    case WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetResult:
                    case WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetStatus:
                    case WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__OnCompleted:
                    case WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorSourceAndToken:
                    case WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorValue:
                    case WellKnownMember.System_Threading_Tasks_ValueTask__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitOnCompleted:
                    case WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitUnsafeOnCompleted:
                    case WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Complete:
                    case WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Create:
                    case WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__MoveNext_T:
                    case WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject:
                    case WellKnownMember.System_Runtime_CompilerServices_NativeIntegerAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_NativeIntegerAttribute__ctorTransformFlags:
                    case WellKnownMember.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler__ToStringAndClear:
                    case WellKnownMember.System_Runtime_CompilerServices_RequiredMemberAttribute__ctor:
                    case WellKnownMember.System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_ScopedRefAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_RefSafetyRulesAttribute__ctor:
                    case WellKnownMember.System_MemoryExtensions__SequenceEqual_Span_T:
                    case WellKnownMember.System_MemoryExtensions__SequenceEqual_ReadOnlySpan_T:
                    case WellKnownMember.System_MemoryExtensions__AsSpan_String:
                    case WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor:
                    case WellKnownMember.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_MetadataUpdateOriginalTypeAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle:
                    case WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan:
                    case WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan:
                    case WellKnownMember.System_Runtime_CompilerServices_Unsafe__Add_T:
                    case WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T:
                    case WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T:
                    case WellKnownMember.System_Runtime_CompilerServices_RequiresLocationAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_ExtensionMarkerAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_ClosedAttribute__ctor:
                        // Not yet in the platform.
                        continue;
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningSingleFile:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningMultipleFiles:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogMethodEntry:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLambdaEntry:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineMethodEntry:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineLambdaEntry:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogReturn:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreBoolean:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreByte:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt16:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt32:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt64:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreSingle:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDouble:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDecimal:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreString:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreObject:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStorePointer:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUnmanaged:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreParameterAlias:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreBoolean:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreByte:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt16:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt32:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt64:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreSingle:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreDouble:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreDecimal:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreString:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreObject:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStorePointer:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUnmanaged:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreParameterAlias:
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreLocalAlias:
                    case WellKnownMember.System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor:
                    case WellKnownMember.System_Runtime_CompilerServices_ITuple__get_Item:
                    case WellKnownMember.System_Runtime_CompilerServices_ITuple__get_Length:
                    case WellKnownMember.System_Runtime_InteropServices_CollectionsMarshal__AsSpan_T:
                    case WellKnownMember.System_Runtime_InteropServices_CollectionsMarshal__SetCount_T:
                    case WellKnownMember.System_Runtime_InteropServices_ImmutableCollectionsMarshal__AsImmutableArray_T:
                    case WellKnownMember.System_Span_T__ToArray:
                    case WellKnownMember.System_ReadOnlySpan_T__ToArray:
                    case WellKnownMember.System_Span_T__CopyTo_Span_T:
                    case WellKnownMember.System_ReadOnlySpan_T__CopyTo_Span_T:
                    case WellKnownMember.System_Collections_Immutable_ImmutableArray_T__AsSpan:
                    case WellKnownMember.System_Collections_Immutable_ImmutableArray_T__Empty:
                    case WellKnownMember.System_Span_T__ctor_ref_T:
                    case WellKnownMember.System_ReadOnlySpan_T__ctor_ref_readonly_T:
                    case WellKnownMember.System_Runtime_CompilerServices_HotReloadException__ctorStringInt32:
                    case WellKnownMember.System_Runtime_CompilerServices_MetadataUpdateDeletedAttribute__ctor:
                        // Not always available.
                        continue;
                }
                if (wkm == WellKnownMember.Count) continue; // Not a real value.

                var symbol = comp.GetWellKnownTypeMember(wkm);
                Assert.True((object)symbol != null, $"Unexpected null for {wkm}");
            }
        }

        [Fact, WorkItem(377890, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=377890")]
        public void System_IntPtr__op_Explicit_FromInt32()
        {
            string source = @"
using System;

public class MyClass
{
    static void Main()
    {
        ((IntPtr)0).GetHashCode();
    }
}
";
            var comp = CreateCompilation(source);
            comp.MakeMemberMissing(SpecialMember.System_IntPtr__op_Explicit_FromInt32);
            comp.VerifyEmitDiagnostics(
                // (8,10): error CS0656: Missing compiler required member 'System.IntPtr.op_Explicit'
                //         ((IntPtr)0).GetHashCode();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(IntPtr)0").WithArguments("System.IntPtr", "op_Explicit").WithLocation(8, 10)
                );
        }

        [Fact]
        public void System_Delegate__Combine()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

namespace RoslynAsyncDelegate
{
    class Program
    {
        static EventHandler MyEvent;

        static void Main(string[] args)
        {
           MyEvent += async delegate { await Task.Delay(0); };
        }
    }
}

";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
            compilation.MakeMemberMissing(SpecialMember.System_Delegate__Combine);
            compilation.VerifyEmitDiagnostics(
                // (13,12): error CS0656: Missing compiler required member 'System.Delegate.Combine'
                //            MyEvent += async delegate { await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "MyEvent += async delegate { await Task.Delay(0); }").WithArguments("System.Delegate", "Combine").WithLocation(13, 12)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_01()
        {
            string source = @"
using System;

public struct S
{
    public static implicit operator int(S n) // 1 native compiler
    {
        Console.WriteLine(1);
        return 0;
    }

    public static implicit operator int?(S n) // 2 Roslyn compiler
    {
        Console.WriteLine(2);
        return null;
    }

    public static void Main()
    {
        int? qa = 5;
        S b = default(S);
        var sum = qa + b;
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (20,19): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         int? qa = 5;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "5").WithArguments("System.Nullable`1", ".ctor").WithLocation(20, 19),
                // (22,19): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var sum = qa + b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "qa + b").WithArguments("System.Nullable`1", ".ctor").WithLocation(22, 19)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_01()
        {
            string source = @"
using System;

public struct S
{
    public static implicit operator int(S n) // 1 native compiler
    {
        Console.WriteLine(1);
        return 0;
    }

    public static implicit operator int?(S n) // 2 Roslyn compiler
    {
        Console.WriteLine(2);
        return null;
    }

    public static void Main()
    {
        int? qa = 5;
        S b = default(S);
        var sum = qa + b;
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (22,19): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         var sum = qa + b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "qa + b").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(22, 19),
                // (22,19): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         var sum = qa + b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "qa + b").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(22, 19)
                );
        }

        [Fact]
        public void System_Nullable_T_get_HasValue_01()
        {
            string source = @"
using System;

public struct S
{
    public static implicit operator int(S n) // 1 native compiler
    {
        Console.WriteLine(1);
        return 0;
    }

    public static implicit operator int?(S n) // 2 Roslyn compiler
    {
        Console.WriteLine(2);
        return null;
    }

    public static void Main()
    {
        int? qa = 5;
        S b = default(S);
        var sum = qa + b;
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_get_HasValue);
            compilation.VerifyEmitDiagnostics(
                // (22,19): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //         var sum = qa + b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "qa + b").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(22, 19),
                // (22,19): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //         var sum = qa + b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "qa + b").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(22, 19)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_02()
        {
            string source = @"
using System;
namespace Test
{
    static class Program
    {
        static void Main()
        {
            int? i = 123;
            C c = (C)i;
        }
    }

    public class C
    {
        public readonly int v;
        public C(int v) { this.v = v; }
        public static implicit operator C(int v)
        {
            Console.Write(v);
            return new C(v);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (10,19): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //             C c = (C)i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(C)i").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(10, 19)
                );
        }

        [Fact]
        public void System_Nullable_T_get_Value()
        {
            var source =
@"
using System;

class C
{
    static void Test()
    {
        byte? b = 0;
        IntPtr p = (IntPtr)b;
        Console.WriteLine(p);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_get_Value);
            compilation.VerifyEmitDiagnostics(
                // (9,28): error CS0656: Missing compiler required member 'System.Nullable`1.get_Value'
                //         IntPtr p = (IntPtr)b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "b").WithArguments("System.Nullable`1", "get_Value").WithLocation(9, 28)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_02()
        {
            var source =
@"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine((IntPtr?)M_int());
        Console.WriteLine((IntPtr?)M_int(42));
        Console.WriteLine((IntPtr?)M_long());
        Console.WriteLine((IntPtr?)M_long(300));
    }

    static int? M_int(int? p = null) { return p; } 
    static long? M_long(long? p = null) { return p; } 
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (8,27): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Console.WriteLine((IntPtr?)M_int());
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(IntPtr?)M_int()").WithArguments("System.Nullable`1", ".ctor").WithLocation(8, 27),
                // (9,42): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Console.WriteLine((IntPtr?)M_int(42));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "42").WithArguments("System.Nullable`1", ".ctor").WithLocation(9, 42),
                // (9,27): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Console.WriteLine((IntPtr?)M_int(42));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(IntPtr?)M_int(42)").WithArguments("System.Nullable`1", ".ctor").WithLocation(9, 27),
                // (10,27): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Console.WriteLine((IntPtr?)M_long());
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(IntPtr?)M_long()").WithArguments("System.Nullable`1", ".ctor").WithLocation(10, 27),
                // (11,43): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Console.WriteLine((IntPtr?)M_long(300));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "300").WithArguments("System.Nullable`1", ".ctor").WithLocation(11, 43),
                // (11,27): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Console.WriteLine((IntPtr?)M_long(300));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(IntPtr?)M_long(300)").WithArguments("System.Nullable`1", ".ctor").WithLocation(11, 27)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_03()
        {
            var source =
@"

using System;

class Class1
{
    static void Main()
    {
        MyClass b = (int?)1;
    }
}

class MyClass
{
    public static implicit operator MyClass(decimal Value)
    {
        return new MyClass();
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (9,21): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         MyClass b = (int?)1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(int?)1").WithArguments("System.Nullable`1", ".ctor").WithLocation(9, 21)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_04()
        {
            var source1 = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class Test
{
    public static void Generic<T>([Optional][DecimalConstant(0, 0, 0, 0, 50)] T x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Decimal([Optional][DecimalConstant(0, 0, 0, 0, 50)] Decimal x)
    {
        Console.WriteLine(x.ToString());
    }

    public static void NullableDecimal([Optional][DecimalConstant(0, 0, 0, 0, 50)] Decimal? x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Object([Optional][DecimalConstant(0, 0, 0, 0, 50)] object x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void String([Optional][DecimalConstant(0, 0, 0, 0, 50)] string x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Int32([Optional][DecimalConstant(0, 0, 0, 0, 50)] int x)
    {
        Console.WriteLine(x.ToString());
    }

    public static void IComparable([Optional][DecimalConstant(0, 0, 0, 0, 50)] IComparable x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void ValueType([Optional][DecimalConstant(0, 0, 0, 0, 50)] ValueType x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }
}
";

            var source2 = @"
class Program
{
    public static void Main()
    {
        // Respects default value
        Test.Generic<decimal>();    
        Test.Generic<decimal?>();   
        Test.Generic<object>();             
        Test.Decimal();                    
        Test.NullableDecimal();            
        Test.Object();                      
        Test.IComparable();                 
        Test.ValueType();                   
        Test.Int32();                       

        // Null, since not convertible
        Test.Generic<string>();             
        Test.String();                      
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source1 + source2);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (55,9): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Test.Generic<decimal?>();   
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "Test.Generic<decimal?>()").WithArguments("System.Nullable`1", ".ctor").WithLocation(55, 9),
                // (58,9): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         Test.NullableDecimal();            
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "Test.NullableDecimal()").WithArguments("System.Nullable`1", ".ctor").WithLocation(58, 9)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_04()
        {
            var source =
@"

using System;

class Class1
{
    static void Main()
    {
        int? a = 1;
        a.ToString();
        MyClass b = a;
        b.ToString();
    }
}

class MyClass
{
    public static implicit operator MyClass(decimal Value)
    {
        return new MyClass();
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (11,21): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         MyClass b = a;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "a").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(11, 21),
                // (11,21): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         MyClass b = a;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "a").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(11, 21)
                );
        }

        [Fact]
        public void System_Nullable_T_get_HasValue_02()
        {
            var source =
@"

using System;

class Class1
{
    static void Main()
    {
        int? a = 1;
        a.ToString();
        MyClass b = a;
        b.ToString();
    }
}

class MyClass
{
    public static implicit operator MyClass(decimal Value)
    {
        Console.WriteLine(""Value is: "" + Value);
        return new MyClass();
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_get_HasValue);
            compilation.VerifyEmitDiagnostics(
                // (11,21): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //         MyClass b = a;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "a").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(11, 21)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_03()
        {
            string source = @"using System;

namespace Test
{
    static class Program
    {
        static void Main()
        {
            S.v = 0;
            S? S2 = 123;                  // not lifted, int=>int?, int?=>S, S=>S?
            Console.WriteLine(S.v == 123);
        }
    }

    public struct S
    {
        public static int v;
        // s == null, return v = -1
        public static implicit operator S(int? s)
        {
            Console.Write(""Imp S::int? -> S "");
            S ss = new S();
            S.v = s ?? -1;
            return ss;
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);

            // We use more optimal `GetValueOrDefault(defaultValue)` member for this case, so no error about missing `GetValueOrDefault()` is reported
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void System_String__ConcatObjectObject()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

class Class1
{
    static void Main()
    {
        Expression<Func<object, string>> e = x => ""X = "" + x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef });
            compilation.MakeMemberMissing(SpecialMember.System_String__ConcatObjectObject);
            compilation.VerifyEmitDiagnostics(
                // (9,51): error CS0656: Missing compiler required member 'System.String.Concat'
                //         Expression<Func<object, string>> e = x => "X = " + x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""X = "" + x").WithArguments("System.String", "Concat").WithLocation(9, 51)
                );
        }

        [Fact]
        public void System_String__ConcatStringStringString()
        {
            string source = @"
using System;
struct S
{
    private string str;
    public S(char chr) { this.str = chr.ToString(); }
    public S(string str) { this.str = str; }
    public static S operator + (S x, S y) { return new S(x.str + '+' + y.str); }
}

class C
{
    static void Main()
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_String__ConcatStringStringString);
            compilation.VerifyEmitDiagnostics(
                // (8,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator + (S x, S y) { return new S(x.str + '+' + y.str); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.str + '+' + y.str").WithArguments("System.String", "Concat").WithLocation(8, 58)
                );
        }

        [Fact]
        public void System_String__ConcatStringStringStringString()
        {
            string source = @"
using System;
struct S
{
    private string str;
    public S(char chr) { this.str = chr.ToString(); }
    public S(string str) { this.str = str; }
    public static S operator + (S x, S y) { return new S('(' + x.str + '+' + y.str); }
}

class C
{
    static void Main()
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_String__ConcatStringStringStringString);
            compilation.VerifyEmitDiagnostics(
                // (8,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator + (S x, S y) { return new S('(' + x.str + '+' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '+' + y.str").WithArguments("System.String", "Concat").WithLocation(8, 58)
                );
        }

        [Fact]
        public void System_String__ConcatStringArray()
        {
            string source = @"
using System;
struct S
{
    private string str;
    public S(char chr) { this.str = chr.ToString(); }
    public S(string str) { this.str = str; }
    public static S operator + (S x, S y) { return new S('(' + x.str + '+' + y.str + ')'); }
    public static S operator - (S x, S y) { return new S('(' + x.str + '-' + y.str + ')'); }
    public static S operator % (S x, S y) { return new S('(' + x.str + '%' + y.str + ')'); }
    public static S operator / (S x, S y) { return new S('(' + x.str + '/' + y.str + ')'); }
    public static S operator * (S x, S y) { return new S('(' + x.str + '*' + y.str + ')'); }
    public static S operator & (S x, S y) { return new S('(' + x.str + '&' + y.str + ')'); }
    public static S operator | (S x, S y) { return new S('(' + x.str + '|' + y.str + ')'); }
    public static S operator ^ (S x, S y) { return new S('(' + x.str + '^' + y.str + ')'); }
    public static S operator << (S x, int y) { return new S('(' + x.str + '<' + '<' + y.ToString() + ')'); }
    public static S operator >> (S x, int y) { return new S('(' + x.str + '>' + '>' + y.ToString() + ')'); }
    public static S operator >= (S x, S y) { return new S('(' + x.str + '>' + '=' + y.str + ')'); }
    public static S operator <= (S x, S y) { return new S('(' + x.str + '<' + '=' + y.str + ')'); }
    public static S operator > (S x, S y) { return new S('(' + x.str + '>' + y.str + ')'); }
    public static S operator < (S x, S y) { return new S('(' + x.str + '<' + y.str + ')'); }
    public override string ToString() { return this.str; }
}

class C
{
    static void Main()
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_String__ConcatStringArray);
            compilation.VerifyEmitDiagnostics(
                // (8,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator + (S x, S y) { return new S('(' + x.str + '+' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '+' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(8, 58),
                // (9,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator - (S x, S y) { return new S('(' + x.str + '-' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '-' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(9, 58),
                // (10,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator % (S x, S y) { return new S('(' + x.str + '%' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '%' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(10, 58),
                // (11,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator / (S x, S y) { return new S('(' + x.str + '/' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '/' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(11, 58),
                // (12,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator * (S x, S y) { return new S('(' + x.str + '*' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '*' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(12, 58),
                // (13,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator & (S x, S y) { return new S('(' + x.str + '&' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '&' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(13, 58),
                // (14,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator | (S x, S y) { return new S('(' + x.str + '|' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '|' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(14, 58),
                // (15,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator ^ (S x, S y) { return new S('(' + x.str + '^' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '^' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(15, 58),
                // (16,61): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator << (S x, int y) { return new S('(' + x.str + '<' + '<' + y.ToString() + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '<' + '<' + y.ToString() + ')'").WithArguments("System.String", "Concat").WithLocation(16, 61),
                // (17,61): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator >> (S x, int y) { return new S('(' + x.str + '>' + '>' + y.ToString() + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '>' + '>' + y.ToString() + ')'").WithArguments("System.String", "Concat").WithLocation(17, 61),
                // (18,59): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator >= (S x, S y) { return new S('(' + x.str + '>' + '=' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '>' + '=' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(18, 59),
                // (19,59): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator <= (S x, S y) { return new S('(' + x.str + '<' + '=' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '<' + '=' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(19, 59),
                // (20,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator > (S x, S y) { return new S('(' + x.str + '>' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '>' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(20, 58),
                // (21,58): error CS0656: Missing compiler required member 'System.String.Concat'
                //     public static S operator < (S x, S y) { return new S('(' + x.str + '<' + y.str + ')'); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "'(' + x.str + '<' + y.str + ')'").WithArguments("System.String", "Concat").WithLocation(21, 58)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_05()
        {
            string source =
@"
struct S
{
    public static int operator +(S s) { return 1; }
    public static void Main()
    {
        S s = new S();
        S? sq = s;
        var j = +sq;
        System.Console.WriteLine(j);
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (9,17): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         var j = +sq;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "+sq").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(9, 17)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_05()
        {
            string source =
@"
struct S
{
    public static int operator +(S s) { return 1; }
    public static void Main()
    {
        S s = new S();
        S? sq = s;
        var j = +sq;
        System.Console.WriteLine(j);
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (8,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         S? sq = s;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "s").WithArguments("System.Nullable`1", ".ctor").WithLocation(8, 17),
                // (9,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var j = +sq;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "+sq").WithArguments("System.Nullable`1", ".ctor").WithLocation(9, 17)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_06()
        {
            string source =
@"
class C
{
  public readonly int? i;
  public C(int? i) { this.i = i; }
  public static implicit operator int?(C c) { return c.i; }
  public static implicit operator C(int? s) { return new C(s); }
  static void Main()
  {
    C c = new C(null);
    c++;
    System.Console.WriteLine(object.ReferenceEquals(c, null) ? 1 : 0);
  }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (11,5): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //     c++;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c++").WithArguments("System.Nullable`1", ".ctor").WithLocation(11, 5)
                );
        }

        [Fact]
        public void System_Decimal__op_Multiply()
        {
            string source = @"
using System;
class Program
{       
    static void Main()
    {
        Func<decimal?, decimal?> lambda = a => { return checked(a * a); };
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Decimal__op_Multiply);
            compilation.VerifyEmitDiagnostics(
                // (7,65): error CS0656: Missing compiler required member 'System.Decimal.op_Multiply'
                //         Func<decimal?, decimal?> lambda = a => { return checked(a * a); };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "a * a").WithArguments("System.Decimal", "op_Multiply").WithLocation(7, 65)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_06()
        {
            string source = @"
using System;

struct S : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine(123);
    }

    static void Main()
    {
        using (S? r = new S())
        {
            Console.Write(r);
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (13,9): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         using (S? r = new S())
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"using (S? r = new S())
        {
            Console.Write(r);
        }").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(13, 9)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_07()
        {
            string source = @"
using System;
class C
{
  static void Main()
  {
    decimal q = 10;
    decimal? x = 10;

    T(2, (x++).Value == (q++));
  }

  static void T(int line, bool b)
  {
  }
}";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (10,11): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //     T(2, (x++).Value == (q++));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x++").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(10, 11)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_07()
        {
            string source = @"
using System;
class C
{
  static void Main()
  {
    decimal q = 10;
    decimal? x = 10;

    T(2, (x++).Value == (q++));
  }

  static void T(int line, bool b)
  {
  }
}";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (8,18): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //     decimal? x = 10;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "10").WithArguments("System.Nullable`1", ".ctor").WithLocation(8, 18),
                // (10,11): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //     T(2, (x++).Value == (q++));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x++").WithArguments("System.Nullable`1", ".ctor").WithLocation(10, 11),
                // (10,11): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //     T(2, (x++).Value == (q++));
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x++").WithArguments("System.Nullable`1", ".ctor").WithLocation(10, 11)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_08()
        {
            string source = @"
using System;
struct S
{
  public int x;
  public S(int x) { this.x = x; }
  public static S operator ++(S s) { return new S(s.x + 1); }
  public static S operator --(S s) { return new S(s.x - 1); }
}

class C
{
  static void Main()
  {
    S? n = new S(1);
    S s = new S(1);

    T(2, (n++).Value.x == (s++).x);
  }

  static void T(int line, bool b)
  {
  }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (18,11): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //     T(2, (n++).Value.x == (s++).x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "n++").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(18, 11)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_08()
        {
            string source = @"
using System;
struct S
{
  public int x;
  public S(int x) { this.x = x; }
  public static S operator ++(S s) { return new S(s.x + 1); }
  public static S operator --(S s) { return new S(s.x - 1); }
}

class C
{
  static void Main()
  {
    S? n = new S(1);
    S s = new S(1);

    T(2, (n++).Value.x == (s++).x);
  }

  static void T(int line, bool b)
  {
  }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (15,12): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //     S? n = new S(1);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "new S(1)").WithArguments("System.Nullable`1", ".ctor").WithLocation(15, 12),
                // (18,11): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //     T(2, (n++).Value.x == (s++).x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "n++").WithArguments("System.Nullable`1", ".ctor").WithLocation(18, 11)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_09()
        {
            string source = @"
using System;
class C
{
    
    static void T(int x, bool? b) {}

    static void Main()
    {
        bool bt = true;
        bool? bnt = bt;

        T(1, true & bnt);
    }
}";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (11,21): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         bool? bnt = bt;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "bt").WithArguments("System.Nullable`1", ".ctor").WithLocation(11, 21),
                // (13,14): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         T(1, true & bnt);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "true").WithArguments("System.Nullable`1", ".ctor").WithLocation(13, 14)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_09()
        {
            string source = @"
using System;
class C
{
    
    static void T(int x, bool? b) {}

    static void Main()
    {
        bool bt = true;
        bool? bnt = bt;

        T(13, bnt & bnt);
    }
}";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (13,15): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         T(13, bnt & bnt);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "bnt & bnt").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(13, 15),
                // (13,15): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         T(13, bnt & bnt);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "bnt & bnt").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(13, 15)
                );
        }

        [Fact]
        public void System_String__op_Equality_01()
        {
            string source = @"
using System;
struct SZ
{
    public string str;
    public SZ(string str) { this.str = str; }
    public SZ(char c) { this.str = c.ToString(); }
    public static bool operator ==(SZ sz1, SZ sz2) { return sz1.str == sz2.str; }
    public static bool operator !=(SZ sz1, SZ sz2) { return sz1.str != sz2.str; }
    public override bool Equals(object x) { return true; }
    public override int GetHashCode() { return 0; }
}
class C
{
    static void Main()
    {
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_String__op_Equality);
            compilation.VerifyEmitDiagnostics(
                // (8,61): error CS0656: Missing compiler required member 'System.String.op_Equality'
                //     public static bool operator ==(SZ sz1, SZ sz2) { return sz1.str == sz2.str; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "sz1.str == sz2.str").WithArguments("System.String", "op_Equality").WithLocation(8, 61)
                );
        }

        [Fact]
        public void System_Nullable_T_get_HasValue_03()
        {
            var source = @"
using System;

static class LiveList
{
    struct WhereInfo<TSource>
    {
        public int Key { get; set; }
    }

    static void Where<TSource>()
    {
        Action subscribe = () =>
        {
            WhereInfo<TSource>? previous = null;

            var previousKey = previous?.Key;
        };
    }
}";

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_get_HasValue);
            compilation.VerifyEmitDiagnostics(
                // (17,31): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //             var previousKey = previous?.Key;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "previous?.Key").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(17, 31)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_10()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        var s = nameof(Main);
        if (s is string t) Console.WriteLine(""1. {0}"", t);
        s = null;
        Console.WriteLine(""2. {0}"", s is string w ? w : nameof(X));
        int? x = 12;
        {if (x is var y) Console.WriteLine(""3. {0}"", y);}
        {if (x is int y) Console.WriteLine(""4. {0}"", y);}
        x = null;
        {if (x is var y) Console.WriteLine(""5. {0}"", y);}
        {if (x is int y) Console.WriteLine(""6. {0}"", y);}
        Console.WriteLine(""7. {0}"", (x is bool is bool));
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (16,38): warning CS0184: The given expression is never of the provided ('bool') type
                //         Console.WriteLine("7. {0}", (x is bool is bool));
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "x is bool").WithArguments("bool").WithLocation(16, 38),
                // (16,38): warning CS0183: The given expression is always of the provided ('bool') type
                //         Console.WriteLine("7. {0}", (x is bool is bool));
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, "x is bool is bool").WithArguments("bool").WithLocation(16, 38),
                // (12,19): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         {if (x is int y) Console.WriteLine("4. {0}", y);}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int y").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(12, 19),
                // (15,19): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         {if (x is int y) Console.WriteLine("6. {0}", y);}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int y").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(15, 19)
                );
        }

        [Fact]
        public void System_String__op_Equality_02()
        {
            var source =
@"
using System;
public class X
{
    public static void Main()
    {
    }

    public static void M(object o)
    {
        switch (o)
        {
            case ""hmm"":
                Console.WriteLine(""hmm""); break;
            case null:
                Console.WriteLine(""null""); break;
            case 1:
                Console.WriteLine(""int 1""); break;
            case ((byte)1):
                Console.WriteLine(""byte 1""); break;
            case ((short)1):
                Console.WriteLine(""short 1""); break;
            case ""bar"":
                Console.WriteLine(""bar""); break;
            case object t when t != o:
                Console.WriteLine(""impossible""); break;
            case 2:
                Console.WriteLine(""int 2""); break;
            case ((byte)2):
                Console.WriteLine(""byte 2""); break;
            case ((short)2):
                Console.WriteLine(""short 2""); break;
            case ""baz"":
                Console.WriteLine(""baz""); break;
            default:
                Console.WriteLine(""other "" + o); break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_String__op_Equality);
            compilation.VerifyEmitDiagnostics(
                // (13,18): error CS0656: Missing compiler required member 'System.String.op_Equality'
                //             case "hmm":
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""hmm""").WithArguments("System.String", "op_Equality").WithLocation(13, 18),
                // (33,18): error CS0656: Missing compiler required member 'System.String.op_Equality'
                //             case "baz":
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""baz""").WithArguments("System.String", "op_Equality").WithLocation(33, 18)
                );
        }

        [Fact]
        public void System_String__Chars()
        {
            var source =
@"using System;

class Program
{
    public static void Main(string[] args)
    {
        bool hasB = false;
        foreach (var c in ""ab"")
        {
           switch (c)
           {
              case char b when IsB(b):
                 hasB = true;
                 break;

              default:
                 hasB = false;
                 break;
           }
        }
        Console.WriteLine(hasB);
    }

    public static bool IsB(char value)
    {
        return value == 'b';
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_String__Chars);
            compilation.VerifyEmitDiagnostics(
                // (8,9): error CS0656: Missing compiler required member 'System.String.get_Chars'
                //         foreach (var c in "ab")
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"foreach (var c in ""ab"")
        {
           switch (c)
           {
              case char b when IsB(b):
                 hasB = true;
                 break;

              default:
                 hasB = false;
                 break;
           }
        }").WithArguments("System.String", "get_Chars").WithLocation(8, 9)
                );
        }

        [Fact]
        public void System_Nullable_T_GetValueOrDefault_11()
        {
            var source =
@"using System;
class Program
{
  static void Main(string[] args)
  {
  }
  static void M(X? x)
  {
    switch (x)
    {
      case null:
        Console.WriteLine(""null"");
        break;
      case 1:
        Console.WriteLine(1);
        break;
    }
  }
}
struct X
{
    public static implicit operator int? (X x)
    {
        return 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            compilation.VerifyEmitDiagnostics(
                // (9,13): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //     switch (x)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(9, 13),
                // (14,12): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //       case 1:
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(14, 12)
                );
        }

        [Fact]
        public void System_String__ConcatObject()
        {
            // It isn't possible to trigger this diagnostic, as we don't use String.Concat(object)

            var source = @"
using System;
public class Test
{
    private static string S = ""F"";
    private static object O = ""O"";
    static void Main()
    {
        Console.WriteLine(O + null);
        Console.WriteLine(S + null);
    }
}
    ";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(SpecialMember.System_String__ConcatObject);
            compilation.VerifyEmitDiagnostics(); // We don't expect any
            CompileAndVerify(compilation, expectedOutput: @"O
F");
        }

        [Fact]
        public void System_Object__ToString()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        char c = 'c';
        Console.WriteLine(c + ""3"");
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Object__ToString);
            compilation.VerifyEmitDiagnostics(
                // (9,27): error CS0656: Missing compiler required member 'System.Object.ToString'
                //         Console.WriteLine(c + "3");
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(9, 27)
                );
        }

        [Fact]
        public void System_Object__ToString_3Args()
        {
            var source = """
                using System;

                public class Test
                {
                    static void Main()
                    {
                        char c = 'c';
                        int i = 2;
                        Console.WriteLine(c + "3" + i);
                    }
                }
                """;

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Object__ToString);
            compilation.VerifyEmitDiagnostics(
                // (9,27): error CS0656: Missing compiler required member 'System.Object.ToString'
                //         Console.WriteLine(c + "3" + i);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(9, 27),
                // (9,37): error CS0656: Missing compiler required member 'System.Object.ToString'
                //         Console.WriteLine(c + "3" + i);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "i").WithArguments("System.Object", "ToString").WithLocation(9, 37)
                );
        }

        [Fact]
        public void System_Object__ToString_4Args()
        {
            var source = """
                using System;

                public class Test
                {
                    static void Main()
                    {
                        char c = 'c';
                        int i = 2;
                        double d = 0.7;
                        Console.WriteLine(c + "3" + i + d);
                    }
                }
                """;

            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Object__ToString);
            compilation.VerifyEmitDiagnostics(
                // (10,27): error CS0656: Missing compiler required member 'System.Object.ToString'
                //         Console.WriteLine(c + "3" + i + d);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(10, 27),
                // (10,37): error CS0656: Missing compiler required member 'System.Object.ToString'
                //         Console.WriteLine(c + "3" + i + d);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "i").WithArguments("System.Object", "ToString").WithLocation(10, 37),
                // (10,41): error CS0656: Missing compiler required member 'System.Object.ToString'
                //         Console.WriteLine(c + "3" + i + d);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "d").WithArguments("System.Object", "ToString").WithLocation(10, 41)
                );
        }

        [Fact]
        public void System_String__ConcatStringString()
        {
            var source = @"
using System;
using System.Linq;
using System.Linq.Expressions;

class Test
{
    public static void Main()
    {
        Expression<Func<string, string, string>> testExpr = (x, y) => x + y;
        var result = testExpr.Compile()(""Hello "", ""World!"");
        Console.WriteLine(result);
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef });
            compilation.MakeMemberMissing(SpecialMember.System_String__ConcatStringString);
            compilation.VerifyEmitDiagnostics(
                // (10,71): error CS0656: Missing compiler required member 'System.String.Concat'
                //         Expression<Func<string, string, string>> testExpr = (x, y) => x + y;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x + y").WithArguments("System.String", "Concat").WithLocation(10, 71)
                );
        }

        [Fact]
        public void System_Array__GetLowerBound()
        {
            var source = @"
class C
{
    static void Main()
    {
        double[,] values = {
            { 1.2, 2.3, 3.4, 4.5 },
            { 5.6, 6.7, 7.8, 8.9 },
        };

        foreach (var x in values)
        {
            System.Console.WriteLine(x);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Array__GetLowerBound);
            compilation.VerifyEmitDiagnostics(
                // (11,9): error CS0656: Missing compiler required member 'System.Array.GetLowerBound'
                //         foreach (var x in values)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"foreach (var x in values)
        {
            System.Console.WriteLine(x);
        }").WithArguments("System.Array", "GetLowerBound").WithLocation(11, 9)
                );
        }

        [Fact]
        public void System_Array__GetUpperBound()
        {
            var source = @"
class C
{
    static void Main()
    {
        double[,] values = {
            { 1.2, 2.3, 3.4, 4.5 },
            { 5.6, 6.7, 7.8, 8.9 },
        };

        foreach (var x in values)
        {
            System.Console.WriteLine(x);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Array__GetUpperBound);
            compilation.VerifyEmitDiagnostics(
                // (11,9): error CS0656: Missing compiler required member 'System.Array.GetUpperBound'
                //         foreach (var x in values)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"foreach (var x in values)
        {
            System.Console.WriteLine(x);
        }").WithArguments("System.Array", "GetUpperBound").WithLocation(11, 9)
                );
        }

        [Fact]
        public void System_Decimal__op_Implicit_FromInt32_1()
        {
            var source =
@"using System;
using System.Linq.Expressions;

public struct SampStruct
{
    public static implicit operator int(SampStruct ss1)
    {
        return 1;
    }
}

public class Test
{
    static void Main()
    {
        Expression<Func<SampStruct?, decimal, decimal>> testExpr = (x, y) => x ?? y;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef });
            compilation.MakeMemberMissing(SpecialMember.System_Decimal__op_Implicit_FromInt32);
            compilation.VerifyEmitDiagnostics(
                // (16,78): error CS0656: Missing compiler required member 'System.Decimal.op_Implicit'
                //         Expression<Func<SampStruct?, decimal, decimal>> testExpr = (x, y) => x ?? y;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x ?? y").WithArguments("System.Decimal", "op_Implicit").WithLocation(16, 78)
                );
        }

        [Fact]
        public void System_Decimal__op_Implicit_FromInt32_2()
        {
            var source =
@"
public class Test
{
    static void Main()
    {
        int x = 1;
        decimal y = x;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef });
            compilation.MakeMemberMissing(SpecialMember.System_Decimal__op_Implicit_FromInt32);
            compilation.VerifyEmitDiagnostics(
                // (7,21): error CS0656: Missing compiler required member 'System.Decimal.op_Implicit'
                //         decimal y = x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.Decimal", "op_Implicit").WithLocation(7, 21)
                );
        }

        [Fact]
        public void System_Decimal__op_Implicit_FromInt32_3()
        {
            var source =
@"
public class Test
{
    static void Main()
    {
        int? x = 1;
        decimal? y = x;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef });
            compilation.MakeMemberMissing(SpecialMember.System_Decimal__op_Implicit_FromInt32);
            compilation.VerifyEmitDiagnostics(
                // (7,22): error CS0656: Missing compiler required member 'System.Decimal.op_Implicit'
                //         decimal? y = x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.Decimal", "op_Implicit").WithLocation(7, 22)
                );
        }

        [Fact]
        public void System_Decimal__op_Implicit_FromInt32_4()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

public class Test
{
    static void Main()
    {
        Expression<Func<int?, decimal?>> testExpr = (x) => x;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef });
            compilation.MakeMemberMissing(SpecialMember.System_Decimal__op_Implicit_FromInt32);
            compilation.VerifyEmitDiagnostics(
                // (9,60): error CS0656: Missing compiler required member 'System.Decimal.op_Implicit'
                //         Expression<Func<int?, decimal?>> testExpr = (x) => x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.Decimal", "op_Implicit").WithLocation(9, 60)
                );
        }

        [Fact]
        public void System_Nullable_T__ctor_10()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

class Test {
    static void LogCallerLineNumber5([CallerLineNumber] int? lineNumber   = 5) { Console.WriteLine(""line: "" + lineNumber); }

    public static void Main() {
        LogCallerLineNumber5();
    }
}";

            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemRef });
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T__ctor);
            compilation.VerifyEmitDiagnostics(
                // (10,9): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         LogCallerLineNumber5();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "LogCallerLineNumber5()").WithArguments("System.Nullable`1", ".ctor").WithLocation(10, 9)
                );
        }
    }
}
