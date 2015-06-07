using System;

namespace Bender.Apis.LastFm
{
    public class LastFmMethodNameAttribute : Attribute
    {
        public string Value { get; private set; }

        public LastFmMethodNameAttribute(string value)
        {
            Value = value;
        }
    }
}
