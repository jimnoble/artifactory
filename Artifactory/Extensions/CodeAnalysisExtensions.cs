using Microsoft.CodeAnalysis;
using System.Linq;

namespace Artifactory.Extensions
{
    public static class CodeAnalysisExtensions
    {
        public static string ToFullName(this INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol.ContainingNamespace.Name != "")
            {
                return namespaceSymbol.ContainingNamespace.ToFullName() + "." +
                    namespaceSymbol.Name;
            }

            return namespaceSymbol.Name;
        }

        public static string ToTypeDisplayName(this ITypeSymbol typeSymbol)
        {
            var displayName = typeSymbol.Name;

            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

            if(namedTypeSymbol != null)
            {
                var args = namedTypeSymbol.TypeArguments;

                if(args.Any())
                {
                    displayName = string.Format(
                        "{0}<{1}>",
                        displayName,
                        args
                            .Select(a => a.ToTypeDisplayName())
                            .Aggregate((a, b) => a + ", " + b));
                }
            }

            return displayName;
        }

        public static INamedTypeSymbol GetTypeOrSubtypeByMetadataName(
            this Compilation compilation, 
            string name)
        {
            var r = compilation.GetTypeByMetadataName(name);

            if (r != null) return r;

            var i = name.LastIndexOf('.');

            if (i > 0)
            {
                return compilation.GetTypeOrSubtypeByMetadataName(
                    name.Substring(0, i) + "+" + name.Substring(i + 1));
            }

            return null;
        }
    }
}
