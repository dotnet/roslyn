// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
