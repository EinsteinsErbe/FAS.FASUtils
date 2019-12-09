using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace FASUtils
{
    public class Connectable : Resource
    {
        public static Connectable INTERNET = new Internet();

        public string URL { get; protected set; }

        public Connectable(string URL, string Name) : base(Name)
        {
            this.URL = URL;
            SetCheckAction(() => (TryToConnect(), string.Empty));
        }

        protected virtual bool TryToConnect()
        {
            Ping ping = new Ping();
            bool success = false;
            string url = URL.Contains("//") ? URL.Substring(URL.IndexOf("//")).Trim('/') : URL;

            try
            {
                PingReply reply = ping.Send(url, Timeout);
                success = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {

            }
            finally
            {
                ping.Dispose();
            }
            return success;
        }
    }

    internal class Internet : Connectable
    {
        public Internet() : base(AzureURL.BASE, "Internet") { }

        protected override bool TryToConnect()
        {
            if (base.TryToConnect())
            {
                return true;
            }
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead(AzureURL.BASEURL))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
