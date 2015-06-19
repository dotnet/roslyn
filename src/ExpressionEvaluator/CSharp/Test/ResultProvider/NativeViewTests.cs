// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NativeViewTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void NativeView()
        {
            TestNativeView(true);
        }

        [Fact]
        public void NativeViewManagedOnly()
        {
            TestNativeView(false);
        }

        private void TestNativeView(bool enableNativeDebugging)
        {
            var source =
@"class C
{
}";
            using (new EnsureEnglishUICulture())
            {
                var assembly = GetAssembly(source);
                var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
                using (ReflectionUtilities.LoadAssemblies(assemblies))
                {
                    var runtime = new DkmClrRuntimeInstance(assemblies, enableNativeDebugging: enableNativeDebugging);
                    var inspectionContext = CreateDkmInspectionContext(runtimeInstance: runtime);
                    var type = assembly.GetType("C");
                    var value = CreateDkmClrValue(
                        value: type.Instantiate(),
                        type: runtime.GetType((TypeImpl)type),
                        nativeComPointer: 0xfe);
                    var evalResult = FormatResult("o", value, inspectionContext: inspectionContext);
                    Verify(evalResult,
                        EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                    var children = GetChildren(evalResult, inspectionContext);
                    if (enableNativeDebugging)
                    {
                        DkmLanguage language = new DkmLanguage(new DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.Cpp));
                        Verify(children,
                            EvalIntermediateResult("Native View", "{C++}(IUnknown*)0x000000FE", "(IUnknown*)0x000000FE", language));
                    }
                    else
                    {
                        Verify(children,
                            EvalFailedResult("Native View", "To inspect the native object, enable native code debugging."));
                    }
                }
            }
        }
    }
}
