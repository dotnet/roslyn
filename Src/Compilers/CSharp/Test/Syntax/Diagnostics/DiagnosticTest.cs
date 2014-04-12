// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticTest : CSharpTestBase
    {
        [Fact]
        public void TestDiagnostic()
        {
            MockMessageProvider provider = new MockMessageProvider();
            SyntaxTree syntaxTree = new MockSyntaxTree();
            CultureInfo englishCulture = CultureInfo.GetCultureInfo("en");

            DiagnosticInfo di1 = new DiagnosticInfo(provider, 1);
            Assert.Equal(1, di1.Code);
            Assert.Equal(DiagnosticSeverity.Error, di1.Severity);
            Assert.Equal("MOCK0001", di1.MessageIdentifier);
            Assert.Equal("The first error", di1.GetMessage(englishCulture));

            DiagnosticInfo di2 = new DiagnosticInfo(provider, 1002, "Elvis", "Mort");
            Assert.Equal(1002, di2.Code);
            Assert.Equal(DiagnosticSeverity.Warning, di2.Severity);
            Assert.Equal("MOCK1002", di2.MessageIdentifier);
            Assert.Equal("The second warning about Elvis and Mort", di2.GetMessage(englishCulture));

            Location l1 = new SourceLocation(syntaxTree, new TextSpan(5, 8));
            var d1 = new CSDiagnostic(di2, l1);
            Assert.Equal(l1, d1.Location);
            Assert.Same(syntaxTree, d1.Location.SourceTree);
            Assert.Equal(new TextSpan(5, 8), d1.Location.SourceSpan);
            Assert.Equal(0, d1.AdditionalLocations.Count());
            Assert.Same(di2, d1.Info);
        }

        [Fact]
        public void TestCustomErrorInfo()
        {
            MockMessageProvider provider = new MockMessageProvider();
            SyntaxTree syntaxTree = new MockSyntaxTree();
            CultureInfo englishCulture = CultureInfo.GetCultureInfo("en");

            DiagnosticInfo di3 = new CustomErrorInfo(provider, "OtherSymbol", new SourceLocation(syntaxTree, new TextSpan(14, 8)));
            var d3 = new CSDiagnostic(di3, new SourceLocation(syntaxTree, new TextSpan(1, 1)));
            Assert.Same(syntaxTree, d3.Location.SourceTree);
            Assert.Equal(new TextSpan(1, 1), d3.Location.SourceSpan);
            Assert.Equal(1, d3.AdditionalLocations.Count());
            Assert.Equal(new TextSpan(14, 8), d3.AdditionalLocations.First().SourceSpan);
            Assert.Equal("OtherSymbol", (d3.Info as CustomErrorInfo).OtherSymbol);
        }

        [WorkItem(537801, "DevDiv")]
        [Fact]
        public void MissingNamespaceOpenBracket()
        {
            var text = @"namespace NS

    interface ITest {
        void Method();
    }

End namespace
";

            var comp = CreateCompilationWithMscorlib(text);
            var actualErrors = comp.GetDiagnostics();
            Assert.InRange(actualErrors.Count(), 1, int.MaxValue);
        }

        [WorkItem(540086, "DevDiv")]
        [Fact]
        public void ErrorApplyIndexingToMethod()
        {
            var text = @"using System;
public class A
{
    static void Main(string[] args)
    {
        Console.WriteLine(foo[0]);
    }

    static int[] foo()
    {
        return new int[0];
    }
}";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadIndexLHS, Line = 6, Column = 27 });

            text = @"
public class A
{
    static void Main(string[] args)
    {        
    }

    void foo(object o)
    {
        System.Console.WriteLine(o.GetType().GetMethods[0].Name);
    }
}";
            comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadIndexLHS, Line = 10, Column = 34 });
        }

        [WorkItem(540329, "DevDiv")]
        [Fact]
        public void ErrorMemberAccessOnLiteralToken()
        {
            var text = @"
class X
{
    static void Main()
    {
        // this statement should produce an error
        int x = null.Length;
        // this statement is valid
        string three = 3.ToString();
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,17): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, @"null.Length").WithArguments(".", "<null>"));
        }

        [WorkItem(542911, "DevDiv")]
        [Fact]
        public void WarningLevel_1()
        {
            for (int i = 0; i < 10000; i++)
            {
                ErrorCode errorCode = (ErrorCode)i;
                string errorCodeName = errorCode.ToString();
                if (errorCodeName.StartsWith("WRN"))
                {
                    Assert.True(ErrorFacts.IsWarning(errorCode));
                    Assert.NotEqual(0, ErrorFacts.GetWarningLevel(errorCode));
                }
                else if (errorCodeName.StartsWith("ERR"))
                {
                    Assert.False(ErrorFacts.IsWarning(errorCode));
                    Assert.Equal(0, ErrorFacts.GetWarningLevel(errorCode));
                }
            }
        }

        [WorkItem(542911, "DevDiv")]
        [Fact]
        public void WarningLevel_2()
        {
            // Check a few warning levels recently added

            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_DeprecatedCollectionInitAddStr));
            Assert.Equal(1, ErrorFacts.GetWarningLevel(ErrorCode.WRN_DefaultValueForUnconsumedLocation));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_UnmatchedParamRefTag));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_UnmatchedTypeParamRefTag));
            Assert.Equal(1, ErrorFacts.GetWarningLevel(ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA));
            Assert.Equal(1, ErrorFacts.GetWarningLevel(ErrorCode.WRN_TypeNotFoundForNoPIAWarning));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_DynamicDispatchToConditionalMethod));
            Assert.Equal(3, ErrorFacts.GetWarningLevel(ErrorCode.WRN_IsDynamicIsConfusing));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_NoSources));

            // There is space in the range of error codes from 7000-8999 that we might add for new errors in post-Dev10.
            // If a new warning is added, this test will fail and adding the new case with the expected error level will be required.

            for (int i = 7000; i < 9000; i++)
            {
                ErrorCode errorCode = (ErrorCode)i;
                string errorCodeName = errorCode.ToString();
                if (errorCodeName.StartsWith("WRN"))
                {
                    Assert.True(ErrorFacts.IsWarning(errorCode));
                    switch (errorCode)
                    {
                        case ErrorCode.WRN_MainIgnored:
                            Assert.Equal(2, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_DelaySignButNoKey:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_InvalidVersionFormat:
                            Assert.Equal(4, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_UnimplementedCommandLineSwitch:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_RefCultureMismatch:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_ConflictingMachineAssembly:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_UnqualifiedNestedTypeInCref:
                            Assert.Equal(2, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_NoRuntimeMetadataVersion:
                            Assert.Equal(2, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_FilterIsConstant:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_PdbLocalNameTooLong:
                            Assert.Equal(3, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_AnalyzerCannotBeCreated:
                        case ErrorCode.WRN_NoAnalyzerInAssembly:
                        case ErrorCode.WRN_UnableToLoadAnalyzer:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        default:
                            // If a new warning is added, this test will fail
                            // and whoever is adding the new warning will have to update it with the expected error level.
                            Assert.True(false, "Please update this test case with a proper warning level for '" + errorCodeName + "'");
                            break;
                    }
                }
            }
        }

        [Fact]
        public void Warning_1()
        {
            var text = @"


public class C
{
    static private volatile int i;
    static public void Test (ref int i) {}
    public static void Main()
    {
        Test (ref i);
    }	
}
";

            CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe).VerifyDiagnostics(
                // (10,19): warning CS0420: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(420), ReportDiagnostic.Suppress);
            CSharpCompilationOptions option = TestOptions.Exe.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = TestOptions.Exe.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (10,19): error CS0420: Warning as Error: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i").WithWarningAsError(true));

            warnings[MessageProvider.Instance.GetIdForErrorCode(420)] = ReportDiagnostic.Error;
            option = TestOptions.Exe.WithGeneralDiagnosticOption(ReportDiagnostic.Default).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (10,19): error CS0420: Warning as Error: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i").WithWarningAsError(true));
        }

        [Fact]
        public void Warning_2()
        {
            var text = @"


public class C
{
    public static void Main()
    {
	int x;
	int j = 0;
    }	
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (8,6): warning CS0168: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Suppress);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Error;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,6): error CS0168: Warning as Error: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,6): warning CS0168: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_2()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning restore
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning disable
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_3()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore 168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_4()
        {
            var text = @"


public class C
{
    public static void Main()
    {
#pragma warning restore 168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning disable 168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (9,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_5()
        {
            var text = @"
public class C
{
    public static void Run()
    {
#pragma warning disable
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (17,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_6()
        {
            var text = @"
#pragma warning disable
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (16,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_7()
        {
            var text = @"

#pragma warning disable 168, 219
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (17,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_8()
        {
            var text = @"
#pragma warning disable 168, 219
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore 219
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics();

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_9()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable ""CS0168""
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore ""CS0168""
        int z;
    }
}
";
            // Verify that a warning can be disabled/restored with a string literal
            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_10()
        {
            var text = @"
#pragma warning disable ""CS0465"", 168, ""CS0219""
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public virtual void Finalize() // CS0465
    {
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";
            // Verify that warnings can be disabled using a mixed list of numeric and string literals
            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (20,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (20,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (20,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarningWithErrors_1()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (7,8): warning CS1633: Unrecognized #pragma directive
                // #pragma
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""),
                // (8,17): warning CS0168: The variable 'x' is declared but never used
                //             int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,17): warning CS0219: The variable 'y' is assigned but its value is never used
                //             int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,17): warning CS0168: The variable 'z' is declared but never used
                //             int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,8): warning CS1633: Unrecognized #pragma directive
                // #pragma
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""),
                // (8,17): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //             int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,17): warning CS0219: The variable 'y' is assigned but its value is never used
                //             int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,17): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //             int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(1633), ReportDiagnostic.Suppress);
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,17): warning CS0168: The variable 'x' is declared but never used
                //             int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,17): warning CS0219: The variable 'y' is assigned but its value is never used
                //             int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,17): warning CS0168: The variable 'z' is declared but never used
                //             int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,8): warning CS1633: Unrecognized #pragma directive
                // #pragma
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""));
        }

        [Fact]
        public void PragmaWarningWithErrors_2()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 1633
#pragma
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarningWithErrors_3()
        {
            var text = @"

public class C
{
    public static void Main()
   {
#pragma warning
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (7,16): warning CS1634: Expected disable or restore
                // #pragma warning
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""),
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,16): warning CS1634: Expected disable or restore
                // #pragma warning
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""),
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,16): warning CS1634: Expected disable or restore
                // #pragma warning
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""));
        }

        [Fact]
        public void PragmaWarningWithErrors_4()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 1
#pragma warning disable ""CS168""
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore all
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (6,25): warning CS1691: '1' is not a valid warning number
                // #pragma warning disable 1
                Diagnostic(ErrorCode.WRN_BadWarningNumber, "1").WithArguments("1"),
                // (7,25): warning CS1691: 'CS168' is not a valid warning number
                // #pragma warning disable "CS168"
                Diagnostic(ErrorCode.WRN_BadWarningNumber, @"""CS168""").WithArguments("CS168"),
                // (9,25): warning CS1072: String or numeric literal expected
                // #pragma warning restore all
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"),
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (6,25): warning CS1691: '1' is not a valid warning number
                // #pragma warning disable 1
                Diagnostic(ErrorCode.WRN_BadWarningNumber, "1").WithArguments("1"),
                // (7,25): warning CS1691: 'CS168' is not a valid warning number
                // #pragma warning disable "CS168"
                Diagnostic(ErrorCode.WRN_BadWarningNumber, @"""CS168""").WithArguments("CS168"),
                // (9,25): warning CS1072: String or numeric literal expected
                // #pragma warning restore all
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"),
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (6,25): warning CS1691: '1' is not a valid warning number
                // #pragma warning disable 1
                Diagnostic(ErrorCode.WRN_BadWarningNumber, "1").WithArguments("1"),
                // (7,25): warning CS1691: 'CS168' is not a valid warning number
                // #pragma warning disable "CS168"
                Diagnostic(ErrorCode.WRN_BadWarningNumber, @"""CS168""").WithArguments("CS168"),
                // (9,25): warning CS1072: String or numeric literal expected
                // #pragma warning restore all
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"));
        }

        [Fact]
        public void PragmaWarningWithErrors_5()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning disable 1, 168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (7,25): warning CS1691: '1' is not a valid warning number
                // #pragma warning disable 1, 168
                Diagnostic(ErrorCode.WRN_BadWarningNumber, "1").WithArguments("1"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,25): warning CS1691: '1' is not a valid warning number
                // #pragma warning disable 1, 168
                Diagnostic(ErrorCode.WRN_BadWarningNumber, "1").WithArguments("1"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,25): warning CS1691: '1' is not a valid warning number
                // #pragma warning disable 1, 168
                Diagnostic(ErrorCode.WRN_BadWarningNumber, "1").WithArguments("1"));
        }

        [Fact]
        public void PragmaWarningWithErrors_6()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning disable all, 168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (7,25): warning CS1072: String or numeric literal expected
                // #pragma warning disable all, 168
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"),
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,25): warning CS1072: String or numeric literal expected
                // #pragma warning disable all, 168
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"),
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,25): warning CS1072: String or numeric literal expected
                // #pragma warning disable all, 168
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"));
        }

        [Fact]
        public void PragmaWarningWithErrors_7()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning disable 168, all
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (7,30): warning CS1072: String or numeric literal expected
                // #pragma warning disable 168, all
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,30): warning CS1072: String or numeric literal expected
                // #pragma warning disable 168, all
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,30): warning CS1072: String or numeric literal expected
                // #pragma warning disable 168, all
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, "all"));
        }

        [Fact]
        public void PragmaWarningWithErrors_8()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore 1
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics();

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarningWithErrors_9()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore 1
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics();

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarningWithErrors_10()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning restore
        int x;      // CS0168
        int y = 0;  // CS0219
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarningWithErrors_11()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning disable ""CS0168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (7,25): error CS1010: Newline in constant
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.ERR_NewlineInConst, ""),
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,25): error CS1010: Newline in constant
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.ERR_NewlineInConst, ""),
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,25): error CS1010: Newline in constant
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.ERR_NewlineInConst, ""),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (7,25): error CS1010: Newline in constant
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.ERR_NewlineInConst, ""));
        }

        [Fact]
        public void PragmaWarningWithErrors_12()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable ,
        int x;      // CS0168
#pragma warning restore , ,
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (6,25): warning CS1072: String or numeric literal expected
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (6,25): warning CS1072: String or numeric literal expected
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (6,25): warning CS1072: String or numeric literal expected
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, compOptions: option).VerifyDiagnostics(
                // (6,25): warning CS1072: String or numeric literal expected
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: String or numeric literal expected
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_StringOrNumericLiteralExpected, ","));
        }

        [WorkItem(546814, "DevDiv")]
        [Fact]
        public void PragmaWarningAlign_0()
        {
            var text = @"
using System;

class Program
{
#pragma warning disable 1691
#pragma warning disable 59526
        public static void Main() { Console.Read(); }

#pragma warning restore 1691, 56529
} ";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics();

        }

        [WorkItem(546814, "DevDiv")]
        [Fact]
        public void PragmaWarningAlign_1()
        {
            var text = @"
using System;

class Program
{
#pragma warning disable 1691, 59526
        public static void Main() { Console.Read(); }

#pragma warning restore 1691, 56529
} ";

            CSharpCompilationOptions commonoption = TestOptions.Exe;
            CreateCompilationWithMscorlib(text, compOptions: commonoption).VerifyDiagnostics(
                // (6,31): warning CS1691: '59526' is not a valid warning number
                // #pragma warning disable 1691, 59526
                Diagnostic(ErrorCode.WRN_BadWarningNumber, "59526").WithArguments("59526")
            );

        }

        [Fact]
        public void PragmaWarningDirectiveMap()
        {
            var text = @"
using System;
public class C
{
#pragma warning disable 
    public static void Main()
#pragma warning restore 168
    {
        int x;
#pragma warning disable 168
        int y;      // CS0168
#pragma warning restore
        int z = 0;  // CS0219
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, "foo.cs");
            Assert.Equal(ReportDiagnostic.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "public class").Start));
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "public static").Start));
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "public static").Start));
            Assert.Equal(ReportDiagnostic.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "int x").Start));
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "int x").Start));
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "int y").Start));
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "int y").Start));
            Assert.Equal(ReportDiagnostic.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "int z").Start));
            Assert.Equal(ReportDiagnostic.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "int z").Start));
        }

        [Fact]
        public void PragmaWarningDirectiveMapWithIfDirective()
        {
            var text = @"
using System;
class Program
{
    static void Main(string[] args)
    {
#pragma warning disable
        var x = 10;
#if false
#pragma warning restore
#endif
        var y = 10;
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, "foo.cs");
            Assert.Equal(ReportDiagnostic.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "static void").Start));
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "var x").Start));
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "var y").Start));
        }

        [WorkItem(545407, "DevDiv")]
        [Fact]
        public void PragmaWarningDirectiveMapAtTheFirstLine()
        {
            var text = @"#pragma warning disable
using System;
class Program
{
    static void Main(string[] args)
    {
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, "foo.cs");
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "static void").Start));
        }

        private TextSpan GetSpanIn(SyntaxTree syntaxTree, string textToFind)
        {
            string s = syntaxTree.GetText().ToString();
            int index = s.IndexOf(textToFind);
            Assert.True(index >= 0, "textToFind not found in the tree");
            return new TextSpan(index, textToFind.Length);
        }

        [WorkItem(543705, "DevDiv")]
        [Fact]
        public void GetDiagnosticsCalledTwice()
        {
            var text = @"
interface IMyEnumerator { }

public class Test
{
    static IMyEnumerator Foo()
    {
        yield break;
    }

    public static int Main()
    {
        return 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text);

            Assert.Equal(1, compilation.GetDiagnostics().Length);
            Assert.Equal(1, compilation.GetDiagnostics().Length);
        }

        #region Mocks
        internal class CustomErrorInfo : DiagnosticInfo
        {
            public readonly object OtherSymbol;
            public readonly Location OtherLocation;
            public override IReadOnlyList<Location> AdditionalLocations
            {
                get
                {
                    return new Location[1] { OtherLocation };
                }
            }

            public CustomErrorInfo(CommonMessageProvider provider, object otherSymbol, Location otherLocation)
                : base(provider, 2)
            {
                this.OtherSymbol = otherSymbol;
                this.OtherLocation = otherLocation;
            }
        }

        internal class MockMessageProvider : TestMessageProvider
        {
            public override DiagnosticSeverity GetSeverity(int code)
            {
                if (code >= 1000)
                {
                    return DiagnosticSeverity.Warning;
                }
                else
                {
                    return DiagnosticSeverity.Error;
                }
            }

            public override string LoadMessage(int code, CultureInfo language)
            {
                switch (code)
                {
                    case 1:
                        return "The first error";
                    case 2:
                        return "The second error is associated with symbol {0}";
                    case 1001:
                        return "The first warning";
                    case 1002:
                        return "The second warning about {0} and {1}";
                    default:
                        return null;
                }
            }

            public override string CodePrefix
            {
                get { return "MOCK"; }
            }

            public override int GetWarningLevel(int code)
            {
                if (code >= 1000)
                {
                    return code % 4 + 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        #endregion
    }
}
