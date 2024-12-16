using Biwen.AutoClassGen.TestConsole.Dtos;

namespace Biwen.AutoClassGen.TestConsole.Entitys
{
    public class User : Info
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; } = null!;
        /// <summary>
        /// first name
        /// </summary>
        public string FirstName { get; set; } = null!;

        /// <summary>
        /// last name
        /// </summary>
        public string LastName { get; set; } = null!;

        /// <summary>
        /// age
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// fullname
        /// </summary>
        public string? FullName => $"{FirstName} {LastName}";

    }

    public abstract class Info : Other
    {
        /// <summary>
        /// email
        /// </summary>
        public string? Email { get; set; }
    }

    public abstract class Other
    {
        /// <summary>
        /// remark
        /// </summary>
        public string? Remark { get; set; }
    }



    public static class UserExtensions
    {
        public static IQueryable<UserDto> ProjectToDto(this IQueryable<User> q)
        {
            return q.Select(x => new UserDto
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                Age = x.Age,
                Email = x.Email,
                Remark = x.Remark
            });
        }
    }
}