using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;
using Timer = System.Timers.Timer;

namespace FASUtils.NI
{
    public class NIMessageClient : IDisposable
    {
        private WebSocket websocket;
        private ManualResetEvent gotResult;
        private JsonNIMailConfirmation result;
        private List<string> subscribtions;

        private Timer connectInterval;

        public bool Connected = false;

        public event EventHandler<MessageEventArgs> OnMessage;

        public NIMessageClient(NIServer niserver)
        {
            subscribtions = new List<string>();
            string url = "ws:";
            if (niserver.URL.Contains("//"))
            {
                url += niserver.URL.Substring(niserver.URL.IndexOf("//"));
            }
            else
            {
                url += "//" + niserver.URL;
            }
            url += "/nimessage/v1/websocket";

            connectInterval = new Timer(10000);
            connectInterval.AutoReset = true;
            connectInterval.Elapsed += (s, e) =>
            {
                Logger.Log("Reconnect...", this);
                websocket.Connect();
            };

            websocket = new WebSocket(url);
            websocket.SetCredentials(niserver.user, niserver.password, true);
            websocket.Compression = CompressionMethod.None;
            websocket.OnMessage += (s, e) => OnMessage?.Invoke(s, e);
            websocket.OnError += (s, e) => Logger.Error(e.Message, this);
            websocket.OnOpen += (s, e) =>
            {
                Logger.Log("Opened connection", this);
                connectInterval.Stop();
                Connected = true;

                //subscribe to all channels after reconnect
                foreach(string sub in subscribtions)
                {
                    websocket.Send(JsonConvert.SerializeObject(new JsonNISubscribe(sub)));
                }
            };
            websocket.OnClose += (s, e) =>
            {
                Logger.Log("Closed: " + e.Reason, this);
                Connected = false;
                connectInterval.Start();
            };

            websocket.Connect();
        }

        public void Send(string topic, string message)
        {
            websocket.Send(JsonConvert.SerializeObject(new JsonNIMessage(topic, message)));
        }

        public void Subscribe(string topic)
        {
            if (!subscribtions.Contains(topic))
            {
                subscribtions.Add(topic);
                websocket.Send(JsonConvert.SerializeObject(new JsonNISubscribe(topic)));
            }   
        }

        public void Unsubscribe(string topic)
        {
            if (subscribtions.Contains(topic))
            {
                subscribtions.Remove(topic);
                websocket.Send(JsonConvert.SerializeObject(new JsonNIUnsubscribe(topic)));
            }
        }

        public JsonNIMailConfirmation SendMail(string from, string subject, string body, params string[] recipients)
        {
            JsonNIMail mail = new JsonNIMail(from, subject, body, recipients);
            mail.uniqueId = JsonConvert.SerializeObject(mail).GetHashCode().ToString();
            result = mail.Validate();
            result.msg = "Timeout";
            Subscribe(mail.uniqueId);
            OnMessage += HandleMailMessage;
            Send("SendMail", JsonConvert.SerializeObject(mail));

            gotResult = new ManualResetEvent(false);
            gotResult.WaitOne(10000);
            Unsubscribe(mail.uniqueId);
            return result;
        }

        private void HandleMailMessage(object sender, MessageEventArgs e)
        {
            try
            {
                JsonNIMailConfirmation mail = JsonConvert.DeserializeObject<JsonNIMailConfirmation>(e.Data);
                if (mail.uniqueId == result.uniqueId)
                {
                    result = mail;
                    gotResult.Set();
                }
            }
            catch (Exception) { }
        }

        public static JsonNIMailConfirmation SendMail(NIServer niserver, string from, string subject, string body, StringCollection recipients)
        {
            return SendMail(niserver, from, subject, body, recipients.Cast<string>().ToArray());
        }

        public static JsonNIMailConfirmation SendMail(NIServer niserver, string from, string subject, string body, params string[] recipients)
        {
            using (NIMessageClient nimc = new NIMessageClient(niserver))
            {
                return nimc.SendMail(from, subject, body, recipients);
            }
        }

        public void Dispose()
        {
            websocket.Close();
        }
    }

    internal class JsonNIMessage
    {
        public string action;
        public string topic;
        public string message;

        internal JsonNIMessage(string topic, string message)
        {
            action = "publish";
            this.topic = topic;
            this.message = message;
        }
    }

    internal class JsonNISubscribe
    {
        public string action;
        public string topic;

        internal JsonNISubscribe(string topic)
        {
            action = "subscribe";
            this.topic = topic;
        }
    }

    internal class JsonNIUnsubscribe
    {
        public string action;
        public string topic;

        internal JsonNIUnsubscribe(string topic)
        {
            action = "unsubscribe";
            this.topic = topic;
        }
    }

    public class JsonNIMail
    {
        public string uniqueId;
        public string from;
        public string subject;
        public string body;
        public string[] recipients;

        internal JsonNIMail() { }

        public JsonNIMail(string from, string subject, string body, params string[] recipients)
        {
            this.from = from;
            this.subject = subject;
            this.body = body;
            this.recipients = recipients;
        }

        public JsonNIMailConfirmation Validate()
        {
            JsonNIMailConfirmation result = new JsonNIMailConfirmation(uniqueId);

            if (string.IsNullOrWhiteSpace(from))
            {
                from = "systemlink.noreply@ch.abb.com";
            }
            else
            {
                if (!from.EndsWith("@ch.abb.com"))
                {
                    result.valid = false;
                    result.msg += "Invalid sender: " + from + Environment.NewLine;
                }
            }
            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = "<no subject>";
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                subject = "<no body>";
            }
            if (recipients.Length == 0)
            {
                result.valid = false;
                result.msg += "No recipient" + Environment.NewLine;
            }
            foreach (string r in recipients)
            {
                if (!r?.EndsWith("@ch.abb.com") ?? true)
                {
                    result.valid = false;
                    result.msg += "Invalid recipient: " + r + Environment.NewLine;
                }
            }
            return result;
        }
    }

    public class JsonNIMailConfirmation
    {
        public string uniqueId;
        public bool ok = false;
        public bool valid = false;
        public string msg;

        internal JsonNIMailConfirmation() { }

        internal JsonNIMailConfirmation(string uniqueId)
        {
            this.uniqueId = uniqueId;
            ok = false;
            valid = true;
            if (string.IsNullOrWhiteSpace(uniqueId))
            {
                msg += "Invalid ID" + Environment.NewLine;
                valid = false;
            }
        }
    }
}
