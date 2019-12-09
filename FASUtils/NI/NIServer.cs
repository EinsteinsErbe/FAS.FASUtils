using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace FASUtils.NI
{
    public class NIServer : Connectable
    {
        public AuthenticationHeaderValue NIAUTHHEADER;
        public string user;
        public string password;

        public NIServer(string url, string user, string password) : base(url, "NI SystemLink")
        {
            NIAUTHHEADER = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password)));
            URL = url.TrimEnd('/');
            this.user = user;
            this.password = password;
        }
    }
}