// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace BoundTreeGenerator
{
    [XmlRoot]
    public class Tree
    {
        [XmlAttribute]
        public string Root;

        [XmlElement(ElementName = "Node", Type = typeof(Node))]
        [XmlElement(ElementName = "AbstractNode", Type = typeof(AbstractNode))]
        [XmlElement(ElementName = "PredefinedNode", Type = typeof(PredefinedNode))]
        [XmlElement(ElementName = "Enum", Type = typeof(EnumType))]
        [XmlElement(ElementName = "ValueType", Type = typeof(ValueType))]
        public List<TreeType> Types;
    }

    interface ICommentedNode
    {
        ref List<CommentNode> GetCommentListField();

        string Comment
        {
            get
            {
                ref var comments = ref GetCommentListField();
                if (comments == null)
                {
                    return null;
                }
                return string.Join(Environment.NewLine, comments.Select(c => c.Summary));
            }
        }

        void AddComment(string value)
        {
            if (value is null or { Length: 0 })
            {
                return;
            }
            ref var comments = ref GetCommentListField();
            comments ??= new();
            comments.Add(new()
            {
                Summary = value
            });
        }
    }

    public sealed class CommentNode
    {
        [XmlElement("summary")]
        public string Summary;
    }

    public abstract class TreeType : ICommentedNode
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Base;

        [XmlAttribute]
        public string HasValidate;

        [XmlElement(ElementName = "TypeComment", Type = typeof(CommentNode))]
        public List<CommentNode> Comments;

        ref List<CommentNode> ICommentedNode.GetCommentListField()
        {
            return ref Comments;
        }
    }

    public sealed class PredefinedNode : TreeType
    {
    }

    public sealed class ValueType : TreeType
    {
    }

    public class AbstractNode : TreeType
    {
        [XmlElement(ElementName = "Field", Type = typeof(Field))]
        public List<Field> Fields;
    }

    public sealed class Node : AbstractNode
    {
        [XmlAttribute]
        public string Root;

        [XmlAttribute]
        public string Errors;

        /// <summary>
        /// For nodes such as BoundBinaryOperators where we use an iterative algorithm instead of the standard
        /// recursive algorithm to deal with deeply-nested stacks
        /// </summary>
        [XmlAttribute]
        public string SkipInNullabilityRewriter;
    }

    public class Kind
    {
        [XmlAttribute]
        public string Name;

        [XmlElement(ElementName = "KindComment", Type = typeof(CommentNode))]
        public List<CommentNode> Comments;
    }

    public sealed class Field : ICommentedNode
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Type;

        [XmlAttribute]
        public string Null;

        [XmlAttribute]
        public bool Override;

        [XmlAttribute]
        public string New;

        [XmlAttribute]
        public string PropertyOverrides;

        [XmlAttribute]
        public string SkipInVisitor;

        [XmlAttribute]
        public string SkipInNullabilityRewriter;

        [XmlElement(ElementName = "FieldComment", Type = typeof(CommentNode))]
        public List<CommentNode> Comments;

        ref List<CommentNode> ICommentedNode.GetCommentListField()
        {
            return ref Comments;
        }
    }

    public sealed class EnumType : TreeType
    {
        [XmlAttribute]
        public string Flags;

        [XmlElement(ElementName = "Field", Type = typeof(EnumField))]
        public List<EnumField> Fields;
    }

    public sealed class EnumField : ICommentedNode
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Value;

        [XmlElement(ElementName = "FieldComment", Type = typeof(CommentNode))]
        public List<CommentNode> Comments;

        ref List<CommentNode> ICommentedNode.GetCommentListField()
        {
            return ref Comments;
        }
    }
}
