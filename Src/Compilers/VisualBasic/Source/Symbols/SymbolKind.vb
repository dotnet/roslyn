Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Represents the different kinds of symbol.
    ''' </summary>
    Public Enum SymbolKind
        [Alias] = 0
        ArrayType = 1
        Assembly = 2
        ' Dynamic type = 3
        ErrorType = 4
        [Event] = 5
        Field = 6
        Label = 7
        Local = 8
        Method = 9
        NetModule = 10
        NamedType = 11
        [Namespace] = 12
        Parameter = 13
        ' PointerType = 14
        [Property] = 15
        RangeVariable = 16
        TypeParameter = 17
        Preprocessing = 18
    End Enum
End Namespace