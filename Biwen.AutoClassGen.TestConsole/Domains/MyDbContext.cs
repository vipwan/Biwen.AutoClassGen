// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App MyDbContext.cs 
// 2025-10-12 12:23:44  Biwen.AutoClassGen.TestConsole 万雅虎


namespace Biwen.AutoClassGen.TestConsole.Domains;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Hobbie> Hobbies { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // 使用内存数据库进行测试
        optionsBuilder.UseInMemoryDatabase("TestDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Hobbie>().Property(h => h.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<User>().HasMany(u => u.Hobbies).WithOne(h => h.User).HasForeignKey(h => h.UserId);
    }

}
