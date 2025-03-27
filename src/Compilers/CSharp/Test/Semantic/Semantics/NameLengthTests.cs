// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.Cci;
using System;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NameLengthTests : CSharpTestBase
    {
        // Longest legal symbol name.
        private static readonly string s_longSymbolName = new string('A', MetadataWriter.NameLengthLimit);
        // Longest legal path name.
        private static readonly string s_longPathName = new string('A', MetadataWriter.PathLengthLimit);
        // Longest legal local name.
        private static readonly string s_longLocalName = new string('A', MetadataWriter.PdbLengthLimit);

        [Fact]
        public void UnmangledMemberNames()
        {
            var sourceTemplate = @"
using System;

class Fields
{{
    int {0};    // Fine
    int {0}1;   // Too long
}}

class FieldLikeEvents
{{
    event Action {0};   // Fine (except accessors)
    event Action {0}1;  // Too long
}}

class CustomEvents
{{
    event Action {0} {{ add {{ }} remove {{ }} }}   // Fine (except accessors)
    event Action {0}1 {{ add {{ }} remove {{ }} }}  // Too long
}}

class AutoProperties
{{
    int {0} {{ get; set; }}     // Fine (except accessors and backing field)
    int {0}1 {{ get; set; }}    // Too long
}}

class CustomProperties
{{
    int {0} {{ get {{ return 0; }} set {{ }} }}     // Fine (except accessors)
    int {0}1 {{ get {{ return 0; }} set {{ }} }}    // Too long
}}

class Methods
{{
    void {0}() {{ }}    // Fine
    void {0}1() {{ }}   // Too long
}}
";

            var source = string.Format(sourceTemplate, s_longSymbolName);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // Uninteresting

                // (6,9): warning CS0169: The field 'Fields.LongSymbolName' is never used
                //     int LongSymbolName;    // Fine
                Diagnostic(ErrorCode.WRN_UnreferencedField, s_longSymbolName).WithArguments("Fields." + s_longSymbolName).WithLocation(6, 9),
                // (7,9): warning CS0169: The field 'Fields.LongSymbolName + 1' is never used
                //     int LongSymbolName + 1;   // Too long
                Diagnostic(ErrorCode.WRN_UnreferencedField, s_longSymbolName + 1).WithArguments("Fields." + s_longSymbolName + 1).WithLocation(7, 9),
                // (12,18): warning CS0067: The event 'FieldLikeEvents.LongSymbolName' is never used
                //     event Action LongSymbolName;   // Fine
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, s_longSymbolName).WithArguments("FieldLikeEvents." + s_longSymbolName).WithLocation(12, 18),
                // (13,18): warning CS0067: The event 'FieldLikeEvents.LongSymbolName + 1' is never used
                //     event Action LongSymbolName + 1;  // Too long
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, s_longSymbolName + 1).WithArguments("FieldLikeEvents." + s_longSymbolName + 1).WithLocation(13, 18));
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1;   // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(7, 9),
                // (12,18): error CS7013: Name 'add_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName;   // Fine (except accessors)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName).WithArguments("add_" + s_longSymbolName).WithLocation(12, 18),
                // (12,18): error CS7013: Name 'remove_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName;   // Fine (except accessors)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName).WithArguments("remove_" + s_longSymbolName).WithLocation(12, 18),
                // (13,18): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName + 1;  // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(13, 18),
                // (13,18): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName + 1;  // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(13, 18), // Would be nice not to report on the backing field.
                                                                                                                                              // (13,18): error CS7013: Name 'add_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                                                                                                                                              //     event Action LongSymbolName + 1;  // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments("add_" + s_longSymbolName + 1).WithLocation(13, 18),
                // (13,18): error CS7013: Name 'remove_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName + 1;  // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments("remove_" + s_longSymbolName + 1).WithLocation(13, 18),
                // (18,1044): error CS7013: Name 'add_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName { add { } remove { } }   // Fine (except accessors)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "add").WithArguments("add_" + s_longSymbolName).WithLocation(18, 1044),
                // (18,1052): error CS7013: Name 'remove_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName { add { } remove { } }   // Fine (except accessors)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "remove").WithArguments("remove_" + s_longSymbolName).WithLocation(18, 1052),
                // (19,18): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName + 1 { add { } remove { } }  // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(19, 18),
                // (19,1045): error CS7013: Name 'add_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName + 1 { add { } remove { } }  // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "add").WithArguments("add_" + s_longSymbolName + 1).WithLocation(19, 1045),
                // (19,1053): error CS7013: Name 'remove_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     event Action LongSymbolName + 1 { add { } remove { } }  // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "remove").WithArguments("remove_" + s_longSymbolName + 1).WithLocation(19, 1053),
                // (24,9): error CS7013: Name '<LongSymbolName>k__BackingField' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName { get; set; }     // Fine (except accessors and backing field)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName).WithArguments("<" + s_longSymbolName + ">k__BackingField").WithLocation(24, 9),
                // (24,1035): error CS7013: Name 'get_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName { get; set; }     // Fine (except accessors and backing field)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "get").WithArguments("get_" + s_longSymbolName).WithLocation(24, 1035),
                // (24,1040): error CS7013: Name 'set_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName { get; set; }     // Fine (except accessors and backing field)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "set").WithArguments("set_" + s_longSymbolName).WithLocation(24, 1040),
                // (25,9): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1 { get; set; }    // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(25, 9),
                // (25,9): error CS7013: Name '<LongSymbolName + 1>k__BackingField' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1 { get; set; }    // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments("<" + s_longSymbolName + "1>k__BackingField").WithLocation(25, 9),
                // (25,1036): error CS7013: Name 'get_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1 { get; set; }    // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "get").WithArguments("get_" + s_longSymbolName + 1).WithLocation(25, 1036),
                // (25,1041): error CS7013: Name 'set_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1 { get; set; }    // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "set").WithArguments("set_" + s_longSymbolName + 1).WithLocation(25, 1041),
                // (30,1035): error CS7013: Name 'get_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName { get { return 0; } set { } }     // Fine (except accessors)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "get").WithArguments("get_" + s_longSymbolName).WithLocation(30, 1035),
                // (30,1053): error CS7013: Name 'set_LongSymbolName' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName { get { return 0; } set { } }     // Fine (except accessors)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "set").WithArguments("set_" + s_longSymbolName).WithLocation(30, 1053),
                // (31,9): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1 { get { return 0; } set { } }    // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(31, 9),
                // (31,1036): error CS7013: Name 'get_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1 { get { return 0; } set { } }    // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "get").WithArguments("get_" + s_longSymbolName + 1).WithLocation(31, 1036),
                // (31,1054): error CS7013: Name 'set_LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int LongSymbolName + 1 { get { return 0; } set { } }    // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "set").WithArguments("set_" + s_longSymbolName + 1).WithLocation(31, 1054),
                // (37,10): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     void LongSymbolName + 1() { }   // Too long
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(37, 10),

                // Uninteresting

                // (6,9): warning CS0169: The field 'Fields.LongSymbolName' is never used
                //     int LongSymbolName;    // Fine
                Diagnostic(ErrorCode.WRN_UnreferencedField, s_longSymbolName).WithArguments("Fields." + s_longSymbolName).WithLocation(6, 9),
                // (7,9): warning CS0169: The field 'Fields.LongSymbolName + 1' is never used
                //     int LongSymbolName + 1;   // Too long
                Diagnostic(ErrorCode.WRN_UnreferencedField, s_longSymbolName + 1).WithArguments("Fields." + s_longSymbolName + 1).WithLocation(7, 9),
                // (12,18): warning CS0067: The event 'FieldLikeEvents.LongSymbolName' is never used
                //     event Action LongSymbolName;   // Fine
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, s_longSymbolName).WithArguments("FieldLikeEvents." + s_longSymbolName).WithLocation(12, 18),
                // (13,18): warning CS0067: The event 'FieldLikeEvents.LongSymbolName + 1' is never used
                //     event Action LongSymbolName + 1;  // Too long
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, s_longSymbolName + 1).WithArguments("FieldLikeEvents." + s_longSymbolName + 1).WithLocation(13, 18));
        }

        [Fact]
        public void EmptyNamespaces()
        {
            var sourceTemplate = @"
namespace {0} {{ }}     // Fine.
namespace {0}1 {{ }}    // Too long, but not checked.
";

            var source = string.Format(sourceTemplate, s_longSymbolName);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void NonGeneratedTypeNames()
        {
            // {n} == LongSymbolName.Substring(n)
            var sourceTemplate = @"
class {0} {{ }}     // Fine
class {0}1 {{ }}    // Too long

namespace N
{{
    struct {2} {{ }}    // Fine
    struct {2}1 {{ }}   // Too long after prepending 'N.'
}}

class Outer
{{
    enum {0} {{ }}     // Fine, since outer class is not prepended
    enum {0}1 {{ }}    // Too long
}}

interface {2}<T> {{ }}  // Fine
interface {2}1<T> {{ }} // Too long after appending '`1'
";

            var substring0 = s_longSymbolName;
            var substring1 = s_longSymbolName.Substring(1);
            var substring2 = s_longSymbolName.Substring(2);
            var source = string.Format(sourceTemplate, substring0, substring1, substring2);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (3,7):
                // class 
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, substring0 + 1).WithArguments(substring0 + 1).WithLocation(3, 7),
                // (8,12):
                //     struct
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, substring2 + 1).WithArguments("N." + substring2 + 1).WithLocation(8, 12),
                // (14,10):
                //     enum
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, substring0 + 1).WithArguments(substring0 + 1).WithLocation(14, 10),
                // (18,11):
                // interface
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, substring2 + 1).WithArguments(substring2 + "1`1").WithLocation(18, 11));
        }

        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            var sourceTemplate = @"
interface I
{{
    void {0}();
    void {0}1();
}}

namespace N
{{
    interface J<T>
    {{
        void {1}();
        void {1}1();
    }}
}}

class C : I, N.J<C>
{{
    void I.{0}() {{ }}
    void I.{0}1() {{ }}

    void N.J<C>.{1}() {{ }}
    void N.J<C>.{1}1() {{ }}
}}
";

            var name0 = s_longSymbolName.Substring(2); // Space for "I."
            var name1 = s_longSymbolName.Substring(7); // Space for "N.J<C>."
            var source = string.Format(sourceTemplate, name0, name1);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (20,12):
                //     void I.{0}1() { }
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, name0 + 1).WithArguments("I." + name0 + 1).WithLocation(20, 12),
                // (23,17):
                //     void N.J<C>.{1}1() { }
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, name1 + 1).WithArguments("N.J<C>." + name1 + 1).WithLocation(23, 17));
        }

        [Fact]
        public void DllImport()
        {
            var sourceTemplate = @"
using System.Runtime.InteropServices;

class C1
{{
    [DllImport(""goo.dll"", EntryPoint = ""Short1"")]
    static extern void {0}();  // Name is fine, entrypoint is fine.
    [DllImport(""goo.dll"", EntryPoint = ""Short2"")]
    static extern void {0}1(); // Name is too long, entrypoint is fine.
}}

class C2
{{
    [DllImport(""goo.dll"", EntryPoint = ""{0}"")]
    static extern void Short1();   // Name is fine, entrypoint is fine.
    [DllImport(""goo.dll"", EntryPoint = ""{0}1"")]
    static extern void Short2();   // Name is fine, entrypoint is too long.
}}

class C3
{{
    [DllImport(""goo.dll"")]
    static extern void {0}();  // Name is fine, entrypoint is unspecified.
    [DllImport(""goo.dll"")]
    static extern void {0}1(); // Name is too long, entrypoint is unspecified.
}}
";

            var source = string.Format(sourceTemplate, s_longSymbolName);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (9,24):
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(9, 24),
                // (17,24):
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "Short2").WithArguments(s_longSymbolName + 1).WithLocation(17, 24),
                // (25,24):
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(25, 24));
        }

        [Fact]
        public void Parameters()
        {
            var sourceTemplate = @"
class C
{{
    void M(bool {0}) {{ }}
    void M(long {0}1) {{ }}
    int this[bool {0}] {{ get {{ return 0; }} }}
    int this[long {0}1] {{ get {{ return 0; }} }}
    delegate void D1(bool {0});
    delegate void D2(long {0}1);
}}
";

            var source = string.Format(sourceTemplate, s_longSymbolName);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (5,17): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     void M(long LongSymbolName + 1) { }
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(5, 17),
                // (7,19): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     int this[long LongSymbolName + 1] { get { return 0; } }
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(7, 19),
                // (9,27): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     delegate void D2(long LongSymbolName + 1);
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(9, 27),
                // (9,27): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     delegate void D2(long LongSymbolName + 1);
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(9, 27)); // Second report is for Invoke method.  Not ideal, but not urgent.
        }

        [Fact]
        public void TypeParameters()
        {
            var sourceTemplate = @"
class C<{0}, {0}1>
{{
}}

delegate void D<{0}, {0}1>();

class E
{{
    void M<{0}, {0}1>() {{ }}
}}
";

            var source = string.Format(sourceTemplate, s_longSymbolName);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (2,1034): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                // class C<LongSymbolName, LongSymbolName + 1>
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(2, 1034),
                // (6,1042): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                // delegate void D<LongSymbolName, LongSymbolName + 1>();
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(6, 1042),
                // (10,1037): error CS7013: Name 'LongSymbolName + 1' exceeds the maximum length allowed in metadata.
                //     void M<LongSymbolName, LongSymbolName + 1>() { }
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, s_longSymbolName + 1).WithArguments(s_longSymbolName + 1).WithLocation(10, 1037));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void Locals()
        {
            var sourceTemplate = @"
class C
{{
    int M() 
    {{
        int {0} = 1;
        int {0}1 = 1;
        return {0} + {0}1;
    }}
}}
";

            var source = string.Format(sourceTemplate, s_longLocalName);
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                options: TestOptions.NativePdbEmit,
                // (7,13): warning CS8029: Local name 'LongLocalName + 1' is too long for PDB.  Consider shortening or compiling without /debug.
                //         int LongSymbolName + 1 = 1;
                Diagnostic(ErrorCode.WRN_PdbLocalNameTooLong, s_longLocalName + 1).WithArguments(s_longLocalName + 1).WithLocation(7, 13));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void ConstantLocals()
        {
            var sourceTemplate = @"
class C
{{
    int M() 
    {{
        const int {0} = 1;
        const int {0}1 = 1;
        return {0} + {0}1;
    }}
}}
";

            var source = string.Format(sourceTemplate, s_longLocalName);
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                options: TestOptions.NativePdbEmit,
                // (7,19): warning CS8029: Local name 'LongSymbolName + 1' is too long for PDB.  Consider shortening or compiling without /debug.
                //         const int LongSymbolName + 1 = 1;
                Diagnostic(ErrorCode.WRN_PdbLocalNameTooLong, s_longLocalName + 1).WithArguments(s_longLocalName + 1).WithLocation(7, 19));
        }

        [Fact]
        public void TestLambdaMethods()
        {
            var sourceTemplate = @"
using System;

class C
{{
    Func<int> {0}(int p)
    {{
        return () => p - 1;
    }}

    Func<int> {0}1(int p)
    {{
        return () => p + 1;
    }}
}}
";
            int padding = GeneratedNames.MakeLambdaMethodName("A", -1, 0, 0, 0).Length - 1;
            string longName = s_longSymbolName.Substring(padding);
            var source = string.Format(sourceTemplate, longName);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (13,16): error CS7013: Name '<longName + 1>b__3' exceeds the maximum length allowed in metadata.
                //         return () => p + 1;
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "() => p + 1").WithArguments("<" + longName + "1>b__0").WithLocation(13, 16));
        }

        [Fact]
        public void TestAnonymousTypeProperties()
        {
            var sourceTemplate = @"
class C
{{
    object M()
    {{
        return new {{ {0} = 1, {0}1 = 'a' }};
    }}
}}
";
            int padding = GeneratedNames.MakeAnonymousTypeBackingFieldName("A").Length - 1;
            string longName = s_longSymbolName.Substring(padding);
            var source = string.Format(sourceTemplate, longName);
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            // CONSIDER: Double reporting (once for field def, once for member ref) is not ideal.
            // CONSIDER: No location since the synthesized field symbol doesn't have one (would light up automatically).
            comp.VerifyEmitDiagnostics(
                // error CS7013: Name '<longName1>i__Field' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments("<" + longName + 1 + ">i__Field").WithLocation(1, 1),
                // error CS7013: Name '<longName1>i__Field' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments("<" + longName + 1 + ">i__Field").WithLocation(1, 1));
        }

        [Fact]
        public void TestStateMachineMethods()
        {
            var sourceTemplate = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Iterators
{{
    IEnumerable<int> {0}()
    {{
        yield return 1;
    }}

    IEnumerable<int> {0}1()
    {{
        yield return 1;
    }}
}}

class Async
{{
    async Task {0}()
    {{
        await {0}();
    }}

    async Task {0}1()
    {{
        await {0}1();
    }}
}}
";
            int padding = GeneratedNames.MakeStateMachineTypeName("A", 0, 0).Length - 1;
            string longName = s_longSymbolName.Substring(padding);
            var source = string.Format(sourceTemplate, longName);
            var comp = CreateCompilationWithMscorlib461(source);
            comp.VerifyDiagnostics();
            // CONSIDER: Location would light up if synthesized methods had them.
            comp.VerifyEmitDiagnostics(
                // error CS7013: Name '<longName1>d__1' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments("<" + longName + 1 + ">d__1").WithLocation(1, 1),
                // error CS7013: Name '<longName1>d__1' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments("<" + longName + 1 + ">d__1").WithLocation(1, 1));
        }

        [WorkItem(531484, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531484")]
        [Fact]
        public void TestFixedSizeBuffers()
        {
            var sourceTemplate = @"
unsafe struct S
{{
    fixed int {0}[1];
    fixed int {0}1[1];
}}
";
            int padding = GeneratedNames.MakeFixedFieldImplementationName("A").Length - 1;
            string longName = s_longSymbolName.Substring(padding);
            var source = string.Format(sourceTemplate, longName);
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();
            // CONSIDER: Location would light up if synthesized methods had them.
            comp.VerifyEmitDiagnostics(
                // error CS7013: Name '<longName1>e__FixedBuffer' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments("<" + longName + 1 + ">e__FixedBuffer").WithLocation(1, 1));
        }

        [Fact]
        public void TestResources()
        {
            var source = "class C { }";
            var comp = CreateCompilation(source);
            Func<Stream> dataProvider = () => new System.IO.MemoryStream();
            var resources = new[]
            {
                new ResourceDescription("name1", "path1", dataProvider, false),   //fine
                new ResourceDescription(s_longSymbolName, "path2", dataProvider, false), //fine
                new ResourceDescription("name2", s_longPathName, dataProvider, false), //fine
                new ResourceDescription(s_longSymbolName + 1, "path3", dataProvider, false), //name error
                new ResourceDescription("name3", s_longPathName + 2, dataProvider, false), //path error
                new ResourceDescription(s_longSymbolName + 3, s_longPathName + 4, dataProvider, false), //name and path errors
            };
            comp.VerifyEmitDiagnostics(resources,
                // error CS7013: Name 'LongSymbolName1' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments(s_longSymbolName + 1).WithLocation(1, 1),
                // error CS7013: Name 'LongPathName2' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments(s_longPathName + 2).WithLocation(1, 1),
                // error CS7013: Name 'LongSymbolName3' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments(s_longSymbolName + 3).WithLocation(1, 1),
                // error CS7013: Name 'LongPathName4' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments(s_longPathName + 4).WithLocation(1, 1));
        }
    }
}
