using Newtonsoft.Json;
using System;

namespace WebUI
{
    public static class Json
    {
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        public static object ParseValue(this Type type, string json)
        {
            return JsonConvert.DeserializeObject(json, type);
        }
        public static string ToJson(this object ths)
        {
            return JsonConvert.SerializeObject(ths);
        }
    }
}