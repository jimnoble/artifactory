using System.Collections.Generic;

namespace Artifactory.Builders.LibraryReference.Model.Comment
{
    public abstract class Commented
    {
        public string CommentId { get; set; }

        public string Summary { get; set; }

        public string Remarks { get; set; }

        public string Returns { get; set; }

        public List<Example> Examples { get; set; }
    }
}
