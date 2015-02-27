// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportOptionProvider, Shared]
    internal class PersistentStorageOptionsProvider : IOptionProvider
    {
        public IEnumerable<IOption> GetOptions()
        {
            return SpecializedCollections.SingletonEnumerable(PersistentStorageOptions.Enabled);
        }
    }
}
