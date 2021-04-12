' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Partial Public MustInherit Class AbstractCodeModelObjectTests(Of TCodeModelObject As Class)

        Protected Class FunctionData
            Public Property Name As String
            Public Property Kind As EnvDTE.vsCMFunction = EnvDTE.vsCMFunction.vsCMFunctionFunction
            Public Property Type As Object
            Public Property Position As Object = 0
            Public Property Access As EnvDTE.vsCMAccess = EnvDTE.vsCMAccess.vsCMAccessDefault
            Public Property Location As Object
        End Class

    End Class
End Namespace
