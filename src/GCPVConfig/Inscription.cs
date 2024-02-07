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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExcelDataReader;

namespace GCPVConfig
{
    public class Inscription: IInscription
    {
        //First Name	Last Name	Sex	DOB	Calculated Age	Membership Numbers	Affiliates
        public Inscription(string firstName, string lastName, IInscription.SexEnum sex, DateOnly birthDate, string? memberNumber, string club)
        {
            FirstName = firstName;
            LastName = lastName;
            Sex = sex;
            BirthDate = birthDate;
            MemberNumber = memberNumber;
            Club = club;
            NoCasque = 0;
        }
        public string FirstName { get; }
        public string LastName { get; }
        public IInscription.SexEnum Sex { get; }
        public DateOnly BirthDate { get; }
        public string? MemberNumber { get; set; }
        public string Club { get; }

        public int NoCasque { get; set; }

        private static Regex REGEX_CLUB_ABRV = new Regex(@"^.+\((\w+)\)$");
        private static string? ExtractClubAbreviation(in string clubdata)
        {
            Match m = REGEX_CLUB_ABRV.Match(clubdata);
            if(m.Success)
            {
                return m.Groups[1].Value;
            }
            return null;
        }

        public static List<Inscription> LoadInscription(string path)
        {
            List<Inscription> inscriptions = new List<Inscription>();

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration()
                                        {
                                            // Gets or sets the encoding to use when the input XLS lacks a CodePage
                                            // record, or when the input CSV lacks a BOM and does not parse as UTF8. 
                                            // Default: cp1252 (XLS BIFF2-5 and CSV only)
                                            FallbackEncoding = Encoding.GetEncoding(1252)
                                         })
                )
                {
                    reader.Read();
                    Dictionary<string,int> headers = new Dictionary<string,int>();
                    for(int i=0; i<reader.FieldCount;i++)
                    {
                        string name = reader.GetString(i);
                        if(name is null)
                        {
                            break;
                        }
                        headers.Add(name.Trim(),i);
                    }

                    while (reader.Read())
                    {
                        string? nom = reader.GetString(headers["First Name"]);
                        if(nom == null)
                        {
                            continue;
                        }

                        string? aff = reader.GetString(headers["Affiliates"]);
                        string? club_abvr = null;
                        if (aff != null)
                        {
                            club_abvr = ExtractClubAbreviation(reader.GetString(headers["Affiliates"]).Trim());
                        }

                        var numberType = reader.GetFieldType(headers["Membership Numbers"]);
                        string memNumber = "";
                        if(numberType == typeof(string))
                        {
                            memNumber = reader.GetString(headers["Membership Numbers"]).Trim();
                        }
                        else if(numberType == typeof(double))
                        {
                            memNumber = reader.GetDouble(headers["Membership Numbers"]).ToString();
                        }

                        if (club_abvr != null)
                        {
                            Inscription inscription = new Inscription(
                                reader.GetString(headers["First Name"]).Trim(),
                                reader.GetString(headers["Last Name"]).Trim(),
                                reader.GetString(headers["Sex"]).Trim().ToLower() == "male" ? IInscription.SexEnum.Male : IInscription.SexEnum.Female,
                                DateOnly.FromDateTime(reader.GetDateTime(headers["DOB"])),
                                memNumber,
                                club_abvr
                            );
                            inscriptions.Add(inscription);
                        }
                        else
                        {
                            throw new Exception("Un patineur n'a pas de club affilié. Corrigez avant de continuer");
                        }
                    }
                }
            }

            return inscriptions;
        }
    }
}
