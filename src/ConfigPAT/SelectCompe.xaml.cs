using ConfigPAT;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Logique d'interaction pour SelectCompe.xaml
    /// </summary>
    public partial class SelectCompe : Window
    {
        private FichierPAT.Competition? choice;
        public SelectCompe(List<FichierPAT.Competition> competitions)
        {
            InitializeComponent();
            list_competitions.Items.Clear();
            foreach (FichierPAT.Competition compe in competitions)
            {
                list_competitions.Items.Add(compe);
            }
            choice = null;
        }

        public FichierPAT.Competition? Choice
        {
            get { return choice; }
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            choice = (FichierPAT.Competition)list_competitions.Items[list_competitions.SelectedIndex];
            this.DialogResult = true;
        }

        private void btn_annuler_Click(object sender, RoutedEventArgs e)
        {
            choice = null;
            this.DialogResult = false;
        }

        internal bool ok_enabled
        {
            get
            {
                return this.list_competitions.SelectedIndex > -1;
            }
        }

        private void list_competitions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btn_ok.IsEnabled = list_competitions.SelectedIndex > -1;
        }
    }
}
