using fastJSON;
using System;

namespace WebUI
{
    public static class Json
    {
        public static T FromJson<T>(string json)
        {
            return JSON.ToObject<T>(json);
        }
        public static object ParseValue(this Type type,string json)
        {
            return JSON.ToObject(json,type);
        }
        public static string ToJson(this object ths)
        {
            return JSON.ToJSON(ths);
        }
    }
}