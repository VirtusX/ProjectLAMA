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
                attrs[0].Value : null;
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
                attrs[0].Value : null;
        }
    }
    public enum PlayerTheme
    {
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
    public class PlayerProperty
    {
        public int Theme { get; set; }
        public double Volume { get; set; }
        public bool Start { get; set; }
        public string MusicLoaded { get; set; }
        public Extension FileExtension { get; set; }
        public bool HasPlaylist { get; set; }

        public PlayerProperty()
        {
            Theme = 0;
            Volume = 1;
            Start = false;
            MusicLoaded = null;
            FileExtension = 0;
            HasPlaylist = false;
        }

        public enum Extension
        {
            PLAYLIST = 0,
            TRACK = 1
        }


    }
    
}
