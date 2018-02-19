// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Enumeration for possible kinds of method symbols.
    /// </summary>
    public enum MethodKind
    {
        /// <summary>
        /// An anonymous method or lambda expression
        /// </summary>
        AnonymousFunction = 0,
        LambdaMethod = 0,  // VB term

        /// <summary>
        /// Method is a constructor.
        /// </summary>
        Constructor = 1,

        /// <summary>
        /// Method is a conversion.
        /// </summary>
        Conversion = 2,

        /// <summary>
        /// Method is a delegate invoke.
        /// </summary>
        DelegateInvoke = 3,

        /// <summary>
        /// Method is a destructor.
        /// </summary>
        Destructor = 4,

        /// <summary>
        /// Method is an event add.
        /// </summary>
        EventAdd = 5,

        /// <summary>
        /// Method is an event raise.
        /// </summary>
        EventRaise = 6,

        /// <summary>
        /// Method is an event remove.
        /// </summary>
        EventRemove = 7,

        /// <summary>
        /// Method is an explicit interface implementation.
        /// </summary>
        ExplicitInterfaceImplementation = 8,

        /// <summary>
        /// Method is an operator.
        /// </summary>
        UserDefinedOperator = 9,

        /// <summary>
        /// Method is an ordinary method.
        /// </summary>
        Ordinary = 10,

        /// <summary>
        /// Method is a property get.
        /// </summary>
        PropertyGet = 11,

        /// <summary>
        /// Method is a property set.
        /// </summary>
        PropertySet = 12,

        /// <summary>
        /// An extension method with the "this" parameter removed.
        /// </summary>
        ReducedExtension = 13,

        /// <summary>
        /// Method is a static constructor.
        /// </summary>
        StaticConstructor = 14,
        SharedConstructor = 14, // VB Term

        /// <summary>
        /// A built-in operator.
        /// </summary>
        BuiltinOperator = 15,

        /// <summary>
        /// Declare Sub or Function.
        /// </summary>
        DeclareMethod = 16,

        /// <summary>
        /// Method is declared inside of another method.
        /// </summary>
        LocalFunction = 17
    }
}
