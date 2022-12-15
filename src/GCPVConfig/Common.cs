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
