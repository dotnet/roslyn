' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers     
    ''' <summary>
    ''' RS0017: Remove deleted types and members from the declared API
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class BasicRemoveDeletedTypesAndMembersFromTheDeclaredAPIFixer
        Inherits RemoveDeletedTypesAndMembersFromTheDeclaredAPIFixer 

    End Class
End Namespace
