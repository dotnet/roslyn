' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.UnitTests.Utilities.VsNavInfo
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ClassView
    Friend Class MockNavigationTool
        Implements IVsNavigationTool

        Private ReadOnly _canonicalNodes As NodeVerifier()
        Private ReadOnly _presentationNodes As NodeVerifier()
        Private _navInfo As IVsNavInfo

        Public Sub New(canonicalNodes As NodeVerifier(), presentationNodes As NodeVerifier())
            _canonicalNodes = canonicalNodes
            _presentationNodes = presentationNodes
        End Sub

        Public Function GetSelectedSymbols(ByRef ppIVsSelectedSymbols As IVsSelectedSymbols) As Integer Implements IVsNavigationTool.GetSelectedSymbols
            Throw New NotImplementedException()
        End Function

        Public Function NavigateToNavInfo(pNavInfo As IVsNavInfo) As Integer Implements IVsNavigationTool.NavigateToNavInfo
            Assert.Null(_navInfo)
            _navInfo = pNavInfo

            Return VSConstants.S_OK
        End Function

        Public Function NavigateToSymbol(ByRef guidLib As Guid, rgSymbolNodes() As SYMBOL_DESCRIPTION_NODE, ulcNodes As UInteger) As Integer Implements IVsNavigationTool.NavigateToSymbol
            Throw New NotImplementedException()
        End Function

        Public Sub VerifyNavInfo()
            Assert.NotNull(_navInfo)

            If _canonicalNodes IsNot Nothing Then
                Dim enumerator As IVsEnumNavInfoNodes = Nothing
                IsOK(Function() _navInfo.EnumCanonicalNodes(enumerator))

                VerifyNodes(enumerator, _canonicalNodes)
            End If

            If _presentationNodes IsNot Nothing Then
                Dim enumerator As IVsEnumNavInfoNodes = Nothing
                IsOK(Function() _navInfo.EnumPresentationNodes(CUInt(_LIB_LISTFLAGS.LLF_NONE), enumerator))

                VerifyNodes(enumerator, _presentationNodes)
            End If

        End Sub
    End Class
End Namespace