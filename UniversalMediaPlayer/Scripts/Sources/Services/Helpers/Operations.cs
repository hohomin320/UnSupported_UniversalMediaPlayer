namespace UMP.Services.Helpers
{
    internal struct Operations
    {
        private string _reverse;
        private string _swap;
        private string _splice;

        public Operations(string reverse, string swap, string splice)
        {
            _reverse = reverse;
            _swap = swap;
            _splice = splice;
        }

        public string Reverse
        {
            get { return _reverse; }
        }

        public string Swap
        {
            get { return _swap; }
        }

        public string Splice
        {
            get { return _splice; }
        }
    }
}
