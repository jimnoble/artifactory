using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artifactory.Builders.WebApi.Model
{
    class ReferencedType
    {
        public string DocumentationCommentId { get; set; }

        public string Name { get; set; }

        public string Summary { get; set; }

        public bool NotFound { get; set; }

        public List<Property> Properties { get; set; }
    }
}
