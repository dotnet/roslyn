' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

