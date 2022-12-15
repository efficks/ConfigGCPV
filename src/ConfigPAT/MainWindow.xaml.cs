using ConfigPAT;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps;

namespace ConfigPat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void NoArgDelegate(IProgress<string> progress);

        public MainWindow()
        {
            InitializeComponent();
            ValidImportation();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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
                txt_patPath.Text = openFileDialog.FileName;
            }
        }

        private void ValidImportation()
        {
            btn_launchImport.IsEnabled = txt_patPath.Text.Length > 0 && txt_inscriptionPath.Text.Length > 0;
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
            Progress<string> progressMessage = new Progress<string>(msg => NewMessageReport(msg));

            txt_log.Clear();
            string inscriptionPath = txt_inscriptionPath.Text;
            string patPath = txt_patPath.Text;
            Importer importer = new Importer(inscriptionPath, patPath);
            importer.ProgressMessage = progressMessage;
            importer.ConflictFound += ConflictFound;
            importer.SelectCompetition += SelectCompetition;
            await importer.Import();
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
                    SelectCompe window = new SelectCompe(e.Competition);
                    var result = window.ShowDialog();
                    if (result is not null && result == true)
                    {
                        e.Choice = window.Choice;
                    }
                    else
                    {
                        e.Choice = null;
                    }
                }
                catch
                {
                    e.Choice = null;
                    throw;
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
    }
}
