/*
    ConfigGCPV - Outil d'aide à la configuration de fichier PAT
    Copyright (C) 2022  François-Xavier Choinière

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

    François-Xavier Choinière, fx@efficks.com
*/
using GCPVConfig;
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

namespace GCPVConfig
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
