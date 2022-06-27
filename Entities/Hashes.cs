using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class Hashes
    {
        [Key]
        public Guid id { get; set; }
        public DateTime date { get; set; }
        public string sha1 { get; set; }
    }
}