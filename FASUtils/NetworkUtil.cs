using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace FASUtils
{
    public class NetworkUtil
    {
        public static string HttpDelete(string uri, AuthenticationHeaderValue auth)
        {
            Logger.Debug("Delete: " + uri, "HTTP");
            string result = "";
            // use the httpclient
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = auth;

                // connect to the REST endpoint        
                HttpResponseMessage response = client.DeleteAsync(uri).Result;

                // check to see if we have a succesfull respond
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    Logger.Debug(response.StatusCode + ": " + response.ReasonPhrase, "HTTP failure");
                }
            }

            return result;
        }

        public static string HttpGetString(string uri, AuthenticationHeaderValue auth)
        {
            Logger.Debug("Get from: " + uri, "HTTP");
            string result = "";
            // use the httpclient
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = auth;

                // connect to the REST endpoint        
                HttpResponseMessage response = client.GetAsync(uri).Result;

                // check to see if we have a succesfull respond
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    Logger.Debug(response.StatusCode + ": " + response.ReasonPhrase, "HTTP failure");
                }
            }

            return result;
        }

        public static string HttpPostString(string uri, AuthenticationHeaderValue auth, string data)
        {
            Logger.Debug("Post to: " + uri + "\tData: " + data, "HTTP");
            string result = "";
            // use the httpclient
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = auth;

                // connect to the REST endpoint        
                HttpResponseMessage response = client.PostAsync(uri, new StringContent(data, Encoding.UTF8, "application/json")).Result;

                // check to see if we have a succesfull respond
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    Logger.Debug(response.StatusCode + ": " + response.ReasonPhrase, "HTTP failure");
                }
            }

            return result;
        }


        public static string HttpPutString(string uri, AuthenticationHeaderValue auth, string data)
        {
            Logger.Debug("Put to: " + uri + "\tData: " + data, "HTTP");
            string result = "";
            // use the httpclient
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = auth;

                // connect to the REST endpoint        
                HttpResponseMessage response = client.PutAsync(uri, new StringContent(data, Encoding.UTF8, "application/json")).Result;

                // check to see if we have a succesfull respond
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    Logger.Debug(response.StatusCode + ": " + response.ReasonPhrase, "HTTP failure");
                }
            }

            return result;
        }

        public static void HttpDownload(string uri, string path, AuthenticationHeaderValue auth)
        {
            Logger.Debug("Download from: " + uri, "HTTP");
            // use the httpclient
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = auth;

                // connect to the REST endpoint        
                HttpResponseMessage response = client.GetAsync(uri).Result;

                // check to see if we have a succesfull respond
                if (response.IsSuccessStatusCode)
                {
                    Stream stream = response.Content.ReadAsStreamAsync().Result;
                    using (var fileStream = File.Create(path))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(fileStream);
                    }
                    stream.Close();
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    Logger.Debug(response.StatusCode + ": " + response.ReasonPhrase, "HTTP failure");
                }
            }
        }

        public static void SetStaticIP(string ipAddress, string adapterName = "Ethernet")
        {
            ProcessStartInfo process = new ProcessStartInfo();
            process.WindowStyle = ProcessWindowStyle.Hidden;
            process.FileName = "cmd.exe";
            process.Arguments = "/C netsh interface ipv4 set address name=\"" + adapterName + "\" static \"" + ipAddress + "\" \"255.255.255.0\"";
            process.Verb = "runas";
            Process.Start(process).WaitForExit();
        }

        public static void SetDHCP(string adapterName = "Ethernet")
        {
            ProcessStartInfo process = new ProcessStartInfo();
            process.WindowStyle = ProcessWindowStyle.Hidden;
            process.FileName = "cmd.exe";
            process.Arguments = "/C netsh interface ipv4 set address name=\"" + adapterName + "\" source=dhcp";
            process.Verb = "runas";
            Process.Start(process).WaitForExit();
        }
    }
}
