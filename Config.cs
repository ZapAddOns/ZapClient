namespace ZapClient
{
    public class Config
    {
        public string Server;
        public int Port;
        public string Username;
        public string Password;
        public bool Encrypted = false;

        public Config()
        {
        }

        public Config(Config config) 
        {
            Server = config.Server;
            Port = config.Port;
            Username = config.Username;
            Password = config.Password;
            Encrypted = config.Encrypted;
        }
    }
}
