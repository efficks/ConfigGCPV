using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCPVConfig.Minute
{
    public class Minuteur
    {
        public class MinuteParameters
        {
            public TimeOnly VerifLame { get; set; }
            public TimeOnly DebutRechauffements { get; set; }
            public TimeSpan DureeRechauffements { get; set; }
            public TimeOnly RencontreEntraineur { get; set; }
            public TimeOnly DebutCourse { get; set; }
            public TimeSpan DureeResurfacage { get; set; }
            public TimeSpan DureeDiner { get; set; }
            public int BlocAvantDiner { get; set; }
            public int SecondesParTour { get; set; }
            public int SecondesEntreeSortie { get; set; }
        }

        public IProgress<string>? ProgressMessage { get; set; }

        private FichierPAT mFichierPath;
        private FichierPAT.Competition mCompetition;
        private MinuteParameters mParameters;

        public Minuteur(FichierPAT pat, FichierPAT.Competition competition, MinuteParameters parameters)
        {
            mFichierPath = pat;
            mCompetition = competition;
            mParameters = parameters;
        }

        public bool Generate()
        {
            List<Event> evenements = new List<Event>();

            evenements.Add(
                new NoGroupEvent("Vérification des lames", mParameters.VerifLame, TimeSpan.Zero)
            );

            bool firstRechauf = true;
            foreach(FichierPAT.Course serie in mFichierPath.GetProgrammeCourse(mCompetition.Id))
            {
                if(!serie.EstQualif || serie.NoBloc!=1)
                {
                    break;
                }
                evenements.Add(
                    new EventGlace(
                        firstRechauf ? mParameters.DebutRechauffements : evenements.Last().Fin,
                        mParameters.DureeRechauffements,
                        "Échauffement",
                        serie.Groupe,
                        $"{serie.NbPatineur} patineurs",
                        ""
                    )
                );
                firstRechauf = false;
            }
            /*foreach(string groupeName in mFichierPath.GetGroupeByCompetition(mCompetition.Id))
            {
                evenements.Add(
                    new EventGlace(
                        firstRechauf?mParameters.DebutRechauffements:evenements.Last().Fin,
                        mParameters.DureeRechauffements,
                        "Échauffement",
                        groupeName,
                        "patineurs",
                        ""
                    )
                );
                firstRechauf = false;
            }*/

            evenements.Add(
                new NoGroupEvent("Réunion des entraîneurs / Resurfaçage", evenements.Last().Fin, TimeSpan.Zero)
            );

            int currentBloc = 1;
            bool estQualif = true;
            int course = 1;
            foreach(FichierPAT.Course serie in mFichierPath.GetProgrammeCourse(mCompetition.Id))
            {
                if(serie.NoBloc != currentBloc || estQualif != serie.EstQualif)
                {
                    if(course == mParameters.BlocAvantDiner)
                    {
                        evenements.Add(
                            new NoGroupEvent("Dîner / Resurfaçage", evenements.Last().Fin, mParameters.DureeDiner)
                        );
                    }
                    else
                    {
                        evenements.Add(
                            new NoGroupEvent("Resurfaçage", evenements.Last().Fin, mParameters.DureeResurfacage)
                        );
                    }
                    course++;
                }
                currentBloc = serie.NoBloc;
                estQualif = serie.EstQualif;

                int nbTours = serie.LongueurEpruve / 100;
                TimeSpan duree = TimeSpan.FromSeconds(
                    (mParameters.SecondesEntreeSortie * serie.NbVagues) +
                    (mParameters.SecondesParTour*serie.NbVagues*nbTours)
                );
                string nomEpreuve = serie.EstQualif ? "Qualification" : "Finale";
                evenements.Add(
                    new EventGlace(
                        firstRechauf ? mParameters.DebutRechauffements : evenements.Last().Fin,
                        duree,
                        $"{nomEpreuve} {serie.LongueurEpruve}m",
                        serie.Groupe,
                        $"{serie.NbVagues} vagues",
                        serie.CritereQualif
                    )
                );
            }

            evenements.Add(
                new NoGroupEvent("Fin approximative", evenements.Last().Fin, TimeSpan.Zero)
            );

            MinuteDocument document = new MinuteDocument(mCompetition, evenements);
            document.GeneratePdf("test.pdf");
            return true;
        }
    }
}
