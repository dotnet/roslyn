// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    public abstract class ExpressionCompiler :
        IDkmClrExpressionCompiler,
        IDkmClrExpressionCompilerCallback,
        IDkmModuleModifiedNotification
    {
        static ExpressionCompiler()
        {
            FatalError.Handler = FailFast.OnFatalException;
        }

        DkmCompiledClrLocalsQuery IDkmClrExpressionCompiler.GetClrLocalVariableQuery(
            DkmInspectionContext inspectionContext,
            DkmClrInstructionAddress instructionAddress,
            bool argumentsOnly)
        {
            try
            {
                var references = instructionAddress.RuntimeInstance.GetMetadataBlocks(instructionAddress.ModuleInstance.AppDomain);
                var context = this.CreateMethodContext(instructionAddress, references);
                var builder = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(
                    builder,
                    argumentsOnly,
                    out typeName,
                    testData: null);
                Debug.Assert((builder.Count == 0) == (assembly.Count == 0));
                var locals = new ReadOnlyCollection<DkmClrLocalVariableInfo>(builder.SelectAsArray(l => DkmClrLocalVariableInfo.Create(l.LocalName, l.MethodName, l.Flags, DkmEvaluationResultCategory.Data)));
                builder.Free();
                return DkmCompiledClrLocalsQuery.Create(inspectionContext.RuntimeInstance, null, this.CompilerId, assembly, typeName, locals);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        void IDkmClrExpressionCompiler.CompileExpression(
            DkmLanguageExpression expression,
            DkmClrInstructionAddress instructionAddress,
            DkmInspectionContext inspectionContext,
            out string error,
            out DkmCompiledClrInspectionQuery result)
        {
            try
            {
                var appDomain = instructionAddress.ModuleInstance.AppDomain;
                var references = instructionAddress.RuntimeInstance.GetMetadataBlocks(appDomain);

                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                ResultProperties resultProperties;
                CompileResult compileResult;
                do
                {
                    var context = this.CreateMethodContext(instructionAddress, references);
                    compileResult = context.CompileExpression(
                        RuntimeInspectionContext.Create(inspectionContext),
                        expression.Text,
                        expression.CompilationFlags,
                        this.DiagnosticFormatter,
                        out resultProperties,
                        out error,
                        out missingAssemblyIdentities,
                        preferredUICulture: null,
                        testData: null);
                } while (ShouldTryAgainWithMoreMetadataBlocks(appDomain, missingAssemblyIdentities, ref references));

                result = compileResult.ToQueryResult(this.CompilerId, resultProperties, instructionAddress.RuntimeInstance);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        void IDkmClrExpressionCompiler.CompileAssignment(
            DkmLanguageExpression expression,
            DkmClrInstructionAddress instructionAddress,
            DkmEvaluationResult lValue,
            out string error,
            out DkmCompiledClrInspectionQuery result)
        {
            try
            {
                var appDomain = instructionAddress.ModuleInstance.AppDomain;
                var references = instructionAddress.RuntimeInstance.GetMetadataBlocks(appDomain);

                ResultProperties resultProperties;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                CompileResult compileResult;
                do
                {
                    var context = this.CreateMethodContext(instructionAddress, references);
                    compileResult = context.CompileAssignment(
                        RuntimeInspectionContext.Create(lValue.InspectionContext),
                        lValue.FullName,
                        expression.Text,
                        this.DiagnosticFormatter,
                        out resultProperties,
                        out error,
                        out missingAssemblyIdentities,
                        preferredUICulture: null,
                        testData: null);
                } while (ShouldTryAgainWithMoreMetadataBlocks(appDomain, missingAssemblyIdentities, ref references));

                Debug.Assert((resultProperties.Flags & DkmClrCompilationResultFlags.PotentialSideEffect) == DkmClrCompilationResultFlags.PotentialSideEffect);
                result = compileResult.ToQueryResult(this.CompilerId, resultProperties, instructionAddress.RuntimeInstance);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        void IDkmClrExpressionCompilerCallback.CompileDisplayAttribute(
            DkmLanguageExpression expression,
            DkmClrModuleInstance moduleInstance,
            int token,
            out string error,
            out DkmCompiledClrInspectionQuery result)
        {
            try
            {
                var appDomain = moduleInstance.AppDomain;
                var references = moduleInstance.RuntimeInstance.GetMetadataBlocks(appDomain);

                ResultProperties unusedResultProperties;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                CompileResult compileResult;
                do
                {
                    var context = this.CreateTypeContext(moduleInstance, references, token);
                    compileResult = context.CompileExpression(
                        RuntimeInspectionContext.Empty,
                        expression.Text,
                        DkmEvaluationFlags.TreatAsExpression,
                        this.DiagnosticFormatter,
                        out unusedResultProperties,
                        out error,
                        out missingAssemblyIdentities,
                        preferredUICulture: null,
                        testData: null);
                } while (ShouldTryAgainWithMoreMetadataBlocks(appDomain, missingAssemblyIdentities, ref references));

                result = compileResult.ToQueryResult(this.CompilerId, default(ResultProperties), moduleInstance.RuntimeInstance);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool ShouldTryAgainWithMoreMetadataBlocks(DkmClrAppDomain appDomain, ImmutableArray<AssemblyIdentity> missingAssemblyIdentities, ref ImmutableArray<MetadataBlock> references)
        {
            return ShouldTryAgainWithMoreMetadataBlocks(
                (AssemblyIdentity assemblyIdentity, out uint size) => appDomain.GetMetaDataBytesPtr(assemblyIdentity.GetDisplayName(), out size),
                missingAssemblyIdentities,
                ref references);
        }


        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal static bool ShouldTryAgainWithMoreMetadataBlocks(DkmUtilities.GetMetadataBytesPtrFunction getMetaDataBytesPtrFunction, ImmutableArray<AssemblyIdentity> missingAssemblyIdentities, ref ImmutableArray<MetadataBlock> references)
        {
            var newReferences = DkmUtilities.GetMetadataBlocks(getMetaDataBytesPtrFunction, missingAssemblyIdentities);
            if (newReferences.Length > 0)
            {
                references = references.AddRange(newReferences);
                return true;
            }

            return false;
        }

        void IDkmModuleModifiedNotification.OnModuleModified(DkmModuleInstance moduleInstance)
        {
            // If the module is not a managed module, the module change has no effect.
            var module = moduleInstance as DkmClrModuleInstance;
            if (module == null)
            {
                return;
            }
            // Drop any context cached on the AppDomain.
            var appDomain = module.AppDomain;
            RemoveDataItem(appDomain);
        }

        internal abstract DiagnosticFormatter DiagnosticFormatter { get; }

        internal abstract DkmCompilerId CompilerId { get; }

        internal abstract EvaluationContextBase CreateTypeContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken);

        internal abstract EvaluationContextBase CreateMethodContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Lazy<ImmutableArray<AssemblyReaders>> lazyAssemblyReaders,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            int ilOffset,
            int localSignatureToken);

        internal abstract bool RemoveDataItem(DkmClrAppDomain appDomain);

        private EvaluationContextBase CreateTypeContext(DkmClrModuleInstance moduleInstance, ImmutableArray<MetadataBlock> references, int typeToken)
        {
            return this.CreateTypeContext(
                moduleInstance.AppDomain,
                references,
                moduleInstance.Mvid,
                typeToken);
        }

        private EvaluationContextBase CreateMethodContext(DkmClrInstructionAddress instructionAddress, ImmutableArray<MetadataBlock> metadataBlocks)
        {
            var moduleInstance = instructionAddress.ModuleInstance;
            var methodToken = instructionAddress.MethodId.Token;
            int localSignatureToken;
            try
            {
                localSignatureToken = moduleInstance.GetLocalSignatureToken(methodToken);
            }
            catch (InvalidOperationException)
            {
                // No local signature. May occur when debugging .dmp.
                localSignatureToken = 0;
            }
            catch (FileNotFoundException)
            {
                // No local signature. May occur when debugging heapless dumps.
                localSignatureToken = 0;
            }
            return this.CreateMethodContext(
                moduleInstance.AppDomain,
                metadataBlocks,
                new Lazy<ImmutableArray<AssemblyReaders>>(() => instructionAddress.MakeAssemblyReaders(), LazyThreadSafetyMode.None),
                symReader: moduleInstance.GetSymReader(),
                moduleVersionId: moduleInstance.Mvid,
                methodToken: methodToken,
                methodVersion: (int)instructionAddress.MethodId.Version,
                ilOffset: (int)instructionAddress.ILOffset,
                localSignatureToken: localSignatureToken);
        }
    }
}
