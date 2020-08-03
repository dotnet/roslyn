// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace BuildValidator
{
    internal interface ISourceResolver
    {
        public Task<SourceText> ResolveSourceAsync(string name, Encoding encoding);
    }
}
