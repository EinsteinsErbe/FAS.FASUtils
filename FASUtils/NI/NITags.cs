using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace FASUtils.NI
{
    public enum NITagType
    {
        INT, U_INT64, DOUBLE, STRING, DATE_TIME, BOOLEAN
    }

    public enum NITagRetention
    {
        NONE, COUNT, DURATION, PERMANENT
    }

    public class NITags
    {
        public string BASEURL;
        public string TAGSBASEURL;
        public string TAGSVALUES;
        public string GETTAGVALUE;
        public string TAGS;
        public string TAGSPATH;
        public string TAGSHISTORY;
        public string TAGSHISTORYTAG;

        public const string BOOT = ".Boot";
        public const string BOOTEXPECTED = ".BootExpected";
        public const string SEDATE = ".DoP";

        private AuthenticationHeaderValue NIAUTHHEADER;

        public NITags(NIServer niserver)
        {
            Init(niserver.URL);
            NIAUTHHEADER = niserver.NIAUTHHEADER;
        }

        public NITags(string url, string user, string password)
        {
            Init(url.TrimEnd('/'));
            NIAUTHHEADER = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password)));
        }

        private void Init(string url)
        {
            BASEURL = url + '/';
            TAGSBASEURL = BASEURL + "nitag/v2/";
            TAGSVALUES = TAGSBASEURL + "tags-with-values?path=*";
            GETTAGVALUE = TAGSBASEURL + "tags-with-values/";
            TAGS = TAGSBASEURL + "tags";
            TAGSPATH = TAGSBASEURL + "tags?path=";
            TAGSHISTORY = BASEURL + "nitaghistorian/v1/tags/query-history";
            TAGSHISTORYTAG = BASEURL + "nitaghistorian/v1/tags/history/";
        }

        public void Activate(NITag t, bool state)
        {
            NetworkUtil.HttpPostString(TAGS, NIAUTHHEADER, "{ \"type\": \"" + t.type + "\", \"path\": \"" + t.path + "\", \"properties\":{\"active\":\"" + state + "\"}}");
        }

        public void ActivateHistorian(string path, string id, bool state)
        {
            NetworkUtil.HttpPostString(TAGSHISTORYTAG + path + "/update-values", NIAUTHHEADER, "{\"values\": [{ \"id\": \"" + id + "\", \"properties\":{\"active\":\"" + state + "\"}}]}");
        }

        public List<T> GetTagHistory<T>(string path, DateTime start) where T : NITagValue
        {
            List<T> history = new List<T>();

            string data = "{\"paths\": [ \"" + path + "\" ],\"startTime\": \"" + new DateTimeOffset(start).UtcDateTime.ToString("s") + "\",\"take\": 10000,\"decimation\": 0}";
            string tagJson = NetworkUtil.HttpPostString(TAGSHISTORY, NIAUTHHEADER, data);

            if (!string.IsNullOrWhiteSpace(tagJson))
            {
                JsonNIHistoryTags tags = JsonConvert.DeserializeObject<JsonNIHistoryTags>(tagJson);
                if (tags.values.Values.Count() > 0)
                {
                    foreach (var t in tags.values.Values.First())
                    {
                        history.Add(Activator.CreateInstance(typeof(T), t.value, t.timestamp, new NITag(t.id, path, t.properties?.active ?? false, NITagType.INT)) as T);
                    }
                }
            }
            return history;
        }

        public List<NITag> GetTags(string path)
        {
            List<NITag> tags = new List<NITag>();

            string tagJson = NetworkUtil.HttpGetString(TAGSPATH + path, NIAUTHHEADER);
            if (!string.IsNullOrWhiteSpace(tagJson))
            {
                JsonNITags ts = JsonConvert.DeserializeObject<JsonNITags>(tagJson);

                foreach (JsonNITag t in ts.tags)
                {
                    tags.Add(new NITag(t));
                }
            }
            return tags;
        }

        public NITagValue GetTag(string path)
        {
            string tagJson = NetworkUtil.HttpGetString(GETTAGVALUE + path, NIAUTHHEADER);
            if (!string.IsNullOrWhiteSpace(tagJson))
            {
                JsonNITagWrap tag = JsonConvert.DeserializeObject<JsonNITagWrap>(tagJson);
                if(tag.tag != null)
                {
                    return new NITagValue(tag.GetValue(), tag.GetTimestamp(), new NITag(tag.tag));
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public string GetTagValue(string path)
        {
            string tagJson = NetworkUtil.HttpGetString(GETTAGVALUE + path, NIAUTHHEADER);
            if (!string.IsNullOrWhiteSpace(tagJson))
            {
                JsonNITagWrap tag = JsonConvert.DeserializeObject<JsonNITagWrap>(tagJson);
                return tag.GetValue();
            }
            return string.Empty;
        }

        public void Create(string path, NITagType type, bool permanent = false)
        {
            NITag tag = new NITag(path, type);
            if (permanent)
            {
                tag.nitagRetention = NITagRetention.PERMANENT;
            }
            Create(tag);
        }

        public void Create(NITag tag)
        {
            NetworkUtil.HttpPostString(TAGS, NIAUTHHEADER, JsonConvert.SerializeObject(new JsonNITag(tag)));
        }

        public string CreateOrGetTag(string path, NITagType type, bool permanent)
        {
            Create(path, type, permanent);
            string tagJson = NetworkUtil.HttpGetString(GETTAGVALUE + path, NIAUTHHEADER);
            JsonNITagWrap tag = JsonConvert.DeserializeObject<JsonNITagWrap>(tagJson);
            return tag.GetValue();
        }

        public void CreateOrUpdateTag(string path, string value, NITagType type, bool permanent)
        {
            Create(path, type, permanent);
            UpdateTag(path, value, type);
        }

        public void UpdateTag(string path, string value, NITagType type)
        {
            NetworkUtil.HttpPutString(TAGS + "/" + path + "/values/current", NIAUTHHEADER, "{ \"value\": { \"type\": \"" + type + "\", \"value\": \"" + value + "\" }}");
        }

        public void UpdateTagMultipleValues(string path, NITagType type, List<NITagValue> values)
        {
            string data = "[";
            foreach (NITagValue value in values)
            {
                data += "{ \"value\": { \"type\": \"" + type + "\", \"value\": \"" + value.value + "\" }, \"timestamp\":\"" + value.timestamp + "\"},";
            }
            data += "]";
            NetworkUtil.HttpPostString(TAGS + "/" + path + "/update-values", NIAUTHHEADER, data);
        }

        public Dictionary<string, List<DateTag>> GetDateTags(int mothsTillWarn)
        {
            Dictionary<string, List<DateTag>> dateTags = new Dictionary<string, List<DateTag>>();

            string tagsJson = NetworkUtil.HttpGetString(TAGSVALUES + SEDATE, NIAUTHHEADER);
            JsonNITagsWithValue tags = JsonConvert.DeserializeObject<JsonNITagsWithValue>(tagsJson);

            foreach (JsonNITagWrap tag in tags.tagsWithValues)
            {
                string val = tag.GetValue();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    tag.CutName(SEDATE);
                    string[] names = tag.name.Split('.');

                    string name = names.First();

                    if (!dateTags.ContainsKey(name))
                    {
                        dateTags.Add(name, new List<DateTag>());
                    }

                    dateTags[name].Add(new DateTag(tag.tag, name, names.Last(), DateTime.Parse(val), mothsTillWarn));
                }
            }

            return dateTags;
        }

        public List<BootTag> GetBootTags()
        {
            List<BootTag> boots = new List<BootTag>();

            string tagsJson = NetworkUtil.HttpGetString(TAGSVALUES + BOOT, NIAUTHHEADER);
            JsonNITagsWithValue tags = JsonConvert.DeserializeObject<JsonNITagsWithValue>(tagsJson);

            foreach (JsonNITagWrap tag in tags.tagsWithValues)
            {
                tag.CutName(BOOT);
            }

            string tags2Json = NetworkUtil.HttpGetString(TAGSVALUES + BOOTEXPECTED, NIAUTHHEADER);
            JsonNITagsWithValue tags2 = JsonConvert.DeserializeObject<JsonNITagsWithValue>(tags2Json);

            foreach (JsonNITagWrap tag in tags2.tagsWithValues)
            {
                tag.CutName(BOOTEXPECTED);

                BootTag bt = new BootTag(tag.name, tag.tag);
                bt.expected = tag.GetValue();

                var actual = tags.tagsWithValues.Where(t => t.name.Equals(tag.name));

                if (actual.Count() > 0)
                {
                    bt.complete = true;
                    bt.actual = actual.First().GetValue();
                }

                boots.Add(bt);
            }
            return boots;
        }
    }

    public class NITag
    {
        public NITagType type;
        public string name;
        public NITagRetention nitagRetention = NIPropertyDefaults.nitagRetention;
        public int nitagHistoryTTLDays = NIPropertyDefaults.nitagHistoryTTLDays;
        public int nitagMaxHistoryCount = NIPropertyDefaults.nitagMaxHistoryCount;
        public bool active = NIPropertyDefaults.active;
        public string path;
        public Dictionary<string, string> properties;
        public string[] keywords;

        internal NITag()
        {
            keywords = new string[0];
            properties = new Dictionary<string, string>();
        }

        public NITag(string name, string path, bool active, NITagType type) : this()
        {
            this.name = name;
            this.path = path;
            this.active = active;
            this.type = type;
        }

        public NITag(string path, NITagType type) : this(path, path, false, type) { }

        internal NITag(JsonNITag tag) : this(tag.path, tag.type.ToEnum(NITagType.STRING))
        {
            if(keywords != null)
            {
                keywords = tag.keywords;
            }

            //Parse common properties
            if (tag.properties != null)
            {
                properties = tag.properties;
                if (properties.ContainsKey("active"))
                {
                    active = bool.Parse(properties["active"]);
                    properties.Remove("active");
                }
                if (properties.ContainsKey("nitagRetention"))
                {
                    nitagRetention = properties["nitagRetention"].ToEnum(NITagRetention.NONE);
                    properties.Remove("nitagRetention");
                }
                if (properties.ContainsKey("nitagHistoryTTLDays"))
                {
                    nitagHistoryTTLDays = int.Parse(properties["nitagHistoryTTLDays"]);
                    properties.Remove("nitagHistoryTTLDays");
                }
                if (properties.ContainsKey("nitagMaxHistoryCount"))
                {
                    nitagMaxHistoryCount = int.Parse(properties["nitagMaxHistoryCount"]);
                    properties.Remove("nitagMaxHistoryCount");
                }
            }
        }
    }

    public class BootTag : NITag
    {
        public string actual;
        public string expected;
        public bool complete = false;
        public bool deprecated { get { return actual != expected; } }

        internal BootTag(string name, JsonNITag tag) : base(tag)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2} {3}{4}", name, expected, actual, complete ? (deprecated ? "[DEPRECATED]" : "") : "[INCOMPLETE]", active ? "[ACTIVE]" : "");
        }

        public string ToShortString()
        {
            return string.Format("{0}:{1}:{2}", name, expected, actual);
        }
    }

    public class DateTag : NITag
    {
        public string se;
        public DateTime date;
        public int lifespan = NIPropertyDefaults.lifespan;
        public bool isDue;

        internal DateTag(JsonNITag tag, string name, string se, DateTime date, int mothsTillWarn) : base(tag)
        {
            this.name = name;
            this.se = se;
            if (properties.ContainsKey("lifespan"))
            {
                int.TryParse(properties["lifespan"], out lifespan);
            }
            this.date = date.AddYears(lifespan);
            isDue = this.date.AddMonths(-mothsTillWarn).CompareTo(DateTime.Today) < 0;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}:{3}y {4}{5}", name, se, date.ToShortDateString(), lifespan, isDue ? "[DUE]" : "", active ? "[ACTIVE]" : "");
        }

        public string ToShortString()
        {
            return string.Format("{0}:{1}:{2}", name, se, date.ToShortDateString());
        }
    }

    public class NITagValue
    {
        public string value;
        public DateTime timestamp = DateTime.MinValue;
        public NITag tag;

        public NITagValue(string value, string timestamp) : this(value, timestamp, null) { }

        public NITagValue(string value, string timestamp, NITag tag)
        {
            this.value = value;
            this.tag = tag;
            DateTime.TryParse(timestamp, out this.timestamp);
        }
    }

    public class NITagValueInt : NITagValue
    {
        public int valueInt;

        public NITagValueInt(string value, string timestamp, NITag tag) : base(value, timestamp, tag)
        {
            valueInt = int.Parse(value);
        }

        public NITagValueInt(string value, string timestamp) : this(value, timestamp, null) { }
    }

    internal class JsonNIHistoryTags
    {
        public int totalCount;
        public Dictionary<string, JsonNIHistoryTagValue[]> values;
    }

    internal class JsonNIHistoryTagValue
    {
        public string id;
        public string value;
        public string timestamp;
        public JsonNIHistoryProperties properties;
    }

    internal class JsonNITagsWithValue
    {
        public int totalCount;
        public JsonNITagWrap[] tagsWithValues;
    }

    internal class JsonNITags
    {
        public int totalCount;
        public JsonNITag[] tags;
    }

    internal class JsonNITagWrap
    {
        public JsonNITag tag;
        public string name;
        public JsonNICurrentValue current;

        public string GetValue()
        {
            return current?.value.value ?? "";
        }

        public string GetTimestamp()
        {
            return current?.timestamp ?? "";
        }

        public void CutName(string end)
        {
            name = tag.path.Substring(0, tag.path.Length - end.Length);
        }
    }

    internal class JsonNITag
    {
        public string path;
        public string type;
        public Dictionary<string, string> properties;
        public string[] keywords;

        public JsonNITag() { }

        public JsonNITag(NITag tag)
        {
            path = tag.path;
            type = tag.type.ToString();
            keywords = tag.keywords;
            properties = tag.properties;
            properties.Add("nitagRetention", tag.nitagRetention.ToString());

            switch (tag.nitagRetention)
            {
                case NITagRetention.COUNT:
                    properties.Add("nitagMaxHistoryCount", tag.nitagMaxHistoryCount.ToString());
                    break;
                case NITagRetention.DURATION:
                    properties.Add("nitagHistoryTTLDays", tag.nitagHistoryTTLDays.ToString());
                    break;
            }

            if (tag.active)
            {
                properties.Add("active", tag.active.ToString());
            }
        }
    }

    internal class JsonNIHistoryProperties
    {
        public bool active = false;
    }

    internal class JsonNICurrentValue
    {
        public JsonNIValue value;
        public string timestamp;
    }

    internal class JsonNIValue
    {
        public string value;
        public string type;
    }


    internal class NIPropertyDefaults
    {
        public const NITagRetention nitagRetention = NITagRetention.NONE;
        public const int nitagHistoryTTLDays = 30;
        public const int nitagMaxHistoryCount = 10000;
        public const bool active = false;
        public const int lifespan = 20;
    }
}
