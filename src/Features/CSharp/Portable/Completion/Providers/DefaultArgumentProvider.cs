// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportArgumentProvider(nameof(DefaultArgumentProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(OutVariableArgumentProvider))]
[Shared]
internal sealed class DefaultArgumentProvider : AbstractDefaultArgumentProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultArgumentProvider()
    {
    }

    public override Task ProvideArgumentAsync(ArgumentContext context)
    {
        if (context.PreviousValue is { })
        {
            context.DefaultValue = context.PreviousValue;
        }
        else if (context.Parameter.Type.IsReferenceType || context.Parameter.Type.IsNullable())
        {
            context.DefaultValue = "null";
        }
        else
        {
            context.DefaultValue = context.Parameter.Type.SpecialType switch
            {
                SpecialType.System_Boolean => "false",
                SpecialType.System_Char => @"'\\0'",
                SpecialType.System_Byte => "(byte)0",
                SpecialType.System_SByte => "(sbyte)0",
                SpecialType.System_Int16 => "(short)0",
                SpecialType.System_UInt16 => "(ushort)0",
                SpecialType.System_Int32 => "0",
                SpecialType.System_UInt32 => "0U",
                SpecialType.System_Int64 => "0L",
                SpecialType.System_UInt64 => "0UL",
                SpecialType.System_Decimal => "0.0m",
                SpecialType.System_Single => "0.0f",
                SpecialType.System_Double => "0.0",
                _ => "default",
            };
        }

        return Task.CompletedTask;
    }
}
