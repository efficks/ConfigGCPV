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

        public Category()
        {
            Name = "";
            MinAge = "0";
            MaxAge = "99";
            Mixte = false;
        }
    }

    internal class TypeCompetition
    {
        private string name="";

        public string Name { get => name; set => name = value; }
        [YamlMember(Alias = "numero_bonnet", ApplyNamingConventions = false)]
        public bool NumeroBonnet { get; set; }

        [YamlMember(Alias = "max_pat", ApplyNamingConventions = false)]
        public int MaxPatineur { get; set; }

        [YamlMember(Alias = "min_derniere", ApplyNamingConventions = false)]
        public int MinPatineurDerniere { get; set; }
    }
    internal class Config
    {
        private List<Category> categories = new List<Category>();
        private List<TypeCompetition> typeCompetition = new List<TypeCompetition>();
        private string division = "";

        public List<Category> Categories { get => categories; set => categories = value; }

        [YamlMember(Alias = "typecompetition", ApplyNamingConventions = false)]
        public List<TypeCompetition> TypeCompetition { get => typeCompetition; set => typeCompetition = value; }

        public string Division { get => division; set => division = value; }

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

        public TypeCompetition? GetTypeConfig(string typename)
        {
            return TypeCompetition.Find(t => t.Name == typename);
        }
    }
}
