// DAL/Entities/Conversation.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities
{
    public class Conversation
    {
        [Key]
        public Guid CoversationId{ get; set; }
        public User User { get; set; }
        [ForeignKey("UserId")]  
        public Guid UserId { get; set; } = Guid.Empty;
        public string AudioFilePath { get; set; } 
        public string TextContent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}