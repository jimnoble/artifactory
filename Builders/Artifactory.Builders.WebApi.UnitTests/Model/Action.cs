using System.Collections.Generic;

namespace Artifactory.Builders.WebApi.UnitTests.Model
{
    class Action
    {
        public string DocumentationCommentId { get; set; }

        public string Name { get; set; }

        public string Summary { get; set; }

        public string Method { get; set; }

        public string Route { get; set; }

        public List<Parameter> RouteParameters { get; set; }

        public List<Parameter> QueryParameters { get; set; }

        public List<Parameter> BodyParameters { get; set; }

        public string RequestContentType { get; set; }

        public string Returns { get; set; }

        public string ResponseContentType { get; set; }

        public List<Header> ResponseHeaders { get; set; }

        public string Remarks { get; set; }

        public List<Example> Examples { get; set; }

        public List<Result> Results { get; set; }
    }
}
