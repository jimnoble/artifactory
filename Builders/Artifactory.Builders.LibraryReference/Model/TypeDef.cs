using System.Collections.Generic;

namespace Artifactory.Builders.LibraryReference.Model
{
    public abstract class TypeDef : CommentedAttributed
    {
        public Namespace Namespace { get; set; }

        public TypeRef ContainingType { get; set; }

        public string Name { get; set; }

        public List<GenericTypeParameter> GenericTypeParameters { get; set; }

        public List<GenericTypeConstraint> GenericTypeConstraints { get; set; }
    }
}
