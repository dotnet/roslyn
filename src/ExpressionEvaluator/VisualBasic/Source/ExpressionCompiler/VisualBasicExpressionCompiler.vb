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

        Private Shared ReadOnly _compilerId As New DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.VB)

        Friend Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return VisualBasicDiagnosticFormatter.Instance
            End Get
        End Property

        Friend Overrides ReadOnly Property CompilerId As DkmCompilerId
            Get
                Return _compilerId
            End Get
        End Property

        Friend Overrides Function CreateTypeContext(
            appDomain As DkmClrAppDomain,
            metadataBlocks As ImmutableArray(Of MetadataBlock),
            moduleVersionId As Guid,
            typeToken As Integer) As EvaluationContextBase

            Dim previous = appDomain.GetDataItem(Of MetadataContextItem(Of VisualBasicMetadataContext))()
            Dim context = EvaluationContext.CreateTypeContext(
                previous.MetadataContext,
                metadataBlocks,
                moduleVersionId,
                typeToken)

            ' New type context is not attached to the AppDomain since it is less
            ' re-usable than the previous attached method context. (We could hold
            ' on to it if we don't have a previous method context but it's unlikely
            ' that we evaluated a type-level expression before a method-level.)
            Debug.Assert(previous Is Nothing OrElse context IsNot previous.MetadataContext.EvaluationContext)

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
            ilOffset As Integer,
            localSignatureToken As Integer) As EvaluationContextBase

            Dim previous = appDomain.GetDataItem(Of MetadataContextItem(Of VisualBasicMetadataContext))()
            Dim context = EvaluationContext.CreateMethodContext(
                previous.MetadataContext,
                metadataBlocks,
                lazyAssemblyReaders,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken)

            If (previous Is Nothing OrElse context IsNot previous.MetadataContext.EvaluationContext) Then
                appDomain.SetDataItem(DkmDataCreationDisposition.CreateAlways, New MetadataContextItem(Of VisualBasicMetadataContext)(New VisualBasicMetadataContext(context)))
            End If

            Return context
        End Function

        Friend Overrides Function RemoveDataItem(appDomain As DkmClrAppDomain) As Boolean
            Return appDomain.RemoveDataItem(Of MetadataContextItem(Of VisualBasicMetadataContext))()
        End Function

    End Class

End Namespace
