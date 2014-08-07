// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// LogMessage that creates key value map lazily
    /// </summary>
    internal sealed class KeyValueLogMessage : LogMessage
    {
        private static readonly ObjectPool<KeyValueLogMessage> Pool = SharedPools.Default<KeyValueLogMessage>();

        public static LogMessage Create(Action<Dictionary<string, string>> propertySetter)
        {
            var logMessage = Pool.Allocate();
            logMessage.Constrcut(propertySetter);

            return logMessage;
        }

        private Dictionary<string, string> map;

        private void Constrcut(Action<Dictionary<string, string>> propertySetter)
        {
            this.map = SharedPools.Default<Dictionary<string, string>>().AllocateAndClear();
            propertySetter(map);
        }

        public bool ContainsProperty
        {
            get { return this.map.Count > 0; }
        }

        public IEnumerable<KeyValuePair<string, string>> Properties
        {
            get { return this.map; }
        }

        protected override string CreateMessage()
        {
            return string.Join("|", map.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
        }

        public override void Dispose()
        {
            SharedPools.Default<Dictionary<string, string>>().ClearAndFree(this.map);
            this.map = null;

            Pool.Free(this);
        }
    }
}
