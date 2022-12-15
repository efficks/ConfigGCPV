using ConfigPAT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static ConfigPAT.FichierPAT;

namespace ConfigPat
{
    internal class ConflictFoundEventArgs : EventArgs
    {
        private ManualResetEvent mre = new ManualResetEvent(false);
        public Inscription? Inscription { get; set; }
        public Patineur? Patineur { get; set; }
        public Importer.ConflictAction UserAction { get; set; }
        public ManualResetEvent MRE
        {
            get{ return mre; }
        }
    }

    internal class SelectCompetitionEventArgs : EventArgs
    {
        public SelectCompetitionEventArgs()
        {
            Competition = new List<FichierPAT.Competition>();
            Choice = null;
        }

        private ManualResetEvent mre = new ManualResetEvent(false);
        public List<FichierPAT.Competition> Competition { get; set; }
        public FichierPAT.Competition? Choice { get; set; }
        public ManualResetEvent MRE
        {
            get { return mre; }
        }
    }

    internal delegate void ConflictFoundEventHandler(Object sender, ConflictFoundEventArgs e);
    internal delegate void SelectCompetitionEventHandler(Object sender, SelectCompetitionEventArgs e);

    internal class Importer
    {
        public enum ConflictAction{
            Cancel,
            KeepInscription,
            KeepPAT
        }
        protected ConflictAction OnConflictDetected(Inscription i, Patineur p)
        {
            ConflictFoundEventArgs e = new ConflictFoundEventArgs
            {
                Inscription = i,
                Patineur = p
            };
            ConflictFound?.Invoke(this, e);
            e.MRE.WaitOne();
            return e.UserAction;
        }

        protected FichierPAT.Competition? OnSelectCompetition(List<FichierPAT.Competition> competitions)
        {
            SelectCompetitionEventArgs e = new SelectCompetitionEventArgs
            {
                Competition = competitions
            };
            SelectCompetition?.Invoke(this, e);
            e.MRE.WaitOne();
            return e.Choice;
        }

        public event ConflictFoundEventHandler ConflictFound;
        public event SelectCompetitionEventHandler SelectCompetition;

        private string mInscriptionPath;
        private string mPATPath;

        public IProgress<string>? ProgressMessage { get; set; }

        public Importer(string inscriptionPath, string patPath)
        {
            mInscriptionPath = inscriptionPath;
            mPATPath = patPath;
        }

        public async Task Import()
        {
            await Task.Run(() =>
            {
                ProgressMessage?.Report("Début de l'importation");

                ProgressMessage?.Report("Ouverture du fichier PAT");
                var pat = FichierPAT.Open(mPATPath);

                if (!pat.Ok())
                {
                    ProgressMessage?.Report("Erreur lors de l'ouverture du fichier PAT");
                    return;
                }

                if (pat is null)
                {
                    return;
                }

                ProgressMessage?.Report("Chargement des inscriptions");
                List<Inscription> inscriptions = new List<Inscription>();
                try
                {
                    inscriptions = Inscription.LoadInscription(mInscriptionPath);
                }
                catch(Exception e)
                {
                    ProgressMessage?.Report("Erreur lors de l'ouverture du fichier d'inscription");
                    ProgressMessage?.Report(e.Message);
                    return;
                }
                ProgressMessage?.Report(String.Format("{0} inscriptions chargées", inscriptions.Count));

                ProgressMessage?.Report("Correction des catégories");
                pat.FixCategories();

                try
                {
                    SetupPatineurs(inscriptions, pat);
                }
                catch(Exception e)
                {
                    ProgressMessage?.Report("Une erreur est survenue");
                    ProgressMessage?.Report(e.Message);
                    return;
                }

                pat.FixPatineurCategories();
                Inscrire(inscriptions, pat);
            });

            ProgressMessage?.Report("Fin de l'importation");
        }

        private void Inscrire(List<Inscription> inscriptions, FichierPAT pat)
        {
            var competitions = pat.GetCompetitions();
            var competitionToImport = OnSelectCompetition(competitions);

            if (competitionToImport is not null)
            {
                pat.ClearInscription(competitionToImport);

                foreach (var inscription in inscriptions)
                {
                    var patineur = pat.GetPatineurByNoMembre(inscription.MemberNumber);
                    pat.Inscrire(patineur, competitionToImport);
                }
            }
        }

        private void SetupPatineurs(List<Inscription> inscriptions, FichierPAT pat)
        {
            foreach (var inscription in inscriptions)
            {
                FichierPAT.Patineur? patineur = null;
                if (inscription.MemberNumber is not null && inscription.MemberNumber.Length > 0)
                {
                    patineur = pat.GetPatineurByNoMembre(inscription.MemberNumber);
                }

                if (patineur is not null)
                {
                    if(!ValidateSame(inscription, patineur))
                    {
                        ProgressMessage?.Report("Arrêt de l'importation");
                        return;
                    }
                }
                else
                {
                    patineur = pat.GetPatineurByInfo(inscription);
                    if (patineur is not null)
                    {
                        if (inscription.MemberNumber is not null && inscription.MemberNumber.Length > 0)
                        {
                            ProgressMessage?.Report(String.Format("Correction du numéro de patineur de {0}, {1}", inscription.LastName, inscription.FirstName));
                            // nouveau numéro
                            if (pat.GetPatineurByNoMembre(inscription.MemberNumber) is not null)
                            {
                                throw new Exception("Le numéro de patineur existe déjà");
                            }

                            string oldNumber = patineur.MemberNumber;

                            patineur.MemberNumber = inscription.MemberNumber;
                            patineur.FirstName = inscription.FirstName;
                            patineur.LastName = inscription.LastName;
                            patineur.Sex = inscription.Sex;
                            patineur.Club = inscription.Club;

                            patineur.Save();

                            pat.AllPatineursDict.Add(patineur.MemberNumber, patineur);
                            pat.AllPatineursDict.Remove(oldNumber);
                        }
                        else
                        {
                            inscription.MemberNumber = patineur.MemberNumber;
                        }
                    }
                    else
                    {
                        //Check partial info
                        /*patineur = pat.GetPatineurPartial(inscription);

                        if (!ValidateSame(inscription, patineur))
                        {
                            ProgressMessage?.Report("Arrêt de l'importation");
                            return;
                        }*/

                        ProgressMessage?.Report(String.Format("Ajout du patineur {0}, {1}", inscription.LastName, inscription.FirstName));
                        pat.Add(inscription);
                    }
                }
                /*
                 * Même nom, prénom, club
                 * Même nom, prénom, dob
                 * Même nom (ci), prénom (ci), dob, club, sexe
                 */
            }
        }

        private bool ValidateSame(Inscription inscription, FichierPAT.Patineur patineur)
        {
            if (inscription.BirthDate == patineur.BirthDate
                && inscription.FirstName.ToUpper() == patineur.FirstName.ToUpper()
                && inscription.LastName.ToUpper() == patineur.LastName.ToUpper()
                && inscription.Sex == patineur.Sex
                && inscription.Club.ToUpper() == patineur.Club.ToUpper()
                && patineur.MemberNumber == inscription.MemberNumber
            )
            {
                ProgressMessage?.Report(String.Format("Inscription de {0}, {1} ok",inscription.LastName, inscription.FirstName));
            }
            else
            {
                var action = OnConflictDetected(inscription, patineur);

                switch(action)
                {
                    case ConflictAction.Cancel:
                        return false;
                    case ConflictAction.KeepInscription:
                        patineur.BirthDate = inscription.BirthDate;
                        patineur.FirstName = inscription.FirstName;
                        patineur.LastName = inscription.LastName;
                        patineur.Club = inscription.Club;
                        patineur.MemberNumber = inscription.MemberNumber;
                        patineur.Sex = inscription.Sex;

                        patineur.Save();
                        ProgressMessage?.Report(String.Format("Inscription de {0}, {1} fusionnée", inscription.LastName, inscription.FirstName));
                        break;
                    case ConflictAction.KeepPAT:
                        inscription.MemberNumber = patineur.MemberNumber;
                        break;
                }
            }
            return true;
        }

        //private void FixPatineurCat()
    }
}
