// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor);

            comp.VerifyEmitDiagnostics(
                // (9,34): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
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
            var comp = CreateCompilation(source);

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

            validatePresent(CreateCompilation(string.Format(sourceTemplate, "public")));
            validatePresent(CreateCompilation(string.Format(sourceTemplate, "internal")));
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

            var corlibRef = CreateCompilation(corlibSource).EmitToImageReference(expectedWarnings: new[]
            {
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
            });

            var publicLibRef = CreateCompilation(string.Format(libSourceTemplate, "public"), new[] { corlibRef }).EmitToImageReference();
            var internalLibRef = CreateCompilation(string.Format(libSourceTemplate, "internal"), new[] { corlibRef }).EmitToImageReference();

            var comp = CreateCompilation("", new[] { corlibRef, publicLibRef, internalLibRef }, assemblyName: "Test");

            var wellKnown = comp.GetWellKnownType(WellKnownType.System_Type);
            Assert.NotNull(wellKnown);
            Assert.Equal(TypeKind.Class, wellKnown.TypeKind);
            Assert.Equal(Accessibility.Public, wellKnown.DeclaredAccessibility);

            var lookup = comp.GetTypeByMetadataName("System.Type");
            Assert.Null(lookup); // Ambiguous
        }

        private static void ValidateSourceAndMetadata(string source, Action<CSharpCompilation> validate)
        {
            var comp1 = CreateCompilation(source);
            validate(comp1);

            var reference = comp1.EmitToImageReference(expectedWarnings: new[]
            {
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
            });

            var comp2 = CreateCompilation("", new[] { reference });
            validate(comp2);
        }

        [Fact]
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        public void AllSpecialTypes()
        {
            var comp = CreateCompilation("", new[] { MscorlibRef_v4_0_30316_17626 });

            for (var special = SpecialType.None + 1; special <= SpecialType.Count; special++)
            {
                var symbol = comp.GetSpecialType(special);
                Assert.NotNull(symbol);
                Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind);
            }
        }

        [Fact]
        [WorkItem(530436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530436")]
        public void AllSpecialTypeMembers()
        {
            var comp = CreateCompilation("", new[] { MscorlibRef_v4_0_30316_17626 });

            foreach (SpecialMember special in Enum.GetValues(typeof(SpecialMember)))
            {
                if (special == SpecialMember.Count) continue; // Not a real value;

                var symbol = comp.GetSpecialTypeMember(special);
                Assert.NotNull(symbol);
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
            var comp = CreateCompilation("", refs);

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
                        // Not yet in the platform.
                    case WellKnownType.Microsoft_CodeAnalysis_Runtime_Instrumentation:
                        // Not always available.
                        continue;
                    case WellKnownType.ExtSentinel:
                        // Not a real type
                        continue;
                }

                switch (wkt)
                {
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
                Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind);
            }
        }

        [Fact]
        public void AllWellKnownTypesBeforeCSharp7()
        {
            foreach (var type in new[] {
                            WellKnownType.System_Math,
                            WellKnownType.System_Array,
                            WellKnownType.System_Attribute,
                            WellKnownType.System_CLSCompliantAttribute,
                            WellKnownType.System_Convert,
                            WellKnownType.System_Exception,
                            WellKnownType.System_FlagsAttribute,
                            WellKnownType.System_FormattableString,
                            WellKnownType.System_Guid,
                            WellKnownType.System_IFormattable,
                            WellKnownType.System_RuntimeTypeHandle,
                            WellKnownType.System_RuntimeFieldHandle,
                            WellKnownType.System_RuntimeMethodHandle,
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

                            WellKnownType.System_Runtime_GCLatencyMode,
                            WellKnownType.System_IFormatProvider }
                )
            {
                Assert.True(type <= WellKnownType.CSharp7Sentinel);
            }

            // There were 204 well-known types prior to CSharp7
            Assert.Equal(204, (int)(WellKnownType.CSharp7Sentinel - WellKnownType.First));
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
                CSharpRef,
                SystemXmlRef,
                SystemXmlLinqRef,
                SystemWindowsFormsRef,
                ValueTupleRef
            }.Concat(WinRtRefs).ToArray();
            var comp = CreateCompilation("", refs);

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
                    case WellKnownMember.System_Array__Empty:
                        // Not yet in the platform.
                        continue;
                    case WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayload:
                        // Not always available.
                        continue;
                }
                if (wkm == WellKnownMember.Count) continue; // Not a real value.

                var symbol = comp.GetWellKnownTypeMember(wkm);
                Assert.NotNull(symbol);
            }
        }
    }
}
