// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class WinMdDelegateTests : CSharpTestBase
    {
        private delegate void VerifyType(bool isWinMd, params string[] expectedMembers);

        /// <summary>
        /// When the output type is .winmdobj, delegate types shouldn't output Begin/End invoke 
        /// members.
        /// </summary>
        [Fact(), WorkItem(1003193)]
        public void SimpleDelegateMembersTest()
        {
            const string libSrc =
@"namespace Test
{
  public delegate void voidDelegate();
}";
            Func<string[], Action<ModuleSymbol>> getValidator = expectedMembers => m =>
            {
                {
                    var actualMembers =
                        m.GlobalNamespace.GetMember<NamespaceSymbol>("Test").
                        GetMember<NamedTypeSymbol>("voidDelegate").GetMembers().ToArray();

                    AssertEx.SetEqual(actualMembers.Select(s => s.Name), expectedMembers);
                };
            };


            VerifyType verify = (winmd, expected) =>
            {
                var validator = getValidator(expected);

                // We should see the same members from both source and metadata
                var verifier = CompileAndVerify(
                    libSrc,
                    sourceSymbolValidator: validator,
                    symbolValidator: validator,
                    options: winmd ? TestOptions.ReleaseWinMD : TestOptions.ReleaseDll);
                verifier.VerifyDiagnostics();
            };

            // Test winmd
            verify(true,
                WellKnownMemberNames.InstanceConstructorName,
                WellKnownMemberNames.DelegateInvokeName);

            // Test normal
            verify(false,
                WellKnownMemberNames.InstanceConstructorName,
                WellKnownMemberNames.DelegateInvokeName,
                WellKnownMemberNames.DelegateBeginInvokeName,
                WellKnownMemberNames.DelegateEndInvokeName);
        }

        [Fact]
        public void TestAllDelegates()
        {
            var winRtDelegateLibrarySrc =
@"using System;

namespace WinRTDelegateLibrary
{
    public struct S1 { }

    public enum E1
    {
        alpha = 1,
        bravo,
        charlie,
        delta,
    };

    public class C1 { }

    public interface I1 { }

    /// 
    /// These are the interesting types
    /// 

    public delegate void voidvoidDelegate();

    public delegate int intintDelegate(int a);

    public delegate S1 structDelegate(S1 s);

    public delegate E1 enumDelegate(E1 e);

    public delegate C1 classDelegate(C1 c);

    public delegate string stringDelegate(string s);

    public delegate Decimal decimalDelegate(Decimal d);

    public delegate voidvoidDelegate WinRTDelegate(voidvoidDelegate d);

    public delegate int? nullableDelegate(int? a);

    public delegate T genericDelegate<T>(T t);
    public delegate T genericDelegate2<T>(T t) where T : new();
    public delegate T genericDelegate3<T>(T t) where T : class;
    public delegate T genericDelegate4<T>(T t) where T : struct;
    public delegate T genericDelegate5<T>(T t) where T : I1;

    public delegate int[] arrayDelegate(int[] arr);

    public delegate I1 interfaceDelegate(I1 i);

    public delegate dynamic dynamicDelegate(dynamic d);

    public unsafe delegate int* pointerDelegate(int* ip);

    public unsafe delegate S1* pointerDelegate2(S1* op);

    public unsafe delegate E1* pointerDelegate3(E1* ep);
}";
            // We need the 4.5 refs here
            var coreRefs45 = new[] {
                MscorlibRef_v4_0_30316_17626,
                SystemCoreRef_v4_0_30319_17929
            };

            var winRtDelegateLibrary = CreateCompilation(
                winRtDelegateLibrarySrc,
                references: coreRefs45,
                options: TestOptions.ReleaseWinMD.WithAllowUnsafe(true),
                assemblyName: "WinRTDelegateLibrary").EmitToImageReference();

            var nonWinRtLibrarySrc = winRtDelegateLibrarySrc.Replace("WinRTDelegateLibrary", "NonWinRTDelegateLibrary");

            var nonWinRtDelegateLibrary = CreateCompilation(
                nonWinRtLibrarySrc,
                references: coreRefs45,
                options: TestOptions.UnsafeReleaseDll,
                assemblyName: "NonWinRTDelegateLibrary").EmitToImageReference();

            var allDelegates =
@"using WinRT = WinRTDelegateLibrary;
using NonWinRT = NonWinRTDelegateLibrary;

class Test
{
    public WinRT.voidvoidDelegate d001;
    public NonWinRT.voidvoidDelegate d101;

    public WinRT.intintDelegate d002;
    public NonWinRT.intintDelegate d102;

    public WinRT.structDelegate d003;
    public NonWinRT.structDelegate d103;

    public WinRT.enumDelegate d004;
    public NonWinRT.enumDelegate d104;

    public WinRT.classDelegate d005;
    public NonWinRT.classDelegate d105;

    public WinRT.stringDelegate d006;
    public NonWinRT.stringDelegate d106;

    public WinRT.decimalDelegate d007;
    public NonWinRT.decimalDelegate d107;

    public WinRT.WinRTDelegate d008;
    public NonWinRT.WinRTDelegate d108;

    public WinRT.nullableDelegate d009;
    public NonWinRT.nullableDelegate d109;

    public WinRT.genericDelegate<float> d010;
    public NonWinRT.genericDelegate<float> d110;

    public WinRT.genericDelegate2<object> d011;
    public NonWinRT.genericDelegate2<object> d111;

    public WinRT.genericDelegate3<WinRT.C1> d012;
    public NonWinRT.genericDelegate3<NonWinRT.C1> d112;

    public WinRT.genericDelegate4<WinRT.S1> d013;
    public NonWinRT.genericDelegate4<NonWinRT.S1> d113;

    public WinRT.genericDelegate5<WinRT.I1> d014;
    public NonWinRT.genericDelegate5<NonWinRT.I1> d114;

    public WinRT.arrayDelegate d015;
    public NonWinRT.arrayDelegate d115;

    public WinRT.interfaceDelegate d016;
    public NonWinRT.interfaceDelegate d116;

    public WinRT.dynamicDelegate d017;
    public NonWinRT.dynamicDelegate d117;

    public WinRT.pointerDelegate d018;
    public NonWinRT.pointerDelegate d118;

    public WinRT.pointerDelegate2 d019;
    public NonWinRT.pointerDelegate2 d119;

    public WinRT.pointerDelegate3 d020;
    public NonWinRT.pointerDelegate3 d120;
}";

            Func<FieldSymbol, bool> isWinRt = (field) =>
            {
                var fieldType = field.Type;

                if ((object)fieldType == null)
                {
                    return false;
                }

                if (!fieldType.IsDelegateType())
                {
                    return false;
                }

                foreach (var member in fieldType.GetMembers())
                {
                    switch (member.Name)
                    {
                        case WellKnownMemberNames.DelegateBeginInvokeName:
                        case WellKnownMemberNames.DelegateEndInvokeName:
                            return false;
                        default:
                            break;
                    }
                }

                return true;
            };

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var fields = type.GetMembers();

                foreach (var field in fields)
                {
                    var fieldSymbol = field as FieldSymbol;
                    if ((object)fieldSymbol != null)
                    {
                        if (fieldSymbol.Name.Contains("d1"))
                        {
                            Assert.False(isWinRt(fieldSymbol));
                        }
                        else
                        {
                            Assert.True(isWinRt(fieldSymbol));
                        }
                    }
                }
            };

            var comp = CompileAndVerify(
                allDelegates,
                additionalRefs: new[] {
                    winRtDelegateLibrary,
                    nonWinRtDelegateLibrary
                },
                symbolValidator: validator);

            // ignore unused variable warnings
            comp.VerifyDiagnostics(
    // (9,33): warning CS0649: Field 'Test.d002' is never assigned to, and will always have its default value null
    //     public WinRT.intintDelegate d002;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d002").WithArguments("Test.d002", "null"),
    // (10,36): warning CS0649: Field 'Test.d102' is never assigned to, and will always have its default value null
    //     public NonWinRT.intintDelegate d102;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d102").WithArguments("Test.d102", "null"),
    // (12,33): warning CS0649: Field 'Test.d003' is never assigned to, and will always have its default value null
    //     public WinRT.structDelegate d003;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d003").WithArguments("Test.d003", "null"),
    // (27,32): warning CS0649: Field 'Test.d008' is never assigned to, and will always have its default value null
    //     public WinRT.WinRTDelegate d008;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d008").WithArguments("Test.d008", "null"),
    // (54,34): warning CS0649: Field 'Test.d017' is never assigned to, and will always have its default value null
    //     public WinRT.dynamicDelegate d017;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d017").WithArguments("Test.d017", "null"),
    // (34,44): warning CS0649: Field 'Test.d110' is never assigned to, and will always have its default value null
    //     public NonWinRT.genericDelegate<float> d110;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d110").WithArguments("Test.d110", "null"),
    // (30,35): warning CS0649: Field 'Test.d009' is never assigned to, and will always have its default value null
    //     public WinRT.nullableDelegate d009;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d009").WithArguments("Test.d009", "null"),
    // (43,51): warning CS0649: Field 'Test.d113' is never assigned to, and will always have its default value null
    //     public NonWinRT.genericDelegate4<NonWinRT.S1> d113;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d113").WithArguments("Test.d113", "null"),
    // (19,35): warning CS0649: Field 'Test.d105' is never assigned to, and will always have its default value null
    //     public NonWinRT.classDelegate d105;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d105").WithArguments("Test.d105", "null"),
    // (61,38): warning CS0649: Field 'Test.d119' is never assigned to, and will always have its default value null
    //     public NonWinRT.pointerDelegate2 d119;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d119").WithArguments("Test.d119", "null"),
    // (24,34): warning CS0649: Field 'Test.d007' is never assigned to, and will always have its default value null
    //     public WinRT.decimalDelegate d007;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d007").WithArguments("Test.d007", "null"),
    // (37,46): warning CS0649: Field 'Test.d111' is never assigned to, and will always have its default value null
    //     public NonWinRT.genericDelegate2<object> d111;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d111").WithArguments("Test.d111", "null"),
    // (13,36): warning CS0649: Field 'Test.d103' is never assigned to, and will always have its default value null
    //     public NonWinRT.structDelegate d103;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d103").WithArguments("Test.d103", "null"),
    // (51,36): warning CS0649: Field 'Test.d016' is never assigned to, and will always have its default value null
    //     public WinRT.interfaceDelegate d016;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d016").WithArguments("Test.d016", "null"),
    // (45,45): warning CS0649: Field 'Test.d014' is never assigned to, and will always have its default value null
    //     public WinRT.genericDelegate5<WinRT.I1> d014;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d014").WithArguments("Test.d014", "null"),
    // (16,34): warning CS0649: Field 'Test.d104' is never assigned to, and will always have its default value null
    //     public NonWinRT.enumDelegate d104;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d104").WithArguments("Test.d104", "null"),
    // (22,36): warning CS0649: Field 'Test.d106' is never assigned to, and will always have its default value null
    //     public NonWinRT.stringDelegate d106;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d106").WithArguments("Test.d106", "null"),
    // (48,32): warning CS0649: Field 'Test.d015' is never assigned to, and will always have its default value null
    //     public WinRT.arrayDelegate d015;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d015").WithArguments("Test.d015", "null"),
    // (15,31): warning CS0649: Field 'Test.d004' is never assigned to, and will always have its default value null
    //     public WinRT.enumDelegate d004;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d004").WithArguments("Test.d004", "null"),
    // (28,35): warning CS0649: Field 'Test.d108' is never assigned to, and will always have its default value null
    //     public NonWinRT.WinRTDelegate d108;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d108").WithArguments("Test.d108", "null"),
    // (64,38): warning CS0649: Field 'Test.d120' is never assigned to, and will always have its default value null
    //     public NonWinRT.pointerDelegate3 d120;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d120").WithArguments("Test.d120", "null"),
    // (7,38): warning CS0649: Field 'Test.d101' is never assigned to, and will always have its default value null
    //     public NonWinRT.voidvoidDelegate d101;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d101").WithArguments("Test.d101", "null"),
    // (52,39): warning CS0649: Field 'Test.d116' is never assigned to, and will always have its default value null
    //     public NonWinRT.interfaceDelegate d116;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d116").WithArguments("Test.d116", "null"),
    // (18,32): warning CS0649: Field 'Test.d005' is never assigned to, and will always have its default value null
    //     public WinRT.classDelegate d005;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d005").WithArguments("Test.d005", "null"),
    // (55,37): warning CS0649: Field 'Test.d117' is never assigned to, and will always have its default value null
    //     public NonWinRT.dynamicDelegate d117;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d117").WithArguments("Test.d117", "null"),
    // (42,45): warning CS0649: Field 'Test.d013' is never assigned to, and will always have its default value null
    //     public WinRT.genericDelegate4<WinRT.S1> d013;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d013").WithArguments("Test.d013", "null"),
    // (58,37): warning CS0649: Field 'Test.d118' is never assigned to, and will always have its default value null
    //     public NonWinRT.pointerDelegate d118;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d118").WithArguments("Test.d118", "null"),
    // (63,35): warning CS0649: Field 'Test.d020' is never assigned to, and will always have its default value null
    //     public WinRT.pointerDelegate3 d020;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d020").WithArguments("Test.d020", "null"),
    // (31,38): warning CS0649: Field 'Test.d109' is never assigned to, and will always have its default value null
    //     public NonWinRT.nullableDelegate d109;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d109").WithArguments("Test.d109", "null"),
    // (25,37): warning CS0649: Field 'Test.d107' is never assigned to, and will always have its default value null
    //     public NonWinRT.decimalDelegate d107;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d107").WithArguments("Test.d107", "null"),
    // (49,35): warning CS0649: Field 'Test.d115' is never assigned to, and will always have its default value null
    //     public NonWinRT.arrayDelegate d115;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d115").WithArguments("Test.d115", "null"),
    // (57,34): warning CS0649: Field 'Test.d018' is never assigned to, and will always have its default value null
    //     public WinRT.pointerDelegate d018;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d018").WithArguments("Test.d018", "null"),
    // (46,51): warning CS0649: Field 'Test.d114' is never assigned to, and will always have its default value null
    //     public NonWinRT.genericDelegate5<NonWinRT.I1> d114;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d114").WithArguments("Test.d114", "null"),
    // (21,33): warning CS0649: Field 'Test.d006' is never assigned to, and will always have its default value null
    //     public WinRT.stringDelegate d006;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d006").WithArguments("Test.d006", "null"),
    // (6,35): warning CS0649: Field 'Test.d001' is never assigned to, and will always have its default value null
    //     public WinRT.voidvoidDelegate d001;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d001").WithArguments("Test.d001", "null"),
    // (60,35): warning CS0649: Field 'Test.d019' is never assigned to, and will always have its default value null
    //     public WinRT.pointerDelegate2 d019;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d019").WithArguments("Test.d019", "null"),
    // (36,43): warning CS0649: Field 'Test.d011' is never assigned to, and will always have its default value null
    //     public WinRT.genericDelegate2<object> d011;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d011").WithArguments("Test.d011", "null"),
    // (33,41): warning CS0649: Field 'Test.d010' is never assigned to, and will always have its default value null
    //     public WinRT.genericDelegate<float> d010;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d010").WithArguments("Test.d010", "null"),
    // (39,45): warning CS0649: Field 'Test.d012' is never assigned to, and will always have its default value null
    //     public WinRT.genericDelegate3<WinRT.C1> d012;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d012").WithArguments("Test.d012", "null"),
    // (40,51): warning CS0649: Field 'Test.d112' is never assigned to, and will always have its default value null
    //     public NonWinRT.genericDelegate3<NonWinRT.C1> d112;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d112").WithArguments("Test.d112", "null"));
        }
    }
}
