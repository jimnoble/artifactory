using Artifactory.Configuration;
using Artifactory.Extensions;
using Artifactory.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Artifactory.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = LoadServices();

            var serviceName = args[0];

            var serviceInstance = Activator.CreateInstance(services[serviceName]) as IViewModelBuilder;

            var viewModel = serviceInstance.BuildViewModel();

            var viewModelJson = JsonConvert.SerializeObject(viewModel);

            var viewTemplate = File.ReadAllText("Views/" + serviceName + "-view.html");

            var viewHtml = viewTemplate.Replace("/* INSERT MODEL HERE */", viewModelJson);

            var filename = Guid.NewGuid().ToString("N") + ".html";

            File.WriteAllText(filename, viewHtml);

            Process.Start(filename);
        }
        
        static IDictionary<string, Type> LoadServices()
        {
            var results = new Dictionary<string, Type>();

            var serviceConfigurationSection = ConfigurationManager.GetSection("servicesSection") 
                as ServiceConfigurationSection;

            foreach(var service in serviceConfigurationSection.Services.Cast<ServiceConfig>())
            {
                var path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    service.AssemblyFilename);

                var assembly = Assembly.LoadFile(path);

                var type = assembly.GetType(service.ViewModelBuilderType);

                var name = type.Name.Replace("ViewModelBuilder", "").PascalCaseToLowerDashed();

                results.Add(name, type);
            }

            return results;
        }
    }
}
