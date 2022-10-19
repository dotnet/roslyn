// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ValueTracking
{
    [UseExportProvider]
    public class VisualBasicValueTrackingTests : AbstractBaseValueTrackingTests
    {
        protected override TestWorkspace CreateWorkspace(string code, TestComposition composition)
            => TestWorkspace.CreateVisualBasic(code, composition: composition);

        [Theory]
        [CombinatorialData]
        public async Task TestProperty(TestHost testHost)
        {
            var code =
@"
Class C
    Private _s As String
    Public Property $$S() As String
        Get
            Return _s
        End Get
        Set(ByVal value As String)
            _s = value
        End Set
    End Property

    
    Public Sub SetS(s As String)
        Me.S = s
    End Sub

    Public Function GetS() As String
        Return Me.S
    End Function
End Class
";

            using var workspace = CreateWorkspace(code, testHost);
            var initialItems = await GetTrackedItemsAsync(workspace);

            //
            // property S 
            //  |> Me.S = s [Code.vb:14]
            //  |> Public Property S() As String [Code.vb:3]
            //
            Assert.Equal(2, initialItems.Length);
            ValidateItem(initialItems[0], 14);
            ValidateItem(initialItems[1], 3);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestPropertyValue(TestHost testHost)
        {
            var code =
@"
Class C
    Private _s As String
    Public Property S() As String
        Get
            Return _s
        End Get
        Set(ByVal value As String)
            _s = $$value
        End Set
    End Property

    
    Public Sub SetS(s As String)
        Me.S = s
    End Sub

    Public Function GetS() As String
        Return Me.S
    End Function
End Class
";

            //
            // _s = value [Code.vb:8]
            //  |> Me.S = s [Code.vb:14]
            //
            using var workspace = CreateWorkspace(code, testHost);

            var items = await ValidateItemsAsync(
                workspace,
                itemInfo: new[]
                {
                    (8, "value") // _s = [|value|] [Code.vb:8]
                });

            var childItems = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (14, "s") // Me.S = [|s|] [Code.vb:14]
                });

            await ValidateChildrenEmptyAsync(workspace, childItems.Single());
        }

        [Theory]
        [CombinatorialData]
        public async Task TestField(TestHost testHost)
        {
            var code =
@"
Class C
    Private $$_s As String = """"
    
    Public Sub SetS(s As String)
        Me._s = s
    End Sub

    Public Function GetS() As String
        Return Me._s
    End Function
End Class
";

            using var workspace = CreateWorkspace(code, testHost);
            var initialItems = await GetTrackedItemsAsync(workspace);

            //
            // field _s 
            //  |> Me._s = s [Code.vb:4]
            //  |> Private _s As String = "" [Code.vb:2]
            //
            await ValidateItemsAsync(
                workspace,
                itemInfo: new[]
                {
                    (5, "s"),
                    (2, "_s")
                });
        }

        [Theory]
        [CombinatorialData]
        public async Task TestLocal(TestHost testHost)
        {
            var code =
@"
Class C    
    Public Function Add(x As Integer, y As Integer) As Integer
        Dim $$z = x
        z += y
        Return z
    End Function
End Class
";

            using var workspace = CreateWorkspace(code, testHost);
            var initialItems = await GetTrackedItemsAsync(workspace);

            //
            // local variable z 
            //  |> z += y [Code.vb:4]
            //  |> Dim z = x [Code.vb:3]
            //
            Assert.Equal(2, initialItems.Length);
            ValidateItem(initialItems[0], 4);
            ValidateItem(initialItems[1], 3);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestParameter(TestHost testHost)
        {
            var code =
@"
Class C    
    Public Function Add($$x As Integer, y As Integer) As Integer
        x += y
        Return x
    End Function
End Class
";

            using var workspace = CreateWorkspace(code, testHost);
            var initialItems = await GetTrackedItemsAsync(workspace);

            //
            // parameter x
            //  |> x += y [Code.vb:3]
            //  |> Public Function Add(x As integer, y As Integer) As Integer [Code.vb:2]
            //
            Assert.Equal(2, initialItems.Length);
            ValidateItem(initialItems[0], 3);
            ValidateItem(initialItems[1], 2);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestVariableReferenceStart(TestHost testHost)
        {
            var code =
@"
Class Test
    Public Sub M()
        Dim x = GetM()
        Console.Write(x)
        Dim y = $$x + 1
    End Sub

    Public Function GetM() As Integer
        Dim x = 0
        Return x
    End Function
End Class";

            //
            //  |> Dim y = x + 1 [Code.vb:7]
            //    |> Dim x = GetM() [Code.vb:5]
            //      |> Return x; [Code.vb:13]
            //        |> Dim x = 0; [Code.vb:12]
            using var workspace = CreateWorkspace(code, testHost);

            var items = await ValidateItemsAsync(
                workspace,
                itemInfo: new[]
                {
                    (5, "x") // |> Dim y = [|x|] + 1; [Code.vb:7]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (3, "GetM()") // |> Dim x = [|GetM()|] [Code.vb:5]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (10, "x") // |> return [|x|]; [Code.vb:13]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (9, "0") // |> var x = [|0|]; [Code.vb:12]
                });

            await ValidateChildrenEmptyAsync(workspace, items.Single());
        }

        [Theory]
        [CombinatorialData]
        public async Task TestVariableReferenceStart2(TestHost testHost)
        {
            var code =
@"
Class Test
    Public Sub M()
        Dim x = GetM()
        Console.Write($$x)
        Dim y = x + 1
    End Sub

    Public Function GetM() As Integer
        Dim x = 0
        Return x
    End Function
End Class";

            //
            //  |> Dim y = x + 1 [Code.vb:7]
            //    |> Dim x = GetM() [Code.vb:5]
            //      |> Return x; [Code.vb:13]
            //        |> Dim x = 0; [Code.vb:12]
            using var workspace = CreateWorkspace(code, testHost);

            var items = await ValidateItemsAsync(
                workspace,
                itemInfo: new[]
                {
                    (4, "x") // |> Dim y = [|x|] + 1; [Code.vb:7]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (3, "GetM()") // |> Dim x = [|GetM()|] [Code.vb:5]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (10, "x") // |> return [|x|]; [Code.vb:13]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (9, "0") // |> var x = [|0|]; [Code.vb:12]
                });

            await ValidateChildrenEmptyAsync(workspace, items.Single());
        }

        [Theory]
        [CombinatorialData]
        public async Task TestMultipleDeclarators(TestHost testHost)
        {
            var code =
@"
Imports System

Class Test
    Public Sub M()
        Dim x = GetM(), z = 1, m As Boolean, n As Boolean, o As Boolean
        Console.Write(x)
        Dim y = $$x + 1
    End Sub

    Public Function GetM() As Integer
        Dim x = 0
        Return x
    End Function
End Class";

            //
            //  |> Dim y = x + 1 [Code.vb:7]
            //    |> Dim x = GetM(), z = 1, m As Boolean, n As Boolean, o As Boolean [Code.vb:5]
            //      |> Return x; [Code.vb:12]
            //        |> Dim x = 0; [Code.vb:11]
            using var workspace = CreateWorkspace(code, testHost);

            var items = await ValidateItemsAsync(
                workspace,
                itemInfo: new[]
                {
                    (7, "x") // |> Dim y = [|x|] + 1; [Code.vb:7]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (5, "GetM()") // |> Dim x = [|GetM()|], z = 1, m As Boolean, n As Boolean, o As Boolean [Code.vb:5]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (12, "x") // |> return [|x|]; [Code.vb:12]
                });

            items = await ValidateChildrenAsync(
                workspace,
                items.Single(),
                childInfo: new[]
                {
                    (11, "0") // |> var x = [|0|]; [Code.vb:11]
                });

            await ValidateChildrenEmptyAsync(workspace, items.Single());
        }
    }
}
