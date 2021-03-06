﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
        public DbSet<BonusPoints> BonusPoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>().ToTable("Event");
            modelBuilder.Entity<Option>().ToTable("Option");
            modelBuilder.Entity<Question>().ToTable("Question");
            modelBuilder.Entity<Token>().ToTable("Token");
            modelBuilder.Entity<Vote>().ToTable("Vote");
            modelBuilder.Entity<Choice>().ToTable("Choice");
            modelBuilder.Entity<BonusPoints>().ToTable("BonusPoints");
        }
    }
}
