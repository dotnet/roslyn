// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class AccessibilityTests : CSharpResultProviderTestBase
    {
        [WorkItem(889710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889710")]
        [Fact]
        public void HideNonPublicMembersBaseClass()
        {
            var sourceA =
@"public class A
{
    public object FA0;
    internal object FA1;
    protected internal object FA2;
    protected object FA3;
    private object FA4;
    public object PA0 { get { return null; } }
    internal object PA1 { get { return null; } }
    protected internal object PA2 { get { return null; } }
    protected object PA3 { get { return null; } }
    private object PA4 { get { return null; } }
    public object PA5 { set { } }
    public object PA6 { internal get; set; }
    public object PA7 { protected internal get; set; }
    public object PA8 { protected get; set; }
    public object PA9 { private get; set; }
    internal object PAA { private get; set; }
    protected internal object PAB { internal get; set; }
    protected internal object PAC { protected get; set; }
    protected object PAD { private get; set; }
    public static object SFA0;
    internal static object SFA1;
    protected static internal object SPA2 { get { return null; } }
    protected static object SPA3 { get { return null; } }
    public static object SPA4 { private get { return null; } set { } }
}";
            var sourceB =
@"public class B : A
{
    public object FB0;
    internal object FB1;
    protected internal object FB2;
    protected object FB3;
    private object FB4;
    public object PB0 { get { return null; } }
    internal object PB1 { get { return null; } }
    protected internal object PB2 { get { return null; } }
    protected object PB3 { get { return null; } }
    private object PB4 { get { return null; } }
    public object PB5 { set { } }
    public object PB6 { internal get; set; }
    public object PB7 { protected internal get; set; }
    public object PB8 { protected get; set; }
    public object PB9 { private get; set; }
    internal object PBA { private get; set; }
    protected internal object PBB { internal get; set; }
    protected internal object PBC { protected get; set; }
    protected object PBD { private get; set; }
    public static object SPB0 { get { return null; } }
    public static object SPB1 { internal get { return null; } set { } }
    protected static internal object SFB2;
    protected static object SFB3;
    private static object SFB4;
}
class C
{
    A a = new B();
}";
            // Derived class in assembly with PDB,
            // base class in assembly without PDB.
            var compilationA = CSharpTestBase.CreateCompilationWithMscorlib(sourceA, options: TestOptions.ReleaseDll);
            var bytesA = compilationA.EmitToArray();
            var referenceA = MetadataReference.CreateFromImage(bytesA);

            var compilationB = CSharpTestBase.CreateCompilationWithMscorlib(sourceB, options: TestOptions.DebugDll, references: new MetadataReference[] { referenceA });
            var bytesB = compilationB.EmitToArray();
            var assemblyA = ReflectionUtilities.Load(bytesA);
            var assemblyB = ReflectionUtilities.Load(bytesB);
            DkmClrValue value;

            using (ReflectionUtilities.LoadAssemblies(assemblyA, assemblyB))
            {
                var runtime = new DkmClrRuntimeInstance(new[] { assemblyB });
                var type = assemblyB.GetType("C", throwOnError: true);
                value = CreateDkmClrValue(
                    Activator.CreateInstance(type),
                    runtime.GetType((TypeImpl)type));
            }

            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.HideNonPublicMembers));
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));

            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "{B}", "A {B}", "(new C()).a", DkmEvaluationResultFlags.Expandable));

            // The native EE includes properties where the setter is accessible but the getter is not.
            // We treat those properties as non-public.
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("FA0", "null", "object", "(new C()).a.FA0"),
                EvalResult("FA2", "null", "object", "(new C()).a.FA2"),
                EvalResult("FA3", "null", "object", "(new C()).a.FA3"),
                EvalResult("FB0", "null", "object", "((B)(new C()).a).FB0"),
                EvalResult("FB1", "null", "object", "((B)(new C()).a).FB1"),
                EvalResult("FB2", "null", "object", "((B)(new C()).a).FB2"),
                EvalResult("FB3", "null", "object", "((B)(new C()).a).FB3"),
                EvalResult("FB4", "null", "object", "((B)(new C()).a).FB4"),
                EvalResult("PA0", "null", "object", "(new C()).a.PA0", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA2", "null", "object", "(new C()).a.PA2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA3", "null", "object", "(new C()).a.PA3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA7", "null", "object", "(new C()).a.PA7"),
                EvalResult("PA8", "null", "object", "(new C()).a.PA8"),
                EvalResult("PAC", "null", "object", "(new C()).a.PAC"),
                EvalResult("PB0", "null", "object", "((B)(new C()).a).PB0", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB1", "null", "object", "((B)(new C()).a).PB1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB2", "null", "object", "((B)(new C()).a).PB2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB3", "null", "object", "((B)(new C()).a).PB3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB4", "null", "object", "((B)(new C()).a).PB4", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB6", "null", "object", "((B)(new C()).a).PB6"),
                EvalResult("PB7", "null", "object", "((B)(new C()).a).PB7"),
                EvalResult("PB8", "null", "object", "((B)(new C()).a).PB8"),
                EvalResult("PB9", "null", "object", "((B)(new C()).a).PB9"),
                EvalResult("PBA", "null", "object", "((B)(new C()).a).PBA"),
                EvalResult("PBB", "null", "object", "((B)(new C()).a).PBB"),
                EvalResult("PBC", "null", "object", "((B)(new C()).a).PBC"),
                EvalResult("PBD", "null", "object", "((B)(new C()).a).PBD"),
                EvalResult("Static members", null, "", "B", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult("Non-Public members", null, "", "(new C()).a, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            // Static members
            var more = GetChildren(children[children.Length - 2]);
            Verify(more,
                EvalResult("SFA0", "null", "object", "A.SFA0"),
                EvalResult("SFB2", "null", "object", "B.SFB2"),
                EvalResult("SFB3", "null", "object", "B.SFB3"),
                EvalResult("SFB4", "null", "object", "B.SFB4"),
                EvalResult("SPA2", "null", "object", "A.SPA2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("SPA3", "null", "object", "A.SPA3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("SPB0", "null", "object", "B.SPB0", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("SPB1", "null", "object", "B.SPB1"),
                EvalResult("Non-Public members", null, "", "B, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            // Non-Public static members
            more = GetChildren(more[more.Length - 1]);
            Verify(more,
                EvalResult("SFA1", "null", "object", "A.SFA1"),
                EvalResult("SPA4", "null", "object", "A.SPA4"));

            // Non-Public members
            more = GetChildren(children[children.Length - 1]);
            Verify(more,
                EvalResult("FA1", "null", "object", "(new C()).a.FA1"),
                EvalResult("FA4", "null", "object", "(new C()).a.FA4"),
                EvalResult("PA1", "null", "object", "(new C()).a.PA1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA4", "null", "object", "(new C()).a.PA4", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA6", "null", "object", "(new C()).a.PA6"),
                EvalResult("PA9", "null", "object", "(new C()).a.PA9"),
                EvalResult("PAA", "null", "object", "(new C()).a.PAA"),
                EvalResult("PAB", "null", "object", "(new C()).a.PAB"),
                EvalResult("PAD", "null", "object", "(new C()).a.PAD"));
        }

        [WorkItem(889710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889710")]
        [Fact]
        public void HideNonPublicMembersDerivedClass()
        {
            var sourceA =
@"public class A
{
    public object FA0;
    internal object FA1;
    protected internal object FA2;
    protected object FA3;
    private object FA4;
    public object PA0 { get { return null; } }
    internal object PA1 { get { return null; } }
    protected internal object PA2 { get { return null; } }
    protected object PA3 { get { return null; } }
    private object PA4 { get { return null; } }
    public object PA5 { set { } }
    public object PA6 { internal get; set; }
    public object PA7 { protected internal get; set; }
    public object PA8 { protected get; set; }
    public object PA9 { private get; set; }
    internal object PAA { private get; set; }
    protected internal object PAB { internal get; set; }
    protected internal object PAC { protected get; set; }
    protected object PAD { private get; set; }
    public static object SFA0;
    internal static object SFA1;
    protected static internal object SPA2 { get { return null; } }
    protected static object SPA3 { get { return null; } }
    public static object SPA4 { private get { return null; } set { } }
}";
            var sourceB =
@"public class B : A
{
    public object FB0;
    internal object FB1;
    protected internal object FB2;
    protected object FB3;
    private object FB4;
    public object PB0 { get { return null; } }
    internal object PB1 { get { return null; } }
    protected internal object PB2 { get { return null; } }
    protected object PB3 { get { return null; } }
    private object PB4 { get { return null; } }
    public object PB5 { set { } }
    public object PB6 { internal get; set; }
    public object PB7 { protected internal get; set; }
    public object PB8 { protected get; set; }
    public object PB9 { private get; set; }
    internal object PBA { private get; set; }
    protected internal object PBB { internal get; set; }
    protected internal object PBC { protected get; set; }
    protected object PBD { private get; set; }
    public static object SPB0 { get { return null; } }
    public static object SPB1 { internal get { return null; } set { } }
    protected static internal object SFB2;
    protected static object SFB3;
    private static object SFB4;
}
class C
{
    A a = new B();
}";
            // Base class in assembly with PDB,
            // derived class in assembly without PDB.
            var compilationA = CSharpTestBase.CreateCompilationWithMscorlib(sourceA, options: TestOptions.DebugDll);
            var bytesA = compilationA.EmitToArray();
            var referenceA = MetadataReference.CreateFromImage(bytesA);

            var compilationB = CSharpTestBase.CreateCompilationWithMscorlib(sourceB, options: TestOptions.ReleaseDll, references: new MetadataReference[] { referenceA });
            var bytesB = compilationB.EmitToArray();
            var assemblyA = ReflectionUtilities.Load(bytesA);
            var assemblyB = ReflectionUtilities.Load(bytesB);
            DkmClrValue value;

            using (ReflectionUtilities.LoadAssemblies(assemblyA, assemblyB))
            {
                var runtime = new DkmClrRuntimeInstance(new[] { assemblyA });
                var type = assemblyB.GetType("C", throwOnError: true);
                value = CreateDkmClrValue(
                    Activator.CreateInstance(type),
                    runtime.GetType((TypeImpl)type));
            }

            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.HideNonPublicMembers));
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));

            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Non-Public members", null, "", "new C(), hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("a", "{B}", "A {B}", "(new C()).a", DkmEvaluationResultFlags.Expandable));

            // The native EE includes properties where the
            // setter is accessible but the getter is not.
            // We treat those properties as non-public.
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("FA0", "null", "object", "(new C()).a.FA0"),
                EvalResult("FA1", "null", "object", "(new C()).a.FA1"),
                EvalResult("FA2", "null", "object", "(new C()).a.FA2"),
                EvalResult("FA3", "null", "object", "(new C()).a.FA3"),
                EvalResult("FA4", "null", "object", "(new C()).a.FA4"),
                EvalResult("FB0", "null", "object", "((B)(new C()).a).FB0"),
                EvalResult("FB2", "null", "object", "((B)(new C()).a).FB2"),
                EvalResult("FB3", "null", "object", "((B)(new C()).a).FB3"),
                EvalResult("PA0", "null", "object", "(new C()).a.PA0", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA1", "null", "object", "(new C()).a.PA1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA2", "null", "object", "(new C()).a.PA2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA3", "null", "object", "(new C()).a.PA3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA4", "null", "object", "(new C()).a.PA4", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PA6", "null", "object", "(new C()).a.PA6"),
                EvalResult("PA7", "null", "object", "(new C()).a.PA7"),
                EvalResult("PA8", "null", "object", "(new C()).a.PA8"),
                EvalResult("PA9", "null", "object", "(new C()).a.PA9"),
                EvalResult("PAA", "null", "object", "(new C()).a.PAA"),
                EvalResult("PAB", "null", "object", "(new C()).a.PAB"),
                EvalResult("PAC", "null", "object", "(new C()).a.PAC"),
                EvalResult("PAD", "null", "object", "(new C()).a.PAD"),
                EvalResult("PB0", "null", "object", "((B)(new C()).a).PB0", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB2", "null", "object", "((B)(new C()).a).PB2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB3", "null", "object", "((B)(new C()).a).PB3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB7", "null", "object", "((B)(new C()).a).PB7"),
                EvalResult("PB8", "null", "object", "((B)(new C()).a).PB8"),
                EvalResult("PBC", "null", "object", "((B)(new C()).a).PBC"),
                EvalResult("Static members", null, "", "B", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult("Non-Public members", null, "", "(new C()).a, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            // Static members
            var more = GetChildren(children[children.Length - 2]);
            Verify(more,
                EvalResult("SFA0", "null", "object", "A.SFA0"),
                EvalResult("SFA1", "null", "object", "A.SFA1"),
                EvalResult("SFB2", "null", "object", "B.SFB2"),
                EvalResult("SFB3", "null", "object", "B.SFB3"),
                EvalResult("SPA2", "null", "object", "A.SPA2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("SPA3", "null", "object", "A.SPA3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("SPA4", "null", "object", "A.SPA4"),
                EvalResult("SPB0", "null", "object", "B.SPB0", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Non-Public members", null, "", "B, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            // Non-Public static members
            more = GetChildren(more[more.Length - 1]);
            Verify(more,
                EvalResult("SFB4", "null", "object", "B.SFB4"),
                EvalResult("SPB1", "null", "object", "B.SPB1"));

            // Non-Public members
            more = GetChildren(children[children.Length - 1]);
            Verify(more,
                EvalResult("FB1", "null", "object", "((B)(new C()).a).FB1"),
                EvalResult("FB4", "null", "object", "((B)(new C()).a).FB4"),
                EvalResult("PB1", "null", "object", "((B)(new C()).a).PB1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB4", "null", "object", "((B)(new C()).a).PB4", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PB6", "null", "object", "((B)(new C()).a).PB6"),
                EvalResult("PB9", "null", "object", "((B)(new C()).a).PB9"),
                EvalResult("PBA", "null", "object", "((B)(new C()).a).PBA"),
                EvalResult("PBB", "null", "object", "((B)(new C()).a).PBB"),
                EvalResult("PBD", "null", "object", "((B)(new C()).a).PBD"));
        }

        /// <summary>
        /// Class in assembly with no module. (For instance,
        /// an anonymous type created during debugging.)
        /// </summary>
        [Fact]
        public void NoModule()
        {
            var source =
@"class C
{
    object F;
}";
            var assembly = GetAssembly(source);
            var runtime = new DkmClrRuntimeInstance(new[] { assembly }, (r, a) => null);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                Activator.CreateInstance(type),
                runtime.GetType((TypeImpl)type));
            var evalResult = FormatResult("o", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.HideNonPublicMembers));
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
        }
    }
}
