// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class CSharpExpressionCompiler : ExpressionCompiler
    {
        private static readonly DkmCompilerId s_compilerId = new DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.CSharp);

        public CSharpExpressionCompiler() : base(new CSharpFrameDecoder(), new CSharpLanguageInstructionDecoder())
        {
        }

        internal override DiagnosticFormatter DiagnosticFormatter
        {
            get { return DebuggerDiagnosticFormatter.Instance; }
        }

        internal override DkmCompilerId CompilerId
        {
            get { return s_compilerId; }
        }

        internal delegate MetadataContext<CSharpMetadataContext> GetMetadataContextDelegate<TAppDomain>(TAppDomain appDomain);
        internal delegate void SetMetadataContextDelegate<TAppDomain>(TAppDomain appDomain, MetadataContext<CSharpMetadataContext> metadataContext, bool report);

        internal override EvaluationContextBase CreateTypeContext(
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken,
            bool useReferencedModulesOnly)
        {
            return CreateTypeContext(
                appDomain,
                ad => ad.GetMetadataContext<CSharpMetadataContext>(),
                metadataBlocks,
                moduleVersionId,
                typeToken,
                GetMakeAssemblyReferencesKind(useReferencedModulesOnly));
        }

        internal static EvaluationContext CreateTypeContext<TAppDomain>(
            TAppDomain appDomain,
            GetMetadataContextDelegate<TAppDomain> getMetadataContext,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken,
            MakeAssemblyReferencesKind kind)
        {
            CSharpCompilation compilation;

            if (kind == MakeAssemblyReferencesKind.DirectReferencesOnly)
            {
                // Avoid using the cache for referenced assemblies only
                // since this should be the exceptional case.
                compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId);
                return EvaluationContext.CreateTypeContext(
                    compilation,
                    moduleVersionId,
                    typeToken);
            }

            var contextId = MetadataContextId.GetContextId(moduleVersionId, kind);
            var previous = getMetadataContext(appDomain);
            CSharpMetadataContext previousMetadataContext = default;
            if (previous.Matches(metadataBlocks))
            {
                previous.AssemblyContexts.TryGetValue(contextId, out previousMetadataContext);
            }

            // Re-use the previous compilation if possible.
            compilation = previousMetadataContext.Compilation;
            if (compilation == null)
            {
                compilation = metadataBlocks.ToCompilation(moduleVersionId, kind);
            }

            var context = EvaluationContext.CreateTypeContext(
                compilation,
                moduleVersionId,
                typeToken);

            // New type context is not attached to the AppDomain since it is less
            // re-usable than the previous attached method context. (We could hold
            // on to it if we don't have a previous method context but it's unlikely
            // that we evaluated a type-level expression before a method-level.)
            Debug.Assert(context != previousMetadataContext.EvaluationContext);

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
            return CreateMethodContext(
                appDomain,
                ad => ad.GetMetadataContext<CSharpMetadataContext>(),
                (ad, mc, report) => ad.SetMetadataContext<CSharpMetadataContext>(mc, report),
                metadataBlocks,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken,
                GetMakeAssemblyReferencesKind(useReferencedModulesOnly));
        }

        internal static EvaluationContext CreateMethodContext<TAppDomain>(
            TAppDomain appDomain,
            GetMetadataContextDelegate<TAppDomain> getMetadataContext,
            SetMetadataContextDelegate<TAppDomain> setMetadataContext,
            ImmutableArray<MetadataBlock> metadataBlocks,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken,
            MakeAssemblyReferencesKind kind)
        {
            CSharpCompilation compilation;
            int offset = EvaluationContextBase.NormalizeILOffset(ilOffset);

            if (kind == MakeAssemblyReferencesKind.DirectReferencesOnly)
            {
                // Avoid using the cache for referenced assemblies only
                // since this should be the exceptional case.
                compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId);
                return EvaluationContext.CreateMethodContext(
                    compilation,
                    symReader,
                    moduleVersionId,
                    methodToken,
                    methodVersion,
                    offset,
                    localSignatureToken);
            }

            var contextId = MetadataContextId.GetContextId(moduleVersionId, kind);
            var previous = getMetadataContext(appDomain);
            var assemblyContexts = previous.Matches(metadataBlocks) ? previous.AssemblyContexts : ImmutableDictionary<MetadataContextId, CSharpMetadataContext>.Empty;
            CSharpMetadataContext previousMetadataContext;
            assemblyContexts.TryGetValue(contextId, out previousMetadataContext);

            // Re-use the previous compilation if possible.
            compilation = previousMetadataContext.Compilation;
            if (compilation != null)
            {
                // Re-use entire context if method scope has not changed.
                var previousContext = previousMetadataContext.EvaluationContext;
                if (previousContext != null &&
                    previousContext.MethodContextReuseConstraints.HasValue &&
                    previousContext.MethodContextReuseConstraints.GetValueOrDefault().AreSatisfied(moduleVersionId, methodToken, methodVersion, offset))
                {
                    return previousContext;
                }
            }
            else
            {
                compilation = metadataBlocks.ToCompilation(moduleVersionId, kind);
            }

            var context = EvaluationContext.CreateMethodContext(
                compilation,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                offset,
                localSignatureToken);

            if (context != previousMetadataContext.EvaluationContext)
            {
                setMetadataContext(
                    appDomain,
                    new MetadataContext<CSharpMetadataContext>(
                        metadataBlocks,
                        assemblyContexts.SetItem(contextId, new CSharpMetadataContext(context.Compilation, context))),
                    report: kind == MakeAssemblyReferencesKind.AllReferences);
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
    }
}
