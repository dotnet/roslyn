' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Partial Public MustInherit Class AbstractCodeModelObjectTests(Of TCodeModelObject As Class)

        Protected Class PropertyData
            Public Property GetterName As String
            Public Property PutterName As String
            Public Property Type As Object
            Public Property Position As Object = 0
            Public Property Access As EnvDTE.vsCMAccess = EnvDTE.vsCMAccess.vsCMAccessDefault
            Public Property Location As Object
        End Class

    End Class
End Namespace
