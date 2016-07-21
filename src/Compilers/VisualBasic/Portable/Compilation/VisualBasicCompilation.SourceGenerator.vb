' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public NotInheritable Class VisualBasicCompilation

        Friend Overrides Function GetSourceGeneratorTypeContext(builder As ArrayBuilder(Of SyntaxTree),
                                                                attributeName As String,
                                                                path As String,
                                                                writeToDisk As Boolean) As SourceGeneratorTypeContext
            Throw New NotImplementedException()
        End Function

    End Class

End Namespace