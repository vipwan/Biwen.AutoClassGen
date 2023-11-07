using Biwen.AutoClassGen.TestConsole.Dtos;
using Biwen.AutoClassGen.TestConsole.Entitys;


#pragma warning disable
namespace Biwen.AutoClassGen.TestConsole.Dtos
{

    /// <summary>
    /// to be generated
    /// </summary>
    [AutoDto(typeof(User), nameof(User.Id), "TestCol")]
    public partial class UserDto
    {
    }

    /// <summary>
    /// to be generated more than one
    /// </summary>
    [AutoDto(typeof(User), nameof(User.Email))]

    public partial class User2Dto
    {
    }

    /// <summary>
    /// another way to be generated
    /// </summary>
    [AutoDto<User>(nameof(User.Email), "TestCol")]
    public partial class User3Dto
    {
    }

}

namespace Biwen.AutoClassGen.TestConsole.Entitys
{
    public static partial class UserToUserDtoExtentions
    {
        ///// <summary>
        ///// mapper to UserDto
        ///// </summary>
        ///// <returns></returns>
        //public static UserDto MapperToDto(this User model)
        //{
        //    return new UserDto()
        //    {
        //        FirstName = model.FirstName,
        //        LastName = model.LastName,
        //        Age = model.Age,
        //    };
        //}
    }
}


#pragma warning restore