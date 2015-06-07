namespace Bender.Persistence
{
    public interface IKeyValuePersistence
    {
        string Get(string key);
        void Set(string key, string value);
    }
}
