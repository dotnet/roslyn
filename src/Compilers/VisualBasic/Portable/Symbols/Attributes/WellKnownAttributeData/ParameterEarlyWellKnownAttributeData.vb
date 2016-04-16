' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Information decoded from early well-known custom attributes applied on a parameter.
    ''' </summary>
    Friend NotInheritable Class ParameterEarlyWellKnownAttributeData
        Inherits CommonParameterEarlyWellKnownAttributeData

#Region "MarshalAsAttribute"
        ' only used for parameters of Declare methods
        Private _hasMarshalAsAttribute As Boolean

        Friend Property HasMarshalAsAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._hasMarshalAsAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._hasMarshalAsAttribute = value
                SetDataStored()
            End Set
        End Property

#End Region

#Region "ParamArrayAttribute"
        ' only used for parameters 
        Private _hasParamArrayAttribute As Boolean

        Friend Property HasParamArrayAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._hasParamArrayAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._hasParamArrayAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

    End Class
End Namespace
