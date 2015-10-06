// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Testing;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input.Commands
{
    public abstract class OpenProjectDesignerCommandBaseTests
    {
        [Fact]
        public void GetCommandStatusAsync_NullAsNodes_ThrowsArgumentNull()
        {
            var command = CreateInstance();

            Assert.Throws<ArgumentNullException>("nodes", () => {

                command.GetCommandStatusAsync((IImmutableSet<IProjectTree>)null, GetCommandId(), true, "commandText", CommandStatus.Enabled);
            });
        }

        [Fact]
        public async void GetCommandStatusAsync_UnrecognizedCommandIdAsCommandId_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, 1, true, "commandText", CommandStatus.Enabled);

            Assert.False(result.Handled);
        }

        [Fact]
        public async void TryHandleCommandAsync_UnrecognizedCommandIdAsCommandId_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, 1, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async void GetCommandStatusAsync_MoreThanOneNodeAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree, tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.False(result.Handled);
        }


        [Fact]
        public async void TryHandleCommandAsync_MoreThanOneNodeAsNodes_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree, tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);
            
            Assert.False(result);
        }

        [Fact]
        public async void GetCommandStatusAsync_NonAppDesignerFolderAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.False(result.Handled);
        }

        [Fact]
        public async void TryHandleCommandAsync_NonAppDesignerFolderAsNodes_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async void GetCommandStatusAsync_AppDesignerFolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Handled);
            Assert.Equal("commandText", result.CommandText);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
        }

        [Fact]
        public async void TryHandleCommandAsync_AppDesignerFolderAsNodes_ReturnsTrue()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
        }

        [Fact]
        public async void TryHandleCommandAsync_AppDesignerFolderAsNodes_CallsShowProjectDesignerAsync()
        {
            int callCount = 0;
            var designerService = IProjectDesignerServiceFactory.ImplementShowProjectDesignerAsync(() => { callCount++; });

            var command = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(1, callCount);
        }

        internal abstract long GetCommandId();

        internal abstract OpenProjectDesignerCommandBase CreateInstance(IProjectDesignerService designerService = null);
    }
}
