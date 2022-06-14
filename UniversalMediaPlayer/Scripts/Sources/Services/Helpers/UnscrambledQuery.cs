namespace UMP.Services.Helpers
{
    internal struct UnscrambledQuery
    {
        private string _uri;
        private bool _encrypted;

        public UnscrambledQuery(string uri, bool encrypted)
        {
            _uri = uri;
            _encrypted = encrypted;
        }

        public string Uri
        {
            get { return _uri; }
        }

        public bool IsEncrypted
        {
            get { return _encrypted; }
        }
    }
}
