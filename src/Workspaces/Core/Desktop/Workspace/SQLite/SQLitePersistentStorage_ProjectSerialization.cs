// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        public override Task<Stream> ReadStreamAsync(
            Project project, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProjectData projectData = null;
            if (!_shutdownTokenSource.IsCancellationRequested)
            {
                var connection = CreateConnection();
                if (TryGetProjectDataId(project, name, out var dataId, connection))
                {
                    // Ensure all pending project writes to this name are flushed to the DB so that 
                    // we can find them below.
                    FlushPendingProjectWrites(connection, project.Id, name);

                    try
                    {
                        projectData = connection.Find<ProjectData>(dataId);
                    }
                    catch (Exception ex)
                    {
                        StorageDatabaseLogger.LogException(ex);
                    }
                }
            }

            return Task.FromResult(GetStream(projectData?.Data));
        }

        public override Task<bool> WriteStreamAsync(
            Project project, string name, Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_shutdownTokenSource.IsCancellationRequested &&
                TryGetProjectDataId(project, name, out var dataId, connectionOpt: null))
            {
                var bytes = GetBytes(stream);

                AddProjectWriteTask(project.Id, name, con =>
                {
                    con.InsertOrReplace(
                        new ProjectData { Id = dataId, Data = bytes });
                });

                return SpecializedTasks.True;
            }

            return SpecializedTasks.False;
        }
    }
}