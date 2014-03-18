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
    /// logger interface actual logger should implements
    /// </summary>
    internal interface ILogger
    {
        /// <summary>
        /// answer whether it is enabled or not for the specific feature and function id
        /// </summary>
        bool IsEnabled(FeatureId featureId, FunctionId functionId);

        /// <summary>
        /// answer whether it is in verbose mode or not
        /// </summary>
        bool IsVerbose();

        /// <summary>
        /// log a specific event with context message
        /// </summary>
        void Log(FeatureId featureId, FunctionId functionId, string message);

        /// <summary>
        /// log a start and end pair with context message
        /// </summary>
        IDisposable LogBlock(FeatureId featureId, FunctionId functionId, string message, int uniquePairId, CancellationToken cancellationToken);
    }
}
