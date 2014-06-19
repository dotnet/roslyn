Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

#If REMOVE Then
    <Flags()>
    Friend Enum SyntaxFlags As Byte
        None = 0
        ContainsDiagnostics = 1 << 0
        HasStructuredTrivia = 1 << 1
        ContainsDirectives = 1 << 2
        HasSkippedText = 1 << 3
        ContainsAnnotations = 1 << 4
        NotMissing = 1 << 5
        ParsedInAsyncContext = 1 << 6
        ParsedInIteratorContext = 1 << 7

        FactoryContextMask = ParsedInAsyncContext Or ParsedInIteratorContext
        InheritMask = Byte.MaxValue And Not FactoryContextMask
    End Enum
#End If
End Namespace