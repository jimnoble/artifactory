using Artifactory.Builders.WebApi.Configuration;
using Artifactory.Builders.WebApi.Model;
using Artifactory.Extensions;
using Artifactory.Interface;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Action = Artifactory.Builders.WebApi.Model.Action;

namespace Artifactory.Builders.WebApi.Implementation
{
    public class WebApiViewModelBuilder : IViewModelBuilder
    {
        public object BuildViewModel()
        {
            var webApiBuilderConfigurationSection = ConfigurationManager.GetSection("webApiBuilderSection")
                as WebApiBuilderConfigurationSection;

            var controllerFilter = new Regex(
                webApiBuilderConfigurationSection.ControllerFilterRegex, 
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var apiControllerSubclassesPattern = webApiBuilderConfigurationSection.ApiControllerSubclassesRegex;

            var apiControllerSubclassesFilter = !string.IsNullOrWhiteSpace(apiControllerSubclassesPattern) ?
                new Regex(
                    apiControllerSubclassesPattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase) : 
                null;

            var solution = LoadSolution(webApiBuilderConfigurationSection.SolutionPath);

            var apiControllers = GetApiControllers(solution, apiControllerSubclassesFilter).ToList();

            var viewModel = new Root
            {
                Controllers = new List<Controller>()
            };

            var referencedSymbols = new Dictionary<string, Compilation>();

            foreach (var apiController in apiControllers)
            {
                var classSymbol = apiController.Item3.GetDeclaredSymbol(apiController.Item2);

                if (!controllerFilter.IsMatch(classSymbol.Name))
                {
                    continue;
                }

                var controller = AnalyzeApiControllerActions(
                    apiController.Item1,
                    apiController.Item2,
                    apiController.Item3,
                    referencedSymbols);

                if(controller.Actions.Any())
                {
                    viewModel.Controllers.Add(controller);
                }                
            }

            viewModel.ReferencedTypes = BuildReferencedTypes(referencedSymbols);

            viewModel.Controllers = viewModel.Controllers.OrderBy(c => c.Name).ToList();

            viewModel.ReferencedTypes = viewModel.ReferencedTypes.OrderBy(c => c.Name).ToList();

            return viewModel;
        }

        static List<ReferencedType> BuildReferencedTypes(Dictionary<string, Compilation> referencedSymbols)
        {
            if(!referencedSymbols.Any()) return Enumerable.Empty<ReferencedType>().ToList();

            var nestedReferencedSymbols = new Dictionary<string, Compilation>();

            return referencedSymbols
                .Where(t => t.Key.StartsWith("T:") && !t.Key.StartsWith("T:System"))
                .ToList()
                .Select(typeNameCompilation =>
                {
                    var typeName = typeNameCompilation.Key.Replace("T:", "");

                    var typeSymbol = typeNameCompilation.Value.GetTypeOrSubtypeByMetadataName(typeName);

                    if(typeSymbol == null)
                    {
                        return new ReferencedType
                        {
                            DocumentationCommentId = typeNameCompilation.Key,

                            Name = typeName,

                            NotFound = true
                        };
                    }

                    var typeCommentsXml = typeSymbol.GetDocumentationCommentXml();

                    var typeSummaryText = default(string);

                    if (!string.IsNullOrWhiteSpace(typeCommentsXml))
                    {
                        var typeCommentRoot = XElement.Parse(typeCommentsXml);

                        var typeSummary = typeCommentRoot.Name == "summary" ?
                            typeCommentRoot :
                            typeCommentRoot.Element("summary");

                        if (typeSummary != null)
                        {
                            foreach (var cref in typeSummary.GetCrefs())
                            {
                                if (!referencedSymbols.ContainsKey(cref))
                                {
                                    referencedSymbols.Add(cref, typeNameCompilation.Value);
                                }
                            }

                            typeSummaryText = typeSummary.ToMarkup();
                        }
                    }

                    var properties = GetAllProperties(typeSymbol).ToList();
                    
                    return new ReferencedType
                    {
                        DocumentationCommentId = typeNameCompilation.Key,

                        Name = typeSymbol.ToTypeDisplayName(),

                        Summary = typeSummaryText,

                        Properties = properties
                            .Select(propertyNameSymbol =>
                            {
                                var propertyCommentXml = propertyNameSymbol.Item2.GetDocumentationCommentXml();

                                var summaryText = "";

                                if(!string.IsNullOrWhiteSpace(propertyCommentXml))
                                {
                                    var propertyCommentRoot = XElement.Parse(
                                        $"<comment>{propertyCommentXml}</comment>");

                                    propertyCommentRoot = propertyCommentRoot.Element("member") ?? propertyCommentRoot;

                                    var summary = propertyCommentRoot.Name == "summary" ? 
                                        propertyCommentRoot : 
                                        propertyCommentRoot.Element("summary");

                                    if (summary != null)
                                    {
                                        foreach (var cref in summary.GetCrefs())
                                        {
                                            if (!referencedSymbols.ContainsKey(cref))
                                            {
                                                referencedSymbols.Add(cref, typeNameCompilation.Value);
                                            }
                                        }

                                        summaryText = summary.ToMarkup();
                                    }
                                }

                                var type = propertyNameSymbol.Item2.Type;

                                if(type.TypeKind == TypeKind.Array)
                                {
                                    type = (type as IArrayTypeSymbol).ElementType;
                                }

                                var key = type.GetDocumentationCommentId();

                                if (!referencedSymbols.ContainsKey(key) && 
                                    !nestedReferencedSymbols.ContainsKey(key))
                                {
                                    nestedReferencedSymbols.Add(key, typeNameCompilation.Value);
                                }

                                return new Property
                                {
                                    Alias = propertyNameSymbol.Item1 != propertyNameSymbol.Item2.Name ? 
                                        propertyNameSymbol.Item1 : 
                                        null,

                                    Name = propertyNameSymbol.Item2.Name,

                                    TypeName = type.ToTypeDisplayName(), 

                                    TypeDocumentCommentId = type.GetDocumentationCommentId(),

                                    Summary = summaryText
                                };
                            })
                            .ToList()
                    };
                })
                .ToList()
                .Concat(BuildReferencedTypes(nestedReferencedSymbols))
                .ToList();
        }

        static IEnumerable<Tuple<string, IPropertySymbol>> GetAllProperties(INamedTypeSymbol typeSymbol)
        {
            if(typeSymbol.Name == "Object")
            {
                yield break;
            }

            if (typeSymbol.BaseType != null)
            {
                foreach (var baseProperty in GetAllProperties(typeSymbol.BaseType))
                {
                    yield return baseProperty;
                }
            }

            var properties = typeSymbol
                .GetMembers()
                .Where(s => s.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>();

            foreach (var property in properties)
            {
                var name = property.Name;

                var attributes = property.GetAttributes();

                if (attributes.Any())
                {
                    if (attributes.Any(a => a.AttributeClass.Name == "JsonIgnoreAttribute"))
                    {
                        continue;
                    }

                    var jsonPropertyAttribute = attributes.SingleOrDefault(
                        a => a.AttributeClass.Name == "JsonPropertyAttribute");

                    if(jsonPropertyAttribute != null)
                    {
                        name = jsonPropertyAttribute.ConstructorArguments
                            .Single()
                            .Value
                            .ToString();
                    }
                }

                yield return Tuple.Create(name, property);
            }
        }

        static Controller AnalyzeApiControllerActions(
            Compilation compilation,
            ClassDeclarationSyntax classDeclaration,
            SemanticModel semanticModel,
            Dictionary<string, Compilation> referencedSymbols)
        {
            var routePrefixSymbol = compilation.GetTypeByMetadataName("System.Web.Http.RoutePrefixAttribute");

            // Get the class and containing namespace symbols.

            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            var namespaceSymbol = classSymbol.ContainingNamespace;

            var controller = new Controller
            {
                DocumentationCommentId = classSymbol.GetDocumentationCommentId(),

                Namespace = classSymbol.ContainingNamespace?.ToFullName(),

                Name = classSymbol.Name,

                Actions = new List<Action>()
            };

            // Go through the class's XML comments, if any.

            var classCommentXml = classSymbol.GetDocumentationCommentXml();

            if (!string.IsNullOrWhiteSpace(classCommentXml))
            {
                var classCommentRoot = XElement.Parse(classCommentXml);

                var summary = classCommentRoot.Element("summary");

                if(summary != null)
                {
                    foreach(var cref in summary.GetCrefs())
                    {
                        if (!referencedSymbols.ContainsKey(cref))
                        {
                            referencedSymbols.Add(cref, compilation);
                        }
                    }

                    controller.Summary = summary.ToMarkup();
                }

                var remarks = classCommentRoot.Element("remarks");

                if (remarks != null)
                {
                    foreach (var cref in remarks.GetCrefs())
                    {
                        if (!referencedSymbols.ContainsKey(cref))
                        {
                            referencedSymbols.Add(cref, compilation);
                        }
                    }

                    controller.Remarks = remarks.ToMarkup();
                }
            }

            // Go through the class's attributes, if any.

            var routePrefix = string.Empty;

            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // For each attribute, try to pull a recognized value.

                    var attributeTypeInfo = semanticModel.GetTypeInfo(attribute);

                    if (attributeTypeInfo.Type.MetadataName == routePrefixSymbol.MetadataName)
                    {
                        var routePrefixArgument = attribute
                            .ArgumentList
                            .Arguments
                            .Single()
                            .Expression as LiteralExpressionSyntax;

                        routePrefix = routePrefixArgument.Token.ValueText + "/";
                    }
                }
            }

            // Get all public methods.

            var actionMethodDeclarations = classDeclaration
                .DescendantNodes()
                .Where(n => n.IsKind(SyntaxKind.MethodDeclaration))
                .Cast<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mo => mo.ValueText == "public"))
                .ToList();

            foreach (var actionMethodDeclaration in actionMethodDeclarations)
            {
                var action = AnalyzeActionMethod(
                    compilation,
                    semanticModel,
                    actionMethodDeclaration,
                    referencedSymbols,
                    routePrefix);

                if(!string.IsNullOrWhiteSpace(action.Method))
                {
                    controller.Actions.Add(action);
                }                
            }

            return controller;
        }

        static Action AnalyzeActionMethod(
            Compilation compilation,
            SemanticModel semanticModel,
            MethodDeclarationSyntax actionMethodDeclaration,
            Dictionary<string, Compilation> referencedSymbols,
            string routePrefix)
        {
            // Get some symbols by full name.

            var httpGetSymbol = compilation.GetTypeByMetadataName("System.Web.Http.HttpGetAttribute");

            var httpPostSymbol = compilation.GetTypeByMetadataName("System.Web.Http.HttpPostAttribute");

            var httpPutSymbol = compilation.GetTypeByMetadataName("System.Web.Http.HttpPutAttribute");

            var httpDeleteSymbol = compilation.GetTypeByMetadataName("System.Web.Http.HttpDeleteAttribute");

            var routeSymbol = compilation.GetTypeByMetadataName("System.Web.Http.RouteAttribute");

            var fromBodySymbol = compilation.GetTypeByMetadataName("System.Web.Http.FromBodyAttribute");

            var fromUriSymbol = compilation.GetTypeByMetadataName("System.Web.Http.FromUriAttribute");

            var methodSymbol = semanticModel.GetDeclaredSymbol(actionMethodDeclaration);

            var action = new Action
            {
                DocumentationCommentId = methodSymbol.GetDocumentationCommentId(),

                Name = methodSymbol.Name,
                
                BodyParameters = new List<Parameter>(),

                QueryParameters = new List<Parameter>(),

                RouteParameters = new List<Parameter>(),

                Examples = new List<Example>()
            };

            // Go through the method's XML comments.

            var methodCommentsXml = methodSymbol.GetDocumentationCommentXml();

            var parameterNotes = new Dictionary<string, XElement>();

            if (!string.IsNullOrWhiteSpace(methodCommentsXml))
            {
                var methodCommentsRoot = XElement.Parse(methodCommentsXml);

                var summary = methodCommentsRoot.Element("summary");

                if(summary != null)
                {
                    foreach (var cref in summary.GetCrefs())
                    {
                        if (!referencedSymbols.ContainsKey(cref))
                        {
                            referencedSymbols.Add(cref, compilation);
                        }
                    }

                    action.Summary = summary.ToMarkup();
                }
                
                var remarks = methodCommentsRoot.Element("remarks");

                if(remarks != null)
                {
                    foreach (var cref in remarks.GetCrefs())
                    {
                        if (!referencedSymbols.ContainsKey(cref))
                        {
                            referencedSymbols.Add(cref, compilation);
                        }
                    }

                    action.Remarks = remarks.ToMarkup();
                }

                var returns = methodCommentsRoot.Element("returns");

                if(returns != null)
                {
                    foreach (var cref in returns.GetCrefs())
                    {
                        if (!referencedSymbols.ContainsKey(cref))
                        {
                            referencedSymbols.Add(cref, compilation);
                        }
                    }

                    action.Returns = returns.ToMarkup();
                }
                
                foreach (var example in methodCommentsRoot.Elements("example"))
                {
                    action.Examples.Add(new Example
                    {
                        Label = example.Attribute("label").Value,

                        Content = string
                            .Concat(example.Nodes())
                            .NormalizeCodeIndentation()
                    });
                }

                parameterNotes = methodCommentsRoot
                    .Elements("param")
                    .ToDictionary(
                        p => p.Attribute("name").Value,
                        p => p);
            }

            var methodName = actionMethodDeclaration.Identifier.ValueText;
            
            var routeKeys = Enumerable.Empty<string>();

            // Go through the method's attributes.

            foreach (var attributeList in actionMethodDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // For each attribute, try to pull a recognized value.

                    var attributeTypeInfo = semanticModel.GetTypeInfo(attribute);

                    if (attributeTypeInfo.Type.MetadataName == httpGetSymbol.MetadataName)
                    {
                        action.Method = "GET";
                    }
                    else if (attributeTypeInfo.Type.MetadataName == httpPostSymbol.MetadataName)
                    {
                        action.Method = "POST";
                    }
                    else if (attributeTypeInfo.Type.MetadataName == httpPutSymbol.MetadataName)
                    {
                        action.Method = "PUT";
                    }
                    else if (attributeTypeInfo.Type.MetadataName == httpDeleteSymbol.MetadataName)
                    {
                        action.Method = "DELETE";
                    }
                    else if (attributeTypeInfo.Type.MetadataName == routeSymbol.MetadataName)
                    {
                        var routeArgument = attribute
                            .ArgumentList
                            .Arguments
                            .Single()
                            .Expression as LiteralExpressionSyntax;
                                                
                        action.Route = routePrefix + routeArgument.Token.ValueText;

                        routeKeys = Regex
                            .Matches(action.Route, "(?<={)[^}]+(?=})")
                            .Cast<Match>()
                            .Select(m => m.Value)
                            .ToList();
                    }
                }
            }

            // Go through parameters

            foreach (var parameter in actionMethodDeclaration.ParameterList.Parameters)
            {
                var predefinedType = parameter.Type as PredefinedTypeSyntax;

                //var typeName = predefinedType != null ?
                //    predefinedType.Keyword.ValueText :
                //    (parameter.Type as SimpleNameSyntax).Identifier.ValueText;

                var typeInfo = semanticModel.GetTypeInfo(parameter.Type);

                var documentCommentId = typeInfo.Type.GetDocumentationCommentId();

                if(!string.IsNullOrWhiteSpace(documentCommentId) && !referencedSymbols.ContainsKey(documentCommentId))
                {
                    referencedSymbols.Add(documentCommentId, compilation);
                }

                var parameterType = semanticModel.GetTypeInfo(parameter.Type);

                var parameterName = parameter.Identifier.ValueText;

                var fromBody = false;

                var fromUri = false;

                foreach (var attributeList in parameter.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var attributeTypeInfo = semanticModel.GetTypeInfo(attribute);

                        if (attributeTypeInfo.Type.MetadataName == fromBodySymbol.MetadataName)
                        {
                            fromBody = true;
                        }
                        else if (attributeTypeInfo.Type.MetadataName == fromUriSymbol.MetadataName)
                        {
                            fromUri = true;
                        }
                    }
                }

                var outParameter = new Parameter
                {
                    Key = parameterName,

                    TypeName = typeInfo.Type.ToTypeDisplayName(),

                    TypeDocumentCommentId = typeInfo.Type.GetDocumentationCommentId(),

                    Optional = false, // TODO: not this

                    Notes = parameterNotes.ContainsKey(parameterName) ?
                        parameterNotes[parameterName].ToMarkup() :
                        null
                };

                if(routeKeys.Contains(parameterName))
                {
                    action.RouteParameters.Add(outParameter);
                }
                else if (fromUri || parameterType.Type.IsValueType)
                {
                    action.QueryParameters.Add(outParameter);

                }
                else if (fromBody || !parameterType.Type.IsValueType)
                {
                    action.BodyParameters.Add(outParameter);
                }
            }

            return action;
        }

        static IEnumerable<Tuple<Compilation, ClassDeclarationSyntax, SemanticModel>> GetApiControllers(
            Solution solution, 
            Regex apiControllerSubclassesRegex)
        {
            foreach (var project in solution.Projects)
            {
                var compilation = project.GetCompilationAsync().Result;

                var apiControllerType = compilation.GetTypeByMetadataName("System.Web.Http.ApiController");

                if (apiControllerType != null)
                {
                    foreach (var syntaxTree in compilation.SyntaxTrees)
                    {
                        var semanticModel = compilation.GetSemanticModel(syntaxTree);

                        var classDeclarations = syntaxTree
                            .GetRoot()
                            .DescendantNodesAndSelf()
                            .Where(n => n.IsKind(SyntaxKind.ClassDeclaration))
                            .Cast<ClassDeclarationSyntax>();

                        foreach (var classDeclaration in classDeclarations)
                        {
                            var bases = classDeclaration.BaseList;

                            if (bases?.Types != null)
                            {
                                foreach (var baseType in bases.Types)
                                {
                                    var namedTypeSymbol = semanticModel
                                        .GetTypeInfo(baseType.Type)
                                        .Type as INamedTypeSymbol;

                                    if (namedTypeSymbol.Equals(apiControllerType) ||
                                        (apiControllerSubclassesRegex?.IsMatch(namedTypeSymbol.Name) ?? false))
                                    {
                                        yield return Tuple.Create(
                                            compilation,
                                            classDeclaration,
                                            semanticModel);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static Solution LoadSolution(string solutionPath)
        {
            return MSBuildWorkspace
                .Create()
                .OpenSolutionAsync(solutionPath)
                .Result;
        }
    }
}
