' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.SourceGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        Private ReadOnly Int32 As ITypeSymbol = CodeGenerator.SpecialType(SpecialType.System_Int32)
        Private ReadOnly [Boolean] As ITypeSymbol = CodeGenerator.SpecialType(SpecialType.System_Boolean)
        Private ReadOnly Void As ITypeSymbol = CodeGenerator.SpecialType(SpecialType.System_Void)
    End Class
End Namespace
