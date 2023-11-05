using Biwen.AutoClassGen.TestConsole.Dtos;
using Biwen.AutoClassGen.TestConsole.Entitys;
using System.Runtime.CompilerServices;


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