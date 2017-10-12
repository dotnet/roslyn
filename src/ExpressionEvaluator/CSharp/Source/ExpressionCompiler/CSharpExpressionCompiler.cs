// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class CSharpExpressionCompiler : ExpressionCompiler, IDkmLanguageFrameDecoder, IDkmLanguageInstructionDecoder
    {
        private static readonly DkmCompilerId s_compilerId = new DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.CSharp);

        // Need to support IDkmLanguageFrameDecoder and IDkmLanguageInstructionDecoder
        // See https://github.com/dotnet/roslyn/issues/22620
        private CSharpFrameDecoder _frameDecoder;
        private CSharpLanguageInstructionDecoder _languageInstructionDecoder;

        internal CSharpExpressionCompiler(): base()
        {
            _frameDecoder = new CSharpFrameDecoder();
            _languageInstructionDecoder = new CSharpLanguageInstructionDecoder();
        }

        internal override DiagnosticFormatter DiagnosticFormatter
        {
            get { return DebuggerDiagnosticFormatter.Instance; }
        }

        internal override DkmCompilerId CompilerId
        {
            get { return s_compilerId; }
        }

        internal override EvaluationContextBase CreateTypeContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken,
            bool useReferencedModulesOnly)
        {
            if (useReferencedModulesOnly)
            {
                // Avoid using the cache for referenced assemblies only
                // since this should be the exceptional case.
                var compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId);
                return EvaluationContext.CreateTypeContext(
                    compilation,
                    moduleVersionId,
                    typeToken);
            }

            var previous = appDomain.GetMetadataContext<CSharpMetadataContext>();
            var context = EvaluationContext.CreateTypeContext(
                previous,
                metadataBlocks,
                moduleVersionId,
                typeToken);

            // New type context is not attached to the AppDomain since it is less
            // re-usable than the previous attached method context. (We could hold
            // on to it if we don't have a previous method context but it's unlikely
            // that we evaluated a type-level expression before a method-level.)
            Debug.Assert(context != previous.EvaluationContext);

            return context;
        }

        internal override EvaluationContextBase CreateMethodContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Lazy<ImmutableArray<AssemblyReaders>> unusedLazyAssemblyReaders,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken,
            bool useReferencedModulesOnly)
        {
            if (useReferencedModulesOnly)
            {
                // Avoid using the cache for referenced assemblies only
                // since this should be the exceptional case.
                var compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId);
                return EvaluationContext.CreateMethodContext(
                    compilation,
                    symReader,
                    moduleVersionId,
                    methodToken,
                    methodVersion,
                    ilOffset,
                    localSignatureToken);
            }

            var previous = appDomain.GetMetadataContext<CSharpMetadataContext>();
            var context = EvaluationContext.CreateMethodContext(
                previous,
                metadataBlocks,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken);

            if (context != previous.EvaluationContext)
            {
                appDomain.SetMetadataContext(new CSharpMetadataContext(metadataBlocks, context));
            }

            return context;
        }

        internal override void RemoveDataItem(DkmClrAppDomain appDomain)
        {
            appDomain.RemoveMetadataContext<CSharpMetadataContext>();
        }

        internal override ImmutableArray<MetadataBlock> GetMetadataBlocks(DkmClrAppDomain appDomain, DkmClrRuntimeInstance runtimeInstance)
        {
            var previous = appDomain.GetMetadataContext<CSharpMetadataContext>();
            return runtimeInstance.GetMetadataBlocks(appDomain, previous.MetadataBlocks);
        }

        #region IDkmLanguageFrameDecoder, IDkmLanguageInstructionDecoder

        void IDkmLanguageFrameDecoder.GetFrameName(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmVariableInfoFlags argumentFlags, DkmCompletionRoutine<DkmGetFrameNameAsyncResult> completionRoutine)
        {
            ((IDkmLanguageFrameDecoder)_frameDecoder).GetFrameName(inspectionContext, workList, frame, argumentFlags, completionRoutine);
        }

        void IDkmLanguageFrameDecoder.GetFrameReturnType(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmCompletionRoutine<DkmGetFrameReturnTypeAsyncResult> completionRoutine)
        {
            ((IDkmLanguageFrameDecoder)_frameDecoder).GetFrameReturnType(inspectionContext, workList, frame, completionRoutine);
        }

        string IDkmLanguageInstructionDecoder.GetMethodName(DkmLanguageInstructionAddress languageInstructionAddress, DkmVariableInfoFlags argumentFlags)
        {
            return ((IDkmLanguageInstructionDecoder)_languageInstructionDecoder).GetMethodName(languageInstructionAddress, argumentFlags);
        }

        #endregion
    }
}
