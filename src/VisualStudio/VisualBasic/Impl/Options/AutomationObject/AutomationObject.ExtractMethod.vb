﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ExtractMethod

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property ExtractMethod_AllowBestEffort As Boolean
            Get
                Return GetBooleanOption(ExtractMethodOptions.AllowBestEffort)
            End Get
            Set(value As Boolean)
                SetBooleanOption(ExtractMethodOptions.AllowBestEffort, value)
            End Set
        End Property

        Public Property ExtractMethod_DoNotPutOutOrRefOnStruct As Boolean
            Get
                Return GetBooleanOption(ExtractMethodOptions.DontPutOutOrRefOnStruct)
            End Get
            Set(value As Boolean)
                SetBooleanOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, value)
            End Set
        End Property
    End Class
End Namespace
