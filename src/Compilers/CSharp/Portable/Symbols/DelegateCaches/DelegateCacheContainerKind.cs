// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// To avoid too many container classes and run-time specialization of containers, containers are concrete and can be shared when possible.
    /// </summary>
    internal enum DelegateCacheContainerKind
    {
        /// <summary>
        /// This kind of containers are used when ALL of the following conditions are satisfied:
        ///   1. The delegate type and its containing types all the way up are fully concrete;
        ///   2. The type arguments of the target method and its containing types all the way up are fully concrete.
        /// A container of this kind can be shared by conversions within the same module, as long as they have the same fully contructed delegate type.
        /// We group module scoped containers by delegate type to:
        ///   1. It might change the time when types are loaded. That could actually break apps, who depend on not loading a type (e.g. when implementing a light-up). - @tmat
        ///   2. Avoid potential massive loading of types used in various delegates once we touch the cache. - @VSadov
        /// </summary>
        ModuleScopedConcrete,

        /// <summary>
        /// This kind of containers are used when ALL of the following conditions are satisfied:
        ///   1. Fails to qualify for a <see cref="ModuleScopedConcrete"/> container;
        ///   2. Neither of the delegate type nor the target method use any of the type parameters from the top level method.
        /// A container of this kind can be shared by conversions within the same enclosing type.
        /// Delegates are not grouped in type level because currently static lambda frame don't do this, and people don't do grouping where they cache manually.
        /// </summary>
        TypeScopedConcrete,

        /// <summary>
        /// This kind of containers are used when a conversion fails to qualify for a concrete container above.
        /// They're specialized at run-time. Type parameters are generated for this kind of container base on the type parameters of the top level method.
        /// A container of this kind can be shared by conversions within the same top level method.
        /// </summary>
        MethodScopedGeneric
    }
}
