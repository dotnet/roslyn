' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic

    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class MultipleSymbolsRenameTests
        Private ReadOnly _outputHelper As ITestOutputHelper

        Public Sub New(outputHelper As ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

    End Class
End Namespace
