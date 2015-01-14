' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Partial Friend Class StackScheduler

        Private Class DummyLocal
            Inherits SynthesizedLocal

            Public Sub New(container As Symbol)
                MyBase.New(container, Nothing, SynthesizedLocalKind.OptimizerTemp)
            End Sub

            Friend Overrides Function ComputeType(Optional containingBinder As Binder = Nothing) As TypeSymbol
                Throw ExceptionUtilities.Unreachable
            End Function

        End Class

    End Class
End Namespace

