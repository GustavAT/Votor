using Microsoft.EntityFrameworkCore;
using Votor.Areas.Portal.Models;

namespace Votor.Areas.Portal.Data
{
    public class VotorContext : DbContext
    {
        public VotorContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Choice> Choices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>().ToTable("Event");
            modelBuilder.Entity<Option>().ToTable("Option");
            modelBuilder.Entity<Question>().ToTable("Question");

            var tokenTable = modelBuilder.Entity<Token>().ToTable("Token");

            var voteTable = modelBuilder.Entity<Vote>().ToTable("Vote");
            voteTable.HasMany(x => x.Choices).WithOne().OnDelete(DeleteBehavior.SetNull);

            var choiceTable = modelBuilder.Entity<Choice>().ToTable("Choice");
            choiceTable.HasOne(x => x.Option).WithMany().OnDelete(DeleteBehavior.Restrict);
            choiceTable.HasOne(x => x.Question).WithMany().OnDelete(DeleteBehavior.Restrict);
            choiceTable.HasOne(x => x.Vote).WithMany().OnDelete(DeleteBehavior.Restrict);

        }
    }
}
