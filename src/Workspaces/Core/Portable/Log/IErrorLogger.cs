﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ErrorLogger
{
    internal interface IErrorLoggerService : IWorkspaceService
    {
        void LogException(object source, Exception exception);
        bool TryLogException(object source, Exception exception);
    }
}
