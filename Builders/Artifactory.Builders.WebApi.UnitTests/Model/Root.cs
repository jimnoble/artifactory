using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artifactory.Builders.WebApi.UnitTests.Model
{
    class Root
    {
        public List<Controller> Controllers { get; set; }

        public List<ReferencedType> ReferencedTypes { get; set; }
    }
}
