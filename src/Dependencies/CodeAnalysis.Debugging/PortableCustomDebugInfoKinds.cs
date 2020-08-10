// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal static class PortableCustomDebugInfoKinds
    {
        public static readonly Guid AsyncMethodSteppingInformationBlob = new Guid("54FD2AC5-E925-401A-9C2A-F94F171072F8");
        public static readonly Guid StateMachineHoistedLocalScopes = new Guid("6DA9A61E-F8C7-4874-BE62-68BC5630DF71");
        public static readonly Guid DynamicLocalVariables = new Guid("83C563C4-B4F3-47D5-B824-BA5441477EA8");
        public static readonly Guid TupleElementNames = new Guid("ED9FDF71-8879-4747-8ED3-FE5EDE3CE710");
        public static readonly Guid DefaultNamespace = new Guid("58b2eab6-209f-4e4e-a22c-b2d0f910c782");
        public static readonly Guid EncLocalSlotMap = new Guid("755F52A8-91C5-45BE-B4B8-209571E552BD");
        public static readonly Guid EncLambdaAndClosureMap = new Guid("A643004C-0240-496F-A783-30D64F4979DE");
        public static readonly Guid SourceLink = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");
        public static readonly Guid EmbeddedSource = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
        public static readonly Guid CompilationMetadataReferences = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");
        public static readonly Guid CompilationOptions = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");
    }
}
