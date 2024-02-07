﻿/*
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
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GCPVConfig
{
    public class CourseTemps
    {
        public string Distance { get; set; }
        public string Secondes { get; set; }
    }

    public class ClassementComboItem
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void NoArgDelegate(IProgress<string> progress);
        private ObservableCollection<CourseTemps> EmployeeCollection = new ObservableCollection<CourseTemps>();
        private Config mConfig = null;

        public MainWindow()
        {
            InitializeComponent();
            ValidImportation();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            this.Title = String.Format("GCPV Config version {0}", Common.version());

            try
            {
                mConfig = Config.load("GCPVCOnfig.yaml");
            }
            catch
            {
                MessageBox.Show("Erreur lors de l'ouverture du fichier de configuration GCPVCOnfig.yaml",
                    "Erreur de configuration",
                    MessageBoxButton.OK,MessageBoxImage.Error);
                throw;
            }

            foreach (var typecompe in mConfig.TypeCompetition)
            {
                this.combo_evenement_type.Items.Add(typecompe.Name);
            }
        }

        private void DG1_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string headername = e.Column.Header.ToString();

            //Cancel the column you don't want to generate
            if (headername == "MiddleName")
            {
                e.Cancel = true;
            }

            //update column details when generating
            if (headername == "FirstName")
            {
                e.Column.Header = "First Name";
            }
            else if (headername == "LastName")
            {
                e.Column.Header = "Last Name";
            }
            else if (headername == "EmailAddress")
            {
                e.Column.Header = "Email";
            }
        }

        private void btn_openInscription_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier d'inscription (*.xlsx)|*.xlsx";
            if (openFileDialog.ShowDialog() == true)
            {
                txt_inscriptionPath.Text = openFileDialog.FileName;
            }
            ValidImportation();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier PAT (*.pat)|*.pat|Tous les fichiers (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                var old_cursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    txt_patPath.Text = openFileDialog.FileName;

                    combo_competition_list.Items.Clear();
                    combo_classement.Items.Clear();

                    if (File.Exists(txt_patPath.Text))
                    {
                        var patfile = FichierPAT.Open(txt_patPath.Text);
                        var competitions = patfile.GetCompetitions();

                        foreach (var compe in competitions)
                        {
                            combo_competition_list.Items.Add(compe.Nom);
                            combo_competition_list.IsEnabled = true;
                        }

                        var classements = patfile.GetClassementName();
                        foreach (var c in classements)
                        {
                            ClassementComboItem newitem = new ClassementComboItem();
                            newitem.Value = c.Item1;
                            newitem.Text = c.Item2;

                            combo_classement.Items.Add(newitem);
                        }
                    }
                    else
                    {
                        combo_competition_list.IsEnabled = false;
                    }

                }
                finally
                {
                    this.Cursor = old_cursor;
                }
            }
        }

        private void ValidImportation()
        {
            btn_launchImport.IsEnabled = txt_patPath.Text.Length > 0 && txt_inscriptionPath.Text.Length > 0 && combo_evenement_type.SelectedValue != null && combo_competition_list.SelectedValue != null;
        }

        private void txt_patPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidImportation();
        }

        private void txt_inscriptionPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidImportation();
        }

        private async void btn_launchImport_Click(object sender, RoutedEventArgs e)
        {
            btn_launchImport.IsEnabled = false;
            var old_cursor = Cursor;
            Cursor = Cursors.Wait;
            Progress<string> progressMessage = new Progress<string>(msg => NewMessageReport(msg));

            txt_log.Clear();
            string inscriptionPath = txt_inscriptionPath.Text;
            string patPath = txt_patPath.Text;
            Importer importer = new Importer(inscriptionPath, patPath, mConfig, combo_evenement_type.SelectedItem.ToString());
            importer.ProgressMessage = progressMessage;
            importer.ConflictFound += ConflictFound;
            importer.SelectCompetition += SelectCompetition;
            await importer.Import();

            btn_launchImport.IsEnabled = true;
            Cursor = old_cursor;
        }
        private void NewMessageReport(string msg)
        {
            lock (txt_log)
            {
                txt_log.AppendText(msg + System.Environment.NewLine);
                txt_log.ScrollToEnd();
            }
        }

        private void SelectCompetition(Object sender, SelectCompetitionEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    e.Choice = null;
                    foreach (var c in e.Competition)
                    {
                        if (c.Nom == combo_competition_list.SelectedItem.ToString())
                        {
                            e.Choice = c;
                        }
                    }
                }
                finally
                {
                    e.MRE.Set();
                }
            });
        }
        private void ConflictFound(Object sender, ConflictFoundEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    ConflitWindow cw = new ConflitWindow(e.Inscription, e.Patineur);
                    var result = cw.ShowDialog();
                    if (result is not null && result == true)
                    {
                        e.UserAction = cw.Result;
                    }
                    else
                    {
                        e.UserAction = Importer.ConflictAction.Cancel;
                    }
                }
                catch
                {
                    e.UserAction = Importer.ConflictAction.Cancel;
                    throw;
                }
                finally
                {
                    e.MRE.Set();
                }
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AideDialogue ad = new AideDialogue();
            ad.ShowDialog();
        }

        private void btn_launchMinute_Click(object sender, RoutedEventArgs e)
        {

        }

        private void combo_evenement_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ValidImportation();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private async void btn_launchRegroupement_Click(object sender, RoutedEventArgs e)
        {
            btn_launchRegroupement.IsEnabled = false;
            var old_cursor = Cursor;
            Cursor = Cursors.Wait;

            Progress<string> progressMessage = new Progress<string>(msg => NewMessageReport(msg));

            txt_log.Clear();
            string inscriptionPath = txt_inscriptionPath.Text;
            string patPath = txt_patPath.Text;
            Importer importer = new Importer(inscriptionPath, patPath, mConfig, combo_evenement_type.SelectedItem.ToString());
            importer.ProgressMessage = progressMessage;
            importer.ConflictFound += ConflictFound;
            importer.SelectCompetition += SelectCompetition;
            await importer.Regroup( ((ClassementComboItem)combo_classement.SelectedItem).Value);

            btn_launchRegroupement.IsEnabled = true;
            Cursor = old_cursor;
        }
    }
}
