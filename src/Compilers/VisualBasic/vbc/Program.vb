' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
    Public Module Program
        Function Main(args As String()) As Integer
            Dim responseFile = CommonCompiler.GetResponseFileFullPath(VisualBasicCompiler.ResponseFileName)
            Return Vbc.Run(responseFile, args)
        End Function
    End Module
End Namespace
