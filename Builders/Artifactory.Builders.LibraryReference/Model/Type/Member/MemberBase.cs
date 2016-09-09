
namespace Artifactory.Builders.LibraryReference.Model.Type.Member
{
    public abstract class MemberBase
    {
        public string DocumentCommentId { get; set; }

        public TypeRef Type { get; set; }

        public string Name { get; set; }
    }
}
