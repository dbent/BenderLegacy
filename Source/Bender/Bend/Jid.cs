using System;

namespace Bender.Bend
{
    // FIXME: BareJid from which Jid extends
    public sealed class Jid
    {
        public string Local { get; }
        public string Domain { get; }
        public string Resource { get; }

        public Jid Bare { get; private set; }

        private readonly string _string;

        public Jid(string local, string domain, string resource = null)
        {
            if (local == null)
            {
                throw new ArgumentNullException(nameof(local));
            }

            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            Local = local;
            Domain = domain;
            Resource = resource;

            Bare = Resource == null ? this : new Jid(Local, Domain);
            _string = Local == null ? Domain : (Resource == null ? $"{Local}@{Domain}" : $"{Local}@{Domain}/{Resource}");
        }

        public override string ToString()
        {
            return _string;
        }

        public static Jid Parse(string jid)
        {
            var split = SplitJid(jid);

            return new Jid(split.Item1, split.Item2, split.Item3);
        }

        private static Tuple<string, string, string> SplitJid(string jid)
        {
            string local = null;
            string domain;
            string resource = null;

            var atIndex = jid.IndexOf('@');
            var slashIndex = jid.IndexOf('/');

            int domainStartIndex;
            int domainEndIndex;

            if (atIndex >= 0 && slashIndex >= 0 && slashIndex < atIndex)
            {
                throw new Exception(); // TODO: More sepcific exception
            }

            if (atIndex == 0 || atIndex == jid.Length - 1)
            {
                throw new Exception(); // TODO: More specific exception
            }
            else if (atIndex > 0)
            {
                local = jid.Substring(0, atIndex);
                domainStartIndex = atIndex + 1;
            }
            else
            {
                domainStartIndex = 0;
            }

            if (slashIndex == 0 || slashIndex == jid.Length - 1)
            {
                throw new Exception(); // TODO: More specific exception
            }
            else if(slashIndex > 0)
            {
                resource = jid.Substring(slashIndex + 1);
                domainEndIndex = slashIndex - 1;
            }
            else
            {
                domainEndIndex = jid.Length - 1;
            }

            if (domainStartIndex <= domainEndIndex)
            {
                domain = jid.Substring(domainStartIndex, domainEndIndex - domainStartIndex + 1);
            }
            else
            {
                throw new Exception(); // TODO: More specific exception
            }

            return Tuple.Create(local, domain, resource);
        }
    }
}
