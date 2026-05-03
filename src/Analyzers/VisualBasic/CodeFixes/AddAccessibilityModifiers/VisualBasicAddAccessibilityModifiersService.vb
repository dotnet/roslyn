' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.AddOrRemoveAccessibilityModifiers
    <ExportLanguageService(GetType(IAddOrRemoveAccessibilityModifiersService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicAddOrRemoveAccessibilityModifiersService
        Inherits VisualBasicAddOrRemoveAccessibilityModifiers
        Implements IAddOrRemoveAccessibilityModifiersService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
