// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
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

        private static readonly ReadOnlyCollection<Alias> s_NoAliases = new ReadOnlyCollection<Alias>(new Alias[0]);

        DkmCompiledClrLocalsQuery IDkmClrExpressionCompiler.GetClrLocalVariableQuery(
            DkmInspectionContext inspectionContext,
            DkmClrInstructionAddress instructionAddress,
            bool argumentsOnly)
        {
            try
            {
                var moduleInstance = instructionAddress.ModuleInstance;
                var runtimeInstance = instructionAddress.RuntimeInstance;
                var runtimeInspectionContext = RuntimeInspectionContext.Create(inspectionContext);
                var aliases = argumentsOnly ? null : s_NoAliases;
                string error;
                var r = this.CompileWithRetry(
                    moduleInstance,
                    runtimeInstance.GetMetadataBlocks(moduleInstance.AppDomain),
                    (blocks, useReferencedModulesOnly) => CreateMethodContext(instructionAddress, blocks, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        var builder = ArrayBuilder<LocalAndMethod>.GetInstance();
                        string typeName;
                        var assembly = context.CompileGetLocals(
                            aliases,
                            builder,
                            argumentsOnly,
                            diagnostics,
                            out typeName,
                            testData: null);
                        Debug.Assert((builder.Count == 0) == (assembly.Count == 0));
                        var locals = new ReadOnlyCollection<DkmClrLocalVariableInfo>(builder.SelectAsArray(ToLocalVariableInfo));
                        builder.Free();
                        return new GetLocalsResult(typeName, locals, assembly);
                    },
                    out error);
                return DkmCompiledClrLocalsQuery.Create(runtimeInstance, null, this.CompilerId, r.Assembly, r.TypeName, r.Locals);
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
                var moduleInstance = instructionAddress.ModuleInstance;
                var runtimeInstance = instructionAddress.RuntimeInstance;
                var runtimeInspectionContext = RuntimeInspectionContext.Create(inspectionContext);
                var r = this.CompileWithRetry(
                    moduleInstance,
                    runtimeInstance.GetMetadataBlocks(moduleInstance.AppDomain),
                    (blocks, useReferencedModulesOnly) => CreateMethodContext(instructionAddress, blocks, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        ResultProperties resultProperties;
                        var compileResult = context.CompileExpression(
                            runtimeInspectionContext,
                            expression.Text,
                            expression.CompilationFlags,
                            diagnostics,
                            out resultProperties,
                            testData: null);
                        return new CompileExpressionResult(compileResult, resultProperties);
                    },
                    out error);
                result = r.CompileResult.ToQueryResult(this.CompilerId, r.ResultProperties, runtimeInstance);
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
                var moduleInstance = instructionAddress.ModuleInstance;
                var runtimeInstance = instructionAddress.RuntimeInstance;
                var runtimeInspectionContext = RuntimeInspectionContext.Create(lValue.InspectionContext);
                var r = this.CompileWithRetry(
                    moduleInstance,
                    runtimeInstance.GetMetadataBlocks(moduleInstance.AppDomain),
                    (blocks, useReferencedModulesOnly) => CreateMethodContext(instructionAddress, blocks, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        ResultProperties resultProperties;
                        var compileResult = context.CompileAssignment(
                            runtimeInspectionContext,
                            lValue.FullName,
                            expression.Text,
                            diagnostics,
                            out resultProperties,
                            testData: null);
                        return new CompileExpressionResult(compileResult, resultProperties);
                    },
                    out error);

                Debug.Assert((r.ResultProperties.Flags & DkmClrCompilationResultFlags.PotentialSideEffect) == DkmClrCompilationResultFlags.PotentialSideEffect);
                result = r.CompileResult.ToQueryResult(this.CompilerId, r.ResultProperties, runtimeInstance);
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
                var runtimeInstance = moduleInstance.RuntimeInstance;
                var appDomain = moduleInstance.AppDomain;
                var compileResult = this.CompileWithRetry(
                    moduleInstance,
                    runtimeInstance.GetMetadataBlocks(appDomain),
                    (blocks, useReferencedModulesOnly) => CreateTypeContext(appDomain, blocks, moduleInstance.Mvid, token, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        ResultProperties unusedResultProperties;
                        return context.CompileExpression(
                            RuntimeInspectionContext.Empty,
                            expression.Text,
                            DkmEvaluationFlags.TreatAsExpression,
                            diagnostics,
                            out unusedResultProperties,
                            testData: null);
                    },
                    out error);
                result = compileResult.ToQueryResult(this.CompilerId, default(ResultProperties), runtimeInstance);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
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
            int typeToken,
            bool useReferencedModulesOnly);

        internal abstract EvaluationContextBase CreateMethodContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Lazy<ImmutableArray<AssemblyReaders>> lazyAssemblyReaders,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            int ilOffset,
            int localSignatureToken,
            bool useReferencedModulesOnly);

        internal abstract void RemoveDataItem(DkmClrAppDomain appDomain);

        private EvaluationContextBase CreateMethodContext(DkmClrInstructionAddress instructionAddress, ImmutableArray<MetadataBlock> metadataBlocks, bool useReferencedModulesOnly)
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
                localSignatureToken: localSignatureToken,
                useReferencedModulesOnly: useReferencedModulesOnly);
        }

        internal delegate EvaluationContextBase CreateContextDelegate(ImmutableArray<MetadataBlock> metadataBlocks, bool useReferencedModulesOnly);
        internal delegate TResult CompileDelegate<TResult>(EvaluationContextBase context, DiagnosticBag diagnostics);

        private TResult CompileWithRetry<TResult>(
            DkmClrModuleInstance moduleInstance,
            ImmutableArray<MetadataBlock> metadataBlocks,
            CreateContextDelegate createContext,
            CompileDelegate<TResult> compile,
            out string errorMessage)
        {
            return CompileWithRetry(
                metadataBlocks,
                this.DiagnosticFormatter,
                createContext,
                compile,
                (AssemblyIdentity assemblyIdentity, out uint size) => moduleInstance.AppDomain.GetMetaDataBytesPtr(assemblyIdentity.GetDisplayName(), out size),
                out errorMessage);
        }

        internal static TResult CompileWithRetry<TResult>(
            ImmutableArray<MetadataBlock> metadataBlocks,
            DiagnosticFormatter formatter,
            CreateContextDelegate createContext,
            CompileDelegate<TResult> compile,
            DkmUtilities.GetMetadataBytesPtrFunction getMetaDataBytesPtr,
            out string errorMessage)
        {
            TResult compileResult;

            PooledHashSet<AssemblyIdentity> assembliesLoadedInRetryLoop = null;
            bool tryAgain;
            do
            {
                errorMessage = null;

                var context = createContext(metadataBlocks, useReferencedModulesOnly: false);
                var diagnostics = DiagnosticBag.GetInstance();
                compileResult = compile(context, diagnostics);
                tryAgain = false;
                if (diagnostics.HasAnyErrors())
                {
                    bool useReferencedModulesOnly;
                    ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                    errorMessage = context.GetErrorMessageAndMissingAssemblyIdentities(
                        diagnostics,
                        formatter,
                        preferredUICulture: null,
                        useReferencedModulesOnly: out useReferencedModulesOnly,
                        missingAssemblyIdentities: out missingAssemblyIdentities);
                    if (useReferencedModulesOnly)
                    {
                        Debug.Assert(missingAssemblyIdentities.IsEmpty);
                        var otherContext = createContext(metadataBlocks, useReferencedModulesOnly: true);
                        var otherDiagnostics = DiagnosticBag.GetInstance();
                        var otherResult = compile(otherContext, otherDiagnostics);
                        if (!otherDiagnostics.HasAnyErrors())
                        {
                            errorMessage = null;
                            compileResult = otherResult;
                        }
                        otherDiagnostics.Free();
                    }
                    else
                    {
                        if (!missingAssemblyIdentities.IsEmpty)
                        {
                            if (assembliesLoadedInRetryLoop == null)
                            {
                                assembliesLoadedInRetryLoop = PooledHashSet<AssemblyIdentity>.GetInstance();
                            }
                            // If any identities failed to add (they were already in the list), then don't retry. 
                            if (assembliesLoadedInRetryLoop.AddAll(missingAssemblyIdentities))
                            {
                                tryAgain = ShouldTryAgainWithMoreMetadataBlocks(getMetaDataBytesPtr, missingAssemblyIdentities, ref metadataBlocks);
                            }
                        }
                    }
                }
                diagnostics.Free();
            } while (tryAgain);
            assembliesLoadedInRetryLoop?.Free();

            return compileResult;
        }

        private static DkmClrLocalVariableInfo ToLocalVariableInfo(LocalAndMethod local)
        {
            return DkmClrLocalVariableInfo.Create(local.LocalName, local.MethodName, local.Flags, DkmEvaluationResultCategory.Data, local.GetCustomTypeInfo().ToDkmClrCustomTypeInfo());
        }

        private struct GetLocalsResult
        {
            internal readonly string TypeName;
            internal readonly ReadOnlyCollection<DkmClrLocalVariableInfo> Locals;
            internal readonly ReadOnlyCollection<byte> Assembly;

            internal GetLocalsResult(string typeName, ReadOnlyCollection<DkmClrLocalVariableInfo> locals, ReadOnlyCollection<byte> assembly)
            {
                this.TypeName = typeName;
                this.Locals = locals;
                this.Assembly = assembly;
            }
        }

        private struct CompileExpressionResult
        {
            internal readonly CompileResult CompileResult;
            internal readonly ResultProperties ResultProperties;

            internal CompileExpressionResult(CompileResult compileResult, ResultProperties resultProperties)
            {
                this.CompileResult = compileResult;
                this.ResultProperties = resultProperties;
            }
        }
    }
}
