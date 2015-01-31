// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public abstract class AbstractTable
        {
            public abstract void Create(JET_SESID sessionId, JET_DBID databaseId);
            public abstract void Initialize(JET_SESID sessionId, JET_DBID databaseId);

            public abstract AbstractTableAccessor GetTableAccessor(OpenSession openSession);
        }
    }
}
