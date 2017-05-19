// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SQLite;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    /// <remarks>
    /// Tests are inherited from <see cref="AbstractPersistentStorageTests"/>.  That way we can
    /// write tests once and have them run against all <see cref="IPersistentStorageService"/>
    /// implementations.
    /// </remarks>
    public class SQLitePersistentStorageTests : AbstractPersistentStorageTests
    {
        protected override IPersistentStorageService GetStorageService()
            => new SQLitePersistentStorageService(_persistentEnabledOptionService, testing: true);

        [Fact]
        public async Task TestNullFilePaths()
        {
            var solution = CreateOrOpenSolution(nullPaths: true);

            var streamName = "stream";

            using (var storage = GetStorage(solution))
            {
                var project = solution.Projects.First();
                var document = project.Documents.First();
                Assert.False(await storage.WriteStreamAsync(project, streamName, EncodeString("")));
                Assert.False(await storage.WriteStreamAsync(document, streamName, EncodeString("")));

                Assert.Null(await storage.ReadStreamAsync(project, streamName));
                Assert.Null(await storage.ReadStreamAsync(document, streamName));
            }
        }
    }
}