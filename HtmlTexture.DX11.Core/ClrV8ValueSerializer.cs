using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Chromium.Remote;

namespace VVVV.HtmlTexture.DX11.Core
{
    public static class ClrV8ValueSerializer
    {

        public static string GetExceptionDump(this CfrV8Exception e)
        {
            return e.Message + Environment.NewLine + e.ScriptResourceName + Environment.NewLine + e.LineNumber + " " + e.StartColumn;
        }

        public static string CfrV8ValueToString(this CfrV8Value val)
        {
            if (val.IsBool)
                return val.BoolValue.ToString(CultureInfo.InvariantCulture);
            else if (val.IsDouble)
                return val.DoubleValue.ToString(CultureInfo.InvariantCulture);
            else if (val.IsInt)
                return val.IntValue.ToString(CultureInfo.InvariantCulture);
            else if (val.IsUint)
                return val.UintValue.ToString(CultureInfo.InvariantCulture);
            else if (val.IsString)
                return val.StringValue;
            else if (val.IsArray)
            {
                string[] arr = new string[val.ArrayLength];
                for (int k = 0; k < val.ArrayLength; k++) arr[k] = CfrV8ValueToString(val.GetValue(k));
                return "[" + string.Join(",", arr) + "]";
            }
            else return "";
        }

        public static string[] CfrV8ValueToStringArray(this CfrV8Value val)
        {
            if (val.IsBool)
                return new []{ val.BoolValue.ToString(CultureInfo.InvariantCulture) };
            else if (val.IsDouble)
                return new []{ val.DoubleValue.ToString(CultureInfo.InvariantCulture) };
            else if (val.IsInt)
                return new []{ val.IntValue.ToString(CultureInfo.InvariantCulture) };
            else if (val.IsUint)
                return new []{ val.UintValue.ToString(CultureInfo.InvariantCulture) };
            else if (val.IsString)
                return new []{ val.StringValue };
            else if (val.IsArray)
            {
                string[] arr = new string[val.ArrayLength];
                for (int k = 0; k < val.ArrayLength; k++) arr[k] = CfrV8ValueToString(val.GetValue(k));
                return arr;
            }
            else return new string[0];
        }

        public static CfrV8Value V8Serialize(this object o)
        {
            switch (o)
            {
                case null:
                    return CfrV8Value.CreateNull();
                case string os:
                    return CfrV8Value.CreateString(os);
                case int oni:
                    return CfrV8Value.CreateInt(oni);
                case uint onui:
                    return CfrV8Value.CreateUint(onui);
                case double ond:
                    return CfrV8Value.CreateDouble(ond);
                case TimeSpan ots:
                    return CfrV8Value.CreateDate(new CfrTime()
                    {
                        Year = 0,
                        Month = 0,
                        DayOfMonth = ots.Days,
                        DayOfWeek = ots.Days,
                        Hour = ots.Hours,
                        Minute = ots.Minutes,
                        Second = ots.Seconds,
                        Millisecond = ots.Milliseconds
                    });
                case DateTime odt:
                    return CfrV8Value.CreateDate(new CfrTime()
                    {
                        Year = odt.Year,
                        Month = odt.Month,
                        DayOfMonth = odt.Day,
                        DayOfWeek = (int)odt.DayOfWeek,
                        Hour = odt.Hour,
                        Minute = odt.Minute,
                        Second = odt.Second,
                        Millisecond = odt.Millisecond
                    });

                case IEnumerable oenum:
                    var reslist = new List<CfrV8Value>();
                    foreach (var obj in oenum)
                    {
                        reslist.Add(obj.V8Serialize());
                    }
                    var res = CfrV8Value.CreateArray(reslist.Count);
                    for (int i = 0; i < reslist.Count; i++)
                    {
                        res.SetValue(i, reslist[i]);
                    }

                    return res;
                default:
                    var oT = o.GetType();
                    if (oT.IsClass || (oT.IsValueType && !oT.IsPrimitive))
                    {
                        var accessor = new CfrV8Accessor();
                        accessor.Get += (sender, e) =>
                        {
                            var prop = oT.GetProperty(e.Name);
                            var field = oT.GetField(e.Name);
                            if (prop == null && field == null)
                            {
                                e.Retval = CfrV8Value.CreateUndefined();
                                return;
                            }
                            if (prop != null)
                                e.Retval = prop.GetValue(o).V8Serialize();
                            else
                                e.Retval = field.GetValue(o).V8Serialize();
                        };
                        return CfrV8Value.CreateObject(new CfrV8Accessor());
                    }
                    else
                    {
                        return CfrV8Value.CreateNull();
                    }
            }
        }
    }
}
