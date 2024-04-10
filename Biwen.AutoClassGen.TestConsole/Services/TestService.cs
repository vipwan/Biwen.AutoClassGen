namespace Biwen.AutoClassGen.TestConsole.Services
{

    using Biwen.AutoClassGen.Attributes;

    public interface ITestService
    {
        string Say(string message);
    }

    public interface ITest2Service
    {
        string Say2(string message);
    }


    [AutoInject<TestService>]
    [AutoInject<ITestService>(ServiceLifetime.Singleton)]
    [AutoInject<ITest2Service>(ServiceLifetime.Scoped)]
    public class TestService : ITestService, ITest2Service
    {
        public string Say(string message)
        {
            return $"hello {message}";
        }

        public string Say2(string message)
        {
            return message;
        }
    }


    [AutoInject]
    [AutoInject(serviceLifetime: ServiceLifetime.Transient)]
    [AutoInject(typeof(ITest2Service), ServiceLifetime.Scoped)]
    public class TestService2 : ITest2Service
    {
        public string Say2(string message)
        {
            return message;
        }
    }




    public partial class TestServiceDto
    {
    }

    [AutoInject]
    [AutoDto<TestServiceDto>]
    public partial class TestService3
    {

    }

}