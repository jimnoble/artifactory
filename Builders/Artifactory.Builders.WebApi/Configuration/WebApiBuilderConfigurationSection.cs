using System.Configuration;

namespace Artifactory.Builders.WebApi.Configuration
{
    public class WebApiBuilderConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty(nameof(SolutionPath), IsRequired = true)]
        public string SolutionPath => base[nameof(SolutionPath)] as string; 

        [ConfigurationProperty(nameof(ControllerFilterRegex), IsRequired = true)]
        public string ControllerFilterRegex => base[nameof(ControllerFilterRegex)] as string;

        [ConfigurationProperty(
            nameof(ApiControllerSubclassesRegex), 
            IsRequired = false, 
            DefaultValue = "")]
        public string ApiControllerSubclassesRegex => base[nameof(ApiControllerSubclassesRegex)] as string;
    }
}
