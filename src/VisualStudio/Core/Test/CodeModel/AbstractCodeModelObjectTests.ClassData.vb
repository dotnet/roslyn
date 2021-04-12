' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Partial Public MustInherit Class AbstractCodeModelObjectTests(Of TCodeModelObject As Class)

        Protected Class ClassData
            Public Property Name As String
            Public Property Position As Object = 0
            Public Property Bases As Object
            Public Property ImplementedInterfaces As Object
            Public Property Access As EnvDTE.vsCMAccess = EnvDTE.vsCMAccess.vsCMAccessDefault
        End Class

    End Class
End Namespace
