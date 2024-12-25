// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DebuggerVisualizerAttributeTests : CSharpResultProviderTestBase
    {
        /// <summary>
        /// Tests that the DebuggerVisualizer attribute works with multiple attributes defined.
        /// </summary>
        [Fact]
        public void Visualizer()
        {
            var source =
@"using System.Diagnostics;
[DebuggerVisualizer(typeof(P))]
[DebuggerVisualizer(typeof(Q), Description = ""Q Visualizer"")]
class C
{
    object F = 1;
    object P { get { return 3; } }
}
class P
{
    public P() { }
}
class Q
{
    public Q() { }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: new DkmClrType((TypeImpl)type),
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("new C()", value);

            var typeP = assembly.GetType("P");
            var typeQ = assembly.GetType("Q");

            string defaultDebuggeeSideVisualizerTypeName = "Microsoft.VisualStudio.DebuggerVisualizers.VisualizerObjectSource";
            var vsVersion = Environment.GetEnvironmentVariable("VisualStudioVersion") ?? "14.0";
            string defaultDebuggeeSideVisualizerAssemblyName = $"Microsoft.VisualStudio.DebuggerVisualizers, Version={vsVersion}.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

            DkmCustomUIVisualizerInfo[] customUIVisualizerInfo =
            [
                new DkmCustomUIVisualizerInfo
                {
                    Id = 0,
                    Description = "P",
                    MenuName = "P",
                    Metric = "ClrCustomVisualizerVSHost",
                    UISideVisualizerTypeName = typeP.FullName,
                    UISideVisualizerAssemblyName = typeP.Assembly.FullName,
                    UISideVisualizerAssemblyLocation = DkmClrCustomVisualizerAssemblyLocation.Unknown,
                    DebuggeeSideVisualizerTypeName = defaultDebuggeeSideVisualizerTypeName,
                    DebuggeeSideVisualizerAssemblyName = defaultDebuggeeSideVisualizerAssemblyName,
                    ExtensionPartId = Guid.Empty
                },
                new DkmCustomUIVisualizerInfo
                {
                    Id = 1,
                    Description = "Q Visualizer",
                    MenuName = "Q Visualizer",
                    Metric = "ClrCustomVisualizerVSHost",
                    UISideVisualizerTypeName = typeQ.FullName,
                    UISideVisualizerAssemblyName = typeQ.Assembly.FullName,
                    UISideVisualizerAssemblyLocation = DkmClrCustomVisualizerAssemblyLocation.Unknown,
                    DebuggeeSideVisualizerTypeName = defaultDebuggeeSideVisualizerTypeName,
                    DebuggeeSideVisualizerAssemblyName = defaultDebuggeeSideVisualizerAssemblyName,
                    ExtensionPartId = Guid.Empty
                }
            ];

            Verify(evalResult,
                EvalResult("new C()", "{C}", "C", "new C()", flags: DkmEvaluationResultFlags.Expandable, customUIVisualizerInfo: customUIVisualizerInfo));
        }
    }
}
