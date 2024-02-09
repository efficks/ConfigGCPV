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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static GCPVConfig.FichierPAT;

namespace GCPVConfig
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

    internal class Groupe
    {
        public Groupe(string nom)
        {
            Nom = nom;
            Patineurs = new List<Patineur>();
        }
        public string Nom { get; set; }
        public List<Patineur> Patineurs { get; set; }
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
        private Config mConfiguration;
        private string mTypeCompetitionName;

        public IProgress<string>? ProgressMessage { get; set; }

        public Importer(string inscriptionPath, string patPath, Config configuration, string typeCompetitionName)
        {
            mInscriptionPath = inscriptionPath;
            mPATPath = patPath;
            mConfiguration = configuration;
            mTypeCompetitionName = typeCompetitionName;
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

                var typeCompeConfig = mConfiguration.GetTypeConfig(mTypeCompetitionName);
                if (typeCompeConfig.NumeroBonnet)
                {
                    inscriptions.Sort((ins1, ins2) => ins1.Club.CompareTo(ins2.Club));
                    int nocasque = 1;
                    foreach(var ins in inscriptions)
                    {
                        ins.NoCasque = nocasque;
                        nocasque++;
                    }
                }

                ProgressMessage?.Report(String.Format("{0} inscriptions chargées", inscriptions.Count));

                ProgressMessage?.Report("Correction des catégories");
                pat.FixCategories(mConfiguration.Categories);
                pat.FixPatineurNom();

                try
                {
                    if(!SetupPatineurs(inscriptions, pat))
                    {
                        return;
                    }
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
                    pat.Inscrire(patineur, competitionToImport, mConfiguration.Division, inscription.NoCasque);
                }
            }
        }

        private bool SetupPatineurs(List<Inscription> inscriptions, FichierPAT pat)
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
                        return false;
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
                        pat.Add(inscription, mConfiguration.Division);
                    }
                }
                /*
                 * Même nom, prénom, club
                 * Même nom, prénom, dob
                 * Même nom (ci), prénom (ci), dob, club, sexe
                 */
            }
            return true;
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

        public async Task Regroup(string v)
        {
            await Task.Run(() =>
            {
                ProgressMessage?.Report("Début du regroupement");

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

                var typeCompeConfig = mConfiguration.GetTypeConfig(mTypeCompetitionName);
                var competitions = pat.GetCompetitions();
                Competition competitionToImport = OnSelectCompetition(competitions);

                List<string> competiteursId = pat.GetCompetiteurs(competitionToImport);

                List<Patineur> competiteurs = new List<Patineur>();
                foreach (string id in competiteursId)
                {
                    competiteurs.Add(pat.GetPatineurByNoMembre(id));
                }

                Dictionary<string, bool> configCategoryMixte = new Dictionary<string, bool>();
                foreach(var cat in mConfiguration.Categories)
                {
                    configCategoryMixte[cat.Name] = cat.Mixte;
                }

                Dictionary<string, List<Patineur>> patineurParCategoye = new Dictionary<string, List<Patineur>>();

                foreach(Patineur patineur in competiteurs)
                {
                    var groupname = pat.GetCategoryNom(patineur.NoCategory);
                    string gr1 = groupname;
                    groupname += " gr. {0}";
                    if (!configCategoryMixte[gr1]) //non-mixte
                    {
                        groupname += patineur.Sex == IInscription.SexEnum.Male ? " M":" F";
                    }

                    if(!patineurParCategoye.ContainsKey(groupname))
                    {
                        patineurParCategoye.Add(groupname, new List<Patineur>());
                    }
                    patineurParCategoye[groupname].Add(patineur);
                }

                Dictionary<string, List<Patineur>> groups = new Dictionary<string, List<Patineur>>();

                

                foreach (string categoryname in patineurParCategoye.Keys)
                {
                    List<Patineur> currentGroup = new List<Patineur>();
                    
                    int i = 1;
                    string currentGroupName = String.Format(categoryname, i);
                    groups.Add(currentGroupName, currentGroup);

                    var patineurcategory = patineurParCategoye[categoryname];
                    patineurcategory.Sort((p1, p2) => p1.GetClassement(v).CompareTo(p2.GetClassement(v)));

                    while(patineurcategory.Count > 0)
                    {
                        

                        if(currentGroup.Count >= typeCompeConfig.MaxPatineur && patineurcategory.Count >= typeCompeConfig.MinPatineurDerniere)
                        {
                            ProgressMessage?.Report(String.Format("Ajout du groupe {0} - {1} patineurs", currentGroupName, currentGroup.Count));

                            i++;
                            currentGroup = new List<Patineur>();
                            currentGroupName = String.Format(categoryname, i);
                            groups.Add(currentGroupName, currentGroup);
                            
                        }
                        Patineur p = patineurcategory[0];
                        patineurcategory.RemoveAt(0);

                        currentGroup.Add(p);
                    }
                    ProgressMessage?.Report(String.Format("Ajout du groupe {0} - {1} patineurs", currentGroupName, currentGroup.Count));
                }

               
                pat.AjoutGroup(groups, competitionToImport);
                ProgressMessage?.Report(String.Format("{0} groupes créés",groups.Count));

                ProgressMessage?.Report("Fin du regroupement");
            });
        }
    }
}
