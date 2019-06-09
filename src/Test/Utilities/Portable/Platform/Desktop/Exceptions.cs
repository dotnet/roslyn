// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if NET472
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using static Roslyn.Test.Utilities.ExceptionHelper;

namespace Roslyn.Test.Utilities.Desktop
{
    [Serializable]
    public class RuntimePeVerifyException : Exception
    {
        public string Output { get; }
        public string ExePath { get; }

        protected RuntimePeVerifyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Output = info.GetString(nameof(Output));
            ExePath = info.GetString(nameof(ExePath));
        }

        public RuntimePeVerifyException(string output, string exePath)
            : base(GetMessageFromResult(output, exePath))
        {
            Output = output;
            ExePath = exePath;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Output), Output);
            info.AddValue(nameof(ExePath), ExePath);
        }
    }
}
#endif
