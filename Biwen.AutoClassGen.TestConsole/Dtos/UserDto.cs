// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App UserDto.cs 
// 2024-09-26 16:17:44  Biwen.AutoClassGen.TestConsole 万雅虎

using Biwen.AutoClassGen.TestConsole.Entitys;

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
    //[AutoDto<User>]
    [AutoDto(typeof(User), nameof(User.Email))]
    public partial class User3Dto
    {
    }


    [AutoDto<VenueImage>]
    public partial class VenueImageDto
    {

    }

    [AutoDto<Venue>(nameof(Venue.Images))]
    public partial class VenueDto
    {
        public IList<VenueImageDto>? Images { get; set; }
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