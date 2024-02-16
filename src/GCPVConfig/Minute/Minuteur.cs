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

        /*public List<Course> GetCourses()
        {

        }*/

        public bool Generate()
        {
            List<Event> evenements = new List<Event>();

            evenements.Add(
                new VerifLame(mParameters.VerifLame, TimeSpan.Zero)
            );

            foreach(string groupeName in mFichierPath.GetGroupeByCompetition(mCompetition.Id))
            {
                evenements.Add(
                    new EventGlace(
                        evenements.Last().Fin,
                        mParameters.DureeRechauffements,
                        "Échauffement",
                        groupeName,
                        "patineurs",
                        ""
                    )
                );
            }

            MinuteDocument document = new MinuteDocument(mCompetition, evenements);
            document.GeneratePdf("test.pdf");
            return true;
        }
    }
}
