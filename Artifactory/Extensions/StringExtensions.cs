//using HeyRed.MarkdownSharp;
using Markdig;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Artifactory.Extensions
{
    public static class StringExtensions
    {
        public static string PascalCaseToLowerDashed(this string s)
        {
            return Regex
                .Replace(
                    s, 
                    @"(?<=[a-z0-9]+)[A-Z]", 
                    m => "-" + m.Value.ToLower())
                .ToLower();            
        }

        public static string NormalizeXmlCommentIndentation(this string input)
        {
            var lines = input
                .Split(
                    new string[] { "\r\n", "\n" },
                    StringSplitOptions.None)
                .Select(l => l.Replace("\t", "    "))
                .ToList();

            if(lines.Count < 2)
            {
                return input;
            }

            var minIndent = lines
                .Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => Regex.Match(l, " *").Value.Length)
                .Min();

            return string.Join(
                "\r\n",
                lines.Select(l => Regex.Replace(l, $"^ {{{minIndent}}}", "")));
        }

        public static string ToMarkdown(this string input)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            return Markdown.ToHtml(input, pipeline);

            //return 
            //    new Markdown(new MarkdownOptions
            //    {
                    
            //    })
            //    .Transform(input);
        }

        public static string ToMarkup(this string input)
        {
            var text = input;

            text = Regex.Replace(text, @"[.]\s*\r*\n\s*", ".</p><p>");

            text = Regex.Replace(text, @"\s*\r*\n\s*", " ");

            return "<p>" + text + "</p>";
        }

        public static string NormalizeCodeIndentation(this string input)
        {
            input = input.Replace("\t", "    ");

            var lines = input
                .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l));

            var minIndent = lines.Select(l => l.TakeWhile(c => c == ' ').Count()).Min();

            var output = lines.Select(l => l.Substring(minIndent)).Aggregate((a, b) => a + "\r\n" + b);

            return output;
        }
    }
}
