' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Causes lookups to ignore base classes. Used for binding
    ''' Imports statements.
    ''' </summary>
    Friend NotInheritable Class IgnoreBaseClassesBinder
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder, ignoreBaseClassesInLookup:=True)
        End Sub
    End Class

End Namespace
