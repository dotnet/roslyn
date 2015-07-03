// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using CodeElementWeakComAggregateHandle =
    Microsoft.VisualStudio.LanguageServices.Implementation.Interop.WeakComHandle<EnvDTE.CodeElement, EnvDTE.CodeElement>;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    public sealed partial class FileCodeModel
    {
        private class ElementTable : IEnumerable<EnvDTE.CodeElement>
        {
            // Since CodeElements have a strong ref on the FileCodeModel, we should use
            // weak references on the CodeElements to prevent a cycle. This class should
            // keep weak refs to the CodeElements so that we get a graph like this:
            //  => - Strong reference           -> - Weak reference
            //
            //      FileCodeModel =>
            //          ElementTable->     // we prevent a cycle with a native object in the mix by using a weak ref here.
            //              RCW =>
            //                  NativeComAggregate =>
            //                      CCW =>
            //                          CodeElement =>
            //                              FileCodeModel
            //
            // See Dev10 Bug 785889 and 799848.

            private readonly CleanableWeakComHandleTable _elementWeakComHandles = new CleanableWeakComHandleTable();

            public void Add(SyntaxNodeKey key, EnvDTE.CodeElement element)
            {
                // This code deals with a weird case of interaction with WinForms: The same
                // element can be added multiple times. What we have to do is bump
                // the conflicting element to a free [KeyName, Ordinal] slot
                // by incrementing [Ordinal]
                // Elements with the same node path can also be added multiple times in 
                // normal editing scenarios, e.g. when a code element is changing preserving its name
                // (e.g. a class becomes an interface) or user just removes a block of code and 
                // then readds it back before code elements for removed code were garbage collected.
                CodeElementWeakComAggregateHandle existingElementHandle;
                if (_elementWeakComHandles.TryGetValue(key, out existingElementHandle))
                {
                    int newOrdinal = key.Ordinal;
                    while (true)
                    {
                        newOrdinal++;
                        var currentKey = new SyntaxNodeKey(key.Name, newOrdinal);
                        if (!_elementWeakComHandles.ContainsKey(currentKey))
                        {
                            // We found a free "slot": use it and release the previous one
                            AbstractKeyedCodeElement existingElement = null;
                            EnvDTE.CodeElement existingElementManagedObject;
                            if (existingElementHandle.TryGetManagedObjectWithoutCaringWhetherNativeObjectIsAlive(out existingElementManagedObject))
                            {
                                existingElement = existingElementManagedObject as AbstractKeyedCodeElement;
                            }

                            _elementWeakComHandles.Remove(key);

                            if (existingElementHandle.ComAggregateObject == null)
                            {
                                // The native object has already been released.
                                // There's no need to re-add this handle.
                                break;
                            }

                            Debug.Assert(existingElement != null, "The ComAggregate is alive. Why isn't the actual managed object?");

                            _elementWeakComHandles.Add(currentKey, existingElementHandle);
                            existingElement.NodeKey = currentKey;
                            break;
                        }
                    }
                }

                Debug.Assert(!_elementWeakComHandles.ContainsKey(key),
                             "All right, we got it wrong. We should have a free entry in the table!");

                _elementWeakComHandles.Add(key, new CodeElementWeakComAggregateHandle(element));
            }

            public void Remove(SyntaxNodeKey nodeKey, out EnvDTE.CodeElement removedElement)
            {
                var elementHandleInTable = RemoveImpl(nodeKey);
                removedElement = elementHandleInTable.ComAggregateObject;
            }

            public void Remove(SyntaxNodeKey nodeKey)
            {
                RemoveImpl(nodeKey);
            }

            private CodeElementWeakComAggregateHandle RemoveImpl(SyntaxNodeKey nodeKey)
            {
                CodeElementWeakComAggregateHandle elementHandleInTable;
                if (!_elementWeakComHandles.TryGetValue(nodeKey, out elementHandleInTable))
                {
                    Debug.Fail("Can't find code element being removed");
                    throw new InvalidOperationException();
                }

                _elementWeakComHandles.Remove(nodeKey);
                return elementHandleInTable;
            }

            public EnvDTE.CodeElement TryGetValue(SyntaxNodeKey nodeKey)
            {
                CodeElementWeakComAggregateHandle resultWeakComHandle;
                if (_elementWeakComHandles.TryGetValue(nodeKey, out resultWeakComHandle))
                {
                    return resultWeakComHandle.ComAggregateObject;
                }

                return null;
            }

            private IEnumerator<EnvDTE.CodeElement> GetEnumeratorForElementsThatAreAlive()
            {
                // We only want to iterate over the comHandles that are still alive.
                var handlesThatAreAlive = new List<EnvDTE.CodeElement>();
                foreach (var handle in _elementWeakComHandles.Values)
                {
                    var element = handle.ComAggregateObject;
                    if (element == null)
                    {
                        // The ComAggregateObject for this element has been released.
                        continue;
                    }

                    handlesThatAreAlive.Add(element);
                }

                return handlesThatAreAlive.GetEnumerator();
            }

            public IEnumerator<EnvDTE.CodeElement> GetEnumerator()
            {
                return GetEnumeratorForElementsThatAreAlive();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumeratorForElementsThatAreAlive();
            }

            public void Cleanup(out bool needMoreTime)
            {
                _elementWeakComHandles.CleanupWeakComHandles();
                needMoreTime = _elementWeakComHandles.NeedCleanup;
            }
        }
    }
}
