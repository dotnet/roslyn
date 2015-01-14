// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Test.MetadataUtilities
{
    public sealed class ILVisualizerAsTokens : ILVisualizer
    {
        public static readonly ILVisualizerAsTokens Instance = new ILVisualizerAsTokens();

        public override string VisualizeUserString(uint token)
        {
            return string.Format("0x{0:X8}", token);
        }

        public override string VisualizeSymbol(uint token)
        {
            return string.Format("0x{0:X8}", token);
        }

        public override string VisualizeLocalType(object type)
        {
            return string.Format("0x{0:X8}", type); // Should be a token.
        }
    }
}
