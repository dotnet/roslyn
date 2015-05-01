// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class TestExtensions
    {
        internal static CompileResult CompileExpression(
            this EvaluationContextBase context,
            string expr,
            out string error,
            CompilationTestData testData = null,
            DiagnosticFormatter formatter = null)
        {
            ResultProperties resultProperties;
            return CompileExpression(context, expr, out resultProperties, out error, testData, formatter);
        }

        internal static CompileResult CompileExpression(
            this EvaluationContextBase context,
            string expr,
            out ResultProperties resultProperties,
            out string error,
            CompilationTestData testData = null,
            DiagnosticFormatter formatter = null)
        {
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = context.CompileExpression(
                expr,
                DkmEvaluationFlags.TreatAsExpression,
                formatter ?? DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            return result;
        }

        internal static CompileResult CompileAssignment(
            this EvaluationContextBase context,
            string target,
            string expr,
            out string error,
            CompilationTestData testData = null,
            DiagnosticFormatter formatter = null)
        {
            ResultProperties resultProperties;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = context.CompileAssignment(
                target,
                expr,
                formatter ?? DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            // This is a crude way to test the language, but it's convenient to share this test helper.
            var isCSharp = context.GetType().Namespace.IndexOf("csharp", StringComparison.OrdinalIgnoreCase) >= 0;
            var expectedFlags = error != null
                ? DkmClrCompilationResultFlags.None
                : isCSharp
                    ? DkmClrCompilationResultFlags.PotentialSideEffect
                    : DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult;
            Assert.Equal(expectedFlags, resultProperties.Flags);
            Assert.Equal(default(DkmEvaluationResultCategory), resultProperties.Category);
            Assert.Equal(default(DkmEvaluationResultAccessType), resultProperties.AccessType);
            Assert.Equal(default(DkmEvaluationResultStorageType), resultProperties.StorageType);
            Assert.Equal(default(DkmEvaluationResultTypeModifierFlags), resultProperties.ModifierFlags);
            return result;
        }
    }
}
