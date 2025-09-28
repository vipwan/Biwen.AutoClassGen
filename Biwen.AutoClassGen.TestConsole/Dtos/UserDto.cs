// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App UserDto.cs 
// 2024-09-26 16:17:44  Biwen.AutoClassGen.TestConsole 万雅虎

using Biwen.AutoClassGen.TestConsole.Dtos;
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


    [AutoDto(typeof(User), nameof(User.Email))]
    public partial record class User4Dto
    {
        public string? Wooo { get; set; }
    }

    [AutoDto<User>(nameof(User.Id))]
    public partial record class User5Dto(int Id)
    {
        /// <summary>
        /// 如果存在主构造函数，必须有对应的参数,否则Mapper ToDto方法会报错
        /// </summary>
        public User5Dto() : this(1) { }

        public string? Wooo { get; set; }
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




    public class MyStaticMapper : IStaticAutoDtoMapper<User, UserMyMapperDto>
    {
        public static void Map(User from, UserMyMapperDto to)
        {
            to.FirstName = from.FirstName + " - mapped by MyStaticMapper";
        }
    }

    [AutoDtoWithMapper<User>(typeof(MyStaticMapper), nameof(User.Id), "TestCol")]
    public partial class UserMyMapperDto;


    [AutoDtoWithMapper<User>(typeof(MyStaticMapper), nameof(User.Id), "TestCol")]
    public partial class UserMyMapperErrorDto; //当前类没有实现IStaticAutoDtoMapper接口会报错


}


namespace Biwen.AutoClassGen.TestConsole.Entitys
{

    public static partial class UserToUserDtoExtentions
    {
        static partial void MapperToPartial(User from, UserDto to)
        {
            to.FirstName = "重写了FirstName";
        }

    }


}