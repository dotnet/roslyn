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

        Friend Overrides Function CreateTypeContext(
            appDomain As DkmClrAppDomain,
            metadataBlocks As ImmutableArray(Of MetadataBlock),
            moduleVersionId As Guid,
            typeToken As Integer,
            useReferencedModulesOnly As Boolean) As EvaluationContextBase

            If useReferencedModulesOnly Then
                ' Avoid using the cache for referenced assemblies only
                ' since this should be the exceptional case.
                Dim compilation = metadataBlocks.ToCompilationReferencedModulesOnly(moduleVersionId)
                Return EvaluationContext.CreateTypeContext(
                    compilation,
                    moduleVersionId,
                    typeToken)
            End If

            Dim previous = appDomain.GetMetadataContext(Of VisualBasicMetadataContext)()
            Dim context = EvaluationContext.CreateTypeContext(
                previous,
                metadataBlocks,
                moduleVersionId,
                typeToken)

            ' New type context is not attached to the AppDomain since it is less
            ' re-usable than the previous attached method context. (We could hold
            ' on to it if we don't have a previous method context but it's unlikely
            ' that we evaluated a type-level expression before a method-level.)
            Debug.Assert(context IsNot previous.EvaluationContext)

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

            If useReferencedModulesOnly Then
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

            Dim previous = appDomain.GetMetadataContext(Of VisualBasicMetadataContext)()
            Dim context = EvaluationContext.CreateMethodContext(
                previous,
                metadataBlocks,
                lazyAssemblyReaders,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken)

            If context IsNot previous.EvaluationContext Then
                appDomain.SetMetadataContext(New VisualBasicMetadataContext(metadataBlocks, context))
            End If

            Return context
        End Function

        Friend Overrides Sub RemoveDataItem(appDomain As DkmClrAppDomain)
            appDomain.RemoveMetadataContext(Of VisualBasicMetadataContext)()
        End Sub

    End Class

End Namespace
