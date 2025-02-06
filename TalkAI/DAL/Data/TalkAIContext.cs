using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data
{
    public class TalkAIContext : DbContext
    {
        public TalkAIContext(DbContextOptions<TalkAIContext> option) : base(option) { }
        public DbSet<Conversation> Conversations { get; set; }
     public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
