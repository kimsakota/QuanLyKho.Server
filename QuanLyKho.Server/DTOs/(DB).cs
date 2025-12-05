
namespace System
{
    using BsonData;
    public static class DB
    {
        static public MainDatabase Main { get; private set; } = new MainDatabase("MainDB");
        static public void Start(string path)
        {
            Main.Connect(path);
        }
        static public void Stop() { Main.Disconnect(); }

        static Collection? _accounts;
        public static Collection Accounts => _accounts ?? (_accounts = Main.GetCollection("account"));
    }
}
