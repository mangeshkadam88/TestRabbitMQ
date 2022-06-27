using Entities;
using System.Text;

namespace TestRabbitMQ.Models
{
    public static class Helpers
    {
        private static Random gen = new Random();

        public static DateTime RandomDay()
        {
            DateTime start = new DateTime(2022, 6, 20);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }

        public static List<Hashes> GetRandomHashes()
        {
            List<Hashes> _list = new List<Hashes>();
            for (int i = 0; i < 40000; i++)
            {
                Hashes _hash = new Hashes();
                _hash.id = Guid.NewGuid();
                _hash.date = RandomDay();
                _hash.sha1 = GetSha1(Guid.NewGuid().ToString());
                _list.Add(_hash);
            }
            return _list;
        }

        public static string GetSha1(string input)
        {
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha1.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
