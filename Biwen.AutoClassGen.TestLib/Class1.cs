using System.ComponentModel.DataAnnotations;

namespace Biwen.AutoClassGen.TestLib
{
    public class TestClass1
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 5)]
        public string Description { get; set; } = null!;

        public string? Sort { get; set; }

    }
}
