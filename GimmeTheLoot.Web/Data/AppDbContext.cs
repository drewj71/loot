using GimmeTheLoot.Shared.Models.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace GimmeTheLoot.Web.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserPlaidAccount> UserPlaidAccounts => Set<UserPlaidAccount>();
        public DbSet<PlaidAccount> PlaidAccounts => Set<PlaidAccount>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionCategory> TransactionCategory => Set<TransactionCategory>();
    }
}
