' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Specifies the syntax that a user defined variable comes from.
    ''' </summary>
    Friend Enum LocalDeclarationKind As Byte
        ''' <summary> 
        ''' The local is not user defined nor it is a copy of a user defined local (e.g. with a substituted type).
        ''' Check the value of <see cref="LocalSymbol.SynthesizedKind"/> for the kind of synthesized variable.
        ''' </summary> 
        None
        Variable

        ''' <summary>
        ''' Implicitly declared variable (w/o variable declaration).
        ''' </summary>
        ImplicitVariable

        Constant
        [Static]
        [Using]
        [Catch]
        [For]
        ForEach
        FunctionValue
        ''' <summary> 
        ''' Only used in flow analysis for the pseudo-local representing a symbol 
        ''' of the implicit receiver in case Dim statement defines more than one 
        ''' variable, but uses the same object initializer for all of them, like in: 
        '''     Dim a,b As New C() With { .X = .Y } 
        ''' </summary>
        AmbiguousLocals
    End Enum

End Namespace
