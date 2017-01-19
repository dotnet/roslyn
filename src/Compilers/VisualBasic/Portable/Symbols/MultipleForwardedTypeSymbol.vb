' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class MultipleForwardedTypeSymbol
        Inherits ErrorTypeSymbol

        Private ReadOnly _metadataTypeName As MetadataTypeName
        Private ReadOnly _assembly1 As AssemblySymbol
        Private ReadOnly _assembly2 As AssemblySymbol

        Public Sub New(metadataTypeName As MetadataTypeName, assembly1 As AssemblySymbol, assembly2 As AssemblySymbol)
            _metadataTypeName = metadataTypeName
            _assembly1 = assembly1
            _assembly2 = assembly2
        End Sub

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return ErrorFactory.ErrorInfo(ERRID.ERR_TypeForwardedToMultipleAssemblies, _metadataTypeName.FullName, _assembly1.Name, _assembly2.Name)
            End Get
        End Property

    End Class

End Namespace
