﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editing

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Option_PlaceSystemNamespaceFirst As Boolean
            Get
                Return GetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst)
            End Get
            Set(value As Boolean)
                SetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst, value)
            End Set
        End Property

        Public Property Option_SeparateImportDirectiveGroups As Boolean
            Get
                Return GetBooleanOption(GenerationOptions.SeparateImportDirectiveGroups)
            End Get
            Set(value As Boolean)
                SetBooleanOption(GenerationOptions.SeparateImportDirectiveGroups, value)
            End Set
        End Property
    End Class
End Namespace
