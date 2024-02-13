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
    /// Logique d'interaction pour SelectClub.xaml
    /// </summary>
    public partial class SelectClub : Window
    {
        public class ClubComboItem
        {
            public string Text { get; set; }
            public FichierPAT.Club Value { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private FichierPAT.Club? choice;
        public SelectClub(List<FichierPAT.Club> clubs, string club_inconnu)
        {
            InitializeComponent();
            list_clubs.Items.Clear();
            foreach(FichierPAT.Club club in clubs)
            {
                ClubComboItem item = new ClubComboItem
                {
                    Text = club.Name,
                    Value = club
                };
                list_clubs.Items.Add(item);
            }
            txtMessage.Text = String.Format("Club {0} inconnu. Sélectionnez le club correspondant.",club_inconnu);
            choice = null;
        }

        public FichierPAT.Club? Choice
        {
            get { return choice; }
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            choice = ((ClubComboItem)list_clubs.SelectedItem).Value;
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
                return this.list_clubs.SelectedIndex > -1;
            }
        }

        private void list_competitions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btn_ok.IsEnabled = list_clubs.SelectedIndex > -1;
        }

        private void list_clubs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            choice = ((ClubComboItem)list_clubs.SelectedItem).Value;
            this.DialogResult = true;
        }
    }
}
