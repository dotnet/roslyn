// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
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
                if (errorCodeName.StartsWith("WRN", StringComparison.Ordinal))
                {
                    Assert.True(ErrorFacts.IsWarning(errorCode));
                    Assert.NotEqual(0, ErrorFacts.GetWarningLevel(errorCode));
                }
                else if (errorCodeName.StartsWith("ERR", StringComparison.Ordinal))
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
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_DynamicDispatchToConditionalMethod));
            Assert.Equal(3, ErrorFacts.GetWarningLevel(ErrorCode.WRN_IsDynamicIsConfusing));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_NoSources));

            // There is space in the range of error codes from 7000-8999 that we might add for new errors in post-Dev10.
            // If a new warning is added, this test will fail and adding the new case with the expected error level will be required.

            for (int i = 7000; i < 9000; i++)
            {
                ErrorCode errorCode = (ErrorCode)i;
                string errorCodeName = errorCode.ToString();
                if (errorCodeName.StartsWith("WRN", StringComparison.Ordinal))
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
                        case ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName:
                        case ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName:
                        case ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath:
                        case ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden:
                        case ErrorCode.WRN_RefCultureMismatch:
                        case ErrorCode.WRN_ConflictingMachineAssembly:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_NubExprIsConstBool2:
                        case ErrorCode.WRN_UnqualifiedNestedTypeInCref:
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
                        case ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName:
                        case ErrorCode.WRN_AlignmentMagnitude:
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

            CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (10,19): warning CS0420: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(420), ReportDiagnostic.Suppress);
            CSharpCompilationOptions option = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (10,19): error CS0420: Warning as Error: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i").WithWarningAsError(true));

            warnings[MessageProvider.Instance.GetIdForErrorCode(420)] = ReportDiagnostic.Error;
            option = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Default).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (8,6): warning CS0168: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Suppress);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Error;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,6): error CS0168: Warning as Error: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,6): warning CS0168: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoErrorCodes1()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoErrorCodes2()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning restore // comment
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning disable // comment
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericErrorCodes1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore 168 // comment
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodes1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS0168 // comment
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore CS0168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericErrorCodes2()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (9,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodes2()
        {
            var text = @"


public class C
{
    public static void Main()
    {
#pragma warning restore CS0168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning disable CS0168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (9,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodesAreCaseSensitive()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable cs0168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore cs0168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13).WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13).WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodesMustMatchExactly1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS168, CS0219L
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore CS0219L
        int z;      // CS0168
#pragma warning disable CS00168
        int w;      // CS0168
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13),
                // (12,13): warning CS0168: The variable 'w' is declared but never used
                //         int w;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "w").WithArguments("w").WithLocation(12, 13));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13).WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13).WithWarningAsError(true),
                // (12,13): error CS0168: Warning as Error: The variable 'w' is declared but never used
                //         int w;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "w").WithArguments("w").WithLocation(12, 13).WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13),
                // (12,13): warning CS0168: The variable 'w' is declared but never used
                //         int w;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "w").WithArguments("w").WithLocation(12, 13));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodesMustMatchExactly2()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable ＣＳ０１６８
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore ＣＳ０１６８
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13).WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13).WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BlockScopeIsNotSignificant1()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (17,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BlockScopeIsNotSignificant2()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (16,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericAndIdentifierErrorCodes1()
        {
            var text = @"

#pragma warning disable 168, CS0219
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (17,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericAndIdentifierErrorCodes2()
        {
            var text = @"
#pragma warning disable 168, CS0219 // comment
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
#pragma warning restore CS0219
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics();

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericAndIdentifierErrorCodes3()
        {
            var text = @"
#pragma warning disable CS0465, 168, CS0219
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
            // Verify that warnings can be disabled using a mixed list of numeric literals and identifier
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (20,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (20,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (20,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BadSyntax1()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,8): warning CS1633: Unrecognized #pragma directive
                // #pragma
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""));
        }

        [Fact]
        public void PragmaWarning_BadSyntax2()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BadSyntax3()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,16): warning CS1634: Expected disable or restore
                // #pragma warning
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""));
        }

        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes1()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 1
#pragma warning disable CS168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore all
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes2()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning disable CS0001, 168, all
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes3()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics();

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_OnlyRestoreWithoutDisableIsNoOp()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_StringLiteralsAreNotAllowed()
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
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25),
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25),
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25));
        }

        [Fact]
        public void PragmaWarning_MostKeywordsAreAllowedAsErrorCodes()
        {
            // Lexing / parsing of identifiers inside #pragma is identical to that inside #define for the below cases.
            // The #define cases below also produce no errors in previous versions of the compiler.
            var text = @"
#define class
#define static
#define int
#define public
#define null
#define warning
#define define
public class C
{
    public static void Main()
    {
#pragma warning disable class, static, int
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore warning
#pragma warning restore public, null, define
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(14, 13),
                // (13,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(15, 13));
        }

        /// <remarks>
        /// See <see cref="SyntaxFacts.IsPreprocessorContextualKeyword"/>.
        /// </remarks>
        [Fact]
        public void PragmaWarning_SomeKeywordsAreNotAllowedAsErrorCodes()
        {
            // A small number of keywords are not legal as error codes inside #pragma. This is because
            // the lexer processes these keywords specially inside preprocessor directives i.e. it returns
            // keyword tokens instead of identifier tokens for these.
            // Lexing / parsing of identifiers inside #pragma is identical to that inside #define for the below cases.
            // The #define cases below also produce identical errors in previous versions of the compiler.
            var text = @"
#define true
#define default
#define hidden
#define disable
#define checksum
#define restore
#define false
public class C
{
    public static void Main()
    {
#pragma warning disable true
#pragma warning disable default
#pragma warning disable hidden
#pragma warning disable disable
#pragma warning restore checksum
#pragma warning restore restore
#pragma warning restore false
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (2,9): error CS1001: Identifier expected
                // #define true
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "true").WithLocation(2, 9),
                // (3,9): error CS1001: Identifier expected
                // #define default
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "default").WithLocation(3, 9),
                // (4,9): error CS1001: Identifier expected
                // #define hidden
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "hidden").WithLocation(4, 9),
                // (5,9): error CS1001: Identifier expected
                // #define disable
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "disable").WithLocation(5, 9),
                // (6,9): error CS1001: Identifier expected
                // #define checksum
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "checksum").WithLocation(6, 9),
                // (7,9): error CS1001: Identifier expected
                // #define restore
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "restore").WithLocation(7, 9),
                // (8,9): error CS1001: Identifier expected
                // #define false
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "false").WithLocation(8, 9),
                // (13,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable true
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "true").WithLocation(13, 25),
                // (14,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable default
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "default").WithLocation(14, 25),
                // (15,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable hidden
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "hidden").WithLocation(15, 25),
                // (16,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable disable
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "disable").WithLocation(16, 25),
                // (17,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore checksum
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "checksum").WithLocation(17, 25),
                // (18,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore restore
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "restore").WithLocation(18, 25),
                // (19,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore false
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "false").WithLocation(19, 25));
        }

        [Fact]
        public void PragmaWarning_VeryLongIdentifiersAreAllowed()
        {
            var text = @"
#define __A_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
public class C
{
    public static void Main()
    {
#pragma warning disable __B_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890, CS0168, CS0219
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore __B_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890, CS0168, CS0219
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics();

            var nodes = ParseWithRoundTripCheck(text).GetRoot().DescendantNodes(descendIntoTrivia: true);
            var defineName = nodes.OfType<Syntax.DefineDirectiveTriviaSyntax>().Single().Name;
            var errorCodeName = nodes.OfType<Syntax.PragmaWarningDirectiveTriviaSyntax>().First()
                                     .ErrorCodes.OfType<Syntax.IdentifierNameSyntax>().First().Identifier;

            // Lexing / parsing of identifiers inside #pragma warning directives is identical
            // to that inside #define directives except that very long identifiers inside #define
            // are truncated to 128 characters to maintain backwards compatibility with previous
            // versions of the compiler.
            Assert.Equal(128, defineName.ValueText.Length);
            Assert.Equal(2335, defineName.Text.Length);

            // Since support for identifiers inside #pragma warning directives is new, 
            // we don't have any backwards compatibility constraints. So we can preserve the
            // identifier exactly as it appears in source.
            Assert.Equal(2335, errorCodeName.ValueText.Length);
            Assert.Equal(2335, errorCodeName.Text.Length);
        }

        [Fact]
        public void PragmaWarning_EscapedKeywordsAreNotAllowedAsErrorCodes()
        {
            var text = @"
#define @true
#define @class
public class C
{
    public static void Main()
    {
#pragma warning disable @true
#pragma warning restore @class
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (2,9): error CS1001: Identifier expected
                // #define @true
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "@").WithLocation(2, 9),
                // (3,9): error CS1001: Identifier expected
                // #define @class
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "@").WithLocation(3, 9),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable @true
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "@").WithLocation(8, 25),
                // (9,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore @class
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "@").WithLocation(9, 25));
        }

        [Fact]
        public void PragmaWarning_ExpressionsAreNotAllowedAsErrorCodes()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS0168 + CS0219
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore CS0168.Empty

#pragma warning disable (CS0168)
        int z;      // CS0168
#pragma warning restore -168
#pragma warning restore 168.1
#pragma warning restore 168L
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (6,32): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning disable CS0168 + CS0219
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, "+").WithLocation(6, 32),
                // (9,31): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning restore CS0168.Empty
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, ".").WithLocation(9, 31),
                // (11,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable (CS0168)
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "(").WithLocation(11, 25),
                // (13,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore -168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "-").WithLocation(13, 25),
                // (14,28): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning restore 168.1
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, ".").WithLocation(14, 28),
                // (15,28): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning restore 168L
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, "L").WithLocation(15, 28),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(12, 13));
        }

        [Fact]
        public void PragmaWarning_WarningsForBadPragmaSyntaxCanBeSuppressed()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS1072, CS1634
#pragma warning disable ~class
#pragma warning restore ""CS0219
#pragma warning blah
#pragma warning restore

#pragma warning disable @class
#pragma warning restore ""CS0168
#pragma warning blah
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (12,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable @class
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "@").WithLocation(12, 25),
                // (13,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(13, 25),
                // (14,17): warning CS1634: Expected disable or restore
                // #pragma warning blah
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, "blah").WithLocation(14, 17));
        }

        [Fact]
        public void PragmaWarning_ErrorsCantBeSuppressed()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS0029
        int x = string.Empty;
#pragma warning restore CS0029
#pragma warning disable 29
        int y = string.Empty;
#pragma warning restore 29

    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (7,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         int x = string.Empty;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "string.Empty").WithArguments("string", "int").WithLocation(7, 17),
                // (10,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         int y = string.Empty;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "string.Empty").WithArguments("string", "int").WithLocation(10, 17));
        }

        [Fact]
        public void PragmaWarning_MissingErrorCodes()
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

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","));

            option = commonoption.WithWarningLevel(2);
            CreateCompilationWithMscorlib(text, options: option).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","));
        }

        [WorkItem(546814, "DevDiv")]
        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes4()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"
using System;

class Program
{
#pragma warning disable 1691
#pragma warning disable 59526
        public static void Main() { Console.Read(); }

#pragma warning restore 1691, 56529
} ";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics();
        }

        [WorkItem(546814, "DevDiv")]
        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes5()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"
using System;

class Program
{
#pragma warning disable 1691, 59526
        public static void Main() { Console.Read(); }

#pragma warning restore 1691, 56529
} ";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: commonoption).VerifyDiagnostics();
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
#pragma warning disable CS0168
        int y;      // CS0168
#pragma warning restore
        int z = 0;  // CS0219
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, path: "foo.cs");
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
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, path: "foo.cs");
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
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, path: "foo.cs");
            Assert.Equal(ReportDiagnostic.Suppress, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "static void").Start));
        }

        private TextSpan GetSpanIn(SyntaxTree syntaxTree, string textToFind)
        {
            string s = syntaxTree.GetText().ToString();
            int index = s.IndexOf(textToFind, StringComparison.Ordinal);
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

        [Fact]
        public void TestArgumentEquality()
        {
            var text = @"
using System;

public class Test
{
    public static void Main()
    {
        (Console).WriteLine();
    }
}";
            var tree = Parse(text);

            // (8,10): error CS0119: 'Console' is a type, which is not valid in the given context
            AssertEx.Equal(CreateCompilationWithMscorlib(tree).GetDiagnostics(), CreateCompilationWithMscorlib(tree).GetDiagnostics());
        }

        /// <summary>
        /// Test that invalid type argument lists produce clean error messages
        /// with minimal noise
        /// </summary>
        [WorkItem(7177, "https://github.com/dotnet/roslyn/issues/7177")]
        [Fact]
        public void InvalidTypeArgumentList()
        {
            var text = @"using System;
public class A
{
    static void Main(string[] args)
    {
        // Invalid type arguments
        object a1 = typeof(Action<0>);
        object a2 = typeof(Action<static>);

        // Valid type arguments
        object a3 = typeof(Action<string>);
        object a4 = typeof(Action<>);

        // Invalid with multiple types
        object a5 = typeof(Func<0,1>);
        object a6 = typeof(Func<0,bool>);
        object a7 = typeof(Func<static,bool>);

        // Valid with multiple types
        object a8 = typeof(Func<string,bool>);
        object a9 = typeof(Func<,>);

        // Invalid with nested types
        object a10 = typeof(Action<Action<0>>);
        object a11 = typeof(Action<Action<static>>);
        object a12 = typeof(Action<Action<>>);

        // Valid with nested types
        object a13 = typeof(Action<Action<string>>);
    }
}";

            CSharpCompilationOptions options = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(text, options: options).VerifyDiagnostics(
                // (7,35): error CS1031: Type expected
                //         object a1 = typeof(Action<0>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(7, 35),
                // (8,35): error CS1031: Type expected
                //         object a2 = typeof(Action<static>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(8, 35),
                // (15,33): error CS1031: Type expected
                //         object a5 = typeof(Func<0,1>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(15, 33),
                // (15,35): error CS1031: Type expected
                //         object a5 = typeof(Func<0,1>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "1").WithLocation(15, 35),
                // (16,33): error CS1031: Type expected
                //         object a6 = typeof(Func<0,bool>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(16, 33),
                // (17,33): error CS1031: Type expected
                //         object a7 = typeof(Func<static,bool>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(17, 33),
                // (24,43): error CS1031: Type expected
                //         object a10 = typeof(Action<Action<0>>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(24, 43),
                // (25,43): error CS1031: Type expected
                //         object a11 = typeof(Action<Action<static>>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(25, 43),
                // (26,36): error CS7003: Unexpected use of an unbound generic name
                //         object a12 = typeof(Action<Action<>>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Action<>").WithLocation(26, 36));
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

            public override LocalizableString GetDescription(int code)
            {
                return string.Empty;
            }

            public override LocalizableString GetTitle(int code)
            {
                return string.Empty;
            }

            public override LocalizableString GetMessageFormat(int code)
            {
                return string.Empty;
            }

            public override string GetHelpLink(int code)
            {
                return string.Empty;
            }

            public override string GetCategory(int code)
            {
                return string.Empty;
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

            public override string ConvertSymbolToString(int errorCode, ISymbol symbol)
            {
                return MessageProvider.Instance.ConvertSymbolToString(errorCode, symbol);
            }
        }

        #endregion
    }
}
