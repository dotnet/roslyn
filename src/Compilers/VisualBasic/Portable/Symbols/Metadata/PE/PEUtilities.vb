' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
    Module PEUtilities

        Friend Sub DeriveUseSiteInfoFromCompilerFeatureRequiredAttributes(ByRef result As UseSiteInfo(Of AssemblySymbol), symbol As Symbol, handle As System.Reflection.Metadata.EntityHandle, allowedFeatures As CompilerFeatureRequiredFeatures, Optional decoder As MetadataDecoder = Nothing)
            If result.DiagnosticInfo IsNot Nothing Then
                Debug.Assert(result.DiagnosticInfo.Severity = DiagnosticSeverity.Error)
                Return
            End If

            Dim [module] = DirectCast(symbol.ContainingModule, PEModuleSymbol)
            decoder = If(decoder, New MetadataDecoder([module]))
            Dim unsupportedFeature = [module].Module.GetUnsupportedCompilerFeature(handle, decoder, [module], allowedFeatures)
            If unsupportedFeature IsNot Nothing Then
                ' '{0}' requires compiler feature '{1}', which is not supported by this version of the Visual Basic compiler.
                result = result.AdjustDiagnosticInfo(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedCompilerFeature, symbol, unsupportedFeature))
                Return
            End If

            ' If this symbol is Shared, we also want to check the containing type
            If symbol.IsShared AndAlso symbol.ContainingType IsNot Nothing Then
                Dim containingType = DirectCast(symbol.ContainingType, PENamedTypeSymbol)
                DeriveUseSiteInfoFromCompilerFeatureRequiredAttributes(result, containingType, containingType.Handle, CompilerFeatureRequiredFeatures.None, decoder)
            End If
        End Sub
    End Module
End Namespace
