using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.IO;

namespace GCPVConfig
{
    internal class Category
    {
        public string Name { get; set; }

        [YamlMember(Alias = "minage", ApplyNamingConventions = false)]
        public string MinAge { get; set; }

        [YamlMember(Alias = "maxage", ApplyNamingConventions = false)]
        public string MaxAge { get; set; }

        public bool Mixte { get; set; }
    }

    internal class TypeCompetition
    {
        public string Name { get; set; }
        [YamlMember(Alias = "numero_bonnet", ApplyNamingConventions = false)]
        public bool NumeroBonnet { get; set; }

        [YamlMember(Alias = "max_pat", ApplyNamingConventions = false)]
        public int MaxPatineur { get; set; }

        [YamlMember(Alias = "min_derniere", ApplyNamingConventions = false)]
        public int MinPatineurDerniere { get; set; }
    }
    internal class Config
    {
        public List<Category> Categories { get; set; }

        [YamlMember(Alias = "typecompetition", ApplyNamingConventions = false)]
        public List<TypeCompetition> TypeCompetition { get; set; }

        public string Division { get; set; }

        public static Config load(string config_path)
        {
            using (var reader = new StreamReader(config_path))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                Config config = deserializer.Deserialize<Config>(reader);
                return config;
            }
        }

        public TypeCompetition GetTypeConfig(string typename)
        {
            foreach(TypeCompetition t in TypeCompetition)
            {
                if(t.Name == typename)
                {
                    return t;
                }
            }
            return null;
        }
    }
}
