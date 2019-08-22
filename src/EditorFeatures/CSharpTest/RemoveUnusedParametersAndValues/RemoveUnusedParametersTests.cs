// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues
{
    public class RemoveUnusedParametersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), new CSharpRemoveUnusedValuesCodeFixProvider());

        private IDictionary<OptionKey, object> NonPublicMethodsOnly =>
            Option(CodeStyleOptions.UnusedParameters,
                new CodeStyleOption<UnusedParametersPreference>(UnusedParametersPreference.NonPublicMethods, NotificationOption.Suggestion));

        // Ensure that we explicitly test missing UnusedParameterDiagnosticId, which has no corresponding code fix (non-fixable diagnostic).
        private Task TestDiagnosticMissingAsync(string initialMarkup, ParseOptions parseOptions = null)
            => TestDiagnosticMissingAsync(initialMarkup, options: null, parseOptions);
        private Task TestDiagnosticsWithAsync(string initialMarkup, ParseOptions parseOptions, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, options: null, parseOptions, expectedDiagnostics);
        private Task TestDiagnosticsAsync(string initialMarkup, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, options: null, parseOptions: null, expectedDiagnostics);
        private Task TestDiagnosticMissingAsync(string initialMarkup, IDictionary<OptionKey, object> options, ParseOptions parseOptions = null)
            => TestDiagnosticMissingAsync(initialMarkup, new TestParameters(parseOptions, options: options, retainNonFixableDiagnostics: true));
        private Task TestDiagnosticsAsync(string initialMarkup, IDictionary<OptionKey, object> options, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, options, parseOptions: null, expectedDiagnostics);
        private Task TestDiagnosticsAsync(string initialMarkup, IDictionary<OptionKey, object> options, ParseOptions parseOptions, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, new TestParameters(parseOptions, options: options, retainNonFixableDiagnostics: true), expectedDiagnostics);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_Used()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(int [|p|])
    {
        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_Unused()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(int [|p|])
    {
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        [InlineData("public", "public")]
        [InlineData("public", "protected")]
        public async Task Parameter_Unused_NonPrivate_NotApplicable(string typeAccessibility, string methodAccessibility)
        {
            await TestDiagnosticMissingAsync(
$@"{typeAccessibility} class C
{{
    {methodAccessibility} void M(int [|p|])
    {{
    }}
}}", NonPublicMethodsOnly);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        [InlineData("public", "private")]
        [InlineData("public", "internal")]
        [InlineData("internal", "private")]
        [InlineData("internal", "public")]
        [InlineData("internal", "internal")]
        [InlineData("internal", "protected")]
        public async Task Parameter_Unused_NonPublicMethod(string typeAccessibility, string methodAccessibility)
        {
            await TestDiagnosticsAsync(
$@"{typeAccessibility} class C
{{
    {methodAccessibility} void M(int [|p|])
    {{
    }}
}}", NonPublicMethodsOnly,
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_Unused_UnusedExpressionAssignment_PreferNone()
        {
            var unusedValueAssignmentOptionSuppressed = Option(CSharpCodeStyleOptions.UnusedValueAssignment,
                new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption.None));

            await TestDiagnosticMissingAsync(
@"class C
{
    void M(int [|p|])
    {
        var x = p;
    }
}", options: unusedValueAssignmentOptionSuppressed);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_WrittenOnly()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(int [|p|])
    {
        p = 1;
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_WrittenThenRead()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(int [|p|])
    {
        p = 1;
        var x = p;
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_WrittenOnAllControlPaths_BeforeRead()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(int [|p|], bool flag)
    {
        if (flag)
        {
            p = 0;
        }
        else
        {
            p = 1;
        }

        var x = p;
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_WrittenOnSomeControlPaths_BeforeRead()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(int [|p|], bool flag, bool flag2)
    {
        if (flag)
        {
            if (flag2)
            {
                p = 0;
            }
        }
        else
        {
            p = 1;
        }

        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OptionalParameter_Unused()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(int [|p|] = 0)
    {
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_UsedInConstructorInitializerOnly()
        {
            await TestDiagnosticMissingAsync(
@"
class B
{
    protected B(int p) { }
}

class C: B
{
    C(int [|p|])
    : base(p)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_NotUsedInConstructorInitializer_UsedInConstructorBody()
        {
            await TestDiagnosticMissingAsync(
@"
class B
{
    protected B(int p) { }
}

class C: B
{
    C(int [|p|])
    : base(0)
    {
        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_UsedInConstructorInitializerAndConstructorBody()
        {
            await TestDiagnosticMissingAsync(
@"
class B
{
    protected B(int p) { }
}

class C: B
{
    C(int [|p|])
    : base(p)
    {
        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedLocalFunctionParameter()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(int y)
    {
        LocalFunction(y);
        void LocalFunction(int [|p|])
        {
        }
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedLocalFunctionParameter_02()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M()
    {
        LocalFunction(0);
        void LocalFunction(int [|p|])
        {
        }
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedLocalFunctionParameter_Discard()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M()
    {
        LocalFunction(0);
        void LocalFunction(int [|_|])
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedLocalFunctionParameter_PassedAsDelegateArgument()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    void M()
    {
        M2(LocalFunction);
        void LocalFunction(int [|p|])
        {
        }
    }

    void M2(Action<int> a) => a(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UsedInLambda_ReturnsDelegate()
        {
            // Currently we bail out from analysis for method returning delegate types.
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    private static Action<int> M(object [|p|] = null, Action<object> myDelegate)
    {
        return d => { myDelegate(p); };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedInLambda_ReturnsDelegate()
        {
            // We bail out from unused value analysis for method returning delegate types.
            // We should still report unused parameters.
            await TestDiagnosticsAsync(
@"using System;

class C
{
    private static Action M(object [|p|])
    {
        return () => { };
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedInLambda_LambdaPassedAsArgument()
        {
            // We bail out from unused value analysis when lambda is passed as argument.
            // We should still report unused parameters.
            await TestDiagnosticsAsync(
@"using System;

class C
{
    private static void M(object [|p|])
    {
        M2(() => { });
    }

    private static void M2(Action a) { }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task ReadInLambda_LambdaPassedAsArgument()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    private static void M(object [|p|])
    {
        M2(() => { M3(p); });
    }

    private static void M2(Action a) { }

    private static void M3(object o) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OnlyWrittenInLambda_LambdaPassedAsArgument()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    private static void M(object [|p|])
    {
        M2(() => { M3(out p); });
    }

    private static void M2(Action a) { }

    private static void M3(out object o) { o = null; }
}");
        }

        [WorkItem(31744, "https://github.com/dotnet/roslyn/issues/31744")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedInExpressionTree_PassedAsArgument()
        {
            await TestDiagnosticsAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1(object [|p|])
    {
        M2(x => x.M3());
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3() { return 0; }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [WorkItem(31744, "https://github.com/dotnet/roslyn/issues/31744")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task ReadInExpressionTree_PassedAsArgument()
        {
            await TestDiagnosticMissingAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1(object [|p|])
    {
        M2(x => x.M3(p));
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3(object o) { return 0; }
}");
        }

        [WorkItem(31744, "https://github.com/dotnet/roslyn/issues/31744")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OnlyWrittenInExpressionTree_PassedAsArgument()
        {
            await TestDiagnosticMissingAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1(object [|p|])
    {
        M2(x => x.M3(out p));
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3(out object o) { o = null; return 0; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UsedInLambda_AssignedToField()
        {
            // Currently we bail out from analysis if we have a delegate creation that is not assigned
            // too a local/parameter.
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    private Action _field;
    private static void M(object [|p|])
    {
        _field = () => { Console.WriteLine(p); };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task MethodWithLockAndControlFlow()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    private static readonly object s_gate = new object();

    public static C M(object [|p|], bool flag, C c1, C c2)
    {
        C c;
        lock (s_gate)
        {
            c = flag > 0 ? c1 : c2;
        }
 
        c.M2(p);
        return c;
    }

    private void M2(object p) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedLambdaParameter()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    void M(int y)
    {
        Action<int> myLambda = [|p|] =>
        {
        };

        myLambda(y);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedLambdaParameter_Discard()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    void M(int y)
    {
        Action<int> myLambda = [|_|] =>
        {
        };

        myLambda(y);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UsedLocalFunctionParameter()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(int y)
    {
        LocalFunction(y);
        void LocalFunction(int [|p|])
        {
            var x = p;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UsedLambdaParameter()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    void M(int y)
    {
        Action<int> myLambda = [|p|] =>
        {
            var x = p;
        }

        myLambda(y);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OptionalParameter_Used()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(int [|p = 0|])
    {
        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task InParameter()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(in int [|p|])
    {
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_Unused()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(ref int [|p|])
    {
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_WrittenOnly()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(ref int [|p|])
    {
        p = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_ReadOnly()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(ref int [|p|])
    {
        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_ReadThenWritten()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(ref int [|p|])
    {
        var x = p;
        p = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_WrittenAndThenRead()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(ref int [|p|])
    {
        p = 1;
        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_WrittenTwiceNotRead()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(ref int [|p|])
    {
        p = 0;
        p = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OutParameter_Unused()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(out int [|p|])
    {
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OutParameter_WrittenOnly()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(out int [|p|])
    {
        p = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OutParameter_WrittenAndThenRead()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(out int [|p|])
    {
        p = 0;
        var x = p;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OutParameter_WrittenTwiceNotRead()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(out int [|p|])
    {
        p = 0;
        p = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_ExternMethod()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    [System.Runtime.InteropServices.DllImport(nameof(M))]
    static extern void M(int [|p|]);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_AbstractMethod()
        {
            await TestDiagnosticMissingAsync(
@"abstract class C
{
    protected abstract void M(int [|p|]);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_VirtualMethod()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    protected virtual void M(int [|p|])
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_OverriddenMethod()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    protected virtual void M(int p)
    {
        var x = p;
    }
}

class D : C
{
    protected override void M(int [|p|])
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_ImplicitInterfaceImplementationMethod()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    void M(int p);
}
class C: I
{
    public void M(int [|p|])
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_ExplicitInterfaceImplementationMethod()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    void M(int p);
}
class C: I
{
    void I.M(int [|p|])
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_IndexerMethod()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    int this[int [|p|]]
    {
        get { return 0; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_ConditionalDirective()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(int [|p|])
    {
#if DEBUG
        System.Console.WriteLine(p);
#endif
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_EventHandler_FirstParameter()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public void MyHandler(object [|obj|], System.EventArgs args)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_EventHandler_SecondParameter()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public void MyHandler(object obj, System.EventArgs [|args|])
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_MethodUsedAsEventHandler()
        {
            await TestDiagnosticMissingAsync(
@"using System;

public delegate void MyDelegate(int x);

class C
{
    private event MyDelegate myDel;

    void M(C c)
    {
        c.myDel += Handler;
    }

    void Handler(int [|x|])
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_CustomEventArgs()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public class CustomEventArgs : System.EventArgs
    {
    }

    public void MyHandler(object [|obj|], CustomEventArgs args)
    {
    }
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        [InlineData(@"[System.Diagnostics.Conditional(nameof(M))]")]
        [InlineData(@"[System.Obsolete]")]
        [InlineData(@"[System.Runtime.Serialization.OnDeserializingAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnDeserializedAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnSerializingAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnSerializedAttribute]")]
        public async Task Parameter_MethodsWithSpecialAttributes(string attribute)
        {
            await TestDiagnosticMissingAsync(
$@"class C
{{
    {attribute}
    void M(int [|p|])
    {{
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        [InlineData("System.Composition", "ImportingConstructorAttribute")]
        [InlineData("System.ComponentModel.Composition", "ImportingConstructorAttribute")]
        public async Task Parameter_ConstructorsWithSpecialAttributes(string attributeNamespace, string attributeName)
        {
            await TestDiagnosticMissingAsync(
$@"
namespace {attributeNamespace}
{{
    public class {attributeName} : System.Attribute {{ }}
}}

class C
{{
    [{attributeNamespace}.{attributeName}()]
    public C(int [|p|])
    {{
    }}
}}");
        }

        [WorkItem(32133, "https://github.com/dotnet/roslyn/issues/32133")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_SerializationConstructor()
        {
            await TestDiagnosticMissingAsync(
@"
using System;
using System.Runtime.Serialization;

internal sealed class NonSerializable
{
    public NonSerializable(string value) => Value = value;

    public string Value { get; set; }
}

[Serializable]
internal sealed class CustomSerializingType : ISerializable
{
    private readonly NonSerializable _nonSerializable;

    public CustomSerializingType(SerializationInfo info, StreamingContext [|context|])
    {
        _nonSerializable = new NonSerializable(info.GetString(""KEY""));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(""KEY"", _nonSerializable.Value);
    }
}");
        }

        [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_DiagnosticMessages()
        {
            var source =
@"public class C
{
    // p1 is unused.
    // p2 is written before read.
    [|int M(int p1, int p2)
    {
        p2 = 0;
        return p2;
    }

    // p3 is unused parameter of a public API.
    // p4 is written before read parameter of a public API.
    public int M2(int p3, int p4)
    {
        p4 = 0;
        return p4;
    }|]
}";
            var testParameters = new TestParameters(retainNonFixableDiagnostics: true);
            using (var workspace = CreateWorkspaceFromOptions(source, testParameters))
            {
                var diagnostics = await GetDiagnosticsAsync(workspace, testParameters).ConfigureAwait(false);
                diagnostics.Verify(
                    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p1").WithLocation(5, 15),
                    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p2").WithLocation(5, 23),
                    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p3").WithLocation(13, 23),
                    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p4").WithLocation(13, 31));
                var sortedDiagnostics = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

                Assert.Equal("Remove unused parameter 'p1'", sortedDiagnostics[0].GetMessage());
                Assert.Equal("Remove unused parameter 'p2', its initial value is never used", sortedDiagnostics[1].GetMessage());
                Assert.Equal("Remove unused parameter 'p3' if it is not part of a shipped public API", sortedDiagnostics[2].GetMessage());
                Assert.Equal("Remove unused parameter 'p4' if it is not part of a shipped public API, its initial value is never used", sortedDiagnostics[3].GetMessage());
            }
        }

        [WorkItem(32287, "https://github.com/dotnet/roslyn/issues/32287")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_DeclarationPatternWithNullDeclaredSymbol()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(object [|o|])
    {
        if (o is int _)
        {
        }
    }
}");
        }

        [WorkItem(32851, "https://github.com/dotnet/roslyn/issues/32851")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_Unused_SpecialNames()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    [|void M(int _, char _1, C _3)|]
    {
    }
}");
        }

        [WorkItem(32851, "https://github.com/dotnet/roslyn/issues/32851")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_Used_SemanticError()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    void M(int [|x|])
    {
        // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type.
        Invoke<string>(() => x);

        T Invoke<T>(Func<T> a) { return a(); }
    }
}");
        }

        [WorkItem(32851, "https://github.com/dotnet/roslyn/issues/32851")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task Parameter_Unused_SemanticError()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M(int [|x|])
    {
        // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type.
        Invoke<string>(() => 0);

        T Invoke<T>(Func<T> a) { return a(); }
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [WorkItem(32973, "https://github.com/dotnet/roslyn/issues/32973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task OutParameter_LocalFunction()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public static bool M(out int x)
    {
        return LocalFunction(out x);

        bool LocalFunction(out int [|y|])
        {
            y = 0;
            return true;
        }
    }
}");
        }

        [WorkItem(32973, "https://github.com/dotnet/roslyn/issues/32973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_Unused_LocalFunction()
        {
            await TestDiagnosticsAsync(
@"class C
{
    public static bool M(ref int x)
    {
        return LocalFunction(ref x);

        bool LocalFunction(ref int [|y|])
        {
            return true;
        }
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [WorkItem(32973, "https://github.com/dotnet/roslyn/issues/32973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RefParameter_Used_LocalFunction()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public static bool M(ref int x)
    {
        return LocalFunction(ref x);

        bool LocalFunction(ref int [|y|])
        {
            y = 0;
            return true;
        }
    }
}");
        }

        [WorkItem(33299, "https://github.com/dotnet/roslyn/issues/33299")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task NullCoalesceAssignment()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public static void M(C [|x|])
    {
        x ??= new C();
    }
}", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [WorkItem(34301, "https://github.com/dotnet/roslyn/issues/34301")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task GenericLocalFunction()
        {
            await TestDiagnosticsAsync(
@"class C
{
    void M()
    {
        LocalFunc(0);

        void LocalFunc<T>(T [|value|])
        {
        }
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [WorkItem(36715, "https://github.com/dotnet/roslyn/issues/36715")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task GenericLocalFunction_02()
        {
            await TestDiagnosticsAsync(
@"using System.Collections.Generic;

class C
{
    void M(object [|value|])
    {
        try
        {
            value = LocalFunc(0);
        }
        finally
        {
            value = LocalFunc(0);
        }

        return;

        IEnumerable<T> LocalFunc<T>(T value)
        {
            yield return value;
        }
    }
}",
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId));
        }

        [WorkItem(36715, "https://github.com/dotnet/roslyn/issues/36715")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task GenericLocalFunction_03()
        {
            await TestDiagnosticsAsync(
@"using System;
using System.Collections.Generic;

class C
{
    void M(object [|value|])
    {
        Func<object, IEnumerable<object>> myDel = LocalFunc;
        try
        {
            value = myDel(value);
        }
        finally
        {
            value = myDel(value);
        }

        return;

        IEnumerable<T> LocalFunc<T>(T value)
        {
            yield return value;
        }
    }
}");
        }

        [WorkItem(34830, "https://github.com/dotnet/roslyn/issues/34830")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RegressionTest_ShouldReportUnusedParameter()
        {
            var options = Option(CodeStyleOptions.UnusedParameters,
                new CodeStyleOption<UnusedParametersPreference>(default, NotificationOption.Suggestion));

            await TestDiagnosticMissingAsync(
@"using System;
using System.Threading.Tasks;

public interface I { event Action MyAction; }

public sealed class C : IDisposable
{
    private readonly Task<I> task;

    public C(Task<I> [|task|])
    {
        this.task = task;
        Task.Run(async () => (await task).MyAction += myAction);
    }

    private void myAction() { }

    public void Dispose() => task.Result.MyAction -= myAction;
}", options);
        }

        [WorkItem(37326, "https://github.com/dotnet/roslyn/issues/37326")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task RegressionTest_ShouldReportUnusedParameter_02()
        {
            var options = Option(CodeStyleOptions.UnusedParameters,
                new CodeStyleOption<UnusedParametersPreference>((UnusedParametersPreference)2, NotificationOption.Suggestion));

            await TestDiagnosticMissingAsync(
@"using System;
using System.Threading.Tasks;

public interface I { event Action MyAction; }

public sealed class C : IDisposable
{
    private readonly Task<I> task;

    public C(Task<I> [|task|])
    {
        this.task = task;
        Task.Run(async () => (await task).MyAction += myAction);
    }

    private void myAction() { }

    public void Dispose() => task.Result.MyAction -= myAction;
}", options);
        }

        [WorkItem(37483, "https://github.com/dotnet/roslyn/issues/37483")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task MethodUsedAsDelegateInGeneratedCode_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(
@"using System;

public partial class C
{
    private void M(int [|x|])
    {
    }
}

public partial class C
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    public void M2(out Action<int> a)
    {
        a = M;
    }
}  
");
        }

        [WorkItem(37483, "https://github.com/dotnet/roslyn/issues/37483")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task UnusedParameterInGeneratedCode_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(
@"public partial class C
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private void M(int [|x|])
    {
    }
}
");
        }

        [WorkItem(36817, "https://github.com/dotnet/roslyn/issues/36817")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
        public async Task ParameterWithoutName_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(
@"public class C
{
    public void M[|(int )|]
    {
    }
}");
        }
    }
}
