using Artifactory.Builders.LibraryReference.Model.Comment;
using System.Collections.Generic;

namespace Artifactory.Builders.LibraryReference.Model
{
    public abstract class CommentedAttributed : Commented
    {
        public List<Attribute> Attributes { get; set; }
    }
}
