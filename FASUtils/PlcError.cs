using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FASUtils
{
    public class PlcError
    {
        public const string DIALOGPATH = @"V:\ProdSysteme\MarliS_85\System\Dialog Files";

        public const string PREFIX = "PLC.";

        private static Dictionary<string, string> EMPTY = new Dictionary<string, string>();

        //private Dictionary<string, string>[] errors;
        private Dictionary<string, Dictionary<string, string>>[] errors;

        public string[] LoadErrors(string path = DIALOGPATH)
        {
            Language[] languages = (Language[])Enum.GetValues(typeof(Language));
            errors = new Dictionary<string, Dictionary<string, string>>[languages.Length];
            for (int i = 0; i < errors.Length; i++)
            {
                errors[i] = new Dictionary<string, Dictionary<string, string>>();
            }

            List<string> keys = new List<string>();

            foreach (string file in Directory.GetFiles(path, "Dialog.Plc_*.csv"))
            {
                List<string> lines = new List<string>();
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.GetEncoding(1252)))
                {
                    string l = "";
                    while ((l = sr.ReadLine()) != null)
                    {
                        lines.Add(l);
                    }
                }
                string[] headers = lines[0].Split(';');

                int lheader = Array.IndexOf(headers, "Sprache");

                foreach (string line in lines)
                {
                    if (line.StartsWith(PREFIX))
                    {

                        string[] rows = line.Split(';');
                        Language l;
                        if (Enum.TryParse(rows[lheader], true, out l))
                        {
                            string key = rows[0].Substring(PREFIX.Length).ToLower();
                            if (!errors[(int)l].Keys.Contains(key))
                            {
                                Dictionary<string, string> data = new Dictionary<string, string>();

                                for (int i = 0; i < headers.Length; i++)
                                {
                                    try
                                    {
                                        data.Add(headers[i], rows[i]);
                                    }
                                    catch (Exception) { }
                                }
                                errors[(int)l].Add(key.ToLower(), data);
                            }
                        }
                    }
                }
            }
            foreach (Dictionary<string, Dictionary<string, string>> err in errors)
            {
                keys.AddRange(err.Keys);
            }

            return keys.ToArray();
        }

        public string GetErrorString(string code, Language l)
        {
            string key = code.ToLower();
            if (errors[(int)l].ContainsKey(key))
            {
                Dictionary<string, string> dic = errors[(int)l][key];
                StringBuilder sb = new StringBuilder();
                foreach (string k in dic.Keys)
                {
                    sb.AppendLine(k + ": " + dic[k]);
                }

                return sb.ToString();
            }
            else
            {
                return (l.Equals(Language.DEUTSCH) ? "Kein Eintrag gefunden" : "No entry found");
            }
        }

        public Dictionary<string, string> GetError(string code, Language l)
        {
            string key = code.ToLower();
            return errors[(int)l].ContainsKey(key) ? errors[(int)l][key] : EMPTY;
        }
    }
}
