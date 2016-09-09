using System.Configuration;

namespace Artifactory.Configuration
{
    public class ServiceConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("Services", IsDefaultCollection = false)]
        [ConfigurationCollection(
            typeof(ServiceCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public ServiceCollection Services
        {
            get { return (ServiceCollection)base[nameof(Services)]; }
        }
    }

    public class ServiceConfig : ConfigurationElement
    {
        public ServiceConfig() { }

        public ServiceConfig(string assemblyFilename, string viewModelBuilderType)
        {

        }

        [ConfigurationProperty(nameof(AssemblyFilename), IsRequired = true, IsKey = true)]
        public string AssemblyFilename
        {
            get { return (string)this[nameof(AssemblyFilename)]; }

            set { this[nameof(AssemblyFilename)] = value; }
        }

        [ConfigurationProperty(nameof(ViewModelBuilderType), IsRequired = true, IsKey = true)]
        public string ViewModelBuilderType
        {
            get { return (string)this[nameof(ViewModelBuilderType)]; }

            set { this[nameof(ViewModelBuilderType)] = value; }
        }
    }

    public class ServiceCollection : ConfigurationElementCollection
    {
        public ServiceCollection() { }

        public ServiceConfig this[int index]
        {
            get { return (ServiceConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ServiceConfig serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ServiceConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var serviceConfig = element as ServiceConfig;

            return serviceConfig.AssemblyFilename + ":" + serviceConfig.ViewModelBuilderType;
        }

        public void Remove(ServiceConfig serviceConfig)
        {
            BaseRemove(GetElementKey(serviceConfig));
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }
}
