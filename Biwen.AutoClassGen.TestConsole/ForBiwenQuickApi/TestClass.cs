// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App TestClass.cs 
// 2025-04-09 03:28:52  Biwen.AutoClassGen.TestConsole 万雅虎


namespace Biwen.AutoClassGen.TestConsole.ForBiwenQuickApi
{
    public class OptionsMultiFieldType<T>
    {
        public T? Value { get; set; }
    }

    public class OptionsFieldType<T>
    {
        public T? Value { get; set; }
    }

    public enum TestEnum
    {
        A,
        B,
        C
    };


    //when you use this class, you will get a error
    //public class DefineError : OptionsFieldType<string>
    //{

    //}

    public class DefineOkay : OptionsFieldType<TestEnum>
    {
    }





}
