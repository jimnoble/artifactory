using System.Collections.Generic;

namespace Artifactory.Builders.WebApi.UnitTests.Model
{
    class Controller
    {
        public string DocumentationCommentId { get; set; }

        public List<Dependency> Dependencies { get; set; }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public string Summary { get; set; }

        public string Remarks { get; set; }

        public List<Action> Actions { get; set; }
    }
}
