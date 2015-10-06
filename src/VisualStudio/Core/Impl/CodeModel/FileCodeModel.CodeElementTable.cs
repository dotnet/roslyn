// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    using CodeElementWeakComAggregateHandle = WeakComHandle<EnvDTE.CodeElement, EnvDTE.CodeElement>;

    public sealed partial class FileCodeModel
    {
        private class CodeElementTable : IEnumerable<EnvDTE.CodeElement>
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
                Debug.Assert(!_elementWeakComHandles.ContainsKey(key), "All right, we got it wrong. We should have a free entry in the table!");

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
