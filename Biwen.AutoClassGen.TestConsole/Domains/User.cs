// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App User.cs 
// 2025-10-12 12:16:30  Biwen.AutoClassGen.TestConsole 万雅虎


using System.ComponentModel.DataAnnotations;

namespace Biwen.AutoClassGen.TestConsole.Domains;

//[AutoCurd<MyDbContext>("Biwen.AutoClassGen.TestConsole.Services.ForCurd")]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [EmailAddress]
    public string? Email { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 兴趣爱好
    /// </summary>
    public ICollection<Hobbie> Hobbies { get; set; } = [];


}
