// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App UserCurdService.cs 
// 2024-09-19 11:24:20  Biwen.AutoClassGen.TestConsole 万雅虎

using Biwen.AutoClassGen.TestConsole.Domains;

namespace Biwen.AutoClassGen.TestConsole.Services.ForCurd;


public partial interface IUserCurdService
{

    Task<User> CreateAsync(User entity);

    Task UpdateAsync(User entity);

    Task DeleteAsync(User entity);

    Task<User?> GetAsync(params object[] ids);
}


public partial class UserCurdService : IUserCurdService
{
    private readonly ILogger<UserCurdService> _logger;
    private readonly MyDbContext _dbContext;

    public UserCurdService(ILogger<UserCurdService> logger, MyDbContext context)
    {
        _logger = logger;
        _dbContext = context;
    }


    public virtual async Task<User> CreateAsync(User entity)
    {
        _dbContext.Set<User>().Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(User entity)
    {
        _dbContext.Set<User>().Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public virtual async Task<User?> GetAsync(params object[] ids)
    {
        return await _dbContext.Set<User>().FindAsync(ids);
    }

    public virtual async Task UpdateAsync(User entity)
    {
        _dbContext.Set<User>().Update(entity);
        await _dbContext.SaveChangesAsync();
    }
}