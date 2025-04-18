// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App ColorEnum.cs 
// 2025-04-18 15:15:57  Biwen.AutoClassGen.TestConsole 万雅虎

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Biwen.AutoClassGen.TestConsole
{
    //[AttributeUsage(AttributeTargets.Enum)]
    //public class AutoDescriptionAttribute : Attribute { }


    [AutoDescription]
    public enum ColorEnum
    {
        [Description("Red Color")]
        Red,
        [Description("Green Color")]
        Green,
        [Description("Blue Color")]
        Blue,
        [Description("Yellow Color")]
        Yellow,
        [Description("Cyan Color")]
        Purple,
        [Description("Magenta Color")]
        Orange,
        [Description("Pink Color")]
        Pink,
        [Description("Brown Color")]
        Brown,
        [Description("Gray Color")]
        Gray,
        [Description("Black Color")]
        Black,
        [Display(Name = "White Color")] //tostring()
        White,
        LightBlue, //tostring()

    }


    [AutoDescription]
    public enum AnimalEnum
    {
        [Description("Dog")]
        Dog,
        [Description("Cat")]
        Cat,
        [Description("Bird")]
        Bird,
        [Description("Fish")]
        Fish,
        [Description("Lion")]
        Lion,
        [Description("Tiger")]
        Tiger,
    }


    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T color) where T : Enum
        {
            var field = color.GetType().GetField(color.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? color.ToString();
        }

        public static string GetDisplayName<T>(this T color) where T : Enum
        {
            var field = color.GetType().GetField(color.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            return attribute?.Name ?? color.ToString();
        }
    }

    //public static partial class ColorEnumExtensions
    //{
    //    /// <summary>
    //    /// Description for the enum value of ColorEnum
    //    /// </summary>
    //    /// <param name="val"></param>
    //    /// <returns></returns>
    //    public static string Description<T>(this T val) where T : Enum => val switch
    //    {
    //        // ColorEnum
    //        ColorEnum.Red => "Red Color",
    //        ColorEnum.Green => "Green Color",
    //        ColorEnum.Blue => "Blue Color",
    //        ColorEnum.Yellow => "Yellow Color",
    //        ColorEnum.Purple => "Cyan Color",
    //        ColorEnum.Orange => "Magenta Color",
    //        ColorEnum.Pink => "Pink Color",
    //        ColorEnum.Brown => "Brown Color",
    //        ColorEnum.Gray => "Gray Color",
    //        ColorEnum.Black => "Black Color",
    //        ColorEnum.White => "White Color",

    //        // AnimalEnum
    //        AnimalEnum.Dog => "Dog",
    //        AnimalEnum.Cat => "Cat",
    //        AnimalEnum.Bird => "Bird",
    //        AnimalEnum.Fish => "Fish",
    //        AnimalEnum.Lion => "Lion",
    //        AnimalEnum.Tiger => "Tiger",

    //        // Default
    //        _ => val.ToString()
    //    };
    //}

    //public static class Test
    //{
    //    public static void Main()
    //    {
    //        var color = ColorEnum.Red;
    //        Console.WriteLine(color.GetDescription());
    //        Console.WriteLine(color.GetDisplayName());
    //        Console.WriteLine(color.Description());

    //        Console.WriteLine(color.Description());
    //    }
    //}
}



namespace Biwen.AutoClassGen.TestConsole.Eunms
{
    /// <summary>
    /// 验证冲突,如果不同命名空间下有相同名称的枚举
    /// </summary>
    [AutoDescription]
    public enum AnimalEnum
    {
        [Description("Dog")]
        Dog,
        [Description("Cat")]
        Cat,
        [Description("Bird")]
        Bird,
        [Description("Fish")]
        Fish,
        [Description("Lion")]
        Lion,
        [Description("Tiger")]
        Tiger,
    }
}