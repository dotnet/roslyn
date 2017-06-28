' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

