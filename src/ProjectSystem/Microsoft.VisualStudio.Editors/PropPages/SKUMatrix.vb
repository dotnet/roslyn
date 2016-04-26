' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports VSLangProj80
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    'Class containing information for specific VS product SKUs
    'Currently only supports whether a property is visible on the
    'property pages.  Any other SKU specific info should go here.
    Friend NotInheritable Class SKUMatrix

        ' This guid is duplicated in "src\appid\VW8Express\stub\guids.h" and "src\wizard\vbdesigner\AppDesigner\PropPages\SKUMatrix.vb"
        Private Shared Readonly s_guidShowEnableUnmanagedDebugging As New Guid("2172A533-76E4-483F-BFB9-71D9B8253B13")

        Private Sub New()
            'Disallow creation
        End Sub

        Friend Shared Function IsHidden(ByVal PropertyId As Integer) As Boolean

            If VSProductSKU.IsExpress Then
                'These properties are to be hidden for all Express SKU
                'VSWhidbey # 239181 - Disable unmanaged debugging in all express SKUs
                '(except VC ... but VC is handled by a different property page so
                'we do not need to worry about it here).

                Select Case PropertyId
                    Case VsProjPropId.VBPROJPROPID_RemoteDebugEnabled, _
                        VsProjPropId.VBPROJPROPID_EnableSQLServerDebugging, _
                        VsProjPropId.VBPROJPROPID_StartAction

                        Return True

                    Case VsProjPropId.VBPROJPROPID_EnableUnmanagedDebugging
                        Return Not UIContext.FromUIContextGuid(s_guidShowEnableUnmanagedDebugging).IsActive
                End Select

                'These properties are to be hidden for the VB Express SKU
                If VSProductSKU.IsVB Then
                    Select Case PropertyId
                        Case VsProjPropId.VBPROJPROPID_RegisterForComInterop, _
                         VsProjPropId.VBPROJPROPID_IncrementalBuild, _
                         VsProjPropId.VBPROJPROPID_DocumentationFile, _
                         VsProjPropId2.VBPROJPROPID_PreBuildEvent, _
                         VsProjPropId2.VBPROJPROPID_PostBuildEvent, _
                         VsProjPropId2.VBPROJPROPID_RunPostBuildEvent
                            Return True
                    End Select
                End If
            ElseIf VSProductSKU.IsStandard Then
                Select Case PropertyId
                    Case VsProjPropId.VBPROJPROPID_EnableSQLServerDebugging, _
                        VsProjPropId.VBPROJPROPID_RemoteDebugEnabled

                        Return True
                End Select
            End If

            Return False
        End Function

    End Class


End Namespace
