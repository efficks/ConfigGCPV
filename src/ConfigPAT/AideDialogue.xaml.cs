using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ConfigPat
{
    /// <summary>
    /// Logique d'interaction pour AideDialogue.xaml
    /// </summary>
    public partial class AideDialogue : Window
    {
        public string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(ConfigPat)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public AideDialogue()
        {
            InitializeComponent();
            mdViewer.Markdown = ReadResource("aide.md");
        }
    }
}
