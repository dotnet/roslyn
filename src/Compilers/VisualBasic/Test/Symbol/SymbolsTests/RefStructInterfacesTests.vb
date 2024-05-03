' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class RefStructInterfacesTests
        Inherits BasicTestBase

        ' PROTOTYPE(RefStructInterfaces): Switch to supporting target framework once we have its ref assemblies.
        Private Shared ReadOnly s_targetFrameworkSupportingByRefLikeGenerics As TargetFramework = TargetFramework.Net80

        <Fact>
        Public Sub RuntimeCapability_01()

            Dim comp = CreateCompilation("", targetFramework:=s_targetFrameworkSupportingByRefLikeGenerics)
            Assert.True(comp.SupportsRuntimeCapability(RuntimeCapability.ByRefLikeGenerics))

            comp = CreateCompilation("", targetFramework:=TargetFramework.DesktopLatestExtended)
            Assert.False(comp.SupportsRuntimeCapability(RuntimeCapability.ByRefLikeGenerics))
        End Sub
    End Class
End Namespace

