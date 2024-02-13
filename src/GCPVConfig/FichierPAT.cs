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
global using Console = System.Diagnostics.Debug;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Data;
using static GCPVConfig.FichierPAT;
using static GCPVConfig.Inscription;

using System.Diagnostics;
using System.Security.Cryptography;

namespace GCPVConfig
{
    public class FichierPAT
    {
        public class Competition
        {
            private int mId;
            private string mNom;

            public Competition(string nom, int id)
            {
                mId = id;
                mNom = nom;
            }

            public string Nom { get { return mNom; } }
            public int Id { get { return mId; } }

            public override string ToString()
            {
                return Nom;
            }
        }

        public class Club
        {
            public Club(int no, string nom, string abreviation)
            {
                Number = no;
                Name = nom;
                Abreviation = abreviation;
            }

            public int Number { get; set; }
            public string Name { get; set; }
            public string Abreviation { get; set; }
        }
        public class Patineur: IInscription
        {
            private FichierPAT fichierPat;
            public Patineur(int nopatineur, string firstName, string lastName, IInscription.SexEnum sex, DateOnly birthDate, string memberNumber, string club, int nocat, FichierPAT p_fichierPat)
            {
                NoPatineur = nopatineur;
                FirstName = firstName;
                LastName = lastName;
                Sex = sex;
                BirthDate = birthDate;
                MemberNumber = memberNumber;
                Club = club;
                fichierPat = p_fichierPat;
                NoCategory = nocat;
            }
            public int NoPatineur { get; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public IInscription.SexEnum Sex { get; set; }
            public DateOnly BirthDate { get; set; }
            public string? MemberNumber { get; set; }
            public string Club { get; set; }
            public int NoCategory { get; set; }

            public double GetClassement(string type)
            {
                using (var conn = fichierPat.GetConnection())
                {
                    conn.Open();
                    var cmd = new OleDbCommand("SELECT " + type + " FROM TPatineurs WHERE NoPatineur=@nopatineur",conn);
                    cmd.Parameters.AddWithValue("@nopatineur", NoPatineur);

                    double? v = (double?)cmd.ExecuteScalar();
                    return v is null ? 999 : (double)v;
                }
            }

            public void Save()
            {
                if(this.MemberNumber is null)
                {
                    throw new Exception("Aucun numéro de patineur");
                }
                if(this.MemberNumber.Length == 0)
                {
                    throw new Exception("Aucun numéro de patineur");
                }

                using (var conn = fichierPat.GetConnection())
                {
                    conn.Open();

                    int noCat = fichierPat.GetCategoryByDOB(BirthDate, Sex);
                    int noClub = fichierPat.GetNoClub(Club);

                    var cmd = new OleDbCommand("UPDATE TPatineurs SET CodePat=@codepat, Prenom=@prenom, Nom=@nom, Sexe=@sexe, NoCategorie=@nocat, NoClub=@noclub, [Date de naissance]=@dob WHERE NoPatineur=@nopatineur", conn);
                    cmd.Parameters.AddRange(new OleDbParameter[]
                    {
                                new OleDbParameter("@codepat", MemberNumber),
                                new OleDbParameter("@prenom", FirstName.ToUpper()),
                                new OleDbParameter("@nom", LastName.ToUpper()),
                                new OleDbParameter("@sexe", Sex==IInscription.SexEnum.Male?"M":"F"),
                                new OleDbParameter("@nocat", noCat),
                                new OleDbParameter("@noclub", noClub),
                                new OleDbParameter("@dob", BirthDate.ToDateTime(new TimeOnly(0,0,0))),
                                new OleDbParameter("@nopatineur", NoPatineur)
                    });
                    var i = cmd.ExecuteNonQuery();
                    if (i == 0)
                    {
                        throw new Exception("Aucun patineur mis à jour");
                    }
                }
            }
        }
        /*private class Categorie
        {
            public Categorie()
            {
                Nom = "";
                Min = 999;
                Max = 999;
            }
            public string Nom { get; set; }
            public int Min { get; set; }
            public int Max { get; set; }
        }*/

        /*private static List<Categorie> CATEGORIES = new List<Categorie>() {
            new Categorie { Nom = "5-6 ans", Min=5, Max=6 },
            new Categorie { Nom = "7-8 ans", Min = 7, Max = 8 },
            new Categorie { Nom = "9-10 ans", Min = 9, Max = 10 },
            new Categorie { Nom = "11-14 ans", Min = 11, Max = 14 }
        };*/

        public string Path { get; }
        private Dictionary<string, Patineur> patineurs = new Dictionary<string, Patineur>();

        public Dictionary<string, Patineur>.ValueCollection AllPatineurs
        {
            get { return patineurs.Values; }
        }

        public Dictionary<string, Patineur> AllPatineursDict
        {
            get { return patineurs; }
        }

        private FichierPAT(string path)
        {
            Path = path;
            Load();
        }

        private void Load()
        {
            foreach(Patineur patineur in GetAllPatineurs())
            {
                if(patineur.MemberNumber is null)
                {
                    throw new Exception("Numéro de membre vide dans le fichier PAT");
                }
                patineurs.Add(patineur.MemberNumber, patineur);
            }
        }

        public static FichierPAT Open(string path)
        {
            var pat = new FichierPAT(path);
            return pat;
        }

        private OleDbConnection GetConnection()
        {
            return new OleDbConnection(String.Format(
                "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}; Persist Security Info=False; OLE DB Services=-1",
                Path)
            );
        }

        public bool Ok()
        {
            using(var conn = GetConnection())
            {
                conn.Open();
                return conn.State == System.Data.ConnectionState.Open;
            }
        }

        public List<Tuple<string, string>> GetClassementName()
        {
            List<Tuple<string, string>> classements = new List<Tuple<string, string>>();
            using (var conn = GetConnection())
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 NomClassement, NomClassementGeneral, NomClassement1000, NomClassement1500, NomClassement2000, NomClassement2500 FROM TParametres", conn);
                var reader = cmd.ExecuteReader();
                
                reader.Read();
                classements.Add(new Tuple<string, string>("Classement", reader.GetString(0)));
                classements.Add(new Tuple<string, string>("ClassementGeneral", reader.GetString(1)));
                classements.Add(new Tuple<string, string>("Classement1000", reader.GetString(2)));
                classements.Add(new Tuple<string, string>("Classement1500", reader.GetString(3)));
                classements.Add(new Tuple<string, string>("Classement2000", reader.GetString(4)));
                classements.Add(new Tuple<string, string>("Classement2500", reader.GetString(5)));
            }
            return classements;
        }

        private List<Patineur> GetAllPatineurs()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand(@"SELECT NoPatineur, Prenom, TPatineurs.Nom, [Date de naissance] AS DOB, Sexe, Division, TPatineurs.NoCategorie, TPatineurs.NoClub, Classement, CategCalc, CodePat, Abreviation, TCategorie.Nom
                    FROM (TPatineurs
                    INNER JOIN TClubs ON TPatineurs.NoClub=TClubs.NoClub)
                    INNER JOIN TCategorie ON TPatineurs.NoCategorie=TCategorie.NoCategorie;", conn);
                var reader = cmd.ExecuteReader();

                List<Patineur> patineurs = new List<Patineur>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        try
                        {
                            Console.WriteLine(reader.GetString(1));
                            Console.WriteLine(reader.GetString(2));
                            var patineur = new Patineur(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetString(4).ToLower() == "m" ? IInscription.SexEnum.Male : IInscription.SexEnum.Female,
                                DateOnly.FromDateTime(reader.GetDateTime(3)),
                                reader.GetString(10),
                                reader.GetString(11),
                                reader.GetInt32(6),
                                this
                            );
                            patineurs.Add(patineur);
                        }
                        catch(System.InvalidCastException)
                        {
                            continue;
                        }
                        
                    }
                }

                return patineurs;
            }
        }

        public string GetCategoryNom(int numerocat)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand(@"SELECT TOP 1 Nom FROM TCategorie WHERE NoCategorie=@nocat", conn);

                cmd.Parameters.AddWithValue("@nocat", numerocat);
                return (string)cmd.ExecuteScalar();
            }
        }

        public Patineur? GetPatineurByNoMembre(string noMembre)
        {
            if(patineurs.ContainsKey(noMembre))
            {
                return patineurs[noMembre];
            }
            return null;
        }

        public Patineur? GetPatineurByInfo(IInscription inscription)
        {
            foreach(var patineur in patineurs.Values)
            {
                if((String.Compare(inscription.FirstName, patineur.FirstName, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0) &&
                    (String.Compare(inscription.LastName, patineur.LastName, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0) &&
                    inscription.BirthDate == patineur.BirthDate
                )
                {
                    return patineur;
                }
            }
            return null;
        }

        private static Regex REGEX_PATINEUR_BAD_STARTS = new Regex(@"^(\d+)\s*(.+)$");
        private static Regex REGEX_PATINEUR_END_NUMBER = new Regex(@"^(.+)\s+(\(\d+\))$");
        public void FixPatineurNom()
        {
            List<Patineur> patineurs = GetAllPatineurs();

            foreach(var patineur in patineurs)
            {
                bool changed = false;
                Match m = REGEX_PATINEUR_BAD_STARTS.Match(patineur.LastName);
                if (m.Success)
                {
                    patineur.LastName = m.Groups[2].Value.Trim();
                    changed = true;
                }

                m = REGEX_PATINEUR_END_NUMBER.Match(patineur.FirstName);
                if (m.Success)
                {
                    patineur.FirstName = m.Groups[1].Value.Trim();
                    changed = true;
                }
                if(changed)
                {
                    patineur.Save();
                }
            }
        }

        internal void FixCategories(List<Category> categories)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {

                    {
                        OleDbCommand cmd = new OleDbCommand("UPDATE TCategorie SET AgeMin=99, AgeMax=99, AgeMinF=99, AgeMaxF=99", conn);
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                    }

                    foreach (Category categorie in categories)
                    {
                        int? catNo = null;
                        {
                            OleDbCommand cmd = new OleDbCommand("SELECT NoCategorie FROM TCategorie WHERE Nom=@nom", conn);
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddRange(new OleDbParameter[]
                            {
                                new OleDbParameter("@nom", categorie.Name)
                            });
                            catNo = (int?)cmd.ExecuteScalar();
                        }

                        if (catNo is not null)
                        {
                            var cmd = new OleDbCommand("UPDATE TCategorie SET AgeMin =@agemin, AgeMax =@agemax, AgeMinF =@ageminf, AgeMaxF =@agemaxf WHERE NoCategorie =@nocat", conn);
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddRange(new OleDbParameter[]
                            {
                                new OleDbParameter("@agemin", categorie.MinAge),
                                new OleDbParameter("@agemax", categorie.MaxAge),
                                new OleDbParameter("@ageminf", categorie.MinAge),
                                new OleDbParameter("@agemaxf", categorie.MaxAge),
                                new OleDbParameter("@nocat", catNo)
                            });
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            var cmd = new OleDbCommand("INSERT INTO TCategorie (Nom, AgeMin, AgeMax, AgeMinF, AgeMaxF) VALUES (@nom,@agemin,@agemax,@ageminf,@agemaxf)", conn);
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddRange(new OleDbParameter[]
                            {
                                new OleDbParameter("@nom", categorie.Name),
                                new OleDbParameter("@agemin", categorie.MinAge),
                                new OleDbParameter("@agemax", categorie.MaxAge),
                                new OleDbParameter("@ageminf", categorie.MinAge),
                                new OleDbParameter("@agemaxf", categorie.MaxAge)
                            });
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        private static int GetAge(DateOnly dob)
        {
            var FIRST_JULY = new DateTime(DateTime.Now.Year, 06, 30,0,0,0);
            if(FIRST_JULY > DateTime.Now)
            {
                FIRST_JULY = new DateTime(DateTime.Now.Year-1, 06, 30, 0, 0, 0);
            }
            var age = new AgeCalculator.Age(dob.ToDateTime(TimeOnly.Parse("00:00:00")), FIRST_JULY);
            return age.Years;
        }

        private int GetCategoryByDOB(DateOnly dob, IInscription.SexEnum sex)
        {
            string request = "";
            if (sex == IInscription.SexEnum.Male)
            {
                request = "SELECT NoCategorie FROM TCategorie WHERE AgeMin <= @agemin AND AgeMax >= @agemax";
            }
            else
            {
                request = "SELECT NoCategorie FROM TCategorie WHERE AgeMinF <= @agemin AND AgeMaxF >= @agemax";
            }

            using (var conn = GetConnection())
            {
                int age = GetAge(dob);

                conn.Open();
                var cmd = new OleDbCommand(request, conn);
                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("@agemin", age),
                    new OleDbParameter("@agemax", age)
                });
                var nocat = cmd.ExecuteScalar();
                if(nocat is null)
                {
                    throw new Exception(String.Format("Catégorie non trouvée pour un patineur agé de {0}", age));
                }
                return (int)nocat;
            }
        }

        private int GetNoClub(string clubabvr)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new OleDbCommand("SELECT NoClub FROM TClubs WHERE Abreviation=@abvr", conn);
                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("@abvr", clubabvr)
                });
                var noclub = cmd.ExecuteScalar();
                if (noclub is null)
                {
                    throw new Exception(String.Format("Le club {0} n'a pas été trouvé dans le fichier PAT",clubabvr));
                }
                return (int)noclub;
            }
        }

        public void Add(IInscription inscription, string division)
        {
            if (inscription.MemberNumber is null)
            {
                throw new Exception("Aucun numéro de patineur");
            }

            if (GetPatineurByNoMembre(inscription.MemberNumber) is not null)
            {
                throw new Exception("Le numéro de patineur existe déjà");
            }
            using (var conn = GetConnection())
            {
                conn.Open();

                int nocat = GetCategoryByDOB(inscription.BirthDate, inscription.Sex);
                {
                        var cmd = new OleDbCommand("INSERT INTO TPatineurs (Prenom, Nom, [Date de naissance], Sexe, Division, NoCategorie, NoClub, Classement, CategCalc, CodePat, Classement1000, Classement1500, ClassementGeneral, Classement2000, Classement2500) VALUES (@prenom, @nom, @dob, @sexe, @division, @nocat, @noclub, 999, 1, @codepat, 999, 999, 999, 999, 999);", conn);
                    cmd.Parameters.AddRange(new OleDbParameter[]
                    {
                        

                        new OleDbParameter("@prenom", inscription.FirstName.ToUpper()),
                        new OleDbParameter("@nom", inscription.LastName.ToUpper()),
                        new OleDbParameter("@dob", inscription.BirthDate.ToDateTime(new TimeOnly(0,0,0))),
                        new OleDbParameter("@sexe", inscription.Sex==IInscription.SexEnum.Male?"M":"F"),
                        new OleDbParameter("@nocat", nocat),
                        new OleDbParameter("@noclub", GetNoClub(inscription.Club)),
                        new OleDbParameter("@codepat", inscription.MemberNumber),
                        new OleDbParameter("@division", division)
                    });
                    cmd.ExecuteNonQuery();
                }
                {
                    var cmd = new OleDbCommand("SELECT @@identity;", conn);
                    int nopat = (int)cmd.ExecuteScalar();

                    Patineur newPat = new Patineur(nopat, inscription.FirstName, inscription.LastName, inscription.Sex, inscription.BirthDate, inscription.MemberNumber, inscription.Club, nocat, this);
                    patineurs.Add(newPat.MemberNumber, newPat);
                }
            }
        }

        public void FixPatineurCategories()
        {
            foreach(var patineur in patineurs.Values)
            {
                int newcat = -1;
                try
                {
                    newcat = GetCategoryByDOB(patineur.BirthDate, patineur.Sex);
                }
                catch(Exception)
                {
                    newcat = -1;
                }

                if (newcat >= 0 && patineur.NoCategory != newcat)
                {
                    patineur.NoCategory = newcat;
                    patineur.Save();
                }
            }
        }
        public List<Competition> GetCompetitions()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new OleDbCommand("SELECT NoCompetition, Lieu FROM TCompetition", conn);

                var reader = cmd.ExecuteReader();

                List<Competition> competitions = new List<Competition>();
                if(reader.HasRows)
                {
                    while(reader.Read())
                    {
                        Competition compe = new Competition(
                            reader.GetString(1),
                            reader.GetInt32(0)
                        );
                        competitions.Add(compe);
                    }
                }

                return competitions;
            }
        }

        public void ClearInscription(Competition competition)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new OleDbCommand("DELETE FROM TPatineur_compe WHERE NoCompetition=@nocompe", conn);

                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("@nocompe", competition.Id)
                });

                cmd.ExecuteNonQuery();
            }
        }

        public void Inscrire(Patineur patineur, Competition competition, string division, int nocasque)
        {
            var idclub = GetNoClub(patineur.Club);

            using (var conn = GetConnection())
            {
                conn.Open();

                OleDbCommand cmd = new OleDbCommand("INSERT INTO TPatineur_compe (NoCompetition, NoPatineur, Division, NoCategorie, NoClub, Rang, Groupe, Si_Regroup_Classement, NoCasque) VALUES (@nocompe, @nopat, @division, @nocat, @noclub, 0, 'Pas dans un groupe', 1, @nocasque)", conn);

                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("@nocompe", competition.Id),
                    new OleDbParameter("@nopat", patineur.NoPatineur),
                    new OleDbParameter("@division", division),
                    new OleDbParameter("@nocat", patineur.NoCategory),
                    new OleDbParameter("@noclub", idclub),
                    new OleDbParameter("@nocasque", nocasque==0?DBNull.Value:nocasque)
            });

                cmd.ExecuteNonQuery();
            }
        }

        internal List<string> GetCompetiteurs(Competition? competitionToImport)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new OleDbCommand("SELECT CodePat FROM TPatineur_compe INNER JOIN TPatineurs ON TPatineurs.NoPatineur=TPatineur_compe.NoPatineur WHERE NoCompetition=@nocompet", conn);
                cmd.Parameters.AddWithValue("@nocompet", competitionToImport.Id);

                var reader = cmd.ExecuteReader();

                List<string> patineurs = new List<string>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        patineurs.Add(reader.GetString(0));
                    }
                }

                return patineurs;
            }
        }

        internal void AjoutGroup(Dictionary<string, List<Patineur>> groups, Competition competitionToImport)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                {
                    var cmd = new OleDbCommand("DELETE FROM TGroupes_Compe WHERE NoCompetition=@nocompet", conn);
                    cmd.Parameters.AddWithValue("@nocompet", competitionToImport.Id);
                    cmd.ExecuteNonQuery();
                }

                foreach(var group in groups)
                {
                    {
                        var cmd = new OleDbCommand("INSERT INTO TGroupes_Compe(NoCompetition, Groupe, GroupeAdditionnelResultats) VALUES (@nocompet,@groupname,'N/A')", conn);
                        cmd.Parameters.AddRange(new OleDbParameter[]
                        {
                            new OleDbParameter("@nocompet",competitionToImport.Id),
                            new OleDbParameter("@groupname",group.Key)
                        });
                        cmd.ExecuteNonQuery();
                    }

                    foreach(Patineur patineur in group.Value)
                    {
                        var cmd = new OleDbCommand("UPDATE TPatineur_compe SET Groupe=@groupname WHERE NoCompetition=@nocompet AND NoPatineur=@nopat", conn);
                        cmd.Parameters.AddRange(new OleDbParameter[]
                        {
                            new OleDbParameter("@groupname",group.Key),
                            new OleDbParameter("@nocompet",competitionToImport.Id),
                            new OleDbParameter("@nopat",patineur.NoPatineur)
                        });
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            }

        internal List<Club> GetClubs()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new OleDbCommand("SELECT NoClub, [Nom du club], Abreviation FROM TClubs", conn);

                var reader = cmd.ExecuteReader();

                List<Club> clubs = new List<Club>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Club c = new Club(
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.GetString(2)
                        );
                        clubs.Add(c);
                    }
                }

                return clubs;
            }
        }

        internal Club? GetClubByAbr(string abr)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new OleDbCommand("SELECT TOP 1 NoClub, [Nom du club], Abreviation FROM TClubs WHERE Abreviation=@abr", conn);
                cmd.Parameters.AddWithValue("@abr", abr);

                var reader = cmd.ExecuteReader();

                List<Club> clubs = new List<Club>();
                if (reader.HasRows)
                {
                    reader.Read();
                    Club c = new Club(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2)
                    );
                    return c;
                }

                return null;
            }
        }
    }
}
