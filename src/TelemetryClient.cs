using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace SimpleOps.GsxRamp
{
    internal sealed class TelemetryClient : ITelemetryClient
    {
        private readonly string _url;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public TelemetryClient(string url)
        {
            _url = url;
        }

        public TelemetrySnapshot GetSnapshot()
        {
            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "GET";
            request.Timeout = 3000;
            request.ReadWriteTimeout = 3000;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                var payload = _serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (payload == null)
                {
                    throw new InvalidOperationException("Telemetry payload was empty.");
                }

                return new TelemetrySnapshot
                {
                    Online = GetBool(payload, "online"),
                    Connected = GetBool(payload, "connected"),
                    OnGround = GetBool(payload, "onGround"),
                    Com1 = GetString(payload, "com1"),
                    Com2 = GetString(payload, "com2")
                };
            }
        }

        private static bool GetBool(IDictionary<string, object> payload, string key)
        {
            object value;
            if (!payload.TryGetValue(key, out value) || value == null)
            {
                return false;
            }

            if (value is bool) return (bool)value;
            if (value is string) return string.Equals((string)value, "true", StringComparison.OrdinalIgnoreCase);
            return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture) != 0d;
        }

        private static string GetString(IDictionary<string, object> payload, string key)
        {
            object value;
            if (!payload.TryGetValue(key, out value) || value == null)
            {
                return null;
            }

            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
