using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Chromium;
using Chromium.Remote;
using SlimDX;
using VVVV.Utils.VColor;

namespace Vanadium.Core
{
    public static class ClrV8ValueSerializer
    {
        public static CfrV8Accessor Accessor;

        public static string GetExceptionDump(this CfrV8Exception e)
        {
            if (e == null) return "No Error?";
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

        public static CfrV8Value V8Serialize(this object o, HashSet<object> refin = null)
        {
            if(Accessor == null) Accessor = new CfrV8Accessor();
            var propattr = CfxV8PropertyAttribute.DontDelete | CfxV8PropertyAttribute.ReadOnly;
            switch (o)
            {
                case null:
                    return CfrV8Value.CreateNull();
                case bool onb:
                    return CfrV8Value.CreateBool(onb);
                case string os:
                    return CfrV8Value.CreateString(os);
                case Enum oenum:
                    return CfrV8Value.CreateString(oenum.ToString());
                case int oni:
                    return CfrV8Value.CreateInt(oni);
                case sbyte oni:
                    return CfrV8Value.CreateInt(oni);
                case short oni:
                    return CfrV8Value.CreateInt(oni);
                case long oni:
                    return CfrV8Value.CreateInt((int)oni);
                case uint onui:
                    return CfrV8Value.CreateUint(onui);
                case byte onui:
                    return CfrV8Value.CreateUint(onui);
                case ushort onui:
                    return CfrV8Value.CreateUint(onui);
                case double ond:
                    return CfrV8Value.CreateDouble(ond);
                case float onf:
                    return CfrV8Value.CreateDouble(onf);

                case ObjectBuilder oob:
                    return oob.Serialize(refin);

                case RGBAColor ocv:
                    var vcolres = CfrV8Value.CreateObject(Accessor);
                    vcolres.SetValue("r", CfrV8Value.CreateInt(ocv.Color.R), propattr);
                    vcolres.SetValue("g", CfrV8Value.CreateInt(ocv.Color.G), propattr);
                    vcolres.SetValue("b", CfrV8Value.CreateInt(ocv.Color.B), propattr);
                    vcolres.SetValue("a", CfrV8Value.CreateDouble(ocv.A), propattr);
                    vcolres.SetValue("name", CfrV8Value.CreateString("#" + ocv.Color.Name.Remove(0, 2)), propattr);
                    return vcolres;
                case Color4 ocdx:
                    var dxcolres = CfrV8Value.CreateObject(Accessor);
                    dxcolres.SetValue("r", CfrV8Value.CreateDouble(ocdx.Red * 255), propattr);
                    dxcolres.SetValue("g", CfrV8Value.CreateDouble(ocdx.Green * 255), propattr);
                    dxcolres.SetValue("b", CfrV8Value.CreateDouble(ocdx.Blue * 255), propattr);
                    dxcolres.SetValue("a", CfrV8Value.CreateDouble(ocdx.Alpha), propattr);
                    dxcolres.SetValue("name", CfrV8Value.CreateString(ocdx.ToString()), propattr);
                    return dxcolres;

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
                    lock (oenum)
                    {
                        var reslist = (from object obj in oenum select obj.V8Serialize()).ToList();
                        var res = CfrV8Value.CreateArray(reslist.Count);
                        for (int i = 0; i < reslist.Count; i++)
                        {
                            res.SetValue(i, reslist[i]);
                        }
                        return res;
                    }

                default:
                    var referenced = refin;
                    if (referenced != null && referenced.Contains(o)) return CfrV8Value.CreateNull();
                    else if (referenced == null)
                    {
                        referenced = new HashSet<object> {o};
                    }
                    else
                    {
                        referenced.Add(o);
                    }

                    var oT = o.GetType();
                    if (oT.IsClass || (oT.IsValueType && !oT.IsPrimitive))
                    {
                        var reso = CfrV8Value.CreateObject(Accessor);
                        foreach (var prop in oT.GetProperties().Where(p =>
                            p.CanRead &&
                            p.GetMethod.IsPublic &&
                            !p.GetMethod.IsStatic &&
                            !p.PropertyType.IsPointer)
                        )
                        {
                            var cobj = prop.GetValue(o).V8Serialize(referenced);
                            reso.SetValue(prop.Name, cobj, propattr);
                        }
                        foreach (var field in oT.GetFields().Where(f =>
                            f.IsPublic &&
                            !f.IsStatic &&
                            !f.FieldType.IsPointer)
                        )
                        {
                            var cobj = field.GetValue(o).V8Serialize(referenced);
                            reso.SetValue(field.Name, cobj, propattr);
                        }
                        return reso;
                    }
                    else
                    {
                        return CfrV8Value.CreateNull();
                    }
            }
        }
    }
}
