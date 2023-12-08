// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
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
        IDkmModuleModifiedNotification,
        IDkmModuleInstanceUnloadNotification,
        IDkmLanguageFrameDecoder,
        IDkmLanguageInstructionDecoder
    {
        // Need to support IDkmLanguageFrameDecoder and IDkmLanguageInstructionDecoder
        // See https://github.com/dotnet/roslyn/issues/22620
        private readonly IDkmLanguageFrameDecoder _languageFrameDecoder;
        private readonly IDkmLanguageInstructionDecoder _languageInstructionDecoder;
        private readonly bool _useReferencedAssembliesOnly;

        public ExpressionCompiler(IDkmLanguageFrameDecoder languageFrameDecoder, IDkmLanguageInstructionDecoder languageInstructionDecoder)
        {
            _languageFrameDecoder = languageFrameDecoder;
            _languageInstructionDecoder = languageInstructionDecoder;
            _useReferencedAssembliesOnly = GetUseReferencedAssembliesOnlySetting();
        }

        DkmCompiledClrLocalsQuery IDkmClrExpressionCompiler.GetClrLocalVariableQuery(
            DkmInspectionContext inspectionContext,
            DkmClrInstructionAddress instructionAddress,
            bool argumentsOnly)
        {
            try
            {
                var moduleInstance = instructionAddress.ModuleInstance;
                var runtimeInstance = instructionAddress.RuntimeInstance;
                var aliases = argumentsOnly
                    ? ImmutableArray<Alias>.Empty
                    : GetAliases(runtimeInstance, inspectionContext); // NB: Not affected by retrying.
                string? error;
                var r = CompileWithRetry(
                    moduleInstance.AppDomain,
                    runtimeInstance,
                    (blocks, useReferencedModulesOnly) => CreateMethodContext(instructionAddress, blocks, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        var builder = ArrayBuilder<LocalAndMethod>.GetInstance();
                        var assembly = context.CompileGetLocals(
                            builder,
                            argumentsOnly,
                            aliases,
                            diagnostics,
                            out var typeName,
                            testData: null);
                        Debug.Assert((builder.Count == 0) == (assembly.Count == 0));
                        var locals = new ReadOnlyCollection<DkmClrLocalVariableInfo>(builder.SelectAsArray(ToLocalVariableInfo));
                        builder.Free();
                        return new GetLocalsResult(typeName, locals, assembly);
                    },
                    out error);
                return DkmCompiledClrLocalsQuery.Create(runtimeInstance, null, CompilerId, r.Assembly, r.TypeName, r.Locals);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static ImmutableArray<Alias> GetAliases(DkmClrRuntimeInstance runtimeInstance, DkmInspectionContext? inspectionContext)
        {
            var dkmAliases = runtimeInstance.GetAliases(inspectionContext);
            if (dkmAliases == null)
            {
                return ImmutableArray<Alias>.Empty;
            }

            var builder = ArrayBuilder<Alias>.GetInstance(dkmAliases.Count);
            foreach (var dkmAlias in dkmAliases)
            {
                builder.Add(new Alias(
                    dkmAlias.Kind,
                    dkmAlias.Name,
                    dkmAlias.FullName,
                    dkmAlias.Type,
                    dkmAlias.CustomTypeInfoPayloadTypeId,
                    dkmAlias.CustomTypeInfoPayload));
            }
            return builder.ToImmutableAndFree();
        }

        void IDkmClrExpressionCompiler.CompileExpression(
            DkmLanguageExpression expression,
            DkmClrInstructionAddress instructionAddress,
            DkmInspectionContext? inspectionContext,
            out string? error,
            out DkmCompiledClrInspectionQuery? result)
        {
            try
            {
                var moduleInstance = instructionAddress.ModuleInstance;
                var runtimeInstance = instructionAddress.RuntimeInstance;
                var aliases = GetAliases(runtimeInstance, inspectionContext); // NB: Not affected by retrying.
                var r = CompileWithRetry(
                    moduleInstance.AppDomain,
                    runtimeInstance,
                    (blocks, useReferencedModulesOnly) => CreateMethodContext(instructionAddress, blocks, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        var compileResult = context.CompileExpression(
                            expression.Text,
                            expression.CompilationFlags,
                            aliases,
                            diagnostics,
                            out var resultProperties,
                            testData: null);
                        return new CompileExpressionResult(compileResult, resultProperties);
                    },
                    out error);
                result = r.CompileResult.ToQueryResult(CompilerId, r.ResultProperties, runtimeInstance);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        void IDkmClrExpressionCompiler.CompileAssignment(
            DkmLanguageExpression expression,
            DkmClrInstructionAddress instructionAddress,
            DkmEvaluationResult lValue,
            out string? error,
            out DkmCompiledClrInspectionQuery? result)
        {
            try
            {
                var moduleInstance = instructionAddress.ModuleInstance;
                var runtimeInstance = instructionAddress.RuntimeInstance;
                var aliases = GetAliases(runtimeInstance, lValue.InspectionContext); // NB: Not affected by retrying.
                var r = CompileWithRetry(
                    moduleInstance.AppDomain,
                    runtimeInstance,
                    (blocks, useReferencedModulesOnly) => CreateMethodContext(instructionAddress, blocks, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        // Concord marks this as nullable but it should always have a value in our scenario.
                        RoslynDebug.AssertNotNull(lValue.FullName);

                        var compileResult = context.CompileAssignment(
                            lValue.FullName,
                            expression.Text,
                            aliases,
                            diagnostics,
                            out var resultProperties,
                            testData: null);
                        return new CompileExpressionResult(compileResult, resultProperties);
                    },
                    out error);

                Debug.Assert(
                    r.CompileResult == null && r.ResultProperties.Flags == default ||
                    (r.ResultProperties.Flags & DkmClrCompilationResultFlags.PotentialSideEffect) == DkmClrCompilationResultFlags.PotentialSideEffect);

                result = r.CompileResult.ToQueryResult(CompilerId, r.ResultProperties, runtimeInstance);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        void IDkmClrExpressionCompilerCallback.CompileDisplayAttribute(
            DkmLanguageExpression expression,
            DkmClrModuleInstance moduleInstance,
            int token,
            out string? error,
            out DkmCompiledClrInspectionQuery? result)
        {
            try
            {
                var runtimeInstance = moduleInstance.RuntimeInstance;
                var appDomain = moduleInstance.AppDomain;
                var compileResult = CompileWithRetry(
                    appDomain,
                    runtimeInstance,
                    (blocks, useReferencedModulesOnly) => CreateTypeContext(appDomain, blocks, moduleInstance.Mvid, token, useReferencedModulesOnly),
                    (context, diagnostics) =>
                    {
                        return context.CompileExpression(
                            expression.Text,
                            DkmEvaluationFlags.TreatAsExpression,
                            ImmutableArray<Alias>.Empty,
                            diagnostics,
                            out var unusedResultProperties,
                            testData: null);
                    },
                    out error);
                result = compileResult.ToQueryResult(CompilerId, resultProperties: default, runtimeInstance);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        internal static bool GetUseReferencedAssembliesOnlySetting()
        {
            return RegistryHelpers.GetBoolRegistryValue("UseReferencedAssembliesOnly");
        }

        internal MakeAssemblyReferencesKind GetMakeAssemblyReferencesKind(bool useReferencedModulesOnly)
        {
            if (useReferencedModulesOnly)
            {
                return MakeAssemblyReferencesKind.DirectReferencesOnly;
            }
            return _useReferencedAssembliesOnly ? MakeAssemblyReferencesKind.AllReferences : MakeAssemblyReferencesKind.AllAssemblies;
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
            RemoveDataItemIfNecessary(moduleInstance);
        }

        void IDkmModuleInstanceUnloadNotification.OnModuleInstanceUnload(DkmModuleInstance moduleInstance, DkmWorkList workList, DkmEventDescriptor eventDescriptor)
        {
            RemoveDataItemIfNecessary(moduleInstance);
        }

        #region IDkmLanguageFrameDecoder, IDkmLanguageInstructionDecoder

        void IDkmLanguageFrameDecoder.GetFrameName(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmVariableInfoFlags argumentFlags, DkmCompletionRoutine<DkmGetFrameNameAsyncResult> completionRoutine)
        {
            _languageFrameDecoder.GetFrameName(inspectionContext, workList, frame, argumentFlags, completionRoutine);
        }

        void IDkmLanguageFrameDecoder.GetFrameReturnType(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmCompletionRoutine<DkmGetFrameReturnTypeAsyncResult> completionRoutine)
        {
            _languageFrameDecoder.GetFrameReturnType(inspectionContext, workList, frame, completionRoutine);
        }

        string IDkmLanguageInstructionDecoder.GetMethodName(DkmLanguageInstructionAddress languageInstructionAddress, DkmVariableInfoFlags argumentFlags)
        {
            return _languageInstructionDecoder.GetMethodName(languageInstructionAddress, argumentFlags);
        }

        #endregion

        private void RemoveDataItemIfNecessary(DkmModuleInstance moduleInstance)
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
            object? symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken,
            bool useReferencedModulesOnly);

        internal abstract void RemoveDataItem(DkmClrAppDomain appDomain);

        internal abstract ImmutableArray<MetadataBlock> GetMetadataBlocks(
            DkmClrAppDomain appDomain,
            DkmClrRuntimeInstance runtimeInstance);

        private EvaluationContextBase CreateMethodContext(
            DkmClrInstructionAddress instructionAddress,
            ImmutableArray<MetadataBlock> metadataBlocks,
            bool useReferencedModulesOnly)
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
            return CreateMethodContext(
                moduleInstance.AppDomain,
                metadataBlocks,
                new Lazy<ImmutableArray<AssemblyReaders>>(() => instructionAddress.MakeAssemblyReaders(), LazyThreadSafetyMode.None),
                symReader: moduleInstance.GetSymReader(),
                moduleVersionId: moduleInstance.Mvid,
                methodToken: methodToken,
                methodVersion: (int)instructionAddress.MethodId.Version,
                ilOffset: instructionAddress.ILOffset,
                localSignatureToken: localSignatureToken,
                useReferencedModulesOnly: useReferencedModulesOnly);
        }

        internal delegate EvaluationContextBase CreateContextDelegate(ImmutableArray<MetadataBlock> metadataBlocks, bool useReferencedModulesOnly);
        internal delegate TResult CompileDelegate<TResult>(EvaluationContextBase context, DiagnosticBag diagnostics);

        private TResult CompileWithRetry<TResult>(
            DkmClrAppDomain appDomain,
            DkmClrRuntimeInstance runtimeInstance,
            CreateContextDelegate createContext,
            CompileDelegate<TResult> compile,
            out string? errorMessage)
        {
            var metadataBlocks = GetMetadataBlocks(appDomain, runtimeInstance);
            return CompileWithRetry(
                metadataBlocks,
                DiagnosticFormatter,
                createContext,
                compile,
                (AssemblyIdentity assemblyIdentity, out uint size) => appDomain.GetMetaDataBytesPtr(assemblyIdentity.GetDisplayName(), out size),
                out errorMessage);
        }

        internal static TResult CompileWithRetry<TResult>(
            ImmutableArray<MetadataBlock> metadataBlocks,
            DiagnosticFormatter formatter,
            CreateContextDelegate createContext,
            CompileDelegate<TResult> compile,
            DkmUtilities.GetMetadataBytesPtrFunction getMetaDataBytesPtr,
            out string? errorMessage)
        {
            TResult compileResult;

            PooledHashSet<AssemblyIdentity>? assembliesLoadedInRetryLoop = null;
            bool tryAgain;
            var linqLibrary = EvaluationContextBase.SystemLinqIdentity;
            do
            {
                errorMessage = null;

                var context = createContext(metadataBlocks, useReferencedModulesOnly: false);
                var diagnostics = DiagnosticBag.GetInstance();
                compileResult = compile(context, diagnostics);
                tryAgain = false;
                if (diagnostics.HasAnyErrors())
                {
                    errorMessage = context.GetErrorMessageAndMissingAssemblyIdentities(
                        diagnostics,
                        formatter,
                        preferredUICulture: null,
                        linqLibrary: linqLibrary,
                        useReferencedModulesOnly: out var useReferencedModulesOnly,
                        missingAssemblyIdentities: out var missingAssemblyIdentities);

                    // If there were LINQ-related errors, we'll initially add System.Linq (set above).
                    // If that doesn't work, we'll fall back to System.Core for subsequent retries.
                    linqLibrary = EvaluationContextBase.SystemCoreIdentity;

                    // Can we remove the `useReferencedModulesOnly` attempt if we're only using
                    // modules reachable from the current module? In short, can we avoid retrying?
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
                            assembliesLoadedInRetryLoop ??= PooledHashSet<AssemblyIdentity>.GetInstance();
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
            ReadOnlyCollection<byte>? customTypeInfo;
            Guid customTypeInfoId = local.GetCustomTypeInfo(out customTypeInfo);
            return DkmClrLocalVariableInfo.Create(
                local.LocalDisplayName,
                local.LocalName,
                local.MethodName,
                local.Flags,
                DkmEvaluationResultCategory.Data,
                customTypeInfo.ToCustomTypeInfo(customTypeInfoId));
        }

        private readonly struct GetLocalsResult
        {
            internal readonly string TypeName;
            internal readonly ReadOnlyCollection<DkmClrLocalVariableInfo> Locals;
            internal readonly ReadOnlyCollection<byte> Assembly;

            internal GetLocalsResult(string typeName, ReadOnlyCollection<DkmClrLocalVariableInfo> locals, ReadOnlyCollection<byte> assembly)
            {
                TypeName = typeName;
                Locals = locals;
                Assembly = assembly;
            }
        }

        private readonly struct CompileExpressionResult
        {
            internal readonly CompileResult? CompileResult;
            internal readonly ResultProperties ResultProperties;

            internal CompileExpressionResult(CompileResult? compileResult, ResultProperties resultProperties)
            {
                CompileResult = compileResult;
                ResultProperties = resultProperties;
            }
        }
    }
}
