// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;

public abstract class ApartmentSensitiveComObject
{
    /// <summary>
    /// Creates a ComHandle for this object.
    /// </summary>
    /// <typeparam name="THandle">The interface type of the handle.</typeparam>
    /// <typeparam name="TObject">The type of the derived object.</typeparam>
    /// <returns>A ComHandle referencing both the object and the wrapped interface form of the object.</returns>
    internal ComHandle<THandle, TObject> GetComHandle<THandle, TObject>()
        where THandle : class
        where TObject : ApartmentSensitiveComObject, THandle
    {
        return new ComHandle<THandle, TObject>((THandle)ComAggregate.CreateAggregatedObject((TObject)this), (TObject)this);
    }
}
