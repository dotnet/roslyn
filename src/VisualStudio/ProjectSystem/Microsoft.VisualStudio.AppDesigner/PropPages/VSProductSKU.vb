'******************************************************************************
'* VSProductSKU.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports System
Imports System.Diagnostics
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop

'NOTE: To test property pages under different SKUs, use the PDSku and PDSubSku
'  switches (see common\switches.vb).

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    'Class retries SKU info from the shell to make available
    ' within our assembly
    Public NotInheritable Class VSProductSKU

        Private Shared m_ProductSKU As VSASKUEdition = VSASKUEdition.None
        Private Shared m_ProductSubSKU As VSASubSKUEdition = VSASubSKUEdition.None

        'CONSIDER: The preferred way to enable/disable runtime features is now to use
        '  registry keys (which are controlled via FLDB) rather than using the SKU/SubSKU.
        '  Too late to change now, should consider for next version.
        Public Enum VSASKUEdition
            None = 0
            Express = 500 'From vsappid80.idl
            Standard = 1000
            VSTO = 1500   'From vsappid80.idl
            Professional = 2000
            AcademicStudent = 2100
            'AcademicStudentMSDNAA = 2200
            'AcademicTeaching = 2300
            'AcademicEnterprise = AcademicTeaching  ' OBSOLETTE, use AcademicTeaching
            AcademicProfessional = AcademicStudent ' OBSOLETTE, use AcademicStudent
            ' Book                  = 2400,  ' OBSOLETTE
            DownloadTrial = 2500
            Enterprise = 3000
        End Enum

        Public Enum VSASubSKUEdition As Integer
            None = 0
            VC = &H1
            VB = &H2
            CSharp = &H4
            Architect = &H8
            IDE = &H10
            JSharp = &H20
            Web = &H40 'from vsappid80.idl
        End Enum

        Private Const VSAPROPID_SKUEdition As Integer = -8534
        Private Const VSAPROPID_SubSKUEdition As Integer = -8546


        ''' <summary>
        ''' Returns the product SKU as an enum.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property ProductSKU() As VSASKUEdition
            Get
                EnsureInited()
                Return m_ProductSKU
            End Get
        End Property

        ''' <summary>
        ''' Returns the product Sub-SKU as an enum.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property ProductSubSKU() As VSASubSKUEdition
            Get
                EnsureInited()
                Return m_ProductSubSKU
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is a Standard SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsStandard() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSKU >= VSASKUEdition.Standard AndAlso m_ProductSKU < VSASKUEdition.VSTO)
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff this is a VSTO SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property IsVSTO() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSKU >= VSASKUEdition.VSTO AndAlso m_ProductSKU < VSASKUEdition.Professional)
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is a Professional SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsProfessional() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSKU >= VSASKUEdition.Professional AndAlso m_ProductSKU < VSASKUEdition.Enterprise)
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff this is an Express SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsExpress() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSKU = VSASKUEdition.Express)
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff this is an Academic SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsAcademic() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSKU = VSASKUEdition.AcademicStudent)
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff this is an Enterprise SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsEnterprise() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSKU >= VSASKUEdition.Enterprise)
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff this is a VB SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property IsVB() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSubSKU = VSASubSKUEdition.VB)
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff this is a VC SKU
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property IsVC() As Boolean
            Get
                EnsureInited()
                Return (m_ProductSubSKU = VSASubSKUEdition.VC)
            End Get
        End Property


#Region "Private implementation"

        ''' <summary>
        ''' Makes sure that our information on the current SKU has been read, and reads it if not
        ''' </summary>
        ''' <remarks></remarks>
        Private Shared Sub EnsureInited()
            If m_ProductSKU = VSASKUEdition.None Then
                If Utils.VBPackageInstance IsNot Nothing Then
                    Init(DirectCast(Utils.VBPackageInstance, IServiceProvider))
                End If
            End If
        End Sub


        ''' <summary>
        ''' Reads information on the current SKU
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <remarks></remarks>
        Private Shared Sub Init(ByVal ServiceProvider As IServiceProvider)
            Dim VsAppIdService As IVsAppId
            Dim objSKU As Object = Nothing
            Dim objSubSKU As Object = Nothing
            Dim hr As Integer

            If ServiceProvider Is Nothing Then
                Return
            End If

            VsAppIdService = TryCast(ServiceProvider.GetService(GetType(IVsAppId)), IVsAppId)
            If VsAppIdService IsNot Nothing Then
                Try
                    hr = VsAppIdService.GetProperty(VSAPROPID_SKUEdition, objSKU)
                    If hr >= 0 AndAlso (TypeOf objSKU Is Integer) Then
                        m_ProductSKU = DirectCast(CInt(objSKU), VSASKUEdition)
                    End If
                    hr = VsAppIdService.GetProperty(VSAPROPID_SubSKUEdition, objSubSKU)
                    If hr >= 0 AndAlso (TypeOf objSubSKU Is Integer) Then
                        m_ProductSubSKU = DirectCast(CInt(objSubSKU), VSASubSKUEdition)
                    End If
                Catch ex As Exception
                    'ignore for now
                    Debug.Fail("Exception getting SKU from AppId service: " & ex.ToString)
                    Debug.WriteLine(ex.ToString())
                End Try
            End If

#If DEBUG Then
            Trace.WriteLine("Project Designer: SKU detected as " & m_ProductSKU.ToString())
            Trace.WriteLine("Project Designer: Sub-SKU detected as " & m_ProductSubSKU.ToString())

            If Switches.PDSku.ValueDefined Then
                Dim NewSku As VSASKUEdition = Switches.PDSku.Value
                Trace.WriteLine("****** PROJECT DESIGNER ONLY: OVERRIDING SKU VALUE TO: " & NewSku.ToString())
                m_ProductSKU = NewSku
            End If
            If Switches.PDSubSku.ValueDefined Then
                Dim NewSubSku As VSASubSKUEdition = Switches.PDSubSku.Value
                Trace.WriteLine("****** PROJECT DESIGNER ONLY: OVERRIDING SUB-SKU VALUE TO: " & NewSubSku.ToString())
                m_ProductSubSKU = NewSubSku
            End If
#End If
        End Sub

#End Region

    End Class

End Namespace
