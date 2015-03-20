// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class EvaluationContextBase
    {
        internal static readonly AssemblyIdentity SystemIdentity = new AssemblyIdentity("System");
        internal static readonly AssemblyIdentity SystemCoreIdentity = new AssemblyIdentity("System.Core");
        internal static readonly AssemblyIdentity SystemXmlIdentity = new AssemblyIdentity("System.Xml");
        internal static readonly AssemblyIdentity SystemXmlLinqIdentity = new AssemblyIdentity("System.Xml.Linq");
        internal static readonly AssemblyIdentity MicrosoftVisualBasicIdentity = new AssemblyIdentity("Microsoft.VisualBasic");

        /// <summary>
        /// Compile C# expression and emit assembly with evaluation method.
        /// </summary>
        /// <returns>
        /// Result containing generated assembly, type and method names, and any format specifiers.
        /// </returns>
        internal abstract CompileResult CompileExpression(
            InspectionContext inspectionContext,
            string expr,
            DkmEvaluationFlags compilationFlags,
            DiagnosticFormatter formatter,
            out ResultProperties resultProperties,
            out string error,
            out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities,
            CultureInfo preferredUICulture,
            CompilationTestData testData);

        internal abstract CompileResult CompileAssignment(
            InspectionContext inspectionContext,
            string target,
            string expr,
            DiagnosticFormatter formatter,
            out ResultProperties resultProperties,
            out string error,
            out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities,
            CultureInfo preferredUICulture,
            CompilationTestData testData);

        internal abstract ReadOnlyCollection<byte> CompileGetLocals(
            ArrayBuilder<LocalAndMethod> locals,
            bool argumentsOnly,
            out string typeName,
            CompilationTestData testData);

        internal static ConstantValue ReinterpretConstantValue(ConstantValue raw, SpecialType specialType)
        {
            return specialType == SpecialType.System_DateTime
                ? ConstantValue.Create(DateTimeUtilities.ToDateTime((double)raw.Value))
                : raw;
        }

        internal string GetErrorMessageAndMissingAssemblyIdentities(DiagnosticBag diagnostics, DiagnosticFormatter formatter, CultureInfo preferredUICulture, out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities)
        {
            Diagnostic firstError = GetErrorAndMissingAssemblyIdentities(diagnostics, out missingAssemblyIdentities);
            Debug.Assert(firstError != null);
            return formatter.Format(firstError, preferredUICulture ?? CultureInfo.CurrentUICulture);
        }

        internal Diagnostic GetErrorAndMissingAssemblyIdentities(DiagnosticBag diagnostics, out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities)
        {
            var diagnosticsEnumerable = diagnostics.AsEnumerable();
            foreach (Diagnostic diagnostic in diagnosticsEnumerable)
            {
                missingAssemblyIdentities = GetMissingAssemblyIdentities(diagnostic);
                if (!missingAssemblyIdentities.IsDefault)
                {
                    break;
                }
            }

            if (missingAssemblyIdentities.IsDefault)
            {
                missingAssemblyIdentities = ImmutableArray<AssemblyIdentity>.Empty;
            }

            return diagnosticsEnumerable.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        }

        internal abstract ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentities(Diagnostic diagnostic);
    }
}
