using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLama
{
    public class UriAttribute : Attribute
    {
        public readonly string Value;
        public UriAttribute(string value)
        {
            Value = value;
        }
        public static string Get(Enum val)
        {
            var type = val.GetType();
            var fi = type.GetField(val.ToString());
            return fi.GetCustomAttributes(typeof(UriAttribute), false) is UriAttribute[] attrs && attrs.Length > 0 ? 
                attrs[0].Value: null;
        }
    }
    public class ResourceAttribute : Attribute
    {
        public readonly string Value;
        public ResourceAttribute(string value)
        {
            Value = value;
        }
        public static string Get(Enum val)
        {
            var type = val.GetType();
            var fi = type.GetField(val.ToString());
            return fi.GetCustomAttributes(typeof(ResourceAttribute), false) is ResourceAttribute[] attrs && attrs.Length > 0 ? 
                attrs[0].Value: null;
        }
    }
    public enum PlayerTheme{
        [Uri(@"\Assets\LamaRose.png")]
        [Resource("RoseDictionary.xaml")]
        ROSE,
        [Uri(@"\Assets\LamaBlue.png")]
        [Resource("BlueDictionary.xaml")]
        BLUE,
        [Uri(@"\Assets\LamaRed.png")]
        [Resource("CrimsonDictionary.xaml")]
        RED,
        [Uri(@"\Assets\LamaGreen.png")]
        [Resource("GreenDictionary.xaml")]
        GREEN,
        [Uri(@"\Assets\LamaGray.png")]
        [Resource("GrayDictionary.xaml")]
        GRAY,
    }
    internal class Utilities
    {

    }
}
