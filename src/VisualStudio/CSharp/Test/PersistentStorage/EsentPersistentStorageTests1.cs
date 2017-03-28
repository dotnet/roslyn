// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Esent;
using Microsoft.CodeAnalysis.Host;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    public class EsentPersistentStorageTests : AbstractPersistentStorageTests
    {
        protected override IPersistentStorageService GetStorageService()
        {
            return new EsentPersistentStorageService(_persistentEnabledOptionService, testing: true);
        }

        [Fact]
        public new Task PersistentService_Solution_WriteReadDifferentInstances()
        {
            return base.PersistentService_Solution_WriteReadDifferentInstances();
        }

        [Fact]
        public new Task PersistentService_Solution_WriteReadReopenSolution()
        {
            return base.PersistentService_Solution_WriteReadReopenSolution();
        }

        [Fact]
        public new Task PersistentService_Solution_WriteReadSameInstance()
        {
            return base.PersistentService_Solution_WriteReadSameInstance();
        }

        [Fact]
        public new Task PersistentService_Project_WriteReadSameInstance()
        {
            return base.PersistentService_Project_WriteReadSameInstance();
        }

        [Fact]
        public new Task PersistentService_Document_WriteReadSameInstance()
        {
            return base.PersistentService_Document_WriteReadSameInstance();
        }

        [Fact]
        public new Task PersistentService_Solution_SimultaneousWrites()
        {
            return base.PersistentService_Solution_SimultaneousWrites();
        }

        [Fact]
        public new Task PersistentService_Project_SimultaneousWrites()
        {
            return base.PersistentService_Project_SimultaneousWrites();
        }

        [Fact]
        public new Task PersistentService_Document_SimultaneousWrites()
        {
            return base.PersistentService_Document_SimultaneousWrites();
        }

        [Fact]
        public new Task PersistentService_Solution_SimultaneousReads()
        {
            return base.PersistentService_Solution_SimultaneousReads();
        }

        [Fact]
        public new Task PersistentService_Project_SimultaneousReads()
        {
            return base.PersistentService_Project_SimultaneousReads();
        }

        [Fact]
        public new Task PersistentService_Document_SimultaneousReads()
        {
            return base.PersistentService_Document_SimultaneousReads();
        }
    }
}