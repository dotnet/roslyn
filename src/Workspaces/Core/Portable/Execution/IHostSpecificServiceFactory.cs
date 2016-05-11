// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// A factory that creates instances of a specific <see cref="IHostSpecificService"/>.
    /// 
    /// Implement a <see cref="IHostSpecificServiceFactory"/> when you want to provide <see cref="IHostSpecificService"/> instances that use other services.
    /// </summary>
    internal interface IHostSpecificServiceFactory
    {
        /// <summary>
        /// Creates a new <see cref="IHostSpecificService"/> instance.
        /// </summary>
        /// <param name="hostSpecificServices">The <see cref="HostSpecificServices"/> that can be used to access other services.</param>
        IHostSpecificService CreateService(HostSpecificServices hostSpecificServices);
    }
}
