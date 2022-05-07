' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
    Module PEUtilities

        Friend Sub DeriveUseSiteInfoFromCompilerFeatureRequiredAttributes(ByRef result As UseSiteInfo(Of AssemblySymbol), symbol As Symbol, [module] As PEModuleSymbol, handle As System.Reflection.Metadata.EntityHandle, allowedFeatures As CompilerFeatureRequiredFeatures, decoder As MetadataDecoder)
            Dim unsupportedFeature = [module].Module.GetFirstUnsupportedCompilerFeatureFromToken(handle, decoder, allowedFeatures)
            If unsupportedFeature IsNot Nothing Then
                ' '{0}' requires compiler feature '{1}', which is not supported by this version of the Visual Basic compiler.
                result = result.AdjustDiagnosticInfo(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedCompilerFeature, symbol, unsupportedFeature))
                Return
            End If
        End Sub
    End Module
End Namespace
