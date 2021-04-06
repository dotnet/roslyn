// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ValueTracking
{
    [UseExportProvider]
    public class VisualBasicValueTrackingTests : AbstractBaseValueTrackingTests
    {
        [Fact]
        public async Task TestProperty()
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

            using var workspace = TestWorkspace.CreateVisualBasic(code);
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

        [Fact]
        public async Task TestField()
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

            using var workspace = TestWorkspace.CreateVisualBasic(code);
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

        [Fact]
        public async Task TestLocal()
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

            using var workspace = TestWorkspace.CreateVisualBasic(code);
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

        [Fact]
        public async Task TestParameter()
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

            using var workspace = TestWorkspace.CreateVisualBasic(code);
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
    }
}
