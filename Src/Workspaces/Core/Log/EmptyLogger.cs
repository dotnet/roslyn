// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// a logger that doesn't do anything
    /// </summary>
    internal sealed class EmptyLogger : ILogger
    {
        public static readonly EmptyLogger Instance = new EmptyLogger();

        public bool IsEnabled(FeatureId featureId, FunctionId functionId)
        {
            return false;
        }

        public bool IsVerbose()
        {
            return false;
        }

        public void Log(FeatureId featureId, FunctionId functionId, string message)
        {
        }

        public IDisposable LogBlock(FeatureId featureId, FunctionId functionId, string message, int uniquePairId, CancellationToken cancellationToken)
        {
            return EmptyLogBlock.Instance;
        }
    }
}
