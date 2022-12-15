using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConfigPAT.Inscription;

namespace ConfigPAT
{
    public interface IInscription
    {
        public enum SexEnum
        {
            Male,
            Female
        }

        public string FirstName { get; }
        public string LastName { get; }
        public SexEnum Sex { get; }
        public DateOnly BirthDate { get; }
        public string? MemberNumber { get; }
        public string Club { get; }
    }
}
