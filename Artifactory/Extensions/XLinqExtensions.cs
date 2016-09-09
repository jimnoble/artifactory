using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace Artifactory.Extensions
{
    public static class XLinqExtensions
    {
        public static IEnumerable<string> GetCrefs(this XElement element)
        {
            var seeElements = element
                .DescendantNodes()
                .Where(n => n is XElement)
                .Cast<XElement>()
                .Where(e => e.Name == "see");

            foreach (var seeElement in seeElements)
            {
                yield return seeElement.Attribute("cref").Value;
            }
        }

        public static string ToMarkup(this XElement element)
        {
            var seeElements = element
                .DescendantNodes()
                .Where(n => n is XElement)
                .Cast<XElement>()
                .Where(e => e.Name == "see");

            foreach (var seeElement in seeElements)
            {
                var target = seeElement.Attribute("cref").Value;

                seeElement.Name = "a";

                seeElement.SetAttributeValue("href", "#" + target);

                seeElement.SetAttributeValue("class", "code");

                seeElement.SetAttributeValue("cref", null);

                if(target.StartsWith("M:"))
                {
                    seeElement.Value = Regex.Match(target, @"[a-zA-Z0-9]+(?=\()").Value;
                }
                else
                {
                    seeElement.Value = target.Substring(target.LastIndexOf('.') + 1);
                }                
            }
            
            return HttpUtility
                .HtmlDecode(string.Concat(element.Nodes()).Trim())
                .NormalizeXmlCommentIndentation()
                .ToMarkdown();
        }
    }
}
