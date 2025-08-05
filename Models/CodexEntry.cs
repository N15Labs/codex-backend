using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; 

namespace CodexBackend.Models
{
    public class CodexEntry
    {
        public string CodexId { get; set; } = string.Empty;
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();

        public List<Relation> Relations { get; set; } = new();

        [NotMapped]
        public Dictionary<string, string> Fields { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    [Owned] 
    public class Relation
    {
        public string TargetId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
