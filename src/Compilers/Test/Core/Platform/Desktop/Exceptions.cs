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

        protected RuntimePeVerifyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Output = info.GetString(nameof(Output));
        }

        public RuntimePeVerifyException(string output)
            : base(output)
        {
            Output = output;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Output), Output);
        }
    }
}
#endif
