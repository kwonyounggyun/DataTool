using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataTool
{
    public interface IValue
    {
        public bool GetJson(ref string key, ref JObject obj);
    }

    public class TValue<T> : IValue
    {
        public TValue(T value)
        {
            _value = value;
        }

        public bool GetJson(ref string key, ref JObject obj)
        {
            obj.Add(new JProperty(key, _value));
            return true;
        }

        T _value;
    }

    public class Vec3Value : IValue
    {
        public Vec3Value(Vec3 value)
        {
            _value = value;
        }

        public bool GetJson(ref string key, ref JObject obj)
        {
            
            JObject vec = new JObject();
            vec.Add(new JProperty("x", _value.x));
            vec.Add(new JProperty("y", _value.y));
            vec.Add(new JProperty("z", _value.z));
            obj.Add(new JProperty(key, vec));
            return true;
        }

        Vec3 _value;
    }

    public class Vec2Value : IValue
    {
        public Vec2Value(Vec2 value)
        {
            _value = value;
        }

        public bool GetJson(ref string key, ref JObject obj)
        {

            JObject vec = new JObject();
            vec.Add(new JProperty("x", _value.x));
            vec.Add(new JProperty("y", _value.y));
            obj.Add(new JProperty(key, vec));
            return true;
        }

        Vec2 _value;
    }

    public class ListValue : IValue
    {
        public ListValue(List<int> value) { _value = value; }

        public bool GetJson(ref string key, ref JObject obj)
        {
            JArray jArray = new JArray();
            foreach(var v in _value)
                jArray.Add(v);
            obj.Add(new JProperty(key, jArray));
            return true;
        }

        List<int> _value;
    }
}
