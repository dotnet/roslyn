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

        [WorkItem(530436, "DevDiv")]
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

        [WorkItem(530436, "DevDiv")]
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
        [WorkItem(530436, "DevDiv")]
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

        [WorkItem(530436, "DevDiv")]
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

        [WorkItem(530436, "DevDiv")]
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

        [WorkItem(530436, "DevDiv")]
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
        [WorkItem(530436, "DevDiv")]
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
        [WorkItem(530436, "DevDiv")]
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

        [WorkItem(530436, "DevDiv")]
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
        [WorkItem(530436, "DevDiv")]
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
        [WorkItem(530436, "DevDiv")]
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
        [WorkItem(530436, "DevDiv")]
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
            }.Concat(WinRtRefs).ToArray();
            var comp = CreateCompilation("", refs);

            for (var wkt = WellKnownType.First; wkt <= WellKnownType.Last; wkt++)
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
                        continue;
                }

                var symbol = comp.GetWellKnownType(wkt);
                Assert.NotNull(symbol);
                Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind);
            }
        }

        [Fact]
        [WorkItem(530436, "DevDiv")]
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
                        // Not available yet, but will be in upcoming release.
                        continue;
                }
                if (wkm == WellKnownMember.Count) continue; // Not a real value.

                var symbol = comp.GetWellKnownTypeMember(wkm);
                Assert.NotNull(symbol);
            }
        }
    }
}
