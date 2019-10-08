// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal delegate TRet FuncInOutOut<T1, T2, T3, TRet>(T1 guid, out T2 errorCode, out T3 localizedMessage);
}
