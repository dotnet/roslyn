' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Information decoded from early well-known custom attributes applied on a parameter.
    ''' </summary>
    Friend NotInheritable Class ParameterEarlyWellKnownAttributeData
        Inherits CommonParameterEarlyWellKnownAttributeData

#Region "MarshalAsAttribute"
        ' only used for parameters of Declare methods
        Private m_hasMarshalAsAttribute As Boolean

        Friend Property HasMarshalAsAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me.m_hasMarshalAsAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me.m_hasMarshalAsAttribute = value
                SetDataStored()
            End Set
        End Property

#End Region

#Region "ParamArrayAttribute"
        ' only used for parameters 
        Private m_hasParamArrayAttribute As Boolean

        Friend Property HasParamArrayAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me.m_hasParamArrayAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me.m_hasParamArrayAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

    End Class
End Namespace