' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Enum GeneratedNameKind
        None = 0
        HoistedMeField
        HoistedSynthesizedLocalField
        HoistedUserVariableField
        IteratorCurrentField
        IteratorInitialThreadIdField
        IteratorParameterProxyField
        StateMachineAwaiterField
        StateMachineStateField
        StateMachineHoistedUserVariableOrDisplayClassField
        HoistedWithLocalPrefix
        StaticLocalField
        TransparentIdentifier
        AnonymousTransparentIdentifier
        AnonymousType

        LambdaCacheField
        LambdaDisplayClass
    End Enum
End Namespace
