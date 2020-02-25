﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend Class SourceAssemblySymbol

        Friend Function GetAssemblyCustomAttributesToEmit(compilationState As ModuleCompilationState,
                                                          emittingRefAssembly As Boolean,
                                                          emittingAssemblyAttributesInNetModule As Boolean) As IEnumerable(Of VisualBasicAttributeData)

            Dim synthesized As ArrayBuilder(Of SynthesizedAttributeData) = Nothing
            AddSynthesizedAttributes(compilationState, synthesized)

            If emittingRefAssembly AndAlso Not HasReferenceAssemblyAttribute Then
                Dim referenceAssemblyAttribute = Me.DeclaringCompilation.
                    TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor, isOptionalUse:=True)

                Symbol.AddSynthesizedAttribute(synthesized, referenceAssemblyAttribute)
            End If

            Return GetCustomAttributesToEmit(Me.GetAttributes(), synthesized, isReturnType:=False, emittingAssemblyAttributesInNetModule:=emittingAssemblyAttributesInNetModule)
        End Function

    End Class
End Namespace
