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
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Data;
using static ConfigPAT.FichierPAT;
using static ConfigPAT.Inscription;

namespace ConfigPAT
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
        private class Categorie
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
        }

        private static List<Categorie> CATEGORIES = new List<Categorie>() {
            new Categorie { Nom = "5-6 ans", Min=5, Max=6 },
            new Categorie { Nom = "7-8 ans", Min = 7, Max = 8 },
            new Categorie { Nom = "9-10 ans", Min = 9, Max = 10 },
            new Categorie { Nom = "11-14 ans", Min = 11, Max = 14 }
        };

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

        private List<Patineur> GetAllPatineurs()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand("SELECT NoPatineur, Prenom, Nom, [Date de naissance] AS DOB, Sexe, Division, NoCategorie, TPatineurs.NoClub, Classement, CategCalc, CodePat, Abreviation FROM TPatineurs INNER JOIN TClubs ON TPatineurs.NoClub=TClubs.NoClub;", conn);
                var reader = cmd.ExecuteReader();

                List<Patineur> patineurs = new List<Patineur>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
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
                }

                return patineurs;
            }
        }

        public Patineur? GetPatineurByNoMembre(string noMembre)
        {
            if(patineurs.ContainsKey(noMembre))
            {
                return patineurs[noMembre];
            }
            return null;
            /*using (var conn = GetConnection())
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 NoPatineur, Prenom, Nom, [Date de naissance] AS DOB, Sexe, Division, NoCategorie, TPatineurs.NoClub, Classement, CategCalc, CodePat, Abreviation FROM TPatineurs INNER JOIN TClubs ON TPatineurs.NoClub=TClubs.NoClub WHERE CodePat=@codepat", conn);
                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("@codepat", noMembre)
                });
                var reader = cmd.ExecuteReader();

                Patineur? patineur = null;
                if (reader.HasRows)
                {
                    reader.Read();

                    patineur = new Patineur(
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
                }

                return patineur;
            }*/
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

            /*using (var conn = GetConnection())
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 NoPatineur, Prenom, Nom, [Date de naissance] AS DOB, Sexe, Division, NoCategorie, TPatineurs.NoClub, Classement, CategCalc, CodePat, Abreviation FROM TPatineurs INNER JOIN TClubs ON TPatineurs.NoClub=TClubs.NoClub WHERE Nom=@nom AND Prenom=@prenom AND [Date de naissance]=@dob", conn);
                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("@nom", inscription.LastName.ToUpper()),
                    new OleDbParameter("@prenom", inscription.FirstName.ToUpper()),
                    new OleDbParameter("@dob", inscription.BirthDate.ToDateTime(new TimeOnly(0,0,0)))
                });
                var reader = cmd.ExecuteReader();

                Patineur? patineur = null;
                if (reader.HasRows)
                {
                    reader.Read();

                    patineur = new Patineur(
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
                }

                return patineur;
            }*/
        }

        public void FixCategories()
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

                    foreach (Categorie categorie in CATEGORIES)
                    {
                        int? catNo = null;
                        {
                            OleDbCommand cmd = new OleDbCommand("SELECT NoCategorie FROM TCategorie WHERE Nom=@nom", conn);
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddRange(new OleDbParameter[]
                            {
                                new OleDbParameter("@nom", categorie.Nom)
                            });
                            catNo = (int?)cmd.ExecuteScalar();
                        }

                        if (catNo is not null)
                        {
                            var cmd = new OleDbCommand("UPDATE TCategorie SET AgeMin =@agemin, AgeMax =@agemax, AgeMinF =@ageminf, AgeMaxF =@agemaxf WHERE NoCategorie =@nocat", conn);
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddRange(new OleDbParameter[]
                            {
                                new OleDbParameter("@agemin", categorie.Min),
                                new OleDbParameter("@agemax", categorie.Max),
                                new OleDbParameter("@ageminf", categorie.Min),
                                new OleDbParameter("@agemaxf", categorie.Max),
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
                                new OleDbParameter("@nom", categorie.Nom),
                                new OleDbParameter("@agemin", categorie.Min),
                                new OleDbParameter("@agemax", categorie.Max),
                                new OleDbParameter("@ageminf", categorie.Min),
                                new OleDbParameter("@agemaxf", categorie.Max)
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
                    throw new Exception("Catégorie non trouvée");
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

        public void Add(IInscription inscription)
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
                        var cmd = new OleDbCommand("INSERT INTO TPatineurs (Prenom, Nom, [Date de naissance], Sexe, Division, NoCategorie, NoClub, Classement, CategCalc, CodePat, Classement1000, Classement1500, ClassementGeneral, Classement2000, Classement2500) VALUES (@prenom, @nom, @dob, @sexe, 'Initiation', @nocat, @noclub, 999, 1, @codepat, 999, 999, 999, 999, 999);", conn);
                    cmd.Parameters.AddRange(new OleDbParameter[]
                    {
                        

                        new OleDbParameter("@prenom", inscription.FirstName.ToUpper()),
                        new OleDbParameter("@nom", inscription.LastName.ToUpper()),
                        new OleDbParameter("@dob", inscription.BirthDate.ToDateTime(new TimeOnly(0,0,0))),
                        new OleDbParameter("@sexe", inscription.Sex==IInscription.SexEnum.Male?"M":"F"),
                        new OleDbParameter("@nocat", nocat),
                        new OleDbParameter("@noclub", GetNoClub(inscription.Club)),
                        new OleDbParameter("@codepat", inscription.MemberNumber)
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
                var newcat = GetCategoryByDOB(patineur.BirthDate, patineur.Sex);
                if (patineur.NoCategory != newcat)
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

        public void Inscrire(Patineur patineur, Competition competition)
        {
            var idclub = GetNoClub(patineur.Club);

            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new OleDbCommand("INSERT INTO TPatineur_compe (NoCompetition, NoPatineur, Division, NoCategorie, NoClub, Rang, Groupe, Si_Regroup_Classement) VALUES (@nocompe, @nopat, 'Initiation', @nocat, @noclub, 0, 'Pas dans un groupe', 1)", conn);

                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("@nocompe", competition.Id),
                    new OleDbParameter("@nopat", patineur.NoPatineur),
                    new OleDbParameter("@nocat", patineur.NoCategory),
                    new OleDbParameter("@noclub", idclub)
                });

                cmd.ExecuteNonQuery();
            }
        }
    }
}
