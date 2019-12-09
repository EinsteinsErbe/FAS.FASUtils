using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;

namespace FASUtils
{
    public class Azure
    {
        public static JsonRepo[] ListRepositories(AuthenticationHeaderValue header)
        {
            return JsonConvert.DeserializeObject<JsonRepos>(NetworkUtil.HttpGetString(AzureURL.REPOSURL, header)).value;
        }
        
        public static int SaveWorkitems(string path, AuthenticationHeaderValue header)
        {
            Directory.CreateDirectory(path);

            //List all workitem ids in batches of 200
            List<string> ids = new List<string>();
            int i = 0;
            StringBuilder sb = new StringBuilder();
            JsonWITID[] wis = JsonConvert.DeserializeObject<JsonWITIDs>(NetworkUtil.HttpPostString(AzureURL.WIQLSURL, header, "{\"query\": \"Select [System.Id] From WorkItems\"}")).workitems;
            foreach (JsonWITID wi in wis)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(wi.id);
                i = (i + 1) % 200;
                if (i == 0)
                {
                    ids.Add(sb.ToString());
                    sb.Clear();
                }
            }
            if (sb.Length > 0)
            {
                ids.Add(sb.ToString());
            }

            //Download workitems
            i = 0;
            foreach(string s in ids)
            {
                File.WriteAllText(Path.Combine(path, "workitems_" + (i++) + ".json"), NetworkUtil.HttpGetString(AzureURL.WORKITEMSURL + s, header));
            }
            return wis.Length;
        }
    }

    public class AzureURL
    {
        public const string HTTP = "https://";
        public const string BASE = "dev.azure.com";
        public const string BASEURL = HTTP+BASE+"/";
        public const string ORGANISATIONURL = BASEURL + "ABB-BDB-CHSEM/";
        public const string PROJECTURL = ORGANISATIONURL + "FAS/";
        public const string GITURL = PROJECTURL + "_git/";
        public const string REPOURL = PROJECTURL + "_apis/git/repositories/";
        public const string REPOSURL = PROJECTURL + "_apis/git/repositories?api-version=5.0";
        public const string WORKITEMSURL = PROJECTURL + "_apis/wit/workitems?$expand=all&ids=";
        public const string WIQLSURL = PROJECTURL + "_apis/wit/wiql?api-version=5.1";

        public static string GitCredentialUrl(string user, string password)
        {
            return GITURL.Insert(HTTP.Length, user + ':' + password + '@');
        }
    }

    internal class JsonRepos
    {
        public int count;
        public JsonRepo[] value;
    }

    public class JsonRepo
    {
        public string id;
        public string name;
    }

    public class JsonWITIDs
    {
        public JsonWITID[] workitems;
    }

    public class JsonWITID
    {
        public string id;
    }
}
