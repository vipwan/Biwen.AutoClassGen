// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App Hobbie.cs 
// 2025-10-12 12:20:26  Biwen.AutoClassGen.TestConsole 万雅虎


namespace Biwen.AutoClassGen.TestConsole.Domains;


[AutoCurd<MyDbContext>("Biwen.AutoClassGen.TestConsole.Services.ForCurd")]
public partial class Hobbie
{
    /// <summary>
    /// 
    /// </summary>
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public User User { get; set; } = null!;

    public int UserId { get; set; }
}
