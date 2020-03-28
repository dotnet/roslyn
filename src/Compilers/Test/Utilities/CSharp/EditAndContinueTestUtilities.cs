﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public static class EditAndContinueTestUtilities
    {
        #region EditAndContinueMethodDebugInformation inspection

        // Helpers that allow tests in IDE layers to inspect internals of EditAndContinueMethodDebugInformation without having IVT to the compiler.

        public static int GetMethodOrdinal(this EditAndContinueMethodDebugInformation debugInfo)
            => debugInfo.MethodOrdinal;

        public static IEnumerable<string> InspectLocalSlots(this EditAndContinueMethodDebugInformation debugInfo)
            => debugInfo.LocalSlots.IsDefault ? null :
               debugInfo.LocalSlots.Select(s => $"Offset={s.Id.SyntaxOffset} Ordinal={s.Id.Ordinal} Kind={s.SynthesizedKind}");

        public static IEnumerable<string> InspectLambdas(this EditAndContinueMethodDebugInformation debugInfo)
            => debugInfo.Lambdas.IsDefault ? null :
               debugInfo.Lambdas.Select(l => $"Offset={l.SyntaxOffset} Id={l.LambdaId.Generation}#{l.LambdaId.Ordinal} Closure={l.ClosureOrdinal}");

        public static IEnumerable<string> InspectClosures(this EditAndContinueMethodDebugInformation debugInfo)
            => debugInfo.Closures.IsDefault ? null :
               debugInfo.Closures.Select(c => $"Offset={c.SyntaxOffset} Id={c.ClosureId.Generation}#{c.ClosureId.Ordinal}");

        public static EmitBaseline GetInitialEmitBaseline(this EmitBaseline baseline)
            => baseline.InitialBaseline;

        #endregion
    }
}
