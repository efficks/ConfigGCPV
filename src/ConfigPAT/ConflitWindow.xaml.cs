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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConfigPat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ConflitWindow : Window
    {
        private Importer.ConflictAction action = Importer.ConflictAction.Cancel;
        public ConflitWindow(Inscription inscription, FichierPAT.Patineur patineur)
        {
            InitializeComponent();
            InitInscription(inscription);
            InitPatineur(patineur);
        }

        private void InitInscription(Inscription inscription)
        {
            lbl_infoInscription.Content =
                "Nom : " + inscription.LastName.ToUpper() + System.Environment.NewLine +
                "Prénom : " + inscription.FirstName.ToUpper() + System.Environment.NewLine +
                "# FPVQ : " + inscription.MemberNumber + System.Environment.NewLine +
                "Sexe : " + (inscription.Sex == IInscription.SexEnum.Male ? "Masculin" : "Féminin") + System.Environment.NewLine +
                "Date de naissance : " + inscription.BirthDate.ToShortDateString() + System.Environment.NewLine +
                "Club : " + inscription.Club;
        }

        private void InitPatineur(FichierPAT.Patineur patineur)
        {
            lbl_infoPAT.Content =
                "Nom : " + patineur.LastName + System.Environment.NewLine +
                "Prénom : " + patineur.FirstName + System.Environment.NewLine +
                "# FPVQ : " + patineur.MemberNumber + System.Environment.NewLine +
                "Sexe : " + (patineur.Sex == IInscription.SexEnum.Male ? "Masculin" : "Féminin") + System.Environment.NewLine +
                "Date de naissance : " + patineur.BirthDate.ToShortDateString() + System.Environment.NewLine +
                "Club : " + patineur.Club;
        }

        internal Importer.ConflictAction Result{
            get { return action; }
        }

        private void btnInscription_Click(object sender, RoutedEventArgs e)
        {
            action = Importer.ConflictAction.KeepInscription;
            this.DialogResult = true;
        }

        private void btnPat_Click(object sender, RoutedEventArgs e)
        {
            action = Importer.ConflictAction.KeepPAT;
            this.DialogResult = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            action = Importer.ConflictAction.Cancel;
            this.DialogResult = false;
        }
    }
}
