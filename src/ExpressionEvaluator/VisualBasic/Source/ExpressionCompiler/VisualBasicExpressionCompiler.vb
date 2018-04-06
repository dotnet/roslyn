' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    <DkmReportNonFatalWatsonException(ExcludeExceptionType:=GetType(NotImplementedException)), DkmContinueCorruptingException>
    Friend NotInheritable Class VisualBasicExpressionCompiler
        Inherits ExpressionCompiler

        Private Shared ReadOnly s_compilerId As New DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.VB)

        Public Sub New()
            MyBase.New(New VisualBasicFrameDecoder(), New VisualBasicLanguageInstructionDecoder())
        End Sub

        Friend Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return DebuggerDiagnosticFormatter.Instance
            End Get
        End Property

        Friend Overrides ReadOnly Property CompilerId As DkmCompilerId
            Get
                Return s_compilerId
            End Get
        End Property

        Friend Delegate Function GetMetadataContextDelegate(Of TAppDomain)(appDomain As TAppDomain) As MetadataContext(Of VisualBasicMetadataContext)
        Friend Delegate Sub SetMetadataContextDelegate(Of TAppDomain)(appDomain As TAppDomain, metadataContext As MetadataContext(Of VisualBasicMetadataContext))

        Friend Overrides Function CreateTypeContext(
            appDomain As DkmClrAppDomain,
            metadataBlocks As ImmutableArray(Of MetadataBlock),
            moduleVersionId As Guid,
            typeToken As Integer,
            useReferencedModulesOnly As Boolean) As EvaluationContextBase

            Return CreateTypeContextHelper(
                appDomain,
                Function(ad) ad.GetMetadataContext(Of MetadataContext(Of VisualBasicMetadataContext))(),
                metadataBlocks,
                moduleVersionId,
                typeToken,
                GetMakeAssemblyReferencesKind(useReferencedModulesOnly))
        End Function

        Friend Shared Function CreateTypeContextHelper(Of TAppDomain)(
            appDomain As TAppDomain,
            getMetadataContext As GetMetadataContextDelegate(Of TAppDomain),
            metadataBlocks As ImmutableArray(Of MetadataBlock),
            moduleVersionId As Guid,
            typeToken As Integer,
            kind As MakeAssemblyReferencesKind) As EvaluationContextBase

            If kind = MakeAssemblyReferencesKind.DirectReferencesOnly Then
                ' Avoid using the cache for referenced assemblies only
                ' since this should be the exceptional case.
                Dim compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId)
                Return EvaluationContext.CreateTypeContext(
                    compilation,
                    moduleVersionId,
                    typeToken)
            End If

            Dim previous = getMetadataContext(appDomain)
            Dim previousContext = If(previous.Matches(metadataBlocks, moduleVersionId), previous.AssemblyContext, Nothing)
            Dim context = EvaluationContext.CreateTypeContext(
                previousContext,
                metadataBlocks,
                moduleVersionId,
                typeToken,
                kind)

            ' New type context is not attached to the AppDomain since it is less
            ' re-usable than the previous attached method context. (We could hold
            ' on to it if we don't have a previous method context but it's unlikely
            ' that we evaluated a type-level expression before a method-level.)
            Debug.Assert(context IsNot previousContext.EvaluationContext)

            Return context
        End Function

        Friend Overrides Function CreateMethodContext(
            appDomain As DkmClrAppDomain,
            metadataBlocks As ImmutableArray(Of MetadataBlock),
            lazyAssemblyReaders As Lazy(Of ImmutableArray(Of AssemblyReaders)),
            symReader As Object,
            moduleVersionId As Guid,
            methodToken As Integer,
            methodVersion As Integer,
            ilOffset As UInteger,
            localSignatureToken As Integer,
            useReferencedModulesOnly As Boolean) As EvaluationContextBase

            Return CreateMethodContextHelper(
                appDomain,
                Function(ad) ad.GetMetadataContext(Of MetadataContext(Of VisualBasicMetadataContext))(),
                Sub(ad, mc) ad.SetMetadataContext(Of MetadataContext(Of VisualBasicMetadataContext))(mc),
                metadataBlocks,
                lazyAssemblyReaders,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken,
                GetMakeAssemblyReferencesKind(useReferencedModulesOnly))
        End Function

        Friend Shared Function CreateMethodContextHelper(Of TAppDomain)(
            appDomain As TAppDomain,
            getMetadataContext As GetMetadataContextDelegate(Of TAppDomain),
            setMetadataContext As SetMetadataContextDelegate(Of TAppDomain),
            metadataBlocks As ImmutableArray(Of MetadataBlock),
            lazyAssemblyReaders As Lazy(Of ImmutableArray(Of AssemblyReaders)),
            symReader As Object,
            moduleVersionId As Guid,
            methodToken As Integer,
            methodVersion As Integer,
            ilOffset As UInteger,
            localSignatureToken As Integer,
            kind As MakeAssemblyReferencesKind) As EvaluationContextBase

            If kind = MakeAssemblyReferencesKind.DirectReferencesOnly Then
                ' Avoid using the cache for referenced assemblies only
                ' since this should be the exceptional case.
                Dim compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId)
                Return EvaluationContext.CreateMethodContext(
                    compilation,
                    lazyAssemblyReaders,
                    symReader,
                    moduleVersionId,
                    methodToken,
                    methodVersion,
                    ilOffset,
                    localSignatureToken)
            End If

            Dim previous = getMetadataContext(appDomain)
            Dim previousContext = If(previous.Matches(metadataBlocks, moduleVersionId), previous.AssemblyContext, Nothing)
            Dim context = EvaluationContext.CreateMethodContext(
                previousContext,
                metadataBlocks,
                lazyAssemblyReaders,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken,
                kind)

            If context IsNot previousContext.EvaluationContext Then
                setMetadataContext(
                    appDomain,
                    New MetadataContext(Of VisualBasicMetadataContext)(
                        metadataBlocks,
                        moduleVersionId,
                        New VisualBasicMetadataContext(context.Compilation, context)))
            End If

            Return context
        End Function

        Friend Overrides Sub RemoveDataItem(appDomain As DkmClrAppDomain)
            appDomain.RemoveMetadataContext(Of MetadataContext(Of VisualBasicMetadataContext))()
        End Sub

        Friend Overrides Function GetMetadataBlocks(appDomain As DkmClrAppDomain, runtimeInstance As DkmClrRuntimeInstance) As ImmutableArray(Of MetadataBlock)
            Dim previous = appDomain.GetMetadataContext(Of MetadataContext(Of VisualBasicMetadataContext))()
            Return runtimeInstance.GetMetadataBlocks(appDomain, previous.MetadataBlocks)
        End Function

    End Class

End Namespace
