namespace Zwietracht
{
    public class WSQueryString
    {
        Dictionary<string, string> _queryString = new Dictionary<string, string>();
        public WSQueryString(string[] args)
        {
            foreach (string arg in args)
            {
                string[] split = arg.Split('=');
                if (split.Length != 2) continue;
                _queryString[split[0]] = split[1];
            }
        }

        public string Get(string name)
        {
            return _queryString.ContainsKey(name) ? _queryString[name] : null;
        }
    }
}