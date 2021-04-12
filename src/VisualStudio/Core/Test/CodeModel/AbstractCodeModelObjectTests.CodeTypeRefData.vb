' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Partial Public MustInherit Class AbstractCodeModelObjectTests(Of TCodeModelObject As Class)

        Protected Class CodeTypeRefData
            Public Property CodeTypeFullName As String
            Public Property TypeKind As EnvDTE.vsCMTypeRef = EnvDTE.vsCMTypeRef.vsCMTypeRefOther
            Public Property AsString As String
            Public Property AsFullName As String
        End Class

    End Class
End Namespace

