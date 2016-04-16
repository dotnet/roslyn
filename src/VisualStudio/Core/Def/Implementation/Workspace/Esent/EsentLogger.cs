// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal class EsentLogger
    {
        private const string Kind = nameof(Kind);
        private const string Reason = nameof(Reason);

        private static readonly ConcurrentDictionary<Type, object> s_set = new ConcurrentDictionary<Type, object>(concurrencyLevel: 2, capacity: 10);

        internal static void LogException(Exception ex)
        {
            // we already reported about this exception. also don't hold onto too many exceptions.
            if (s_set.Count > 10 || !s_set.TryAdd(ex.GetType(), null))
            {
                return;
            }

            Logger.Log(FunctionId.Esent_Exceptions, KeyValueLogMessage.Create(m =>
            {
                // this is okay since it is our exception
                m[Kind] = ex.GetType().ToString();
                m[Reason] = ex.ToString();
            }));
        }
    }
}
