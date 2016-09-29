// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    public sealed partial class FileCodeModel
    {
        private const int ElementAddedDispId = 1;
        private const int ElementChangedDispId = 2;
        private const int ElementDeletedDispId = 3;
        private const int ElementDeletedDispId2 = 4;

        public bool FireEvents()
        {
            var needMoreTime = false;

            _codeElementTable.CleanUpDeadObjects();
            needMoreTime = _codeElementTable.NeedsCleanUp;

            if (this.IsZombied)
            {
                // file is removed from the solution. this can happen if a fireevent is enqueued to foreground notification service
                // but the file itself is removed from the solution before it has a chance to run
                return needMoreTime;
            }

            Document document;
            if (!TryGetDocument(out document))
            {
                // file is removed from the solution. this can happen if a fireevent is enqueued to foreground notification service
                // but the file itself is removed from the solution before it has a chance to run
                return needMoreTime;
            }

            // TODO(DustinCa): Enqueue unknown change event if a file is closed without being saved.
            var oldTree = _lastSyntaxTree;
            var newTree = document
                .GetSyntaxTreeAsync(CancellationToken.None)
                .WaitAndGetResult_CodeModel(CancellationToken.None);

            _lastSyntaxTree = newTree;

            if (oldTree == newTree ||
                oldTree.IsEquivalentTo(newTree, topLevel: true))
            {
                return needMoreTime;
            }

            var eventQueue = this.CodeModelService.CollectCodeModelEvents(oldTree, newTree);
            if (eventQueue.Count == 0)
            {
                return needMoreTime;
            }

            var provider = GetAbstractProject() as IProjectCodeModelProvider;
            if (provider == null)
            {
                return needMoreTime;
            }

            ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> fileCodeModelHandle;
            if (!provider.ProjectCodeModel.TryGetCachedFileCodeModel(this.Workspace.GetFilePath(GetDocumentId()), out fileCodeModelHandle))
            {
                return needMoreTime;
            }

            var extensibility = (EnvDTE80.IVsExtensibility2)this.State.ServiceProvider.GetService(typeof(EnvDTE.IVsExtensibility));

            foreach (var codeModelEvent in eventQueue)
            {
                EnvDTE.CodeElement element;
                object parentElement;
                GetElementsForCodeModelEvent(codeModelEvent, out element, out parentElement);

                if (codeModelEvent.Type == CodeModelEventType.Add)
                {
                    extensibility.FireCodeModelEvent(ElementAddedDispId, element, EnvDTE80.vsCMChangeKind.vsCMChangeKindUnknown);
                }
                else if (codeModelEvent.Type == CodeModelEventType.Remove)
                {
                    extensibility.FireCodeModelEvent3(ElementDeletedDispId2, parentElement, element, EnvDTE80.vsCMChangeKind.vsCMChangeKindUnknown);
                    extensibility.FireCodeModelEvent(ElementDeletedDispId, element, EnvDTE80.vsCMChangeKind.vsCMChangeKindUnknown);
                }
                else if (codeModelEvent.Type.IsChange())
                {
                    extensibility.FireCodeModelEvent(ElementChangedDispId, element, ConvertToChangeKind(codeModelEvent.Type));
                }
                else
                {
                    Debug.Fail("Invalid event type: " + codeModelEvent.Type);
                }
            }

            return needMoreTime;
        }

        private EnvDTE80.vsCMChangeKind ConvertToChangeKind(CodeModelEventType eventType)
        {
            EnvDTE80.vsCMChangeKind result = 0;

            if ((eventType & CodeModelEventType.Rename) != 0)
            {
                result |= EnvDTE80.vsCMChangeKind.vsCMChangeKindRename;
            }

            if ((eventType & CodeModelEventType.Unknown) != 0)
            {
                result |= EnvDTE80.vsCMChangeKind.vsCMChangeKindUnknown;
            }

            if ((eventType & CodeModelEventType.BaseChange) != 0)
            {
                result |= EnvDTE80.vsCMChangeKind.vsCMChangeKindBaseChange;
            }

            if ((eventType & CodeModelEventType.TypeRefChange) != 0)
            {
                result |= EnvDTE80.vsCMChangeKind.vsCMChangeKindTypeRefChange;
            }

            if ((eventType & CodeModelEventType.SigChange) != 0)
            {
                result |= EnvDTE80.vsCMChangeKind.vsCMChangeKindSignatureChange;
            }

            if ((eventType & CodeModelEventType.ArgChange) != 0)
            {
                result |= EnvDTE80.vsCMChangeKind.vsCMChangeKindArgumentChange;
            }

            return result;
        }

        // internal for testing
        internal void GetElementsForCodeModelEvent(CodeModelEvent codeModelEvent, out EnvDTE.CodeElement element, out object parentElement)
        {
            parentElement = GetParentElementForCodeModelEvent(codeModelEvent);

            if (codeModelEvent.Node == null)
            {
                element = this.CodeModelService.CreateUnknownRootNamespaceCodeElement(this.State, this);
            }
            else if (this.CodeModelService.IsParameterNode(codeModelEvent.Node))
            {
                element = GetParameterElementForCodeModelEvent(codeModelEvent, parentElement);
            }
            else if (this.CodeModelService.IsAttributeNode(codeModelEvent.Node))
            {
                element = GetAttributeElementForCodeModelEvent(codeModelEvent, parentElement);
            }
            else if (this.CodeModelService.IsAttributeArgumentNode(codeModelEvent.Node))
            {
                element = GetAttributeArgumentElementForCodeModelEvent(codeModelEvent, parentElement);
            }
            else
            {
                if (codeModelEvent.Type == CodeModelEventType.Remove)
                {
                    element = this.CodeModelService.CreateUnknownCodeElement(this.State, this, codeModelEvent.Node);
                }
                else
                {
                    element = this.GetOrCreateCodeElement<EnvDTE.CodeElement>(codeModelEvent.Node);
                }
            }

            if (element == null)
            {
                Debug.Fail("We should have created an element for this event!");
            }

            Debug.Assert(codeModelEvent.Type != CodeModelEventType.Remove || parentElement != null);
        }

        private object GetParentElementForCodeModelEvent(CodeModelEvent codeModelEvent)
        {
            if (this.CodeModelService.IsParameterNode(codeModelEvent.Node) ||
                this.CodeModelService.IsAttributeArgumentNode(codeModelEvent.Node))
            {
                if (codeModelEvent.ParentNode != null)
                {
                    return this.GetOrCreateCodeElement<EnvDTE.CodeElement>(codeModelEvent.ParentNode);
                }
            }
            else if (this.CodeModelService.IsAttributeNode(codeModelEvent.Node))
            {
                if (codeModelEvent.ParentNode != null)
                {
                    return this.GetOrCreateCodeElement<EnvDTE.CodeElement>(codeModelEvent.ParentNode);
                }
                else
                {
                    return this;
                }
            }
            else if (codeModelEvent.Type == CodeModelEventType.Remove)
            {
                if (codeModelEvent.ParentNode != null &&
                    codeModelEvent.ParentNode.Parent != null)
                {
                    return this.GetOrCreateCodeElement<EnvDTE.CodeElement>(codeModelEvent.ParentNode);
                }
                else
                {
                    return this;
                }
            }

            return null;
        }

        private EnvDTE.CodeElement GetParameterElementForCodeModelEvent(CodeModelEvent codeModelEvent, object parentElement)
        {
            var parentDelegate = parentElement as EnvDTE.CodeDelegate;
            if (parentDelegate != null)
            {
                return GetParameterElementForCodeModelEvent(codeModelEvent, parentDelegate.Parameters, parentElement);
            }

            var parentFunction = parentElement as EnvDTE.CodeFunction;
            if (parentFunction != null)
            {
                return GetParameterElementForCodeModelEvent(codeModelEvent, parentFunction.Parameters, parentElement);
            }

            var parentProperty = parentElement as EnvDTE80.CodeProperty2;
            if (parentProperty != null)
            {
                return GetParameterElementForCodeModelEvent(codeModelEvent, parentProperty.Parameters, parentElement);
            }

            return null;
        }

        private EnvDTE.CodeElement GetParameterElementForCodeModelEvent(CodeModelEvent codeModelEvent, EnvDTE.CodeElements parentParameters, object parentElement)
        {
            if (parentParameters == null)
            {
                return null;
            }

            var parameterName = this.CodeModelService.GetName(codeModelEvent.Node);

            if (codeModelEvent.Type == CodeModelEventType.Remove)
            {
                var parentCodeElement = ComAggregate.TryGetManagedObject<AbstractCodeMember>(parentElement);
                if (parentCodeElement != null)
                {
                    return (EnvDTE.CodeElement)CodeParameter.Create(this.State, parentCodeElement, parameterName);
                }
            }
            else
            {
                return parentParameters.Item(parameterName);
            }

            return null;
        }

        private EnvDTE.CodeElement GetAttributeElementForCodeModelEvent(CodeModelEvent codeModelEvent, object parentElement)
        {
            var node = codeModelEvent.Node;
            var parentNode = codeModelEvent.ParentNode;
            var eventType = codeModelEvent.Type;

            var parentType = parentElement as EnvDTE.CodeType;
            if (parentType != null)
            {
                return GetAttributeElementForCodeModelEvent(node, parentNode, eventType, parentType.Attributes, parentElement);
            }

            var parentFunction = parentElement as EnvDTE.CodeFunction;
            if (parentFunction != null)
            {
                return GetAttributeElementForCodeModelEvent(node, parentNode, eventType, parentFunction.Attributes, parentElement);
            }

            var parentProperty = parentElement as EnvDTE.CodeProperty;
            if (parentProperty != null)
            {
                return GetAttributeElementForCodeModelEvent(node, parentNode, eventType, parentProperty.Attributes, parentElement);
            }

            var parentEvent = parentElement as EnvDTE80.CodeEvent;
            if (parentEvent != null)
            {
                return GetAttributeElementForCodeModelEvent(node, parentNode, eventType, parentEvent.Attributes, parentElement);
            }

            var parentVariable = parentElement as EnvDTE.CodeVariable;
            if (parentVariable != null)
            {
                return GetAttributeElementForCodeModelEvent(node, parentNode, eventType, parentVariable.Attributes, parentElement);
            }

            // In the following case, parentNode is null and the root should be used instead.
            var parentFileCodeModel = parentElement as EnvDTE.FileCodeModel;
            if (parentFileCodeModel != null)
            {
                var fileCodeModel = ComAggregate.TryGetManagedObject<FileCodeModel>(parentElement);
                parentNode = fileCodeModel.GetSyntaxRoot();

                return GetAttributeElementForCodeModelEvent(node, parentNode, eventType, parentFileCodeModel.CodeElements, parentElement);
            }

            return null;
        }

        private EnvDTE.CodeElement GetAttributeElementForCodeModelEvent(SyntaxNode node, SyntaxNode parentNode, CodeModelEventType eventType, EnvDTE.CodeElements elementsToSearch, object parentObject)
        {
            if (elementsToSearch == null)
            {
                return null;
            }

            string name;
            int ordinal;
            CodeModelService.GetAttributeNameAndOrdinal(parentNode, node, out name, out ordinal);

            if (eventType == CodeModelEventType.Remove)
            {
                if (parentObject is EnvDTE.CodeElement)
                {
                    var parentCodeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(parentObject);
                    if (parentCodeElement != null)
                    {
                        return (EnvDTE.CodeElement)CodeAttribute.Create(this.State, this, parentCodeElement, name, ordinal);
                    }
                }
                else if (parentObject is EnvDTE.FileCodeModel)
                {
                    var parentFileCodeModel = ComAggregate.TryGetManagedObject<FileCodeModel>(parentObject);
                    if (parentFileCodeModel != null && parentFileCodeModel == this)
                    {
                        return (EnvDTE.CodeElement)CodeAttribute.Create(this.State, this, null, name, ordinal);
                    }
                }
            }
            else
            {
                int testOrdinal = 0;
                foreach (EnvDTE.CodeElement element in elementsToSearch)
                {
                    if (element.Kind != EnvDTE.vsCMElement.vsCMElementAttribute)
                    {
                        continue;
                    }

                    if (element.Name == name)
                    {
                        if (ordinal == testOrdinal)
                        {
                            return element;
                        }

                        testOrdinal++;
                    }
                }
            }

            return null;
        }

        private EnvDTE.CodeElement GetAttributeArgumentElementForCodeModelEvent(CodeModelEvent codeModelEvent, object parentElement)
        {
            var parentAttribute = parentElement as EnvDTE80.CodeAttribute2;
            if (parentAttribute != null)
            {
                return GetAttributeArgumentForCodeModelEvent(codeModelEvent, parentAttribute.Arguments, parentElement);
            }

            return null;
        }

        private EnvDTE.CodeElement GetAttributeArgumentForCodeModelEvent(CodeModelEvent codeModelEvent, EnvDTE.CodeElements parentAttributeArguments, object parentElement)
        {
            if (parentAttributeArguments == null)
            {
                return null;
            }

            SyntaxNode attributeNode;
            int ordinal;
            CodeModelService.GetAttributeArgumentParentAndIndex(codeModelEvent.Node, out attributeNode, out ordinal);

            if (codeModelEvent.Type == CodeModelEventType.Remove)
            {
                var parentCodeElement = ComAggregate.TryGetManagedObject<CodeAttribute>(parentElement);
                if (parentCodeElement != null)
                {
                    return (EnvDTE.CodeElement)CodeAttributeArgument.Create(this.State, parentCodeElement, ordinal);
                }
            }
            else
            {
                return parentAttributeArguments.Item(ordinal + 1); // Needs to be 1-based to call back into code model
            }

            return null;
        }
    }
}
