// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class EvaluationContextBase
    {
        internal static readonly AssemblyIdentity SystemIdentity = new AssemblyIdentity("System");
        internal static readonly AssemblyIdentity SystemCoreIdentity = new AssemblyIdentity("System.Core");
        internal static readonly AssemblyIdentity SystemLinqIdentity = new AssemblyIdentity("System.Linq");
        internal static readonly AssemblyIdentity SystemXmlIdentity = new AssemblyIdentity("System.Xml");
        internal static readonly AssemblyIdentity SystemXmlLinqIdentity = new AssemblyIdentity("System.Xml.Linq");
        internal static readonly AssemblyIdentity MicrosoftVisualBasicIdentity = new AssemblyIdentity("Microsoft.VisualBasic");

        internal abstract CompileResult CompileExpression(
            string expr,
            DkmEvaluationFlags compilationFlags,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            CompilationTestData testData);

        internal abstract CompileResult CompileAssignment(
            string target,
            string expr,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            CompilationTestData testData);

        internal abstract ReadOnlyCollection<byte> CompileGetLocals(
            ArrayBuilder<LocalAndMethod> locals,
            bool argumentsOnly,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out string typeName,
            CompilationTestData testData);

        internal string GetErrorMessageAndMissingAssemblyIdentities(DiagnosticBag diagnostics, DiagnosticFormatter formatter, CultureInfo preferredUICulture, AssemblyIdentity linqLibrary, out bool useReferencedModulesOnly, out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities)
        {
            var errors = diagnostics.AsEnumerable().Where(d => d.Severity == DiagnosticSeverity.Error);
            foreach (var error in errors)
            {
                missingAssemblyIdentities = this.GetMissingAssemblyIdentities(error, linqLibrary);
                if (!missingAssemblyIdentities.IsDefault)
                {
                    break;
                }
            }

            if (missingAssemblyIdentities.IsDefault)
            {
                missingAssemblyIdentities = ImmutableArray<AssemblyIdentity>.Empty;
            }

            useReferencedModulesOnly = errors.All(HasDuplicateTypesOrAssemblies);

            var firstError = errors.FirstOrDefault();
            Debug.Assert(firstError != null);

            var simpleMessage = firstError as SimpleMessageDiagnostic;
            return (simpleMessage != null) ?
                simpleMessage.GetMessage() :
                formatter.Format(firstError, preferredUICulture ?? CultureInfo.CurrentUICulture);
        }

        internal abstract bool HasDuplicateTypesOrAssemblies(Diagnostic diagnostic);

        internal abstract ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentities(Diagnostic diagnostic, AssemblyIdentity linqLibrary);

        // ILOffset == 0xffffffff indicates an instruction outside of IL.
        // Treat such values as the beginning of the IL.
        protected static int NormalizeILOffset(uint ilOffset)
        {
            return (ilOffset == uint.MaxValue) ? 0 : (int)ilOffset;
        }

        protected sealed class SimpleMessageDiagnostic : Diagnostic
        {
            private readonly string _message;

            internal SimpleMessageDiagnostic(string message)
            {
                _message = message;
            }

            public override IReadOnlyList<Location> AdditionalLocations
            {
                get { throw new NotImplementedException(); }
            }

            public override DiagnosticDescriptor Descriptor
            {
                get { throw new NotImplementedException(); }
            }

            public override string Id
            {
                get { throw new NotImplementedException(); }
            }

            public override Location Location
            {
                get { throw new NotImplementedException(); }
            }

            public override DiagnosticSeverity Severity
            {
                get { return DiagnosticSeverity.Error; }
            }

            public override bool IsSuppressed
            {
                get { return false; }
            }

            public override int WarningLevel
            {
                get { throw new NotImplementedException(); }
            }

            public override bool Equals(Diagnostic obj)
            {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }

            public override string GetMessage(IFormatProvider formatProvider = null)
            {
                return _message;
            }

            internal override Diagnostic WithLocation(Location location)
            {
                throw new NotImplementedException();
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                throw new NotImplementedException();
            }

            internal override Diagnostic WithIsSuppressed(bool isSuppressed)
            {
                throw new NotImplementedException();
            }
        }
    }
}
