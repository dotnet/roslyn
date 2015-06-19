' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.DiaSymReader

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend Module SymUnmanagedReaderExtensions
        <Extension>
        Public Function GetMethodDebugInfo(
            reader As ISymUnmanagedReader,
            methodToken As Integer,
            methodVersion As Integer) As MethodDebugInfo

            Dim importStrings = reader.GetVisualBasicImportStrings(methodToken, methodVersion)
            If importStrings.IsDefault Then
                Return Nothing
            End If

            Dim projectLevelImportRecords = ArrayBuilder(Of ImportRecord).GetInstance()
            Dim fileLevelImportRecords = ArrayBuilder(Of ImportRecord).GetInstance()
            Dim defaultNamespaceName As String = Nothing

            For Each importString As String In importStrings
                Debug.Assert(importString IsNot Nothing)
                If importString.Length > 0 AndAlso importString(0) = "*"c Then
                    Dim [alias] As String = Nothing
                    Dim target As String = Nothing
                    Dim kind As ImportTargetKind = Nothing
                    Dim scope As ImportScope = Nothing
                    If Not CustomDebugInfoReader.TryParseVisualBasicImportString(importString, [alias], target, kind, scope) Then
                        Debug.WriteLine($"Unable to parse import string '{importString}'")
                        Continue For
                    ElseIf kind = ImportTargetKind.Defunct
                        Continue For
                    End If

                    Debug.Assert([alias] Is Nothing) ' The default namespace is never aliased.
                    Debug.Assert(target IsNot Nothing)
                    Debug.Assert(kind = ImportTargetKind.DefaultNamespace)

                    ' We only expect to see one of these, but it looks like ProcedureContext::LoadImportsAndDefaultNamespaceNormal
                    ' implicitly uses the last one if there are multiple.
                    Debug.Assert(defaultNamespaceName Is Nothing)

                    defaultNamespaceName = target
                Else
                    Dim importRecord As ImportRecord = Nothing
                    Dim scope As ImportScope = Nothing
                    If NativeImportRecord.TryCreateFromVisualBasicImportString(importString, importRecord, scope) Then
                        If scope = ImportScope.Project Then
                            projectLevelImportRecords.Add(importRecord)
                        Else
                            Debug.Assert(scope = ImportScope.File OrElse scope = ImportScope.Unspecified)
                            fileLevelImportRecords.Add(importRecord)
                        End If
                    Else
                        Debug.WriteLine($"Failed to parse import string {importString}")
                    End If
                End If
            Next

            ' Note: We don't need to try to bind this string because this is analogous to passing
            ' a command-line argument - as long as the syntax is valid, an appropriate symbol will
            ' be created for us.
            If String.IsNullOrEmpty(defaultNamespaceName) OrElse Not CompilationContext.TryParseDottedName(defaultNamespaceName, Nothing) Then
                defaultNamespaceName = ""
            End If

            Dim importRecordGroups = ImmutableArray.Create(
                projectLevelImportRecords.ToImmutableAndFree(),
                fileLevelImportRecords.ToImmutableAndFree())

            ' TODO (https://github.com/dotnet/roslyn/issues/702): portable format overload
            ' Somehow construct hoistedLocalScopeRecords.
            Dim hoistedLocalScopeRecords = ImmutableArray(Of HoistedLocalScopeRecord).Empty

            Return New MethodDebugInfo(
                hoistedLocalScopeRecords,
                importRecordGroups,
                defaultNamespaceName:=defaultNamespaceName,
                externAliasRecords:=ImmutableArray(Of ExternAliasRecord).Empty,
                dynamicLocalMap:=ImmutableDictionary(Of Integer, ImmutableArray(Of Boolean)).Empty,
                dynamicLocalConstantMap:=ImmutableDictionary(Of String, ImmutableArray(Of Boolean)).Empty)
        End Function

        ' TODO (https://github.com/dotnet/roslyn/issues/702): portable format overload
    End Module
End Namespace
